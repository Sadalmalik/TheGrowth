using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Rng;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using TheGrowth.Model;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Locations;
using TheGrowth.Model.Slots;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.EditorTools.Decks
{
    /// <summary>
    /// Проверки колод и раскладки локаций для валидатора контента:
    /// <list type="bullet">
    /// <item>строки колоды — конфиг есть, количество не меньше 1 (вид конфига — <c>ModelKindChecks</c>);</item>
    /// <item>локация с колодой — у неё есть поле, в графе есть клетка <see cref="DealSystem.PlayerSpawnTag"/>;</item>
    /// <item><c>SpawnSlot</c> карт колоды — первый шаг раскладки на пустом поле в сессии без сцены: каждая карта строк без
    /// «не раскладывать» по порядку строк вычисляет набор и занимает первую свободную клетку из него (клетка героя — первая
    /// <c>PlayerSpawn</c> — занята); карты без свободной клетки — предупреждение. Случайный выбор раскладки здесь заменён
    /// первым слотом: для наборов по тегам число разложенных карт от выбора не зависит.</item>
    /// </list>
    /// </summary>
    public static class DeckChecks
    {
        /// <summary>Строки колоды: конфиг есть, количество не меньше 1.</summary>
        public static List<string> CheckDeck(DeckModel deck)
        {
            if (deck == null)
                throw new ArgumentNullException(nameof(deck));

            var errors = new List<string>();
            for (var i = 0; i < deck.Entries.Count; i++)
            {
                var entry = deck.Entries[i];
                if (entry?.Model == null)
                    errors.Add($"row {i}: no card config");
                else if (entry.Count < 1)
                    errors.Add($"row {i} ('{entry.Model.Id}'): count is {entry.Count}, at least 1 is expected");
            }

            return errors;
        }

        /// <summary>Локация с колодой: есть поле и в нём клетка для героя.</summary>
        public static List<string> CheckField(LocationModel location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            var errors = new List<string>();
            if (location.Deck == null)
                return errors;

            if (location.Graph == null)
            {
                errors.Add($"the location has deck '{location.Deck.Id}' but no field graph");
                return errors;
            }

            if (!location.Graph.Slots.Any(slot => slot != null && slot.HasTag(DealSystem.PlayerSpawnTag)))
                errors.Add($"field graph '{location.Graph.name}' has no slot tagged {DealSystem.PlayerSpawnTag}: the hero can't be dealt");

            return errors;
        }

        /// <summary>
        /// Предупреждения <c>SpawnSlot</c> для локаций с колодой и полем (<see cref="DeckChecks"/>). Контент должен быть без
        /// ошибок: сессия без сцены строится из него, и ошибки объявлений иначе попали бы в лог повторно. Файл скрипта с
        /// ошибками разбора пропускается (их показывает импорт).
        /// </summary>
        /// <param name="language">Язык скриптов игры; null — проверки нет.</param>
        public static List<(LocationModel Location, string Message)> CheckSpawnSlots(IReadOnlyList<ContentAsset> assets,
            IReadOnlyList<ScriptFileEntry> scripts, ScriptRegistry language)
        {
            var warnings = new List<(LocationModel, string)>();
            var locations = assets.OfType<LocationModel>().Where(location => location.Deck != null && location.Graph != null).ToList();
            if (language == null || locations.Count == 0)
                return warnings;

            var registry = ScriptableObject.CreateInstance<ContentRegistry>();
            registry.hideFlags = HideFlags.HideAndDontSave;
            registry.SetContent(assets.Where(asset => asset != null), scripts);
            var global = new LifetimeScope("DeckChecks");
            try
            {
                var catalog = VariableCatalog.FromContent(registry);
                global.Container.Add(registry);
                global.Container.Add(new ScriptLanguage(language));
                global.Container.Add(new EventBus());
                global.Container.Add(catalog);
                global.Container.Init();
                var parser = new ScriptParser(language, new ContentScriptResolver(registry), new VariableScriptResolver(catalog));
                var files = new Dictionary<string, ScriptFile>(StringComparer.Ordinal);
                foreach (var location in locations)
                {
                    foreach (var message in CheckSpawnSlots(location, global, registry, parser, files))
                        warnings.Add((location, message));
                }
            }
            catch (Exception exception)
            {
                warnings.Add((locations[0], $"{CardProperties.SpawnSlot} of the deck cards is not checked: {exception.Message}"));
            }
            finally
            {
                global.Dispose();
                Object.DestroyImmediate(registry);
            }

            return warnings;
        }

        private static List<string> CheckSpawnSlots(LocationModel location, LifetimeScope global, IContentRegistry registry, ScriptParser parser,
            Dictionary<string, ScriptFile> files)
        {
            var warnings = new List<string>();
            var session = Session.Create(global, new SaveGame(), created => created.Get<RngSystem>().NewGameSeed = 0);
            try
            {
                var slots = session.Get<SlotSystem>();
                var expedition = slots.CreateExpedition(location);
                var cells = new HashSet<Slot>(expedition.Field.Cells);
                var heroCell = expedition.Field.Cells.FirstOrDefault(cell => cell.HasTag(DealSystem.PlayerSpawnTag));
                foreach (var entry in location.Deck.Entries)
                {
                    if (entry?.Model is not { Kind: EntityKind.Card } || entry.Count < 1 || entry.KeepInDeck)
                        continue;

                    if (!TryGetSpawnSlot(entry.Model, registry, parser, files, out var spawn))
                        continue;

                    var left = 0;
                    for (var i = 0; i < entry.Count; i++)
                    {
                        var card = slots.Create(entry.Model, expedition.Deck);
                        SlotSet set;
                        try
                        {
                            set = spawn.Evaluate(session.Scripts.RootContext.With(new ActiveCard(card)));
                        }
                        catch (Exception exception)
                        {
                            warnings.Add($"{CardProperties.SpawnSlot} of '{entry.Model.Id}' fails on the empty field: {exception.Message}");
                            left = -1;
                            break;
                        }

                        var free = set.FirstOrDefault(slot => slot != heroCell && slot.IsEmpty && cells.Contains(slot));
                        if (free == null)
                            left++;
                        else
                            slots.Move(card, free);
                    }

                    if (left == entry.Count)
                        warnings.Add($"{CardProperties.SpawnSlot} of '{entry.Model.Id}' has no free field slot on the empty field of deck '{location.Deck.Id}': its {entry.Count} card(s) stay in the deck");
                    else if (left > 0)
                        warnings.Add($"{CardProperties.SpawnSlot} of '{entry.Model.Id}' has free field slots for {entry.Count - left} of {entry.Count} card(s) of deck '{location.Deck.Id}': {left} stay in the deck");
                }
            }
            finally
            {
                session.Dispose();
            }

            return warnings;
        }

        /// <summary>Выражение <c>SpawnSlot</c> из файла скрипта конфига; файла нет, он с ошибками или свойства нет — false.</summary>
        private static bool TryGetSpawnSlot(EntityModel model, IContentRegistry registry, ScriptParser parser, Dictionary<string, ScriptFile> files,
            out Evaluator<SlotSet> spawn)
        {
            spawn = null;
            var id = model.ScriptId;
            if (string.IsNullOrEmpty(id))
                return false;

            if (!files.TryGetValue(id, out var file))
            {
                file = registry.TryGetScript(id, out var entry) && entry.Text != null ? Parse(parser, entry) : null;
                files.Add(id, file);
            }

            if (file == null || !file.TryGetProperty(CardProperties.SpawnSlot, out var property))
                return false;

            spawn = property.Value as Evaluator<SlotSet>;
            return spawn != null;
        }

        private static ScriptFile Parse(ScriptParser parser, ScriptFileEntry entry)
        {
            var result = parser.Parse(entry.Id, entry.Kind, entry.Text.text);
            return result.HasErrors ? null : result.File;
        }
    }
}
