using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда выводит в консоль сообщение
    /// </summary>
    public class DebugLog : Command
    {
        public string Message;

        public override void Execute(Context context)
        {
            Debug.Log(Message);
        }
    }
}