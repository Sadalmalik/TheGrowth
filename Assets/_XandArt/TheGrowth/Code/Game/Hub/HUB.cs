using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class HUB : WidgetBase
    {
        [SerializeField]
        private HUBRoom _start;

        private List<HUBRoom> _rooms;

        private HUBRoom _prev;
        private HUBRoom _active;
        
        public override void Init()
        {
            _rooms = GetComponentsInChildren<HUBRoom>().ToList();
            foreach (var room in _rooms)
                room.Hide();
            GoToRoom(_start);
        }

        public void GoToRoom(HUBRoom room)
        {
            _active?.Hide();
            _prev = _active;
            _active = room;
            _active?.Show();
            
            Debug.Log($"Enter room: {room.name}");
        }

        public void GoToLastRoom()
        {
            GoToRoom(_prev);
        }
    }
}