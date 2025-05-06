using System;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace XandArt.Architecture
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
            return objectType.IsGenericType && typeof(AssetRef<>) == objectType.GetGenericTypeDefinition();
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
                AssetRef<ScriptableAsset>.GuidPropertyName,
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
            
            if (Guid.TryParse(parts[1], out var guid))
                propertyInfo.SetValue(reference, guid);
            
            return reference;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            var guid = Guid.Empty;
            if (value != null)
            {
                // Get GUID from generic object
                var propertyInfo = value.GetType().GetProperty(
                    Ref<Entity>.GuidPropertyName,
                    BindingFlags.Instance | BindingFlags.Public)!;
                guid = (Guid)propertyInfo.GetValue(value!);
            }

            writer.WriteValue($"{Tag}:{guid}");
        }
    }
}