using System.IO;
using TheGrowth.EditorTools.Flow;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Graphs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Связь конфига графа со сценой и локацией: конфиг помнит свою сцену (<see cref="GraphModel.SceneName"/>), локация
    /// сцены (<see cref="LocationModel"/> с её именем, как <see cref="PlayModeStart.FindLocation"/>) ссылается на конфиг
    /// из компонента <see cref="Graph"/>. Запись — при назначении и создании конфига в инспекторе, при включении
    /// инструментов поля и при сохранении сцены; без Undo, ассеты сохраняются сразу. Совпадение проверяет валидатор контента.
    /// </summary>
    [InitializeOnLoad]
    public static class GraphLocationSync
    {
        public const string ConfigSuffix = "_Graph";

        static GraphLocationSync()
        {
            EditorSceneManager.sceneSaved += HandleSceneSaved;
        }

        /// <summary>Путь конфига графа рядом со сценой: <c>&lt;папка сцены&gt;/&lt;имя сцены&gt;_Graph.asset</c>.</summary>
        public static string GetConfigPath(string scenePath)
        {
            var folder = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
            return $"{folder}/{Path.GetFileNameWithoutExtension(scenePath)}{ConfigSuffix}.asset";
        }

        /// <summary>Записать конфиг графа сцены в её локацию. Проблема (сцена не сохранена, нет локации, чужой граф) — текст, иначе null.</summary>
        public static string Sync(Graph graph)
        {
            if (graph == null || graph.Model == null)
                return null;

            var scene = graph.gameObject.scene;
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                return "the scene is not saved: save it to bind the graph to its location";

            return Apply(graph.Model, scene.name, PlayModeStart.FindLocationModel(scene.name));
        }

        /// <summary>Конфиг графа помнит сцену и записан в локацию <paramref name="location"/> (null — у сцены нет локации).</summary>
        public static string Apply(GraphModel graph, string sceneName, LocationModel location)
        {
            if (!string.IsNullOrEmpty(graph.SceneName) && graph.SceneName != sceneName)
                return $"graph '{GetPath(graph)}' belongs to scene '{graph.SceneName}': create a graph for scene '{sceneName}' or rebind it in the Graph inspector";

            if (graph.SceneName != sceneName)
            {
                graph.SetSceneName(sceneName);
                Save(graph);
            }

            if (location == null)
                return $"no {nameof(LocationModel)} references scene '{sceneName}'";

            if (location.Graph == graph)
                return null;

            var serialized = new SerializedObject(location);
            serialized.FindProperty("m_Graph").objectReferenceValue = graph;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Save(location);
            Debug.Log($"[Graph] {location}: graph is '{GetPath(graph)}'", location);
            return null;
        }

        /// <summary>Перенести граф на сцену <paramref name="sceneName"/> (сцена переименована или скопирована вместе с графом).</summary>
        public static void Rebind(GraphModel graph, string sceneName)
        {
            graph.SetSceneName(sceneName);
            Save(graph);
        }

        private static void HandleSceneSaved(Scene scene)
        {
            Graph graph = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var found in root.GetComponentsInChildren<Graph>(true))
                {
                    if (graph == null)
                    {
                        graph = found;
                        continue;
                    }

                    Debug.LogWarning($"[Graph] Scene '{scene.name}': more than one {nameof(Graph)} ('{graph.name}', '{found.name}'), the first is used", found);
                }
            }

            var problem = Sync(graph);
            if (problem != null)
                Debug.LogWarning($"[Graph] Scene '{scene.name}': {problem}", graph);
        }

        private static void Save(Object asset)
        {
            EditorUtility.SetDirty(asset);
            if (EditorUtility.IsPersistent(asset))
                AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static string GetPath(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? asset.name : path;
        }
    }
}
