using UnityEngine;

namespace XandArt.TheGrowth
{
    public class EntityVisual : IEntityComponentModel
    {
        public string Title;
        public Sprite Face;
        public Sprite Cover;
        public EntityCard CustomPrefab;
        
        public void OnEntityCreated(EntityCard card)
        {
            if (Face != null)
                card.view.face.sprite = Face;
            if (Cover != null)
                card.view.cover.sprite = Cover;
            
            card.view.title.SetText(Title);
        }
    }
}