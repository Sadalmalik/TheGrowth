using System;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Упаковка слота в zip: <c>meta.json</c>, по файлу <c>&lt;key&gt;.json</c> на секцию, <c>thumbnail.png</c>.
    /// <see cref="ReadMeta"/> читает только центральный каталог архива и запись <c>meta.json</c> (поток с поиском) —
    /// список слотов не читает и не разбирает секции. Ошибки формата — <see cref="SaveException"/>.
    /// </summary>
    public static class SaveArchive
    {
        /// <summary>Версия формата архива; архив новее — ошибка чтения.</summary>
        public const int FormatVersion = 1;

        internal const string MetaKey = "meta";

        private const string MetaEntry = MetaKey + ".json";
        private const string ThumbnailEntry = "thumbnail.png";
        private const string SectionExtension = ".json";
        private const string VersionField = "version";
        private const string DataField = "data";

        /// <summary>Записать архив в поток (поток остаётся открытым). <see cref="SaveMeta.FormatVersion"/> ставится здесь.</summary>
        public static void Write(SaveGame save, Stream stream)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            save.Meta.FormatVersion = FormatVersion;
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                using (var entry = archive.CreateEntry(MetaEntry, CompressionLevel.Optimal).Open())
                    SaveJson.Write(entry, save.Meta);

                foreach (var section in save.Sections)
                {
                    var root = new JObject { [VersionField] = section.Version, [DataField] = section.Data };
                    using (var entry = archive.CreateEntry(section.Key + SectionExtension, CompressionLevel.Optimal).Open())
                        SaveJson.Write(entry, root);
                }

                if (save.Thumbnail is { Length: > 0 })
                {
                    using (var entry = archive.CreateEntry(ThumbnailEntry, CompressionLevel.NoCompression).Open())
                        entry.Write(save.Thumbnail, 0, save.Thumbnail.Length);
                }
            }
        }

        /// <summary>Только описание слота: центральный каталог и <c>meta.json</c>, без секций и превью.</summary>
        public static SaveMeta ReadMeta(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
                    return ReadMeta(archive);
            }
            catch (Exception exception) when (IsFormatError(exception))
            {
                throw new SaveException($"Save archive is broken: {exception.Message}", exception);
            }
        }

        /// <summary>Весь слот с проверкой: описание, все секции (версия и данные-объект), превью. Неизвестные записи пропускаются.</summary>
        public static SaveGame Read(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
                {
                    var save = new SaveGame(ReadMeta(archive));
                    foreach (var entry in archive.Entries)
                    {
                        var name = entry.FullName;
                        if (name == MetaEntry)
                            continue;

                        if (name == ThumbnailEntry)
                            save.Thumbnail = ReadBytes(entry);
                        else if (name.EndsWith(SectionExtension, StringComparison.Ordinal))
                            save.AddSection(ReadSection(entry, name.Substring(0, name.Length - SectionExtension.Length)));
                    }

                    return save;
                }
            }
            catch (Exception exception) when (IsFormatError(exception))
            {
                throw new SaveException($"Save archive is broken: {exception.Message}", exception);
            }
        }

        private static SaveMeta ReadMeta(ZipArchive archive)
        {
            var entry = archive.GetEntry(MetaEntry) ?? throw new SaveException($"Save archive has no {MetaEntry}");

            SaveMeta meta;
            using (var stream = entry.Open())
                meta = SaveJson.Read<SaveMeta>(stream);

            if (meta == null)
                throw new SaveException($"{MetaEntry} is empty");

            if (meta.FormatVersion > FormatVersion)
                throw new SaveException($"Save format version {meta.FormatVersion} is newer than supported {FormatVersion}: the save is from a newer game version");

            if (meta.FormatVersion < 1)
                throw new SaveException($"{MetaEntry} has invalid format version {meta.FormatVersion}");

            return meta;
        }

        private static SaveSection ReadSection(ZipArchiveEntry entry, string key)
        {
            JObject root;
            using (var stream = entry.Open())
                root = SaveJson.ReadObject(stream);

            if (root[VersionField] is not { Type: JTokenType.Integer } version)
                throw new SaveException($"Section '{entry.FullName}' has no integer '{VersionField}'");

            if (root[DataField] is not JObject data)
                throw new SaveException($"Section '{entry.FullName}' has no object '{DataField}'");

            return new SaveSection(key, (int)version, data);
        }

        private static byte[] ReadBytes(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        private static bool IsFormatError(Exception exception)
        {
            return exception is InvalidDataException or JsonException or IOException or ArgumentException or NotSupportedException or OverflowException;
        }
    }
}
