using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardVisual : IEntityModelComponent
    {
        public string Title;
        public Sprite Face;
        public Sprite Cover;
        public EntityCard CustomPrefab;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            card.AddComponent<Component>();
            // if (Face != null)
            //     card.view.face.sprite = Face;
            // if (Cover != null)
            //     card.view.cover.sprite = Cover;
            //
            // card.view.title.SetText(Title);
        }
        
        public class Component : EntityComponent
        {
            public override void OnPostLoad()
            {
                //Owner.View = Object.Instantiate();
            }
        }
    }

}