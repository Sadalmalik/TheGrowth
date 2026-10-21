using System.Linq;
using NUnit.Framework;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Core.Saves;
using TheGrowth.UI.MainMenu;
using TheGrowth.UI.Saves;
using TheGrowth.UI.Texts;
using UnityEngine;

namespace TheGrowth.Tests.EditMode.UI
{
    /// <summary>VM экранов с заглушкой Game: главное меню, окно слотов, пауза. Хранилище в памяти завершается синхронно.</summary>
    public sealed class ScreenViewModelTests
    {
        private UITestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new UITestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        private MainMenuViewModel OpenMenu()
        {
            return m_Kit.Root.MainMenu.Open(new MainMenuViewModel(m_Kit.Root, m_Kit.Flow, m_Kit.Saves, m_Kit.Popups));
        }

        private SaveLoadViewModel OpenSlots(SaveLoadMode mode)
        {
            return m_Kit.Root.SaveLoad.Open(new SaveLoadViewModel(mode, m_Kit.Flow, m_Kit.Saves, m_Kit.Popups));
        }

        private void Answer(int button)
        {
            var popup = m_Kit.Root.Popup.Value;
            Assert.IsNotNull(popup, "a confirmation is shown");
            popup.Choose(button);
        }

#region Main menu

        [Test]
        public void MainMenu_NoSlots_ContinueHidden()
        {
            var menu = OpenMenu();

            Assert.IsFalse(menu.CanContinue.CurrentValue);
            menu.Continue();
            Assert.IsEmpty(m_Kit.Flow.Journal);
            Assert.IsFalse(menu.Cancel(), "the main menu is not cancelled");
            Assert.IsFalse(menu.IsDisposed);
            Assert.IsFalse(menu.IsModal);
        }

        [Test]
        public void MainMenu_Continue_LoadsLatestReadableSlot()
        {
            m_Kit.AddSlot(1);
            var latest = m_Kit.AddSlot(5);
            m_Kit.Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };
            LogAssert_IgnoreBrokenSlot();

            var menu = OpenMenu();

            Assert.IsTrue(menu.CanContinue.CurrentValue);
            Assert.AreEqual(latest.SlotId, menu.Latest.SlotId);
            menu.Continue();
            Assert.That(m_Kit.Flow.Journal, Is.EqualTo(new[] { "load:" + latest.SlotId }));
        }

        [Test]
        public void MainMenu_NewGame_LoadWindow_QuitWithConfirmation()
        {
            var slot = m_Kit.AddSlot(1);
            var menu = OpenMenu();

            menu.NewGame();
            menu.OpenLoad();
            var slots = m_Kit.Root.SaveLoad.Value;
            Assert.AreEqual(SaveLoadMode.Load, slots.Mode);

            slots.Slots[0].Delete();
            Answer(0);
            Assert.IsEmpty(slots.Slots);
            slots.Close();
            Assert.IsFalse(menu.CanContinue.CurrentValue, "the menu re-reads the slots when the slots window closes");

            menu.Quit();
            Answer(1);
            Assert.That(m_Kit.Flow.Journal, Is.EqualTo(new[] { "new" }), "«No» keeps the game");
            menu.Quit();
            Answer(0);
            Assert.That(m_Kit.Flow.Journal, Is.EqualTo(new[] { "new", "quit" }));
            Assert.IsFalse(m_Kit.Storage.Slots.ContainsKey(slot.SlotId));
        }

#endregion

#region Slots

        [Test]
        public void Slots_LoadMode_ListsNewestFirst_LoadClosesWindow()
        {
            var older = m_Kit.AddSlot(1, "expedition");
            var newer = m_Kit.AddSlot(2, "hub", SaveSlotKind.Auto);

            var slots = OpenSlots(SaveLoadMode.Load);

            Assert.That(slots.Slots.Select(slot => slot.SlotId), Is.EqualTo(new[] { newer.SlotId, older.SlotId }));
            Assert.IsFalse(slots.IsEmpty.CurrentValue);
            Assert.AreEqual(UITexts.Get(UITexts.SavesTitleLoad), slots.Title);
            Assert.IsFalse(slots.CanSave, "no saving from the load window");
            var row = slots.Slots[1];
            Assert.AreEqual(UITexts.SlotDate(older.Meta.SavedAtUtc), row.Title);
            Assert.AreEqual("Ручное · expedition · 1:02:05", row.Details);
            Assert.AreEqual("Автосохранение · hub · 1:02:05", slots.Slots[0].Details);
            Assert.IsTrue(row.CanLoad);
            Assert.IsFalse(row.CanOverwrite);

            row.Load();

            Assert.That(m_Kit.Flow.Journal, Is.EqualTo(new[] { "load:" + older.SlotId }));
            Assert.IsTrue(slots.IsDisposed);
            Assert.IsTrue(row.IsDisposed, "slot rows are disposed with the window");
        }

        [Test]
        public void Slots_Delete_AsksConfirmation()
        {
            var slot = m_Kit.AddSlot(1);
            var slots = OpenSlots(SaveLoadMode.Load);

            slots.Slots[0].Delete();
            Assert.IsTrue(slots.IsBusy.CurrentValue, "waits for the answer");
            Answer(1);
            Assert.IsTrue(m_Kit.Storage.Slots.ContainsKey(slot.SlotId), "«No» keeps the slot");
            Assert.IsFalse(slots.IsBusy.CurrentValue);

            slots.Slots[0].Delete();
            Assert.AreEqual(UITexts.Get(UITexts.ConfirmDelete), m_Kit.Root.Popup.Value.Message);
            Answer(0);

            Assert.IsFalse(m_Kit.Storage.Slots.ContainsKey(slot.SlotId));
            Assert.IsEmpty(slots.Slots);
            Assert.IsTrue(slots.IsEmpty.CurrentValue);
        }

        [Test]
        public void Slots_SaveMode_NewSlot_OverwriteManualOnly_WithConfirmation()
        {
            var auto = m_Kit.AddSlot(1, "hub", SaveSlotKind.Auto);
            var slots = OpenSlots(SaveLoadMode.Save);
            Assert.IsTrue(slots.CanSave);
            Assert.IsFalse(slots.Slots[0].CanOverwrite, "autosaves are not overwritten by hand");
            Assert.IsFalse(slots.Slots[0].CanLoad, "no loading from the save window");

            m_Kit.Now = m_Kit.Now.AddMinutes(10);
            slots.SaveNew();

            Assert.AreEqual(2, slots.Slots.Count, "the list is re-read after saving");
            var manual = slots.Slots[0];
            Assert.AreNotEqual(auto.SlotId, manual.SlotId);
            Assert.IsTrue(manual.CanOverwrite);

            manual.Overwrite();
            Assert.AreEqual(UITexts.Get(UITexts.ConfirmOverwrite), m_Kit.Root.Popup.Value.Message);
            Answer(1);
            manual = slots.Slots[0];
            manual.Overwrite();
            Answer(0);

            Assert.That(m_Kit.Flow.Journal, Is.EqualTo(new[] { "save:new", "save:" + manual.SlotId }));
            Assert.AreEqual(2, m_Kit.Storage.Slots.Count, "overwriting does not add a slot");
        }

        [Test]
        public void Slots_SaveFailed_ShowsError()
        {
            m_Kit.Flow.FailSaves = true;
            var slots = OpenSlots(SaveLoadMode.Save);

            slots.SaveNew();

            Assert.AreEqual(UITexts.Get(UITexts.ErrorSave), m_Kit.Root.Popup.Value?.Message);
            Assert.IsFalse(slots.IsBusy.CurrentValue);
        }

        [Test]
        public void Slots_Thumbnail_LoadedAndDestroyedWithRow()
        {
            var image = new Texture2D(4, 2, TextureFormat.RGB24, false);
            var png = image.EncodeToPNG();
            Object.DestroyImmediate(image);
            m_Kit.AddSlot(1, thumbnail: png);
            m_Kit.AddSlot(2);

            var slots = OpenSlots(SaveLoadMode.Load);

            Assert.IsNull(slots.Slots[0].Thumbnail.CurrentValue, "no thumbnail in the slot");
            var texture = slots.Slots[1].Thumbnail.CurrentValue;
            Assert.IsNotNull(texture);
            Assert.AreEqual(4, texture.width);

            slots.Close();
            Assert.IsTrue(texture == null, "the texture is destroyed with the row");
        }

#endregion

#region Pause

        [Test]
        public void Pause_Buttons()
        {
            var pause = m_Kit.Root.Pause.Open(m_Kit.CreatePause());

            pause.OpenLoad();
            Assert.AreEqual(SaveLoadMode.Load, m_Kit.Root.SaveLoad.Value.Mode);
            m_Kit.Root.SaveLoad.Value.Close();
            pause.OpenSave();
            Assert.AreEqual(SaveLoadMode.Save, m_Kit.Root.SaveLoad.Value.Mode);
            m_Kit.Root.SaveLoad.Value.Close();

            pause.ExitToMenu();
            Assert.AreEqual(UITexts.Get(UITexts.ConfirmExitToMenu), m_Kit.Root.Popup.Value.Message);
            Answer(1);
            pause.Quit();
            Answer(1);
            Assert.IsEmpty(m_Kit.Flow.Journal, "«No» keeps the game");

            pause.ExitToMenu();
            Answer(0);
            pause.Quit();
            Answer(0);
            Assert.That(m_Kit.Flow.Journal, Is.EqualTo(new[] { "exit", "quit" }));

            pause.Resume();
            Assert.IsTrue(pause.IsDisposed);
            Assert.IsNull(m_Kit.Root.Pause.Value);
        }

#endregion

        private static void LogAssert_IgnoreBrokenSlot()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Slot 'manual_broken' is broken"));
        }
    }
}
