using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class BoardEntity : Entity
    {
#region Savable

        [JsonProperty]
        private List<Ref<SlotEntity>> _slots;

        [JsonProperty]
        private Ref<SlotEntity> _deck;
        
        [JsonProperty]
        private Ref<SlotEntity> _hand;
        
        [JsonProperty]
        private Ref<SlotEntity> _back;

#endregion


#region Lifecycle

        [JsonIgnore]
        public SlotEntity DeckSlot { get => _deck; set => _deck = value; }
        
        [JsonIgnore]
        public SlotEntity HandSlot { get => _hand; set => _hand = value; }
        
        [JsonIgnore]
        public SlotEntity BackSlot { get => _back; set => _back = value; }
        
        [JsonIgnore]
        public Vector2Int Size;
        
        [JsonIgnore]
        public Dictionary<Vector2Int, SlotEntity> Slots;

        public void Initialize(GameState gameState, Grid grid)
        {
            Size = grid.size;
            var sx = grid.size.x;
            var sy = grid.size.y;

            var firstStart = Slots == null;
            Slots ??= new Dictionary<Vector2Int, SlotEntity>();

            for (int y = 0; y < sy; y++)
            for (int x = 0; x < sx; x++)
            {
                var index = new Vector2Int(x, y);
                var view = grid.Cells[x, y].GetComponent<EntitySlotView>();
                if (firstStart)
                {
                    var slot = gameState.Create<SlotEntity>();
                    slot.IsTableSlot = true;
                    slot.Index = index;
                    slot.Position = view.transform.position;
                    slot.SetView(view);
                    Slots[index] = slot;
                }
                else
                {
                    Slots[index].SetView(view);
                }
            }
        }

        public void Dispose(GameState gameState)
        {
            foreach (var slot in Slots.Values)
                gameState.Destroy(slot);
        }

        public override void OnPreSave()
        {
            _slots ??= new List<Ref<SlotEntity>>();
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
                var slot = (SlotEntity) slotRef;
                Slots[slot.Index] = slot;
            }
        }

#endregion
    }
}