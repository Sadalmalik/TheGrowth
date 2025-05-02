using System;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Special converter, allows to use generic Ref<T> struct for optimal entities referencing.
    /// In JSON reference to entity serialize as string "ref:guid"
    /// </summary>
    public class AssetRefConverter : JsonConverter
    {
        private static readonly string Tag = "res";
        private static readonly char[] Separator = { ':' };

        public override bool CanConvert(Type objectType)
        {
            var result = objectType.IsGenericType && typeof(AssetRef<>) == objectType.GetGenericTypeDefinition();
            if (result)
                Debug.Log($"[TEST] AssetRef: {objectType} == {typeof(AssetRef<>)} ? ----- {result}");
            return result;
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            // Make empty AssetRef<T>
            var reference = Activator.CreateInstance(objectType);
            var propertyInfo = objectType.GetProperty(
                AssetRef<ScriptableAsset>.PathPropertyName,
                BindingFlags.Instance | BindingFlags.Public)!;
            propertyInfo.SetValue(reference, null);
            
            if (reader.TokenType == JsonToken.Null)
                return reference;

            if (reader.TokenType != JsonToken.String)
                return reference;

            var enumText = reader.Value?.ToString();
            if (string.IsNullOrEmpty(enumText))
                return reference;

            // Parse string representation
            var parts = enumText.Split(Separator, 2, StringSplitOptions.None);
            if (parts[0] != Tag)
                // TODO: Decide, should we raise error here or not?
                return reference; 
            
            propertyInfo.SetValue(reference, parts[1]);
            Debug.LogWarning($"[TEST] AssetRef parsed: {parts[1]}");
            
            return reference;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            string path = null;
            if (value != null)
            {
                // Get GUID from generic object
                var propertyInfo = value.GetType().GetProperty(
                    AssetRef<ScriptableAsset>.PathPropertyName,
                    BindingFlags.Instance | BindingFlags.Public)!;
                path = (string) propertyInfo.GetValue(value!);
            }

            var result = $"{Tag}:{path}";
            Debug.Log($"Save reference as: {result}");
            writer.WriteValue(result);
        }
    }
}