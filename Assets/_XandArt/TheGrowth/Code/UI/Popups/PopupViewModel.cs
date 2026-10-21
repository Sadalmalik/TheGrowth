using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ObservableCollections;
using TheGrowth.UI.Root;
using TheGrowth.UI.Texts;

namespace TheGrowth.UI.Popups
{
    /// <summary>Кнопка всплывающего окна — данные для вьюхи кнопки (не VM).</summary>
    public sealed class PopupButton
    {
        private readonly PopupViewModel m_Owner;

        public PopupButton(PopupViewModel owner, int index, string label)
        {
            m_Owner = owner;
            Index = index;
            Label = label;
        }

        public int Index { get; }

        public string Label { get; }

        public bool IsCancel => m_Owner.CancelIndex == Index;

        public void Click()
        {
            m_Owner.Choose(Index);
        }
    }

    /// <summary>
    /// Всплывающее окно: сообщение и кнопки (OK; Да/Нет). Выбор кнопки закрывает окно и завершает <see cref="Result"/>
    /// её номером; отмена (Esc, B) и закрытие без выбора — номер кнопки отмены. Показывает очередь
    /// <see cref="PopupService"/>, поверх всех окон, кроме экрана загрузки.
    /// </summary>
    public sealed class PopupViewModel : WindowViewModel
    {
        private readonly ObservableList<PopupButton> m_Buttons = new ObservableList<PopupButton>();
        private readonly UniTaskCompletionSource<int> m_Result = new UniTaskCompletionSource<int>();

        /// <param name="cancelIndex">Кнопка, которую выбирает отмена.</param>
        public PopupViewModel(string message, IReadOnlyList<string> buttons, int cancelIndex)
        {
            if (buttons == null || buttons.Count == 0)
                throw new ArgumentException("A popup needs at least one button", nameof(buttons));

            if (cancelIndex < 0 || cancelIndex >= buttons.Count)
                throw new ArgumentOutOfRangeException(nameof(cancelIndex), cancelIndex, "Cancel button is out of range");

            Message = message ?? string.Empty;
            CancelIndex = cancelIndex;
            for (var i = 0; i < buttons.Count; i++)
                m_Buttons.Add(new PopupButton(this, i, buttons[i]));
        }

        public string Message { get; }

        public IReadOnlyObservableList<PopupButton> Buttons => m_Buttons;

        public int CancelIndex { get; }

        /// <summary>Номер выбранной кнопки; при закрытии без выбора — <see cref="CancelIndex"/>.</summary>
        public UniTask<int> Result => m_Result.Task;

        /// <summary>Сообщение с кнопкой OK.</summary>
        public static PopupViewModel CreateMessage(string message)
        {
            return new PopupViewModel(message, new[] { UITexts.Get(UITexts.PopupOk) }, 0);
        }

        /// <summary>Подтверждение: «Да» (0) и «Нет» (1, отмена).</summary>
        public static PopupViewModel CreateConfirm(string message)
        {
            return new PopupViewModel(message, new[] { UITexts.Get(UITexts.PopupYes), UITexts.Get(UITexts.PopupNo) }, 1);
        }

        public void Choose(int index)
        {
            if (IsDisposed)
                return;

            if (index < 0 || index >= m_Buttons.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Popup button is out of range");

            m_Result.TrySetResult(index);
            Close();
        }

        public override bool Cancel()
        {
            Choose(CancelIndex);
            return true;
        }

        protected override void OnDispose()
        {
            m_Result.TrySetResult(CancelIndex);
        }
    }
}
