using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardVisual : IEntityModelComponent
    {
        public string Title;
        [TextArea(3, 15)]
        public string Description;
        public Sprite Decor;
        public Sprite Portrait;
        
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
        
        public class Component : EntityComponent, IViewComponent
        {
            public override void OnPostLoad()
            {
                //Owner.View = Object.Instantiate();
            }

            public GameObject GetPrefab() => null;

            GameObject IViewComponent.View
            {
                get => throw new System.NotImplementedException();
                set => throw new System.NotImplementedException();
            }
        }
    }
}