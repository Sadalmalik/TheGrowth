using UnityEngine;

namespace Sadalmalik.Utils
{
	public static class MathUtils
	{
		public static float Ranged(float value, float min, float max)
		{
			return (value - min) / (max - min);
		}
		
		public static float RangedClamped(float value, float min, float max)
		{
			return Mathf.Clamp01((value - min) / (max - min));
		}
	}
}