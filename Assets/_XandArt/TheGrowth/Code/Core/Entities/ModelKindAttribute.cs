using System;
using System.Collections.Generic;
using System.Linq;
using TheGrowth.Core.Scripting;
using UnityEngine;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Ссылка на конфиг сущности определённых видов: на поле <see cref="EntityModel"/> (или списка) конфига, компонента или
    /// параметра команды. Инспектор предлагает только конфиги этих видов, валидатор контента и парсер скриптов
    /// (<see cref="IScriptValueFilter"/>) проверяют вид. Пустая ссылка подходит.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ModelKindAttribute : PropertyAttribute, IScriptValueFilter
    {
        private readonly EntityKind[] m_Kinds;

        public ModelKindAttribute(EntityKind kind, params EntityKind[] other)
        {
            m_Kinds = new[] { kind }.Concat(other ?? Array.Empty<EntityKind>()).Distinct().ToArray();
        }

        public IReadOnlyList<EntityKind> Kinds => m_Kinds;

        public bool Accepts(EntityKind kind)
        {
            return Array.IndexOf(m_Kinds, kind) >= 0;
        }

        /// <summary>null — конфиг подходит (или ссылка пустая); иначе текст ошибки.</summary>
        public string Check(EntityModel model)
        {
            if (model == null || Accepts(model.Kind))
                return null;

            return $"'{model.Id}' is a {model.Kind.ToText()}, expected {Describe()}";
        }

        /// <summary>Ожидаемые виды: <c>card</c>, <c>card or mechanic</c>.</summary>
        public string Describe()
        {
            return string.Join(" or ", m_Kinds.Select(kind => kind.ToText()));
        }

        string IScriptValueFilter.Check(object value)
        {
            return value is EntityModel model ? Check(model) : null;
        }
    }
}
