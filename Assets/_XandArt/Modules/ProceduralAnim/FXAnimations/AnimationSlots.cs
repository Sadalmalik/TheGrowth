using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.ProcAnim
{
	public enum AnimationSlots
	{
		None = 0,
		Default = 1,
		
		MergeSpawnCurve = 1001,
		MergeDestroyCurve = 1002,
		
		MergeBounceTap = 1003,
		MergeBounceSelect = 1004,
		MergeBounceTip = 1005,
		
		MergeDropCurve = 1006,
		MergeSpawnFlyCurve = 1007,

		FXCollectBonusDrop = 2001,
		FXCollectBonusFly = 2002,
	}
}