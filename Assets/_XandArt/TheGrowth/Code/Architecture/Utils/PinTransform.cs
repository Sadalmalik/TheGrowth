using System;
using UnityEngine;

namespace XandArt.Architecture.Utils
{
    [ExecuteInEditMode]
    public class PinTransform : MonoBehaviour
    {
        [SerializeField]
        private Mode _mode;

        private Vector3 _position;

        private void OnDrawGizmosSelected()
        {
            switch (_mode)
            {
                case Mode.None:
                    _position = transform.position;
                    return;
                case Mode.Zero:
                    transform.position = Vector3.zero;
                    _position = Vector3.zero;
                    return;
                case Mode.Position:
                    transform.position = _position;
                    return;
            }
        }

        public enum Mode
        {
            None,
            Zero,
            Position
        }
    }
}