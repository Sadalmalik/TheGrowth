using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда вызывает завершение игры
    /// </summary>
    public class EndGame : Command
    {
        public enum EVariant
        {
            Win,
            LooseBySteps,
            LooseByMonster
        }

        public EVariant Variant;

        public override void Execute(Context context)
        {
            Debug.Log($"EndGame: {Variant}");
        }
    }
}