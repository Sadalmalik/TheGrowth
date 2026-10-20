// Source code by Kaleb Sadalmalik
// Link: https://github.com/Sadalmalik/Autumn/tree/master/Autumn.IOC

using System;

namespace XandArt.Architecture.IOC
{
    public class ContainerTypeException : Exception
    {
        public ContainerTypeException(string message = null) : base(message)
        {
        }
    }

    public class ContainerInitializationException : Exception
    {
        public ContainerInitializationException(string message = null) : base(message)
        {
        }
    }
}