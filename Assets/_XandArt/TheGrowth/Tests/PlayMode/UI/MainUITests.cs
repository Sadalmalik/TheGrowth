using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Core.Saves;
using TheGrowth.Game.Bootstrap;
using TheGrowth.Game.Flow;
using TheGrowth.Model.Locations;
using TheGrowth.UI.MainMenu;
using TheGrowth.UI.Pause;
using TheGrowth.UI.Popups;
using TheGrowth.UI.Root;
using TheGrowth.UI.Saves;
using TheGrowth.UI.Texts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.PlayMode.UI
{
    /// <summary>
    /// UI сцены Main через кнопки вьюх и ввод «назад» (<see cref="UIManager.HandleInput"/>): главное меню → новая игра
    /// → пауза → сохранить → в меню → «Продолжить» → та же локация; загрузка слота новее клиента → всплывающее окно.
    /// Сохранения — во временной папке теста (<see cref="TestSavesFolder"/>); папку игрока тест не трогает.
    /// </summary>
    public sealed class MainUITests
    {
        private const string MainScene = "Main";
        private const string TestLocationScene = "TestLocation";
        private const string TestLocationId = "TestLocation";
        private const float Timeout = 30f;

        private readonly TestSavesFolder m_Saves = new TestSavesFolder();

        [SetUp]
        public void SetUp()
        {
            m_Saves.Create();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var name in new[] { TestLocationScene, MainScene })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid() && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
            }

            m_Saves.Delete();
        }

        [UnityTest]
        public IEnumerator MainMenu_NewGame_PauseSave_ExitToMenu_Continue_SameLocation()
        {
            yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Additive);
            var starter = FindStarter();
            yield return WaitUntil(() => starter.Flow is { State: FlowState.MainMenu, IsTransitioning: false }, "main menu");
            var flow = starter.Flow;
            var root = starter.Global.Get<UIRootViewModel>();
            var ui = starter.UIRoot;

            var menuView = (MainMenuView)ui.GetView(root.MainMenu.Value);
            Assert.IsNotNull(menuView, "the main menu window is shown");
            Assert.IsFalse(menuView.ContinueButton.gameObject.activeSelf, "no saves: «Continue» is hidden");
            Assert.AreSame(menuView.NewGameButton.gameObject, ui.EventSystem.currentSelectedGameObject, "gamepad focus on «New game»");

            menuView.NewGameButton.onClick.Invoke();
            yield return WaitUntil(() => flow is { State: FlowState.Hub, IsTransitioning: false }, "new game in the test location");
            Assert.IsNull(root.MainMenu.Value, "the main menu is closed in the game");
            Assert.IsNull(root.Loading.Value, "the loading screen is hidden");

            starter.UI.HandleInput(false, true);
            var pause = root.Pause.Value;
            Assert.IsNotNull(pause, "Esc / Start opens the pause");
            var pauseView = (PauseView)ui.GetView(pause);
            Assert.AreSame(pauseView.ResumeButton.gameObject, ui.EventSystem.currentSelectedGameObject);

            // Геймпад: навигация до «Сохранить» и нажатие.
            ui.EventSystem.SetSelectedGameObject(pauseView.SaveButton.gameObject);
            pauseView.SaveButton.onClick.Invoke();
            var slots = root.SaveLoad.Value;
            Assert.AreEqual(SaveLoadMode.Save, slots.Mode);
            var slotsView = (SaveLoadView)ui.GetView(slots);
            slotsView.NewSaveButton.onClick.Invoke();
            yield return WaitUntil(() => slots.Slots.Count == 1 && !slots.IsBusy.CurrentValue, "a new manual slot in the list");
            Assert.AreEqual(1, slotsView.Slots.Views.Count, "a row widget for the slot");
            Assert.AreEqual(SaveSlotKind.Manual, slots.Slots[0].Info.Meta.Kind);

            starter.UI.HandleInput(true, false);
            Assert.IsTrue(slots.IsDisposed, "B / Esc closes the slots window");
            Assert.AreSame(pause, root.Pause.Value, "the pause stays open under it");
            Assert.AreSame(pauseView.SaveButton.gameObject, ui.EventSystem.currentSelectedGameObject, "focus returns to the pause");

            pauseView.ExitToMenuButton.onClick.Invoke();
            var confirm = root.Popup.Value;
            Assert.AreEqual(UITexts.Get(UITexts.ConfirmExitToMenu), confirm.Message);
            var popupView = (PopupView)ui.GetView(confirm);
            Assert.AreSame(popupView.Buttons.Views[1].Button.gameObject, ui.EventSystem.currentSelectedGameObject, "focus on «No»");
            popupView.Buttons.Views[0].Button.onClick.Invoke();
            yield return WaitUntil(() => flow is { State: FlowState.MainMenu, IsTransitioning: false }, "exit to menu");
            Assert.IsNull(root.Pause.Value, "the pause is closed with the session");
            Assert.IsFalse(SceneManager.GetSceneByName(TestLocationScene).isLoaded);

            var menu = root.MainMenu.Value;
            yield return WaitUntil(() => menu.CanContinue.CurrentValue, "«Continue» is available");
            menuView = (MainMenuView)ui.GetView(menu);
            Assert.IsTrue(menuView.ContinueButton.gameObject.activeSelf);
            Assert.AreEqual(SaveService.GetAutosaveSlotId(0), menu.Latest.SlotId, "the latest slot is the exit autosave");

            menuView.ContinueButton.onClick.Invoke();
            yield return WaitUntil(() => flow is { State: FlowState.Hub, IsTransitioning: false }, "continue");
            Assert.AreEqual(TestLocationId, flow.Location.Model.Id, "the same location");
            Assert.IsTrue(SceneManager.GetSceneByName(TestLocationScene).isLoaded);
            Assert.IsNull(root.Popup.Value, "no errors");

            flow.Request(new ExitToMenuRequest()).Forget();
            yield return WaitUntil(() => flow is { State: FlowState.MainMenu, IsTransitioning: false }, "exit to menu after continue");
        }

        [UnityTest]
        public IEnumerator LoadSlotOfNewerVersion_ShowsPopup()
        {
            yield return SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Additive);
            var starter = FindStarter();
            yield return WaitUntil(() => starter.Flow is { State: FlowState.MainMenu, IsTransitioning: false }, "main menu");
            var flow = starter.Flow;
            var root = starter.Global.Get<UIRootViewModel>();
            var ui = starter.UIRoot;

            var save = new SaveGame();
            save.Meta.Location = TestLocationId;
            save.AddSection(SaveSection.FromObject(LocationSystem.Key, 99, new LocationSave { Current = TestLocationId }));
            SaveSlotInfo slot = null;
            yield return starter.Saves.SaveAsync(save, SaveSlotKind.Manual).ToCoroutine(result => slot = result);

            var menu = root.MainMenu.Value;
            ((MainMenuView)ui.GetView(menu)).LoadButton.onClick.Invoke();
            var slots = root.SaveLoad.Value;
            yield return WaitUntil(() => slots.Slots.Count == 1, "the slot in the list");
            var slotsView = (SaveLoadView)ui.GetView(slots);
            Assert.AreEqual(slot.SlotId, slotsView.Slots.Views[0].Source.SlotId);

            LogAssert.Expect(LogType.Exception, new Regex("SaveException.*version 99 is newer"));
            LogAssert.Expect(LogType.Warning, new Regex("failed, returning to main menu"));
            slotsView.Slots.Views[0].LoadButton.onClick.Invoke();
            yield return WaitUntil(() => root.Popup.Value != null && flow is { State: FlowState.MainMenu, IsTransitioning: false }, "error popup in the main menu");

            var popup = root.Popup.Value;
            Assert.AreEqual("Невозможно загрузиться! Сохранение имеет более высокую версию, чем клиент.", popup.Message);
            Assert.IsNull(flow.Session);
            Assert.IsNotNull(root.MainMenu.Value);
            var popupView = (PopupView)ui.GetView(popup);
            Assert.IsTrue(popupView.gameObject.activeInHierarchy);
            Assert.AreSame(popupView.Buttons.Views[0].Button.gameObject, ui.EventSystem.currentSelectedGameObject, "focus on «OK»");

            starter.UI.HandleInput(true, false);
            Assert.IsNull(root.Popup.Value, "B / Esc closes the popup");
        }

        private static GameStarter FindStarter()
        {
            var main = SceneManager.GetSceneByName(MainScene);
            Assert.IsTrue(main.isLoaded, "Main is loaded");
            foreach (var rootObject in main.GetRootGameObjects())
            {
                if (rootObject.TryGetComponent<GameStarter>(out var starter))
                    return starter;
            }

            Assert.Fail($"No {nameof(GameStarter)} on root objects of {MainScene}");
            return null;
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
