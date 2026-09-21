namespace Sadalmalik.ProcAnim
{
	public enum ScaleCurveType
	{
		None = 0, Uniform = 1, Separate = 2
	}

	public enum PositionCurveType
	{
		None      = 0,
		Static    = 1,
		Linear    = 2,
		Quadratic = 3,
		Cubic     = 4
	}

	public enum PositionCurveMirrorType
	{
		None = 0,
		MirrorX = 1,
		MirrorY = 2,
		MirrorBoth = 3
	}

	public enum PositionCurveScaleType
	{
		None = 0,
		AxisX = 1,
		AxisY = 2,
		AxisBoth = 3,
		Distance = 4
	}
	
	public enum PositionCurveAspectType
	{
		None = 0,
		HorizontalAspect = 1,
		VerticalAspect = 2
	}

	public enum ColorCurveType
	{
		None = 0,
		Colors = 1,
		Gradient = 2
	}

	public enum RotationCurveType
	{
		None = 0,
		RotateX = 1,
		RotateY = 2,
		RotateZ = 3,
		RotateWorld = 4
	}
}