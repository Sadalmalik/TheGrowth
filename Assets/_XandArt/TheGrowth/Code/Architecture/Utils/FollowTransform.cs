using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.Architecture.Utils
{
    [ExecuteAlways]
    public class FollowTransform : SerializedMonoBehaviour
    {
        [SerializeField]
        private FollowMode _mode;

        [SerializeField]
        private Transform _target;
        
        private void Update()
        {
            if (0 != (_mode & FollowMode.Position))
                transform.position = _target.position;
            if (0 != (_mode & FollowMode.Rotation))
                transform.rotation = _target.rotation;
            if (0 != (_mode & FollowMode.Scale))
                transform.localScale = _target.localScale;
        }

        [Flags]
        public enum FollowMode
        {
            Position = 0b001,
            Rotation = 0b010,
            Scale    = 0b100,
        }
    }
}