using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.UI
{
    /// <summary>
    /// Phase 4d — strict-TDD for the pause-menu controller. Per
    /// <c>Docs/QUD-PARITY.md §2.1</c>. New work, not audit.
    ///
    /// <para><b>Lifecycle.</b> Closed on construct. Pressing
    /// <c>Tab</c> opens; Up/Down arrows navigate Save↔Load; Enter
    /// (or mouse click via <see cref="PauseMenuController.ClickSelect"/>)
    /// confirms; Tab again closes without dispatch.</para>
    ///
    /// <para><b>Why Tab and not Esc:</b> Esc is the project-wide
    /// "close current modal" key (PickupUI, ContainerPickerUI,
    /// WorldActionMenuUI, etc.) — making Esc also OPEN a modal would
    /// either fight those existing handlers or accidentally double-
    /// dismiss. Tab is unbound elsewhere and reads as
    /// "switch context."</para>
    ///
    /// <para><b>Two items.</b> Index 0 = Save, Index 1 = Load. No
    /// wrap on navigation — Up at top stays at top, Down at bottom
    /// stays at bottom (matches <c>WorldActionMenuUI</c> behavior).</para>
    /// </summary>
    [TestFixture]
    public class PauseMenuControllerTests
    {
        private FakeInputProbe _input;
        private FakeSaveLoadService _service;
        private List<string> _log;
        private PauseMenuController _controller;

        [SetUp]
        public void Setup()
        {
            _input = new FakeInputProbe();
            _service = new FakeSaveLoadService();
            _log = new List<string>();
            _controller = new PauseMenuController();
        }

        // ============================================================
        // Closed-state behavior
        // ============================================================

        [Test]
        public void New_IsClosed_SelectionAtSave()
        {
            Assert.IsFalse(_controller.IsOpen);
            Assert.AreEqual(PauseMenuController.SaveIndex, _controller.SelectedIndex,
                "Newly-constructed menu should default to Save (index 0).");
        }

        [Test]
        public void Tick_WhenClosed_OpenCloseKey_OpensMenu_AndConsumesInput()
        {
            _input.PressKey(_controller.OpenCloseKey);

            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.IsTrue(_controller.IsOpen, "OpenCloseKey while closed must open menu.");
            Assert.IsTrue(consumed, "Opening must signal consumed=true so host short-circuits.");
            Assert.AreEqual(PauseMenuController.SaveIndex, _controller.SelectedIndex,
                "Open should reset selection to Save (top item).");
        }

        [Test]
        public void Tick_WhenClosed_OtherKey_DoesNothing_NotConsumed()
        {
            // Use a key the controller doesn't bind (avoid arrow keys / Enter).
            _input.PressKey(KeyCode.A);

            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.IsFalse(_controller.IsOpen);
            Assert.IsFalse(consumed,
                "Unbound keys while closed must return consumed=false so host continues normal flow.");
        }

        [Test]
        public void Tick_WhenClosed_NoKey_NoChange_NotConsumed()
        {
            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.IsFalse(_controller.IsOpen);
            Assert.IsFalse(consumed);
            CollectionAssert.IsEmpty(_log);
        }

        // ============================================================
        // Open-state navigation
        // ============================================================

        [Test]
        public void Tick_WhenOpen_DownArrow_MovesSelectionToLoad()
        {
            _controller.Open();
            _input.PressKey(_controller.DownKey);

            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(PauseMenuController.LoadIndex, _controller.SelectedIndex);
            Assert.IsTrue(consumed);
        }

        [Test]
        public void Tick_WhenOpen_UpArrow_FromSave_StaysAtSave_NoWrap()
        {
            _controller.Open();
            // Already at Save (index 0). Up arrow should NOT wrap to Load (index 1).
            _input.PressKey(_controller.UpKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(PauseMenuController.SaveIndex, _controller.SelectedIndex,
                "Up at top must clamp to top (no wrap).");
        }

        [Test]
        public void Tick_WhenOpen_DownArrow_FromLoad_StaysAtLoad_NoWrap()
        {
            _controller.Open();
            _controller.MoveSelectionForTest(PauseMenuController.LoadIndex);  // jump to bottom
            _input.PressKey(_controller.DownKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(PauseMenuController.LoadIndex, _controller.SelectedIndex,
                "Down at bottom must clamp to bottom (no wrap).");
        }

        [Test]
        public void Tick_WhenOpen_UpArrow_FromLoad_MovesBackToSave()
        {
            _controller.Open();
            _controller.MoveSelectionForTest(PauseMenuController.LoadIndex);
            _input.PressKey(_controller.UpKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(PauseMenuController.SaveIndex, _controller.SelectedIndex);
        }

        // ============================================================
        // Open-state confirm (Enter key)
        // ============================================================

        [Test]
        public void Tick_WhenOpen_EnterOnSave_CallsQuickSave_AndCloses()
        {
            _controller.Open();
            // Selection is already at Save by default
            _input.PressKey(_controller.ConfirmKey);

            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickSaveCalls);
            Assert.IsFalse(_controller.IsOpen, "After Save dispatch the menu closes.");
            Assert.IsTrue(consumed);
        }

        [Test]
        public void Tick_WhenOpen_EnterOnLoad_SaveExists_CallsQuickLoad_AndCloses()
        {
            _service.HasQuickSaveResult = true;
            _controller.Open();
            _controller.MoveSelectionForTest(PauseMenuController.LoadIndex);
            _input.PressKey(_controller.ConfirmKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls);
            Assert.IsFalse(_controller.IsOpen);
        }

        [Test]
        public void Tick_WhenOpen_EnterOnLoad_NoSave_DoesNotCallLoad_StaysOpen_LogsMessage()
        {
            _service.HasQuickSaveResult = false;
            _controller.Open();
            _controller.MoveSelectionForTest(PauseMenuController.LoadIndex);
            _input.PressKey(_controller.ConfirmKey);
            _log.Clear();

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls,
                "Load with no save MUST NOT call QuickLoad — menu stays open.");
            Assert.IsTrue(_controller.IsOpen, "Menu stays open so the player can pick Save instead.");
            CollectionAssert.IsNotEmpty(_log, "Player needs explanation of why nothing happened.");
        }

        // ============================================================
        // Open-state close (OpenCloseKey)
        // ============================================================

        [Test]
        public void Tick_WhenOpen_OpenCloseKey_ClosesWithoutDispatch()
        {
            _controller.Open();
            _input.PressKey(_controller.OpenCloseKey);  // toggle close

            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.IsFalse(_controller.IsOpen);
            Assert.AreEqual(0, _service.QuickSaveCalls);
            Assert.AreEqual(0, _service.QuickLoadCalls);
            Assert.IsTrue(consumed);
        }

        [Test]
        public void Tick_WhenOpen_NoKey_StaysOpen_NotConsumed()
        {
            _controller.Open();

            bool consumed = _controller.Tick(_input, _service, _log.Add);

            Assert.IsTrue(_controller.IsOpen);
            // When the menu is open and no key is pressed, the host still wants to
            // suppress other input — but the controller hasn't actually consumed
            // anything. Returning false here is correct; the host's existing
            // "if (controller.IsOpen) return;" guard handles input-suppression
            // independently of Tick's return value.
            Assert.IsFalse(consumed);
        }

        // ============================================================
        // Mouse click confirm
        // ============================================================

        [Test]
        public void ClickSelect_OnSaveIndex_CallsQuickSave_AndCloses()
        {
            _controller.Open();

            _controller.ClickSelect(PauseMenuController.SaveIndex, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickSaveCalls);
            Assert.IsFalse(_controller.IsOpen);
        }

        [Test]
        public void ClickSelect_OnLoadIndex_SaveExists_CallsQuickLoad_AndCloses()
        {
            _service.HasQuickSaveResult = true;
            _controller.Open();

            _controller.ClickSelect(PauseMenuController.LoadIndex, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls);
            Assert.IsFalse(_controller.IsOpen);
        }

        [Test]
        public void ClickSelect_InvalidIndex_DoesNothing_StaysOpen()
        {
            _controller.Open();

            _controller.ClickSelect(999, _service, _log.Add);

            Assert.IsTrue(_controller.IsOpen);
            Assert.AreEqual(0, _service.QuickSaveCalls);
            Assert.AreEqual(0, _service.QuickLoadCalls);
        }

        [Test]
        public void ClickSelect_WhileClosed_DoesNothing()
        {
            // Modal is closed — a stray mouse-click index shouldn't dispatch.
            _controller.ClickSelect(PauseMenuController.SaveIndex, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickSaveCalls);
        }

        // ============================================================
        // Mouse hover (changes selection without confirming)
        // ============================================================

        [Test]
        public void HoverSelect_WhileOpen_ChangesSelectedIndex()
        {
            _controller.Open();

            _controller.HoverSelect(PauseMenuController.LoadIndex);

            Assert.AreEqual(PauseMenuController.LoadIndex, _controller.SelectedIndex);
            Assert.IsTrue(_controller.IsOpen, "Hover does not close — only changes highlight.");
        }

        [Test]
        public void HoverSelect_InvalidIndex_LeavesSelectionUnchanged()
        {
            _controller.Open();
            _controller.MoveSelectionForTest(PauseMenuController.SaveIndex);

            _controller.HoverSelect(999);

            Assert.AreEqual(PauseMenuController.SaveIndex, _controller.SelectedIndex);
        }

        // ============================================================
        // Test doubles
        // ============================================================

        private sealed class FakeInputProbe : IInputProbe
        {
            private readonly HashSet<KeyCode> _down = new HashSet<KeyCode>();
            public void PressKey(KeyCode k) => _down.Add(k);
            public bool GetKeyDown(KeyCode k) => _down.Contains(k);
        }

        private sealed class FakeSaveLoadService : ISaveLoadService
        {
            public int QuickSaveCalls;
            public int QuickLoadCalls;
            public bool HasQuickSaveResult;
            public bool QuickSave() { QuickSaveCalls++; return true; }
            public bool QuickLoad() { QuickLoadCalls++; return true; }
            public bool HasQuickSave() => HasQuickSaveResult;
        }
    }
}
