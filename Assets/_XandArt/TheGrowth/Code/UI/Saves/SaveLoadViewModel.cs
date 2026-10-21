using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using R3;
using TheGrowth.Core.Saves;
using TheGrowth.UI.Flow;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using TheGrowth.UI.Texts;
using UnityEngine;

namespace TheGrowth.UI.Saves
{
    /// <summary>Окно слотов: загрузка (из меню и из игры) или сохранение (только из игры).</summary>
    public enum SaveLoadMode
    {
        Load = 0,
        Save = 1,
    }

    /// <summary>
    /// Окно слотов: список слотов (новые первыми, повреждённые в конце), загрузка, удаление (с подтверждением);
    /// в режиме сохранения — новый слот и перезапись ручного слота (с подтверждением). Список и превью читаются из
    /// <see cref="SaveService"/>, сохранение и загрузка — через <see cref="IUIFlow"/>. Пока идёт операция
    /// (<see cref="IsBusy"/>), новые не начинаются. Загрузка закрывает окно; ошибку покажет Game всплывающим окном.
    /// </summary>
    public sealed class SaveLoadViewModel : WindowViewModel
    {
        private readonly IUIFlow m_Flow;
        private readonly SaveService m_Saves;
        private readonly PopupService m_Popups;
        private readonly ObservableList<SaveSlotViewModel> m_Slots = new ObservableList<SaveSlotViewModel>();
        private readonly ReactiveProperty<bool> m_IsBusy = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> m_IsEmpty = new ReactiveProperty<bool>();

        public SaveLoadViewModel(SaveLoadMode mode, IUIFlow flow, SaveService saves, PopupService popups)
        {
            Mode = mode;
            m_Flow = flow ?? throw new ArgumentNullException(nameof(flow));
            m_Saves = saves ?? throw new ArgumentNullException(nameof(saves));
            m_Popups = popups ?? throw new ArgumentNullException(nameof(popups));
            RefreshAsync().Forget();
        }

        public SaveLoadMode Mode { get; }

        public string Title => UITexts.Get(Mode == SaveLoadMode.Save ? UITexts.SavesTitleSave : UITexts.SavesTitleLoad);

        /// <summary>Кнопка «Новое сохранение».</summary>
        public bool CanSave => Mode == SaveLoadMode.Save;

        public IReadOnlyObservableList<SaveSlotViewModel> Slots => m_Slots;

        public ReadOnlyReactiveProperty<bool> IsBusy => m_IsBusy;

        /// <summary>Список прочитан и пуст.</summary>
        public ReadOnlyReactiveProperty<bool> IsEmpty => m_IsEmpty;

        /// <summary>Перечитать список слотов.</summary>
        public async UniTask RefreshAsync()
        {
            IReadOnlyList<SaveSlotInfo> slots;
            try
            {
                slots = await m_Saves.ListAsync(Lifetime);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                slots = Array.Empty<SaveSlotInfo>();
            }

            if (IsDisposed)
                return;

            ClearSlots();
            foreach (var slot in slots)
                m_Slots.Add(new SaveSlotViewModel(this, slot, m_Saves));

            m_IsEmpty.Value = m_Slots.Count == 0;
        }

        /// <summary>Сохранить в новый слот.</summary>
        public void SaveNew()
        {
            if (CanSave)
                RunAsync(() => SaveAsync(null)).Forget();
        }

        internal void Load(SaveSlotViewModel slot)
        {
            if (m_IsBusy.Value || !slot.CanLoad || IsDisposed)
                return;

            m_Flow.Load(slot.SlotId);
            Close();
        }

        internal void Overwrite(SaveSlotViewModel slot)
        {
            if (slot.CanOverwrite)
                RunAsync(() => OverwriteAsync(slot.SlotId)).Forget();
        }

        internal void Delete(SaveSlotViewModel slot)
        {
            RunAsync(() => DeleteAsync(slot.SlotId)).Forget();
        }

        protected override void OnDispose()
        {
            ClearSlots();
            m_IsBusy.Dispose();
            m_IsEmpty.Dispose();
        }

        /// <summary>Операция со списком: по одной за раз; окно закрыто во время операции — результат не применяется.</summary>
        private async UniTaskVoid RunAsync(Func<UniTask> operation)
        {
            if (m_IsBusy.Value || IsDisposed)
                return;

            m_IsBusy.Value = true;
            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (!IsDisposed)
                    m_IsBusy.Value = false;
            }
        }

        private async UniTask SaveAsync(string slotId)
        {
            var saved = await m_Flow.SaveAsync(slotId);
            if (IsDisposed)
                return;

            if (saved == null)
            {
                m_Popups.ShowMessageAsync(UITexts.Get(UITexts.ErrorSave)).Forget();
                return;
            }

            await RefreshAsync();
        }

        private async UniTask OverwriteAsync(string slotId)
        {
            if (!await m_Popups.ConfirmAsync(UITexts.Get(UITexts.ConfirmOverwrite)) || IsDisposed)
                return;

            await SaveAsync(slotId);
        }

        private async UniTask DeleteAsync(string slotId)
        {
            if (!await m_Popups.ConfirmAsync(UITexts.Get(UITexts.ConfirmDelete)) || IsDisposed)
                return;

            try
            {
                await m_Saves.DeleteAsync(slotId, Lifetime);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
                m_Popups.ShowMessageAsync(UITexts.Get(UITexts.ErrorDelete)).Forget();
            }

            if (!IsDisposed)
                await RefreshAsync();
        }

        private void ClearSlots()
        {
            var slots = new SaveSlotViewModel[m_Slots.Count];
            m_Slots.CopyTo(slots, 0);
            m_Slots.Clear();
            foreach (var slot in slots)
                slot.Dispose();
        }
    }
}
