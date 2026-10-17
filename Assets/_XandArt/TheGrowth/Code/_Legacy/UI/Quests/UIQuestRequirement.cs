using TMPro;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class UIQuestRequirement : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text m_Text;
        
        public void Set(string text)
        {
            m_Text.SetText(text);
        }
    }
}