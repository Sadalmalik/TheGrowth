using System;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда модифицирует колличество оставшихся кодов
    /// </summary>
    public class SetStep : Command
    {
        public enum EMode
        {
            Set,
            Add
        }

        public EMode Mode;
        public int Amount;

        public override void Execute(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();

            switch (Mode)
            {
                case EMode.Add:
                    expeditionManager.Steps += Amount;
                    break;
                case EMode.Set:
                    expeditionManager.Steps = Amount;
                    break;
            }
        }
    }
}