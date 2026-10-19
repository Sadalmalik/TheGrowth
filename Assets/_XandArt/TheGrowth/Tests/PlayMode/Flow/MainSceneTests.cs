using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Game.Flow;
using TheGrowth.Presentation.Locations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.PlayMode.Flow
{
    /// <summary>
    /// Сцена Main целиком: точка входа → главное меню → новая игра в тестовой локации → выход в меню. Main грузится
    /// additive (сцена запуска тестов остаётся), сцены должны быть в Build Settings.
    /// </summary>
    public sealed class MainSceneTests
    {
        private const string MainScene = "Main";
        private const string TestLocationScene = "TestLocation";
        private const string TestLocationId = "TestLocation";
        private const float Timeout = 30f;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var name in new[] { TestLocationScene, MainScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid() && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator NewGame_EntersTestLocation_ExitToMenu_UnloadsLocationAndDisposesSession()
        {
            yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Additive);
            var starter = FindStarter();

            yield return WaitUntil(() => starter.Flow is { State: FlowState.MainMenu, IsTransitioning: false }, "main menu");
            var flow = starter.Flow;

            flow.Request(new NewGameRequest(TestLocationId)).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.Hub, IsTransitioning: false }, "test location");

            var scene = SceneManager.GetSceneByName(TestLocationScene);
            Assert.IsTrue(scene.isLoaded, "the location scene is loaded");
            Assert.AreEqual(scene, SceneManager.GetActiveScene(), "the location scene is active");

            var behaviours = FindBehaviours(scene);
            Assert.That(behaviours.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(behaviours, Has.All.Matches<LocationLogBehaviour>(behaviour => behaviour.IsInitialized), "all behaviours are initialized");
            Assert.That(behaviours, Has.Some.Matches<LocationLogBehaviour>(behaviour => !behaviour.gameObject.activeInHierarchy), "an inactive behaviour is initialized too");

            var session = flow.Session;
            var location = flow.Location;
            Assert.IsNotNull(session);
            Assert.AreEqual(TestLocationId, location.Model.Id);
            Assert.IsTrue(flow.CanSave);

            flow.Request(new ExitToMenuRequest()).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.MainMenu, IsTransitioning: false }, "exit to menu");

            Assert.IsFalse(SceneManager.GetSceneByName(TestLocationScene).isLoaded, "the location scene is unloaded");
            Assert.IsTrue(location.IsDisposed);
            Assert.IsTrue(session.IsDisposed);
            Assert.IsNull(flow.Session);
            Assert.That(behaviours, Has.All.Matches<LocationLogBehaviour>(behaviour => behaviour.IsDisposed && behaviour.WasCancelledOnDispose));
        }

        private static GameStarter FindStarter()
        {
            var main = SceneManager.GetSceneByName(MainScene);
            Assert.IsTrue(main.isLoaded, "Main is loaded");
            foreach (var root in main.GetRootGameObjects())
            {
                if (root.TryGetComponent<GameStarter>(out var starter))
                    return starter;
            }

            Assert.Fail($"No {nameof(GameStarter)} on root objects of {MainScene}");
            return null;
        }

        private static List<LocationLogBehaviour> FindBehaviours(Scene scene)
        {
            var behaviours = new List<LocationLogBehaviour>();
            foreach (var root in scene.GetRootGameObjects())
                behaviours.AddRange(root.GetComponentsInChildren<LocationLogBehaviour>(true));

            return behaviours;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what)
        {
            var deadline = Time.realtimeSinceStartup + Timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timeout ({Timeout} s) waiting for: {what}");

                yield return null;
            }
        }
    }
}
