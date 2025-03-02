using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TableSlots : Evaluator<HashSet<CardSlot>>
    {
        public enum EVariant
        {
            Around,
            FromSide
        }

        public EVariant Variant;

        [ShowIf(nameof(Variant), EVariant.Around)]
        public Evaluator<Vector2Int> Position = new TableCardPosition();

        public enum EFigure
        {
            Square,
            Rombus,
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


        public override HashSet<CardSlot> Evaluate()
        {
            return Variant switch
            {
                EVariant.Around => GetSlotsAround(),
                EVariant.FromSide => GetSlotsFromSide(),
                _ => null
            };
        }

        private HashSet<CardSlot> GetSlotsAround()
        {
            return null;
        }

        private HashSet<CardSlot> GetSlotsFromSide()
        {
            // CardTable.Instance.size

            return null;
        }
    }
}