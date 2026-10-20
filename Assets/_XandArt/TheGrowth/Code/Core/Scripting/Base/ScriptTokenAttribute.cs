using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Запись значения enum в языке скриптов вместо имени: <c>[ScriptToken("&gt;")] Greater</c>.
    /// Парсер принимает и имя, и запись; принтер пишет запись.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ScriptTokenAttribute : Attribute
    {
        public string Token { get; }

        public ScriptTokenAttribute(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Script token is empty", nameof(token));

            Token = token.Trim();
        }
    }
}
