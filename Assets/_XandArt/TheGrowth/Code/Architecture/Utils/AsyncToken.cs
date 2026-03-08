using System.Threading.Tasks;

namespace XandArt.Architecture.Utils
{
    public class AsyncToken
    {
        private TaskCompletionSource<bool> _tcs = new();

        public Task Task => _tcs.Task;

        public void Complete()
        {
            _tcs.SetResult(true);
        }
    }
}