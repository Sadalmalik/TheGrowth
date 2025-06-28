using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace XandArt.Architecture
{
    public class AssetGuidsManager : IPreprocessBuildWithReport
    {
        private static readonly string[] Separators = new[] { "Resources" };

        private static Dictionary<Guid, ScriptableAsset> _assets;
        
        public static void Initialize()
        {
            _assets ??= new Dictionary<Guid, ScriptableAsset>();
            _assets.Clear();
            var assets = Resources.LoadAll<ScriptableAsset>("").Distinct();
            foreach (var asset in assets)
            {
                _assets.Add(asset.Guid, asset);
            }
        }

        public static ScriptableAsset GetAsset(Guid guid)
        {
            if (_assets.TryGetValue(guid, out var asset))
                return asset;
            return null;
        }

        public static T GetAsset<T>(Guid guid) where T : ScriptableAsset
        {
            if (_assets.TryGetValue(guid, out var asset))
                return (T) asset;
            return null;
        }
        
        [MenuItem("[TheGrowth]/Update Assets Guids")]
        public static void UpdateGuids()
        {
            var paths = AssetDatabase.GetAllAssetPaths();

            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset is ScriptableAsset scriptableAsset)
                {
                    var meta = $"{Path.GetFullPath(path)}.meta";
                    var guid = GetMetaGuid(meta);
                    if (guid.HasValue)
                    {
                        if (scriptableAsset.Guid != Guid.Empty && scriptableAsset.Guid != guid.Value)
                        {
                            Debug.LogError($"Asset guid was changed: {asset}, ignoring new guid.\nPlease check .meta file");
                        }
                        else
                        {
                            scriptableAsset.Guid = guid.Value;
                            EditorUtility.SetDirty(scriptableAsset);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Can't load guid for asset {asset}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static Guid? GetMetaGuid(string metaPath)
        {
            var raw = (string) null;
            var lines = File.ReadLines(metaPath);
            foreach (var line in lines)
            {
                if (line.StartsWith("guid: "))
                {
                    raw = line.Substring(6);
                    break;
                }
            }

            if (Guid.TryParse(raw, out var guid))
                return guid;
            return null;
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            UpdateGuids();
        }
    }
}