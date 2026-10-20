using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Слоты сохранений (глобальная область): список, сохранить, загрузить, удалить — через <see cref="ISaveStorage"/>.
    /// Ручные слоты без лимита (ID — по времени записи), автосейвы — <see cref="AutosaveSlotCount"/> слота по кругу:
    /// перезаписывается самый старый по дате в meta, пустой или повреждённый — первым. Операции выполняются по одной в
    /// порядке вызова (запись автосейва не пересекается с записью другого слота и чтением списка). Когда сохранять,
    /// решает <c>GameFlow</c> (точки сохранения); сервис только пишет и читает.
    /// </summary>
    public sealed class SaveService : SharedObject
    {
        public const int AutosaveSlotCount = 3;
        public const string AutosavePrefix = "auto_";
        public const string ManualPrefix = "manual_";

        private static readonly Regex SlotIdPattern = new Regex("^[a-z0-9_-]+$", RegexOptions.CultureInvariant);

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private ISaveStorage m_Storage;
#pragma warning restore CS0649

        private readonly Func<DateTime> m_UtcNow;
        private readonly Queue<UniTaskCompletionSource> m_Waiting = new Queue<UniTaskCompletionSource>();
        private bool m_IsBusy;

        /// <param name="utcNow">Часы для даты записи (тесты); по умолчанию <see cref="DateTime.UtcNow"/>.</param>
        public SaveService(Func<DateTime> utcNow = null)
        {
            m_UtcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>ID слота — имя файла: латиница в нижнем регистре, цифры, «_», «-».</summary>
        public static bool IsValidSlotId(string slotId)
        {
            return slotId != null && SlotIdPattern.IsMatch(slotId);
        }

        /// <summary>ID автосейва по номеру от 0: <c>auto_1</c>, <c>auto_2</c>, ...</summary>
        public static string GetAutosaveSlotId(int index)
        {
            return AutosavePrefix + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Слоты по дате записи, новые первыми; повреждённые — в конце. У каждого слота читается только meta.json.</summary>
        public async UniTask<IReadOnlyList<SaveSlotInfo>> ListAsync(CancellationToken token = default)
        {
            using (await LockAsync())
            {
                var ids = await m_Storage.ListAsync(token);
                var slots = new List<SaveSlotInfo>(ids.Count);
                foreach (var id in ids)
                {
                    if (!IsValidSlotId(id))
                        continue;

                    var slot = await ReadInfoAsync(id, token);
                    if (slot != null)
                        slots.Add(slot);
                }

                slots.Sort(CompareByDate);
                return slots;
            }
        }

        /// <summary>
        /// Записать сейв. <see cref="SaveSlotKind.Manual"/>: <paramref name="slotId"/> null — новый слот, иначе перезапись
        /// ручного слота. <see cref="SaveSlotKind.Auto"/>: слот выбирается по кругу, <paramref name="slotId"/> не задаётся.
        /// В meta ставятся дата записи и вид слота. Ошибка записи пробрасывается и не портит прежний файл слота.
        /// </summary>
        public async UniTask<SaveSlotInfo> SaveAsync(SaveGame save, SaveSlotKind kind, string slotId = null, CancellationToken token = default)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));

            if (kind == SaveSlotKind.Auto && slotId != null)
                throw new ArgumentException("Autosave slot is chosen by rotation", nameof(slotId));

            if (slotId != null && (!IsValidSlotId(slotId) || slotId.StartsWith(AutosavePrefix, StringComparison.Ordinal)))
                throw new ArgumentException($"Invalid manual slot id '{slotId}'", nameof(slotId));

            using (await LockAsync())
            {
                var now = m_UtcNow();
                var id = slotId;
                if (id == null)
                    id = kind == SaveSlotKind.Auto ? await ChooseAutosaveSlotAsync(token) : await CreateManualSlotIdAsync(now, token);

                save.Meta.SavedAtUtc = now;
                save.Meta.Kind = kind;
                await m_Storage.WriteAsync(id, stream => SaveArchive.Write(save, stream), token);
                return new SaveSlotInfo(id, save.Meta);
            }
        }

        /// <summary>Прочитать и проверить слот целиком (описание, все секции). Нет слота или он повреждён — <see cref="SaveException"/>.</summary>
        public async UniTask<SaveGame> LoadAsync(string slotId, CancellationToken token = default)
        {
            if (!IsValidSlotId(slotId))
                throw new SaveException($"Invalid slot id '{slotId}'");

            using (await LockAsync())
            {
                Stream stream;
                try
                {
                    stream = await m_Storage.OpenReadAsync(slotId, token);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    throw new SaveException($"Slot '{slotId}' can't be opened: {exception.Message}", exception);
                }

                if (stream == null)
                    throw new SaveException($"Slot '{slotId}' not found");

                using (stream)
                {
                    try
                    {
                        return SaveArchive.Read(stream);
                    }
                    catch (SaveException exception)
                    {
                        throw new SaveException($"Slot '{slotId}': {exception.Message}", exception);
                    }
                }
            }
        }

        public async UniTask DeleteAsync(string slotId, CancellationToken token = default)
        {
            if (!IsValidSlotId(slotId))
                throw new ArgumentException($"Invalid slot id '{slotId}'", nameof(slotId));

            using (await LockAsync())
                await m_Storage.DeleteAsync(slotId, token);
        }

        /// <summary>Дождаться конца начатых операций (выход из приложения после записи).</summary>
        public async UniTask WaitIdleAsync()
        {
            var turn = await LockAsync();
            turn.Dispose();
        }

#region Slots

        /// <summary>Описание слота; слота нет — null; не читается — повреждённый слот (предупреждение в лог).</summary>
        private async UniTask<SaveSlotInfo> ReadInfoAsync(string slotId, CancellationToken token)
        {
            try
            {
                using (var stream = await m_Storage.OpenReadAsync(slotId, token))
                    return stream == null ? null : new SaveSlotInfo(slotId, SaveArchive.ReadMeta(stream));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogWarning($"[Saves] Slot '{slotId}' is broken: {exception.Message}");
                return new SaveSlotInfo(slotId, null, exception.Message);
            }
        }

        /// <summary>Пустой или повреждённый автосейв — первым (по номеру), иначе самый старый по дате в meta.</summary>
        private async UniTask<string> ChooseAutosaveSlotAsync(CancellationToken token)
        {
            string oldest = null;
            var oldestDate = DateTime.MaxValue;
            for (var i = 0; i < AutosaveSlotCount; i++)
            {
                var id = GetAutosaveSlotId(i);
                var slot = await ReadInfoAsync(id, token);
                if (slot is not { IsBroken: false })
                    return id;

                if (slot.Meta.SavedAtUtc >= oldestDate)
                    continue;

                oldestDate = slot.Meta.SavedAtUtc;
                oldest = id;
            }

            return oldest;
        }

        private async UniTask<string> CreateManualSlotIdAsync(DateTime now, CancellationToken token)
        {
            var ids = new HashSet<string>(await m_Storage.ListAsync(token), StringComparer.Ordinal);
            var baseId = ManualPrefix + now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var id = baseId;
            for (var n = 2; ids.Contains(id); n++)
                id = baseId + "_" + n.ToString(CultureInfo.InvariantCulture);

            return id;
        }

        private static int CompareByDate(SaveSlotInfo a, SaveSlotInfo b)
        {
            if (a.IsBroken != b.IsBroken)
                return a.IsBroken ? 1 : -1;

            var byDate = a.IsBroken ? 0 : b.Meta.SavedAtUtc.CompareTo(a.Meta.SavedAtUtc);
            return byDate != 0 ? byDate : string.CompareOrdinal(a.SlotId, b.SlotId);
        }

#endregion

#region Queue

        /// <summary>
        /// Очередь операций (главный поток): операция начинается после <c>Dispose</c> результата предыдущей. Ожидание
        /// очереди не отменяется: брошенная очередь заблокировала бы следующие операции.
        /// </summary>
        private async UniTask<IDisposable> LockAsync()
        {
            if (m_IsBusy)
            {
                var turn = new UniTaskCompletionSource();
                m_Waiting.Enqueue(turn);
                await turn.Task;
            }

            m_IsBusy = true;
            return new Turn(this);
        }

        private void Release()
        {
            if (m_Waiting.Count > 0)
                m_Waiting.Dequeue().TrySetResult();
            else
                m_IsBusy = false;
        }

        private sealed class Turn : IDisposable
        {
            private SaveService m_Service;

            public Turn(SaveService service)
            {
                m_Service = service;
            }

            public void Dispose()
            {
                m_Service?.Release();
                m_Service = null;
            }
        }

#endregion
    }
}
