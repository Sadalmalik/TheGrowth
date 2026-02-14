using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда переключает отображение зарядов указанной карты
    /// </summary>
    public class ToggleChargesText : Command
    {
        public enum EVariant
        {
            Show,
            Hide,
            Switch
        }

        public Evaluator<CompositeEntity> Card;
        public EVariant Variant = EVariant.Switch;
        
        public override void Execute(Context context)
        {
            var card = Card.Evaluate(context);
            var cardCharges = card.GetComponent<Charges.Component>();

            cardCharges.ShowOnCard = Variant switch
            {
                EVariant.Show => true,
                EVariant.Hide => false,
                EVariant.Switch => !cardCharges.ShowOnCard,
                _ => cardCharges.ShowOnCard
            };
        }
    }
}