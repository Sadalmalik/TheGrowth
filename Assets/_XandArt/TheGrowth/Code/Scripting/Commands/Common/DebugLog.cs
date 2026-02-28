using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth.Common
{
    /// <summary>
    /// Команда выводит в консоль сообщение
    /// </summary>
    [GUIColor("#FFFFA0")]
    public class DebugLog : Command
    {
        public string Message;
        public bool Enabled = true;

        public override void Execute(Context context)
        {
            if (Enabled)
                Debug.Log(Message);
        }
    }
}