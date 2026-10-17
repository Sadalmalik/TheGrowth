using UnityEngine;

namespace TheGrowth.Architecture.ProceduralAnim
{
    [CreateAssetMenu(
        fileName = "AnimationProfile",
        menuName = "[Game]/Animations/Animation Profile",
        order = 1)]
    public partial class AnimationProfile : ScriptableObject
    {
#region Main

        [Header("Main")]
        [Range(0.0f, 10.0f)]
        public float time = 1.0f;

        public float speedFactorFrom = 1.0f;

        public float speedFactorTo = 1.0f;

#endregion


#region Scale

        [Header("Scale")]
        public ScaleCurveType scaleType = ScaleCurveType.None;

        public AnimationCurve scaleCurveX = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

        public AnimationCurve scaleCurveY = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

        public AnimationCurve scaleCurveZ = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

#endregion


#region Position

        [Header("Position")]
        public PositionCurveType positionType = PositionCurveType.None;

        public Vector3 positionShift = Vector3.zero;

        public bool pivotRelativeToStart = false;

        public Vector3 bezierPivot1 = Vector3.zero;

        public Vector3 bezierPivot2 = Vector3.zero;

        public PositionCurveMirrorType positionMirrorType = PositionCurveMirrorType.None;

        public PositionCurveScaleType positionScaleType = PositionCurveScaleType.None;

        public PositionCurveAspectType positionAspectType = PositionCurveAspectType.None;

        public AnimationCurve positionCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

#endregion


#region Color

        [Header("Color")]
        public ColorCurveType colorType = ColorCurveType.None;

        public Color startColor = Color.white;

        public Color endColor = Color.white;

        public Gradient gradient = new Gradient();

        public AnimationCurve colorCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

#endregion


#region Rotation

        [Header("Rotation")]
        public RotationCurveType rotationType = RotationCurveType.None;

        public float startAngle = 0.0f;

        public float endAngle = 0.0f;

        public Vector3 startAngleFull = Vector3.zero;

        public Vector3 endAngleFull = Vector3.zero;

        public AnimationCurve angleCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

#endregion
    }
}
