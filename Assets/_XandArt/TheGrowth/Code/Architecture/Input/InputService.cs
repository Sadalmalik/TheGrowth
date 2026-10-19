using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Architecture.Input
{
    /// <summary>
    /// Блокировка игрового ввода (глобальная область): счётчик владельцев. Блокирует <c>GameFlow</c> на время
    /// переходов, проверяют обработчики ввода сцены (перетаскивание карт, камера). UI этот флаг не читает: на время
    /// перехода UI закрыт экраном загрузки. Понятий игры здесь нет, поэтому сервис в Architecture — ниже и Game
    /// (кто блокирует), и Presentation (кто читает).
    /// </summary>
    public sealed class InputService : SharedObject
    {
        private readonly List<object> m_Blockers = new List<object>();

        public bool IsBlocked => m_Blockers.Count > 0;

        /// <summary>Текущие владельцы блокировки (для отладки); один владелец может блокировать несколько раз.</summary>
        public IReadOnlyList<object> Blockers => m_Blockers;

        /// <summary>Заблокировать ввод до <see cref="IDisposable.Dispose"/> результата (повторный вызов — без эффекта).</summary>
        public IDisposable Block(object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            m_Blockers.Add(owner);
            return new BlockHandle(this, owner);
        }

        public override void Dispose()
        {
            m_Blockers.Clear();
        }

        private void Release(object owner)
        {
            m_Blockers.Remove(owner);
        }

        private sealed class BlockHandle : IDisposable
        {
            private InputService m_Service;
            private readonly object m_Owner;

            public BlockHandle(InputService service, object owner)
            {
                m_Service = service;
                m_Owner = owner;
            }

            public void Dispose()
            {
                m_Service?.Release(m_Owner);
                m_Service = null;
            }
        }
    }
}
