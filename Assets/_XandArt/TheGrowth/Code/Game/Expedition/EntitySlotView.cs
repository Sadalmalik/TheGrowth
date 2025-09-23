using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntitySlotView : SerializedMonoBehaviour, IEntityView
    {
        [SerializeField]
        public GameObject Marker;

        [SerializeField]
        private bool UseCustomOffset;

        [SerializeField]
        private Vector3 CustomOffset;

        public GameObject Object => gameObject;

        public Entity Data { get; set; }

        public Vector3? GetCustomOffset()
        {
            if (UseCustomOffset)
                return transform.localToWorldMatrix * CustomOffset;
            return null;
        }

        public void ShowMarker(bool active)
        {
            Marker?.SetActive(active);
        }

        private void OnDrawGizmos()
        {
            if (!UseCustomOffset)
                return;

            Debug.DrawRay(
                transform.position,
                transform.localToWorldMatrix * CustomOffset);
        }
    }
}