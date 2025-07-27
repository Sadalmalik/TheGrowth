using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntitySlot : Entity
    {
#region Savable

        [JsonProperty]
        public bool IsTableSlot;

        [JsonProperty]
        public Vector2Int Index;

        [JsonProperty]
        public Vector3 Position;

        [JsonProperty]
        private List<Ref<CompositeEntity>> _cards;

#endregion


#region Access

        [JsonIgnore]
        public EntitySlotView SlotView => View as EntitySlotView;

        [JsonIgnore]
        private List<CompositeEntity> _cardsList;

        [JsonIgnore]
        public List<CompositeEntity> Cards => _cardsList;

        [JsonIgnore]
        public bool IsEmpty => _cards.Count == 0;

        [JsonIgnore]
        public int Count => _cards.Count;

        [JsonIgnore]
        public CompositeEntity this[int key]
        {
            get => _cards[key];
            set => _cards[key] = value;
        }

#endregion


#region Slot API

        public void Add(CompositeEntity card)
        {
            _cards.Add(card);
        }

        public void Remove(CompositeEntity card)
        {
            _cards.Remove(card);
        }

        public CompositeEntity Top()
        {
            return _cards.Top();
        }

        public CompositeEntity Peek()
        {
            return _cards.Peek();
        }

        public void ShowMarker(bool show)
        {
            SlotView?.ShowMarker(show);
        }

        public Vector3 GetNewPosition()
        {
            if (SlotView == null) return Position;
            return SlotView.transform.position +
                   SlotView.transform.up * CardsViewConfig.Instance.cardThickness * (1 + _cards.Count);
        }

        public Vector3 GetNewRotation()
        {
            if (SlotView == null) return Vector3.zero;
            return SlotView.transform.rotation.eulerAngles +
                   Vector3.up * Random.Range(
                       -CardsViewConfig.Instance.randomAngle,
                       +CardsViewConfig.Instance.randomAngle);
        }

        public override void OnPreSave()
        {
            _cards ??= new List<Ref<CompositeEntity>>();
            _cards.Clear();
            foreach (var slot in _cardsList)
            {
                _cards.Add(slot);
            }
        }

        public override void OnPostLoad()
        {
            _cardsList ??= new List<CompositeEntity>();
            _cardsList.Clear();
            foreach (var cardRef in _cards)
            {
                _cardsList.Add(cardRef);
            }
        }

#endregion
    }
}