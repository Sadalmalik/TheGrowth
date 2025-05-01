using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace XandArt.TheGrowth
{
    public class AssetPathsManager : IPreprocessBuildWithReport
    {
        private static readonly string[] Separators = new[] { "Resources" };
        
        [MenuItem("[TheGrowth]/Fix Assets Paths")]
        public static void FixPaths()
        {
            var paths = AssetDatabase.GetAllAssetPaths();

            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset is ScriptableAsset scriptableAsset)
                {
                    scriptableAsset.Path = path.Split(
                            Separators,
                            StringSplitOptions.None)
                        .Last().TrimStart('\\', '/');
                    EditorUtility.SetDirty(scriptableAsset);
                }
            }

            AssetDatabase.SaveAssets();
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            FixPaths();
        }
    }
}