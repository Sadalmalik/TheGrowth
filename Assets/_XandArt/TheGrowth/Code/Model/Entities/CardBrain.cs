using System;
using TheGrowth.Core.Entities;
using UnityEngine;

namespace TheGrowth.Model.Entities
{
    /// <summary>Настройки мозга карты — обязательного компонента вида «карта» (<see cref="RequiredComponents"/>).</summary>
    [Serializable]
    [ComponentKey("card_brain")]
    public sealed class CardBrainSettings : ComponentSettings<CardBrain>
    {
        [SerializeField]
        [Tooltip("The card is face up when created. A slot may turn it when the card is placed there.")]
        private bool m_FaceUp = true;

        /// <summary>Лицом вверх при создании.</summary>
        public bool FaceUp => m_FaceUp;
    }

    /// <summary>
    /// Мозг карты — обязательный компонент карты, состояние карты в игре. Пока — лицом вверх или рубашкой; хуки и свойства
    /// скриптов карты — следующий шаг E03. Состояние в сейве — <see cref="CardBrainSave"/> под ключом <c>card_brain</c>.
    /// </summary>
    public sealed class CardBrain : ComponentBehaviour<CardBrainSettings, CardBrainSave>
    {
        public bool IsFaceUp { get; private set; }

        /// <summary>Повернуть лицом вверх или рубашкой; true — состояние изменилось.</summary>
        public bool SetFaceUp(bool faceUp)
        {
            if (IsFaceUp == faceUp)
                return false;

            IsFaceUp = faceUp;
            return true;
        }

        protected override void OnCreated()
        {
            IsFaceUp = Settings.FaceUp;
        }

        protected override CardBrainSave WriteState()
        {
            return new CardBrainSave { FaceUp = IsFaceUp };
        }

        protected override void ReadState(CardBrainSave state)
        {
            IsFaceUp = state.FaceUp;
        }
    }

    /// <summary>DTO состояния <see cref="CardBrain"/>. Имена полей — ключи JSON: переименование поля — миграция.</summary>
    public sealed class CardBrainSave
    {
        public bool FaceUp;
    }
}
