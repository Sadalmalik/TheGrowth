using System.Collections.Generic;
using Naninovel;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CommandAlias("executeCommandScript")]
    public class ExecuteScriptNaninovel : Naninovel.Command
    {
        public StringParameter Name;

        public override UniTask Execute(AsyncToken asyncToken = default)
        {
            InitializeStepsList();

            if (Assigned(Name) && m_Commands.TryGetValue(Name.Value, out var commandConfig))
            {
                if (commandConfig.Commands is { Count: > 0 })
                {
                    commandConfig.Commands.ExecuteAll(Game.BaseContext);
                }
            }

            return UniTask.CompletedTask;
        }

        private static Dictionary<string, CommandConfig> m_Commands;

        private static void InitializeStepsList()
        {
            if (m_Commands != null) return;

            m_Commands = new Dictionary<string, CommandConfig>();
            var steps = AssetGuidsManager.GetAllAssets<CommandConfig>();
            foreach (var commands in steps)
            {
                m_Commands.Add(commands.name, commands);
            }
        }
    }
}