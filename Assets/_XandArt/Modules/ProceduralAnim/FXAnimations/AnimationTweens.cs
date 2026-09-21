using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Sadalmalik.Utils;

namespace Sadalmalik.ProcAnim
{
	public partial class AnimationProfile
	{
#region Full Tween

		public Tween GetFullTween(
				Transform      transform,
				Vector3?       target     = null,
				Vector3?       startScale = null,
				Vector3?       endScale   = null,
				SpriteRenderer sprite     = null,
				Image          image      = null
			)
		{
			var seq = DOTween.Sequence();

			if (transform != null)
			{
				if (startScale.HasValue
				 && endScale.HasValue)
					Insert(GetScaleCurve(transform, startScale.Value, endScale.Value));
				else if (startScale.HasValue)
					Insert(GetScaleCurve(transform, startScale.Value));
				else
					Insert(GetScaleCurve(transform, Vector3.one));

				Insert(GetPositionTween(transform, target));

				Insert(GetRotationTween(transform));
			}

			if (sprite != null)
				Insert(GetColorTween(sprite));

			if (sprite != null)
				Insert(GetColorTween(image));
				

			void Insert(Tween tween)
			{
				if (tween != null)
					seq.Insert(0, tween);
			}
			
			return seq;
		}

#endregion


#region Scale Tweens

		private Vector3 EvaluateScales(float value)
		{
			return new Vector3(
				scaleCurveX.Evaluate(value),
				scaleCurveY.Evaluate(value),
				scaleCurveZ.Evaluate(value));
		}

		public Tween GetScaleCurve(Transform transform, Vector3 startScale)
		{
			if (transform == null)
				throw new ArgumentNullException(nameof(transform));

			switch (scaleType)
			{
				case ScaleCurveType.Uniform:
					return DOTween.To(
						() => 0.0f,
						value => { transform.localScale = startScale * scaleCurveX.Evaluate(value); },
						1.0f,
						time);

				case ScaleCurveType.Separate:
					return DOTween.To(
						() => 0.0f,
						value => { transform.localScale = Vector3.Scale(startScale, EvaluateScales(value)); },
						1.0f,
						time);
			}

			return null;
		}

		public Tween GetScaleCurve(Transform transform, Vector3 startScale, Vector3 endScale)
		{
			if (transform == null)
				throw new ArgumentNullException(nameof(transform));

			var delta = endScale - startScale;

			switch (scaleType)
			{
				case ScaleCurveType.Uniform:
					return DOTween.To(
						() => 0.0f,
						value => { transform.localScale = startScale + delta * scaleCurveX.Evaluate(value); },
						1.0f,
						time);

				case ScaleCurveType.Separate:
					return DOTween.To(
						() => 0.0f,
						value => { transform.localScale = startScale + Vector3.Scale(delta, EvaluateScales(value)); },
						1.0f,
						time);
			}

			return null;
		}

#endregion


#region Position Tweens

		public Tween GetPositionTween(Transform transform, Vector3? target)
		{
			if (transform == null)
			{
				Debug.LogWarning($"AnimationProfile '{name}': Tween requires transform for tweening! Tween will not be created!");
				return null;
			}

			switch (positionType)
			{
				case PositionCurveType.None:
					return null;
				case PositionCurveType.Static:
				{
					if (target.HasValue)
						Debug.LogWarning($"AnimationProfile '{name}': Static position tween does not requires target transform! It will be ignored.");
					return CreateStaticCurve(transform);
				}
				case PositionCurveType.Linear:
				{
					if (!target.HasValue)
					{
						Debug.LogWarning($"AnimationProfile '{name}': Linear tween requires target! Tween will not be created!");
						return null;
					}
					return CreateLinearCurve(transform, target.Value);
				}
				case PositionCurveType.Quadratic:
				{
					if (!target.HasValue)
					{
						Debug.LogWarning($"AnimationProfile '{name}': Quadratic tween requires target! Tween will not be created!");
						return null;
					}
					return CreateQuadraticCurve(transform, target.Value);
				}
				case PositionCurveType.Cubic:
				{
					if (!target.HasValue)
					{
						Debug.LogWarning($"AnimationProfile '{name}': Cubic tween requires target! Tween will not be created!");
						return null;
					}
					return CreateCubicCurve(transform, target.Value);
				}
			}

			return null;
		}

		private Tween CreateStaticCurve(Transform transform)
		{
			var startPosition = transform.localPosition;
			var endPosition   = startPosition + positionShift;

			return DOTween.To(
				() => 0.0f,
				value => transform.localPosition = Vector3.LerpUnclamped(
					startPosition,
					endPosition,
					positionCurve.Evaluate(value)),
				1.0f,
				time);
		}

		private Tween CreateLinearCurve(Transform transform, Vector3 endPosition)
		{
			var startPosition = transform.position;

			return DOTween.To(
				() => 0.0f,
				value => transform.position = BezierUtils.EvaluateLinear(
					startPosition,
					endPosition,
					positionCurve.Evaluate(value)),
				1.0f,
				time);
		}

		private Tween CreateQuadraticCurve(Transform transform, Vector3 endPosition)
		{
			var startPosition = transform.position;

			var pivot = bezierPivot1;

			var delta = endPosition - startPosition;

			ApplyDistanceModification(delta, ref pivot);

			switch (positionAspectType)
			{
				case PositionCurveAspectType.HorizontalAspect:
					ApplyCurveHorizontalAspect(delta, ref pivot);
					break;
				case PositionCurveAspectType.VerticalAspect:
					ApplyCurveVerticalAspect(delta, ref pivot);
					break;
			}
			
			ApplyMirrorModification(delta, ref pivot);

			pivot += pivotRelativeToStart ? startPosition : endPosition;

			Debug.DrawLine(startPosition, pivot, Color.black, 5);
			Debug.DrawLine(pivot, endPosition, Color.black, 5);

			return DOTween.To(
				() => 0.0f,
				value => transform.position = BezierUtils.EvaluateQuadratic(
					startPosition,
					endPosition,
					pivot,
					positionCurve.Evaluate(value)),
				1.0f,
				time);
		}

		private Tween CreateCubicCurve(Transform transform, Vector3 endPosition)
		{
			var startPosition = transform.position;

			var pivot1 = bezierPivot1;
			var pivot2 = bezierPivot2;

			var delta = endPosition - startPosition;

			ApplyDistanceModification(delta, ref pivot1, ref pivot2);

			switch (positionAspectType)
			{
				case PositionCurveAspectType.HorizontalAspect:
					ApplyCurveHorizontalAspect(delta, ref pivot1, ref pivot2);
					break;
				case PositionCurveAspectType.VerticalAspect:
					ApplyCurveVerticalAspect(delta, ref pivot1, ref pivot2);
					break;
			}
			
			ApplyMirrorModification(delta, ref pivot1, ref pivot2);

			pivot1 += startPosition;
			pivot2 += endPosition;

			Debug.DrawLine(startPosition, pivot1, Color.black, 5);
			Debug.DrawLine(pivot1, pivot2, Color.black, 5);
			Debug.DrawLine(pivot2, endPosition, Color.black, 5);
			
			return DOTween.To(
				() => 0.0f,
				value => transform.position = BezierUtils.EvaluateCubic(
					startPosition,
					endPosition,
					pivot1,
					pivot2,
					positionCurve.Evaluate(value)),
				1.0f,
				time);
		}

		private void ApplyMirrorModification(Vector3 delta, ref Vector3 pivot)
		{
			switch (positionMirrorType)
			{
				case PositionCurveMirrorType.MirrorX:
					if (delta.x < 0) pivot.x = -pivot.x;
					break;
				case PositionCurveMirrorType.MirrorY:
					if (delta.y < 0) pivot.y = -pivot.y;
					break;
				case PositionCurveMirrorType.MirrorBoth:
					if (delta.x < 0) pivot.x = -pivot.x;
					if (delta.y < 0) pivot.y = -pivot.y;
					break;
			}
		}

		private void ApplyMirrorModification(Vector3 delta, ref Vector3 pivot1, ref Vector3 pivot2)
		{
			switch (positionMirrorType)
			{
				case PositionCurveMirrorType.MirrorX:
					if (delta.x < 0)
					{
						pivot1.x = -pivot1.x;
						pivot2.x = -pivot2.x;
					}
					break;
				case PositionCurveMirrorType.MirrorY:
					if (delta.y < 0)
					{
						pivot1.y = -pivot1.y;
						pivot2.y = -pivot2.y;
					}
					break;
				case PositionCurveMirrorType.MirrorBoth:
					if (delta.x < 0)
					{
						pivot1.x = -pivot1.x;
						pivot2.x = -pivot2.x;
					}
					if (delta.y < 0)
					{
						pivot1.y = -pivot1.y;
						pivot2.y = -pivot2.y;
					}
					break;
			}
		}

		private void ApplyDistanceModification(Vector3 delta, ref Vector3 pivot)
		{
			switch (positionScaleType)
			{
				case PositionCurveScaleType.AxisX:
					pivot.x *= Mathf.Abs(delta.x);
					break;
				case PositionCurveScaleType.AxisY:
					pivot.y *= Mathf.Abs(delta.y);
					break;
				case PositionCurveScaleType.AxisBoth:
					pivot.x *= Mathf.Abs(delta.x);
					pivot.y *= Mathf.Abs(delta.y);
					break;
				case PositionCurveScaleType.Distance:
					pivot *= delta.magnitude;
					break;
			}
		}

		private void ApplyDistanceModification(Vector3 delta, ref Vector3 pivot1, ref Vector3 pivot2)
		{
			switch (positionScaleType)
			{
				case PositionCurveScaleType.AxisX:
				{
					var xScale = Mathf.Abs(delta.x);
					pivot1.x *= xScale;
					pivot2.x *= xScale;
					break;
				}
				case PositionCurveScaleType.AxisY:
				{
					var yScale = Mathf.Abs(delta.x);
					pivot1.y *= yScale;
					pivot2.y *= yScale;
					break;
				}
				case PositionCurveScaleType.AxisBoth:
				{
					var xScale = Mathf.Abs(delta.x);
					pivot1.x *= xScale;
					pivot2.x *= xScale;
					var yScale = Mathf.Abs(delta.x);
					pivot1.y *= yScale;
					pivot2.y *= yScale;
					break;
				}
				case PositionCurveScaleType.Distance:
				{
					var dist = delta.magnitude;
					pivot1 *= dist;
					pivot2 *= dist;
					break;
				}
			}
		}

		private void ApplyCurveHorizontalAspect(Vector3 delta, ref Vector3 pivot)
		{
			float dx  = delta.x;
			float dy  = delta.y;
			float adx = Mathf.Abs(dx);

			if (adx > 0)
			{
				float k = dy / adx;
				pivot.y += k * pivot.x;
			}
		}

		private void ApplyCurveHorizontalAspect(Vector3 delta, ref Vector3 pivot1, ref Vector3 pivot2)
		{
			float dx  = delta.x;
			float dy  = delta.y;
			float adx = Mathf.Abs(dx);

			if (adx > 0)
			{
				float k = dy / adx;
				pivot1.y += k * pivot1.x;
				pivot2.y += k * pivot2.x;
			}
		}

		private void ApplyCurveVerticalAspect(Vector3 delta, ref Vector3 pivot)
		{
			float dx  = delta.x;
			float dy  = delta.y;
			float ady = Mathf.Abs(dy);
			if (ady > 0)
			{
				float k = dx / ady;
				pivot.x += k * pivot.y;
			}
		}

		private void ApplyCurveVerticalAspect(Vector3 delta, ref Vector3 pivot1, ref Vector3 pivot2)
		{
			float dx  = delta.x;
			float dy  = delta.y;
			float ady = Mathf.Abs(dy);
			if (ady > 0)
			{
				float k = dx / ady;
				pivot1.x += k * pivot1.y;
				pivot2.x += k * pivot2.y;
			}
		}

#endregion


#region Color Tween

		public Tween GetColorTween(SpriteRenderer sprite)
		{
			switch (colorType)
			{
				case ColorCurveType.None:
					return null;
				case ColorCurveType.Colors:
					return DOTween.To(
						() => 0.0f,
						value =>
						{
							sprite.color = Color.LerpUnclamped(startColor, endColor, colorCurve.Evaluate(value));
						},
						1.0f,
						time);
				case ColorCurveType.Gradient:
					return DOTween.To(
						() => 0.0f,
						value => { sprite.color = gradient.Evaluate(colorCurve.Evaluate(value)); },
						1.0f,
						time);
			}

			return null;
		}

		public Tween GetColorTween(Image image)
		{
			switch (colorType)
			{
				case ColorCurveType.None:
					return null;
				case ColorCurveType.Colors:
					return DOTween.To(
						() => 0.0f,
						value =>
						{
							image.color = Color.LerpUnclamped(startColor, endColor, colorCurve.Evaluate(value));
						},
						1.0f,
						time);
				case ColorCurveType.Gradient:
					return DOTween.To(
						() => 0.0f,
						value => { image.color = gradient.Evaluate(colorCurve.Evaluate(value)); },
						1.0f,
						time);
			}

			return null;
		}

#endregion


#region Rotation Tween

		public Tween GetRotationTween(Transform transform)
		{
			switch (rotationType)
			{
				case RotationCurveType.None:
					return null;
				case RotationCurveType.RotateX:
					return DOTween.To(
						() => 0.0f,
						value =>
						{
							var angle = transform.localRotation.eulerAngles;
							angle.x = Mathf.LerpUnclamped(
								startAngle,
								endAngle,
								angleCurve.Evaluate(value));
							transform.localRotation = Quaternion.Euler(angle);
						},
						1.0f,
						time);
				case RotationCurveType.RotateY:
					return DOTween.To(
						() => 0.0f,
						value =>
						{
							var angle = transform.localRotation.eulerAngles;
							angle.y = Mathf.LerpUnclamped(
								startAngle,
								endAngle,
								angleCurve.Evaluate(value));
							transform.localRotation = Quaternion.Euler(angle);
						},
						1.0f,
						time);
				case RotationCurveType.RotateZ:
					return DOTween.To(
						() => 0.0f,
						value =>
						{
							var angle = transform.localRotation.eulerAngles;
							angle.z = Mathf.LerpUnclamped(
								startAngle,
								endAngle,
								angleCurve.Evaluate(value));
							transform.localRotation = Quaternion.Euler(angle);
						},
						1.0f,
						time);
				case RotationCurveType.RotateWorld:
					return DOTween.To(
						() => 0.0f,
						value =>
						{
							var angle = Vector3.LerpUnclamped(startAngleFull, endAngleFull, angleCurve.Evaluate(value));
							transform.localRotation = Quaternion.Euler(angle);
						},
						1.0f,
						time);
			}

			return null;
		}

#endregion
	}
}