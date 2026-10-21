#if UNITY_EDITOR
using System;
using TheGrowth.Game.Flow;

namespace TheGrowth.Game.Bootstrap
{
    /// <summary>
    /// Только редактор: подмены старта сцены Main. Первый запрос вместо главного меню — Play из сцены локации (T058):
    /// делегат ставит сборка TheGrowth.Editor при загрузке домена, runtime-код на UnityEditor не ссылается; в билде
    /// класса нет. Папки сохранений и профиля — PlayMode-тесты (T068, T052). Статика — канал «редактор, тесты → сцена Main»: значение
    /// выбирается до загрузки Main, когда её объектов ещё нет. Пресеты (<c>StoryStep</c>) встанут в запрос, который
    /// возвращает делегат.
    /// </summary>
    public static class EditorStartHook
    {
        /// <summary>Запрос старта или null (обычный старт с главного меню).</summary>
        public static Func<IFlowRequest> CreateRequest { get; set; }

        /// <summary>
        /// Папка сохранений вместо папки игрока (<c>SaveFileStorage.DefaultFolder</c>) или null. Ставят PlayMode-тесты:
        /// тесты не трогают сохранения игрока.
        /// </summary>
        public static string SavesFolder { get; set; }

        /// <summary>
        /// Папка профиля игрока вместо папки игрока (<c>ProfileFileStorage.DefaultFolder</c>) или null. Ставят PlayMode-тесты:
        /// тесты не трогают профиль игрока.
        /// </summary>
        public static string ProfileFolder { get; set; }
    }
}
#endif
