using System;
using Sirenix.OdinInspector;

namespace XandArt.Architecture
{
    /// <summary>
    /// Variation of Scriptable object for linking purposes
    /// </summary>
    public class ScriptableAsset : SerializedScriptableObject
    {
        [ReadOnly]
        public Guid Guid;
    }
}