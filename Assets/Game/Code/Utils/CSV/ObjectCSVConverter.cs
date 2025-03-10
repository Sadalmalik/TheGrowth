using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GeekyHouse.Architecture.CSV
{
	// Just like ORM is Object-Relation Mapping
	// This one is Object-CSV Mapping
	public static class ObjectCSVConverter
	{
		private static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		private static readonly string[] TrueStatements = {"true", "истина", "да"};

		/// <summary>
		/// T must be plain object without object fields
		/// </summary>
		/// <param name="csv">raw csv</param>
		/// <typeparam name="T">Field object type</typeparam>
		/// <returns></returns>
		public static List<T> FromCSV<T>(string csv) where T : new()
		{
			var items = new List<T>();
			var lines = CSVUtils.FromStrint(csv);

			var type      = typeof(T);
			var fieldsMap = new Dictionary<int, FieldInfo>();

			var header = lines[0];
			for (int i = 0; i < header.Length; i++)
			{
				var name = header[i];
				if (name.StartsWith("#")) continue;
				fieldsMap[i] = type.GetField(name, flags);
			}

			for (int i = 1; i < lines.Count; i++)
			{
				var line = lines[i];
				var item = new T();
				for (int k = 0; k < line.Length; k++)
				{
					if (!fieldsMap.TryGetValue(k, out var field))
						continue;
					if (field==null)
						continue;

					var value = FromString(field.FieldType, line[k]);

					field.SetValue(item, value);
				}
				items.Add(item);
			}

			return items;
		}

		/// <summary>
		/// T must be plain object without object fields
		/// </summary>
		/// <param name="objects">List of objects of type T</param>
		/// <typeparam name="T">Field object type</typeparam>
		/// <returns></returns>
		public static string ToCSV<T>(List<T> items, bool addPrimaryField = true)
		{
			var lines  = new List<string[]>();
			var fields = typeof(T).GetFields(flags);

			var step   = addPrimaryField ? 1 : 0;
			var length = fields.Length + step;

			var header = new string[length];
			if (addPrimaryField)
				header[0] = "#";
			for (int i = 0; i < fields.Length; i++)
			{
				var k = i + step;
				header[k] = fields[i].Name;
			}
			lines.Add(header);

			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var line = new string[length];

				if (addPrimaryField)
					line[0] = i.ToString();

				for (int k = 0; k < fields.Length; k++)
				{
					var m     = k + step;
					var field = fields[k];
					var value = field.GetValue(item);
					line[m] = ToString(field.FieldType, value);
				}

				lines.Add(line);
			}

			return CSVUtils.ToString(lines);
		}

		private static object FromString(Type type, string value)
		{
			if (type == typeof(string))
				return value;

			if (type == typeof(bool))
				return TrueStatements.Contains(value);
			if (type == typeof(byte))
			{
				if (byte.TryParse(value, out byte result))
					return result;
				return (byte)0;
			}
			if (type == typeof(short))
			{
				if (short.TryParse(value, out short result))
					return result;
				return (short)0;
			}
			if (type == typeof(int))
			{
				if (int.TryParse(value, out int result))
					return result;
				return (int)0;
			}
			if (type == typeof(long))
			{
				if (long.TryParse(value, out long result))
					return result;
				return (long)0;
			}
			if (type == typeof(float))
			{
				if (float.TryParse(value, out float result))
					return result;
				return (float)0;
			}
			if (type == typeof(double))
			{
				if (double.TryParse(value, out double result))
					return result;
				return (double)0;
			}

			return value;
		}

		private static string ToString(Type type, object value)
		{
			if (type == typeof(string))
				return value.ToString();

			if (type == typeof(bool))
				return (bool) value ? "true" : "false";

			return value.ToString();
		}
	}
}