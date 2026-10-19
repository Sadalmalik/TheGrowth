#if UNITY_EDITOR
using System;
using TheGrowth.Game.Flow;

namespace TheGrowth.Game.Bootstrap
{
    /// <summary>
    /// Только редактор: первый запрос вместо главного меню — Play из сцены локации (T058). Делегат ставит сборка
    /// TheGrowth.Editor при загрузке домена, runtime-код на UnityEditor не ссылается; в билде класса нет.
    /// Статика — канал «редактор → сцена Main»: значение выбирается до входа в Play, когда объектов Main ещё нет.
    /// Пресеты (<c>StoryStep</c>) встанут в запрос, который возвращает делегат.
    /// </summary>
    public static class EditorStartHook
    {
        /// <summary>Запрос старта или null (обычный старт с главного меню).</summary>
        public static Func<IFlowRequest> CreateRequest { get; set; }
    }
}
#endif
