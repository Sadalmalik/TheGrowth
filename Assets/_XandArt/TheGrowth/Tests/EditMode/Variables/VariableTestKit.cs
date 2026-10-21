using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Profile;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Scripting;

namespace TheGrowth.Tests.EditMode.Variables
{
#region Owner

    /// <summary>Тестовый владелец переменных карты; он же данные контекста — активный владелец.</summary>
    public sealed class TestVariableOwner : IVariableOwner, IContextData
    {
        public TestVariableOwner(string name, EventBus events = null)
        {
            Name = name;
            Variables = new VariableStore(VariableScope.Card, events, this);
        }

        public string Name { get; }

        public VariableStore Variables { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>Эвалуатор владельца: активный владелец из контекста.</summary>
    [Script("active_owner", "active owner")]
    public sealed class ActiveOwnerEvaluator : Evaluator<TestVariableOwner>
    {
        public override TestVariableOwner Evaluate(ScriptContext context)
        {
            return context.GetRequired<TestVariableOwner>();
        }
    }

    /// <summary>Эвалуатор не-владельца: <c>[x].имя</c> с ним не разбирается.</summary>
    [Script("some_text", "some text")]
    public sealed class SomeTextEvaluator : Evaluator<string>
    {
        public override string Evaluate(ScriptContext context)
        {
            return "text";
        }
    }

#endregion

#region Profile storage

    /// <summary>Профиль в памяти: текст JSON (null — профиля нет), счётчик записей; <see cref="FailWrites"/> — ошибка записи.</summary>
    public sealed class MemoryProfileStorage : SharedObject, IProfileStorage
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public string Text { get; set; }

        public int WriteCount { get; private set; }

        public bool FailWrites { get; set; }

        public bool FailCopies { get; set; }

        /// <summary>Копии профиля (<see cref="KeepCopy"/>) по порядку.</summary>
        public List<string> Copies { get; } = new List<string>();

        public Stream OpenRead()
        {
            return Text == null ? null : new MemoryStream(Utf8.GetBytes(Text), false);
        }

        public void Write(Action<Stream> write)
        {
            using (var buffer = new MemoryStream())
            {
                write(buffer);
                if (FailWrites)
                    throw new IOException("disk full");

                Text = Utf8.GetString(buffer.ToArray());
                WriteCount++;
            }
        }

        public string KeepCopy(string tag)
        {
            if (Text == null)
                return null;

            if (FailCopies)
                throw new IOException("access denied");

            Copies.Add(Text);
            return "memory." + tag + "-" + Copies.Count;
        }
    }

#endregion

    /// <summary>
    /// Объявления переменных для тестов, язык с владельцем переменных (<see cref="ActiveOwnerEvaluator"/>) и глобальная
    /// область для сессии: реестр, язык, шина, объявления, профиль в памяти.
    /// </summary>
    public static class VariableTestKit
    {
        private static ScriptRegistry m_Language;

        /// <summary>Встроенные узлы Core, тестовые узлы языка и владелец переменных.</summary>
        public static ScriptRegistry Language => m_Language ??= ScriptRegistry.Build(ScriptLanguageTypes.All().Concat(new[]
        {
            typeof(ActiveOwnerEvaluator),
            typeof(SomeTextEvaluator),
        }));

        /// <summary>
        /// <c>trust_lucia</c>, <c>coins</c> (int), <c>met_lucia</c> (bool), <c>player_name</c> (string, «Player»), <c>gold</c>
        /// (int, прежнее имя <c>money</c>) — игра; <c>ending_seen</c> (bool) — профиль; <c>charges</c> (int, 2) — карта.
        /// </summary>
        public static VariableDeclaration[] Standard()
        {
            return new[]
            {
                new VariableDeclaration("trust_lucia", VariableScope.Game, VariableType.Int, "0", "Lucia's trust"),
                new VariableDeclaration("coins", VariableScope.Game, VariableType.Int),
                new VariableDeclaration("met_lucia", VariableScope.Game, VariableType.Bool, "false"),
                new VariableDeclaration("player_name", VariableScope.Game, VariableType.String, "Player"),
                new VariableDeclaration("gold", VariableScope.Game, VariableType.Int, null, null, "money"),
                new VariableDeclaration("ending_seen", VariableScope.Profile, VariableType.Bool),
                new VariableDeclaration("charges", VariableScope.Card, VariableType.Int, "2"),
            };
        }

        public static VariableDeclarations Declarations(ContentTestKit kit, string id, params VariableDeclaration[] variables)
        {
            var asset = kit.Asset<VariableDeclarations>(id);
            asset.SetVariables(variables);
            return asset;
        }

        public static VariableCatalog Catalog(ContentTestKit kit, params VariableDeclaration[] variables)
        {
            return new VariableCatalog(new[] { Declarations(kit, "Variables", variables) });
        }

        /// <summary>
        /// Глобальная область: реестр, язык <see cref="Language"/>, шина, объявления из реестра, профиль в памяти
        /// (<paramref name="profile"/>, null — новый пустой); инициализирована.
        /// </summary>
        public static LifetimeScope Global(ContentRegistry registry, MemoryProfileStorage profile = null)
        {
            var global = new LifetimeScope("Global");
            var container = global.Container;
            container.Add(registry);
            container.Add(new ScriptLanguage(Language));
            container.Add(new EventBus());
            container.Add(VariableCatalog.FromContent(registry));
            container.Add(profile ?? new MemoryProfileStorage());
            container.Add(new ProfileService());
            container.Init();
            return global;
        }

        /// <summary>События изменения переменных из шины, до <see cref="IDisposable.Dispose"/>.</summary>
        public static IDisposable Record(EventBus events, List<VariableChanged> changes)
        {
            Action<VariableChanged> handler = changes.Add;
            events.Subscribe(handler);
            return new Subscription(() => events.Unsubscribe(handler));
        }

        private sealed class Subscription : IDisposable
        {
            private Action m_Dispose;

            public Subscription(Action dispose)
            {
                m_Dispose = dispose;
            }

            public void Dispose()
            {
                m_Dispose?.Invoke();
                m_Dispose = null;
            }
        }
    }
}
