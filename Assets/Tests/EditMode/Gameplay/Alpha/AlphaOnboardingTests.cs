using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 5 — onboarding (P0, verified). The game
    /// opened in total silence: ~25 unguessable bindings, no help
    /// surface, no boot text, no goal, Escape unbound, and a 2-item
    /// pause menu without Controls or Quit. These tests pin the
    /// ControlsReference single source of truth, the boot summary, and
    /// the pause-menu expansion.
    /// </summary>
    [TestFixture]
    public class AlphaOnboardingTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ── ControlsReference: the single display table ──────────

        [Test]
        public void Controls_CoverTheCoreBindings()
        {
            string all = string.Join("\n", Array.ConvertAll(
                ControlsReference.Bindings, b => b.Key + " " + b.What)).ToLowerInvariant();
            foreach (var token in new[] {
                "wasd", "i", "c", "g", "l", "x", "m", "q", "f5", "f6", "f1", "tab" })
            {
                StringAssert.Contains(token, all,
                    $"the controls table must document '{token}'");
            }
        }

        [Test]
        public void Controls_PrintHelp_EmitsTheFullTable()
        {
            var lines = new List<string>();
            ControlsReference.PrintHelp(lines.Add);
            Assert.GreaterOrEqual(lines.Count, 10,
                "the help dump must actually list the bindings, not a teaser");
        }

        [Test]
        public void Controls_BootSummary_IsShortAndPointsAtTheQuestLog()
        {
            var lines = new List<string>();
            ControlsReference.PrintBootSummary(lines.Add);
            Assert.LessOrEqual(lines.Count, 4,
                "boot text must be glanceable, not a wall");
            string all = string.Join(" ", lines).ToLowerInvariant();
            StringAssert.Contains("f1", all, "must point at the full help");
            StringAssert.Contains("quest", all,
                "the call-to-adventure: tell the player where goals live");
        }

        // ── Pause menu: 4 items, Esc alias ───────────────────────

        [Test]
        public void PauseMenu_HasControlsAndQuitEntries()
        {
            Assert.AreEqual(4, PauseMenuController.ItemCount);
            Assert.AreEqual(2, PauseMenuController.ControlsIndex);
            Assert.AreEqual(3, PauseMenuController.QuitIndex);
        }

        [Test]
        public void PauseMenu_ControlsEntry_InvokesShowControls_AndCloses()
        {
            var menu = new PauseMenuController();
            int shown = 0;
            menu.ShowControls = () => shown++;
            menu.Open();
            menu.ClickSelect(PauseMenuController.ControlsIndex, new NoopSave(), _ => { });
            Assert.AreEqual(1, shown);
            Assert.IsFalse(menu.IsOpen, "picking Controls closes the menu so the log is readable");
        }

        [Test]
        public void PauseMenu_QuitEntry_InvokesRequestQuit()
        {
            var menu = new PauseMenuController();
            int quits = 0;
            menu.RequestQuit = () => quits++;
            menu.Open();
            menu.ClickSelect(PauseMenuController.QuitIndex, new NoopSave(), _ => { });
            Assert.AreEqual(1, quits);
        }

        [Test]
        public void PauseMenu_Escape_OpensAndCloses()
        {
            var menu = new PauseMenuController();
            var probe = new KeyProbe();

            probe.Down = KeyCode.Escape;
            Assert.IsTrue(menu.Tick(probe, new NoopSave(), _ => { }),
                "Escape must open the menu from normal play");
            Assert.IsTrue(menu.IsOpen);

            Assert.IsTrue(menu.Tick(probe, new NoopSave(), _ => { }),
                "Escape must close it again");
            Assert.IsFalse(menu.IsOpen);
        }

        [Test]
        public void PauseMenu_Tab_StillWorks()
        {
            // Counter-check: the original binding survives the alias.
            var menu = new PauseMenuController();
            var probe = new KeyProbe { Down = KeyCode.Tab };
            menu.Tick(probe, new NoopSave(), _ => { });
            Assert.IsTrue(menu.IsOpen);
        }

        private sealed class NoopSave : ISaveLoadService
        {
            public bool QuickSave() => true;
            public bool QuickLoad() => true;
            public bool HasQuickSave() => false;
        }

        private sealed class KeyProbe : IInputProbe
        {
            public KeyCode Down;
            public bool GetKeyDown(KeyCode k) => k == Down;
        }
    }
}
