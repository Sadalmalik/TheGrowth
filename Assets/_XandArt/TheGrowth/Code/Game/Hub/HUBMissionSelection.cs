using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XandArt.Architecture.IOC;
using XandArt.Architecture.Utils;

namespace XandArt.TheGrowth
{
    [SelectionBase]
    public class HUBMissionSelection : WidgetBase
    {
#region Settings

        [SerializeField]
        private List<HUBButton> m_LocationButtons;

        [SerializeField]
        private List<LocationModel> m_Locations;

        [Space]
        [SerializeField]
        private Transform m_ResourceContainer;

        [SerializeField]
        private Image m_ResourcePrefab;

        [Space]
        [SerializeField]
        private Transform m_EnemiesContainer;

        [SerializeField]
        private UIItem m_EnemyPrefab;

        [Space]
        [SerializeField]
        private TMP_Text m_Title;

        [SerializeField]
        private TMP_Text m_Description;

#endregion


#region API

        [Inject]
        private GameManager m_GameManager;
        
        private LocationModel m_Selected;

        public override void Init()
        {
            var count = Mathf.Max(m_LocationButtons.Count, m_Locations.Count);
            for (var i = 0; i < count; i++)
            {
                m_LocationButtons[i].Button.interactable = m_Locations[i] != null;
            }

            SetSelected(0);
        }

        public void SetSelected(int index)
        {
            m_Selected = m_Locations[index];

            m_Title.SetText(m_Selected.Title);
            m_Description.SetText(m_Selected.Description);

            m_EnemiesContainer.DestroyChildren();
            m_ResourceContainer.DestroyChildren();

            var models = m_Selected.Deck.Entities;
            foreach (var model in models)
            {
                var brain = model.GetComponent<CardBrain>();
                if (brain == null) continue;
                if (0 == (brain.Type & (CardType.Resource | CardType.Monster))) continue;
                var visual = model.GetComponent<CardVisual>();

                switch (brain.Type)
                {
                    case CardType.Resource:
                        var item = Instantiate(m_ResourcePrefab, m_ResourceContainer);
                        item.sprite = visual.Portrait;
                        break;
                    case CardType.Monster:
                        var card = Instantiate(m_EnemyPrefab, m_EnemiesContainer);
                        card.Set(visual);
                        break;
                }
            }
        }

        public void StartSelected()
        {
            m_GameManager.CurrentGameState.GotoLocation(m_Selected);
        }

#endregion
    }
}