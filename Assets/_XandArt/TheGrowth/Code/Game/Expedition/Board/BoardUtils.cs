using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public static class BoardUtils
    {
        public static async Task MoveTo(
            this CompositeEntity card,
            SlotEntity target,
            Action OnMoveComplete = null,
            bool instant = false,
            bool cardEvents = true)
        {
            var brain = card.GetComponent<CardBrain.Component>();

            var cardUncover = (CompositeEntity) null;
            var cardCover = (CompositeEntity) null;

            if (brain.Slot != null)
            {
                brain.Slot.Remove(card);
                cardUncover = brain.Slot.Top();
            }
            brain.Slot = target;
            cardCover = target.Top();
            target.Add(card);
            
            var tcs = new TaskCompletionSource<bool>();
            var view = card.View as EntityCardView;
            if (view != null)
            {
                view.MoveTo(target, () =>
                {
                    brain.Position = view.transform.position;
                    HandleMoved();
                }, instant);
            }
            else
            {
                brain.Position = target.Position;
                HandleMoved();
            }

            await tcs.Task;
            return;

            void HandleMoved()
            {
                if (cardEvents && target.IsTableSlot)
                {
                    cardUncover?.GetComponent<CardBrain.Component>()?.OnUnCovered(card);
                    cardCover?.GetComponent<CardBrain.Component>()?.OnCovered(card);
                    brain.OnPlaced();
                }
                OnMoveComplete?.Invoke();
                tcs.SetResult(true);
            }
        }

        public static async Task DealCards(
            this SlotEntity source,
            List<SlotEntity> slots,
            Context context,
            bool waitEachCard = false)
        {
            await Task.Delay(Mathf.FloorToInt(1000 * CardsViewConfig.Instance.dealDelay));
            
            var cardDelay = Mathf.FloorToInt(1000 * CardsViewConfig.Instance.dealDuration / slots.Count);
            while (0 < source.Count && 0 < slots.Count)
            {
                var card = source.Peek();
                var brainModel = card.Model.GetComponent<CardBrain>();
                var slot = brainModel?.SpawnSlot?.Evaluate(context) ?? slots.Peek();
                if (slot == null) continue;
                slots.Remove(slot);

                var task = card.MoveTo(slot, () => card.GetComponent<CardBrain.Component>()?.OnPlacedFirstTime());
                if (waitEachCard)
                    await task;

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