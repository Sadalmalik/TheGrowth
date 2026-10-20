using System.Text;
using TheGrowth.EditorTools.Content;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TheGrowth.EditorTools.UI
{
    /// <summary>
    /// Статический SDF-шрифт UI: атлас собирается один раз из набора символов (латиница, кириллица, знаки) и при игре
    /// не меняется. Динамический атлас дописывает глифы при каждом Play и меняет ассет в git. Исходник — Cormorant
    /// Garamond из <c>Fonts/</c>. Пересборка создаёт ассет заново (новый GUID) — после неё нужен
    /// <c>[TheGrowth]/Flow/Rebuild Main UI</c>. Запуск: меню или batchmode
    /// <c>-executeMethod TheGrowth.EditorTools.UI.UIFontBuilder.Build</c>.
    /// </summary>
    public static class UIFontBuilder
    {
        public const string SourcePath = ContentRegistryBuilder.ContentRoot + "/Fonts/CormorantGaramond-VariableFont_wght.ttf";
        public const string FontPath = ContentRegistryBuilder.ContentRoot + "/Fonts/CormorantGaramond UI SDF.asset";

        private const string AssetName = "CormorantGaramond UI SDF";
        private const int SamplingPointSize = 72;
        private const int AtlasPadding = 7;
        private const int AtlasSize = 1024;

        /// <summary>Знаки сверх ASCII; отсутствующие в шрифте — предупреждение.</summary>
        private const string Punctuation = " «»©°·–—‘’‚“”„•…№";

        [MenuItem("[TheGrowth]/UI/Rebuild UI Font")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (font == null)
            {
                Debug.LogError($"[UI] Source font not found: {SourcePath}");
                return;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
            if (asset == null)
            {
                Debug.LogError($"[UI] Can't create a font asset from {SourcePath}");
                return;
            }

            asset.name = AssetName;
            asset.TryAddCharacters(RequiredCharacters(), out var missingRequired);
            if (!string.IsNullOrEmpty(missingRequired))
            {
                Debug.LogError($"[UI] {AssetName}: required characters are missing in the font or don't fit the atlas: '{missingRequired}'");
                return;
            }

            asset.TryAddCharacters(Punctuation, out var missingPunctuation);
            if (!string.IsNullOrEmpty(missingPunctuation))
                Debug.LogWarning($"[UI] {AssetName}: characters are missing in the font or don't fit the atlas: '{missingPunctuation}'");

            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            Save(asset);
            Debug.Log($"[UI] {FontPath}: {asset.characterTable.Count} characters, atlas {AtlasSize}x{AtlasSize}");
        }

        private static string RequiredCharacters()
        {
            var builder = new StringBuilder();
            AppendRange(builder, ' ', '~');
            AppendRange(builder, 'А', 'я');
            builder.Append('Ё').Append('ё');
            return builder.ToString();
        }

        private static void AppendRange(StringBuilder builder, char first, char last)
        {
            for (var c = first; c <= last; c++)
                builder.Append(c);
        }

        private static void Save(TMP_FontAsset asset)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(FontPath) != null)
                AssetDatabase.DeleteAsset(FontPath);

            AssetDatabase.CreateAsset(asset, FontPath);

            var texture = asset.atlasTexture;
            texture.name = AssetName + " Atlas";
            AssetDatabase.AddObjectToAsset(texture, asset);

            var material = asset.material;
            material.name = texture.name + " Material";
            AssetDatabase.AddObjectToAsset(material, asset);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
