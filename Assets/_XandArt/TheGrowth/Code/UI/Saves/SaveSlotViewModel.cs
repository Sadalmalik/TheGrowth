using System;
using Cysharp.Threading.Tasks;
using R3;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Saves;
using TheGrowth.UI.Texts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.UI.Saves
{
    /// <summary>
    /// Строка списка слотов: дата, вид, локация, время в игре, превью (<c>thumbnail.png</c>, читается отдельно от
    /// списка). Действия передаёт окну (<see cref="SaveLoadViewModel"/>). Текстура превью уничтожается вместе с VM.
    /// </summary>
    public sealed class SaveSlotViewModel : ViewModel
    {
        private readonly SaveLoadViewModel m_Owner;
        private readonly ReactiveProperty<Texture2D> m_Thumbnail = new ReactiveProperty<Texture2D>();

        public SaveSlotViewModel(SaveLoadViewModel owner, SaveSlotInfo info, SaveService saves)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Info = info ?? throw new ArgumentNullException(nameof(info));
            Title = info.IsBroken ? UITexts.Get(UITexts.SlotBroken) : UITexts.SlotDate(info.Meta.SavedAtUtc);
            Details = info.IsBroken ? info.SlotId : UITexts.SlotDetails(info.Meta);
            if (!info.IsBroken && saves != null)
                LoadThumbnailAsync(saves).Forget();
        }

        public SaveSlotInfo Info { get; }

        public string SlotId => Info.SlotId;

        public string Title { get; }

        public string Details { get; }

        public bool IsBroken => Info.IsBroken;

        public bool IsAutosave => Info is { IsBroken: false, Meta: { Kind: SaveSlotKind.Auto } };

        /// <summary>Превью или null (нет, ещё читается, не читается).</summary>
        public ReadOnlyReactiveProperty<Texture2D> Thumbnail => m_Thumbnail;

        public bool CanLoad => m_Owner.Mode == SaveLoadMode.Load && !IsBroken;

        /// <summary>Перезаписать можно только ручной слот и только в окне сохранения.</summary>
        public bool CanOverwrite => m_Owner.Mode == SaveLoadMode.Save && !IsBroken && !IsAutosave;

        public void Load()
        {
            m_Owner.Load(this);
        }

        public void Overwrite()
        {
            m_Owner.Overwrite(this);
        }

        public void Delete()
        {
            m_Owner.Delete(this);
        }

        protected override void OnDispose()
        {
            var texture = m_Thumbnail.Value;
            m_Thumbnail.Dispose();
            if (texture == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(texture);
            else
                Object.DestroyImmediate(texture);
        }

        private async UniTaskVoid LoadThumbnailAsync(SaveService saves)
        {
            byte[] bytes;
            try
            {
                bytes = await saves.LoadThumbnailAsync(SlotId, Lifetime);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (IsDisposed || bytes is not { Length: > 0 })
                return;

            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "Thumbnail " + SlotId };
            if (texture.LoadImage(bytes, true))
            {
                m_Thumbnail.Value = texture;
                return;
            }

            Debug.LogWarning($"[UI] Thumbnail of slot '{SlotId}' is not a valid image");
            Object.DestroyImmediate(texture);
        }
    }
}
