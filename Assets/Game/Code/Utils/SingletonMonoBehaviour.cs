using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class SingletonMonoBehaviour<T> : SerializedMonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject o = new GameObject(typeof(T).Name);
                    _instance = o.AddComponent<T>();
                    DontDestroyOnLoad(o);
                    _instance.Initialize();
                }

                return _instance;
            }
        }

        protected virtual void Initialize()
        {
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.LogError(
                    $"Can be only one instance of {typeof(T).Name}!\nObject {gameObject} will be turned off!");
                gameObject.SetActive(false);
                return;
            }

            _instance = (T) this;
        }
    }
}