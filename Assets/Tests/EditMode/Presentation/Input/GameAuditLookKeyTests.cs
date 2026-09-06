using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public abstract class LookKeyFixture : ShortcutFixture
    {
        protected InputHandler Input()
        {
            var input = Component<InputHandler>(); input.PlayerEntity = Player; input.CurrentZone = Zone; input.MoveRepeatDelay = 0;
            var turns = new TurnManager(); turns.AddEntity(Player); turns.ProcessUntilPlayerTurn(); input.TurnManager = turns;
            Set(input, "_lastMoveTime", -999f); return input;
        }
        protected void Keys(params Key[] keys)
        { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys)); InputSystem.Update(); }
        protected static void Tick(InputHandler input) => Call(input, "Update");
        protected static WorldCursorState Cursor(InputHandler input) => (WorldCursorState)Get(input, "_worldCursorState");
        protected static string Mode(InputHandler input) => Get(input, "_inputState").ToString();
        protected void AssertHeld(Key key)
        { InputSystem.Update(); Assert.IsTrue(Keyboard.current[key].isPressed); Assert.IsFalse(Keyboard.current[key].wasPressedThisFrame, "a genuine later input frame, not repeated fresh-edge dispatch"); }
    }
    public class GameAuditLookKeyTests : LookKeyFixture
    {
        [Test] public void HeldLookKeyInNormalCannotMoveOrSpendTurn()
        {
            var input = Input(); int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Keys(Key.L); AssertHeld(Key.L); Tick(input);
            Assert.AreEqual("Normal", Mode(input)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.AreEqual(tick, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
        }
        [TestCase(false)] [TestCase(true)]
        public void RateLimitedLookPressNeverBecomesDelayedEastMovement(bool limited)
        {
            var input = Input(); input.MoveRepeatDelay = .12f; Set(input, "_lastMoveTime", limited ? Time.time : -999f);
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Keys(Key.L); Assert.IsTrue(Keyboard.current.lKey.wasPressedThisFrame); Tick(input);
            Assert.AreEqual(limited ? "Normal" : "LookMode", Mode(input)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            AssertHeld(Key.L); Set(input, "_lastMoveTime", -999f); Tick(input);
            Assert.AreEqual(limited ? "Normal" : "LookMode", Mode(input)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.AreEqual(tick, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
        }
        [Test] public void FreshLookPressOpensCursorWithoutMovingActor()
        {
            var input = Input(); int energy = input.TurnManager.GetEnergy(Player); Press(Key.L, () => Tick(input));
            Assert.AreEqual("LookMode", Mode(input)); Assert.IsTrue(Cursor(input).Active); Assert.AreEqual(WorldCursorMode.Look, Cursor(input).Mode);
            Assert.AreEqual((10, 10), (Cursor(input).X, Cursor(input).Y)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
        }
        [TestCase(false)] [TestCase(true)]
        public void EscapeWhileHoldingLookCannotWalkAfterLeavingCursor(bool releaseLook)
        {
            var input = Input(); Press(Key.L, () => Tick(input)); int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Keys(Key.L, Key.Escape); Assert.IsFalse(Keyboard.current.lKey.wasPressedThisFrame); Assert.IsTrue(Keyboard.current.escapeKey.wasPressedThisFrame); Tick(input);
            Assert.AreEqual("Normal", Mode(input)); Assert.IsFalse(Cursor(input).Active);
            if (releaseLook) Keys(); else Keys(Key.L);
            Assert.IsFalse(Keyboard.current.lKey.wasPressedThisFrame); Tick(input);
            Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player)); Assert.AreEqual(tick, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
        }
        [Test] public void ModalLookLStillMovesCursorOnFreshPressButNotHold()
        {
            var input = Input(); Press(Key.L, () => Tick(input)); Press(Key.L, () => Tick(input));
            Assert.AreEqual((11, 10), (Cursor(input).X, Cursor(input).Y)); AssertHeld(Key.L); Tick(input);
            Assert.AreEqual((11, 10), (Cursor(input).X, Cursor(input).Y)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
        }
        [TestCase(Key.D)] [TestCase(Key.RightArrow)] [TestCase(Key.Numpad6)]
        public void AdvertisedEastKeysStillMoveAndRepeat(Key key)
        {
            var input = Input(); Press(key, () => Tick(input)); Assert.AreEqual((11, 10), Zone.GetEntityPosition(Player));
            AssertHeld(key); Tick(input); Assert.AreEqual((12, 10), Zone.GetEntityPosition(Player));
        }
        [Test] public void HeldLookDoesNotBlockFreshLegitimateEastKey()
        {
            var input = Input(); Keys(Key.L); AssertHeld(Key.L); Keys(Key.L, Key.RightArrow);
            Assert.IsFalse(Keyboard.current.lKey.wasPressedThisFrame); Assert.IsTrue(Keyboard.current.rightArrowKey.wasPressedThisFrame); Tick(input);
            Assert.AreEqual((11, 10), Zone.GetEntityPosition(Player)); Assert.AreEqual("Normal", Mode(input));
        }
        [Test] public void FreshLookAndEastTogetherKeepLookFirstPrecedence()
        {
            var input = Input(); Keys(Key.L, Key.RightArrow); Tick(input);
            Assert.AreEqual("LookMode", Mode(input)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
        }
        [Test] public void ReleaseAndRepressAfterRateLimitStillOpensLook()
        {
            var input = Input(); input.MoveRepeatDelay = .12f; Set(input, "_lastMoveTime", Time.time); Press(Key.L, () => Tick(input));
            Assert.AreEqual("Normal", Mode(input)); Set(input, "_lastMoveTime", -999f); Press(Key.L, () => Tick(input));
            Assert.AreEqual("LookMode", Mode(input)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
        }
    }
}
