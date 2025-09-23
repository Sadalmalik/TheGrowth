using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class SlotEntity : Entity
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
        public bool IsEmpty => _cardsList.Count == 0;

        [JsonIgnore]
        public int Count => _cardsList.Count;

        [JsonIgnore]
        public CompositeEntity this[int key]
        {
            get => _cardsList[key];
            set => _cardsList[key] = value;
        }

#endregion


#region Lifecycle

        private Vector3 _cardOffset;

        public override void OnInit()
        {
            _cards = new List<Ref<CompositeEntity>>();
            _cardsList = new List<CompositeEntity>();
        }

        public void Add(CompositeEntity card)
        {
            _cardsList.Add(card);
        }

        public void Remove(CompositeEntity card)
        {
            _cardsList.Remove(card);
        }

        public CompositeEntity Top()
        {
            return _cardsList.Top();
        }

        public CompositeEntity Peek()
        {
            return _cardsList.Peek();
        }

        public void ShowMarker(bool show)
        {
            SlotView?.ShowMarker(show);
        }

        public Vector3 GetNewPosition(int index)
        {
            if (SlotView == null) return Position;
            if (index == -1) index = 1 + _cardsList.Count;
            return SlotView.transform.position + _cardOffset * index;
        }

        public override void OnViewAssigned()
        {
            if (SlotView == null)
            {
                _cardOffset = SlotView.transform.up * CardsViewConfig.Instance.cardThickness;
            }
            else
            {
                _cardOffset = SlotView.GetCustomOffset() ?? SlotView.transform.up * CardsViewConfig.Instance.cardThickness;
            }
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

        public override void OnDestroy()
        {
        }

#endregion
    }
}