using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class ExplorationEndScreen : WidgetBase
    {
        [SerializeField]
        private TMP_Text m_Header;

        [SerializeField]
        private TMP_Text m_Message;

        [SerializeField]
        private string m_HeaderTextSuccess = "Stage Completed!";

        [SerializeField]
        private string m_HeaderTextFailure = "Stage Failed!";

        [SerializeField]
        private Button m_Continue;

        [Space]
        [SerializeField]
        private List<Command> OnContinue;

        public override void Init()
        {
            m_Continue.onClick.AddListener(HandleContinue);
        }

        public void Show(bool success, string message)
        {
            gameObject.SetActive(true);
            m_Header.SetText(success ? m_HeaderTextSuccess : m_HeaderTextFailure);
            m_Message.SetText(message);
        }

        private void HandleContinue()
        {
            gameObject.SetActive(false);

            OnContinue.ExecuteAll(Game.BaseContext);
        }
    }
}