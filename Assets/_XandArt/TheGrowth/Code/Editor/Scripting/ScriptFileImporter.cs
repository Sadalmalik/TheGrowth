using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TheGrowth.EditorTools.Scripting
{
    /// <summary>
    /// Импорт <c>.script</c> / <c>.dialog</c>: главный объект — <see cref="TextAsset"/> с исходным текстом
    /// (дерево разбирается из текста при загрузке). Текст разбирается и при импорте — только чтобы показать
    /// ошибки с файлом и строкой; импорт при этом успешен, ассет существует.
    /// </summary>
    [ScriptedImporter(1, new[] { "script", "dialog" })]
    public sealed class ScriptFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
            var asset = new TextAsset(text) { name = Path.GetFileNameWithoutExtension(ctx.assetPath) };
            ctx.AddObjectToAsset("text", asset);
            ctx.SetMainObject(asset);

            foreach (var message in ScriptImportValidator.Validate(ctx.assetPath, text))
                ctx.LogImportError(message, asset);
        }
    }
}
