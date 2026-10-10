using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual Input System events at the context-aware controller boundary.</summary>
    public sealed class NativeGamepadInputTests
    {
        readonly InputTestFixture scope = new InputTestFixture();
        Gamepad pad;

        void State(GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
            InputHelper.GetKey(KeyCode.None);
        }
        void Context(GamepadInputContext context)
        {
            State(new GamepadState());
            NativeGamepadInput.SetContext(context);
            State(new GamepadState());
        }
        [SetUp] public void Setup()
        {
            scope.Setup(); pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            State(new GamepadState());
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            State(new GamepadState());
        }
        [TearDown] public void Cleanup()
        {
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            scope.TearDown();
        }

        [TestCase(GamepadButton.South, KeyCode.Return)]
        [TestCase(GamepadButton.East, KeyCode.Escape)]
        [TestCase(GamepadButton.West, KeyCode.I)]
        [TestCase(GamepadButton.North, KeyCode.Tab)]
        [TestCase(GamepadButton.Start, KeyCode.Tab)]
        [TestCase(GamepadButton.Select, KeyCode.Escape)]
        [TestCase(GamepadButton.LeftShoulder, KeyCode.LeftArrow)]
        [TestCase(GamepadButton.RightShoulder, KeyCode.RightArrow)]
        public void MenuButtonHasOnePressHeldAndRelease(GamepadButton button, KeyCode key)
        {
            State(new GamepadState().WithButton(button));
            Assert.True(InputHelper.GetKeyDown(key)); Assert.True(InputHelper.GetKey(key));
            Assert.False(InputHelper.GetKeyDown(KeyCode.F12));
            AssertNoWorldCommands();
            State(new GamepadState().WithButton(button));
            Assert.False(InputHelper.GetKeyDown(key)); Assert.True(InputHelper.GetKey(key));
            State(new GamepadState());
            Assert.True(InputHelper.GetKeyUp(key)); Assert.False(InputHelper.GetKey(key));
            State(new GamepadState()); Assert.False(InputHelper.GetKeyUp(key));
        }

        [TestCase(GamepadButton.South, GamepadCommand.Interact)]
        [TestCase(GamepadButton.East, GamepadCommand.WaitUntilHealed)]
        [TestCase(GamepadButton.West, GamepadCommand.ActivateAbility)]
        [TestCase(GamepadButton.North, GamepadCommand.Walk)]
        [TestCase(GamepadButton.LeftShoulder, GamepadCommand.AttackNearest)]
        [TestCase(GamepadButton.RightShoulder, GamepadCommand.Fire)]
        [TestCase(GamepadButton.Start, GamepadCommand.Character)]
        [TestCase(GamepadButton.Select, GamepadCommand.Pause)]
        [TestCase(GamepadButton.LeftStick, GamepadCommand.PointOfInterest)]
        [TestCase(GamepadButton.RightStick, GamepadCommand.Reload)]
        public void WorldButtonProducesTypedCommandWithoutKeyboardLeak(GamepadButton button, GamepadCommand command)
        {
            Context(GamepadInputContext.World);
            State(new GamepadState().WithButton(button));
            Assert.True(NativeGamepadInput.IsPressed(command));
            Assert.True(NativeGamepadInput.IsHeld(command));
            Assert.True(NativeGamepadInput.AnyPressed);
            AssertNoGamepadKeys();
            State(new GamepadState().WithButton(button));
            Assert.False(NativeGamepadInput.IsPressed(command));
            Assert.True(NativeGamepadInput.IsHeld(command));
            Assert.False(NativeGamepadInput.AnyPressed);
            State(new GamepadState()); Assert.False(NativeGamepadInput.IsHeld(command));
        }

        [TestCase(GamepadButton.South, GamepadCommand.InteractDirection, GamepadCommand.Interact)]
        [TestCase(GamepadButton.East, GamepadCommand.WaitMenu, GamepadCommand.WaitUntilHealed)]
        [TestCase(GamepadButton.West, GamepadCommand.Abilities, GamepadCommand.ActivateAbility)]
        [TestCase(GamepadButton.North, GamepadCommand.AutoExplore, GamepadCommand.Walk)]
        [TestCase(GamepadButton.LeftShoulder, GamepadCommand.ForceAttack, GamepadCommand.AttackNearest)]
        [TestCase(GamepadButton.RightShoulder, GamepadCommand.Throw, GamepadCommand.Fire)]
        [TestCase(GamepadButton.Start, GamepadCommand.Help, GamepadCommand.Character)]
        [TestCase(GamepadButton.RightStick, GamepadCommand.ReplaceCell, GamepadCommand.Reload)]
        public void ModifiedWorldPressRetainsItsMeaningUntilButtonRelease(GamepadButton button, GamepadCommand modified, GamepadCommand ordinary)
        {
            Context(GamepadInputContext.World);
            State(new GamepadState { leftTrigger = 1 }.WithButton(button));
            Assert.True(NativeGamepadInput.IsPressed(modified));
            Assert.False(NativeGamepadInput.IsHeld(ordinary));
            State(new GamepadState().WithButton(button));
            Assert.False(NativeGamepadInput.IsPressed(modified));
            Assert.True(NativeGamepadInput.IsHeld(modified));
            Assert.False(NativeGamepadInput.IsHeld(ordinary));
            State(new GamepadState()); State(new GamepadState().WithButton(button));
            Assert.True(NativeGamepadInput.IsPressed(ordinary));
            Assert.False(NativeGamepadInput.IsHeld(modified));
        }

        [TestCase(GamepadButton.DpadLeft, false, GamepadCommand.PreviousAbility)]
        [TestCase(GamepadButton.DpadRight, false, GamepadCommand.NextAbility)]
        [TestCase(GamepadButton.DpadUp, false, GamepadCommand.Ascend)]
        [TestCase(GamepadButton.DpadDown, false, GamepadCommand.Descend)]
        [TestCase(GamepadButton.DpadUp, true, GamepadCommand.PreviousAbilityPage)]
        [TestCase(GamepadButton.DpadDown, true, GamepadCommand.NextAbilityPage)]
        [TestCase(GamepadButton.DpadLeft, true, GamepadCommand.ZoomIn)]
        [TestCase(GamepadButton.DpadRight, true, GamepadCommand.ZoomOut)]
        public void WorldDpadUsesCommandsInsteadOfWalking(GamepadButton button, bool modified, GamepadCommand command)
        {
            Context(GamepadInputContext.World);
            State(new GamepadState { leftTrigger = modified ? 1 : 0 }.WithButton(button));
            Assert.True(NativeGamepadInput.IsPressed(command));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
            AssertNoGamepadKeys();
        }

        [TestCase(-1,-1)] [TestCase(0,-1)] [TestCase(1,-1)]
        [TestCase(-1,0)] [TestCase(1,0)]
        [TestCase(-1,1)] [TestCase(0,1)] [TestCase(1,1)]
        public void WorldSelectsEightDirectionsButMovesOnlyWhileRightTriggerHeld(int dx, int dy)
        {
            Context(GamepadInputContext.World);
            var state = new GamepadState { leftStick = new Vector2(dx, -dy) };
            State(state);
            Assert.True(NativeGamepadInput.TrySelectedDirection(out int sx, out int sy));
            Assert.AreEqual(dx, sx); Assert.AreEqual(dy, sy);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.TryDirection(true, out _, out _));
            AssertNoGamepadKeys();
            state.rightTrigger = 1; State(state);
            Assert.True(NativeGamepadInput.TryMoveDirection(out int mx, out int my));
            Assert.AreEqual(dx, mx); Assert.AreEqual(dy, my);
            State(state); Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
            state.leftStick = Vector2.zero; State(state);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _), "No remembered movement after releasing LS.");
        }

        [Test] public void RightTriggerWithoutDirectionRequestsStepForCallerToWait()
        {
            Context(GamepadInputContext.World);
            State(new GamepadState { rightTrigger = 1 });
            Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.Step));
            Assert.True(NativeGamepadInput.IsHeld(GamepadCommand.Step));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.IsHeld(GamepadCommand.WaitUntilHealed));
            AssertNoGamepadKeys();
        }

        [Test] public void HighlightChordDoesNotBecomeMovementWhenLeftTriggerReleased()
        {
            Context(GamepadInputContext.World);
            State(new GamepadState { leftStick = Vector2.right, leftTrigger = 1, rightTrigger = 1 });
            Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.Highlight));
            Assert.True(NativeGamepadInput.IsHighlightHeld);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            State(new GamepadState { leftStick = Vector2.right, rightTrigger = 1 });
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            State(new GamepadState { leftStick = Vector2.right });
            State(new GamepadState { leftStick = Vector2.right, rightTrigger = 1 });
            Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [TestCase(-1,-1)] [TestCase(0,-1)] [TestCase(1,-1)]
        [TestCase(-1,0)] [TestCase(1,0)]
        [TestCase(-1,1)] [TestCase(0,1)] [TestCase(1,1)]
        public void TargetingDirectionsPreserveDiagonalsAndSeparateConfirmation(int dx, int dy)
        {
            Context(GamepadInputContext.Targeting);
            var state = new GamepadState { leftStick = new Vector2(dx, -dy) };
            State(state);
            Assert.True(NativeGamepadInput.TryDirection(true, out int tx, out int ty));
            Assert.AreEqual(dx, tx); Assert.AreEqual(dy, ty);
            Assert.True(NativeGamepadInput.TrySelectedDirection(out int sx, out int sy));
            Assert.AreEqual(dx, sx); Assert.AreEqual(dy, sy);
            Assert.False(InputHelper.GetKeyDown(KeyCode.Return));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            State(state);
            Assert.False(NativeGamepadInput.TryDirection(true, out _, out _));
            Assert.True(NativeGamepadInput.TryDirection(false, out _, out _));
            AssertNoWorldCommands();
        }

        [TestCase(GamepadButton.South, KeyCode.Return)]
        [TestCase(GamepadButton.East, KeyCode.Escape)]
        [TestCase(GamepadButton.West, KeyCode.Period)]
        public void TargetingButtonsConfirmCancelOrChooseUnderfoot(GamepadButton button, KeyCode key)
        {
            Context(GamepadInputContext.Targeting);
            State(new GamepadState().WithButton(button));
            Assert.True(InputHelper.GetKeyDown(key)); AssertNoWorldCommands();
        }

        [Test] public void TargetingRightTriggerConfirmsWithoutWorldMovement()
        {
            Context(GamepadInputContext.Targeting);
            State(new GamepadState { leftStick = Vector2.up, rightTrigger = 1 });
            Assert.True(InputHelper.GetKeyDown(KeyCode.Return));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [Test] public void WorldRightStickLooksWithoutChangingFacingOrCasting()
        {
            Context(GamepadInputContext.World);
            State(new GamepadState { rightStick = Vector2.right });
            Assert.True(NativeGamepadInput.TryLookDirection(out int dx, out int dy));
            Assert.AreEqual(1, dx); Assert.AreEqual(0, dy);
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.ActivateAbility));
            AssertNoGamepadKeys();
        }

        [Test] public void MenuStickNavigatesAndRightStickPages()
        {
            State(new GamepadState { leftStick = Vector2.down });
            Assert.True(InputHelper.GetKeyDown(KeyCode.DownArrow));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            State(new GamepadState { rightStick = Vector2.up });
            Assert.True(InputHelper.GetKeyDown(KeyCode.PageUp));
            AssertNoWorldCommands();
        }

        [Test] public void MenuDetailChordUsesHelpWithoutConfirmation()
        {
            State(new GamepadState { leftTrigger = 1, rightTrigger = 1 });
            Assert.True(InputHelper.GetKeyDown(KeyCode.F1));
            Assert.False(InputHelper.GetKey(KeyCode.Return));
            AssertNoWorldCommands();
        }

        [Test] public void KeyboardOnlyFallbackNeverReadsGamepadArrows()
        {
            State(new GamepadState().WithButton(GamepadButton.DpadLeft));
            Assert.True(InputHelper.GetKey(KeyCode.LeftArrow));
            Assert.False(InputHelper.GetKeyboardKey(KeyCode.LeftArrow));
            Assert.False(InputHelper.GetKeyboardKeyDown(KeyCode.LeftArrow));
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftArrow)); InputSystem.Update();
            Assert.True(InputHelper.GetKeyboardKey(KeyCode.LeftArrow));
            Assert.True(InputHelper.GetKeyboardKeyDown(KeyCode.LeftArrow));
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
            Assert.NotNull(field);
            var probe = (IInputProbe)field.GetValue(null);
            var service = new Saves(); var restarter = new Restarter();
            var boot = new BootMenuController(); var dead = new DeathScreenController();
            Action activate = () => { if (death) dead.Activate(null); else boot.TryActivate(true, null); };
            Action tick = () => { if (death) dead.Tick(probe, service, restarter, null); else boot.Tick(probe, service, null); };
            activate(); tick(); Assert.AreEqual(0, service.Loads);
            State(new GamepadState().WithButton(GamepadButton.South)); tick();
            Assert.AreEqual(1, service.Loads); Assert.AreEqual(0, service.NewGames + restarter.Calls);
            State(new GamepadState()); activate();
            State(new GamepadState().WithButton(GamepadButton.West)); tick();
            Assert.AreEqual(1, service.NewGames + restarter.Calls); Assert.AreEqual(1, service.Loads);
            tick(); Assert.AreEqual(1, service.NewGames + restarter.Calls);
        }

        static void AssertNoGamepadKeys()
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                Assert.False(NativeGamepadInput.GetKey(key), "World must not synthesize " + key);
        }
        static void AssertNoWorldCommands()
        {
            foreach (GamepadCommand command in Enum.GetValues(typeof(GamepadCommand)))
            {
                Assert.False(NativeGamepadInput.IsPressed(command), command.ToString());
                Assert.False(NativeGamepadInput.IsHeld(command), command.ToString());
            }
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
    }
}
