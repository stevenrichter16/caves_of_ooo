using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class NativeGamepadInputTests
    {
        readonly InputTestFixture scope = new InputTestFixture();
        Gamepad pad;
        void State(GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
            InputHelper.GetKey(KeyCode.None); // Sample the actual common input boundary.
        }
        [SetUp] public void Setup()
        {
            scope.Setup(); pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            State(new GamepadState());
        }
        [TearDown] public void Cleanup() => scope.TearDown();

        [TestCase(GamepadButton.South, KeyCode.Return)]
        [TestCase(GamepadButton.East, KeyCode.Escape)]
        [TestCase(GamepadButton.West, KeyCode.I)]
        [TestCase(GamepadButton.North, KeyCode.C)]
        [TestCase(GamepadButton.LeftShoulder, KeyCode.L)]
        [TestCase(GamepadButton.RightShoulder, KeyCode.G)]
        [TestCase(GamepadButton.Start, KeyCode.Tab)]
        [TestCase(GamepadButton.Select, KeyCode.F1)]
        [TestCase(GamepadButton.LeftStick, KeyCode.X)]
        [TestCase(GamepadButton.RightStick, KeyCode.M)]
        public void BuiltInButtonHasOnePressHeldAndRelease(GamepadButton button, KeyCode key)
        {
            Assert.False(InputHelper.GetKey(key));
            State(new GamepadState().WithButton(button));
            Assert.True(InputHelper.GetKeyDown(key)); Assert.True(InputHelper.GetKey(key));
            Assert.False(InputHelper.GetKeyDown(KeyCode.F12), "Never grant invincibility.");
            State(new GamepadState().WithButton(button));
            Assert.False(InputHelper.GetKeyDown(key)); Assert.True(InputHelper.GetKey(key));
            State(new GamepadState());
            Assert.True(InputHelper.GetKeyUp(key)); Assert.False(InputHelper.GetKey(key));
            State(new GamepadState()); Assert.False(InputHelper.GetKeyUp(key));
        }

        [TestCase(GamepadButton.South, KeyCode.Alpha1, KeyCode.Return)]
        [TestCase(GamepadButton.East, KeyCode.Alpha2, KeyCode.Escape)]
        [TestCase(GamepadButton.West, KeyCode.Alpha3, KeyCode.I)]
        [TestCase(GamepadButton.North, KeyCode.Alpha4, KeyCode.C)]
        [TestCase(GamepadButton.LeftShoulder, KeyCode.Alpha5, KeyCode.L)]
        [TestCase(GamepadButton.RightShoulder, KeyCode.Alpha6, KeyCode.G)]
        [TestCase(GamepadButton.Start, KeyCode.Alpha7, KeyCode.Tab)]
        [TestCase(GamepadButton.Select, KeyCode.Alpha8, KeyCode.F1)]
        [TestCase(GamepadButton.LeftStick, KeyCode.Alpha9, KeyCode.X)]
        [TestCase(GamepadButton.RightStick, KeyCode.Alpha0, KeyCode.M)]
        public void AbilityChordDoesNotLeakBaseActionWhenModifierReleased(GamepadButton button, KeyCode slot, KeyCode ordinary)
        {
            State(new GamepadState { leftTrigger = 1 }.WithButton(button));
            Assert.True(InputHelper.GetKeyDown(slot)); Assert.False(InputHelper.GetKeyDown(ordinary));
            State(new GamepadState().WithButton(button));
            Assert.False(InputHelper.GetKeyDown(slot)); Assert.False(InputHelper.GetKey(ordinary));
            State(new GamepadState());
            State(new GamepadState().WithButton(button));
            Assert.True(InputHelper.GetKeyDown(ordinary)); Assert.False(InputHelper.GetKey(slot));
        }

        [Test] public void ModifierPressedAfterHeldConfirmDoesNotCast()
        {
            State(new GamepadState().WithButton(GamepadButton.South));
            Assert.True(InputHelper.GetKeyDown(KeyCode.Return));
            State(new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.South));
            Assert.False(InputHelper.GetKeyDown(KeyCode.Alpha1));
            Assert.False(InputHelper.GetKeyDown(KeyCode.Return));
        }

        [Test] public void TriggerWaitIsSuppressedForEntireModifiedPress()
        {
            State(new GamepadState { rightTrigger = 1 }); Assert.True(InputHelper.GetKeyDown(KeyCode.Period));
            State(new GamepadState());
            State(new GamepadState { leftTrigger = 1, rightTrigger = 1 }); Assert.False(InputHelper.GetKey(KeyCode.Period));
            State(new GamepadState { rightTrigger = 1 }); Assert.False(InputHelper.GetKey(KeyCode.Period));
        }

        [TestCase(-1,-1)] [TestCase(0,-1)] [TestCase(1,-1)]
        [TestCase(-1,0)] [TestCase(1,0)]
        [TestCase(-1,1)] [TestCase(0,1)] [TestCase(1,1)]
        public void StickSupportsEightNativeMovementAndTargetingDirections(int dx, int dy)
        {
            var state = new GamepadState { leftStick = new Vector2(dx, -dy) };
            State(state);
            AssertDirection("GetMoveInput", true, dx, dy);
            AssertDirection("GetDirectionKeyDown", true, dx, dy);
            State(state);
            AssertDirection("GetMoveInput", true, dx, dy);
            AssertDirection("GetDirectionKeyDown", false, 0, 0);
            State(new GamepadState()); AssertDirection("GetMoveInput", false, 0, 0);
        }

        [Test] public void DpadDiagonalIsNotReducedToCardinal()
        {
            State(new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.DpadRight));
            AssertDirection("GetMoveInput", true, 1, -1);
        }

        [Test] public void StickDriftDoesNotMoveOrWait()
        {
            State(new GamepadState { leftStick = new Vector2(.15f, -.2f), rightTrigger = .15f });
            AssertDirection("GetMoveInput", false, 0, 0); Assert.False(InputHelper.GetKey(KeyCode.Period));
        }

        [TestCase(GamepadButton.DpadUp, KeyCode.Comma)]
        [TestCase(GamepadButton.DpadDown, KeyCode.Period)]
        public void TravelChordUsesShiftWithoutMoving(GamepadButton direction, KeyCode key)
        {
            State(new GamepadState { leftTrigger = 1 }.WithButton(direction));
            Assert.True(InputHelper.GetKeyDown(key)); Assert.True(InputHelper.GetKey(KeyCode.LeftShift));
            AssertDirection("GetMoveInput", false, 0, 0);
            State(new GamepadState().WithButton(direction)); AssertDirection("GetMoveInput", false, 0, 0);
            State(new GamepadState()); State(new GamepadState().WithButton(direction));
            AssertDirection("GetMoveInput", true, 0, direction == GamepadButton.DpadUp ? -1 : 1);
        }

        [Test] public void RightStickCyclesSlotsAndPagesWithoutWalking()
        {
            State(new GamepadState { rightStick = Vector2.right });
            Assert.True(InputHelper.GetKeyDown(KeyCode.RightBracket)); AssertDirection("GetMoveInput", false, 0, 0);
            State(new GamepadState()); State(new GamepadState { rightStick = Vector2.up });
            Assert.True(InputHelper.GetKeyDown(KeyCode.PageUp)); AssertDirection("GetMoveInput", false, 0, 0);
        }

        [Test] public void KeyboardWorksWithIdleGamepadAndAfterDisconnect()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); InputSystem.Update();
            Assert.True(InputHelper.GetKeyDown(KeyCode.I));
            State(new GamepadState().WithButton(GamepadButton.DpadLeft));
            Assert.True(InputHelper.GetKey(KeyCode.LeftArrow));
            InputSystem.RemoveDevice(pad); InputSystem.Update();
            Assert.False(InputHelper.GetKey(KeyCode.LeftArrow)); Assert.True(InputHelper.GetKey(KeyCode.I));
        }

        [Test] public void ControllerCanChooseAStartingBuildExactlyOnce()
        {
            var menu = new StartingBuildMenuController(); int choices = 0; string chosen = null;
            menu.Open(new[] { new StartingBuildDef { Id="a", Name="A" }, new StartingBuildDef { Id="b", Name="B" } },
                def => { choices++; chosen = def.Id; });
            var probe = new Probe();
            State(new GamepadState().WithButton(GamepadButton.DpadDown)); menu.Tick(probe);
            Assert.AreEqual(1, menu.SelectedIndex);
            State(new GamepadState()); State(new GamepadState().WithButton(GamepadButton.South)); menu.Tick(probe);
            Assert.AreEqual("b", chosen); Assert.AreEqual(1, choices); Assert.False(menu.IsOpen);
            menu.Tick(probe); Assert.AreEqual(1, choices);
        }
        [TestCase(false)] [TestCase(true)]
        public void SavedGameMenusAcceptControllerConfirmAndNewGame(bool death)
        {
            var field = typeof(InputHandler).GetField(death ? "_deathGamepadProbe" : "_bootGamepadProbe",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field, "Saved-game gates need their own controller choice mapping.");
            var probe = (IInputProbe)field.GetValue(null);
            var service = new Saves(); var restarter = new Restarter();
            var boot = new BootMenuController(); var dead = new DeathScreenController();
            System.Action activate = () => { if (death) dead.Activate(null); else boot.TryActivate(true, null); };
            System.Action tick = () => { if (death) dead.Tick(probe, service, restarter, null); else boot.Tick(probe, service, null); };
            activate(); tick(); Assert.AreEqual(0, service.Loads);
            State(new GamepadState().WithButton(GamepadButton.South)); tick();
            Assert.AreEqual(1, service.Loads); Assert.AreEqual(0, service.NewGames + restarter.Calls);
            State(new GamepadState()); activate();
            State(new GamepadState().WithButton(GamepadButton.West)); tick();
            Assert.AreEqual(1, service.NewGames + restarter.Calls); Assert.AreEqual(1, service.Loads);
            tick(); Assert.AreEqual(1, service.NewGames + restarter.Calls, "One press, one choice.");
            // LT+X remains an ability chord, never a new character at a modal gate.
            State(new GamepadState()); activate();
            State(new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.West)); tick();
            Assert.AreEqual(1, service.NewGames + restarter.Calls);
        }
        sealed class Saves : ISaveLoadService
        {
            public int Loads, NewGames;
            public bool HasQuickSave() => true;
            public bool QuickLoad() { Loads++; return true; }
            public bool QuickSave() => true;
            public bool BeginNewGame() { NewGames++; return true; }
        }
        sealed class Restarter : ISceneRestarter { public int Calls; public void Restart() => Calls++; }

        sealed class Probe : IInputProbe { public bool GetKeyDown(KeyCode key) => InputHelper.GetKeyDown(key); }
        static void AssertDirection(string method, bool expected, int dx, int dy)
        {
            var root = new GameObject("Owned gamepad input test"); root.SetActive(false);
            try
            {
                var input = root.AddComponent<InputHandler>(); var args = new object[] { 0, 0 };
                var result = (bool)typeof(InputHandler).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, args);
                Assert.AreEqual(expected, result, method); Assert.AreEqual(dx, args[0]); Assert.AreEqual(dy, args[1]);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
