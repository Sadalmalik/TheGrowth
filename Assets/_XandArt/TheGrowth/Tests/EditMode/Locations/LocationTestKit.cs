using System.Collections.Generic;
using TheGrowth.Game.Flow;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Locations;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.Support;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Locations
{
    /// <summary>Конфиги локаций в памяти и «сцены» локаций — корневые объекты в памяти с уничтожением в <see cref="Dispose"/>.</summary>
    public sealed class LocationTestKit
    {
        private readonly List<GameObject> m_Objects = new List<GameObject>();

        public static LocationModel Location(ContentTestKit kit, string id, string sceneName, LocationType type = LocationType.Hub)
        {
            var location = kit.Asset<LocationModel>(id);
            var serialized = new SerializedObject(location);
            serialized.FindProperty("m_SceneName").stringValue = sceneName;
            serialized.FindProperty("m_Type").intValue = (int)type;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return location;
        }

        public static GameConfig Config(ContentTestKit kit, string id, LocationModel startLocation)
        {
            var config = kit.Asset<GameConfig>(id);
            var serialized = new SerializedObject(config);
            serialized.FindProperty("m_StartLocation").objectReferenceValue = startLocation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        /// <summary>Объект в памяти; корневые уничтожаются в <see cref="Dispose"/>, дочерние — вместе с родителем.</summary>
        public GameObject Object(string name, Transform parent = null, bool active = true)
        {
            var gameObject = new GameObject(name);
            if (parent == null)
                m_Objects.Add(gameObject);
            else
                gameObject.transform.SetParent(parent, false);

            gameObject.SetActive(active);
            return gameObject;
        }

        public static LocationTestBehaviour Behaviour(GameObject gameObject, string id, List<string> journal)
        {
            var behaviour = gameObject.AddComponent<LocationTestBehaviour>();
            behaviour.Id = id;
            behaviour.Journal = journal;
            return behaviour;
        }

        /// <summary>«Сцена» локации: корень с <see cref="LocationRoot"/> и поведением <paramref name="id"/>, второй корневой объект без них.</summary>
        public GameObject[] Scene(string id, List<string> journal, bool withRoot = true, bool throwOnInit = false)
        {
            var root = Object(id);
            if (withRoot)
                root.AddComponent<LocationRoot>();

            Behaviour(root, id, journal).ThrowOnInit = throwOnInit;
            return new[] { root, Object(id + "_other") };
        }

        public static void Destroy(IEnumerable<GameObject> objects)
        {
            foreach (var gameObject in objects)
            {
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        public void Dispose()
        {
            Destroy(m_Objects);
            m_Objects.Clear();
        }
    }
}
