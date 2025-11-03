using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    [SelectionBase]
    public class UICraftItemSlot : WidgetBase
    {
        [BoxGroup("UI")]
        [SerializeField]
        private Image m_Icon;
        
        [BoxGroup("UI")]
        [SerializeField]
        private TMP_Text m_Counter;
        
        [BoxGroup("Craft")]
        [SerializeField]
        private InventoryModel m_Inventory;

        [BoxGroup("Craft")]
        [SerializeField]
        private EntityModel m_Entity;

        [BoxGroup("Craft")]
        [SerializeField]
        private int m_Required;

        [Inject]
        private GameManager m_GameManager;
        
        public override void Init()
        {
            Refresh();
        }

        public void Refresh()
        {
            var inventory = m_GameManager.CurrentGameState.GetInventory(m_Inventory);
            var total = inventory.Count(m_Entity);
            m_Icon.sprite = m_Entity.GetComponent<CardVisual>()?.Portrait;
            m_Counter.SetText($"{total} / {m_Required}");
        }
    }
}