using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Entities;
using TheGrowth.Model;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Graphs;
using TheGrowth.Model.Locations;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Graphs;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Cards
{
#region Trace

    /// <summary>
    /// Журнал тестовых скриптов карт (система сессии): записи команд <c>trace</c> и запуски, в которых они выполнены.
    /// </summary>
    public sealed class CardTrace : SharedObject
    {
        public readonly List<string> Entries = new List<string>();
        public readonly List<ScriptExecution> Executions = new List<ScriptExecution>();

        public void Add(ScriptContext context, string entry)
        {
            Entries.Add(entry);
            Executions.Add(context.Execution);
        }

        public void Clear()
        {
            Entries.Clear();
            Executions.Clear();
        }

        public override string ToString()
        {
            return string.Join(" | ", Entries);
        }

        public static string Name(Entity card)
        {
            return card?.Model.Id ?? "none";
        }

        public static string Name(Slot slot)
        {
            return slot == null ? "none" : slot.Id.Substring(slot.Id.LastIndexOf('/') + 1);
        }
    }

    /// <summary>
    /// <c>trace [card] текст</c> — «ID конфига: текст», и <c> &lt;- ID</c>, если в контексте есть <c>[covering card]</c>
    /// (видно, что реакция не наследует данные вызвавшего скрипта).
    /// </summary>
    [Script("test_trace", "trace [Card] [Text...]")]
    public sealed class TraceCommand : InstantCommand
    {
        public Evaluator<Entity> Card;
        public string Text;

        protected override void Run(ScriptContext context)
        {
            var covering = context.GetOptional<CoveringCard>()?.Card;
            var suffix = covering == null ? string.Empty : " <- " + CardTrace.Name(covering);
            context.Get<CardTrace>().Add(context, $"{CardTrace.Name(Card.Evaluate(context))}: {Text}{suffix}");
        }
    }

    /// <summary><c>trace cards [набор]</c> — «cards: ID,ID» по порядку набора.</summary>
    [Script("test_trace_cards", "trace cards [Cards]")]
    public sealed class TraceCardsCommand : InstantCommand
    {
        public Evaluator<CardSet> Cards;

        protected override void Run(ScriptContext context)
        {
            context.Get<CardTrace>().Add(context, "cards: " + string.Join(",", Cards.Evaluate(context).Select(CardTrace.Name)));
        }
    }

    /// <summary><c>trace slots [набор]</c> — «slots: a1,b2» по порядку набора.</summary>
    [Script("test_trace_slots", "trace slots [Slots]")]
    public sealed class TraceSlotsCommand : InstantCommand
    {
        public Evaluator<SlotSet> Slots;

        protected override void Run(ScriptContext context)
        {
            context.Get<CardTrace>().Add(context, "slots: " + string.Join(",", Slots.Evaluate(context).Select(CardTrace.Name)));
        }
    }

    /// <summary><c>trace slot [слот]</c> — «slot: a1», пусто — «slot: none».</summary>
    [Script("test_trace_slot", "trace slot [Slot]")]
    public sealed class TraceSlotCommand : InstantCommand
    {
        public Evaluator<Slot> Slot;

        protected override void Run(ScriptContext context)
        {
            context.Get<CardTrace>().Add(context, "slot: " + CardTrace.Name(Slot.Evaluate(context)));
        }
    }

#endregion

    /// <summary>
    /// Контент и сессии для тестов скриптов карт без сцены: язык Core + Model (+ <see cref="TraceCommand"/>), теги
    /// (<c>PlayerSpawn</c>, <c>Exit</c>, <c>Hero</c>, <c>Monster</c>, <c>Resource</c>), переменная карты <c>charges</c>, конфиги
    /// примеров контента (<see cref="Hero"/>, <see cref="Monster"/>, <see cref="Herb"/>, <see cref="Exit"/>) с текстами скриптов из
    /// <c>Content/</c> и тестовые конфиги со скриптами из теста (<see cref="Model"/>). Поле — граф 3 × 3, построенный здесь
    /// (не из ассета): клетки <c>a1..c3</c> (строки a, b, c сверху вниз, столбцы 1..3 слева направо, шаг 1 по X и −Z),
    /// связи крестом; <c>a1</c> — <c>PlayerSpawn</c>, <c>c3</c> — <c>Exit</c>; отдельная клетка <c>x</c> без связей.
    /// </summary>
    public sealed class CardTestKit : IDisposable
    {
        public const string ContentFolder = ContentRegistryBuilder.ContentRoot + "/Content";

        private static ScriptRegistry m_Language;

        private readonly List<ContentAsset> m_Assets = new List<ContentAsset>();
        private readonly List<ScriptFileEntry> m_Scripts = new List<ScriptFileEntry>();
        private ContentRegistry m_Registry;
        private LifetimeScope m_Global;

        public CardTestKit()
        {
            Content = new ContentTestKit();
            Graphs = new GraphTestKit();

            Tags = Content.Asset<TagDeclarations>("Tags");
            Tags.SetTags(new[] { "PlayerSpawn", "Exit", "Hero", "Monster", "Resource" }.Select(name => new TagDeclaration(name)));
            Variables = Content.Asset<VariableDeclarations>("Variables");
            Variables.SetVariables(new[] { new VariableDeclaration("charges", VariableScope.Card, VariableType.Int) });
            m_Assets.Add(Tags);
            m_Assets.Add(Variables);

            Hero = Model("Hero", ReadContent("Entities/Hero.script"), "Hero");
            Monster = Model("Monster", ReadContent("Entities/Monster.script"), "Monster");
            Monster.VariableList.Add(new EntityVariable("charges", "3"));
            Herb = Model("Herb", ReadContent("Entities/Herb.script"), "Resource");
            Exit = Model("Exit", ReadContent("Entities/Exit.script"), "Exit");
            Script("Monsters", ReadContent("Scripts/Monsters.script"));
            Alpha = Model("Alpha", TracerScript);
            Beta = Model("Beta", TracerScript);
            Gamma = Model("Gamma", TracerScript);
            Plain = Model("Plain", null);
            Poison = Content.Asset<EntityModel>("Poison");
            EntityModelEditing.SetKind(Poison, EntityKind.Modifier);
            m_Assets.Add(Poison);

            Graph = Graphs.Graph("Field");
            var rows = new[] { "a", "b", "c" };
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                    Graph.SlotList.Add(new GraphSlot($"{rows[row]}{column + 1}", new Vector3(column, 0f, -row)));
            }

            Graph.SlotList.Add(new GraphSlot("x", new Vector3(10f, 0f, 10f)));
            Graph.FindSlot("a1").TagList.Add("PlayerSpawn");
            Graph.FindSlot("c3").TagList.Add("Exit");
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var id = $"{rows[row]}{column + 1}";
                    if (column < 2)
                        Graph.LinkList.Add(new GraphLink($"h{id}", id, $"{rows[row]}{column + 2}"));

                    if (row < 2)
                        Graph.LinkList.Add(new GraphLink($"v{id}", id, $"{rows[row + 1]}{column + 1}"));
                }
            }

            Location = GraphTestKit.Location(Content, "Field", "Field", Graph);
            m_Assets.Add(Location);
        }

        /// <summary>
        /// Скрипт тестовых карт: каждый хук пишет в журнал своё имя (<c>Alpha: OnPlaced</c>, с <c> &lt;- накрывшая</c> в
        /// <c>OnCovered</c> / <c>OnUnCovered</c>).
        /// </summary>
        public static readonly string TracerScript = string.Join("\n", CardHooks.All.Select(hook => $"{hook}:\n    trace [active card] {hook}\n"));

        /// <summary>Язык: команды и узлы Core и Model, расширение языка карт, тестовые команды <c>trace</c>.</summary>
        public static ScriptRegistry Language => m_Language ??= ScriptRegistry.Build(
            ScriptRegistry.CollectTypes(new[] { typeof(Command).Assembly, typeof(CardSystem).Assembly })
                .Concat(new[] { typeof(TraceCommand), typeof(TraceCardsCommand), typeof(TraceSlotsCommand), typeof(TraceSlotCommand) }));

        public ContentTestKit Content { get; }

        public GraphTestKit Graphs { get; }

        public TagDeclarations Tags { get; }

        public VariableDeclarations Variables { get; }

        public EntityModel Hero { get; }

        public EntityModel Monster { get; }

        public EntityModel Herb { get; }

        public EntityModel Exit { get; }

        /// <summary>Тестовые карты с <see cref="TracerScript"/>.</summary>
        public EntityModel Alpha { get; }

        public EntityModel Beta { get; }

        public EntityModel Gamma { get; }

        /// <summary>Карта без файла скрипта.</summary>
        public EntityModel Plain { get; }

        /// <summary>Модификатор (не карта).</summary>
        public EntityModel Poison { get; }

        public GraphModel Graph { get; }

        public LocationModel Location { get; }

        /// <summary>Реестр контента; после первого обращения конфиги и скрипты больше не добавляются.</summary>
        public ContentRegistry Registry => m_Registry ??= Content.Registry(m_Assets, m_Scripts);

        /// <summary>Глобальная область: реестр, язык (<see cref="Language"/>), шина событий, объявления переменных.</summary>
        public LifetimeScope Global
        {
            get
            {
                if (m_Global != null)
                    return m_Global;

                m_Global = new LifetimeScope("Global");
                var container = m_Global.Container;
                container.Add(Registry);
                container.Add(new ScriptLanguage(Language));
                container.Add(new EventBus());
                container.Add(VariableCatalog.FromContent(Registry));
                container.Init();
                return m_Global;
            }
        }

        /// <summary>Конфиг карты (как из пресета) с тегами и файлом скрипта <paramref name="script"/> (null — без файла).</summary>
        public EntityModel Model(string id, string script, params string[] tags)
        {
            if (m_Registry != null)
                throw new InvalidOperationException("The registry is already built");

            var model = Content.Asset<EntityModel>(id);
            EntityModelEditing.SetKind(model, EntityKind.Card);
            model.TagList.AddRange(tags);
            m_Assets.Add(model);
            if (script == null)
                return model;

            Script(id, script);
            model.SetScript(id);
            return model;
        }

        /// <summary>Файл скрипта в реестре.</summary>
        public void Script(string id, string text)
        {
            if (m_Registry != null)
                throw new InvalidOperationException("The registry is already built");

            m_Scripts.Add(Content.Script(id, text));
        }

        /// <summary>Сессия с журналом <see cref="CardTrace"/> и слотами экспедиции на поле <see cref="Location"/>.</summary>
        public Session NewSession()
        {
            var session = Session.Create(Global, new SaveGame(), configure => configure.Container.Add(new CardTrace()));
            session.Get<SlotSystem>().CreateExpedition(Location);
            return session;
        }

        public ScriptParser Parser()
        {
            return new ScriptParser(Language, new ContentScriptResolver(Registry), new VariableScriptResolver(Global.Get<VariableCatalog>()));
        }

        public ScriptPrinter Printer()
        {
            return new ScriptPrinter(Language, new ContentScriptResolver(Registry), new VariableScriptResolver(Global.Get<VariableCatalog>()));
        }

        /// <summary>Текст файла контента проекта (<c>Content/...</c>).</summary>
        public static string ReadContent(string path)
        {
            var full = Path.Combine(ContentFolder, path);
            Assert.IsTrue(File.Exists(full), $"content file '{full}' not found");
            return File.ReadAllText(full);
        }

        public void Dispose()
        {
            m_Global?.Dispose();
            Graphs.Dispose();
            Content.Dispose();
        }
    }

    /// <summary>Сессия теста карт: системы, клетки поля, создание карт без хуков, синхронный запуск.</summary>
    public sealed class CardSession : IDisposable
    {
        public CardSession(CardTestKit kit)
        {
            Kit = kit;
            Session = kit.NewSession();
            Slots = Session.Get<SlotSystem>();
            Cards = Session.Get<CardSystem>();
            Entities = Session.Get<EntityRegistry>();
            Trace = Session.Get<CardTrace>();
        }

        public CardTestKit Kit { get; }

        public Session Session { get; }

        public SlotSystem Slots { get; }

        public CardSystem Cards { get; }

        public EntityRegistry Entities { get; }

        public CardTrace Trace { get; }

        public ScriptRunner Runner => Session.Scripts;

        public Slot Cell(string id)
        {
            return Slots.Expedition.Field.GetCell(id) ?? throw new ArgumentException($"no cell '{id}'");
        }

        /// <summary>Карта в клетке поля через систему слотов — без хуков, как раскладка.</summary>
        public Entity Place(EntityModel model, string cell, bool faceUp = true)
        {
            return Place(model, Cell(cell), faceUp);
        }

        public Entity Place(EntityModel model, Slot slot, bool faceUp = true)
        {
            var card = Slots.Create(model, slot);
            card.GetRequired<CardBrain>().SetFaceUp(faceUp);
            return card;
        }

        public static bool IsFaceUp(Entity card)
        {
            return card.GetRequired<CardBrain>().IsFaceUp;
        }

        public int Charges(Entity card)
        {
            return card.Variables.Get<int>(Session.Get<VariableCatalog>().Get("charges"));
        }

        /// <summary>Задача завершилась синхронно (анимаций нет) без исключения.</summary>
        public static void Complete(UniTask task)
        {
            var probe = Probe.Watch(task);
            Assert.IsTrue(probe.IsCompleted, "the task is not completed synchronously");
            Assert.IsNull(probe.Exception, probe.Exception?.ToString());
        }

        /// <summary>Разбор секции <c>Start</c> из строк (отступ добавляется) — файл <c>test</c>.</summary>
        public ScriptFile Parse(params string[] lines)
        {
            var result = Kit.Parser().Parse("test", ScriptFileKind.Script, "Start:\n" + string.Join("\n", lines.Select(line => "    " + line)) + "\n");
            Assert.IsFalse(result.HasErrors, result.ToString());
            return result.File;
        }

        /// <summary>Запуск секции <c>Start</c> из строк с <c>[active card]</c> (пусто — без активной карты); журнал очищается.</summary>
        public void Run(Entity active, params string[] lines)
        {
            var file = Parse(lines);
            Trace.Clear();
            var context = active == null ? Runner.RootContext : Runner.RootContext.With(new ActiveCard(active));
            Complete(Runner.Run(file, "Start", context));
        }

        /// <summary>Журнал после <see cref="Run"/>.</summary>
        public string[] RunTrace(Entity active, params string[] lines)
        {
            Run(active, lines);
            return Trace.Entries.ToArray();
        }

        /// <summary>Непустые слоты экспедиции: <c>a1:[Hero,Herb] backpack:[Herb]</c> (снизу вверх).</summary>
        public string Describe()
        {
            return string.Join(" ", Slots.Expedition.All
                .Where(slot => !slot.IsEmpty)
                .Select(slot => $"{slot.Id.Substring(slot.Id.LastIndexOf('/') + 1)}:[{string.Join(",", slot.Entities.Select(entity => entity.Model.Id))}]"));
        }

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
