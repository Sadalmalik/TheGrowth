using Sirenix.OdinInspector;
using System.Collections.Generic;
using GeekyHouse.Architecture.Logger;

namespace GeekyHouse.Architecture.CSV
{
	public class TEST_OCM : SerializedMonoBehaviour
	{
		public class MyItem
		{
			public string name;
			public bool   boolValue;
			public int    intValue;
			public float  floatValue;
		}

		public List<MyItem> items1;
		public List<MyItem> items2;

		[Sirenix.OdinInspector.Button]
		public void DoTests()
		{
			var csv = ObjectCSVConverter.ToCSV(items1);
			
			Log.Temp($"CSV:\n\n{csv}");
			
			items2 = ObjectCSVConverter.FromCSV<MyItem>(csv);
		}
	}
}