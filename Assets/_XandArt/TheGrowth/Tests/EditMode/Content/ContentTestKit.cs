using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.Content
{
#region Commands with config parameters

    [Script("spawn", "spawn [Card]")]
    public sealed class SpawnCardCommand : InstantCommand
    {
        public TestCardModel Card;

        protected override void Run(ScriptContext context)
        {
            context.GetRequired<TraceLog>().Entries.Add("spawn " + Card.Id);
        }
    }

    [Script("summon", "summon [Hero]")]
    public sealed class SummonHeroCommand : InstantCommand
    {
        public TestHeroModel Hero;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("travel", "travel to [Place]")]
    public sealed class TravelCommand : InstantCommand
    {
        public TestLocationModel Place;

        protected override void Run(ScriptContext context)
        {
        }
    }

#endregion

    /// <summary>
    /// Объекты контента в памяти (ассеты, тексты скриптов, реестр) с уничтожением в <see cref="Dispose"/>;
    /// язык — встроенные команды Core, тестовые узлы и команды с параметрами-конфигами.
    /// </summary>
    public sealed class ContentTestKit : IDisposable
    {
        private static ScriptRegistry m_Language;

        private readonly List<Object> m_Objects = new List<Object>();

        public static ScriptRegistry Language => m_Language ??= ScriptRegistry.Build(ScriptLanguageTypes.All().Concat(new[]
        {
            typeof(SpawnCardCommand),
            typeof(SummonHeroCommand),
            typeof(TravelCommand),
        }));

        public T Asset<T>(string id, string name = null) where T : ContentAsset
        {
            var asset = Track(ScriptableObject.CreateInstance<T>());
            asset.name = name ?? id ?? typeof(T).Name;
            asset.SetId(id);
            return asset;
        }

        public ScriptFileEntry Script(string id, string text, ScriptFileKind kind = ScriptFileKind.Script)
        {
            var asset = Track(new TextAsset(text) { name = id });
            return new ScriptFileEntry(id, kind, asset);
        }

        public ContentRegistry Registry(IEnumerable<ContentAsset> assets, IEnumerable<ScriptFileEntry> scripts = null)
        {
            var registry = Track(ScriptableObject.CreateInstance<ContentRegistry>());
            registry.name = "TestRegistry";
            registry.SetContent(assets, scripts);
            return registry;
        }

        /// <summary>Глобальная область: реестр контента и язык, инициализирована.</summary>
        public LifetimeScope Global(ContentRegistry registry)
        {
            var global = new LifetimeScope("Global");
            global.Container.Add(registry);
            global.Container.Add(new ScriptLanguage(Language));
            global.Container.Init();
            return global;
        }

        public void Dispose()
        {
            foreach (var item in m_Objects)
            {
                if (item != null)
                    Object.DestroyImmediate(item);
            }

            m_Objects.Clear();
        }

        private T Track<T>(T item) where T : Object
        {
            m_Objects.Add(item);
            return item;
        }
    }
}
