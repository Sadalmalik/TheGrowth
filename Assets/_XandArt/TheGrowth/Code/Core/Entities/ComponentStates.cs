using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheGrowth.Core.Saves;
using UnityEngine;

namespace TheGrowth.Core.Entities
{
    /// <summary>
    /// Состояния компонентов сущности в JSON сейва: <c>{ ключ: DTO }</c> — компоненты с <see cref="IComponentState"/>, ключ
    /// из <see cref="ComponentKeyAttribute"/> (не имя типа), DTO — тем же сериализатором, что и секции. Чтение: компонента с
    /// ключом у сущности нет (убран из конфига) или данные не читаются — предупреждение, у компонента остаётся начальное
    /// состояние.
    /// </summary>
    public static class ComponentStates
    {
        /// <summary>Состояния в порядке компонентов; сохранять нечего — null.</summary>
        public static Dictionary<string, JToken> Write(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Dictionary<string, JToken> states = null;
            foreach (var component in entity.Components)
            {
                if (component is not IComponentState stateful)
                    continue;

                var state = stateful.WriteState();
                if (state == null)
                    continue;

                states ??= new Dictionary<string, JToken>(StringComparer.Ordinal);
                states.Add(component.Key, SaveJson.ToToken(state));
            }

            return states;
        }

        /// <param name="source">Откуда данные — для предупреждений (<c>[Saves] Section 'entities': card 3 'Wolf'</c>).</param>
        public static void Read(Entity entity, IReadOnlyDictionary<string, JToken> states, string source)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (states == null)
                return;

            foreach (var pair in states)
            {
                if (entity.Find(pair.Key) is not IComponentState stateful)
                {
                    Debug.LogWarning($"{source}: component '{pair.Key}' has no saved state on the entity any more, its state is ignored");
                    continue;
                }

                object state;
                try
                {
                    state = pair.Value is { Type: JTokenType.Null } ? null : SaveJson.ToObject(pair.Value, stateful.StateType);
                }
                catch (JsonException exception)
                {
                    Debug.LogWarning($"{source}: state of component '{pair.Key}' can't be read ({exception.Message}), the initial state is used");
                    continue;
                }

                if (state == null)
                {
                    Debug.LogWarning($"{source}: state of component '{pair.Key}' is empty, the initial state is used");
                    continue;
                }

                stateful.ReadState(state);
            }
        }
    }
}
