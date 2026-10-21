using System;
using R3;

namespace TheGrowth.Architecture.MVVM
{
    /// <summary>
    /// Время жизни подписок: <c>x.AddTo(view)</c> — до отвязки вьюхи, <c>x.AddTo(viewModel)</c> — до уничтожения VM.
    /// Перегрузка для <see cref="ViewBase"/> точнее, чем <c>AddTo(Component)</c> из R3.Unity (та снимает подписку только
    /// при уничтожении объекта, а вьюхи переиспользуются пулом).
    /// </summary>
    public static class MvvmExtensions
    {
        public static T AddTo<T>(this T disposable, ViewBase view) where T : IDisposable
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            view.AddSubscription(disposable);
            return disposable;
        }

        public static T AddTo<T>(this T disposable, ViewModel viewModel) where T : IDisposable
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            viewModel.AddDisposable(disposable);
            return disposable;
        }
    }

    /// <summary>
    /// Свойство окна (<c>ReactiveProperty&lt;XxxVM&gt;</c> корневой VM): окно существует, пока в свойстве есть VM.
    /// <see cref="Open{T}"/> кладёт VM (прежняя уничтожается), уничтожение VM обнуляет свойство — закрыть окно можно и
    /// <see cref="Close{T}"/>, и <see cref="IDisposable.Dispose"/> самой VM.
    /// </summary>
    public static class ViewModelPropertyExtensions
    {
        public static T Open<T>(this ReactiveProperty<T> property, T viewModel) where T : ViewModel
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            if (viewModel.IsDisposed)
                throw new ObjectDisposedException(viewModel.GetType().Name, $"Can't open disposed {viewModel.GetType().Name}");

            var previous = property.Value;
            if (ReferenceEquals(previous, viewModel))
                return viewModel;

            previous?.Dispose();
            viewModel.Disposing += _ =>
            {
                if (ReferenceEquals(property.Value, viewModel))
                    property.Value = null;
            };

            property.Value = viewModel;
            return viewModel;
        }

        /// <summary>Уничтожить VM в свойстве (свойство обнулится); свойство пусто — без эффекта.</summary>
        public static void Close<T>(this ReactiveProperty<T> property) where T : ViewModel
        {
            property.Value?.Dispose();
        }
    }
}
