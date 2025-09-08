using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntitySlotView : SerializedMonoBehaviour, IEntityView
    {
        public GameObject Marker;

        public GameObject Object => gameObject;

        public Entity Data { get; set; }

        public void ShowMarker(bool active)
        {
            Marker?.SetActive(active);
        }
    }
}