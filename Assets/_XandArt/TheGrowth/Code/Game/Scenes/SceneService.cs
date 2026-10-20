using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheGrowth.Game.Scenes
{
    /// <summary>Сцены через <see cref="SceneManager"/>; сцена ищется по имени (имена сцен локаций уникальны).</summary>
    public sealed class SceneService : SharedObject, ISceneService
    {
        public async UniTask<IReadOnlyList<GameObject>> LoadAdditiveAsync(string sceneName, bool activate, CancellationToken token)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("Scene name is empty", nameof(sceneName));

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                throw new InvalidOperationException($"Scene '{sceneName}' can't be loaded: it is not in Build Settings");

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
                throw new InvalidOperationException($"Scene '{sceneName}' failed to start loading");

            await operation.ToUniTask(cancellationToken: token);

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Scene '{sceneName}' is not loaded after loading");

            if (activate)
                SceneManager.SetActiveScene(scene);

            return scene.GetRootGameObjects();
        }

        public async UniTask UnloadAsync(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"[Scenes] Scene '{sceneName}' is not loaded, nothing to unload");
                return;
            }

            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
                throw new InvalidOperationException($"Scene '{sceneName}' failed to start unloading");

            await operation.ToUniTask();
        }
    }
}
