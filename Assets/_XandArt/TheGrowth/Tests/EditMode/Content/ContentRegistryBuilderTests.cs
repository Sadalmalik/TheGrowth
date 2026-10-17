using System.IO;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Scripting;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Content
{
    /// <summary>
    /// Сборка реестра из ассетов на диске. Временная папка — вне папки контента проекта, поэтому
    /// автопересборка реестра проекта (<see cref="ContentAssetPostprocessor"/>) её не видит.
    /// </summary>
    public sealed class ContentRegistryBuilderTests
    {
        private const string TempFolder = "Assets/__T060ContentTest";
        private const string ContentFolder = TempFolder + "/Content";
        private const string OtherFolder = TempFolder + "/Other";
        private const string RegistryPath = TempFolder + "/Registry/TestRegistry.asset";

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder();
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));
            AssetDatabase.CreateFolder(TempFolder, "Content");
            AssetDatabase.CreateFolder(TempFolder, "Other");
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTempFolder();
        }

        [Test]
        public void Rebuild_CollectsContentAndScripts_FillsEmptyIdFromAssetName()
        {
            var monster = CreateAsset<TestCardModel>(ContentFolder + "/Monster One.asset", null);
            var hero = CreateAsset<TestHeroModel>(ContentFolder + "/Hero.asset", "lucia");
            CreateAsset<TestLocationModel>(OtherFolder + "/Forest.asset", "forest");
            WriteScript(ContentFolder + "/Hub.script", "Start:\n    log \"a\"\n");
            WriteScript(ContentFolder + "/Lucia_intro.dialog", "Start:\n    log \"b\"\n");

            var registry = ContentRegistryBuilder.Rebuild(ContentFolder, RegistryPath);

            Assert.AreSame(registry, AssetDatabase.LoadAssetAtPath<ContentRegistry>(RegistryPath), "registry is saved as an asset");
            Assert.AreEqual("Monster_One", monster.Id, "empty id is filled from the file name");
            Assert.That(registry.Assets, Is.EqualTo(new ContentAsset[] { monster, hero }), "sorted by id scope, then id (ordinal)");
            Assert.AreSame(hero, registry.Get<TestCardModel>("lucia"));

            Assert.That(registry.Scripts.Select(script => script.Id), Is.EqualTo(new[] { "Hub", "Lucia_intro" }));
            Assert.IsTrue(registry.TryGetScript("Lucia_intro", out var dialog));
            Assert.AreEqual(ScriptFileKind.Dialog, dialog.Kind);
            Assert.AreEqual("Start:\n    log \"b\"\n", dialog.Text.text);
        }

        [Test]
        public void Rebuild_RenameKeepsId_NewAssetUpdatesRegistry()
        {
            var monster = CreateAsset<TestCardModel>(ContentFolder + "/Monster.asset", "monster_1");
            var registry = ContentRegistryBuilder.Rebuild(ContentFolder, RegistryPath);

            Assert.IsEmpty(AssetDatabase.RenameAsset(ContentFolder + "/Monster.asset", "Wolf"));
            var bear = CreateAsset<TestCardModel>(ContentFolder + "/Bear.asset", "bear");
            var rebuilt = ContentRegistryBuilder.Rebuild(ContentFolder, RegistryPath);

            Assert.AreSame(registry, rebuilt);
            Assert.AreEqual("monster_1", monster.Id);
            Assert.AreEqual(ContentFolder + "/Wolf.asset", AssetDatabase.GetAssetPath(monster));
            Assert.That(rebuilt.Assets, Is.EqualTo(new ContentAsset[] { bear, monster }));
        }

        [Test]
        public void ValidateProject_ReportsContentOutsideRegistry_WithPaths()
        {
            CreateAsset<TestCardModel>(ContentFolder + "/Monster.asset", "monster_1");
            CreateAsset<TestCardModel>(ContentFolder + "/Copy.asset", "monster_1");
            CreateAsset<TestLocationModel>(OtherFolder + "/Forest.asset", "forest");
            WriteScript(OtherFolder + "/Stray.script", "Start:\n    log \"c\"\n");

            var registry = ContentRegistryBuilder.Rebuild(ContentFolder, RegistryPath);
            var errors = ContentValidator.ValidateProject(registry, TempFolder);

            Assert.AreEqual(4, errors.Count, string.Join("\n", errors));
            Assert.That(errors.Where(error => error.Kind == ContentErrorKind.DuplicateId).Select(error => error.Path),
                Is.EquivalentTo(new[] { ContentFolder + "/Copy.asset", ContentFolder + "/Monster.asset" }));
            Assert.That(errors.Where(error => error.Kind == ContentErrorKind.NotInRegistry).Select(error => error.Path),
                Is.EquivalentTo(new[] { OtherFolder + "/Forest.asset", OtherFolder + "/Stray.script" }));
        }

        [Test]
        public void ImporterResolver_UsesRegistryAsset_FallsBackWithWarningWhenMissing()
        {
            var monster = CreateAsset<TestCardModel>(ContentFolder + "/Monster.asset", "monster_1");
            ContentRegistryBuilder.Rebuild(ContentFolder, RegistryPath);

            var content = ScriptImportValidator.GetContentResolver(RegistryPath, out var warning);
            Assert.IsNull(warning);
            Assert.IsInstanceOf<ContentScriptResolver>(content);
            Assert.IsTrue(content.TryResolve(typeof(TestCardModel), "monster_1", out var value));
            Assert.AreSame(monster, value);
            Assert.IsFalse(content.TryResolve(typeof(TestCardModel), "nope", out _));

            var fallback = ScriptImportValidator.GetContentResolver(TempFolder + "/Missing.asset", out warning);
            Assert.IsInstanceOf<PermissiveContentResolver>(fallback);
            StringAssert.Contains("not found", warning);
        }

#region Helpers

        private static T CreateAsset<T>(string path, string id) where T : ContentAsset
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.SetId(id);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void WriteScript(string path, string text)
        {
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            if (Directory.Exists(TempFolder))
                Directory.Delete(TempFolder, true);

            if (File.Exists(TempFolder + ".meta"))
                File.Delete(TempFolder + ".meta");
        }

#endregion
    }
}
