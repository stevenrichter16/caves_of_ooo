using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Rendering
{
    /// <summary>Built-in standard gamepad bindings, shared by native Deck input and
    /// Steam Input's ordinary gamepad output. No synthetic keyboard events or input
    /// updates: existing controllers retain their own action costs and repeat timing.</summary>
    public static class NativeGamepadInput
    {
        static readonly KeyCode[] Ordinary = {
            KeyCode.Return, KeyCode.Escape, KeyCode.I, KeyCode.C, KeyCode.L, KeyCode.G,
            KeyCode.Tab, KeyCode.F1, KeyCode.X, KeyCode.M, KeyCode.Period
        };
        static readonly KeyCode[] Modified = {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.None
        };
        static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> previous = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> down = new HashSet<KeyCode>();
        static readonly HashSet<KeyCode> up = new HashSet<KeyCode>();
        static readonly KeyCode[] latched = new KeyCode[11];
        static readonly bool[] buttonsHeld = new bool[11];
        static Gamepad device;
        static ButtonControl[] buttons;
        static uint sampledUpdate;
        static bool sampled, modifierWasHeld, directionModified, directionSuppressed;
        static Vector2Int physicalDirection, movement, rightDirection;
        static bool movementPressed;
        public static bool IsConnected => Gamepad.current != null && Gamepad.current.added;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            device = null; buttons = null; sampled = false;
            held.Clear(); previous.Clear(); down.Clear(); up.Clear();
            System.Array.Clear(latched, 0, latched.Length);
            System.Array.Clear(buttonsHeld, 0, buttonsHeld.Length);
            physicalDirection = movement = rightDirection = Vector2Int.zero;
            modifierWasHeld = directionModified = directionSuppressed = movementPressed = false;
        }

        public static bool GetKey(KeyCode key) { Sample(); return held.Contains(key); }
        public static bool GetKeyDown(KeyCode key) { Sample(); return down.Contains(key); }
        public static bool GetKeyUp(KeyCode key) { Sample(); return up.Contains(key); }
        public static bool TryDirection(bool pressedOnly, out int dx, out int dy)
        {
            Sample(); dx = dy = 0;
            if (movement == Vector2Int.zero || (pressedOnly && !movementPressed)) return false;
            dx = movement.x; dy = -movement.y; return true;
        }

        static void Sample()
        {
            var current = Gamepad.current;
            if (current != null && !current.added) current = null;
            if (!ReferenceEquals(device, current))
            {
                Reset(); device = current;
                if (current != null) buttons = new[] {
                    current.buttonSouth, current.buttonEast, current.buttonWest, current.buttonNorth,
                    current.leftShoulder, current.rightShoulder, current.startButton, current.selectButton,
                    current.leftStickButton, current.rightStickButton, current.rightTrigger
                };
            }
            if (sampled && sampledUpdate == InputState.updateCount) return;
            sampled = true; sampledUpdate = InputState.updateCount;
            previous.Clear(); previous.UnionWith(held); held.Clear(); down.Clear(); up.Clear();
            var previousMovement = movement; movement = Vector2Int.zero; movementPressed = false;
            if (device != null)
            {
                bool modifier = device.leftTrigger.isPressed;
                for (int i = 0; i < buttons.Length; i++)
                {
                    bool pressed = buttons[i].isPressed;
                    // A press retains its meaning until release, even across LT changes.
                    if (pressed && (!buttonsHeld[i] || buttons[i].wasPressedThisFrame))
                        latched[i] = modifier ? Modified[i] : Ordinary[i];
                    buttonsHeld[i] = pressed;
                    if (pressed) Add(latched[i]); else latched[i] = KeyCode.None;
                }
                var raw = device.dpad.ReadValue();
                if (raw.sqrMagnitude < .25f) raw = device.leftStick.ReadValue();
                var direction = Quantize(raw, physicalDirection);
                if (direction == Vector2Int.zero)
                    directionModified = directionSuppressed = false;
                else if (physicalDirection == Vector2Int.zero) directionModified = modifier;
                else if ((modifier && !modifierWasHeld) || (directionModified && !modifier))
                    directionSuppressed = true;
                if (!directionSuppressed && direction != Vector2Int.zero)
                {
                    if (directionModified && modifier)
                    {
                        if (direction.y != 0)
                        { Add(KeyCode.LeftShift); Add(direction.y > 0 ? KeyCode.Comma : KeyCode.Period); }
                        else Add(direction.x < 0 ? KeyCode.Q : KeyCode.F);
                    }
                    else if (!modifier)
                    {
                        movement = direction;
                        if (direction.x != 0) Add(direction.x < 0 ? KeyCode.LeftArrow : KeyCode.RightArrow);
                        if (direction.y != 0) Add(direction.y > 0 ? KeyCode.UpArrow : KeyCode.DownArrow);
                    }
                }
                physicalDirection = direction; modifierWasHeld = modifier;
                rightDirection = Quantize(device.rightStick.ReadValue(), rightDirection);
                // The dominant axis selects one operation; diagonal stick noise cannot page and cast.
                if (Mathf.Abs(device.rightStick.x.ReadValue()) > Mathf.Abs(device.rightStick.y.ReadValue()))
                { if (rightDirection.x != 0) Add(rightDirection.x < 0 ? KeyCode.LeftBracket : KeyCode.RightBracket); }
                else if (rightDirection.y != 0) Add(rightDirection.y > 0 ? KeyCode.PageUp : KeyCode.PageDown);
                movementPressed = movement != Vector2Int.zero && movement != previousMovement;
            }
            foreach (var key in held) if (!previous.Contains(key)) down.Add(key);
            foreach (var key in previous) if (!held.Contains(key)) up.Add(key);
        }
        static void Add(KeyCode key) { if (key != KeyCode.None) held.Add(key); }
        static Vector2Int Quantize(Vector2 value, Vector2Int before) =>
            new Vector2Int(Axis(value.x, before.x), Axis(value.y, before.y));
        static int Axis(float value, int before)
        {
            // Hysteresis prevents stick drift and chatter around the activation threshold.
            if (value >= (before > 0 ? .35f : .55f)) return 1;
            if (value <= -(before < 0 ? .35f : .55f)) return -1;
            return 0;
        }
    }
}
