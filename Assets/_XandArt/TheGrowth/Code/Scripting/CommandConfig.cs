using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CommandConfig",
        menuName = "[Game]/Commands",
        order = 0)]
    public class CommandConfig : ScriptableAsset
    {
        public List<Command> Commands = new List<Command>();
    }
}