using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TheGrowth.Core.Variables
{
    /// <summary>
    /// Значения переменных в JSON сохранения (секция игры, файл профиля): <c>{ "имя": значение }</c>, только явно заданные.
    /// Чтение по объявлениям: значение по прежнему имени читается под новым, необъявленное, другой области или другого
    /// типа — предупреждение и значение по умолчанию.
    /// </summary>
    public static class VariableValues
    {
        public static Dictionary<string, JToken> Write(VariableStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var values = new Dictionary<string, JToken>(StringComparer.Ordinal);
            foreach (var pair in store.GetValues())
                values.Add(pair.Key, new JValue(pair.Value));

            return values;
        }

        /// <summary>
        /// Заменить значения хранилища значениями из <paramref name="values"/> (null — все по умолчанию). Сначала текущие
        /// имена, затем прежние: если в данных есть оба, остаётся значение под текущим именем.
        /// </summary>
        /// <param name="source">Откуда данные — для предупреждений (<c>[Saves] Section 'variables'</c>).</param>
        public static void Read(VariableStore store, VariableCatalog catalog, IReadOnlyDictionary<string, JToken> values, string source)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            store.Clear();
            if (values == null)
                return;

            var renamed = new List<(string Name, VariableDeclaration Declaration, JToken Token)>();
            foreach (var pair in values)
            {
                if (catalog.TryGet(pair.Key, out var declaration))
                {
                    Restore(store, declaration, pair.Key, pair.Value, source);
                    continue;
                }

                if (catalog.TryGetRenamed(pair.Key, out declaration))
                    renamed.Add((pair.Key, declaration, pair.Value));
                else
                    Debug.LogWarning($"{source}: variable '{pair.Key}' is not declared, its value is ignored");
            }

            foreach (var (name, declaration, token) in renamed)
            {
                if (declaration.Scope == store.Scope && store.IsSet(declaration))
                    Debug.LogWarning($"{source}: variable '{name}' is renamed to '{declaration.Name}', which is also saved: the old value is ignored");
                else
                    Restore(store, declaration, name, token, source);
            }
        }

        private static void Restore(VariableStore store, VariableDeclaration declaration, string name, JToken token, string source)
        {
            if (declaration.Scope != store.Scope)
            {
                Debug.LogWarning($"{source}: variable '{name}' is now a {declaration.Scope.ToText()} variable, its saved value is ignored");
                return;
            }

            if (!TryConvert(declaration.Type, token, out var value))
            {
                Debug.LogWarning($"{source}: value '{token}' of variable '{name}' is not {declaration.Type.ToText()}, the default value is used");
                return;
            }

            store.Restore(declaration, value);
        }

        private static bool TryConvert(VariableType type, JToken token, out object value)
        {
            value = null;
            switch (type)
            {
                case VariableType.Bool:
                    if (token is { Type: JTokenType.Boolean })
                        value = token.Value<bool>();

                    break;
                case VariableType.Int:
                    if (token is JValue { Type: JTokenType.Integer, Value: IConvertible number })
                    {
                        try
                        {
                            value = number.ToInt32(CultureInfo.InvariantCulture);
                        }
                        catch (OverflowException)
                        {
                        }
                    }

                    break;
                case VariableType.String:
                    if (token is { Type: JTokenType.String })
                        value = token.Value<string>();

                    break;
            }

            return value != null;
        }
    }
}
