using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public interface ITickable
    {
        public void Tick();
    }
    
    public class TickManager : SerializedMonoBehaviour, IShared
    {
        private List<ITickable> _tickables;

        [Inject]
        private Container _container;
        
        public void Init()
        {
            _tickables = _container.GetAll<ITickable>();
        }

        public void Dispose()
        {
        }

        private void Update()
        {
            if (_tickables == null) return;
            foreach (var tickable in _tickables)
            {
                try
                {
                    tickable.Tick();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }
    }
}