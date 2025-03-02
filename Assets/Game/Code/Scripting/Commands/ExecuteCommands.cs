namespace Sadalmalik.TheGrowth
{
    public class ExecuteCommands : Command
    {
        public CommandConfig config;
        
        public override void Execute(Context context)
        {
            config.Commands.ExecuteAll(context);
        }
    }
}