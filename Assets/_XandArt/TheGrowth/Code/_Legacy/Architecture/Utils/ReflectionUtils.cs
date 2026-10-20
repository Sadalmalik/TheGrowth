using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;

namespace XandArt.Architecture.Utils
{
	public static class ReflectionUtils
	{
		public static List<Type> GetAllSubtypes<T>()
		{
			var targetType = typeof(T);
			var types      = new List<Type>();

			foreach (var domain_assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var assembly_types =
					domain_assembly
					   .GetTypes()
					   .Where(
							type =>
								!type.IsAbstract && type.IsSubclassOf(targetType) || targetType.IsAssignableFrom(type));
				types.AddRange(assembly_types);
			}

			return types;
		}

		public static List<FieldInfo> GetAllFields(this Type type, BindingFlags flags)
		{
			if (type == null ||
				type == typeof(Object) ||
				type == typeof(System.Object))
				return new List<FieldInfo>();

			var list = type.BaseType.GetAllFields(flags);
			
			list.AddRange(type.GetFields(flags | BindingFlags.DeclaredOnly));
			
			return list;
		}
	}
}