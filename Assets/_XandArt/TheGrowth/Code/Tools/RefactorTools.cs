using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XandArt.TheGrowth
{
    public static class RefactorTools
    {
#if UNITY_EDITOR
        [MenuItem("[TheGrowth]/Refactoring/Get Types")]
#endif
        private static void DumpTypes()
        {
            var sb = new StringBuilder();
            var assembly = Assembly.GetExecutingAssembly();
            var allTypes = assembly.GetTypes();

            var commands = allTypes.Where(type => typeof(XandArt.TheGrowth.Command).IsAssignableFrom(type));
            var conditions = allTypes.Where(type => typeof(XandArt.TheGrowth.Condition).IsAssignableFrom(type));
            var evaluators = allTypes.Where(type => typeof(XandArt.TheGrowth.IEvaluator).IsAssignableFrom(type));

            sb.AppendLine("\n// Commands:");
            IterateTypes(commands);
            
            sb.AppendLine("\n// Conditions:");
            IterateTypes(conditions);
            
            sb.AppendLine("\n// Evaluators:");
            IterateTypes(evaluators);
            
            Debug.Log(sb.ToString());

            void IterateTypes(IEnumerable<Type> types)
            {
                foreach (Type type in types)
                {
                    sb.AppendLine($"[assembly: BindTypeNameToType(\"XandArt.TheGrowth.{type.Name}\", typeof({type.Name}))]");
                }
            }
        }
    }
}