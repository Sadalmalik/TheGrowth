using System.Collections.Generic;
using System.Linq;
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
            if (m_Elapsed < m_UpdateDelay)
                return;
            m_Elapsed -= m_UpdateDelay;

            m_QuestPanels ??= new Dictionary<CompositeEntity, UIQuest>();
            var state = Game.Container.Get<GameManager>().CurrentGameState;
            if (state == null)
            {
                if (m_QuestPanels.Count > 0)
                {
                    foreach (var uiQuest in m_QuestPanels.Values)
                        Destroy(uiQuest);
                    m_QuestPanels.Clear();
                }

                return;
            }

            var toRemove = m_QuestPanels.Keys.ToList();

            foreach (var entity in Game.Container.Get<GameManager>().CurrentGameState.Entities)
            {
                if (entity is not CompositeEntity compositeEntity) continue;
                if (!QuestHelper.IsQuest(compositeEntity)) continue;

                toRemove.Remove(compositeEntity);
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

            if (toRemove.Count==0) return;
            
            foreach (var questEntity in toRemove)
            {
                Destroy(m_QuestPanels[questEntity].gameObject);
                m_QuestPanels.Remove(questEntity);
            }
        }
    }
}