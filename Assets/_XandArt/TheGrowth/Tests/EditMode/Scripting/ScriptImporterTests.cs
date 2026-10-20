using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.EditorTools.Scripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptImporterTests
    {
        private const string TempFolder = "Assets/__T056ImporterTest";

        [Test]
        public void Validate_ReportsErrorsWithPathLineAndColumn()
        {
            var messages = ScriptImportValidator.Validate("Assets/Content/Scripts/Hub.script",
                "Start:\n    goto End\n    jump End\n    log 5\n");

            Assert.AreEqual(2, messages.Count, string.Join("\n", messages));
            Assert.AreEqual("Assets/Content/Scripts/Hub.script(3,5): unknown command 'jump'", messages[0]);
            StringAssert.StartsWith("Assets/Content/Scripts/Hub.script(4,9): cannot parse 'log'", messages[1]);
        }

        [Test]
        public void Validate_PermissiveResolvers_AcceptAnyIdButCheckSyntax()
        {
            // Реестра контента и переменных ещё нет: ID и переменные любые, синтаксис проверяется.
            Assert.IsEmpty(ScriptImportValidator.Validate("Assets/a.script", "S:\n    if $anything\n        log \"a\"\n"));
            Assert.IsNotEmpty(ScriptImportValidator.Validate("Assets/a.script", "S:\n    if $\n"));
            Assert.IsNotEmpty(ScriptImportValidator.Validate("Assets/a.script", "S:\n    if [$anything\n"));
        }

        [Test]
        public void Validate_FileKindFromExtension()
        {
            Assert.IsEmpty(ScriptImportValidator.Validate("Assets/a.dialog", "S:\n    log \"a\"\n"));
            Assert.IsNotEmpty(ScriptImportValidator.Validate("Assets/a.txt", "S:\n"));
        }

        [Test]
        public void Import_ScriptWithError_LogsImportErrorAndKeepsTextAsset()
        {
            const string path = TempFolder + "/Broken.script";
            const string text = "Start:\n    log \"ok\"\n    jump End\n";
            try
            {
                if (!AssetDatabase.IsValidFolder(TempFolder))
                    AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempFolder));

                File.WriteAllText(path, text);

                LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(path + "(3,5): unknown command 'jump'")));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var asset = AssetDatabase.LoadMainAssetAtPath(path) as TextAsset;
                Assert.IsNotNull(asset, "import must produce a TextAsset");
                Assert.AreEqual(text, asset.text);
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempFolder);
                if (Directory.Exists(TempFolder))
                    Directory.Delete(TempFolder, true);

                if (File.Exists(TempFolder + ".meta"))
                    File.Delete(TempFolder + ".meta");
            }
        }
    }
}
