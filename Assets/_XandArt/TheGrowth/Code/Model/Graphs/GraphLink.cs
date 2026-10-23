using System;
using UnityEngine;

namespace TheGrowth.Model.Graphs
{
    /// <summary>Связь поля: стабильный ID и два слота. Связь ненаправленная: порядок слотов не важен.</summary>
    [Serializable]
    public sealed class GraphLink
    {
        [SerializeField]
        private string m_Id;

        [SerializeField]
        private string m_SlotA;

        [SerializeField]
        private string m_SlotB;

        /// <summary>Для сериализации Unity.</summary>
        public GraphLink()
        {
        }

        public GraphLink(string id, string slotA, string slotB)
        {
            m_Id = id;
            m_SlotA = slotA;
            m_SlotB = slotB;
        }

        public string Id => m_Id;

        public string SlotA => m_SlotA;

        public string SlotB => m_SlotB;

        public bool Connects(string slot)
        {
            return slot != null && (m_SlotA == slot || m_SlotB == slot);
        }

        /// <summary>Связь между этими слотами в любом порядке.</summary>
        public bool Connects(string a, string b)
        {
            return (m_SlotA == a && m_SlotB == b) || (m_SlotA == b && m_SlotB == a);
        }

        /// <summary>Второй слот связи; <paramref name="slot"/> не из этой связи — null.</summary>
        public string GetOther(string slot)
        {
            if (m_SlotA == slot)
                return m_SlotB;

            return m_SlotB == slot ? m_SlotA : null;
        }

        public override string ToString()
        {
            return $"link '{m_Id}' ({m_SlotA} - {m_SlotB})";
        }
    }
}
