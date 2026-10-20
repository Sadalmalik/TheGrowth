using System;
using System.Collections.Generic;
using System.Globalization;
using TheGrowth.Core.Saves;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TheGrowth.UI.Temporary
{
    /// <summary>
    /// ВРЕМЕННО (до UI на MVVM, E08): главное меню («Продолжить», «Новая игра», «Загрузить», «Выход»), панель локации
    /// («Сохранить», «Загрузить», «В меню») и список слотов (дата, вид, локация; загрузка и удаление). Про
    /// <c>GameFlow</c> и сохранения не знает: события кнопок, переключение панелей и данные списка связывает
    /// <c>GameStarter</c>.
    /// </summary>
    public sealed class TemporaryMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_MainMenuPanel;

        [SerializeField]
        private Button m_ContinueButton;

        [SerializeField]
        private Button m_NewGameButton;

        [SerializeField]
        private Button m_LoadButton;

        [SerializeField]
        private Button m_QuitButton;

        [SerializeField]
        private GameObject m_LocationPanel;

        [SerializeField]
        private Button m_SaveButton;

        [SerializeField]
        private Button m_LocationLoadButton;

        [SerializeField]
        private Button m_ExitToMenuButton;

        [SerializeField]
        private GameObject m_SlotsPanel;

        [SerializeField]
        private RectTransform m_SlotsContent;

        [SerializeField]
        [Tooltip("Inactive row copied for every slot.")]
        private TemporarySlotRow m_SlotTemplate;

        [SerializeField]
        private GameObject m_SlotsEmptyLabel;

        [SerializeField]
        private Button m_SlotsCloseButton;

        private readonly List<TemporarySlotRow> m_Rows = new List<TemporarySlotRow>();

        public event Action ContinueClicked;

        public event Action NewGameClicked;

        /// <summary>«Загрузить» в главном меню или в локации: открыть список слотов.</summary>
        public event Action LoadClicked;

        public event Action SaveClicked;

        public event Action QuitClicked;

        public event Action ExitToMenuClicked;

        public event Action<string> SlotLoadClicked;

        public event Action<string> SlotDeleteClicked;

        public bool IsSlotsVisible => m_SlotsPanel != null && m_SlotsPanel.activeSelf;

        /// <summary>Панели главного меню и локации; список слотов закрывается.</summary>
        public void Show(bool mainMenu, bool location)
        {
            if (m_MainMenuPanel != null)
                m_MainMenuPanel.SetActive(mainMenu);

            if (m_LocationPanel != null)
                m_LocationPanel.SetActive(location);

            HideSlots();
        }

        /// <summary>«Продолжить» — есть слот для загрузки.</summary>
        public void SetContinueAvailable(bool available)
        {
            if (m_ContinueButton != null)
                m_ContinueButton.gameObject.SetActive(available);
        }

        /// <summary>Открыть список слотов (порядок — как передан).</summary>
        public void ShowSlots(IReadOnlyList<SaveSlotInfo> slots)
        {
            ClearRows();
            if (m_SlotsPanel == null)
                return;

            m_SlotsPanel.SetActive(true);
            if (m_SlotsEmptyLabel != null)
                m_SlotsEmptyLabel.SetActive(slots.Count == 0);

            if (m_SlotTemplate == null || m_SlotsContent == null)
                return;

            foreach (var slot in slots)
            {
                var row = Instantiate(m_SlotTemplate, m_SlotsContent);
                row.name = slot.SlotId;
                row.gameObject.SetActive(true);
                var slotId = slot.SlotId;
                row.Bind(Describe(slot), !slot.IsBroken, () => SlotLoadClicked?.Invoke(slotId), () => SlotDeleteClicked?.Invoke(slotId));
                m_Rows.Add(row);
            }
        }

        public void HideSlots()
        {
            ClearRows();
            if (m_SlotsPanel != null)
                m_SlotsPanel.SetActive(false);
        }

        private void Awake()
        {
            if (m_SlotTemplate != null)
                m_SlotTemplate.gameObject.SetActive(false);

            Show(false, false);
            SetContinueAvailable(false);
            Subscribe(true);
        }

        private void OnDestroy()
        {
            Subscribe(false);
        }

        private void Subscribe(bool subscribe)
        {
            Listen(m_ContinueButton, HandleContinue, subscribe);
            Listen(m_NewGameButton, HandleNewGame, subscribe);
            Listen(m_LoadButton, HandleLoad, subscribe);
            Listen(m_QuitButton, HandleQuit, subscribe);
            Listen(m_SaveButton, HandleSave, subscribe);
            Listen(m_LocationLoadButton, HandleLoad, subscribe);
            Listen(m_ExitToMenuButton, HandleExitToMenu, subscribe);
            Listen(m_SlotsCloseButton, HideSlots, subscribe);
        }

        private void ClearRows()
        {
            foreach (var row in m_Rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            m_Rows.Clear();
        }

        private static string Describe(SaveSlotInfo slot)
        {
            if (slot.IsBroken)
                return $"{slot.SlotId} — повреждён";

            var meta = slot.Meta;
            var date = meta.SavedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var kind = meta.Kind == SaveSlotKind.Auto ? "Авто" : "Ручное";
            var time = TimeSpan.FromSeconds(meta.PlayTimeSeconds);
            return $"{date} · {kind} · {meta.Location} · {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
        }

        private static void Listen(Button button, UnityAction handler, bool subscribe)
        {
            if (button == null)
                return;

            if (subscribe)
                button.onClick.AddListener(handler);
            else
                button.onClick.RemoveListener(handler);
        }

        private void HandleContinue()
        {
            ContinueClicked?.Invoke();
        }

        private void HandleNewGame()
        {
            NewGameClicked?.Invoke();
        }

        private void HandleLoad()
        {
            LoadClicked?.Invoke();
        }

        private void HandleSave()
        {
            SaveClicked?.Invoke();
        }

        private void HandleQuit()
        {
            QuitClicked?.Invoke();
        }

        private void HandleExitToMenu()
        {
            ExitToMenuClicked?.Invoke();
        }
    }
}
