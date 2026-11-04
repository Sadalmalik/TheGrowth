using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Model.Cards
{
    /// <summary>Карта, чей хук выполняется или чьё свойство вычисляется: <c>[active card]</c>.</summary>
    public sealed class ActiveCard : IContextData
    {
        public ActiveCard(Entity card)
        {
            Card = card;
        }

        public Entity Card { get; }
    }

    /// <summary>В <c>OnCovered</c> — карта, которая легла сверху; в <c>OnUnCovered</c> — карта, которая ушла: <c>[covering card]</c>.</summary>
    public sealed class CoveringCard : IContextData
    {
        public CoveringCard(Entity card)
        {
            Card = card;
        }

        public Entity Card { get; }
    }

    /// <summary>Текущая карта цикла <c>for each card in [...]</c>: <c>[iterating card]</c>.</summary>
    public sealed class IteratingCard : IContextData
    {
        public IteratingCard(Entity card)
        {
            Card = card;
        }

        public Entity Card { get; }
    }

    /// <summary>Глубина вложенных реакций карт в запуске (первый хук — 1): защита стека от зацикливания реакций.</summary>
    public sealed class ReactionDepth : IContextData
    {
        public ReactionDepth(int depth)
        {
            Depth = depth;
        }

        public int Depth { get; }
    }
}
