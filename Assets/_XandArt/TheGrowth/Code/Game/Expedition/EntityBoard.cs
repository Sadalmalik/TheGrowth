using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntityBoard : Entity
    {
#region Savable

        [JsonProperty]
        private List<Ref<EntitySlot>> _slots;

#endregion


#region Lifecycle

        [JsonIgnore]
        public Dictionary<Vector2Int, EntitySlot> Slots;

        public void Initialize(GameState gameState, Grid grid)
        {
            var sx = grid.size.x;
            var sy = grid.size.y;

            var firstStart = Slots == null;
            Slots ??= new Dictionary<Vector2Int, EntitySlot>();

            for (int y = 0; y < sy; y++)
            for (int x = 0; x < sx; x++)
            {
                var index = new Vector2Int(x, y);
                var view = grid.Cells[x, y].GetComponent<EntitySlotView>();
                if (firstStart)
                {
                    var slot = gameState.Create<EntitySlot>();
                    slot.IsTableSlot = true;
                    slot.Index = index;
                    slot.Position = view.transform.position;
                    slot.View = view;
                    Slots[index] = slot;
                }
                else
                {
                    Slots[index].View = view;
                }
            }
        }

        public override void OnPreSave()
        {
            _slots ??= new List<Ref<EntitySlot>>();
            _slots.Clear();
            foreach (var slot in Slots.Values)
            {
                _slots.Add(slot);
            }
        }

        public override void OnPostLoad()
        {
            foreach (var slotRef in _slots)
            {
                var slot = (EntitySlot) slotRef;
                Slots[slot.Index] = slot;
            }
        }

#endregion
    }
}