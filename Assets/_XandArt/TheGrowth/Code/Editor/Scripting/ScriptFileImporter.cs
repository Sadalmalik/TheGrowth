using System.IO;
using System.Text;
using TheGrowth.EditorTools.Content;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TheGrowth.EditorTools.Scripting
{
    /// <summary>
    /// Импорт <c>.script</c> / <c>.dialog</c>: главный объект — <see cref="TextAsset"/> с исходным текстом
    /// (дерево разбирается из текста при загрузке). Текст разбирается и при импорте — только чтобы показать
    /// ошибки с файлом и строкой; импорт при этом успешен, ассет существует. ID конфигов проверяются по
    /// реестру контента; файл зависит от набора ID (<see cref="ContentRegistryBuilder.IdsDependency"/>) и
    /// переимпортируется при его смене.
    /// </summary>
    [ScriptedImporter(2, new[] { "script", "dialog" })]
    public sealed class ScriptFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
            var asset = new TextAsset(text) { name = Path.GetFileNameWithoutExtension(ctx.assetPath) };
            ctx.AddObjectToAsset("text", asset);
            ctx.SetMainObject(asset);

            ctx.DependsOnCustomDependency(ContentRegistryBuilder.IdsDependency);
            var content = ScriptImportValidator.GetContentResolver(ContentRegistryBuilder.RegistryPath, out var warning);
            if (warning != null)
                ctx.LogImportWarning($"{ctx.assetPath}: {warning}", asset);

            foreach (var message in ScriptImportValidator.Validate(ctx.assetPath, text, content))
                ctx.LogImportError(message, asset);
        }
    }
}
