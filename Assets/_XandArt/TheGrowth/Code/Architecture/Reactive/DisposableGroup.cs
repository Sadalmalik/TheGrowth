using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace XandArt.Architecture
{
    public class DisposableGroup : IDisposable, IEnumerable, IEnumerable<IDisposable>
    {
        private Type m_Type;
        private bool m_IsDisposed;
        private bool m_IsClearing;
        private List<IDisposable> _children;
        
        public DisposableGroup(Type type, params IDisposable[] disposables)
        {
            m_Type = type;
            m_IsDisposed = false;
            m_IsClearing = false;
            _children = new List<IDisposable>();
            _children.AddRange(disposables);
        }

        public void Add(IDisposable disposable)
        {
            Assert.IsFalse(m_IsClearing, $"Detect Add() while clearing ({m_Type})");
            Assert.IsFalse(m_IsDisposed, $"Detect Add() into disposed object ({m_Type})");
            Assert.IsNotNull(disposable, $"Trying to add null ({m_Type})");
            _children.Add(disposable);
        }

        public void Remove(IDisposable disposable)
        {
            Assert.IsFalse(m_IsClearing, $"Detect Remove() while clearing ({m_Type})");
            Assert.IsFalse(m_IsDisposed, $"Detect Remove() from disposed object ({m_Type})");
            Assert.IsNotNull(disposable, $"Trying to remove null ({m_Type})");
            _children.Remove(disposable);
        }
        
        public IEnumerator<IDisposable> GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Dispose()
        {
            Assert.IsFalse(m_IsClearing, $"Detect Clear() recursive call ({m_Type})");
            m_IsClearing = true;
            for(int i=_children.Count-1;i>=0;i--)
                _children[i]?.Dispose();
            m_IsClearing = false;
            m_IsDisposed = true;
        }
    }
}