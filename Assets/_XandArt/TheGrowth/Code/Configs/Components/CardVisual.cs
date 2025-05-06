using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntityModelVisual : IEntityModelComponent
    {
        public string Title;
        public Sprite Face;
        public Sprite Cover;
        public EntityCard CustomPrefab;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            // if (Face != null)
            //     card.view.face.sprite = Face;
            // if (Cover != null)
            //     card.view.cover.sprite = Cover;
            //
            // card.view.title.SetText(Title);
        }
    }
}