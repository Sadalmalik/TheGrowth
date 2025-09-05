using UnityEngine;

namespace XandArt.TheGrowth
{
    public class Obsolete_Draggable : MonoBehaviour
    {
        private Vector3 _startMousePosition;

        private Vector3 GetScreenPosition()
        {
            return Camera.main.WorldToScreenPoint(transform.position);
        }

        private void OnMouseDown()
        {
            _startMousePosition = Input.mousePosition - GetScreenPosition();
        }

        private void OnMouseDrag()
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition - _startMousePosition);
        }
    }
}