using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XandArt.Architecture;
using XandArt.Architecture.IOC;
using XandArt.Architecture.Utils;

namespace XandArt.TheGrowth
{
    [SelectionBase]
    public class HUBMissionSelection : WidgetBase
    {
#region Settings

        [BoxGroup("Layout")]
        [SerializeField]
        private TMP_Text m_Title;

        [BoxGroup("Layout")]
        [SerializeField]
        private TMP_Text m_Description;

        [BoxGroup("Layout")]
        [SerializeField]
        private List<HUBButton> m_LocationButtons;

        [BoxGroup("Layout")]
        [SerializeField]
        private List<LocationModel> m_Locations;

        [BoxGroup("Layout")]
        [Space]
        [SerializeField]
        private Transform m_ResourceContainer;

        [BoxGroup("Layout")]
        [SerializeField]
        private Transform m_EnemiesContainer;

        [BoxGroup("Settings")]
        [SerializeField]
        private Image m_ResourcePrefab;

        [BoxGroup("Settings")]
        [SerializeField]
        private UIItem m_EnemyPrefab;

        [BoxGroup("Startup")]
        [SerializeField]
        private List<string> m_SlotsToTransfer;

        [BoxGroup("Startup")]
        [SerializeField]
        private InventoryModel m_SourceInventory;
        
        [BoxGroup("Startup")]
        [SerializeField]
        private InventoryModel m_TargetInventory;

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
                        card.Setup(visual);
                        card.enabled = false;
                        break;
                }
            }
        }

        public void StartSelected()
        {
            var fromInventory = m_GameManager.CurrentGameState.GetInventory(m_SourceInventory);
            var intoInventory = m_GameManager.CurrentGameState.GetInventory(m_TargetInventory);

            var items = fromInventory.Items.Select(itemRef => itemRef.Value as CompositeEntity).ToList();
            foreach (var entity in items)
            {
                if (entity == null) continue;
                var slotIdComponent = entity.GetComponent<CardInventoryComponent>();
                if (slotIdComponent == null) continue;
                if (string.IsNullOrEmpty(slotIdComponent.SlotName)) continue;
                if (!m_SlotsToTransfer.Contains(slotIdComponent.SlotName)) continue;
                fromInventory.Remove(entity);
                intoInventory.Add(entity);
            }

            m_GameManager.CurrentGameState.GotoLocation(m_Selected);
        }

#endregion
    }
}