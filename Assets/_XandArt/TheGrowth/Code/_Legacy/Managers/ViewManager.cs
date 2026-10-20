using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public interface IViewComponent
    {
        public GameObject GetPrefab();
        public GameObject View { get; internal set; }
    }

    public class ViewManager : IShared
    {
        [Inject]
        private GameManager _gameManager;

        private Dictionary<Entity, EntityViewConnector> _views;
        
        public void Init()
        {
            _views = new Dictionary<Entity, EntityViewConnector>();
            
            _gameManager.OnGameStateLoaded += HandleLoad;
            _gameManager.OnGameStateWillBeUnload += HandleUnload;
        }

        public void Dispose()
        {
            _gameManager.OnGameStateLoaded -= HandleLoad;
            _gameManager.OnGameStateWillBeUnload -= HandleUnload;
        }

        public void OnGameLoad()
        {
            
        }
        
        private void HandleLoad(GameState state)
        {
            state.OnEntityAdded += HandleEntityAdded;
            state.OnEntityRemoved -= HandleEntityRemoved;

            foreach (var entity in state.Entities)
                HandleEntityAdded(entity);
        }

        private void HandleUnload(GameState state)
        {
            state.OnEntityAdded -= HandleEntityAdded;
            state.OnEntityRemoved -= HandleEntityRemoved;

            foreach (var entity in state.Entities)
                HandleEntityRemoved(entity);
        }

        private void HandleEntityAdded(Entity entity)
        {
            var com = entity as CompositeEntity;
            if (com == null) return;
            var views = com.GetInterfaces<IViewComponent>();
            foreach (var viewComponent in views)
            {
                var prefab = viewComponent.GetPrefab();
                if (prefab == null) continue;
                var pool = ObjectPoolService.GetPool(prefab);
                //pool.Get()

            }
        }

        private void HandleEntityRemoved(Entity entity)
        {
            
        }

        private class EntityViewConnector
        {
            public Entity Entity;
            public List<GameObject> Views;
        }
    }
}