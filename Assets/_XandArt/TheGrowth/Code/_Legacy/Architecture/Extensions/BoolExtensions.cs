using System;

namespace XandArt.Architecture
{
    public static class BoolExtensions
    {
        public static void Trigger(this ref bool trigger, Action action)
        {
            if (!trigger) return;
            trigger = false;
            action?.Invoke();
        }
    }
}