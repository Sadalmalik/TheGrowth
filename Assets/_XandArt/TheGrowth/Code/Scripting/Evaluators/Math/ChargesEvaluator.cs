using XandArt.Architecture;

namespace XandArt.TheGrowth.Cards
{
    /// <summary>
    /// Возвращает колличество зарядов
    /// </summary>
    public class ChargesEvaluator : Evaluator<float>
    {
        public Evaluator<CompositeEntity> Card = new ActiveCard();

        public override float Evaluate(Context context)
        {
            var card = Card.Evaluate(context);
            var chargesComponent = card.GetComponent<Charges.Component>();

            return chargesComponent?.Charges ?? 0;
        }
    }
}