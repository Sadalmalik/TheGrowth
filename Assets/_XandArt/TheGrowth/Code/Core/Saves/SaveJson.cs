using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// JSON сейва: Newtonsoft без <see cref="TypeNameHandling"/> (имена .NET-типов в сейв не попадают), имена полей —
    /// camelCase (ключи словарей не меняются), enum — строкой, даты — ISO 8601 в UTC; списки и объекты DTO при чтении
    /// заменяются, а не дополняются значениями по умолчанию. Текст — UTF-8 без BOM.
    /// </summary>
    internal static class SaveJson
    {
        private const int BufferSize = 4096;

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        // Не меняется после инициализации: настройки только читаются при создании сериализатора.
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false, OverrideSpecifiedNames = false },
            },
            Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateParseHandling = DateParseHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        public static JsonSerializer CreateSerializer()
        {
            return JsonSerializer.Create(Settings);
        }

        public static JToken ToToken(object value)
        {
            return JToken.FromObject(value, CreateSerializer());
        }

        public static T ToObject<T>(JToken token)
        {
            return token.ToObject<T>(CreateSerializer());
        }

        public static void Write(Stream stream, object value)
        {
            using (var writer = CreateWriter(stream))
                CreateSerializer().Serialize(writer, value);
        }

        public static void Write(Stream stream, JToken token)
        {
            using (var writer = CreateWriter(stream))
                token.WriteTo(writer);
        }

        public static T Read<T>(Stream stream)
        {
            using (var reader = CreateReader(stream))
                return CreateSerializer().Deserialize<T>(reader);
        }

        public static JObject ReadObject(Stream stream)
        {
            using (var reader = CreateReader(stream))
                return JObject.Load(reader);
        }

        private static JsonTextWriter CreateWriter(Stream stream)
        {
            var text = new StreamWriter(stream, Utf8, BufferSize, true);
            return new JsonTextWriter(text) { Formatting = Formatting.Indented, CloseOutput = true };
        }

        private static JsonTextReader CreateReader(Stream stream)
        {
            var text = new StreamReader(stream, Utf8, true, BufferSize, true);
            return new JsonTextReader(text) { DateParseHandling = DateParseHandling.None, CloseInput = true };
        }
    }
}
