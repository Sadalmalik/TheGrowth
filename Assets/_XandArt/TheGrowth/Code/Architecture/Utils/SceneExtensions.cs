using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XandArt.Architecture.Utils
{
    public static class SceneExtensions
    {
        public static T Find<T>(this Scene scene) where T : Component
        {
            var component = default(T);
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                component = root.GetComponentInChildren<T>();
                if (component != null) return component;
            }
            return null;
        }
        
        public static List<T> FindAll<T>(this Scene scene, List<T> list = null) where T : Component
        {
            var components = list ?? new List<T>();
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                components.AddRange(root.GetComponentsInChildren<T>());
            return components;
        }
    }
}