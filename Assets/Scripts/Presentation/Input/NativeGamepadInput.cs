using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Rendering
{
    public enum GamepadInputContext { World, Menu, Targeting }
    public enum GamepadCommand
    {
        None, Interact, InteractDirection, WaitUntilHealed, WaitMenu, Step,
        ActivateAbility, Abilities, Walk, AutoExplore, AttackNearest, ForceAttack,
        Fire, Throw, Character, Pause, PointOfInterest, Reload, ReplaceCell, Help,
        Highlight, Ascend, Descend, PreviousAbility, NextAbility,
        PreviousAbilityPage, NextAbilityPage, ZoomIn, ZoomOut, Look
    }

    /// <summary>Samples standard gamepads once per input update. World commands
    /// never synthesize keyboard actions; menu/targeting keys stay contextual.
    /// A physical press keeps its meaning until release. Changing context or
    /// device quarantines held controls until each control returns to neutral.</summary>
    public static class NativeGamepadInput
    {
        const int RightTrigger = 10, DpadUp = 11, DpadDown = 12, DpadLeft = 13, DpadRight = 14;
        const int ButtonCount = 15;
        static readonly GamepadCommand[] Ordinary = {
            GamepadCommand.Interact, GamepadCommand.WaitUntilHealed, GamepadCommand.ActivateAbility,
            GamepadCommand.Walk, GamepadCommand.AttackNearest, GamepadCommand.Fire,
            GamepadCommand.Character, GamepadCommand.Pause, GamepadCommand.PointOfInterest,
            GamepadCommand.Reload, GamepadCommand.Step, GamepadCommand.Ascend,
            GamepadCommand.Descend, GamepadCommand.PreviousAbility, GamepadCommand.NextAbility
        };
        static readonly GamepadCommand[] Modified = {
            GamepadCommand.InteractDirection, GamepadCommand.WaitMenu, GamepadCommand.Abilities,
            GamepadCommand.AutoExplore, GamepadCommand.ForceAttack, GamepadCommand.Throw,
            GamepadCommand.Help, GamepadCommand.None, GamepadCommand.None,
            GamepadCommand.ReplaceCell, GamepadCommand.Highlight, GamepadCommand.PreviousAbilityPage,
            GamepadCommand.NextAbilityPage, GamepadCommand.ZoomIn, GamepadCommand.ZoomOut
        };
        static readonly KeyCode[] MenuKeys = {
            KeyCode.Return, KeyCode.Escape, KeyCode.I, KeyCode.Tab, KeyCode.LeftArrow,
            KeyCode.RightArrow, KeyCode.Tab, KeyCode.Escape, KeyCode.None, KeyCode.None,
            KeyCode.None, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };
        static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> previous = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> down = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> up = new HashSet<KeyCode>();
        static readonly KeyCode[] latchedKeys = new KeyCode[ButtonCount];
        static readonly GamepadCommand[] latchedCommands = new GamepadCommand[ButtonCount];
        static readonly bool[] buttonsHeld = new bool[ButtonCount];
        static readonly bool[] quarantinedButtons = new bool[ButtonCount];
        static readonly bool[] commandHeld = new bool[(int)GamepadCommand.Look + 1];
        static readonly bool[] previousCommands = new bool[commandHeld.Length];
        static readonly bool[] commandPressed = new bool[commandHeld.Length];
        static Gamepad device;
        static ButtonControl[] buttons;
        static uint sampledUpdate;
        static bool sampled, modifierWasHeld, modifierQuarantined, anyPressed, targetPressed;
        static bool leftQuarantined, rightQuarantined, dpadQuarantined;
        static Vector2Int leftPhysical, rightPhysical, dpadPhysical;
        static Vector2Int selected, looking, target;
        static GamepadInputContext context = GamepadInputContext.Menu;

        public static bool IsConnected => Gamepad.current != null && Gamepad.current.added;
        public static bool AnyPressed { get { Sample(); return anyPressed; } }
        public static bool IsHighlightHeld
        {
            get { Sample(); return context == GamepadInputContext.World && commandHeld[(int)GamepadCommand.Highlight]
                && device != null && device.leftTrigger.isPressed && !modifierQuarantined; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            context = GamepadInputContext.Menu;
            device = null; buttons = null; sampled = false;
            ResetPhysical();
        }
        static void ResetPhysical()
        {
            ClearOutput();
            Array.Clear(buttonsHeld, 0, ButtonCount);
            Array.Clear(quarantinedButtons, 0, ButtonCount);
            Array.Clear(latchedKeys, 0, ButtonCount);
            Array.Clear(latchedCommands, 0, ButtonCount);
            leftPhysical = rightPhysical = dpadPhysical = Vector2Int.zero;
            modifierWasHeld = modifierQuarantined = leftQuarantined = rightQuarantined = dpadQuarantined = false;
        }
        static void ClearOutput()
        {
            held.Clear(); previous.Clear(); down.Clear(); up.Clear();
            Array.Clear(commandHeld, 0, commandHeld.Length);
            Array.Clear(previousCommands, 0, previousCommands.Length);
            Array.Clear(commandPressed, 0, commandPressed.Length);
            selected = looking = target = Vector2Int.zero;
            anyPressed = targetPressed = false;
        }

        public static void SetContext(GamepadInputContext value)
        {
            if (context == value) return;
            Sample();
            context = value;
            QuarantineCurrentControls();
            ClearOutput();
        }
        /// <summary>Require neutral controls after a save load, actor replacement or zone transition.</summary>
        public static void QuarantineHeldControls()
        {
            Sample(); QuarantineCurrentControls(); ClearOutput();
        }

        static void QuarantineCurrentControls()
        {
            if (device == null) return;
            for (int i = 0; i < ButtonCount; i++)
            {
                quarantinedButtons[i] = buttons[i].isPressed;
                buttonsHeld[i] = buttons[i].isPressed;
                latchedKeys[i] = KeyCode.None;
                latchedCommands[i] = GamepadCommand.None;
            }
            leftQuarantined = !Neutral(device.leftStick.ReadValue());
            rightQuarantined = !Neutral(device.rightStick.ReadValue());
            dpadQuarantined = !Neutral(device.dpad.ReadValue());
            modifierQuarantined = device.leftTrigger.isPressed;
            modifierWasHeld = device.leftTrigger.isPressed;
        }

        public static bool GetKey(KeyCode key) { Sample(); return held.Contains(key); }
        public static bool GetKeyDown(KeyCode key) { Sample(); return down.Contains(key); }
        public static bool GetKeyUp(KeyCode key) { Sample(); return up.Contains(key); }
        public static bool IsPressed(GamepadCommand command)
        { Sample(); return context == GamepadInputContext.World && Valid(command) && commandPressed[(int)command]; }
        public static bool IsHeld(GamepadCommand command)
        { Sample(); return context == GamepadInputContext.World && Valid(command) && commandHeld[(int)command]; }
        public static bool TrySelectedDirection(out int dx, out int dy)
        {
            Sample();
            return Direction(context == GamepadInputContext.Menu ? Vector2Int.zero : selected, out dx, out dy);
        }
        public static bool TryLookDirection(out int dx, out int dy)
        { Sample(); return Direction(context == GamepadInputContext.World ? looking : Vector2Int.zero, out dx, out dy); }
        public static bool TryMoveDirection(out int dx, out int dy)
        {
            Sample();
            bool allowed = context == GamepadInputContext.World && commandHeld[(int)GamepadCommand.Step]
                && device != null && !device.leftTrigger.isPressed;
            return Direction(allowed ? selected : Vector2Int.zero, out dx, out dy);
        }
        public static bool TryDirection(bool pressedOnly, out int dx, out int dy)
        {
            Sample();
            bool allowed = context == GamepadInputContext.Targeting && (!pressedOnly || targetPressed);
            return Direction(allowed ? target : Vector2Int.zero, out dx, out dy);
        }
        static bool Direction(Vector2Int value, out int dx, out int dy)
        { dx = value.x; dy = -value.y; return value != Vector2Int.zero; }
        static bool Valid(GamepadCommand command) => command > GamepadCommand.None && (int)command < commandHeld.Length;

        static void Sample()
        {
            var current = Gamepad.current;
            if (current != null && !current.added) current = null;
            if (!ReferenceEquals(device, current))
            {
                ResetPhysical(); device = current; sampled = false;
                buttons = current == null ? null : new[] {
                    current.buttonSouth, current.buttonEast, current.buttonWest, current.buttonNorth,
                    current.leftShoulder, current.rightShoulder, current.startButton, current.selectButton,
                    current.leftStickButton, current.rightStickButton, current.rightTrigger,
                    current.dpad.up, current.dpad.down, current.dpad.left, current.dpad.right
                };
                QuarantineCurrentControls();
            }
            if (sampled && sampledUpdate == InputState.updateCount) return;
            sampled = true; sampledUpdate = InputState.updateCount;
            previous.Clear(); previous.UnionWith(held); held.Clear(); down.Clear(); up.Clear();
            Array.Copy(commandHeld, previousCommands, commandHeld.Length);
            Array.Clear(commandHeld, 0, commandHeld.Length);
            Array.Clear(commandPressed, 0, commandPressed.Length);
            var oldSelected = selected; var oldLooking = looking; var oldTarget = target;
            selected = looking = target = Vector2Int.zero;
            anyPressed = targetPressed = false;
            if (device != null)
            {
                bool modifier = device.leftTrigger.isPressed;
                if (!modifier) modifierQuarantined = false;
                if (modifier && !modifierWasHeld && !modifierQuarantined) anyPressed = true;
                // A held movement trigger must not resume moving after an LT
                // gesture; release RT to request another ordinary step.
                if (modifier && !modifierWasHeld && buttonsHeld[RightTrigger]
                    && latchedCommands[RightTrigger] == GamepadCommand.Step)
                    quarantinedButtons[RightTrigger] = true;

                Vector2 rawLeft = device.leftStick.ReadValue(), rawRight = device.rightStick.ReadValue(), rawDpad = device.dpad.ReadValue();
                if (Neutral(rawLeft)) leftQuarantined = false;
                if (Neutral(rawRight)) rightQuarantined = false;
                if (Neutral(rawDpad)) dpadQuarantined = false;
                leftPhysical = Quantize(rawLeft, leftPhysical);
                rightPhysical = Quantize(rawRight, rightPhysical);
                dpadPhysical = Quantize(rawDpad, dpadPhysical);
                var left = leftQuarantined ? Vector2Int.zero : leftPhysical;
                var right = rightQuarantined ? Vector2Int.zero : rightPhysical;
                var dpad = dpadQuarantined ? Vector2Int.zero : dpadPhysical;

                for (int i = 0; i < ButtonCount; i++)
                {
                    bool pressed = buttons[i].isPressed;
                    bool newlyPressed = pressed && !buttonsHeld[i];
                    buttonsHeld[i] = pressed;
                    if (!pressed)
                    {
                        quarantinedButtons[i] = false;
                        latchedKeys[i] = KeyCode.None; latchedCommands[i] = GamepadCommand.None;
                        continue;
                    }
                    if (quarantinedButtons[i] || (i >= DpadUp && dpadQuarantined)) continue;
                    // An LT held in the previous context has not been rearmed.
                    // Suppress fresh chords rather than treating them as unmodified actions.
                    if (modifier && modifierQuarantined) continue;
                    if (newlyPressed)
                    {
                        anyPressed = true;
                        if (context == GamepadInputContext.World)
                            latchedCommands[i] = modifier ? Modified[i] : Ordinary[i];
                        else latchedKeys[i] = KeyForPress(i, modifier);
                    }
                    if (context == GamepadInputContext.World) Add(latchedCommands[i]);
                    else Add(latchedKeys[i]);
                }

                selected = left; looking = right;
                if (context == GamepadInputContext.World)
                {
                    if (right != Vector2Int.zero) Add(GamepadCommand.Look);
                    if ((left != Vector2Int.zero && left != oldSelected)
                        || (right != Vector2Int.zero && right != oldLooking)) anyPressed = true;
                }
                else if (context == GamepadInputContext.Menu)
                {
                    // Dpad keys were latched above. LS navigation has priority
                    // only when no dpad direction is active.
                    if (dpad == Vector2Int.zero) AddArrows(left);
                    if (Mathf.Abs(rawRight.x) <= Mathf.Abs(rawRight.y) && right.y != 0)
                        Add(right.y > 0 ? KeyCode.PageUp : KeyCode.PageDown);
                    if ((left != Vector2Int.zero && left != oldSelected)
                        || (right != Vector2Int.zero && right != oldLooking)) anyPressed = true;
                }
                else
                {
                    target = dpad != Vector2Int.zero ? dpad : left != Vector2Int.zero ? left : right;
                    targetPressed = target != Vector2Int.zero && target != oldTarget;
                    if (targetPressed) anyPressed = true;
                }
                modifierWasHeld = modifier;
            }
            foreach (var key in held) if (!previous.Contains(key)) down.Add(key);
            foreach (var key in previous) if (!held.Contains(key)) up.Add(key);
            for (int i = 1; i < commandHeld.Length; i++) commandPressed[i] = commandHeld[i] && !previousCommands[i];
        }

        static KeyCode KeyForPress(int button, bool modifier)
        {
            if (context == GamepadInputContext.Menu)
            {
                if (modifier && button == RightTrigger) return KeyCode.F1;
                if (modifier && button < DpadUp) return KeyCode.None;
                return MenuKeys[button];
            }
            if (modifier && button == RightTrigger) return KeyCode.F1;
            if (modifier && button != 1) return KeyCode.None;
            if (button == 0 || button == RightTrigger) return KeyCode.Return;
            if (button == 1) return KeyCode.Escape;
            if (button == 2) return KeyCode.Period;
            return KeyCode.None;
        }
        static void Add(KeyCode key) { if (key != KeyCode.None) held.Add(key); }
        static void Add(GamepadCommand command) { if (Valid(command)) commandHeld[(int)command] = true; }
        static void AddArrows(Vector2Int value)
        {
            if (value.x != 0) Add(value.x < 0 ? KeyCode.LeftArrow : KeyCode.RightArrow);
            if (value.y != 0) Add(value.y > 0 ? KeyCode.UpArrow : KeyCode.DownArrow);
        }
        static bool Neutral(Vector2 value) => Mathf.Abs(value.x) < .35f && Mathf.Abs(value.y) < .35f;
        static Vector2Int Quantize(Vector2 value, Vector2Int before)
            => new Vector2Int(Axis(value.x, before.x), Axis(value.y, before.y));
        static int Axis(float value, int before)
        {
            if (value >= (before > 0 ? .35f : .55f)) return 1;
            if (value <= -(before < 0 ? .35f : .55f)) return -1;
            return 0;
        }
    }
}
