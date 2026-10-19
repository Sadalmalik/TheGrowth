using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Game.Scenes;
using TheGrowth.Tests.EditMode.Locations;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.Flow
{
    /// <summary>
    /// Сцены без SceneManager: «сцена» — фабрика корневых объектов в памяти. Пишет load / unload в журнал;
    /// <see cref="LoadGate"/> задерживает загрузку до <see cref="Gate.Open"/>. Выгрузка уничтожает объекты сцены.
    /// </summary>
    public sealed class FakeSceneService : SharedObject, ISceneService
    {
        private readonly Dictionary<string, Func<GameObject[]>> m_Scenes = new Dictionary<string, Func<GameObject[]>>();
        private readonly Dictionary<string, GameObject[]> m_Loaded = new Dictionary<string, GameObject[]>();
        private readonly List<string> m_Journal;

        public FakeSceneService(List<string> journal)
        {
            m_Journal = journal;
        }

        /// <summary>Ожидание каждой загрузки; null — загрузка завершается сразу.</summary>
        public Gate LoadGate { get; set; }

        public IReadOnlyCollection<string> Loaded => m_Loaded.Keys;

        public string LastActivated { get; private set; }

        public void Add(string sceneName, Func<GameObject[]> createRoots)
        {
            m_Scenes.Add(sceneName, createRoots);
        }

        public async UniTask<IReadOnlyList<GameObject>> LoadAdditiveAsync(string sceneName, bool activate, CancellationToken token)
        {
            m_Journal.Add($"load:{sceneName}");
            if (LoadGate != null)
                await LoadGate.Wait(token);

            if (!m_Scenes.TryGetValue(sceneName, out var createRoots))
                throw new InvalidOperationException($"Scene '{sceneName}' is not in the fake build");

            var roots = createRoots();
            m_Loaded.Add(sceneName, roots);
            if (activate)
                LastActivated = sceneName;

            return roots;
        }

        public UniTask UnloadAsync(string sceneName)
        {
            m_Journal.Add($"unload:{sceneName}");
            if (m_Loaded.TryGetValue(sceneName, out var roots))
            {
                m_Loaded.Remove(sceneName);
                LocationTestKit.Destroy(roots);
            }

            return UniTask.CompletedTask;
        }

        public void DestroyAll()
        {
            foreach (var roots in m_Loaded.Values)
                LocationTestKit.Destroy(roots);

            m_Loaded.Clear();
        }
    }
}
