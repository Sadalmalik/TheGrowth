using System;
using R3;
using TheGrowth.Architecture.IOC;
using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Loading;
using TheGrowth.UI.MainMenu;
using TheGrowth.UI.Pause;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Saves;

namespace TheGrowth.UI.Root
{
    /// <summary>
    /// Корневая VM UI (глобальная область): свойство на слой (<see cref="UILayer"/>), окно слоя существует, пока
    /// свойство не null. Открыть окно — <c>Layer.Open(vm)</c>, закрыть — <c>Layer.Close()</c> или <c>vm.Close()</c>
    /// (<see cref="ViewModelPropertyExtensions"/>). Кто подписан на свойства — <see cref="UIManager"/> (явный список,
    /// без рефлексии).
    /// </summary>
    public sealed class UIRootViewModel : ViewModel, IShared
    {
        public ReactiveProperty<MainMenuViewModel> MainMenu { get; } = new ReactiveProperty<MainMenuViewModel>();

        public ReactiveProperty<PauseViewModel> Pause { get; } = new ReactiveProperty<PauseViewModel>();

        public ReactiveProperty<SaveLoadViewModel> SaveLoad { get; } = new ReactiveProperty<SaveLoadViewModel>();

        public ReactiveProperty<PopupViewModel> Popup { get; } = new ReactiveProperty<PopupViewModel>();

        public ReactiveProperty<LoadingViewModel> Loading { get; } = new ReactiveProperty<LoadingViewModel>();

        /// <summary>Окно слоя или null.</summary>
        public WindowViewModel Get(UILayer layer)
        {
            return layer switch
            {
                UILayer.MainMenu => MainMenu.Value,
                UILayer.Pause => Pause.Value,
                UILayer.SaveLoad => SaveLoad.Value,
                UILayer.Popup => Popup.Value,
                UILayer.Loading => Loading.Value,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null),
            };
        }

        void IShared.Init()
        {
        }

        /// <summary>Окна закрываются сверху вниз, затем свойства завершаются.</summary>
        protected override void OnDispose()
        {
            Loading.Close();
            Popup.Close();
            SaveLoad.Close();
            Pause.Close();
            MainMenu.Close();

            Loading.Dispose();
            Popup.Dispose();
            SaveLoad.Dispose();
            Pause.Dispose();
            MainMenu.Dispose();
        }
    }
}
