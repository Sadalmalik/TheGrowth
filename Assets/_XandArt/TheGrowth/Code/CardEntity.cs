namespace XandArt.TheGrowth
{
    public class CardEntity : CompositeEntity
    {
        public Ref<CardEntity> Self;
        public AssetRef<CardModel> Model;

        public CardEntity(CardModel model)
        {
            Model = model;
            Self = this;
        }
    }
}