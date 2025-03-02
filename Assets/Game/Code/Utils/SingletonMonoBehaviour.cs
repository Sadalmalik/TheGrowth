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
    }
}