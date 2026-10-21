using System;
using System.IO;
using Newtonsoft.Json;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Variables;
using UnityEngine;

namespace TheGrowth.Core.Profile
{
    /// <summary>
    /// Профиль игрока (глобальная область): переменные области «профиль» — общие для всех слотов (открытые концовки,
    /// галерея). Читается из <see cref="IProfileStorage"/> при <c>Init</c> глобальной области; записывается целиком сразу
    /// при каждом изменении значения (изменения редкие, запись атомарная: сбой посреди игры не теряет уже открытое).
    /// Ошибка чтения или записи — в лог, игра продолжается. Нечитаемый профиль сохраняется копией рядом
    /// (<see cref="IProfileStorage.KeepCopy"/>) и начинается с значений по умолчанию. Профиль более новой версии игры и
    /// нечитаемый профиль, копию которого сделать не удалось, не перезаписываются (<see cref="IsReadOnly"/>).
    /// Требует в контейнере <see cref="IProfileStorage"/>, <see cref="VariableCatalog"/>, <see cref="EventBus"/>.
    /// </summary>
    public sealed class ProfileService : SharedObject, IProfileVariables
    {
        public const int Version = 1;

        private const string LogSource = "[Profile] Profile";

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private IProfileStorage m_Storage;

        [Inject]
        private VariableCatalog m_Catalog;

        [Inject]
        private EventBus m_Events;
#pragma warning restore CS0649

        public VariableStore Variables { get; private set; }

        /// <summary>Изменения не записываются: перезапись уничтожила бы данные, которые эта версия не читает.</summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>Сколько раз профиль записан (для тестов).</summary>
        internal int WriteCount { get; private set; }

        public override void Init()
        {
            Variables = new VariableStore(VariableScope.Profile, m_Events);
            Load();
            Variables.Changed += HandleChanged;
        }

        public override void Dispose()
        {
            if (Variables != null)
                Variables.Changed -= HandleChanged;
        }

        /// <summary>Записать профиль сейчас. Ошибка — в лог.</summary>
        public void Save()
        {
            if (IsReadOnly)
            {
                Debug.LogWarning("[Profile] Profile is read-only, the change is not written (see the profile warning at startup)");
                return;
            }

            var save = new ProfileSave { Version = Version, Variables = VariableValues.Write(Variables) };
            try
            {
                m_Storage.Write(stream => SaveJson.Write(stream, save));
                WriteCount++;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Profile] Profile is not written: {exception.Message}\n{exception}");
            }
        }

        private void Load()
        {
            ProfileSave save;
            try
            {
                using (var stream = m_Storage.OpenRead())
                    save = stream == null ? null : SaveJson.Read<ProfileSave>(stream);
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Profile] Profile can't be read, default values are used: {exception.Message}");
                KeepUnreadable();
                save = null;
            }

            if (save != null && save.Version > Version)
            {
                IsReadOnly = true;
                Debug.LogWarning($"[Profile] Profile version {save.Version} is newer than supported {Version}: known variables are read, the profile is not overwritten");
            }

            VariableValues.Read(Variables, m_Catalog, save?.Variables, LogSource);
        }

        /// <summary>Копия нечитаемого профиля; не удалась — профиль не перезаписывается.</summary>
        private void KeepUnreadable()
        {
            try
            {
                var copy = m_Storage.KeepCopy("unreadable");
                if (copy != null)
                    Debug.LogWarning($"[Profile] The unreadable profile is kept as {copy}");
            }
            catch (Exception exception)
            {
                IsReadOnly = true;
                Debug.LogError($"[Profile] The unreadable profile can't be copied, the profile is not overwritten: {exception.Message}");
            }
        }

        private void HandleChanged(VariableChanged change)
        {
            Save();
        }
    }
}
