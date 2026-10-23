using System.IO;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Tags;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Flow;
using TheGrowth.EditorTools.Graphs;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Tests.EditMode.Content;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.Graphs
{
    /// <summary>
    /// Граф и локация: запись графа в локацию (<see cref="GraphLocationSync.Apply"/>), проверки валидатора контента
    /// (сцена графа, граф у двух локаций, целостность, теги, граф сцены без ссылки из локации, ровно один конфиг настроек
    /// поля), поле тестовой локации.
    /// </summary>
    public sealed class GraphValidationTests
    {
        private const string TempFolder = "Assets/__T069GraphTest";

        private ContentTestKit m_Content;
        private GraphTestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Content = new ContentTestKit();
            m_Kit = new GraphTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
            m_Content.Dispose();
            AssetDatabase.DeleteAsset(TempFolder);
        }

        private static ContentError[] GraphErrors(params ContentAsset[] assets)
        {
            return ContentValidator.Validate(assets, null).Where(error => error.Kind == ContentErrorKind.InvalidGraph).ToArray();
        }

        private GraphModel TaggedGraph(string sceneName)
        {
            var graph = m_Kit.Graph(sceneName);
            var slot = GraphEditing.AddSlot(graph, Vector3.zero);
            GraphEditing.AddTag(graph, slot.Id, "PlayerSpawn");
            GraphEditing.AddLink(graph, slot.Id, GraphEditing.AddSlot(graph, Vector3.right).Id);
            return graph;
        }

#region Sync

        [Test]
        public void Apply_RemembersScene_WritesGraphToLocation()
        {
            var graph = m_Kit.Graph();
            var location = LocationTestKitGraph("forest", "ForestScene", null);

            Assert.IsNull(GraphLocationSync.Apply(graph, "ForestScene", location));

            Assert.AreEqual("ForestScene", graph.SceneName);
            Assert.AreSame(graph, location.Graph);
            Assert.IsNull(GraphLocationSync.Apply(graph, "ForestScene", location), "repeated call changes nothing");
        }

        [Test]
        public void Apply_GraphOfOtherScene_NotWritten_UntilRebound()
        {
            var graph = m_Kit.Graph("OldScene");
            var location = LocationTestKitGraph("forest", "ForestScene", null);

            var problem = GraphLocationSync.Apply(graph, "ForestScene", location);

            StringAssert.Contains("belongs to scene 'OldScene'", problem);
            Assert.IsNull(location.Graph);

            GraphLocationSync.Rebind(graph, "ForestScene");
            Assert.IsNull(GraphLocationSync.Apply(graph, "ForestScene", location));
            Assert.AreSame(graph, location.Graph);
        }

        [Test]
        public void Apply_NoLocation_RemembersScene_ReportsIt()
        {
            var graph = m_Kit.Graph();

            var problem = GraphLocationSync.Apply(graph, "Sketch", null);

            StringAssert.Contains("no LocationModel references scene 'Sketch'", problem);
            Assert.AreEqual("Sketch", graph.SceneName);
        }

        [Test]
        public void ConfigPath_NextToScene()
        {
            Assert.AreEqual("Assets/Scenes/Locations/Forest_Graph.asset", GraphLocationSync.GetConfigPath("Assets/Scenes/Locations/Forest.unity"));
        }

#endregion

#region Validator

        [Test]
        public void LocationGraph_SameScene_DeclaredTags_NoErrors()
        {
            var graph = TaggedGraph("ForestScene");
            var location = LocationTestKitGraph("forest", "ForestScene", graph);
            var tags = GraphTestKit.Tags(m_Content, "Tags", "PlayerSpawn", "Exit");

            var errors = ContentValidator.Validate(new ContentAsset[] { location, tags }, null);

            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void LocationGraph_OfOtherScene_Error()
        {
            var graph = m_Kit.Graph("OtherScene");
            var location = LocationTestKitGraph("forest", "ForestScene", graph);

            var errors = GraphErrors(location);

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("belongs to scene 'OtherScene', the location scene is 'ForestScene'", errors[0].Message);
            Assert.AreSame(location, errors[0].Context);
        }

        [Test]
        public void Graph_OfTwoLocations_Error()
        {
            var graph = m_Kit.Graph("ForestScene");
            var forest = LocationTestKitGraph("forest", "ForestScene", graph);
            var copy = LocationTestKitGraph("forest_copy", "ForestScene", graph);

            var errors = GraphErrors(forest, copy);

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("is also the graph of LocationModel 'forest'", errors[0].Message);
        }

        [Test]
        public void SlotTag_NotDeclared_Error()
        {
            var graph = TaggedGraph("ForestScene");
            var location = LocationTestKitGraph("forest", "ForestScene", graph);
            var tags = GraphTestKit.Tags(m_Content, "Tags", "Exit");

            var errors = GraphErrors(location, tags);

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("tag 'PlayerSpawn' is not declared", errors[0].Message);
            Assert.AreSame(graph, errors[0].Context);
        }

        [Test]
        public void BrokenGraphData_Errors()
        {
            var graph = m_Kit.Graph("ForestScene");
            graph.SlotList.Add(new GraphSlot("aaaaaa", Vector3.zero));
            graph.SlotList.Add(new GraphSlot("aaaaaa", Vector3.one));
            graph.SlotList.Add(new GraphSlot("bbbbbb", Vector3.right, "nogrid"));
            graph.LinkList.Add(new GraphLink("cccccc", "aaaaaa", "bbbbbb"));
            graph.LinkList.Add(new GraphLink("dddddd", "bbbbbb", "aaaaaa"));
            graph.LinkList.Add(new GraphLink("eeeeee", "aaaaaa", "missing"));
            graph.LinkList.Add(new GraphLink("ffffff", "bbbbbb", "bbbbbb"));
            graph.GridList.Add(new GraphGrid("gggggg", GridType.Square, 1f, Vector3.zero, Vector3.one));
            graph.GridList.Add(new GraphGrid("cccccc", GridType.Square, 1f, Vector3.zero, Vector3.one));

            var messages = GraphChecks.Check(graph);

            Assert.AreEqual(8, messages.Count, string.Join("\n", messages));
            Assert.AreEqual(2, messages.Count(message => message.Contains("duplicate id")), "a slot id twice, a link id equal to a grid id");
            Assert.IsTrue(messages.Any(message => message.Contains("grid 'nogrid' is missing")));
            Assert.AreEqual(2, messages.Count(message => message.Contains("has no slots")), "both grids are empty");
            Assert.IsTrue(messages.Any(message => message.Contains("link 'dddddd'") && message.Contains("already linked")));
            Assert.IsTrue(messages.Any(message => message.Contains("link 'eeeeee'") && message.Contains("slot is missing")));
            Assert.IsTrue(messages.Any(message => message.Contains("link 'ffffff'") && message.Contains("to itself")));
            Assert.AreEqual(8, GraphErrors(LocationTestKitGraph("forest", "ForestScene", graph)).Length, "the validator reports graph data errors");
        }

        [Test]
        public void ValidateProject_GraphOfLocationScene_NotReferenced_Error()
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));
            var location = ScriptableObject.CreateInstance<LocationModel>();
            location.SetId("forest");
            var serialized = new SerializedObject(location);
            serialized.FindProperty("m_SceneName").stringValue = "ForestScene";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(location, TempFolder + "/Forest.asset");
            var graph = ScriptableObject.CreateInstance<GraphModel>();
            graph.SetSceneName("ForestScene");
            AssetDatabase.CreateAsset(graph, TempFolder + "/ForestScene_Graph.asset");
            var registry = ContentRegistryBuilder.Rebuild(TempFolder, TempFolder + "/Registry.asset");

            var errors = ContentValidator.ValidateProject(registry, TempFolder);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(ContentErrorKind.InvalidGraph, errors[0].Kind);
            Assert.AreEqual(TempFolder + "/ForestScene_Graph.asset", errors[0].Path);
            StringAssert.Contains("is not referenced by LocationModel 'forest'", errors[0].Message);

            Assert.IsNull(GraphLocationSync.Apply(graph, "ForestScene", location));
            Assert.IsEmpty(ContentValidator.ValidateProject(registry, TempFolder));
        }

#endregion

#region Field config

        private static ContentError[] FieldConfigErrors(bool isComplete, params ContentAsset[] assets)
        {
            return ContentValidator.Validate(assets, null, isComplete).Where(error => error.Kind == ContentErrorKind.InvalidFieldConfig).ToArray();
        }

        [Test]
        public void FieldConfig_ExactlyOne_NoErrors()
        {
            var config = m_Content.Asset<FieldConfig>("FieldConfig");

            Assert.IsEmpty(ContentValidator.Validate(new ContentAsset[] { config }, null, true));
            Assert.AreEqual(FieldConfig.DefaultCardSize, config.CardSize, "1.5 x 2 by default");
        }

        [Test]
        public void FieldConfig_None_ErrorInCompleteContentOnly()
        {
            var errors = FieldConfigErrors(true);

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("no FieldConfig in the project", errors[0].Message);
            Assert.IsEmpty(FieldConfigErrors(false), "partial content (test registries) may have none");
        }

        [Test]
        public void FieldConfig_Two_ErrorForEach()
        {
            var first = m_Content.Asset<FieldConfig>("FieldConfig");
            var second = m_Content.Asset<FieldConfig>("FieldConfig_2");

            var errors = FieldConfigErrors(false, first, second);

            Assert.AreEqual(2, errors.Length, string.Join("\n", errors));
            Assert.That(errors.Select(error => error.Context), Is.EquivalentTo(new Object[] { first, second }));
            StringAssert.Contains("more than one FieldConfig", errors[0].Message);
        }

        [Test]
        public void Project_HasOneFieldConfigInContent_EditorUsesItsCardSize()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            var configs = registry.GetAll<FieldConfig>();

            Assert.AreEqual(1, configs.Count);
            Assert.AreEqual(GraphSceneSetup.FieldConfigPath, AssetDatabase.GetAssetPath(configs[0]));
            Assert.AreSame(configs[0], GraphCardSize.FindConfig());
            Assert.IsEmpty(ContentValidator.ValidateProject(registry).Where(error => error.Kind == ContentErrorKind.InvalidFieldConfig));
        }

#endregion

        [Test]
        public void TestLocation_HasGraphNextToScene_WithSpawnAndExit()
        {
            var location = AssetDatabase.LoadAssetAtPath<LocationModel>(FlowSceneSetup.TestLocationPath);
            var tags = AssetDatabase.LoadAssetAtPath<TagDeclarations>(GraphSceneSetup.TagsPath);

            Assert.IsNotNull(location);
            Assert.IsNotNull(tags);
            Assert.That(tags.Tags.Select(tag => tag.Name), Is.SupersetOf(new[] { GraphSceneSetup.PlayerSpawnTag, GraphSceneSetup.ExitTag }));

            var graph = location.Graph;
            Assert.IsNotNull(graph);
            Assert.AreEqual(GraphLocationSync.GetConfigPath(FlowSceneSetup.TestLocationScenePath), AssetDatabase.GetAssetPath(graph));
            Assert.AreEqual(location.SceneName, graph.SceneName);
            Assert.AreEqual(1, graph.Slots.Count(slot => slot.HasTag(GraphSceneSetup.PlayerSpawnTag)));
            Assert.AreEqual(1, graph.Slots.Count(slot => slot.HasTag(GraphSceneSetup.ExitTag)));
            Assert.AreEqual(1, graph.Grids.Count);
            Assert.AreEqual(GraphSceneSetup.TestGridStep, graph.Grids[0].Step, "rebuilt with the step in card sizes");
            Assert.IsEmpty(GraphChecks.Check(graph, new TagCatalog(new[] { tags })));

            // Карты лежат с зазорами: прямоугольники карт не пересекаются.
            var cardSize = GraphCardSize.FindConfig().CardSize;
            for (var a = 0; a < graph.Slots.Count; a++)
            {
                for (var b = a + 1; b < graph.Slots.Count; b++)
                {
                    var offset = graph.Slots[b].Position - graph.Slots[a].Position;
                    Assert.IsTrue(Mathf.Abs(offset.x) > cardSize.x || Mathf.Abs(offset.z) > cardSize.y, $"{graph.Slots[a]} and {graph.Slots[b]} overlap");
                }
            }
        }

        private LocationModel LocationTestKitGraph(string id, string sceneName, GraphModel graph)
        {
            return GraphTestKit.Location(m_Content, id, sceneName, graph);
        }
    }
}
