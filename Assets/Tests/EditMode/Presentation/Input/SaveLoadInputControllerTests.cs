using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.Input
{
    /// <summary>
    /// Phase 4 — strict-TDD for the save/load input controller. Per
    /// <c>Docs/QUD-PARITY.md §2.1</c> and <c>Docs/roadmap.md</c> Tier-1 #2.
    ///
    /// <para><b>Discipline.</b> These tests are written FIRST, watched
    /// fail (RED for missing types and missing impl), then satisfied
    /// by the minimum production code needed. New work, not audit —
    /// strict §2.1 TDD applies (vs the §3.9 audit-cadence used for
    /// the SaveSystem foundation).</para>
    ///
    /// <para><b>Boundaries.</b> Production code uses two seams:
    /// <list type="bullet">
    ///   <item><see cref="IInputProbe"/> — abstracts key polling
    ///   so tests don't depend on Unity's real keyboard</item>
    ///   <item><see cref="ISaveLoadService"/> — abstracts the
    ///   actual save/load file I/O so tests don't touch disk</item>
    /// </list>
    /// The MonoBehaviour adapter (wired into InputHandler) is the
    /// thinnest possible glue between Unity input → IInputProbe and
    /// SaveGameService → ISaveLoadService. The dispatch logic itself
    /// lives in <see cref="SaveLoadInputController.Tick"/> and is
    /// 100% testable.</para>
    /// </summary>
    [TestFixture]
    public class SaveLoadInputControllerTests
    {
        private FakeInputProbe _input;
        private FakeSaveLoadService _service;
        private List<string> _log;
        private SaveLoadInputController _controller;

        [SetUp]
        public void Setup()
        {
            _input = new FakeInputProbe();
            _service = new FakeSaveLoadService();
            _log = new List<string>();
            _controller = new SaveLoadInputController();
        }

        // ============================================================
        // Save key
        // ============================================================

        [Test]
        public void Tick_SaveKeyPressed_CallsQuickSave()
        {
            _input.PressKey(_controller.SaveKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickSaveCalls,
                "Pressing the save key must invoke ISaveLoadService.QuickSave exactly once.");
            Assert.AreEqual(0, _service.QuickLoadCalls,
                "Save key must NOT invoke QuickLoad.");
        }

        [Test]
        public void Tick_SaveKeyPressed_QuickSaveSucceeds_LogsConfirmation()
        {
            _input.PressKey(_controller.SaveKey);
            _service.NextSaveResult = true;

            _controller.Tick(_input, _service, _log.Add);

            CollectionAssert.IsNotEmpty(_log,
                "Successful QuickSave must log a confirmation message.");
            StringAssert.Contains("aved", _log[_log.Count - 1],
                "Log message should mention 'saved' (case-insensitive).");
        }

        [Test]
        public void Tick_SaveKeyPressed_QuickSaveFails_LogsFailure()
        {
            _input.PressKey(_controller.SaveKey);
            _service.NextSaveResult = false;

            _controller.Tick(_input, _service, _log.Add);

            CollectionAssert.IsNotEmpty(_log,
                "Failed QuickSave must log a failure message (so the player knows their save didn't land).");
            StringAssert.Contains("ail", _log[_log.Count - 1],
                "Failure log should mention 'failed' or similar.");
        }

        // ============================================================
        // Load key
        // ============================================================

        [Test]
        public void Tick_LoadKeyPressed_SaveExists_CallsQuickLoad()
        {
            _service.HasQuickSaveResult = true;
            _input.PressKey(_controller.LoadKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickLoadCalls,
                "Pressing load key while a save exists must invoke QuickLoad.");
        }

        [Test]
        public void Tick_LoadKeyPressed_NoSaveExists_DoesNotCallQuickLoad()
        {
            _service.HasQuickSaveResult = false;
            _input.PressKey(_controller.LoadKey);

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickLoadCalls,
                "Load key with no save MUST NOT invoke QuickLoad — would otherwise read garbage.");
        }

        [Test]
        public void Tick_LoadKeyPressed_NoSaveExists_LogsNoSaveMessage()
        {
            _service.HasQuickSaveResult = false;
            _input.PressKey(_controller.LoadKey);

            _controller.Tick(_input, _service, _log.Add);

            CollectionAssert.IsNotEmpty(_log,
                "Load key with no save must explain to the player why nothing happened.");
            string last = _log[_log.Count - 1].ToLowerInvariant();
            Assert.IsTrue(last.Contains("no save") || last.Contains("nothing") || last.Contains("none"),
                $"Log should signal absence; got: '{_log[_log.Count - 1]}'.");
        }

        [Test]
        public void Tick_LoadKeyPressed_QuickLoadFails_LogsFailure()
        {
            _service.HasQuickSaveResult = true;
            _service.NextLoadResult = false;
            _input.PressKey(_controller.LoadKey);

            _controller.Tick(_input, _service, _log.Add);

            CollectionAssert.IsNotEmpty(_log,
                "Failed QuickLoad must log so the player knows the save was unreadable.");
            StringAssert.Contains("ail", _log[_log.Count - 1]);
        }

        // ============================================================
        // No-op cases
        // ============================================================

        [Test]
        public void Tick_NoKeyPressed_NeitherCalled()
        {
            _service.HasQuickSaveResult = true;  // even if save exists

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(0, _service.QuickSaveCalls);
            Assert.AreEqual(0, _service.QuickLoadCalls);
            CollectionAssert.IsEmpty(_log,
                "Quiet tick (no save/load key) must produce no log spam.");
        }

        // ============================================================
        // Conflict resolution
        // ============================================================

        [Test]
        public void Tick_BothKeysPressedSameFrame_PrioritizesSave()
        {
            // Defensive — by default save and load keys differ, but if a future
            // remap binds them to the same key (or modifier confusion), the
            // safer outcome is "save the current state" rather than "overwrite
            // it with the disk save."
            _input.PressKey(_controller.SaveKey);
            _input.PressKey(_controller.LoadKey);
            _service.HasQuickSaveResult = true;

            _controller.Tick(_input, _service, _log.Add);

            Assert.AreEqual(1, _service.QuickSaveCalls, "Save fires.");
            Assert.AreEqual(0, _service.QuickLoadCalls,
                "Load must NOT fire when save also fires — prioritize the non-destructive op.");
        }

        // ============================================================
        // Consume contract — Tick returns whether it consumed input
        // ============================================================

        [Test]
        public void Tick_SaveKeyPressed_ReturnsTrue_ConsumesInput()
        {
            _input.PressKey(_controller.SaveKey);
            bool consumed = _controller.Tick(_input, _service, _log.Add);
            Assert.IsTrue(consumed,
                "Save key dispatched → must signal consumed=true so the host short-circuits remaining bindings.");
        }

        [Test]
        public void Tick_LoadKeyPressed_SaveExists_ReturnsTrue_ConsumesInput()
        {
            _service.HasQuickSaveResult = true;
            _input.PressKey(_controller.LoadKey);
            bool consumed = _controller.Tick(_input, _service, _log.Add);
            Assert.IsTrue(consumed, "Successful load dispatch → consumed=true.");
        }

        [Test]
        public void Tick_LoadKeyPressed_NoSaveExists_ReturnsTrue_ConsumesInput()
        {
            // The 'no save' message IS dispatch — the F-key was a save/load
            // intent, never let it fall through to a debug binding.
            _service.HasQuickSaveResult = false;
            _input.PressKey(_controller.LoadKey);
            bool consumed = _controller.Tick(_input, _service, _log.Add);
            Assert.IsTrue(consumed,
                "Even no-save fallback consumes the load key — host must NOT pass it through to debug bindings.");
        }

        [Test]
        public void Tick_NoKeyPressed_ReturnsFalse_NotConsumed()
        {
            bool consumed = _controller.Tick(_input, _service, _log.Add);
            Assert.IsFalse(consumed, "Quiet tick must return false so host continues normally.");
        }

        // ============================================================
        // Test doubles
        // ============================================================

        private sealed class FakeInputProbe : IInputProbe
        {
            private readonly HashSet<KeyCode> _down = new HashSet<KeyCode>();
            public void PressKey(KeyCode k) => _down.Add(k);
            public void ReleaseAll() => _down.Clear();
            public bool GetKeyDown(KeyCode k) => _down.Contains(k);
        }

        private sealed class FakeSaveLoadService : ISaveLoadService
        {
            public int QuickSaveCalls;
            public int QuickLoadCalls;
            public bool HasQuickSaveResult;
            public bool NextSaveResult = true;
            public bool NextLoadResult = true;

            public bool QuickSave()
            {
                QuickSaveCalls++;
                return NextSaveResult;
            }

            public bool QuickLoad()
            {
                QuickLoadCalls++;
                return NextLoadResult;
            }

            public bool HasQuickSave() => HasQuickSaveResult;
        }
    }
}
