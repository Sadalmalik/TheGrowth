using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.EditorTools.Content
{
    /// <summary>
    /// Проверки контента: ID конфигов (пустой, не идентификатор, дубль в области ID), ID файлов скриптов,
    /// объявления переменных (<see cref="VariableCatalog"/>) и тегов (<see cref="TagCatalog"/>), графы полей локаций
    /// (граф помнит сцену локации, один граф — одна локация, данные графа — <see cref="GraphChecks"/>), пропавшие ссылки,
    /// конфиги и файлы скриптов проекта вне реестра, графы сцен локаций, на которые локация не ссылается, настройки поля
    /// (<see cref="FieldConfig"/> — ровно один в реестре проекта). Сцены не открываются. Ошибки — списком (для тестов и
    /// pre-build), <see cref="Log"/> выводит их в консоль с выделением ассета.
    /// </summary>
    public static class ContentValidator
    {
        public const string ProjectFolder = "Assets";

        private const string NotInRegistryHint = "content must be under " + ContentRegistryBuilder.ContentRoot + "; run [TheGrowth]/Content/Rebuild Registry";

        /// <summary>
        /// Проверка списков реестра без обращения к проекту. <paramref name="isComplete"/> — списки содержат весь контент
        /// проекта (реестр проекта): обязательные конфиги (<see cref="FieldConfig"/>) должны быть; без него проверяются только
        /// лишние (тестовые реестры содержат часть контента).
        /// </summary>
        public static List<ContentError> Validate(IReadOnlyList<ContentAsset> assets, IReadOnlyList<ScriptFileEntry> scripts, bool isComplete = false)
        {
            var errors = new List<ContentError>();
            ValidateAssets(assets ?? Array.Empty<ContentAsset>(), errors);
            ValidateScripts(scripts ?? Array.Empty<ScriptFileEntry>(), errors);
            ValidateVariables(assets ?? Array.Empty<ContentAsset>(), errors);
            var tags = new TagCatalog((assets ?? Array.Empty<ContentAsset>()).OfType<TagDeclarations>());
            ValidateTags(tags, errors);
            ValidateGraphs(assets ?? Array.Empty<ContentAsset>(), tags, errors);
            ValidateFieldConfig(assets ?? Array.Empty<ContentAsset>(), isComplete, errors);
            return errors;
        }

        /// <summary>
        /// <see cref="Validate"/> по спискам реестра и поиск в <paramref name="searchFolder"/> конфигов и файлов
        /// скриптов, которых в реестре нет (лежат вне папки контента или реестр устарел). Реестр проекта
        /// (<see cref="ContentRegistryBuilder.RegistryPath"/>) проверяется как полный: обязательные конфиги должны быть.
        /// </summary>
        public static List<ContentError> ValidateProject(ContentRegistry registry, string searchFolder = ProjectFolder)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var isProjectRegistry = AssetDatabase.GetAssetPath(registry) == ContentRegistryBuilder.RegistryPath;
            var errors = Validate(registry.Assets, registry.Scripts, isProjectRegistry);

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

            ValidateUnusedGraphs(registry, searchFolder, errors);
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

        private static void ValidateVariables(IReadOnlyList<ContentAsset> assets, List<ContentError> errors)
        {
            var catalog = new VariableCatalog(assets.OfType<VariableDeclarations>());
            foreach (var error in catalog.Errors)
            {
                var message = string.IsNullOrEmpty(error.Variable) ? error.Message : $"variable '{error.Variable}': {error.Message}";
                errors.Add(Error(ContentErrorKind.InvalidVariable, error.Source, message));
            }
        }

        private static void ValidateTags(TagCatalog catalog, List<ContentError> errors)
        {
            foreach (var error in catalog.Errors)
            {
                var message = string.IsNullOrEmpty(error.Tag) ? error.Message : $"tag '{error.Tag}': {error.Message}";
                errors.Add(Error(ContentErrorKind.InvalidTag, error.Source, message));
            }
        }

        /// <summary>Граф локации: помнит её сцену, не принадлежит другой локации, данные целы, теги слотов объявлены.</summary>
        private static void ValidateGraphs(IReadOnlyList<ContentAsset> assets, TagCatalog tags, List<ContentError> errors)
        {
            var owners = new Dictionary<GraphModel, LocationModel>();
            foreach (var location in assets.OfType<LocationModel>())
            {
                var graph = location.Graph;
                if (graph == null)
                    continue;

                if (owners.TryGetValue(graph, out var owner))
                {
                    errors.Add(Error(ContentErrorKind.InvalidGraph, location, $"graph '{GetPath(graph)}' is also the graph of {Describe(owner)}"));
                    continue;
                }

                owners.Add(graph, location);
                if (graph.SceneName != location.SceneName)
                {
                    errors.Add(Error(ContentErrorKind.InvalidGraph, location,
                        $"graph '{GetPath(graph)}' belongs to scene '{graph.SceneName}', the location scene is '{location.SceneName}' (open the location scene and save it)"));
                }

                foreach (var message in GraphChecks.Check(graph, tags))
                    errors.Add(Error(ContentErrorKind.InvalidGraph, graph, message));
            }
        }

        /// <summary>Настройки поля — один конфиг на проект: несколько — ошибка у каждого; ни одного в полном реестре — ошибка.</summary>
        private static void ValidateFieldConfig(IReadOnlyList<ContentAsset> assets, bool isComplete, List<ContentError> errors)
        {
            var configs = assets.OfType<FieldConfig>().ToList();
            if (configs.Count == 0 && isComplete)
            {
                errors.Add(new ContentError(ContentErrorKind.InvalidFieldConfig, ContentRegistryBuilder.RegistryPath,
                    $"no {nameof(FieldConfig)} in the project: create one ([Game]/Field Config) under {ContentRegistryBuilder.ContentRoot}/Content", null));
                return;
            }

            if (configs.Count < 2)
                return;

            foreach (var config in configs)
            {
                var others = string.Join(", ", configs.Where(other => other != config).Select(GetPath));
                errors.Add(Error(ContentErrorKind.InvalidFieldConfig, config, $"more than one {nameof(FieldConfig)} in the project, one is allowed (also: {others})"));
            }
        }

        /// <summary>Граф сцены локации есть в проекте, а локация ссылается на другой граф или ни на какой.</summary>
        private static void ValidateUnusedGraphs(ContentRegistry registry, string searchFolder, List<ContentError> errors)
        {
            var locations = registry.Assets.OfType<LocationModel>().ToList();
            var used = new HashSet<GraphModel>(locations.Where(location => location.Graph != null).Select(location => location.Graph));
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(GraphModel), new[] { searchFolder }))
            {
                var graph = AssetDatabase.LoadAssetAtPath<GraphModel>(AssetDatabase.GUIDToAssetPath(guid));
                if (graph == null || used.Contains(graph) || string.IsNullOrEmpty(graph.SceneName))
                    continue;

                var location = locations.Find(item => item.SceneName == graph.SceneName);
                if (location != null)
                {
                    errors.Add(Error(ContentErrorKind.InvalidGraph, graph,
                        $"graph of scene '{graph.SceneName}' is not referenced by {Describe(location)} (open the location scene and save it)"));
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
