using System.Collections.Generic;
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

        public int Steps;


        public override HashSet<EntitySlot> Evaluate(Context context)
        {
            return Variant switch
            {
                EVariant.AllSlots => GetAllSlots(),
                EVariant.Around => GetSlotsAround(),
                EVariant.FromSide => GetSlotsFromSide(),
                _ => null
            };
        }

        private HashSet<EntitySlot> GetAllSlots()
        {
            return new HashSet<EntitySlot>(CardTable.Instance.slots);
        }
        private HashSet<EntitySlot> GetSlotsAround()
        {
            return null;
        }

        private HashSet<EntitySlot> GetSlotsFromSide()
        {
            // CardTable.Instance.size

            return null;
        }
    }
}