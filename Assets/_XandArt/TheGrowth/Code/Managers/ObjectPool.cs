using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XandArt.TheGrowth
{
    public static class ObjectPoolService
    {
        private static Dictionary<Object, ObjectPool<Object>> _pools;

        static ObjectPoolService()
        {
            _pools = new Dictionary<Object, ObjectPool<Object>>();
        }

        public static ObjectPool<Object> GetPool(Object prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
                _pools[prefab] = pool = new ObjectPool<Object>(prefab);
            return pool;
        }
    }
    
    public class ObjectPool<TObject> where TObject : Object
    {
        private Transform _root;
        private TObject _prefab;

        private Queue<TObject> _pool;

        public ObjectPool(TObject prefab)
        {
            _prefab = prefab;
            _pool = new Queue<TObject>();
            
            var rootObject = new GameObject($"[pool:{prefab.name}]");
            Object.DontDestroyOnLoad(rootObject);
            _root = rootObject.transform;
            rootObject.SetActive(false);
        }

        public TObject Get()
        {
            var obj = _pool.Count > 0
                ? _pool.Dequeue()
                : Object.Instantiate<TObject>(_prefab);
            var go = GetOptionalGameObject(obj);
            if (go != null)
            {
                go.transform.SetParent(null);
                go.SetActive(true);
            }
            return obj;
        }

        public IDisposable Get(out TObject obj)
        {
            obj = Get();
            return new PoolDisposable(this, obj);
        }

        public void Free(TObject obj)
        {
            _pool.Enqueue(obj);
            var go = GetOptionalGameObject(obj);
            if (go != null)
            {
                go.transform.SetParent(_root);
                go.SetActive(false);
            }
        }

        private GameObject GetOptionalGameObject(TObject obj)
        {
            return
                obj switch
                {
                    MonoBehaviour beh => beh.gameObject,
                    GameObject gameObject => gameObject,
                    Transform trans => trans.gameObject,
                    _ => null
                };
        }

        private class PoolDisposable : IDisposable
        {
            private ObjectPool<TObject> _pool;
            private TObject _object;
            
            public PoolDisposable(ObjectPool<TObject> pool, TObject o)
            {
                _pool = pool;
                _object = o;
            }

            public void Dispose()
            {
                _pool.Free(_object);
            }
        }
    }
}