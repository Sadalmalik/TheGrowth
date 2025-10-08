using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда вызывает OnStep у всех карт на поле
    /// </summary>
    public partial class CallStep : Command
    {
        public List<Command> AfterCommands;
        
        public override void Execute(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();
            
            _ = expeditionManager.CallStep(OnComplete);
            
            return;

            void OnComplete()
            {
                AfterCommands.ExecuteAll(context);
            }
        }
    }
}