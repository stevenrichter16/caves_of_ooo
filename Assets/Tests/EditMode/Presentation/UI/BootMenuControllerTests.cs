using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.UI
{
    /// <summary>
    /// Phase 4c — strict-TDD for the boot-menu modal controller.
    /// Per <c>Docs/QUD-PARITY.md §2.1</c> and <c>Docs/roadmap.md</c>
    /// Tier-1 #2 (boot menu Continue / New Game).
    ///
    /// <para><b>Lifecycle.</b> Starts inactive. GameBootstrap calls
    /// <see cref="BootMenuController.TryActivate"/> at end-of-init;
    /// only activates if a save exists (no save = nothing to
    /// continue from = skip the menu, go straight to play).</para>
    ///
    /// <para><b>Default keys.</b> <c>C</c> = continue (load),
    /// <c>N</c> = new game (dismiss menu, keep current bootstrap
    /// state).</para>
    /// </summary>
    [TestFixture]
    public class BootMenuControllerTests
    {
        private FakeInputProbe _input;
        private FakeSaveLoadService _service;
        private List<string> _log;
        private BootMenuController _controller;

        [SetUp]
        public void Setup()
        {
            _input = new FakeInputProbe();
            _service = new FakeSaveLoadService();
            _log = new List<string>();
            _controller = new BootMenuController();
        }

        // ============================================================
        // Activation
        // ============================================================

        [Test]
        public void NewController_IsInactive()
        {
            Assert.IsFalse(_controller.IsActive);
        }

        [Test]
        public void TryActivate_NoSaveExists_ReturnsFalse_StaysInactive()
        {
            bool activated = _controller.TryActivate(hasSave: false, _log.Add);

            Assert.IsFalse(activated, "No save → no menu shown.");
            Assert.IsFalse(_controller.IsActive);
            CollectionAssert.IsEmpty(_log,
                "No menu activation → no log spam.");
        }

        [Test]
        public void TryActivate_SaveExists_ReturnsTrue_ActivatesAndLogsPrompt()
        {
            bool activated = _controller.TryActivate(hasSave: true, _log.Add);

            Assert.IsTrue(activated);
            Assert.IsTrue(_controller.IsActive);
            CollectionAssert.IsNotEmpty(_log);
            string prompt = string.Join(" ", _log).ToLowerInvariant();
            Assert.IsTrue(prompt.Contains("continue") && prompt.Contains("new"),
                $"Prompt should mention Continue and New Game choices; got: {string.Join(" / ", _log)}");
        }

        [Test]
        public void TryActivate_AlreadyActive_DoesNotDoubleLogPrompt()
        {
            _controller.TryActivate(hasSave: true, _log.Add);
            int firstLogCount = _log.Count;

            _controller.TryActivate(hasSave: true, _log.Add);

            Assert.AreEqual(firstLogCount, _log.Count,
                "Re-activating already-active modal must not spam the log.");
        }

        // ============================================================
        // Continue choice
        // ============================================================

        [Test]
        public void Tick_ContinueKey_CallsQuickLoad_AndDeactivates()
        {
            _service.HasQuickSaveResult = true;
            _controller.TryActivate(hasSave: true, _log.Add);
            _input.PressKey(_controller.ContinueKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls);
            Assert.IsFalse(_controller.IsActive);
        }

        [Test]
        public void Tick_ContinueKey_SaveVanishedBetweenActivateAndTick_StaysActive_LogsMessage()
        {
            // Defensive: save existed at activation, gone by Tick (e.g., user deleted
            // the file from disk while menu was up). Don't crash; explain and let
            // user pick New Game instead.
            _controller.TryActivate(hasSave: true, _log.Add);
            _service.HasQuickSaveResult = false;  // file deleted
            _input.PressKey(_controller.ContinueKey);
            _log.Clear();

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls,
                "Save vanished — must NOT call QuickLoad on missing data.");
            Assert.IsTrue(_controller.IsActive,
                "Stay active so user can still pick New Game.");
            CollectionAssert.IsNotEmpty(_log,
                "Tell the player why nothing happened.");
        }

        // ============================================================
        // New Game choice
        // ============================================================

        [Test]
        public void Tick_NewGameKey_DeactivatesWithoutLoading()
        {
            _service.HasQuickSaveResult = true;
            _controller.TryActivate(hasSave: true, _log.Add);
            _input.PressKey(_controller.NewGameKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls,
                "New Game must NOT load the save.");
            Assert.IsFalse(_controller.IsActive);
        }

        // ============================================================
        // Inactive + collision policies
        // ============================================================

        [Test]
        public void Tick_WhenInactive_DoesNothing()
        {
            _input.PressKey(_controller.ContinueKey);
            _input.PressKey(_controller.NewGameKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls);
        }

        [Test]
        public void Tick_BothKeysSameFrame_PrioritizesContinue()
        {
            _service.HasQuickSaveResult = true;
            _controller.TryActivate(hasSave: true, _log.Add);
            _input.PressKey(_controller.ContinueKey);
            _input.PressKey(_controller.NewGameKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls,
                "Continue wins — lets player recover their save; New Game is destructive.");
            Assert.IsFalse(_controller.IsActive);
        }

        [Test]
        public void Tick_NoKeyPressed_StaysActive_NoDispatch()
        {
            _service.HasQuickSaveResult = true;
            _controller.TryActivate(hasSave: true, _log.Add);
            _log.Clear();

            _controller.Tick(_input, _service, _log.Add);

            Assert.IsTrue(_controller.IsActive);
            Assert.AreEqual(0, _service.QuickLoadCalls);
            CollectionAssert.IsEmpty(_log);
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
            public int QuickLoadCalls;
            public bool HasQuickSaveResult;
            public bool QuickSave() => true;
            public bool QuickLoad() { QuickLoadCalls++; return true; }
            public bool HasQuickSave() => HasQuickSaveResult;
        }
    }
}
