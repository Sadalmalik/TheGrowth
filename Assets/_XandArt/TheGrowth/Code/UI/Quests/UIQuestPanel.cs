using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;
using XandArt.TheGrowth.Quests;

namespace XandArt.TheGrowth
{
    public class UIQuestPanel : WidgetBase
    {
        [SerializeField]
        private Transform m_Container;

        [SerializeField]
        private UIQuest m_Prefab;
        
        [SerializeField]
        private float m_UpdateDelay = 1f;

        private float m_Elapsed;

        private Dictionary<CompositeEntity, UIQuest> m_QuestPanels;
        
        private void Update()
        {
            m_Elapsed += Time.deltaTime;
            if (m_Elapsed <m_UpdateDelay)
                return;
            m_Elapsed -= m_UpdateDelay;

            m_QuestPanels ??= new Dictionary<CompositeEntity, UIQuest>();
            foreach (var entity in Game.Container.Get<GameManager>().CurrentGameState.Entities)
            {
                if (entity is not CompositeEntity compositeEntity) continue;
                if (!QuestHelper.IsQuest(compositeEntity)) continue;

                if (!m_QuestPanels.TryGetValue(compositeEntity, out var uiQuest))
                {
                    m_QuestPanels[compositeEntity] = uiQuest = Instantiate(m_Prefab, m_Container, false);
                    uiQuest.Setup(compositeEntity);
                }
                else
                {
                    uiQuest.Refresh();
                }
            }
        }
    }
}