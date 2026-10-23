using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Tags;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Graphs;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Locations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Графы полей в памяти с уничтожением в <see cref="Dispose"/>; проверки целостности и степеней слотов. Пока набор жив,
    /// размер карты редактора поля — 1 × 1 (расстояния в размерах карты равны обычным), <see cref="CardSize"/> меняет его.
    /// </summary>
    public sealed class GraphTestKit : IDisposable
    {
        private readonly List<Object> m_Objects = new List<Object>();
        private readonly List<IDisposable> m_Overrides = new List<IDisposable>();

        public GraphTestKit()
        {
            CardSize(Vector2.one);
        }

        /// <summary>Размер карты редактора поля до <see cref="Dispose"/>.</summary>
        public void CardSize(Vector2 size)
        {
            m_Overrides.Add(GraphCardSize.Override(size));
        }

        /// <summary>Объект сцены с графом (позиции слотов — в его локальных координатах), уничтожается в <see cref="Dispose"/>.</summary>
        public Graph SceneGraph(GraphModel model, Vector3 position = default)
        {
            var gameObject = Track(new GameObject("Graph"));
            gameObject.transform.position = position;
            var graph = gameObject.AddComponent<Graph>();
            var serialized = new SerializedObject(graph);
            serialized.FindProperty("m_Model").objectReferenceValue = model;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return graph;
        }

        /// <summary>Объект сцены для уничтожения в <see cref="Dispose"/>.</summary>
        public T Track<T>(T item) where T : Object
        {
            m_Objects.Add(item);
            return item;
        }

        /// <summary>Пустой граф: минимальный радиус 0, автоконнект выключен, квадратная сетка с шагом 1.</summary>
        public GraphModel Graph(string sceneName = null, string name = "TestGraph")
        {
            var graph = ScriptableObject.CreateInstance<GraphModel>();
            m_Objects.Add(graph);
            graph.name = name;
            graph.SetSceneName(sceneName);
            var settings = graph.EditorSettings;
            settings.MinRadius = 0f;
            settings.AutoConnect = false;
            settings.AutoConnectRadius = 0f;
            settings.GridType = GridType.Square;
            settings.GridStep = 1f;
            settings.Diagonals = false;
            return graph;
        }

        public static LocationModel Location(ContentTestKit kit, string id, string sceneName, GraphModel graph)
        {
            var location = LocationTestKit.Location(kit, id, sceneName);
            var serialized = new SerializedObject(location);
            serialized.FindProperty("m_Graph").objectReferenceValue = graph;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return location;
        }

        public static TagDeclarations Tags(ContentTestKit kit, string id, params string[] names)
        {
            var tags = kit.Asset<TagDeclarations>(id);
            tags.SetTags(names.Select(name => new TagDeclaration(name)));
            return tags;
        }

        public static void AssertIntact(GraphModel graph)
        {
            var errors = GraphChecks.Check(graph);
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        public static int Degree(GraphModel graph, string slotId)
        {
            return graph.Links.Count(link => link.Connects(slotId));
        }

        public static GraphSlot SlotAt(GraphModel graph, Vector3 position)
        {
            var slot = graph.Slots.FirstOrDefault(item => Vector3.Distance(item.Position, position) < 1e-4f);
            Assert.IsNotNull(slot, $"no slot at {position}");
            return slot;
        }

        public static bool AreLinked(GraphModel graph, GraphSlot a, GraphSlot b)
        {
            return graph.FindLink(a.Id, b.Id) != null;
        }

        public void Dispose()
        {
            foreach (var item in m_Objects)
            {
                if (item == null)
                    continue;

                Undo.ClearUndo(item);
                Object.DestroyImmediate(item);
            }

            m_Objects.Clear();
            for (var i = m_Overrides.Count - 1; i >= 0; i--)
                m_Overrides[i].Dispose();

            m_Overrides.Clear();
        }
    }
}
