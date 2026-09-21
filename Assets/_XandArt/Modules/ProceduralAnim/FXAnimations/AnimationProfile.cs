using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.ProcAnim
{
	[CreateAssetMenu(
		fileName = "AnimationProfile",
		menuName = "[GeekyHouse]/VFX/Animation Profile",
		order = 1)]
	public partial class AnimationProfile : SerializedScriptableObject
	{
#region Main

		public AnimationSlots Key => slot;

		[BoxGroup("Main")]
		public AnimationSlots slot;

		[BoxGroup("Main")]
		[Range(0.0f, 10.0f)]
		public float time = 1.0f;

		[BoxGroup("Main")]
		public float speedFactorFrom = 1.0f;

		[BoxGroup("Main")]
		public float speedFactorTo = 1.0f;

#endregion


#region Scale

		[BoxGroup("Scale")]
		public ScaleCurveType scaleType = ScaleCurveType.None;
		private bool ScaleTypeOver(ScaleCurveType type) => (int) type <= (int) scaleType;

		[BoxGroup("Scale")]
		[ShowIf("@ScaleTypeOver(ScaleCurveType.Uniform)")]
		[LabelText("@scaleType==ScaleCurveType.Uniform?\"Scale Curve\":\"Scale Curve X\"")]
		public AnimationCurve scaleCurveX = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

		[BoxGroup("Scale")]
		[ShowIf("@ScaleTypeOver(ScaleCurveType.Separate)")]
		public AnimationCurve scaleCurveY = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

		[BoxGroup("Scale")]
		[ShowIf("@ScaleTypeOver(ScaleCurveType.Separate)")]
		public AnimationCurve scaleCurveZ = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

#endregion


#region Position

		[BoxGroup("Position")]
		public PositionCurveType positionType = PositionCurveType.None;
		private bool PositionTypeOver(PositionCurveType type) => (int) type <= (int) positionType;


		[BoxGroup("Position")]
		[ShowIf("positionType", PositionCurveType.Static)]
		public Vector3 positionShift = Vector3.zero;

		[BoxGroup("Position")]
		[ShowIf("positionType", PositionCurveType.Quadratic)]
		public bool pivotRelativeToStart = false;

		[BoxGroup("Position")]
		[ShowIf("@PositionTypeOver(PositionCurveType.Quadratic)")]
		[LabelText("@positionType==PositionCurveType.Quadratic?\"Bezier Pivot\":\"Bezier Pivot 1\"")]
		public Vector3 bezierPivot1 = Vector3.zero;

		[BoxGroup("Position")]
		[ShowIf("@PositionTypeOver(PositionCurveType.Cubic)")]
		public Vector3 bezierPivot2 = Vector3.zero;

		[BoxGroup("Position")]
		[ShowIf("@PositionTypeOver(PositionCurveType.Quadratic)")]
		public PositionCurveMirrorType positionMirrorType = PositionCurveMirrorType.None;

		[BoxGroup("Position")]
		[ShowIf("@PositionTypeOver(PositionCurveType.Quadratic)")]
		public PositionCurveScaleType positionScaleType = PositionCurveScaleType.None;

		[BoxGroup("Position")]
		[ShowIf("@PositionTypeOver(PositionCurveType.Quadratic)")]
		public PositionCurveAspectType positionAspectType = PositionCurveAspectType.None;

		[BoxGroup("Position")]
		[ShowIf("@PositionTypeOver(PositionCurveType.Static)")]
		public AnimationCurve positionCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

#endregion


#region Color

		[BoxGroup("Color")]
		public ColorCurveType colorType = ColorCurveType.None;

		[BoxGroup("Color")]
		[ShowIf("colorType", ColorCurveType.Colors)]
		public Color startColor = Color.white;

		[BoxGroup("Color")]
		[ShowIf("colorType", ColorCurveType.Colors)]
		public Color endColor = Color.white;

		[BoxGroup("Color")]
		[ShowIf("colorType", ColorCurveType.Gradient)]
		public Gradient gradient = new Gradient();

		[BoxGroup("Color")]
		[HideIf("colorType", ColorCurveType.None)]
		public AnimationCurve colorCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

#endregion


#region Rotation

		[BoxGroup("Rotation")]
		public RotationCurveType rotationType = RotationCurveType.None;

		[BoxGroup("Rotation")]
		[ShowIf("rotationType", RotationCurveType.RotateZ)]
		public float startAngle = 0.0f;

		[BoxGroup("Rotation")]
		[ShowIf("rotationType", RotationCurveType.RotateZ)]
		public float endAngle = 0.0f;

		[BoxGroup("Rotation")]
		[ShowIf("rotationType", RotationCurveType.RotateWorld)]
		public Vector3 startAngleFull = Vector3.zero;

		[BoxGroup("Rotation")]
		[ShowIf("rotationType", RotationCurveType.RotateWorld)]
		public Vector3 endAngleFull = Vector3.zero;

		[BoxGroup("Rotation")]
		[HideIf("rotationType", RotationCurveType.None)]
		public AnimationCurve angleCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

#endregion
    }
}