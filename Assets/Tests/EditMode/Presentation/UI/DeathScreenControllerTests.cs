using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.UI
{
    /// <summary>
    /// Phase 4b — strict-TDD for the death-screen modal controller.
    /// Per <c>Docs/QUD-PARITY.md §2.1</c> and <c>Docs/roadmap.md</c>
    /// Tier-1 #2 (death-screen continue from autosave).
    ///
    /// <para><b>Discipline.</b> Tests written FIRST (RED for missing
    /// types), then production written to make them GREEN. New work,
    /// not audit. Mirrors the SaveLoadInputController pattern so the
    /// modal is unit-testable without a real keyboard or scene reload.</para>
    ///
    /// <para><b>Boundaries.</b> Two seams: <see cref="IInputProbe"/>
    /// (key polling) and a new <see cref="ISceneRestarter"/> (so we
    /// can verify "Restart" without actually reloading the scene).
    /// Save/load uses the existing <see cref="ISaveLoadService"/>.</para>
    ///
    /// <para><b>Lifecycle.</b> The modal starts inactive. Something
    /// outside (the player-Died listener) calls
    /// <see cref="DeathScreenController.Activate"/>. From there it
    /// polls Tick every frame; when the player picks an option it
    /// dispatches and goes inactive. Subsequent Tick calls do
    /// nothing until reactivated.</para>
    /// </summary>
    [TestFixture]
    public class DeathScreenControllerTests
    {
        private FakeInputProbe _input;
        private FakeSaveLoadService _service;
        private FakeSceneRestarter _restarter;
        private List<string> _log;
        private DeathScreenController _controller;

        [SetUp]
        public void Setup()
        {
            _input = new FakeInputProbe();
            _service = new FakeSaveLoadService();
            _restarter = new FakeSceneRestarter();
            _log = new List<string>();
            _controller = new DeathScreenController();
        }

        // ============================================================
        // Initial state + activation
        // ============================================================

        [Test]
        public void NewController_IsInactive()
        {
            Assert.IsFalse(_controller.IsActive,
                "Death screen must start inactive — only shows when the player dies.");
        }

        [Test]
        public void Activate_SetsActiveTrue_AndLogsHelpText()
        {
            _controller.Activate(_log.Add);

            Assert.IsTrue(_controller.IsActive);
            CollectionAssert.IsNotEmpty(_log,
                "Activation should log a prompt so the player knows what to press.");
            // The prompt should mention the two key bindings.
            string prompt = string.Join(" ", _log).ToLowerInvariant();
            Assert.IsTrue(prompt.Contains("load") && prompt.Contains("restart"),
                $"Prompt must mention 'load' and 'restart' choices; got: {string.Join(" / ", _log)}");
        }

        [Test]
        public void Tick_WhenInactive_DoesNothing()
        {
            _input.PressKey(_controller.LoadKey);
            _input.PressKey(_controller.RestartKey);

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls);
            Assert.AreEqual(0, _restarter.RestartCalls,
                "Inactive modal must not dispatch anything regardless of keys held.");
        }

        // ============================================================
        // Load choice
        // ============================================================

        [Test]
        public void Tick_LoadKey_SaveExists_CallsQuickLoad_AndDeactivates()
        {
            _service.HasQuickSaveResult = true;
            _controller.Activate(_log.Add);
            _input.PressKey(_controller.LoadKey);

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls);
            Assert.IsFalse(_controller.IsActive,
                "After dispatching a choice the modal must deactivate.");
        }

        [Test]
        public void Tick_LoadKey_NoSaveExists_DoesNothing_StaysActive()
        {
            _service.HasQuickSaveResult = false;
            _controller.Activate(_log.Add);
            _input.PressKey(_controller.LoadKey);
            _log.Clear();  // ignore activation prompt

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls,
                "Pressing load with no save must NOT invoke load (would read garbage).");
            Assert.IsTrue(_controller.IsActive,
                "Modal stays active so the player can pick Restart instead.");
            CollectionAssert.IsNotEmpty(_log,
                "Player should be told why nothing happened.");
        }

        // ============================================================
        // Restart choice
        // ============================================================

        [Test]
        public void Tick_RestartKey_CallsRestart_AndDeactivates()
        {
            _controller.Activate(_log.Add);
            _input.PressKey(_controller.RestartKey);

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.AreEqual(1, _restarter.RestartCalls,
                "Restart key must invoke ISceneRestarter.Restart exactly once.");
            Assert.IsFalse(_controller.IsActive,
                "After Restart the modal deactivates.");
        }

        [Test]
        public void Tick_RestartKey_NoSaveNeeded_StillCallsRestart()
        {
            // Restart works regardless of save state — it's the always-available exit.
            _service.HasQuickSaveResult = false;
            _controller.Activate(_log.Add);
            _input.PressKey(_controller.RestartKey);

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.AreEqual(1, _restarter.RestartCalls);
        }

        // ============================================================
        // Conflict resolution + safety
        // ============================================================

        [Test]
        public void Tick_BothKeysSameFrame_PrioritizesLoad()
        {
            // If a save exists, prefer Load over Restart on collision —
            // Load is recoverable (player can still die again and Restart);
            // Restart wipes the save-load opportunity.
            _service.HasQuickSaveResult = true;
            _controller.Activate(_log.Add);
            _input.PressKey(_controller.LoadKey);
            _input.PressKey(_controller.RestartKey);

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls);
            Assert.AreEqual(0, _restarter.RestartCalls,
                "Collision: Load wins because Restart is non-recoverable.");
        }

        [Test]
        public void Tick_NoKeyPressed_StaysActive_DispatchesNothing()
        {
            _controller.Activate(_log.Add);
            _log.Clear();

            _controller.Tick(_input, _service, _restarter, _log.Add);

            Assert.IsTrue(_controller.IsActive);
            Assert.AreEqual(0, _service.QuickLoadCalls);
            Assert.AreEqual(0, _restarter.RestartCalls);
            CollectionAssert.IsEmpty(_log,
                "No key pressed → no log spam.");
        }

        [Test]
        public void Activate_WhenAlreadyActive_DoesNotDoubleLogPrompt()
        {
            _controller.Activate(_log.Add);
            int firstLogCount = _log.Count;

            _controller.Activate(_log.Add);

            Assert.AreEqual(firstLogCount, _log.Count,
                "Re-activating an already-active modal must not re-prompt (no log spam).");
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

        private sealed class FakeSceneRestarter : ISceneRestarter
        {
            public int RestartCalls;
            public void Restart() => RestartCalls++;
        }
    }
}
