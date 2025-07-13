using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public enum LocationType
    {
        UI,
        Space
    }

    [CreateAssetMenu(
        fileName = "Location",
        menuName = "[Game]/Location",
        order = 0)]
    public class LocationModel : AbstractEntityModel
    {
        public string Title;
        public LocationType Type;
        public string Scene;

        public List<EntityModel> Cards;

        public override Entity Create()
        {
            var entity = new LocationEntity { _model = this };
            entity.OnInit();
            return entity;
        }
    }
}