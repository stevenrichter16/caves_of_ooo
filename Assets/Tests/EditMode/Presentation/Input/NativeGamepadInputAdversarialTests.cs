using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    /// <summary>Release-order, context, device and mixed-input counterchecks.</summary>
    public sealed class NativeGamepadInputAdversarialTests
    {
        readonly InputTestFixture scope = new InputTestFixture();
        Gamepad pad;
        void State(GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state); InputSystem.Update();
            InputHelper.GetKey(KeyCode.None);
        }
        void Context(GamepadInputContext context)
        {
            State(new GamepadState()); NativeGamepadInput.SetContext(context); State(new GamepadState());
        }
        [SetUp] public void Setup()
        {
            scope.Setup(); pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            State(new GamepadState()); NativeGamepadInput.SetContext(GamepadInputContext.World); State(new GamepadState());
        }
        [TearDown] public void Cleanup()
        {
            NativeGamepadInput.SetContext(GamepadInputContext.Menu); scope.TearDown();
        }

        [TestCase(GamepadButton.South, GamepadCommand.Interact, GamepadCommand.InteractDirection)]
        [TestCase(GamepadButton.East, GamepadCommand.WaitUntilHealed, GamepadCommand.WaitMenu)]
        [TestCase(GamepadButton.West, GamepadCommand.ActivateAbility, GamepadCommand.Abilities)]
        [TestCase(GamepadButton.North, GamepadCommand.Walk, GamepadCommand.AutoExplore)]
        [TestCase(GamepadButton.LeftShoulder, GamepadCommand.AttackNearest, GamepadCommand.ForceAttack)]
        [TestCase(GamepadButton.RightShoulder, GamepadCommand.Fire, GamepadCommand.Throw)]
        public void Adversarial_ModifierAfterButtonCannotCreateAlternateAction(GamepadButton button, GamepadCommand ordinary, GamepadCommand alternate)
        {
            State(new GamepadState().WithButton(button)); Assert.True(NativeGamepadInput.IsPressed(ordinary));
            State(new GamepadState { leftTrigger = 1 }.WithButton(button));
            Assert.True(NativeGamepadInput.IsHeld(ordinary));
            Assert.False(NativeGamepadInput.IsPressed(ordinary));
            Assert.False(NativeGamepadInput.IsHeld(alternate));
        }

        [TestCase(GamepadButton.South, KeyCode.Return)]
        [TestCase(GamepadButton.East, KeyCode.Escape)]
        [TestCase(GamepadButton.West, KeyCode.I)]
        [TestCase(GamepadButton.North, KeyCode.Tab)]
        public void Adversarial_HeldWorldButtonCannotCascadeIntoMenu(GamepadButton button, KeyCode key)
        {
            State(new GamepadState().WithButton(button));
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            Assert.False(InputHelper.GetKeyDown(key), "Same input update must be quarantined too.");
            State(new GamepadState().WithButton(button));
            Assert.False(InputHelper.GetKey(key)); Assert.False(NativeGamepadInput.AnyPressed);
            State(new GamepadState()); State(new GamepadState().WithButton(button));
            Assert.True(InputHelper.GetKeyDown(key));
        }

        [Test] public void Adversarial_HighlightChordCannotConfirmTargetOrThrow()
        {
            Context(GamepadInputContext.Targeting);
            State(new GamepadState { leftTrigger = 1, rightTrigger = 1, leftStick = Vector2.right });
            Assert.False(InputHelper.GetKeyDown(KeyCode.Return), "LT + RT highlights; it must never commit a targeted action.");
            State(new GamepadState { rightTrigger = 1, leftStick = Vector2.right });
            Assert.False(InputHelper.GetKeyDown(KeyCode.Return), "Releasing LT must not turn a held highlight into confirm.");
        }

        [TestCase(GamepadButton.West, GamepadCommand.Abilities)]
        [TestCase(GamepadButton.North, GamepadCommand.AutoExplore)]
        public void Adversarial_HeldModifierMustRearmAfterContextChange(GamepadButton button, GamepadCommand command)
        {
            State(new GamepadState { leftTrigger = 1 });
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            NativeGamepadInput.SetContext(GamepadInputContext.World);
            State(new GamepadState { leftTrigger = 1 }.WithButton(button));
            Assert.False(NativeGamepadInput.IsPressed(command));
            State(new GamepadState());
            State(new GamepadState { leftTrigger = 1 }.WithButton(button));
            Assert.True(NativeGamepadInput.IsPressed(command));
        }

        [Test] public void Adversarial_HeldMenuConfirmCannotInteractOnReturnToWorld()
        {
            Context(GamepadInputContext.Menu);
            State(new GamepadState().WithButton(GamepadButton.South)); Assert.True(InputHelper.GetKeyDown(KeyCode.Return));
            NativeGamepadInput.SetContext(GamepadInputContext.World);
            State(new GamepadState().WithButton(GamepadButton.South));
            Assert.False(NativeGamepadInput.IsHeld(GamepadCommand.Interact));
            Assert.False(InputHelper.GetKey(KeyCode.Return));
            State(new GamepadState()); State(new GamepadState().WithButton(GamepadButton.South));
            Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.Interact));
        }

        [Test] public void Adversarial_TargetingTriggerAndStickCannotStepWhenTargetingCloses()
        {
            Context(GamepadInputContext.Targeting);
            var held = new GamepadState { leftStick = Vector2.right, rightTrigger = 1 };
            State(held); Assert.True(InputHelper.GetKeyDown(KeyCode.Return));
            NativeGamepadInput.SetContext(GamepadInputContext.World); State(held);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
            held.rightTrigger = 0; State(held);
            held.rightTrigger = 1; State(held);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _), "LS is still quarantined until neutral.");
            State(new GamepadState()); State(held);
            Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [Test] public void Adversarial_HeldWorldDirectionCannotPickFreshTarget()
        {
            State(new GamepadState { leftStick = Vector2.up });
            NativeGamepadInput.SetContext(GamepadInputContext.Targeting);
            State(new GamepadState { leftStick = Vector2.up });
            Assert.False(NativeGamepadInput.TryDirection(true, out _, out _));
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
            State(new GamepadState()); State(new GamepadState { leftStick = Vector2.up });
            Assert.True(NativeGamepadInput.TrySelectedDirection(out _, out _));
        }

        [Test] public void Adversarial_QuarantineIsPerControlRatherThanAllControlsNeutral()
        {
            State(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South));
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            State(new GamepadState().WithButton(GamepadButton.South));
            State(new GamepadState { leftStick = Vector2.down }.WithButton(GamepadButton.South));
            Assert.True(InputHelper.GetKeyDown(KeyCode.DownArrow), "Released LS can navigate while the old A remains held.");
            Assert.False(InputHelper.GetKey(KeyCode.Return));
        }

        [Test] public void Adversarial_RepeatedSameContextDoesNotSuppressHeldMovement()
        {
            var held = new GamepadState { leftStick = Vector2.right, rightTrigger = 1 };
            State(held); Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
            NativeGamepadInput.SetContext(GamepadInputContext.World); State(held);
            Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [Test] public void Adversarial_ContextRoundTripCannotReviveSamePress()
        {
            State(new GamepadState().WithButton(GamepadButton.South));
            Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.Interact));
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            NativeGamepadInput.SetContext(GamepadInputContext.World);
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.Interact));
            Assert.False(NativeGamepadInput.AnyPressed);
        }

        [Test] public void Adversarial_ModifierDuringMovementStopsUntilTriggerRearmed()
        {
            var held = new GamepadState { leftStick = Vector2.right, rightTrigger = 1 };
            State(held); Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
            held.leftTrigger = 1; State(held);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.Highlight), "No alternate command on a held trigger.");
            held.leftTrigger = 0; State(held);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            held.rightTrigger = 0; State(held); held.rightTrigger = 1; State(held);
            Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [Test] public void Adversarial_DriftAndPartialTriggersHaveNoIntent()
        {
            State(new GamepadState { leftStick = new Vector2(.15f, -.2f), rightStick = new Vector2(-.2f, .15f), leftTrigger = .15f, rightTrigger = .15f });
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
            Assert.False(NativeGamepadInput.TryLookDirection(out _, out _));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.IsHighlightHeld); Assert.False(NativeGamepadInput.AnyPressed);
        }

        // QueueStateEvent values pass through Unity's stick deadzone processor
        // before the adapter's hysteresis. These raw values straddle that second boundary.
        [Test] public void Adversarial_StickHysteresisDoesNotChatterAtActivationBoundary()
        {
            State(new GamepadState { leftStick = new Vector2(.65f, 0) });
            Assert.True(NativeGamepadInput.TrySelectedDirection(out _, out _)); Assert.True(NativeGamepadInput.AnyPressed);
            State(new GamepadState { leftStick = new Vector2(.5f, 0) });
            Assert.True(NativeGamepadInput.TrySelectedDirection(out _, out _)); Assert.False(NativeGamepadInput.AnyPressed);
            State(new GamepadState { leftStick = new Vector2(.3f, 0) });
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
            State(new GamepadState { leftStick = new Vector2(.5f, 0) });
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _));
        }

        [Test] public void Adversarial_TargetDpadPriorityPreservesDiagonalAgainstOtherSticks()
        {
            Context(GamepadInputContext.Targeting);
            State(new GamepadState { leftStick = Vector2.down, rightStick = Vector2.left }
                .WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.DpadRight));
            Assert.True(NativeGamepadInput.TryDirection(true, out int dx, out int dy));
            Assert.AreEqual(1, dx); Assert.AreEqual(-1, dy);
        }

        [Test] public void Adversarial_TargetRightStickWorksWithoutLeftStick()
        {
            Context(GamepadInputContext.Targeting);
            State(new GamepadState { rightStick = new Vector2(-1, -1) });
            Assert.True(NativeGamepadInput.TryDirection(true, out int dx, out int dy));
            Assert.AreEqual(-1, dx); Assert.AreEqual(1, dy);
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _, out _), "Paid direction selection remains LS-owned.");
        }

        [Test] public void Adversarial_WorldDpadAndTriggerCannotAccidentallyMove()
        {
            State(new GamepadState { rightTrigger = 1 }.WithButton(GamepadButton.DpadUp));
            Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.Ascend));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [TestCase(GamepadButton.DpadUp, GamepadCommand.PreviousAbilityPage, GamepadCommand.Ascend)]
        [TestCase(GamepadButton.DpadRight, GamepadCommand.ZoomOut, GamepadCommand.NextAbility)]
        public void Adversarial_ModifiedDpadReleaseCannotLeakBaseCommand(GamepadButton direction, GamepadCommand modified, GamepadCommand ordinary)
        {
            State(new GamepadState { leftTrigger = 1 }.WithButton(direction));
            Assert.True(NativeGamepadInput.IsPressed(modified));
            State(new GamepadState().WithButton(direction));
            Assert.False(NativeGamepadInput.IsPressed(ordinary)); Assert.False(NativeGamepadInput.IsHeld(ordinary));
            State(new GamepadState()); State(new GamepadState().WithButton(direction));
            Assert.True(NativeGamepadInput.IsPressed(ordinary));
        }

        [Test] public void Adversarial_DisconnectClearsGamepadButPreservesKeyboard()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
            State(new GamepadState { leftStick = Vector2.right, rightTrigger = 1 }.WithButton(GamepadButton.East));
            Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _));
            InputSystem.RemoveDevice(pad); InputSystem.Update();
            Assert.False(NativeGamepadInput.IsConnected);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            Assert.False(NativeGamepadInput.IsHeld(GamepadCommand.WaitUntilHealed));
            Assert.True(InputHelper.GetKeyboardKey(KeyCode.W)); Assert.True(InputHelper.GetKey(KeyCode.W));
        }

        [Test] public void Adversarial_NewDeviceHeldStateRequiresNeutralBeforeWorldAction()
        {
            State(new GamepadState());
            var replacement = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(replacement, new GamepadState { leftStick = Vector2.right, rightTrigger = 1 }.WithButton(GamepadButton.South));
            InputSystem.Update(); replacement.MakeCurrent();
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.Interact));
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
            pad = replacement;
            State(new GamepadState()); State(new GamepadState { leftStick = Vector2.right, rightTrigger = 1 }.WithButton(GamepadButton.South));
            Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.Interact));
            Assert.True(NativeGamepadInput.TryMoveDirection(out _, out _), "Device changes preserve caller's World context.");
        }

        [Test] public void Adversarial_QueriesAreStableWithinOneInputUpdate()
        {
            State(new GamepadState().WithButton(GamepadButton.West));
            for (int i = 0; i < 4; i++)
            {
                Assert.True(NativeGamepadInput.IsPressed(GamepadCommand.ActivateAbility));
                Assert.True(NativeGamepadInput.AnyPressed);
            }
            State(new GamepadState().WithButton(GamepadButton.West));
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.ActivateAbility));
            Assert.False(NativeGamepadInput.AnyPressed);
        }

        [Test] public void Adversarial_DirectionChangeCanInterruptWithoutRequestingMovement()
        {
            State(new GamepadState { leftStick = Vector2.up }); Assert.True(NativeGamepadInput.AnyPressed);
            State(new GamepadState { leftStick = Vector2.up }); Assert.False(NativeGamepadInput.AnyPressed);
            State(new GamepadState { leftStick = Vector2.right }); Assert.True(NativeGamepadInput.AnyPressed);
            Assert.False(NativeGamepadInput.TryMoveDirection(out _, out _));
        }

        [TestCase(GamepadButton.South, KeyCode.Return)]
        [TestCase(GamepadButton.East, KeyCode.Escape)]
        [TestCase(GamepadButton.West, KeyCode.I)]
        [TestCase(GamepadButton.North, KeyCode.Tab)]
        public void Adversarial_MenuModifiedFaceButtonCannotLeakAfterModifierRelease(GamepadButton button, KeyCode ordinary)
        {
            Context(GamepadInputContext.Menu);
            State(new GamepadState { leftTrigger = 1 }.WithButton(button));
            Assert.False(InputHelper.GetKey(ordinary));
            State(new GamepadState().WithButton(button)); Assert.False(InputHelper.GetKey(ordinary));
            State(new GamepadState()); State(new GamepadState().WithButton(button));
            Assert.True(InputHelper.GetKeyDown(ordinary));
        }
    }
}
