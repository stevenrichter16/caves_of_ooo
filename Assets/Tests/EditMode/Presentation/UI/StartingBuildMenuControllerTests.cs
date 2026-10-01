using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.UI
{
    /// <summary>
    /// Key dispatch for the new-game build picker, and the one change to the boot
    /// menu it needs: when a save exists, "New game" must open the picker rather
    /// than checkpoint the fresh character immediately (the build has to be
    /// chosen BEFORE the first save, or the save would hold a character with no
    /// kit).
    /// </summary>
    [TestFixture]
    public class StartingBuildMenuControllerTests
    {
        private FakeInputProbe _input;
        private StartingBuildMenuController _menu;
        private List<StartingBuildDef> _options;
        private List<StartingBuildDef> _chosen;

        [SetUp]
        public void Setup()
        {
            _input = new FakeInputProbe();
            _menu = new StartingBuildMenuController();
            _options = new List<StartingBuildDef>
            {
                new StartingBuildDef { Id = "a", Name = "A" },
                new StartingBuildDef { Id = "b", Name = "B" },
                new StartingBuildDef { Id = "c", Name = "C" },
            };
            _chosen = new List<StartingBuildDef>();
        }

        private bool Open() => _menu.Open(_options, _chosen.Add);

        private bool Press(KeyCode k)
        {
            _input.Clear();
            _input.PressKey(k);
            return _menu.Tick(_input);
        }

        // ── lifecycle ───────────────────────────────────────────

        [Test]
        public void NewController_IsClosed_AndConsumesNothing()
        {
            Assert.IsFalse(_menu.IsOpen);
            Assert.IsFalse(Press(KeyCode.Return), "a closed picker must not swallow Enter");
        }

        [Test]
        public void Open_WithNoOptions_StaysClosed()
        {
            Assert.IsFalse(_menu.Open(new List<StartingBuildDef>(), _chosen.Add));
            Assert.IsFalse(_menu.IsOpen);
            Assert.IsFalse(_menu.Open(null, _chosen.Add));
        }

        [Test]
        public void Open_StartsOnTheFirstOption()
        {
            Assert.IsTrue(Open());
            Assert.IsTrue(_menu.IsOpen);
            Assert.AreEqual(0, _menu.SelectedIndex);
        }

        // ── navigation ──────────────────────────────────────────

        [Test]
        public void DownAndUp_MoveAndWrap()
        {
            Open();
            Assert.IsTrue(Press(KeyCode.DownArrow));
            Assert.AreEqual(1, _menu.SelectedIndex);
            Press(KeyCode.UpArrow); Press(KeyCode.UpArrow);
            Assert.AreEqual(2, _menu.SelectedIndex, "up from the top wraps to the bottom");
        }

        [Test]
        public void WAndS_AreAlternateUpAndDown()
        {
            Open();
            Press(KeyCode.S);
            Assert.AreEqual(1, _menu.SelectedIndex);
            Press(KeyCode.W);
            Assert.AreEqual(0, _menu.SelectedIndex);
        }

        [Test]
        public void DigitKeys_JumpToThatRow_AndIgnoreRowsThatDoNotExist()
        {
            Open();
            Assert.IsTrue(Press(KeyCode.Alpha3));
            Assert.AreEqual(2, _menu.SelectedIndex);
            Press(KeyCode.Alpha9);
            Assert.AreEqual(2, _menu.SelectedIndex, "there is no row 9");
            Assert.IsEmpty(_chosen, "a digit selects; it does not choose");
        }

        [Test]
        public void HoverSelect_MovesTheHighlight_ButNeverChooses()
        {
            Open();
            _menu.HoverSelect(2);
            Assert.AreEqual(2, _menu.SelectedIndex);
            Assert.IsEmpty(_chosen);
        }

        // ── choosing ────────────────────────────────────────────

        [TestCase(KeyCode.Return)]
        [TestCase(KeyCode.KeypadEnter)]
        [TestCase(KeyCode.Space)]
        public void ConfirmKeys_ChooseTheHighlightedBuild_ExactlyOnce_AndClose(KeyCode key)
        {
            Open();
            Press(KeyCode.DownArrow);

            Assert.IsTrue(Press(key));

            Assert.AreEqual(new[] { "b" }, _chosen.Select(c => c.Id).ToArray());
            Assert.IsFalse(_menu.IsOpen);
            Assert.IsFalse(Press(key), "closed: a second Enter is not consumed");
            Assert.AreEqual(1, _chosen.Count, "the callback never fires twice");
        }

        [Test]
        public void ClickConfirm_SelectsAndChooses()
        {
            Open();
            _menu.ClickConfirm(2);
            Assert.AreEqual("c", _chosen.Single().Id);
            Assert.IsFalse(_menu.IsOpen);
        }

        [Test]
        public void ClickConfirm_OnAClosedMenu_DoesNothing()
        {
            _menu.ClickConfirm(1);
            Assert.IsEmpty(_chosen);
        }

        [Test]
        public void Escape_DoesNotCancel_ThereIsNothingToGoBackTo()
        {
            Open();
            Assert.IsFalse(Press(KeyCode.Escape));
            Assert.IsTrue(_menu.IsOpen);
            Assert.IsEmpty(_chosen);
        }

        [Test]
        public void TheModalIsClosed_BeforeTheCallbackRuns()
        {
            // The callback applies the build and starts the game; if the modal were
            // still "open" at that moment, the game loop would still think a modal
            // owned the screen.
            bool openInsideCallback = true;
            _menu.Open(_options, _ => openInsideCallback = _menu.IsOpen);

            Press(KeyCode.Return);

            Assert.IsFalse(openInsideCallback);
        }

        [Test]
        public void ANullCallback_IsSafe()
        {
            _menu.Open(_options, null);
            Assert.DoesNotThrow(() => Press(KeyCode.Return));
            Assert.IsFalse(_menu.IsOpen);
        }

        // ── the boot menu gate ──────────────────────────────────

        [Test]
        public void BootMenu_NewGame_WithAGate_HandsOverToThePicker_AndDoesNotCheckpoint()
        {
            var boot = new BootMenuController();
            var service = new FakeSaveLoadService();
            int gateCalls = 0;
            boot.NewGameGate = () => { gateCalls++; return true; };
            boot.TryActivate(hasSave: true, _ => { });
            _input.PressKey(KeyCode.N);

            boot.Tick(_input, service, _ => { });

            Assert.AreEqual(1, gateCalls);
            Assert.AreEqual(0, service.BeginNewGameCalls,
                "the new game is checkpointed after the build is chosen, not before");
            Assert.IsFalse(boot.IsActive, "the boot menu is done; the picker owns the screen now");
        }

        [Test]
        public void BootMenu_NewGame_WhenTheGateDeclines_CheckpointsAsBefore()
        {
            var boot = new BootMenuController { NewGameGate = () => false };
            var service = new FakeSaveLoadService();
            boot.TryActivate(hasSave: true, _ => { });
            _input.PressKey(KeyCode.N);

            boot.Tick(_input, service, _ => { });

            Assert.AreEqual(1, service.BeginNewGameCalls);
        }

        [Test]
        public void BootMenu_NewGame_WithNoGate_IsUnchanged()
        {
            var boot = new BootMenuController();
            var service = new FakeSaveLoadService();
            boot.TryActivate(hasSave: true, _ => { });
            _input.PressKey(KeyCode.N);

            boot.Tick(_input, service, _ => { });

            Assert.AreEqual(1, service.BeginNewGameCalls);
            Assert.IsFalse(boot.IsActive);
        }

        [Test]
        public void BootMenu_Continue_NeverOpensThePicker()
        {
            // Counter-check: continuing a save discards the fresh character, so no
            // build is chosen and the gate must not run.
            var boot = new BootMenuController();
            var service = new FakeSaveLoadService { HasQuickSaveResult = true };
            int gateCalls = 0;
            boot.NewGameGate = () => { gateCalls++; return true; };
            boot.TryActivate(hasSave: true, _ => { });
            _input.PressKey(KeyCode.C);

            boot.Tick(_input, service, _ => { });

            Assert.AreEqual(0, gateCalls);
            Assert.AreEqual(1, service.QuickLoadCalls);
        }

        // ── test doubles ────────────────────────────────────────

        private sealed class FakeInputProbe : IInputProbe
        {
            private readonly HashSet<KeyCode> _down = new HashSet<KeyCode>();
            public void PressKey(KeyCode k) => _down.Add(k);
            public void Clear() => _down.Clear();
            public bool GetKeyDown(KeyCode k) => _down.Contains(k);
        }

        private sealed class FakeSaveLoadService : ISaveLoadService
        {
            public int BeginNewGameCalls;
            public int QuickLoadCalls;
            public bool HasQuickSaveResult;
            public bool BeginNewGame() { BeginNewGameCalls++; return true; }
            public bool QuickSave() => true;
            public bool QuickLoad() { QuickLoadCalls++; return true; }
            public bool HasQuickSave() => HasQuickSaveResult;
        }
    }
}
