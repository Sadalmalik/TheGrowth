using System.Collections.Generic;
using Naninovel;
using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Команда запускает диалог
    /// </summary>
    public class StartDialogue : Command
    {
        public Script Script;
        public List<Command> AfterCommands;

        public override void Execute(Context context)
        {
            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            if (scriptPlayer == null)
            {
                Debug.LogError($"No ScriptPlayer available! Check Naninovel initialization!");
                return;
            }
        
            scriptPlayer.OnStop += OnComplete;
            _ = scriptPlayer.LoadAndPlay(Script.Path);
            
            return;

            void OnComplete(Script script)
            {
                scriptPlayer.OnStop -= OnComplete;
                Debug.Log($"Script {script} completed!");
                AfterCommands.ExecuteAll(context);
            }
        }

    }
}