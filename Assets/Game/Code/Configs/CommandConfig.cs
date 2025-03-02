using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CommandConfig",
        menuName = "[Game]/Commands",
        order = 0)]
    public class CommandConfig : SerializedScriptableObject
    {
        public List<Command> Commands = new List<Command>();
    }
}