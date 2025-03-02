using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TableSlots : Evaluator<HashSet<EntitySlot>>
    {
        public enum EVariant
        {
            AllSlots,
            Around,
            FromSide
        }

        public EVariant Variant = EVariant.AllSlots;

        [ShowIf(nameof(Variant), EVariant.Around)]
        public Evaluator<Vector2Int> Position = new TableCardPosition();

        public enum EFigure
        {
            Square,
            Rhombus,
            Circle,
            Cross,
            Diagonals
        }

        [ShowIf(nameof(Variant), EVariant.Around)]
        public EFigure Figure;

        public enum ESide
        {
            Left,
            Right,
            Top,
            Bottom,
        }

        [ShowIf(nameof(Variant), EVariant.FromSide)]
        public ESide Side;

        [HideIf(nameof(Variant), EVariant.AllSlots)]
        public int Steps;


        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            return Variant switch
            {
                EVariant.AllSlots => GetAllSlots(context),
                EVariant.Around => GetSlotsAround(context),
                EVariant.FromSide => GetSlotsFromSide(context),
                _ => null
            };
        }

        private HashSet<EntitySlot> GetAllSlots(Context context)
        {
            return new HashSet<EntitySlot>(CardTable.Instance.slots);
        }
        private HashSet<EntitySlot> GetSlotsAround(Context context)
        {
            var all = CardTable.Instance.slots;
            var center = CardTable.Instance[Position.Evaluate(context)].transform.position;
            return Figure switch
            {
                EFigure.Circle => new HashSet<EntitySlot>(all.Where(
                    slot => Vector3.Distance(slot.transform.position, center) < Steps)),
                _ => null
            };
        }

        private HashSet<EntitySlot> GetSlotsFromSide(Context context)
        {
            // CardTable.Instance.size

            return null;
        }
    }
}