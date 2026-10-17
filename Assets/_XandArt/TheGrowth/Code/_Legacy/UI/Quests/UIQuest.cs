using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using XandArt.Architecture;
using XandArt.TheGrowth.Quests;

namespace XandArt.TheGrowth
{
    public class UIQuest : WidgetBase
    {
        [SerializeField]
        private Transform m_Container;
        
        [SerializeField]
        private TMP_Text m_Title;
        
        [SerializeField]
        private TMP_Text m_Description;
            
        [SerializeField]
        private UIQuestRequirement m_RequirementPrefab;
        
        private CompositeEntity m_Quest;
        private List<UIQuestRequirement> m_UIRequirements;
        
        public void Setup(CompositeEntity questEntity)
        {
            m_Quest = questEntity;
            var info = m_Quest.Model.components.OfType<QuestInfo>().FirstOrDefault();
            if (info != null)
            {
                m_Title.SetText(info.Title);
                m_Description.SetText(info.Description);
            }

            m_UIRequirements = new List<UIQuestRequirement>();
            var requirements = m_Quest.Components.OfType<QuestRequirementComponent>();
            foreach (var requirement in requirements)
            {
                var uiRequirement = Instantiate(m_RequirementPrefab, m_Container, false);
                uiRequirement.Set(requirement.GetDescription());
                m_UIRequirements.Add(uiRequirement);
            }
        }

        public void Refresh()
        {
            var requirements = m_Quest.Components.OfType<QuestRequirementComponent>().ToArray();
            for (int i = 0; i < m_UIRequirements.Count; i++)
            {
                m_UIRequirements[i].Set(requirements[i].GetDescription());
            }
        }
    }
}