using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Выполняет список команд для каждой карты в коллекции
    /// Карта в момент исполнения доступна через IteratingCard
    /// </summary>
    public class ForEachCard : Command
    {
        public Evaluator<HashSet<EntityCard>> Cards;
        public List<Command> Commands;

        public override void Execute(Context context)
        {
            var cards = Cards.Evaluate(context);
            if (cards.Count == 0) return;
            var data = new IteratingCard.Data { Card = null };
            var subContext = new Context(context, data);
            foreach (var card in cards)
            {
                data.Card = card;
                Commands.ExecuteAll(subContext);
            }
        }
    }
}