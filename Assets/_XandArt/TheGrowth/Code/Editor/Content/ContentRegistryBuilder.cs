using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Content
{
    /// <summary>
    /// Сборка <see cref="ContentRegistry"/>: все <see cref="ContentAsset"/> и файлы скриптов (<c>.script</c>,
    /// <c>.dialog</c>) из папки контента. Пустые ID заполняются из имени ассета. Ассет реестра записывается,
    /// только если состав изменился. Набор ID конфигов и набор объявлений переменных публикуются как custom dependency
    /// <see cref="IdsDependency"/> и <see cref="VariablesDependency"/>: импортёр скриптов зависит от них и
    /// переимпортируется при их смене. Запуск: меню, <see cref="ContentAssetPostprocessor"/>, <see cref="ContentBuildPreprocessor"/>.
    /// </summary>
    public static class ContentRegistryBuilder
    {
        public const string ContentRoot = "Assets/_XandArt/TheGrowth";
        public const string RegistryPath = ContentRoot + "/Content/ContentRegistry.asset";
        public const string IdsDependency = "TheGrowth/ContentIds";
        public const string VariablesDependency = "TheGrowth/Variables";

        private const string MenuRoot = "[TheGrowth]/Content/";

        private static Hash128 m_PublishedIds;
        private static Hash128 m_PublishedVariables;
        private static bool m_IsRefreshScheduled;

#region Menu

        [MenuItem(MenuRoot + "Rebuild Registry")]
        public static void RebuildMenu()
        {
            var registry = Rebuild();
            var errors = ContentValidator.ValidateProject(registry);
            ContentValidator.Log(errors);
            Debug.Log($"[Content] Registry rebuilt: {registry.Assets.Count} assets, {registry.Scripts.Count} script files, {errors.Count} errors ({RegistryPath})", registry);
        }

        [MenuItem(MenuRoot + "Validate")]
        public static void ValidateMenu()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError($"[Content] Content registry not found at {RegistryPath}: run {MenuRoot}Rebuild Registry");
                return;
            }

            var errors = ContentValidator.ValidateProject(registry);
            ContentValidator.Log(errors);
            Debug.Log($"[Content] Validation: {errors.Count} errors", registry);
        }

#endregion

#region Rebuild

        /// <summary>
        /// Собрать реестр из <paramref name="root"/> в <paramref name="registryPath"/> (папки создаются).
        /// ID публикуются только для реестра проекта (<see cref="RegistryPath"/>).
        /// </summary>
        public static ContentRegistry Rebuild(string root = ContentRoot, string registryPath = RegistryPath)
        {
            var assets = FindContentAssets(root);
            FillEmptyIds(assets);
            assets.Sort(CompareAssets);

            var scripts = new List<ScriptFileEntry>();
            foreach (var path in FindScriptPaths(root))
            {
                ScriptSyntax.TryGetFileKind(Path.GetExtension(path), out var kind);
                scripts.Add(new ScriptFileEntry(ScriptParser.GetFileId(path), kind, AssetDatabase.LoadMainAssetAtPath(path) as TextAsset));
            }

            scripts.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var registry = WriteRegistry(registryPath, assets, scripts);
            if (registryPath == RegistryPath)
                PublishDependencies(registry);

            return registry;
        }

        /// <summary>Конфиги в папке (главные ассеты и вложенные), каждый один раз.</summary>
        public static List<ContentAsset> FindContentAssets(string folder)
        {
            var assets = new List<ContentAsset>();
            if (!AssetDatabase.IsValidFolder(folder))
                return assets;

            var seen = new HashSet<ContentAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(ContentAsset), new[] { folder }))
            {
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    if (item is ContentAsset asset && seen.Add(asset))
                        assets.Add(asset);
                }
            }

            return assets;
        }

        /// <summary>Пути файлов скриптов в папке, по алфавиту.</summary>
        public static List<string> FindScriptPaths(string folder)
        {
            var prefix = folder.TrimEnd('/') + "/";
            var paths = new List<string>();
            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (path.StartsWith(prefix, StringComparison.Ordinal) && ScriptSyntax.TryGetFileKind(Path.GetExtension(path), out _))
                    paths.Add(path);
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static void FillEmptyIds(List<ContentAsset> assets)
        {
            foreach (var asset in assets)
            {
                if (!string.IsNullOrEmpty(asset.Id))
                    continue;

                var path = AssetDatabase.GetAssetPath(asset);
                var name = AssetDatabase.IsMainAsset(asset) ? Path.GetFileNameWithoutExtension(path) : asset.name;
                asset.SetId(ContentAsset.MakeId(name));
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                Debug.Log($"[Content] {path}: empty id filled from asset name: '{asset.Id}'", asset);
            }
        }

        private static int CompareAssets(ContentAsset a, ContentAsset b)
        {
            var result = string.CompareOrdinal(ContentAsset.GetIdScope(a.GetType()).FullName, ContentAsset.GetIdScope(b.GetType()).FullName);
            if (result == 0)
                result = string.CompareOrdinal(a.Id, b.Id);

            if (result == 0)
                result = string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b));

            return result;
        }

        private static ContentRegistry WriteRegistry(string registryPath, List<ContentAsset> assets, List<ScriptFileEntry> scripts)
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(registryPath);
            if (registry == null)
            {
                if (File.Exists(registryPath))
                    throw new InvalidOperationException($"'{registryPath}' exists but is not a {nameof(ContentRegistry)}");

                EnsureFolder(Path.GetDirectoryName(registryPath)?.Replace('\\', '/'));
                registry = ScriptableObject.CreateInstance<ContentRegistry>();
                registry.SetContent(assets, scripts);
                AssetDatabase.CreateAsset(registry, registryPath);
                return registry;
            }

            if (registry.HasContent(assets, scripts))
            {
                // Состав тот же, но ID конфигов могли измениться — индексы пересобрать.
                registry.Invalidate();
                return registry;
            }

            registry.SetContent(assets, scripts);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssetIfDirty(registry);
            return registry;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

#endregion

#region Dependencies

        /// <summary>Хеш набора «тип: ID» конфигов реестра — значение <see cref="IdsDependency"/>.</summary>
        public static Hash128 ComputeIdsHash(ContentRegistry registry)
        {
            var builder = new StringBuilder();
            foreach (var asset in registry.Assets)
            {
                if (asset != null)
                    builder.Append(asset.GetType().FullName).Append(':').Append(asset.Id).Append('\n');
            }

            return Hash128.Compute(builder.ToString());
        }

        /// <summary>
        /// Хеш объявлений переменных реестра (имя, область, тип, прежние имена — всё, от чего зависит разбор скриптов) —
        /// значение <see cref="VariablesDependency"/>. Значения по умолчанию и описания на разбор не влияют.
        /// </summary>
        public static Hash128 ComputeVariablesHash(ContentRegistry registry)
        {
            var builder = new StringBuilder();
            foreach (var asset in registry.Assets)
            {
                if (asset is not VariableDeclarations declarations)
                    continue;

                foreach (var variable in declarations.Variables)
                {
                    if (variable == null)
                        continue;

                    builder.Append(variable.Name).Append(':').Append((int)variable.Scope).Append(':').Append((int)variable.Type);
                    foreach (var previous in variable.PreviousNames)
                        builder.Append(':').Append(previous);

                    builder.Append('\n');
                }
            }

            return Hash128.Compute(builder.ToString());
        }

        /// <summary>
        /// Опубликовать ID и объявления переменных для импортёра скриптов. Unity переимпортирует зависимые файлы при
        /// следующем Refresh; он запускается после текущего импорта, если набор изменился.
        /// </summary>
        private static void PublishDependencies(ContentRegistry registry)
        {
            var ids = ComputeIdsHash(registry);
            var variables = ComputeVariablesHash(registry);
            AssetDatabase.RegisterCustomDependency(IdsDependency, ids);
            AssetDatabase.RegisterCustomDependency(VariablesDependency, variables);
            if (ids == m_PublishedIds && variables == m_PublishedVariables)
                return;

            m_PublishedIds = ids;
            m_PublishedVariables = variables;
            if (m_IsRefreshScheduled)
                return;

            m_IsRefreshScheduled = true;
            EditorApplication.delayCall += HandleDelayedRefresh;
        }

        private static void HandleDelayedRefresh()
        {
            m_IsRefreshScheduled = false;
            AssetDatabase.Refresh();
        }

#endregion
    }
}
