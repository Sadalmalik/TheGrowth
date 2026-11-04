using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Entities;
using TheGrowth.Model;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Graphs;
using TheGrowth.Tests.EditMode.Locations;
using TheGrowth.Tests.EditMode.Saves;
using TheGrowth.Tests.EditMode.Scripting;
using TheGrowth.Tests.EditMode.Variables;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Entities
{
#region Test components

    /// <summary>Тестовый компонент с состоянием в сейве: счётчик с начальным значением из настроек.</summary>
    [Serializable]
    [ComponentKey("test_counter")]
    public sealed class CounterSettings : ComponentSettings<CounterBehaviour>
    {
        public int Start;
    }

    public sealed class CounterBehaviour : ComponentBehaviour<CounterSettings, CounterState>
    {
        public int Value;

        protected override void OnCreated()
        {
            Value = Settings.Start;
        }

        protected override CounterState WriteState()
        {
            return new CounterState { Value = Value };
        }

        protected override void ReadState(CounterState state)
        {
            Value = state.Value;
        }
    }

    public sealed class CounterState
    {
        public int Value;
    }

    /// <summary>Компонент без состояния.</summary>
    [Serializable]
    [ComponentKey("test_marker")]
    public sealed class MarkerSettings : ComponentSettings<MarkerBehaviour>
    {
    }

    public sealed class MarkerBehaviour : ComponentBehaviour<MarkerSettings>
    {
    }

    /// <summary>Компонент со ссылкой на модификатор: для проверки <see cref="ModelKindAttribute"/> внутри <c>[SerializeReference]</c>.</summary>
    [Serializable]
    [ComponentKey("test_link")]
    public sealed class LinkSettings : ComponentSettings<LinkBehaviour>
    {
        [ModelKind(EntityKind.Modifier)]
        public EntityModel Modifier;
    }

    public sealed class LinkBehaviour : ComponentBehaviour<LinkSettings>
    {
    }

    /// <summary>Ошибка кода: без ключа.</summary>
    [Serializable]
    public sealed class KeylessSettings : ComponentSettings<KeylessBehaviour>
    {
    }

    public sealed class KeylessBehaviour : ComponentBehaviour<KeylessSettings>
    {
    }

    /// <summary>Ошибка кода: ключ другого типа.</summary>
    [Serializable]
    [ComponentKey("test_counter")]
    public sealed class DuplicateCounterSettings : ComponentSettings<DuplicateCounterBehaviour>
    {
    }

    public sealed class DuplicateCounterBehaviour : ComponentBehaviour<DuplicateCounterSettings>
    {
    }

#endregion

#region Script nodes

    /// <summary>Данные контекста: активная сущность.</summary>
    public sealed class ActiveEntity : IContextData
    {
        public ActiveEntity(Entity entity)
        {
            Entity = entity;
        }

        public Entity Entity { get; }
    }

    /// <summary>Владелец переменных карты в скрипте: <c>[active entity].charges</c>.</summary>
    [Script("test_active_entity", "active entity")]
    public sealed class ActiveEntityEvaluator : Evaluator<Entity>
    {
        public override Entity Evaluate(ScriptContext context)
        {
            return context.GetRequired<ActiveEntity>().Entity;
        }
    }

    /// <summary>Параметр-конфиг только вида «модификатор».</summary>
    [Script("test_modify_with", "modify with [Model]")]
    public sealed class ModifierWithCommand : InstantCommand
    {
        [ModelKind(EntityKind.Modifier)]
        public EntityModel Model;

        protected override void Run(ScriptContext context)
        {
        }
    }

    /// <summary>Список конфигов видов «карта» и «объект механики».</summary>
    [Script("test_place", "place [Models]")]
    public sealed class PlaceCommand : InstantCommand
    {
        [ModelKind(EntityKind.Card, EntityKind.Mechanic)]
        public List<EntityModel> Models;

        protected override void Run(ScriptContext context)
        {
        }
    }

#endregion

    /// <summary>
    /// Контент и сессии для тестов сущностей и слотов: теги (<c>hero</c>, <c>monster</c>, <c>resource</c>, <c>exit</c>),
    /// переменные (<see cref="VariableTestKit.Standard"/> и карточная <c>mood</c>), конфиги сущностей пресетами видов
    /// (<see cref="Hero"/>, <see cref="Monster"/> с <c>charges = 3</c>, <see cref="Herb"/>, <see cref="Exit"/>,
    /// <see cref="Counter"/> со счётчиком, модификаторы <see cref="Poison"/> и <see cref="Burn"/>, объект механики
    /// <see cref="Wall"/>), экспедиционная локация с графом поля (клетки <c>s1 - s2 - s3</c> связями <c>k1</c>, <c>k2</c> и
    /// отдельная <c>s4</c> с тегом <c>PlayerSpawn</c>), локация без графа; глобальная область для сессий.
    /// </summary>
    public sealed class EntityTestKit : IDisposable
    {
        private static ScriptRegistry m_Language;

        public EntityTestKit()
        {
            Content = new ContentTestKit();
            Graphs = new GraphTestKit();

            Tags = Content.Asset<TagDeclarations>("Tags");
            Tags.SetTags(new[] { new TagDeclaration("Hero"), new TagDeclaration("Monster"), new TagDeclaration("Resource"), new TagDeclaration("Exit") });
            Variables = VariableTestKit.Declarations(Content, "Variables", VariableTestKit.Standard()
                .Append(new VariableDeclaration("mood", VariableScope.Card, VariableType.String, "calm")).ToArray());

            Hero = Model("Hero", EntityKind.Card, "Hero");
            Monster = Model("Monster", EntityKind.Card, "Monster");
            Monster.VariableList.Add(new EntityVariable("charges", "3"));
            Monster.VariableList.Add(new EntityVariable("mood"));
            Herb = Model("Herb", EntityKind.Card, "Resource");
            Exit = Model("Exit", EntityKind.Card, "Exit");
            Counter = Model("Counter", EntityKind.Card);
            ((CounterSettings)EntityModelEditing.Add(Counter, typeof(CounterSettings))).Start = 5;
            Poison = Model("Poison", EntityKind.Modifier);
            Burn = Model("Burn", EntityKind.Modifier);
            Wall = Model("Wall", EntityKind.Mechanic);

            Graph = Graphs.Graph("Field");
            Graph.SlotList.Add(new GraphSlot("s1", new Vector3(0f, 0f, 0f)));
            Graph.SlotList.Add(new GraphSlot("s2", new Vector3(1f, 0f, 0f)));
            Graph.SlotList.Add(new GraphSlot("s3", new Vector3(2f, 0f, 0f)));
            var spawn = new GraphSlot("s4", new Vector3(0f, 0f, 5f));
            spawn.TagList.Add("PlayerSpawn");
            Graph.SlotList.Add(spawn);
            Graph.LinkList.Add(new GraphLink("k1", "s1", "s2"));
            Graph.LinkList.Add(new GraphLink("k2", "s2", "s3"));
            Location = GraphTestKit.Location(Content, "Field", "Field", Graph);
            NoField = LocationTestKit.Location(Content, "NoField", "NoField", LocationType.Expedition);

            Registry = Content.Registry(new ContentAsset[] { Tags, Variables, Hero, Monster, Herb, Exit, Counter, Poison, Burn, Wall, Location, NoField });
            Global = Content.Global(Registry);
        }

        /// <summary>Язык тестов: встроенные узлы, владелец-сущность, команды с <see cref="ModelKindAttribute"/>.</summary>
        public static ScriptRegistry Language => m_Language ??= ScriptRegistry.Build(ScriptLanguageTypes.All().Concat(new[]
        {
            typeof(ActiveEntityEvaluator),
            typeof(ModifierWithCommand),
            typeof(PlaceCommand),
        }));

        public ContentTestKit Content { get; }

        public GraphTestKit Graphs { get; }

        public TagDeclarations Tags { get; }

        public VariableDeclarations Variables { get; }

        public EntityModel Hero { get; }

        public EntityModel Monster { get; }

        public EntityModel Herb { get; }

        public EntityModel Exit { get; }

        public EntityModel Counter { get; }

        public EntityModel Poison { get; }

        public EntityModel Burn { get; }

        public EntityModel Wall { get; }

        public GraphModel Graph { get; }

        public LocationModel Location { get; }

        public LocationModel NoField { get; }

        public ContentRegistry Registry { get; }

        public LifetimeScope Global { get; }

        public VariableCatalog Catalog => Global.Get<VariableCatalog>();

        public VariableDeclaration Declaration(string name)
        {
            return Catalog.Get(name);
        }

        /// <summary>Конфиг сущности как из пресета вида (обязательные компоненты), с тегами.</summary>
        public EntityModel Model(string id, EntityKind kind, params string[] tags)
        {
            var model = Content.Asset<EntityModel>(id);
            EntityModelEditing.SetKind(model, kind);
            model.TagList.AddRange(tags);
            return model;
        }

        public Session NewSession()
        {
            return Session.Create(Global, new SaveGame());
        }

        /// <summary>Сессия из снимка через архив (как загрузка слота).</summary>
        public Session Reload(Session session)
        {
            var save = session.Capture();
            return Session.Create(Global, SaveTestKit.Unpack(SaveTestKit.Pack(save)));
        }

        public Session Load(SaveGame save)
        {
            return Session.Create(Global, save);
        }

        public ScriptParser Parser()
        {
            return new ScriptParser(Language, new ContentScriptResolver(Registry), new VariableScriptResolver(Catalog));
        }

        /// <summary>
        /// Инвариант места: каждая живая карта и объект механики — ровно в одном слоте, и система знает этот слот; в слотах
        /// только живые не-модификаторы; модификатор — у живого владельца, в его списке, не в слоте.
        /// </summary>
        public static void AssertPlacement(Session session)
        {
            var entities = session.Get<EntityRegistry>();
            var slots = session.Get<SlotSystem>();
            var counts = new Dictionary<Entity, int>();
            foreach (var slot in slots.Slots)
            {
                foreach (var entity in slot.Entities)
                {
                    Assert.IsTrue(entities.Contains(entity), $"{entity} in {slot} is destroyed");
                    Assert.AreNotEqual(EntityKind.Modifier, entity.Kind, $"{entity} is a modifier in {slot}");
                    Assert.AreSame(slot, slots.GetSlot(entity), $"{entity}: the system knows another slot");
                    counts[entity] = counts.TryGetValue(entity, out var count) ? count + 1 : 1;
                }
            }

            foreach (var entity in entities.Entities)
            {
                if (entity.Kind == EntityKind.Modifier)
                {
                    Assert.IsNull(slots.GetSlot(entity), $"{entity} is a modifier in a slot");
                    Assert.IsNotNull(entity.Owner, $"{entity} has no owner");
                    Assert.IsTrue(entities.Contains(entity.Owner), $"owner of {entity} is destroyed");
                    Assert.That(entity.Owner.Modifiers, Has.Exactly(1).SameAs(entity), $"{entity} must be once in the modifiers of its owner");
                    continue;
                }

                Assert.AreEqual(1, counts.TryGetValue(entity, out var placed) ? placed : 0, $"{entity} must be in exactly one slot");
            }
        }

        /// <summary>Раскладка: <c>слот:[ID модели#InstanceId, ...]</c> по всем непустым слотам — для сравнения до и после.</summary>
        public static string Describe(SlotSystem slots)
        {
            var builder = new StringBuilder();
            foreach (var slot in slots.Slots)
            {
                if (slot.IsEmpty)
                    continue;

                builder.Append(slot.Id).Append(":[");
                builder.Append(string.Join(",", slot.Entities.Select(entity => $"{entity.Model.Id}#{entity.InstanceId}")));
                builder.Append("] ");
            }

            return builder.ToString().TrimEnd();
        }

        public void Dispose()
        {
            Global.Dispose();
            Graphs.Dispose();
            Content.Dispose();
        }
    }
}
