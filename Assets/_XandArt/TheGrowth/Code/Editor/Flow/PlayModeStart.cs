using System;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Game.Flow;
using TheGrowth.Model.Locations;
using TheGrowth.Presentation.Locations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheGrowth.EditorTools.Flow
{
    /// <summary>
    /// Play в редакторе всегда начинается со сцены Main (<see cref="EditorSceneManager.playModeStartScene"/>). Если перед
    /// Play была открыта сцена локации (на корне — <see cref="LocationRoot"/>, на неё ссылается <see cref="LocationModel"/>),
    /// <c>GameStarter</c> начинает новую игру сразу в этой локации (<see cref="EditorStartHook"/>). ID локации
    /// переживает перезагрузку домена через <see cref="SessionState"/>.
    /// Исключения: несохранённая сцена и сцена запуска PlayMode-тестов играются как есть (стартовая сцена не задаётся).
    /// Пресет локации (<c>StoryStep</c>, T058) — точка расширения в <see cref="CreateStartRequest"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStart
    {
        public const string MainScenePath = "Assets/_XandArt/TheGrowth/Scenes/Main.unity";

        private const string LocationKey = "TheGrowth.PlayModeStart.Location";

        // UnityEngine.TestTools.TestRunner.PlaymodeTestsController.kPlaymodeTestControllerName (internal).
        private const string TestRunnerObjectName = "Code-based tests runner";
        private const string TestRunnerScenePrefix = "Assets/InitTestScene";

        static PlayModeStart()
        {
            EditorStartHook.CreateRequest = CreateStartRequest;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
            EditorSceneManager.sceneSaved += HandleSceneSaved;
            EditorApplication.delayCall += RefreshStartScene;
        }

        /// <summary>Location ID для старта из сцены (после последнего входа в Play) или null.</summary>
        public static string PendingLocationId
        {
            get
            {
                var id = SessionState.GetString(LocationKey, string.Empty);
                return string.IsNullOrEmpty(id) ? null : id;
            }
        }

        /// <summary>Локация, сцена которой открыта в редакторе: сначала активная сцена, затем остальные.</summary>
        public static LocationModel FindOpenLocation()
        {
            var active = SceneManager.GetActiveScene();
            var location = FindLocation(active);
            if (location != null)
                return location;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == active)
                    continue;

                location = FindLocation(scene);
                if (location != null)
                    return location;
            }

            return null;
        }

        /// <summary>Локация сцены: корень <see cref="LocationRoot"/> и <see cref="LocationModel"/> с её именем.</summary>
        public static LocationModel FindLocation(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                return null;

            if (!LocationRoot.TryFind(scene.GetRootGameObjects(), out _, out _))
                return null;

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(LocationModel)))
            {
                var model = AssetDatabase.LoadAssetAtPath<LocationModel>(AssetDatabase.GUIDToAssetPath(guid));
                if (model != null && model.SceneName == scene.name)
                    return model;
            }

            Debug.LogWarning($"[Flow] Scene '{scene.name}' has a {nameof(LocationRoot)}, but no {nameof(LocationModel)} references it: Play starts from the main menu");
            return null;
        }

        private static IFlowRequest CreateStartRequest()
        {
            var id = PendingLocationId;
            if (id == null)
                return null;

            Debug.Log($"[Flow] Play from location scene: new game in '{id}'");
            return new NewGameRequest(id);
        }

#region Start scene

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                ApplyStartScene();
        }

        private static void HandleActiveSceneChanged(Scene previous, Scene active)
        {
            RefreshStartScene();
        }

        private static void HandleSceneSaved(Scene scene)
        {
            RefreshStartScene();
        }

        private static void RefreshStartScene()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                SetStartScene(IsPlayedAsIs(SceneManager.GetActiveScene()) ? null : LoadMainScene());
        }

        /// <summary>Перед входом в Play: стартовая сцена и локация для старта из сцены.</summary>
        private static void ApplyStartScene()
        {
            SessionState.EraseString(LocationKey);
            if (IsPlayedAsIs(SceneManager.GetActiveScene()))
            {
                SetStartScene(null);
                return;
            }

            var main = LoadMainScene();
            SetStartScene(main);
            if (main == null)
                return;

            var location = FindOpenLocation();
            if (location == null)
                return;

            if (string.IsNullOrEmpty(location.Id))
            {
                Debug.LogWarning($"[Flow] {location} has an empty id: run [TheGrowth]/Content/Rebuild Registry. Play starts from the main menu");
                return;
            }

            SessionState.SetString(LocationKey, location.Id);
        }

        private static SceneAsset LoadMainScene()
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        }

        private static void SetStartScene(SceneAsset scene)
        {
            if (EditorSceneManager.playModeStartScene != scene)
                EditorSceneManager.playModeStartScene = scene;
        }

        /// <summary>Несохранённая сцена или сцена запуска PlayMode-тестов: Play без Main.</summary>
        private static bool IsPlayedAsIs(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                return true;

            if (scene.path.StartsWith(TestRunnerScenePrefix, StringComparison.Ordinal))
                return true;

            if (!scene.isLoaded)
                return false;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == TestRunnerObjectName)
                    return true;
            }

            return false;
        }

#endregion
    }
}
