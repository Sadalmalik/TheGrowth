using System;
using System.Collections.Generic;
using TheGrowth.Core.Entities;

namespace TheGrowth.Model.Entities
{
    /// <summary>
    /// Обязательные компоненты видов сущностей — единственное их описание. По нему пресеты создания добавляют компоненты,
    /// инспектор конфига не даёт их удалить, сборка реестра контента дописывает недостающие во все конфиги вида (если
    /// описание изменилось), реестр сущностей сообщает о недостающих. Описание в Model: здесь конкретные компоненты карты;
    /// обязательный компонент из слоя выше (визуал, Presentation) сюда добавить нельзя — тогда описание переедет в
    /// редакторский код.
    /// </summary>
    public static class RequiredComponents
    {
        private static readonly Type[] Card = { typeof(CardBrainSettings) };

        /// <summary>Типы настроек обязательных компонентов вида.</summary>
        public static IReadOnlyList<Type> Get(EntityKind kind)
        {
            return kind == EntityKind.Card ? Card : Type.EmptyTypes;
        }

        public static bool IsRequired(EntityKind kind, Type settingsType)
        {
            foreach (var type in Get(kind))
            {
                if (type == settingsType)
                    return true;
            }

            return false;
        }

        /// <summary>Обязательные компоненты вида конфига, которых в нём нет (сравнение по точному типу настроек).</summary>
        public static List<Type> GetMissing(EntityModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var missing = new List<Type>();
            foreach (var type in Get(model.Kind))
            {
                if (!Contains(model, type))
                    missing.Add(type);
            }

            return missing;
        }

        private static bool Contains(EntityModel model, Type type)
        {
            foreach (var settings in model.Components)
            {
                if (settings != null && settings.GetType() == type)
                    return true;
            }

            return false;
        }
    }
}
