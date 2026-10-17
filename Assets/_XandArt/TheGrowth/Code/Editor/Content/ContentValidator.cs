using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.EditorTools.Content
{
    /// <summary>
    /// Проверки контента: ID конфигов (пустой, не идентификатор, дубль в области ID), ID файлов скриптов,
    /// пропавшие ссылки, конфиги и файлы скриптов проекта вне реестра. Ошибки — списком (для тестов и
    /// pre-build), <see cref="Log"/> выводит их в консоль с выделением ассета.
    /// </summary>
    public static class ContentValidator
    {
        public const string ProjectFolder = "Assets";

        private const string NotInRegistryHint = "content must be under " + ContentRegistryBuilder.ContentRoot + "; run [TheGrowth]/Content/Rebuild Registry";

        /// <summary>Проверка списков реестра без обращения к проекту.</summary>
        public static List<ContentError> Validate(IReadOnlyList<ContentAsset> assets, IReadOnlyList<ScriptFileEntry> scripts)
        {
            var errors = new List<ContentError>();
            ValidateAssets(assets ?? Array.Empty<ContentAsset>(), errors);
            ValidateScripts(scripts ?? Array.Empty<ScriptFileEntry>(), errors);
            return errors;
        }

        /// <summary>
        /// <see cref="Validate"/> по спискам реестра и поиск в <paramref name="searchFolder"/> конфигов и файлов
        /// скриптов, которых в реестре нет (лежат вне папки контента или реестр устарел).
        /// </summary>
        public static List<ContentError> ValidateProject(ContentRegistry registry, string searchFolder = ProjectFolder)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var errors = Validate(registry.Assets, registry.Scripts);

            var known = new HashSet<Object>(registry.Assets.Where(asset => asset != null));
            foreach (var asset in ContentRegistryBuilder.FindContentAssets(searchFolder))
            {
                if (!known.Contains(asset))
                    errors.Add(Error(ContentErrorKind.NotInRegistry, asset, $"{Describe(asset)} is not in content registry '{GetPath(registry)}' ({NotInRegistryHint})"));
            }

            var knownScripts = new HashSet<Object>(registry.Scripts.Where(entry => entry.Text != null).Select(entry => (Object)entry.Text));
            foreach (var path in ContentRegistryBuilder.FindScriptPaths(searchFolder))
            {
                var text = AssetDatabase.LoadMainAssetAtPath(path);
                if (text == null || !knownScripts.Contains(text))
                    errors.Add(new ContentError(ContentErrorKind.NotInRegistry, path, $"script file is not in content registry '{GetPath(registry)}' ({NotInRegistryHint})", text));
            }

            return errors;
        }

        public static void Log(IReadOnlyList<ContentError> errors)
        {
            foreach (var error in errors)
                Debug.LogError($"[Content] {error}", error.Context);
        }

#region Checks

        private static void ValidateAssets(IReadOnlyList<ContentAsset> assets, List<ContentError> errors)
        {
            var byScope = new Dictionary<(Type, string), List<ContentAsset>>();
            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    errors.Add(new ContentError(ContentErrorKind.MissingAsset, $"<registry entry {i}>", "content asset is missing (deleted or not imported)", null));
                    continue;
                }

                if (string.IsNullOrEmpty(asset.Id))
                {
                    errors.Add(Error(ContentErrorKind.EmptyId, asset, $"{asset.GetType().Name} has empty id"));
                    continue;
                }

                if (!ContentAsset.IsValidId(asset.Id))
                {
                    errors.Add(Error(ContentErrorKind.InvalidId, asset,
                        $"{asset.GetType().Name} id '{asset.Id}' is not an identifier (letters, digits, '_', not starting with a digit)"));
                    continue;
                }

                var key = (ContentAsset.GetIdScope(asset.GetType()), asset.Id);
                if (!byScope.TryGetValue(key, out var group))
                    byScope.Add(key, group = new List<ContentAsset>());

                group.Add(asset);
            }

            foreach (var pair in byScope)
            {
                var group = pair.Value;
                if (group.Count < 2)
                    continue;

                foreach (var asset in group)
                {
                    var others = string.Join(", ", group.Where(other => other != asset).Select(GetPath));
                    errors.Add(Error(ContentErrorKind.DuplicateId, asset, $"duplicate {pair.Key.Item1.Name} id '{asset.Id}' (also: {others})"));
                }
            }
        }

        private static void ValidateScripts(IReadOnlyList<ScriptFileEntry> scripts, List<ContentError> errors)
        {
            var byId = new Dictionary<string, List<ScriptFileEntry>>(StringComparer.Ordinal);
            foreach (var entry in scripts)
            {
                if (entry.Text == null)
                {
                    errors.Add(new ContentError(ContentErrorKind.MissingScriptText, $"<script {entry}>", "script file text asset is missing", null));
                    continue;
                }

                if (!ScriptSyntax.IsIdentifier(entry.Id))
                {
                    errors.Add(Error(ContentErrorKind.InvalidScriptId, entry.Text,
                        $"script file id '{entry.Id}' is not an identifier (letters, digits, '_', not starting with a digit): scripts can't reference it"));
                    continue;
                }

                if (!byId.TryGetValue(entry.Id, out var group))
                    byId.Add(entry.Id, group = new List<ScriptFileEntry>());

                group.Add(entry);
            }

            foreach (var group in byId.Values)
            {
                if (group.Count < 2)
                    continue;

                foreach (var entry in group)
                {
                    var others = string.Join(", ", group.Where(other => other.Text != entry.Text).Select(other => GetPath(other.Text)));
                    errors.Add(Error(ContentErrorKind.DuplicateScriptId, entry.Text, $"duplicate script file id '{entry.Id}' (also: {others})"));
                }
            }
        }

#endregion

#region Helpers

        private static ContentError Error(ContentErrorKind kind, Object asset, string message)
        {
            return new ContentError(kind, GetPath(asset), message, asset);
        }

        private static string Describe(ContentAsset asset)
        {
            return string.IsNullOrEmpty(asset.Id) ? asset.GetType().Name : $"{asset.GetType().Name} '{asset.Id}'";
        }

        /// <summary>Путь ассета; объект в памяти — <c>&lt;memory:имя&gt;</c>.</summary>
        public static string GetPath(Object asset)
        {
            if (asset == null)
                return "<missing>";

            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? $"<memory:{asset.name}>" : path;
        }

#endregion
    }
}
