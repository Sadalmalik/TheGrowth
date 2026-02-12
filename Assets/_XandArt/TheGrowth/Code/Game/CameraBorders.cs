using UnityEngine;

namespace XandArt.TheGrowth
{
    public class CameraBorders : MonoBehaviour
    {
        public Vector2 Dimensions = new Vector2(20, 20);

        public Vector3 ClampPosition(Vector3 position)
        {
            var dim = Dimensions * 0.5f;
            position.x = Mathf.Clamp(
                position.x,
                transform.position.x - dim.x,
                transform.position.x + dim.x
            );
            position.z = Mathf.Clamp(
                position.z,
                transform.position.z - dim.y,
                transform.position.z + dim.y
            );
            return position;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(Dimensions.x, 0.5f, Dimensions.y));
        }
    }
}