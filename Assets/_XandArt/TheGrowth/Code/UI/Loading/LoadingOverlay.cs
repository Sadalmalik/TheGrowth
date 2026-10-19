using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.UI.Loading
{
    /// <summary>
    /// ВРЕМЕННО (до UI на MVVM, E08): экран загрузки — полноэкранная панель поверх UI сцены Main, перекрывает и клики.
    /// Показ — активация своего объекта. Регистрируется в глобальном контейнере как <see cref="ILoadingScreen"/>.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour, ILoadingScreen, IShared
    {
        public bool IsVisible => this != null && gameObject.activeSelf;

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        void IShared.Init()
        {
        }

        void IShared.Dispose()
        {
        }

        private void SetVisible(bool visible)
        {
            // Объект уничтожается вместе со сценой Main раньше глобальной области.
            if (this != null)
                gameObject.SetActive(visible);
        }
    }
}
