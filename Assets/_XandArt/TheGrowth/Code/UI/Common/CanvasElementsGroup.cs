using System;
using System.Collections.Generic;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class CanvasElementsGroup : MonoBehaviour
    {
        private static Dictionary<string, HashSet<CanvasElementsGroup>> m_Groups;

        public static HashSet<CanvasElementsGroup> GetGroup(string groupId)
        {
            return m_Groups.TryGetValue(groupId, out var group) ? group : null;
        }

        public static void SetGroupActive(string groupId, bool active)
        {
            var group = GetGroup(groupId);
            if (group == null) return;
            foreach (var element in group)
                element.gameObject.SetActive(active);
        }

        [SerializeField]
        private string m_GroupId;

        [SerializeField]
        private bool m_HideOnAwake;

        private void Awake()
        {
            m_Groups ??= new Dictionary<string, HashSet<CanvasElementsGroup>>();
            if (!m_Groups.TryGetValue(m_GroupId, out var groups))
                m_Groups[m_GroupId] = groups = new HashSet<CanvasElementsGroup>();

            groups.Add(this);

            if (m_HideOnAwake)
                gameObject.SetActive(false);
        }
    }
}