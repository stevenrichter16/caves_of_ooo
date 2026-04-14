using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Thin wrapper over Unity input that checks BOTH legacy Input and New Input System.
    /// Tracks New Input System key state transitions manually to reliably detect
    /// "pressed this frame" even when QueueStateEvent timing doesn't align with
    /// the game's Update loop.
    /// </summary>
    public static class InputHelper
    {
        private static readonly Dictionary<KeyCode, Key> KeyCodeToKey = new Dictionary<KeyCode, Key>
        {
            // Letters
            { KeyCode.A, Key.A }, { KeyCode.B, Key.B }, { KeyCode.C, Key.C }, { KeyCode.D, Key.D },
            { KeyCode.E, Key.E }, { KeyCode.F, Key.F }, { KeyCode.G, Key.G }, { KeyCode.H, Key.H },
            { KeyCode.I, Key.I }, { KeyCode.J, Key.J }, { KeyCode.K, Key.K }, { KeyCode.L, Key.L },
            { KeyCode.M, Key.M }, { KeyCode.N, Key.N }, { KeyCode.O, Key.O }, { KeyCode.P, Key.P },
            { KeyCode.Q, Key.Q }, { KeyCode.R, Key.R }, { KeyCode.S, Key.S }, { KeyCode.T, Key.T },
            { KeyCode.U, Key.U }, { KeyCode.V, Key.V }, { KeyCode.W, Key.W }, { KeyCode.X, Key.X },
            { KeyCode.Y, Key.Y }, { KeyCode.Z, Key.Z },
            // Digits
            { KeyCode.Alpha0, Key.Digit0 }, { KeyCode.Alpha1, Key.Digit1 },
            { KeyCode.Alpha2, Key.Digit2 }, { KeyCode.Alpha3, Key.Digit3 },
            { KeyCode.Alpha4, Key.Digit4 }, { KeyCode.Alpha5, Key.Digit5 },
            { KeyCode.Alpha6, Key.Digit6 }, { KeyCode.Alpha7, Key.Digit7 },
            { KeyCode.Alpha8, Key.Digit8 }, { KeyCode.Alpha9, Key.Digit9 },
            // Numpad
            { KeyCode.Keypad0, Key.Numpad0 }, { KeyCode.Keypad1, Key.Numpad1 },
            { KeyCode.Keypad2, Key.Numpad2 }, { KeyCode.Keypad3, Key.Numpad3 },
            { KeyCode.Keypad4, Key.Numpad4 }, { KeyCode.Keypad5, Key.Numpad5 },
            { KeyCode.Keypad6, Key.Numpad6 }, { KeyCode.Keypad7, Key.Numpad7 },
            { KeyCode.Keypad8, Key.Numpad8 }, { KeyCode.Keypad9, Key.Numpad9 },
            // Arrows
            { KeyCode.UpArrow, Key.UpArrow }, { KeyCode.DownArrow, Key.DownArrow },
            { KeyCode.LeftArrow, Key.LeftArrow }, { KeyCode.RightArrow, Key.RightArrow },
            // Function keys
            { KeyCode.F1, Key.F1 }, { KeyCode.F2, Key.F2 }, { KeyCode.F3, Key.F3 },
            { KeyCode.F4, Key.F4 }, { KeyCode.F5, Key.F5 }, { KeyCode.F6, Key.F6 },
            { KeyCode.F7, Key.F7 }, { KeyCode.F8, Key.F8 }, { KeyCode.F9, Key.F9 },
            { KeyCode.F10, Key.F10 }, { KeyCode.F11, Key.F11 }, { KeyCode.F12, Key.F12 },
            // Special
            { KeyCode.Space, Key.Space }, { KeyCode.Return, Key.Enter },
            { KeyCode.Escape, Key.Escape }, { KeyCode.Tab, Key.Tab },
            { KeyCode.Backspace, Key.Backspace }, { KeyCode.Delete, Key.Delete },
            { KeyCode.LeftShift, Key.LeftShift }, { KeyCode.RightShift, Key.RightShift },
            { KeyCode.LeftControl, Key.LeftCtrl }, { KeyCode.RightControl, Key.RightCtrl },
            { KeyCode.LeftAlt, Key.LeftAlt }, { KeyCode.RightAlt, Key.RightAlt },
            // Punctuation
            { KeyCode.Period, Key.Period }, { KeyCode.Comma, Key.Comma },
            { KeyCode.Semicolon, Key.Semicolon }, { KeyCode.Slash, Key.Slash },
            { KeyCode.Minus, Key.Minus }, { KeyCode.Equals, Key.Equals },
            { KeyCode.LeftBracket, Key.LeftBracket }, { KeyCode.RightBracket, Key.RightBracket },
        };

        // Manual state tracking: previous frame's pressed state per key
        private static readonly Dictionary<Key, bool> _prevPressed = new Dictionary<Key, bool>();
        private static int _lastTrackedFrame = -1;

        /// <summary>
        /// Update previous-frame state. Must be called once per frame before any GetKeyDown checks.
        /// Called automatically on first GetKeyDown/GetKey/GetKeyUp call each frame.
        /// </summary>
        private static void UpdateTracking()
        {
            int frame = Time.frameCount;
            if (frame == _lastTrackedFrame)
                return;

            // Save current pressed state as "previous" for next frame's transition detection
            if (Keyboard.current != null)
            {
                foreach (var kvp in KeyCodeToKey)
                {
                    var key = kvp.Value;
                    _prevPressed[key] = Keyboard.current[key].isPressed;
                }
            }
            _lastTrackedFrame = frame;
        }

        /// <summary>
        /// Returns true if the key was pressed this frame — checks both legacy Input AND New Input System.
        /// Uses manual state tracking to detect transitions from QueueStateEvent.
        /// </summary>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            if (Input.GetKeyDown(keyCode))
                return true;

            if (Keyboard.current != null && KeyCodeToKey.TryGetValue(keyCode, out var key))
            {
                // Check New Input System's own tracking first
                if (Keyboard.current[key].wasPressedThisFrame)
                    return true;

                // Also check manual transition: was not pressed last frame, is pressed now
                bool currentlyPressed = Keyboard.current[key].isPressed;
                bool wasPrevPressed = _prevPressed.TryGetValue(key, out var prev) && prev;
                if (currentlyPressed && !wasPrevPressed)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the key is currently held — checks both systems.
        /// </summary>
        public static bool GetKey(KeyCode keyCode)
        {
            UpdateTracking();

            if (Input.GetKey(keyCode))
                return true;

            if (Keyboard.current != null && KeyCodeToKey.TryGetValue(keyCode, out var key))
                return Keyboard.current[key].isPressed;

            return false;
        }

        /// <summary>
        /// Returns true if the key was released this frame — checks both systems.
        /// </summary>
        public static bool GetKeyUp(KeyCode keyCode)
        {
            if (Input.GetKeyUp(keyCode))
                return true;

            if (Keyboard.current != null && KeyCodeToKey.TryGetValue(keyCode, out var key))
            {
                if (Keyboard.current[key].wasReleasedThisFrame)
                    return true;

                // Manual transition: was pressed last frame, not pressed now
                bool currentlyPressed = Keyboard.current[key].isPressed;
                bool wasPrevPressed = _prevPressed.TryGetValue(key, out var prev) && prev;
                if (!currentlyPressed && wasPrevPressed)
                    return true;
            }

            return false;
        }
    }
}
