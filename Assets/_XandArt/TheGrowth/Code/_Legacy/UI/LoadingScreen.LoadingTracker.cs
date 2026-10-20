using System;
using System.Linq;
using System.Threading.Tasks;

namespace XandArt.TheGrowth
{
    /// Bridge between loading process and UI
    public sealed class LoadingTracker : IDisposable, IProgress<float>
    {
        public static event Func<LoadingTracker, Task> Created;
        public static event Func<LoadingTracker, Task> Disposed;

        public static LoadingTracker Create(string tag = null!, object data = null!)
            => CreateAsync(tag, data).Result;
        
        public static async Task<LoadingTracker> CreateAsync(string tag = null!, object data = null!)
        {
            var result = new LoadingTracker(tag, data);
            await InvokeAsync(Created, result);
            return result;
        }

        private static Task InvokeAsync<T>(Func<T, Task> handler, T argument)
        {
            if (handler == null)
                return Task.CompletedTask;

            var tasks = handler
                .GetInvocationList()
                .OfType<Func<T, Task>>()
                .Select(h => h(argument));

            return Task.WhenAll(tasks);
        }

        public string Tag { get; }
        public object Data { get; }

        private float _progress;

        private LoadingTracker(string tag, object data)
        {
            Tag = tag;
            Data = data;
        }

        public void Dispose()
            => InvokeAsync(Disposed, this);
        
        public void Report(float value)
            => _progress = value;

        public float GetProgress()
            => _progress;
    }
}