using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Model;

namespace TheGrowth.Tests.EditMode.Saves
{
#region Saveable probes

    public sealed class ProbeSave
    {
        public string Value;
    }

    /// <summary>
    /// Сохраняемая система-проба без базового класса: журнал init / read / ready. Контейнер регистрирует объект по типу,
    /// поэтому для нескольких проб в одной сессии — разные наследники.
    /// </summary>
    public abstract class ProbeSaveable : SharedObject, ISaveable, ISessionReady
    {
        private readonly List<string> m_Journal;

        protected ProbeSaveable(string key, int loadOrder, List<string> journal)
        {
            SaveKey = key;
            LoadOrder = loadOrder;
            m_Journal = journal;
        }

        public string SaveKey { get; }

        public int SaveVersion => 1;

        public int LoadOrder { get; }

        public string Value { get; set; }

        public override void Init()
        {
            m_Journal.Add($"{SaveKey}:init");
        }

        public object Write()
        {
            return new ProbeSave { Value = Value };
        }

        public void Read(SaveSection section)
        {
            Value = section == null ? "default" : section.Get<ProbeSave>(SaveVersion).Value;
            m_Journal.Add($"{SaveKey}:read:{Value}");
        }

        public void OnSessionReady()
        {
            m_Journal.Add($"{SaveKey}:ready");
        }
    }

    public sealed class FirstProbe : ProbeSaveable
    {
        public FirstProbe(string key, int loadOrder, List<string> journal) : base(key, loadOrder, journal)
        {
        }
    }

    public sealed class SecondProbe : ProbeSaveable
    {
        public SecondProbe(string key, int loadOrder, List<string> journal) : base(key, loadOrder, journal)
        {
        }
    }

    public sealed class ThirdProbe : ProbeSaveable
    {
        public ThirdProbe(string key, int loadOrder, List<string> journal) : base(key, loadOrder, journal)
        {
        }
    }

    /// <summary>DTO <see cref="CounterSystem"/>, версия 3.</summary>
    public sealed class CounterSave
    {
        public int Value;
        public List<string> Tags = new List<string>();
    }

    /// <summary>
    /// Система с историей схемы: v1 <c>{ count }</c> → v2 <c>{ value }</c> (переименование) → v3 <c>{ value, tags }</c>
    /// (новое поле со значением «migrated»). Журнал применённых шагов — <see cref="Steps"/>.
    /// </summary>
    public sealed class CounterSystem : SaveableSystem<CounterSave>
    {
        public const string Key = "counter";
        public const int Version = 3;

        public readonly List<string> Steps = new List<string>();

        public override string SaveKey => Key;

        public override int SaveVersion => Version;

        public int Value { get; set; }

        public List<string> Tags { get; private set; } = new List<string>();

        public bool ReadDefaultsCalled { get; private set; }

        protected override CounterSave Write()
        {
            return new CounterSave { Value = Value, Tags = new List<string>(Tags) };
        }

        protected override void Read(CounterSave save)
        {
            Value = save.Value;
            Tags = new List<string>(save.Tags);
        }

        protected override void ReadDefaults()
        {
            ReadDefaultsCalled = true;
        }

        protected override void RegisterMigrations(SaveMigrations migrations)
        {
            migrations.Add(1, MigrateFrom1).Add(2, MigrateFrom2);
        }

        private JObject MigrateFrom1(JObject data)
        {
            Steps.Add("1->2");
            data["value"] = data["count"];
            data.Remove("count");
            return data;
        }

        private JObject MigrateFrom2(JObject data)
        {
            Steps.Add("2->3");
            data["tags"] = new JArray("migrated");
            return data;
        }
    }

#endregion

    /// <summary>Упаковка и фикстуры сейвов в памяти; синхронное ожидание задач хранилищ в памяти.</summary>
    public static class SaveTestKit
    {
        /// <summary>meta.json, как его записала игра версии 0.9 (формат 1).</summary>
        public const string MetaV1 = "{\n  \"formatVersion\": 1,\n  \"gameVersion\": \"0.9.0\",\n  \"savedAtUtc\": \"2026-01-02T03:04:05Z\",\n"
            + "  \"playTimeSeconds\": 3600.5,\n  \"storyStep\": null,\n  \"location\": \"hub\",\n  \"kind\": \"manual\"\n}";

        /// <summary>Секция counter версии 1 (поле ещё называлось count).</summary>
        public const string CounterV1 = "{\n  \"version\": 1,\n  \"data\": {\n    \"count\": 5\n  }\n}";

        /// <summary>Секция locations версии 1.</summary>
        public const string LocationsV1 = "{\n  \"version\": 1,\n  \"data\": {\n    \"current\": \"hub\"\n  }\n}";

        /// <summary>Архив из готовых текстов записей — как файл, записанный старой версией игры (не через <see cref="SaveArchive"/>).</summary>
        public static byte[] Zip(params (string Name, string Text)[] entries)
        {
            using (var buffer = new MemoryStream())
            {
                using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
                {
                    foreach (var (name, text) in entries)
                    {
                        using (var stream = archive.CreateEntry(name).Open())
                        {
                            var bytes = new UTF8Encoding(false).GetBytes(text);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }

                return buffer.ToArray();
            }
        }

        public static byte[] Pack(SaveGame save)
        {
            using (var buffer = new MemoryStream())
            {
                SaveArchive.Write(save, buffer);
                return buffer.ToArray();
            }
        }

        public static SaveGame Unpack(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes, false))
                return SaveArchive.Read(stream);
        }

        public static string ReadEntry(byte[] archiveBytes, string name)
        {
            using (var archive = new ZipArchive(new MemoryStream(archiveBytes, false), ZipArchiveMode.Read))
            using (var reader = new StreamReader(archive.GetEntry(name).Open(), Encoding.UTF8))
                return reader.ReadToEnd();
        }

        /// <summary>Результат задачи, которая должна была завершиться синхронно (хранилище в памяти без ворот).</summary>
        public static T Sync<T>(UniTask<T> task)
        {
            if (!task.Status.IsCompleted())
                Assert.Fail("The task is expected to complete synchronously");

            return task.GetAwaiter().GetResult();
        }

        public static void Sync(UniTask task)
        {
            if (!task.Status.IsCompleted())
                Assert.Fail("The task is expected to complete synchronously");

            task.GetAwaiter().GetResult();
        }
    }
}
