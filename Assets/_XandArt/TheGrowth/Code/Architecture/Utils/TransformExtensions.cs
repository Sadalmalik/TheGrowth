using System;
using System.Collections.Generic;
using UnityEngine;

namespace XandArt.Architecture.Utils
{
	public enum Axis
	{
		xAxis, yAxis, zAxis
	}

	public static class TransformExtensions
	{
		public static void DestroyChilds(this Transform t)
		{
			var childs = new List<Transform>();
			foreach (Transform child in t)
				childs.Add(child);
			foreach (var child in childs)
				HierarchyUtils.SafeDestroy(child.gameObject);
		}

		public static float GetAxis(this Transform t, Axis axis)
		{
			switch (axis)
			{
				case Axis.xAxis:
					return t.localRotation.eulerAngles.x;
				case Axis.yAxis:
					return t.localRotation.eulerAngles.y;
				case Axis.zAxis:
					return t.localRotation.eulerAngles.z;
			}
			throw new Exception($"Unknown axis: {axis}");
		}

		public static void SetAxis(this Transform t, Axis axis, float value)
		{
			var rotation = t.localRotation.eulerAngles;
			switch (axis)
			{
				case Axis.xAxis:
					rotation.x = value;
					break;
				case Axis.yAxis:
					rotation.y = value;
					break;
				case Axis.zAxis:
					rotation.z = value;
					break;
			}
			t.localRotation = Quaternion.Euler(rotation);
		}
	}
}