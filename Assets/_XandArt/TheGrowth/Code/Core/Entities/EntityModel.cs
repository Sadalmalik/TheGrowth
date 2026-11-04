using System.Collections.Generic;
using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Конфиг сущности — единый для всех видов (<see cref="EntityKind"/>): вид, теги из реестра тегов (во время игры не
    /// меняются), переменные области «карта» со значениями по умолчанию, компоненты (<see cref="ComponentSettings"/>,
    /// <c>[SerializeReference]</c>). Одно пространство ID для всех видов. Создаётся пресетом вида (меню
    /// <c>[Game]/Entity/...</c>): пресет добавляет обязательные компоненты вида, инспектор не даёт их удалить, сборка реестра
    /// контента дописывает недостающие. Ссылка на конфиг определённого вида — поле с <see cref="ModelKindAttribute"/>.
    /// Логика — файл скрипта <c>&lt;имя ассета&gt;.script</c> в папке конфига: привязку (<see cref="ScriptId"/>) записывает
    /// сборка реестра контента. Экземпляр в игре — <see cref="Entity"/>.
    /// </summary>
    public sealed class EntityModel : ContentAsset
    {
        [SerializeField]
        private EntityKind m_Kind;

        [SerializeField]
        [Tooltip("Id of the script file '<asset name>.script' next to the asset. Written by the content registry build.")]
        private string m_Script;

        [SerializeField]
        [Tooltip("Tags from the tag registry. They don't change in the game.")]
        private List<string> m_Tags = new List<string>();

        [SerializeField]
        [Tooltip("Card variables of the entity and their defaults. Other card variables are read as their declared defaults and can't be written.")]
        private List<EntityVariable> m_Variables = new List<EntityVariable>();

        [SerializeReference]
        private List<ComponentSettings> m_Components = new List<ComponentSettings>();

        public EntityKind Kind => m_Kind;

        /// <summary>ID файла скрипта сущности (хуки, свойства); файла нет — пусто.</summary>
        public string ScriptId => m_Script;

        public IReadOnlyList<string> Tags => TagList;

        public IReadOnlyList<EntityVariable> Variables => VariableList;

        /// <summary>Настройки компонентов в порядке конфига; пустой элемент или пропавший тип — null.</summary>
        public IReadOnlyList<ComponentSettings> Components => ComponentList;

        /// <summary>Только редактор и тесты.</summary>
        internal List<string> TagList => m_Tags ??= new List<string>();

        /// <summary>Только редактор и тесты.</summary>
        internal List<EntityVariable> VariableList => m_Variables ??= new List<EntityVariable>();

        /// <summary>Только редактор и тесты.</summary>
        internal List<ComponentSettings> ComponentList => m_Components ??= new List<ComponentSettings>();

        public bool HasTag(string tag)
        {
            return tag != null && m_Tags != null && m_Tags.Contains(tag);
        }

        /// <summary>Настройки с ключом <paramref name="key"/>; нет — null.</summary>
        public ComponentSettings FindSettings(string key)
        {
            foreach (var settings in ComponentList)
            {
                if (settings != null && settings.Key == key)
                    return settings;
            }

            return null;
        }

        /// <summary>Только редактор (пресеты, смена вида) и тесты.</summary>
        internal void SetKind(EntityKind kind)
        {
            m_Kind = kind;
        }

        /// <summary>Только редактор (сборка реестра контента) и тесты.</summary>
        internal void SetScript(string scriptId)
        {
            m_Script = string.IsNullOrEmpty(scriptId) ? null : scriptId;
        }
    }
}
