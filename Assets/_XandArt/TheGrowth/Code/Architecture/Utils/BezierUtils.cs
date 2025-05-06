using UnityEngine;

namespace XandArt.Architecture.Utils
{
	public static class BezierUtils
	{
		public static Vector3 EvaluateLinear(Vector3 start, Vector3 end, float t)
		{
			// a -> b
			// return Vector3.Lerp(a, b, t);
			
			//return (1 - t) * a + t * b;
			return start + t * (end - start);
		}
        
        public static Vector3 EvaluateQuadratic(Vector3 start, Vector3 end, Vector3 pivot, float t)
        {
			// a -> b -> c  // b is pivot
			// var d = Vector3.Lerp(a, b, t);
			// var e = Vector3.Lerp(b, c, t);
			//
			// return Vector3.Lerp(d, e, t);
			
            float u = 1 - t;
            
            return u * u * start + 2 * u * t * pivot + t * t * end;
        }
        
        
        public static Vector3 EvaluateCubic(Vector3 start, Vector3 end, Vector3 pivot1, Vector3 pivot2, float t)
        {
			// a -> b -> c -> d  // b and c is pivots
			// var e = Vector3.Lerp(a, b, t);
			// var f = Vector3.Lerp(b, c, t);
			// var g = Vector3.Lerp(c, d, t);
			//
			// var h = Vector3.Lerp(e, f, t);
			// var i = Vector3.Lerp(f, g, t);
			//
			// return Vector3.Lerp(h, i, t);
			
            float u = 1 - t;
            float uu = u * u;
            float tt = t * t;
            
            return uu * (u * start + 3 * t * pivot1) + tt * (3 * u * pivot2 + t * end);
        }
	}
}