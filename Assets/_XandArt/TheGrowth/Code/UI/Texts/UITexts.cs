using System;
using System.Collections.Generic;
using System.Globalization;
using TheGrowth.Core.Saves;

namespace TheGrowth.UI.Texts
{
    /// <summary>
    /// Все тексты UI в одном месте (временно, до локализации T039): ключи статических надписей префабов
    /// (<see cref="UIStaticText"/>) и тексты, которые собирают VM (сообщения, описания слотов). Локализация заменит
    /// таблицу и <see cref="Get"/>, ключи останутся.
    /// </summary>
    public static class UITexts
    {
#region Keys

        public const string GameTitle = "game.title";
        public const string MenuContinue = "menu.continue";
        public const string MenuNewGame = "menu.new_game";
        public const string MenuLoad = "menu.load";
        public const string MenuQuit = "menu.quit";
        public const string PauseTitle = "pause.title";
        public const string PauseResume = "pause.resume";
        public const string PauseSave = "pause.save";
        public const string PauseLoad = "pause.load";
        public const string PauseExitToMenu = "pause.exit_to_menu";
        public const string PauseQuit = "pause.quit";
        public const string SavesTitleSave = "saves.title_save";
        public const string SavesTitleLoad = "saves.title_load";
        public const string SavesNew = "saves.new";
        public const string SavesEmpty = "saves.empty";
        public const string SavesBack = "saves.back";
        public const string SlotLoad = "slot.load";
        public const string SlotOverwrite = "slot.overwrite";
        public const string SlotDelete = "slot.delete";
        public const string SlotManual = "slot.manual";
        public const string SlotAuto = "slot.auto";
        public const string SlotBroken = "slot.broken";
        public const string PopupOk = "popup.ok";
        public const string PopupYes = "popup.yes";
        public const string PopupNo = "popup.no";
        public const string ConfirmOverwrite = "confirm.overwrite";
        public const string ConfirmDelete = "confirm.delete";
        public const string ConfirmExitToMenu = "confirm.exit_to_menu";
        public const string ConfirmQuit = "confirm.quit";
        public const string ErrorSave = "error.save";
        public const string ErrorDelete = "error.delete";
        public const string Loading = "loading";

#endregion

        private static readonly Dictionary<string, string> m_Texts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GameTitle] = "Thraumatrope",
            [MenuContinue] = "Продолжить",
            [MenuNewGame] = "Новая игра",
            [MenuLoad] = "Загрузить",
            [MenuQuit] = "Выход",
            [PauseTitle] = "Пауза",
            [PauseResume] = "Продолжить",
            [PauseSave] = "Сохранить",
            [PauseLoad] = "Загрузить",
            [PauseExitToMenu] = "В главное меню",
            [PauseQuit] = "Выход",
            [SavesTitleSave] = "Сохранение",
            [SavesTitleLoad] = "Загрузка",
            [SavesNew] = "Новое сохранение",
            [SavesEmpty] = "Сохранений нет",
            [SavesBack] = "Назад",
            [SlotLoad] = "Загрузить",
            [SlotOverwrite] = "Перезаписать",
            [SlotDelete] = "Удалить",
            [SlotManual] = "Ручное",
            [SlotAuto] = "Автосохранение",
            [SlotBroken] = "Сохранение повреждено",
            [PopupOk] = "OK",
            [PopupYes] = "Да",
            [PopupNo] = "Нет",
            [ConfirmOverwrite] = "Перезаписать сохранение?",
            [ConfirmDelete] = "Удалить сохранение? Это действие нельзя отменить.",
            [ConfirmExitToMenu] = "Выйти в главное меню?",
            [ConfirmQuit] = "Выйти из игры?",
            [ErrorSave] = "Не удалось сохранить игру.",
            [ErrorDelete] = "Не удалось удалить сохранение.",
            [Loading] = "Загрузка…",
        };

        public static IEnumerable<string> Keys => m_Texts.Keys;

        /// <summary>Текст по ключу; нет ключа — сам ключ (видно в игре, что текста нет).</summary>
        public static string Get(string key)
        {
            return key != null && m_Texts.TryGetValue(key, out var text) ? text : key;
        }

        /// <summary>Причина неудачной загрузки для игрока.</summary>
        public static string LoadFailed(SaveErrorKind kind)
        {
            return kind switch
            {
                SaveErrorKind.NewerVersion => "Невозможно загрузиться! Сохранение имеет более высокую версию, чем клиент.",
                SaveErrorKind.NotFound => "Невозможно загрузиться! Сохранение не найдено.",
                SaveErrorKind.Corrupted => "Невозможно загрузиться! Файл сохранения повреждён.",
                SaveErrorKind.MissingMigration => "Невозможно загрузиться! Сохранение несовместимо с этой версией игры.",
                _ => "Невозможно загрузиться! Ошибка при загрузке сохранения.",
            };
        }

        /// <summary>Дата записи слота, местное время.</summary>
        public static string SlotDate(DateTime savedAtUtc)
        {
            return savedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>Время в игре: часы:минуты:секунды.</summary>
        public static string PlayTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0d, seconds));
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
        }

        /// <summary>Строка описания слота: вид · локация · шаг сюжета · время в игре (пустые части пропускаются).</summary>
        public static string SlotDetails(SaveMeta meta)
        {
            var parts = new List<string> { Get(meta.Kind == SaveSlotKind.Auto ? SlotAuto : SlotManual) };
            if (!string.IsNullOrEmpty(meta.Location))
                parts.Add(meta.Location);

            if (!string.IsNullOrEmpty(meta.StoryStep))
                parts.Add(meta.StoryStep);

            parts.Add(PlayTime(meta.PlayTimeSeconds));
            return string.Join(" · ", parts);
        }
    }
}
