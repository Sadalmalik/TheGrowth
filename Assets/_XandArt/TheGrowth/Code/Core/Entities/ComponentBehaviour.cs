using System;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Поведение компонента — runtime-часть компонента у экземпляра сущности: данные и API (логика — скрипты карты).
    /// Создаётся настройками (<see cref="ComponentSettings.CreateBehaviour"/>) вместе с сущностью. Состояние в сейве —
    /// <see cref="IComponentState"/> (база <see cref="ComponentBehaviour{TSettings,TState}"/>) под ключом настроек.
    /// </summary>
    public abstract class ComponentBehaviour
    {
        public Entity Entity { get; private set; }

        public ComponentSettings Settings { get; private set; }

        /// <summary>Ключ компонента (<see cref="ComponentKeyAttribute"/> настроек).</summary>
        public string Key { get; private set; }

        internal void Attach(Entity entity, ComponentSettings settings, string key)
        {
            Entity = entity;
            Settings = settings;
            Key = key;
        }

        internal void NotifyCreated()
        {
            OnCreated();
        }

        /// <summary>Все компоненты сущности созданы: здесь можно найти соседние. Состояние из сейва читается после.</summary>
        protected virtual void OnCreated()
        {
        }

        public override string ToString()
        {
            return $"{GetType().Name} ({Key})";
        }
    }

    /// <summary>Поведение с типизированными настройками.</summary>
    public abstract class ComponentBehaviour<TSettings> : ComponentBehaviour where TSettings : ComponentSettings
    {
        public new TSettings Settings => (TSettings)base.Settings;
    }

    /// <summary>
    /// Состояние компонента в сейве сущности: DTO (только примитивы, ID и вложенные DTO) под ключом компонента, без имён
    /// .NET-типов. Реализация — через <see cref="ComponentBehaviour{TSettings,TState}"/>.
    /// </summary>
    public interface IComponentState
    {
        /// <summary>Тип DTO для чтения.</summary>
        Type StateType { get; }

        /// <summary>DTO текущего состояния; null — сохранять нечего.</summary>
        object WriteState();

        /// <summary>Состояние из сейва: после создания сущности и <see cref="ComponentBehaviour.OnCreated"/>.</summary>
        void ReadState(object state);
    }

    /// <summary>Поведение с настройками <typeparamref name="TSettings"/> и состоянием в сейве <typeparamref name="TState"/>.</summary>
    public abstract class ComponentBehaviour<TSettings, TState> : ComponentBehaviour<TSettings>, IComponentState
        where TSettings : ComponentSettings
        where TState : class, new()
    {
        Type IComponentState.StateType => typeof(TState);

        object IComponentState.WriteState()
        {
            return WriteState();
        }

        void IComponentState.ReadState(object state)
        {
            ReadState((TState)state);
        }

        protected abstract TState WriteState();

        protected abstract void ReadState(TState state);
    }
}
