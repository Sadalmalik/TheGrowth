using System;
using System.Collections.Generic;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Heroes;

namespace TheGrowth.EditorTools.Heroes
{
    /// <summary>
    /// Проверки карт героев и способностей для валидатора контента:
    /// <list type="bullet">
    /// <item>компонент героя (<see cref="HeroCardSettings"/>) — только у карты, ссылка на персонажа обязательна (ошибки);</item>
    /// <item>компонент героя без тега <see cref="HeroCardSettings.Tag"/> и тег без компонента — предупреждения: скрипты видят
    /// героя по тегу, раскладка и способности — по компоненту;</item>
    /// <item>компонент способности (<see cref="AbilityCardSettings"/>) — только у карты (ошибка); владелец необязателен
    /// (пусто — способность игрока).</item>
    /// </list>
    /// </summary>
    public static class HeroChecks
    {
        public static List<(string Message, bool IsWarning)> Check(EntityModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var problems = new List<(string, bool)>();
            var hero = Find<HeroCardSettings>(model);
            var ability = Find<AbilityCardSettings>(model);
            var isTagged = model.HasTag(HeroCardSettings.Tag);
            if (hero != null)
            {
                if (model.Kind != EntityKind.Card)
                    problems.Add(($"a {model.Kind.ToText()} has a hero card component: only cards can be heroes", false));

                if (hero.Character == null)
                    problems.Add(("hero card has no character: set the character of the hero card component", false));

                if (!isTagged)
                    problems.Add(($"hero card is not tagged {HeroCardSettings.Tag}: scripts don't see it as a hero ([card] has tag {HeroCardSettings.Tag})", true));
            }
            else if (isTagged)
            {
                problems.Add(($"the card is tagged {HeroCardSettings.Tag} but has no hero card component: it has no character, add the component or remove the tag", true));
            }

            if (ability != null && model.Kind != EntityKind.Card)
                problems.Add(($"a {model.Kind.ToText()} has an ability card component: only cards can be abilities", false));

            return problems;
        }

        private static T Find<T>(EntityModel model) where T : ComponentSettings
        {
            foreach (var settings in model.Components)
            {
                if (settings is T typed)
                    return typed;
            }

            return null;
        }
    }
}
