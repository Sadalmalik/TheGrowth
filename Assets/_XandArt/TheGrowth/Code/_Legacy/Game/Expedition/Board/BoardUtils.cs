using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.Utils;
using XandArt.TheGrowth.Slots;

namespace XandArt.TheGrowth
{
    public static class BoardUtils
    {
        public static void MoveTo(
            this CompositeEntity card,
            SlotEntity target,
            Action onMoveComplete = null,
            bool instant = false,
            bool cardEvents = true)
        {
            var brain = card.GetComponent<CardBrain.Component>();

            var cardUncover = (CompositeEntity)null;
            var cardCover = (CompositeEntity)null;

            if (brain.Slot != null)
            {
                brain.Slot.Remove(card);
                cardUncover = brain.Slot.Top();
            }

            brain.Slot = target;
            cardCover = target.Top();
            target.Add(card);

            var view = card.View as EntityCardView;
            if (view != null)
            {
                view.SetCanvasSortingOrder(target.SlotView.SortingOrder);
                view.MoveTo(target, () =>
                {
                    brain.Position = view.transform.position;
                    HandleMoved();
                }, instant, target.Count - 1);
            }
            else
            {
                view.SetCanvasSortingOrder(0);
                brain.Position = target.Position;
                HandleMoved();
            }

            return;

            void HandleMoved()
            {
                if (target.Inventory != null)
                {
                    var component = card.GetOrAddComponent<CardInventoryComponent>();
                    if (component.Inventory)
                        component.Inventory.Value.Remove(card);
                    target.Inventory.Add(card, false);
                }

                if (cardEvents && target.IsTableSlot)
                {
                    cardUncover?.GetComponent<CardBrain.Component>()?.OnUnCovered(card);
                    cardCover?.GetComponent<CardBrain.Component>()?.OnCovered(card);
                    brain.OnPlaced();
                }

                onMoveComplete?.Invoke();
            }
        }

        public static async Task DealCards(
            this SlotEntity source,
            List<SlotEntity> slots,
            Context context,
            bool waitEachCard = false)
        {
            await Task.Delay(Mathf.FloorToInt(1000 * CardsViewConfig.Instance.dealDelay));

            var dealContext = new Context(context, new DealingSlots.Data { Slots = slots });
            var cardDelay = Mathf.FloorToInt(1000 * CardsViewConfig.Instance.dealDuration / slots.Count);
            while (0 < source.Count && 0 < slots.Count)
            {
                var card = source.Peek();
                var brainModel = card.Model.GetComponent<CardBrain>();
                var slot = brainModel?.SpawnSlot?.Evaluate(dealContext) ?? slots.Peek();
                if (slot == null) continue;
                slots.Remove(slot);

                card.MoveTo(slot, () => card.GetComponent<CardBrain.Component>()?.OnPlacedFirstTime());
                if (waitEachCard)
                    await Task.Delay(Mathf.FloorToInt(CardsViewConfig.Instance.jumpDuration * 1000));

                await Task.Delay(cardDelay);
            }
        }

        public static void RunOnPlaceFirstTime(IEnumerable<CompositeEntity> cards)
        {
            foreach (var card in cards)
            {
                var brain = card.GetComponent<CardBrain.Component>();
                if (brain == null) continue;
                brain.OnPlacedFirstTime();
            }
        }
    }
}