using System;
using System.Collections.Generic;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Locations;
using UnityEngine;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Слоты сессии (система сессии, секция <c>slots</c>): все слоты и порядок сущностей в них. Карта и объект механики —
    /// всегда ровно в одном слоте: создаются сразу в слоте (<see cref="Create"/>), перемещаются атомарно (<see cref="Move"/>:
    /// проверка правил, затем убрать из старого, добавить в новый, событие), при уничтожении сущности убираются из слота.
    /// Ссылок «сущность знает свой слот» нет: место знает только система (<see cref="GetSlot"/>). Слоты: инвентарь игрока
    /// (<see cref="SlotIds.Inventory"/>, всегда) и слоты экспедиции (<see cref="CreateExpedition"/> — служебные и поле из
    /// графа локации; <see cref="DestroyExpedition"/>) с картой героя (<see cref="SetHero"/>). Перемещение — событие C#
    /// <see cref="Moved"/> и <see cref="EntityMoved"/> в <see cref="EventBus"/>. Сейв: локация экспедиции (по её графу слоты
    /// восстанавливаются без сцены), <c>InstanceId</c> героя и непустые слоты со списком <c>InstanceId</c>; секция читается
    /// после <c>entities</c>, содержимое связывается с сущностями в <see cref="ISessionReady.OnSessionReady"/>. Сущность из
    /// сейва в двух слотах остаётся в первом, без слота — уничтожается (с предупреждением); герой не в слоте экспедиции —
    /// предупреждение, героя нет. Ёмкость инвентаря и руки — из <see cref="SlotsConfig"/> реестра контента (при создании
    /// сессии; конфига нет — его значения по умолчанию).
    /// Требует в контейнере <see cref="EntityRegistry"/>, <see cref="IContentRegistry"/> и <see cref="EventBus"/>.
    /// </summary>
    public sealed class SlotSystem : SaveableSystem<SlotsSave>, ISessionReady
    {
        public const string Key = "slots";

        /// <summary>После секции сущностей.</summary>
        public const int Order = EntityRegistry.Order + 10;

        private const string LogSource = "[Saves] Section '" + Key + "'";

        /// <summary>Колода экспедиции: карты рубашкой вверх.</summary>
        public static readonly SlotRules DeckRules = new SlotRules(new[] { EntityKind.Card }, 0, SlotFacing.FaceDown);

        /// <summary>Сброс: карты лицом вверх.</summary>
        public static readonly SlotRules DiscardRules = new SlotRules(new[] { EntityKind.Card }, 0, SlotFacing.FaceUp);

        /// <summary>Рюкзак: карты как лежали.</summary>
        public static readonly SlotRules BackpackRules = new SlotRules(new[] { EntityKind.Card });

        /// <summary>Клетка поля: карты, несколько, как лежали (переворот — правила хода), хуки размещения.</summary>
        public static readonly SlotRules CellRules = new SlotRules(new[] { EntityKind.Card }, invokePlaceEvent: true);

        /// <summary>Слот связи: один объект механики.</summary>
        public static readonly SlotRules LinkRules = new SlotRules(new[] { EntityKind.Mechanic }, 1);

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private EntityRegistry m_Entities;

        [Inject]
        private IContentRegistry m_Content;

        [Inject]
        private EventBus m_Events;
#pragma warning restore CS0649

        private readonly List<Slot> m_Slots = new List<Slot>();
        private readonly Dictionary<string, Slot> m_ById = new Dictionary<string, Slot>(StringComparer.Ordinal);
        private readonly Dictionary<Entity, Slot> m_Places = new Dictionary<Entity, Slot>();
        private SlotsSave m_Pending;

        public override string SaveKey => Key;

        public override int LoadOrder => Order;

        /// <summary>Все слоты в порядке создания.</summary>
        public IReadOnlyList<Slot> Slots => m_Slots;

        /// <summary>Инвентарь игрока (<see cref="SlotIds.Inventory"/>).</summary>
        public Slot Inventory { get; private set; }

        /// <summary>Слоты активной экспедиции; нет экспедиции — null.</summary>
        public ExpeditionSlots Expedition { get; private set; }

        /// <summary>Инвентарь игрока: карты, как лежали, ёмкость — <see cref="SlotsConfig.InventoryCapacity"/>.</summary>
        public SlotRules InventoryRules { get; private set; }

        /// <summary>Рука экспедиции: карты лицом вверх, ёмкость — <see cref="SlotsConfig.HandCapacity"/>.</summary>
        public SlotRules HandRules { get; private set; }

        /// <summary>Сущность перемещена; вызывается до события в шине.</summary>
        public event Action<EntityMoved> Moved;

        public override void Init()
        {
            m_Entities.Detaching += HandleDetaching;

            var configs = m_Content.GetAll<SlotsConfig>();
            var config = configs.Count > 0 ? configs[0] : null;
            var inventory = config != null ? config.InventoryCapacity : SlotsConfig.DefaultInventoryCapacity;
            var hand = config != null ? config.HandCapacity : SlotsConfig.DefaultHandCapacity;
            InventoryRules = new SlotRules(new[] { EntityKind.Card }, inventory);
            HandRules = new SlotRules(new[] { EntityKind.Card }, hand, SlotFacing.FaceUp);
        }

        /// <summary>Недособранная сессия (ошибка инжекта) тоже уничтожается: реестра может не быть.</summary>
        public override void Dispose()
        {
            if (m_Entities != null)
                m_Entities.Detaching -= HandleDetaching;
        }

#region Lookup

        public bool TryGet(string id, out Slot slot)
        {
            slot = null;
            return id != null && m_ById.TryGetValue(id, out slot);
        }

        public Slot Get(string id)
        {
            if (TryGet(id, out var slot))
                return slot;

            throw new KeyNotFoundException($"Slot '{id}' does not exist");
        }

        /// <summary>Слот сущности; модификатор или уничтоженная сущность — null.</summary>
        public Slot GetSlot(Entity entity)
        {
            return entity != null && m_Places.TryGetValue(entity, out var slot) ? slot : null;
        }

        /// <summary>Слот существует в системе (ссылка на слот уничтоженной экспедиции — нет).</summary>
        public bool Contains(Slot slot)
        {
            return slot != null && m_ById.TryGetValue(slot.Id, out var own) && own == slot;
        }

#endregion

#region Placement

        /// <summary>Сущность из конфига можно создать в слоте: вид не модификатор, правила слота, ёмкость.</summary>
        public bool CanCreate(EntityModel model, Slot slot, out string reason)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!CheckSlot(slot, out reason))
                return false;

            if (model.Kind == EntityKind.Modifier)
            {
                reason = $"{model} is a modifier: modifiers are created on their owner";
                return false;
            }

            if (!slot.Rules.Accepts(model, out reason))
                return false;

            return CheckCapacity(slot, 0, out reason);
        }

        /// <summary>
        /// Создать сущность из конфига сразу в слоте на место <paramref name="index"/> (-1 — в конец) и положить по правилам
        /// слота. Нарушение правил — <see cref="InvalidOperationException"/>, ничего не создаётся.
        /// </summary>
        public Entity Create(EntityModel model, Slot slot, int index = -1)
        {
            if (!CanCreate(model, slot, out var reason))
                throw new InvalidOperationException($"Can't create {model} in {slot?.ToString() ?? "null"}: {reason}");

            CheckIndex(index, slot.Count);
            var entity = m_Entities.Instantiate(model);
            Place(entity, slot, index < 0 ? slot.Count : index);
            ApplyFacing(entity, slot.Rules);
            m_Entities.NotifyCreated(entity);
            return entity;
        }

        /// <summary>Сущность можно переместить в слот: она жива и лежит в слоте, правила и ёмкость целевого слота.</summary>
        public bool CanMove(Entity entity, Slot to, out string reason)
        {
            if (!m_Entities.Contains(entity))
            {
                reason = $"{entity?.ToString() ?? "null"} is destroyed or not from this session";
                return false;
            }

            var from = GetSlot(entity);
            if (from == null)
            {
                reason = entity.Kind == EntityKind.Modifier ? $"{entity} is a modifier: it is on its owner, not in a slot" : $"{entity} is in no slot";
                return false;
            }

            if (!CheckSlot(to, out reason) || !to.Rules.Accepts(entity.Model, out reason))
                return false;

            return CheckCapacity(to, from == to ? 1 : 0, out reason);
        }

        /// <summary>
        /// Атомарно переместить сущность в слот на место <paramref name="index"/> в нём (-1 — в конец; в том же слоте —
        /// перестановка, индекс — после изъятия): проверка правил, затем убрать из старого слота, добавить в новый, положить по
        /// правилам слота (если слот сменился), событие. Нарушение — исключение, ничего не меняется. Место не меняется — без
        /// события.
        /// </summary>
        public void Move(Entity entity, Slot to, int index = -1)
        {
            if (!CanMove(entity, to, out var reason))
                throw new InvalidOperationException($"Can't move {entity?.ToString() ?? "null"} to {to?.ToString() ?? "null"}: {reason}");

            var from = GetSlot(entity);
            var fromIndex = from.IndexOf(entity);
            var count = from == to ? to.Count - 1 : to.Count;
            CheckIndex(index, count);
            var toIndex = index < 0 ? count : index;
            if (from == to && fromIndex == toIndex)
                return;

            from.RemoveAt(fromIndex);
            m_Places.Remove(entity);
            Place(entity, to, toIndex);
            if (from != to)
                ApplyFacing(entity, to.Rules);

            if (Expedition != null && Expedition.Hero == entity && !Expedition.Contains(to))
                Expedition.Hero = null;

            var moved = new EntityMoved(entity, from, fromIndex, to, toIndex);
            Moved?.Invoke(moved);
            m_Events.Invoke(moved);
        }

        private bool CheckSlot(Slot slot, out string reason)
        {
            reason = null;
            if (Contains(slot))
                return true;

            reason = slot == null ? "the slot is null" : $"{slot} does not exist (the expedition is over?)";
            return false;
        }

        /// <summary>Ёмкость; <paramref name="leaving"/> — сколько сущностей уйдёт из слота при операции (перестановка).</summary>
        private static bool CheckCapacity(Slot slot, int leaving, out string reason)
        {
            reason = null;
            if (slot.Rules.Capacity == 0 || slot.Count - leaving < slot.Rules.Capacity)
                return true;

            reason = $"{slot} is full ({slot.Rules.Capacity})";
            return false;
        }

        private static void CheckIndex(int index, int count)
        {
            if (index < -1 || index > count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index in the slot is -1 (the end) or 0..{count}");
        }

        private void Place(Entity entity, Slot slot, int index)
        {
            slot.Insert(index, entity);
            m_Places.Add(entity, slot);
        }

        private static void ApplyFacing(Entity entity, SlotRules rules)
        {
            if (rules.Facing != SlotFacing.Keep && entity.TryGet<CardBrain>(out var brain))
                brain.SetFaceUp(rules.Facing == SlotFacing.FaceUp);
        }

        private void HandleDetaching(Entity entity)
        {
            if (Expedition != null && Expedition.Hero == entity)
                Expedition.Hero = null;

            if (!m_Places.TryGetValue(entity, out var slot))
                return;

            slot.RemoveAt(slot.IndexOf(entity));
            m_Places.Remove(entity);
        }

#endregion

#region Expedition

        /// <summary>
        /// Слоты экспедиции в локации: служебные (<see cref="SlotIds.Deck"/>, <see cref="SlotIds.Hand"/>,
        /// <see cref="SlotIds.Discard"/>, <see cref="SlotIds.Backpack"/>) и поле из графа <see cref="LocationModel.Graph"/>
        /// (клетки и слоты связей). Экспедиция уже есть или у локации нет графа — <see cref="InvalidOperationException"/>.
        /// </summary>
        public ExpeditionSlots CreateExpedition(LocationModel location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            if (Expedition != null)
                throw new InvalidOperationException($"Expedition slots of {Expedition.Location} already exist");

            if (location.Graph == null)
                throw new InvalidOperationException($"{location} has no field graph");

            var expedition = new ExpeditionSlots(location,
                new Slot(SlotIds.Deck, DeckRules),
                new Slot(SlotIds.Hand, HandRules),
                new Slot(SlotIds.Discard, DiscardRules),
                new Slot(SlotIds.Backpack, BackpackRules),
                new FieldSlots(location.Graph, CellRules, LinkRules));

            foreach (var slot in expedition.All)
                Register(slot);

            Expedition = expedition;
            return expedition;
        }

        /// <summary>
        /// Уничтожить слоты экспедиции вместе с сущностями, которые в них остались (перенос в хаб — до этого, правилами
        /// конца экспедиции). Экспедиции нет — <see cref="InvalidOperationException"/>.
        /// </summary>
        public void DestroyExpedition()
        {
            if (Expedition == null)
                throw new InvalidOperationException("There are no expedition slots");

            foreach (var slot in Expedition.All)
            {
                for (var i = slot.Count - 1; i >= 0; i--)
                    m_Entities.Destroy(slot.Entities[i]);
            }

            foreach (var slot in Expedition.All)
            {
                m_ById.Remove(slot.Id);
                m_Slots.Remove(slot);
            }

            Expedition = null;
        }

        /// <summary>
        /// Карта героя экспедиции (<see cref="ExpeditionSlots.Hero"/>): живая карта в слоте экспедиции; null — героя нет.
        /// Ставит раскладка (шаг 5 E03). Сбрасывается при уничтожении карты, при перемещении её из слотов экспедиции и
        /// вместе с экспедицией. Экспедиции нет или карта не подходит — <see cref="InvalidOperationException"/>.
        /// </summary>
        public void SetHero(Entity card)
        {
            if (Expedition == null)
                throw new InvalidOperationException("There are no expedition slots");

            if (card == null)
            {
                Expedition.Hero = null;
                return;
            }

            if (!m_Entities.Contains(card) || card.Kind != EntityKind.Card)
                throw new InvalidOperationException($"Can't make {card} the hero: it is destroyed, not from this session or not a card");

            if (!Expedition.Contains(GetSlot(card)))
                throw new InvalidOperationException($"Can't make {card} the hero: it is not in an expedition slot");

            Expedition.Hero = card;
        }

        private void Register(Slot slot)
        {
            if (m_ById.ContainsKey(slot.Id))
                throw new InvalidOperationException($"{slot} already exists");

            m_ById.Add(slot.Id, slot);
            m_Slots.Add(slot);
        }

#endregion

#region Save

        protected override SlotsSave Write()
        {
            var save = new SlotsSave { Expedition = Expedition?.Location.Id, Hero = Expedition?.Hero?.InstanceId };
            foreach (var slot in m_Slots)
            {
                if (slot.IsEmpty)
                    continue;

                var item = new SlotSave { Id = slot.Id };
                foreach (var entity in slot.Entities)
                    item.Entities.Add(entity.InstanceId);

                save.Slots.Add(item);
            }

            return save;
        }

        /// <summary>Структура слотов: инвентарь и слоты экспедиции по локации; содержимое — в <see cref="OnSessionReady"/>.</summary>
        protected override void Read(SlotsSave save)
        {
            Reset();
            m_Pending = save;
            if (string.IsNullOrEmpty(save.Expedition))
                return;

            if (!m_Content.TryGet<LocationModel>(save.Expedition, out var location))
                Debug.LogWarning($"{LogSource}: expedition location '{save.Expedition}' is not in the content, the expedition slots are not restored");
            else if (location.Graph == null)
                Debug.LogWarning($"{LogSource}: expedition location '{save.Expedition}' has no field graph, the expedition slots are not restored");
            else
                CreateExpedition(location);
        }

        protected override void ReadDefaults()
        {
            Reset();
            m_Pending = null;
        }

        /// <summary>Содержимое слотов из сейва; карты и объекты механики без слота — уничтожаются.</summary>
        public void OnSessionReady()
        {
            var pending = m_Pending;
            m_Pending = null;
            if (pending != null)
                Fill(pending);

            var lost = new List<Entity>();
            foreach (var entity in m_Entities.Entities)
            {
                if (entity.Kind != EntityKind.Modifier && !m_Places.ContainsKey(entity))
                    lost.Add(entity);
            }

            lost.Sort((a, b) => a.InstanceId.CompareTo(b.InstanceId));
            foreach (var entity in lost)
            {
                Debug.LogWarning($"{LogSource}: {entity} is in no slot, it is removed");
                m_Entities.Destroy(entity);
            }

            if (pending?.Hero != null)
                RestoreHero(pending.Hero.Value);
        }

        /// <summary>Герой из сейва: живая карта в слоте экспедиции; иначе — предупреждение, героя нет.</summary>
        private void RestoreHero(int id)
        {
            if (Expedition == null)
            {
                Debug.LogWarning($"{LogSource}: hero {id}: there are no expedition slots, the hero is not restored");
                return;
            }

            if (!m_Entities.TryGet(id, out var hero) || hero.Kind != EntityKind.Card)
            {
                Debug.LogWarning($"{LogSource}: hero {id} is not a restored card, the expedition has no hero");
                return;
            }

            if (!Expedition.Contains(GetSlot(hero)))
            {
                Debug.LogWarning($"{LogSource}: hero {hero} is not in an expedition slot, the expedition has no hero");
                return;
            }

            Expedition.Hero = hero;
        }

        private void Fill(SlotsSave save)
        {
            foreach (var item in save.Slots ?? new List<SlotSave>())
            {
                if (item == null)
                    continue;

                if (!TryGet(item.Id, out var slot))
                {
                    Debug.LogWarning($"{LogSource}: slot '{item.Id}' does not exist any more, its {item.Entities?.Count ?? 0} entities have no slot");
                    continue;
                }

                foreach (var id in item.Entities ?? new List<int>())
                    Restore(slot, id);
            }
        }

        private void Restore(Slot slot, int id)
        {
            if (!m_Entities.TryGet(id, out var entity))
            {
                Debug.LogWarning($"{LogSource}: entity {id} in {slot} is not restored, skipped");
                return;
            }

            if (entity.Kind == EntityKind.Modifier)
            {
                Debug.LogWarning($"{LogSource}: {entity} in {slot} is a modifier: modifiers are on their owner, skipped");
                return;
            }

            if (m_Places.TryGetValue(entity, out var other))
            {
                Debug.LogError($"{LogSource}: {entity} is in two slots ({other.Id}, {slot.Id}), it stays in {other.Id}");
                return;
            }

            Place(entity, slot, slot.Count);
        }

        /// <summary>Только инвентарь, без экспедиции и содержимого.</summary>
        private void Reset()
        {
            m_Slots.Clear();
            m_ById.Clear();
            m_Places.Clear();
            Expedition = null;
            Inventory = new Slot(SlotIds.Inventory, InventoryRules);
            Register(Inventory);
        }

#endregion
    }
}
