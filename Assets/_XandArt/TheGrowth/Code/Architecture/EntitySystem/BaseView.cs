using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.Architecture
{
    public class BaseView : SerializedMonoBehaviour, IEntityView
    {
        public GameObject Object => gameObject;
    }
}