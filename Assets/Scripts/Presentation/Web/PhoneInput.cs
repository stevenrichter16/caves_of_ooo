using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public static class PhoneInput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        public const bool Enabled = true;
#else
        public const bool Enabled = false;
#endif
        internal static readonly PhoneIntentGate Gate = new PhoneIntentGate();
        internal static readonly PhoneDirectionSelection Direction = new PhoneDirectionSelection();
        internal static PhoneIntent FrameIntent;
        private static GamepadInputContext _mode;
        private static KeyCode _key = KeyCode.None;
        internal static bool AnyPressed => FrameIntent != null;
        public static void Receive(string json)
        {
            if (!Enabled || string.IsNullOrEmpty(json) || json.Length > 2048) return;
            try { Gate.Offer(JsonUtility.FromJson<PhoneIntent>(json)); }
            catch (ArgumentException) { /* An invalid message never enters gameplay. */ }
        }
        internal static void BeginFrame(int context, GamepadInputContext mode)
        {
            _key = KeyCode.None; FrameIntent = null;
            if (!Enabled) return;
            _mode = mode;
            Gate.SetContext(context); Direction.SetContext(context);
            FrameIntent = Gate.Take();
            if (FrameIntent == null) return;
            if (FrameIntent.kind == "key" && TryKey(FrameIntent.value, out var key)) _key = key;
            if (FrameIntent.kind == "step" && (mode == GamepadInputContext.Menu || mode == GamepadInputContext.Skills))
                _key = FrameIntent.y < 0 ? KeyCode.UpArrow : FrameIntent.y > 0 ? KeyCode.DownArrow
                    : FrameIntent.x < 0 ? KeyCode.LeftArrow : KeyCode.RightArrow;
        }
        internal static void EndFrame()
        {
            if (FrameIntent != null) Gate.Quarantine();
            FrameIntent = null; _key = KeyCode.None;
        }
        public static bool GetMenuKey(KeyCode key) => _mode != GamepadInputContext.World && GetKey(key);
        public static bool GetKey(KeyCode key) => Enabled && _key != KeyCode.None && _key == key;
        internal static bool TryDirection(bool cursor, out int x, out int y)
        {
            x = y = 0;
            return Enabled && Direction.Resolve(FrameIntent, cursor, out x, out y);
        }
        internal static bool TrySelectedDirection(out int x, out int y)
        { x = y = 0; return Enabled && Direction.TrySelected(out x, out y); }
        public static bool TryKey(string value, out KeyCode key)
        {
            key = KeyCode.None;
            if (string.IsNullOrEmpty(value)) return false;
            if (value.Length == 1)
            {
                char c = char.ToUpperInvariant(value[0]);
                if (c >= 'A' && c <= 'Z') { key = KeyCode.A + (c - 'A'); return true; }
                if (c >= '0' && c <= '9') { key = KeyCode.Alpha0 + (c - '0'); return true; }
            }
            switch (value)
            {
                case "Return": case "Enter": key = KeyCode.Return; return true;
                case "Escape": case "Esc": key = KeyCode.Escape; return true;
                case "Tab": key = KeyCode.Tab; return true;
                case "Space": key = KeyCode.Space; return true;
                case "UpArrow": key = KeyCode.UpArrow; return true;
                case "DownArrow": key = KeyCode.DownArrow; return true;
                case "LeftArrow": key = KeyCode.LeftArrow; return true;
                case "RightArrow": key = KeyCode.RightArrow; return true;
                case "PageUp": key = KeyCode.PageUp; return true;
                case "PageDown": key = KeyCode.PageDown; return true;
                case "Home": key = KeyCode.Home; return true;
                case "End": key = KeyCode.End; return true;
                case "F1": key = KeyCode.F1; return true;
                case "F2": key = KeyCode.F2; return true;
                case "F5": key = KeyCode.F5; return true;
                case "F6": key = KeyCode.F6; return true;
                case "+": case "=": key = KeyCode.Equals; return true;
                case "-": key = KeyCode.Minus; return true;
                case ",": key = KeyCode.Comma; return true;
                case ".": key = KeyCode.Period; return true;
            }
            return false;
        }
    }
}
