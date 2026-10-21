using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Core.Profile
{
    /// <summary>
    /// DTO профиля (<see cref="ProfileService"/>): <c>{ "version": 1, "variables": { "имя": значение } }</c>. Имена полей —
    /// ключи JSON: переименование поля — новая версия.
    /// </summary>
    public sealed class ProfileSave
    {
        public int Version;

        /// <summary>Явно заданные переменные области «профиль».</summary>
        public Dictionary<string, JToken> Variables = new Dictionary<string, JToken>();
    }
}
