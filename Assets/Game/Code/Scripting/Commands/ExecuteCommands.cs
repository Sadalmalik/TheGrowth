namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда выполняет список команд из конфига
    /// </summary>
    public class ExecuteCommands : Command
    {
        public CommandConfig config;
        
        public override void Execute(Context context)
        {
            config.Commands.ExecuteAll(context);
        }
    }
}