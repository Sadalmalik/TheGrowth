using System;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class AutosaveManager : IShared, ITickable
    {
        public const float AutosaveDelay = 5 * 60;

        [Inject]
        private PersistenceManager _persistenceManager;
        
        [Inject]
        private GameManager _gameManager;
        
        private float _nextTime;
        
        public void Init()
        {
            _nextTime = Time.unscaledTime + AutosaveDelay;
        }

        public void Dispose()
        {
        }

        public void Tick()
        {
            if (_nextTime <= Time.unscaledTime)
            {
                _nextTime = Time.unscaledTime + AutosaveDelay;
                if (_gameManager.CurrentGameState != null)
                {
                    var date = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");
                    _gameManager.SaveGame($"{date}-autosave");
                }
            }
        }
    }
}