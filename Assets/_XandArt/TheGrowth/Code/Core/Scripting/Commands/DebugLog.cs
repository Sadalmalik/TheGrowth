using UnityEngine;

namespace TheGrowth.Core.Scripting
{
    [Script("log", "log [Message]")]
    public sealed class DebugLog : InstantCommand
    {
        public string Message;

        protected override void Run(ScriptContext context)
        {
            Debug.Log(Message);
        }
    }
}
