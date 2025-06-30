using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public abstract class DialogNode
    {
        public string next;
    }

    public class ReplicaNode : DialogNode
    {
        public string text;
    }

    public class ChoiceNode : DialogNode
    {
        public string text;

        [TableList]
        public List<Choice> choices;

        public class Choice
        {
            public string text;
            public string branch;
        }
    }

    public class DialogBranch
    {
        public string id;
        public List<DialogNode> nodes = new List<DialogNode>();
    }
    
    [CreateAssetMenu(
        fileName = "Dialog",
        menuName = "[Game]/Configs/Dialog",
        order = 2)]
    public class Dialog : ScriptableAsset
    {
        public List<DialogBranch> branches;
    }
}