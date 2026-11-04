using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Rng;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Slots;
using UnityEngine;

namespace TheGrowth.Model.Decks
{
    /// <summary>
    /// Начальная раскладка поля экспедиции (система сессии, без состояния). Экспедиция уже создана
    /// (<see cref="SlotSystem.CreateExpedition"/>), карта героя — живая карта в любом слоте (например, в инвентаре).
    /// <list type="number">
    /// <item>Колода экспедиции наполняется картами строк колоды локации (<c>LocationModel.Deck</c>, создание в
    /// <see cref="SlotIds.Deck"/>) и перемешивается ГСЧ сессии (<see cref="RngSystem"/>). Раздача идёт сверху колоды; карты строк
    /// «не раскладывать» остаются в колоде.</item>
    /// <item>Клетка героя выбирается сразу — случайная свободная клетка с тегом <see cref="PlayerSpawnTag"/>, чтобы её не заняли
    /// карты с <c>SpawnSlot</c>; свободной нет — <see cref="InvalidOperationException"/> до любых изменений.</item>
    /// <item>Карты со свойством <c>SpawnSlot</c> — первыми: каждая занимает случайную свободную клетку поля из набора свойства
    /// (слоты набора не на поле не учитываются); свободной нет — карта остаётся в колоде, предупреждение в лог.</item>
    /// <item>Остальные — по одной в случайные свободные клетки без тега <see cref="PlayerSpawnTag"/>; клетки кончились —
    /// остаток в колоде, карт меньше — клетки пустые.</item>
    /// <item>Герой — последним, лицом вверх; <see cref="SlotSystem.SetHero"/>.</item>
    /// <item><c>OnPlacedFirstTime</c> (<see cref="CardSystem.InvokeHook"/>) — отдельным циклом в порядке раздачи, у героя
    /// последним. Хук каждой карты — отдельный запуск: ошибка в хуке одной карты не прерывает раскладку.</item>
    /// </list>
    /// Карты двигаются через <see cref="SlotSystem"/> напрямую — без хуков размещения и анимаций. Вся раскладка — один запуск
    /// <see cref="ScriptRunner.RunCode"/>: пока она идёт, точки сохранения нет.
    /// </summary>
    public sealed class DealSystem : SharedObject
    {
        /// <summary>Тег клеток поля, где начинает герой; обычные карты на них не раскладываются.</summary>
        public const string PlayerSpawnTag = "PlayerSpawn";

        private const string LogSource = "[Deal]";

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private SlotSystem m_Slots;

        [Inject]
        private EntityRegistry m_Entities;

        [Inject]
        private CardSystem m_Cards;

        [Inject]
        private RngSystem m_Rng;

        [Inject]
        private ScriptRunner m_Runner;
#pragma warning restore CS0649

        /// <summary>
        /// Разложить колоду локации экспедиции и героя. Экспедиции нет, герой не живая карта или на поле нет свободной клетки
        /// <see cref="PlayerSpawnTag"/> — <see cref="InvalidOperationException"/>, ничего не меняется.
        /// </summary>
        public UniTask Deal(Entity hero)
        {
            var expedition = m_Slots.Expedition;
            if (expedition == null)
                return UniTask.FromException(new InvalidOperationException("Can't deal: there are no expedition slots (create the expedition first)"));

            if (!m_Entities.Contains(hero) || hero.Kind != EntityKind.Card)
                return UniTask.FromException(new InvalidOperationException($"Can't deal {expedition.Location}: the hero {hero?.ToString() ?? "null"} is destroyed, not from this session or not a card"));

            var spawns = expedition.Field.Cells.Where(cell => cell.IsEmpty && cell.HasTag(PlayerSpawnTag)).ToList();
            if (spawns.Count == 0)
            {
                var total = expedition.Field.Cells.Count(cell => cell.HasTag(PlayerSpawnTag));
                return UniTask.FromException(new InvalidOperationException(
                    $"Can't deal {expedition.Location}: no free field slot tagged {PlayerSpawnTag} for the hero {hero} ({total} slot(s) with the tag in the graph)"));
            }

            return m_Runner.RunCode($"deal {expedition.Location.Id}", context => DealAsync(expedition, hero, spawns, context));
        }

        private async UniTask DealAsync(ExpeditionSlots expedition, Entity hero, List<Slot> spawns, ScriptContext context)
        {
            var kept = Fill(expedition);
            Shuffle(expedition.Deck);
            var heroCell = spawns[m_Rng.Index(spawns.Count)];

            var cells = new HashSet<Slot>(expedition.Field.Cells);
            var order = expedition.Deck.Entities.Reverse().Where(card => !kept.Contains(card)).ToList();
            var dealt = new List<Entity>();
            var ordinary = new List<Entity>();
            foreach (var card in order)
            {
                if (!HasSpawnSlot(card))
                {
                    ordinary.Add(card);
                    continue;
                }

                var spawn = m_Cards.TryEvaluate<SlotSet>(card, CardProperties.SpawnSlot, out var set) ? set : SlotSet.Empty;
                var free = spawn.Where(slot => slot != heroCell && slot.IsEmpty && cells.Contains(slot)).ToList();
                if (free.Count == 0)
                {
                    Debug.LogWarning($"{LogSource} {expedition.Location}: {card} has no free field slot in its {CardProperties.SpawnSlot} {spawn}, the card stays in the deck");
                    continue;
                }

                m_Slots.Move(card, free[m_Rng.Index(free.Count)]);
                dealt.Add(card);
            }

            var open = expedition.Field.Cells.Where(cell => cell.IsEmpty && !cell.HasTag(PlayerSpawnTag)).ToList();
            foreach (var card in ordinary)
            {
                if (open.Count == 0)
                    break;

                var index = m_Rng.Index(open.Count);
                m_Slots.Move(card, open[index]);
                open.RemoveAt(index);
                dealt.Add(card);
            }

            m_Slots.Move(hero, heroCell);
            if (hero.TryGet<CardBrain>(out var brain))
                brain.SetFaceUp(true);

            m_Slots.SetHero(hero);

            foreach (var card in dealt)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                await m_Cards.InvokeHook(card, CardHooks.OnPlacedFirstTime);
            }

            context.Cancellation.ThrowIfCancellationRequested();
            await m_Cards.InvokeHook(hero, CardHooks.OnPlacedFirstTime);
        }

        /// <summary>Карты строк колоды локации — в колоду экспедиции; результат — карты строк «не раскладывать».</summary>
        private HashSet<Entity> Fill(ExpeditionSlots expedition)
        {
            var kept = new HashSet<Entity>();
            var deck = expedition.Location.Deck;
            if (deck == null)
                return kept;

            foreach (var entry in deck.Entries)
            {
                if (entry?.Model == null || entry.Count < 1)
                {
                    Debug.LogWarning($"{LogSource} {deck}: row '{entry?.ToString() ?? "null"}' has no card or count below 1, skipped");
                    continue;
                }

                for (var i = 0; i < entry.Count; i++)
                {
                    var card = m_Slots.Create(entry.Model, expedition.Deck);
                    if (entry.KeepInDeck)
                        kept.Add(card);
                }
            }

            return kept;
        }

        /// <summary>Перемешать слот (Фишер — Йетс): новый порядок собирается перемещениями в конец слота.</summary>
        private void Shuffle(Slot slot)
        {
            var cards = slot.Entities.ToList();
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var j = m_Rng.Index(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

            foreach (var card in cards)
                m_Slots.Move(card, slot);
        }

        private bool HasSpawnSlot(Entity card)
        {
            return m_Cards.TryGetScript(card, out var file) && file.TryGetProperty(CardProperties.SpawnSlot, out _);
        }
    }
}
