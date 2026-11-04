using System;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Настройки компонента сущности — элемент списка <c>[SerializeReference]</c> конфига (<see cref="EntityModel"/>): данные
    /// конфига и создание поведения (<see cref="ComponentBehaviour"/>) для каждого экземпляра. Конкретный тип —
    /// <c>[Serializable]</c>, не абстрактный, с конструктором без параметров и ключом <see cref="ComponentKeyAttribute"/>.
    /// Типы объявляются в любом слое (визуал — Presentation): конфиг хранит их, не зная типа. Переименование класса —
    /// <c>[MovedFrom]</c>, иначе Unity теряет тип в конфигах.
    /// </summary>
    [Serializable]
    public abstract class ComponentSettings
    {
        /// <summary>Ключ из <see cref="ComponentKeyAttribute"/>; атрибута нет — null.</summary>
        public string Key => ComponentKeyAttribute.GetKey(GetType());

        /// <summary>Новое поведение для экземпляра сущности.</summary>
        public abstract ComponentBehaviour CreateBehaviour();
    }

    /// <summary>Настройки, создающие поведение <typeparamref name="TBehaviour"/>.</summary>
    [Serializable]
    public abstract class ComponentSettings<TBehaviour> : ComponentSettings where TBehaviour : ComponentBehaviour, new()
    {
        public sealed override ComponentBehaviour CreateBehaviour()
        {
            return new TBehaviour();
        }
    }
}
