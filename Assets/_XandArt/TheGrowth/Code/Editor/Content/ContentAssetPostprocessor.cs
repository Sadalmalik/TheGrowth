using System;
using System.IO;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Content
{
    /// <summary>
    /// Автоматическая пересборка реестра при импорте, удалении и перемещении конфигов и файлов скриптов в папке
    /// контента. Ошибки ID выводятся в консоль сразу (например, дубль после копирования ассета).
    /// </summary>
    public sealed class ContentAssetPostprocessor : AssetPostprocessor
    {
        private const string AssetExtension = ".asset";

        private static bool m_IsRebuilding;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (m_IsRebuilding || !IsRelevant(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths))
                return;

            m_IsRebuilding = true;
            try
            {
                var registry = ContentRegistryBuilder.Rebuild();
                ContentValidator.Log(ContentValidator.Validate(registry.Assets, registry.Scripts));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                m_IsRebuilding = false;
            }
        }

        private static bool IsRelevant(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (IsInContent(path) && (IsScript(path) || IsContentAsset(path)))
                    return true;
            }

            foreach (var path in deleted)
            {
                if (IsInContent(path) && (IsScript(path) || path.EndsWith(AssetExtension, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            for (var i = 0; i < moved.Length; i++)
            {
                if ((IsInContent(moved[i]) || IsInContent(movedFrom[i])) && (IsScript(moved[i]) || IsContentAsset(moved[i])))
                    return true;
            }

            return false;
        }

        private static bool IsInContent(string path)
        {
            return path.StartsWith(ContentRegistryBuilder.ContentRoot + "/", StringComparison.Ordinal)
                   && path != ContentRegistryBuilder.RegistryPath;
        }

        private static bool IsScript(string path)
        {
            return ScriptSyntax.TryGetFileKind(Path.GetExtension(path), out _);
        }

        private static bool IsContentAsset(string path)
        {
            if (!path.EndsWith(AssetExtension, StringComparison.OrdinalIgnoreCase))
                return false;

            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return type != null && typeof(ContentAsset).IsAssignableFrom(type);
        }
    }
}
