using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public static class RefactorTools
    {
        [MenuItem("[TheGrowth]/Refactoring/Get Types")]
        private static void DumpTypes()
        {
            var sb = new StringBuilder();
            var assembly = Assembly.GetExecutingAssembly();
            var allTypes = assembly.GetTypes();

            foreach (Type type in allTypes)
            {
                if (type.Namespace == null) continue;
                if (!type.Namespace.Contains("XandArt.TheGrowth")) continue;

                var isCommand = typeof(XandArt.TheGrowth.Command).IsAssignableFrom(type);
                var isCondition = typeof(XandArt.TheGrowth.Condition).IsAssignableFrom(type);
                var isEvaluator = typeof(XandArt.TheGrowth.Evaluator<>).IsAssignableFrom(type);
                if (!isCommand && !isCondition && !isEvaluator) continue;
                sb.AppendLine($"[assembly: BindTypeNameToType(\"XandArt.TheGrowth.{type.Name}\", typeof({type.Name}))]");
            }
            
            Debug.Log(sb.ToString());
        }
    }
}