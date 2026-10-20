// Source code by Kaleb Sadalmalik
// Link: https://github.com/Sadalmalik/Autumn/tree/master/Autumn.IOC

namespace XandArt.Architecture.IOC
{
    public interface ISharedInterface
    {
    }

    public interface IShared : ISharedInterface
    {
        void Init();

        void Dispose();
    }

    public class SharedObject : IShared
    {
        [Inject]
        public Container container;

        public virtual void Init()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}