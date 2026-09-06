using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    /// <summary>Held/fresh boundaries, alternate direction precedence and modal consumers.</summary>
    public class GameAuditLookKeyAdversarialTests : LookKeyFixture
    {
        [TestCase(Key.Y, -1, -1, false)] [TestCase(Key.Y, -1, -1, true)]
        [TestCase(Key.U, 1, -1, false)] [TestCase(Key.U, 1, -1, true)]
        [TestCase(Key.B, -1, 1, false)] [TestCase(Key.B, -1, 1, true)]
        [TestCase(Key.N, 1, 1, false)] [TestCase(Key.N, 1, 1, true)]
        public void HeldLookCannotStealAnIntendedDiagonal(Key key, int dx, int dy, bool holdLook)
        {
            var input = Input(); if (holdLook) { Keys(Key.L); AssertHeld(Key.L); Keys(Key.L, key); } else Keys(key);
            Tick(input); Assert.AreEqual("Normal", Mode(input)); Assert.AreEqual((10 + dx, 10 + dy), Zone.GetEntityPosition(Player));
        }

        [TestCase(Key.W, 0, -1)] [TestCase(Key.UpArrow, 0, -1)] [TestCase(Key.Numpad8, 0, -1)]
        [TestCase(Key.S, 0, 1)] [TestCase(Key.DownArrow, 0, 1)] [TestCase(Key.Numpad2, 0, 1)]
        [TestCase(Key.A, -1, 0)] [TestCase(Key.LeftArrow, -1, 0)] [TestCase(Key.Numpad4, -1, 0)]
        [TestCase(Key.D, 1, 0)] [TestCase(Key.RightArrow, 1, 0)] [TestCase(Key.Numpad6, 1, 0)]
        [TestCase(Key.H, -1, 0)] [TestCase(Key.J, 0, 1)] [TestCase(Key.K, 0, -1)]
        public void SupportedHeldMovementStillReachesItsActualCell(Key key, int dx, int dy)
        {
            var input = Input(); Keys(key); AssertHeld(key); Tick(input);
            Assert.AreEqual("Normal", Mode(input)); Assert.AreEqual((10 + dx, 10 + dy), Zone.GetEntityPosition(Player));
        }

        [TestCase(Key.LeftShift)] [TestCase(Key.RightShift)] [TestCase(Key.LeftCtrl)]
        [TestCase(Key.RightCtrl)] [TestCase(Key.LeftAlt)] [TestCase(Key.RightAlt)]
        public void ModifiersCannotReviveHeldLookMovement(Key modifier)
        {
            var input = Input(); int ticks = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Keys(Key.L, modifier); AssertHeld(Key.L); Tick(input);
            Assert.AreEqual("Normal", Mode(input)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.AreEqual(ticks, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
        }

        [TestCase(Key.L)] [TestCase(Key.D)] [TestCase(Key.RightArrow)] [TestCase(Key.Numpad6)]
        public void RealInteractDirectionKeepsModalEastBindings(Key key)
        {
            var input = Input(); var chest = Item("Chest"); Assert.IsTrue(Zone.AddEntity(chest, 11, 10));
            input.WorldActionMenuUI = Component<WorldActionMenuUI>();
            Press(Key.C, () => Tick(input)); Assert.AreEqual("AwaitingTalkDirection", Mode(input));
            Press(key, () => Tick(input)); Assert.AreEqual("WorldActionMenuOpen", Mode(input));
            Assert.AreSame(chest, input.WorldActionMenuUI.SelectedTarget); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
        }

        [Test] public void DisplayedHelpKeepsLookAndAdvertisedMovementDistinct()
        {
            var movement = ControlsReference.Bindings.Single(row => row.What.StartsWith("move ("));
            Assert.AreEqual("WASD / arrows / numpad", movement.Key);
            Assert.IsTrue(ControlsReference.Bindings.Any(row => row.Key == "L" && row.What.StartsWith("look mode")));
            var lines = new System.Collections.Generic.List<string>(); ControlsReference.PrintHelp(lines.Add);
            Assert.IsTrue(lines.Any(line => line.StartsWith("L — look mode")));
        }
    }
}
