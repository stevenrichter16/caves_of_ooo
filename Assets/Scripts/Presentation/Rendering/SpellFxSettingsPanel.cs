using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>F10 opens the presentation preferences without spending a turn.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SpellFxSettingsPanel : MonoBehaviour
    {
        private const int Width = 614;
        private const int Height = 518;
        private const int ModeRow = 0;
        private const int FirstSliderRow = 1;
        private const int DoneRow = 5;
        private static readonly Rect DoneBounds = new Rect(432, 417, 156, 36);
        private static readonly string[] ModeNames = { "Off", "Reduced", "Full" };
        private static readonly Slider[] Sliders =
        {
            new Slider("Animation speed", .25f, 4f, .25f,
                () => SpellFxSettings.AnimationSpeed, value => SpellFxSettings.AnimationSpeed = value),
            new Slider("Flash", 0f, 1f, .05f,
                () => SpellFxSettings.FlashIntensity, value => SpellFxSettings.FlashIntensity = value),
            new Slider("Shake", 0f, 1f, .05f,
                () => SpellFxSettings.ShakeIntensity, value => SpellFxSettings.ShakeIntensity = value),
            new Slider("Sound volume", 0f, 1f, .05f,
                () => SpellFxSettings.SoundVolume, value => SpellFxSettings.SoundVolume = value)
        };

        private readonly struct Slider
        {
            internal readonly string Label;
            internal readonly float Min, Max, Step;
            internal readonly Func<float> Read;
            internal readonly Action<float> Write;

            internal Slider(string label, float min, float max, float step, Func<float> read, Action<float> write)
            {
                Label = label;
                Min = min;
                Max = max;
                Step = step;
                Read = read;
                Write = write;
            }
        }

        public static bool IsOpen { get; private set; }
        private LedgerModalView _view;
        private int _selectedRow;
        private int _draggingSlider = -1;
        private bool _closeRequested;
        private bool _lastGamepad;
        private bool _dirty;

        private void Awake() { SpellFxSettings.Load(); }

        private void OnDisable() => Hide();

        private void Hide()
        {
            _view?.Hide();
            _closeRequested = false;
            _draggingSlider = -1;
            IsOpen = false;
        }

        private void Update()
        {
            if (InputHelper.GetKeyboardKeyDown(KeyCode.F10))
            {
                if (IsOpen) _closeRequested = true;
                else Open();
                return;
            }
            if (!IsOpen) return;
            if (HandlePointer() || HandleKeys() || _lastGamepad != NativeGamepadInput.IsConnected)
                Render();
        }

        private void LateUpdate()
        {
            if (!_closeRequested) return;
            // Consume the closing press before gameplay or another modal can see it.
            if (_dirty) SpellFxSettings.Save();
            Hide();
        }

        private void Open()
        {
            _view ??= LedgerModalView.Create(transform, "Spell Effects Ledger", 1300);
            _selectedRow = ModeRow;
            _dirty = false;
            IsOpen = true;
            Render();
        }

        private bool HandleKeys()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                _closeRequested = true;
                return false;
            }
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.DownArrow))
            {
                int step = InputHelper.GetKeyDown(KeyCode.UpArrow) ? -1 : 1;
                _selectedRow = Mathf.Clamp(_selectedRow + step, ModeRow, DoneRow);
                return true;
            }
            bool confirm = InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Space);
            if (confirm && _selectedRow == DoneRow)
            {
                _closeRequested = true;
                return false;
            }
            int direction = InputHelper.GetKeyDown(KeyCode.LeftArrow) ? -1
                : InputHelper.GetKeyDown(KeyCode.RightArrow) ? 1 : 0;
            if (direction == 0 && !(confirm && _selectedRow == ModeRow)) return false;
            if (_selectedRow == ModeRow)
            {
                int mode = (int)SpellFxSettings.Mode + (confirm && direction == 0 ? 1 : direction);
                SetMode((mode + ModeNames.Length) % ModeNames.Length);
            }
            else if (_selectedRow < DoneRow)
            {
                Slider slider = Sliders[_selectedRow - FirstSliderRow];
                SetSlider(slider, slider.Read() + direction * slider.Step);
            }
            return true;
        }

        private bool HandlePointer()
        {
            Mouse mouse = Mouse.current is { enabled: true } active ? active : null;
            bool pressed = mouse != null ? mouse.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
            bool held = mouse != null ? mouse.leftButton.isPressed : Input.GetMouseButton(0);
            if (!held) _draggingSlider = -1;
            if (!pressed && _draggingSlider < 0) return false;

            Vector2 screen = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
            bool inCard = _view.TryPoint(screen, out Vector2 point);
            if (pressed && inCard)
            {
                for (int i = 0; i < ModeNames.Length; i++)
                {
                    if (!ModeBounds(i).Contains(point)) continue;
                    _selectedRow = ModeRow;
                    SetMode(i);
                    return true;
                }
                if (DoneBounds.Contains(point))
                {
                    _closeRequested = true;
                    return true;
                }
                for (int i = 0; i < Sliders.Length; i++)
                {
                    if (!SliderHitBounds(i).Contains(point)) continue;
                    _selectedRow = FirstSliderRow + i;
                    _draggingSlider = i;
                    break;
                }
            }
            if (_draggingSlider < 0) return false;
            var track = SliderTrack(_draggingSlider);
            Slider slider = Sliders[_draggingSlider];
            float value = Mathf.Lerp(slider.Min, slider.Max, Mathf.InverseLerp(track.xMin, track.xMax, point.x));
            if (!pressed && Mathf.Approximately(value, slider.Read())) return false;
            SetSlider(slider, value);
            return true;
        }

        private void SetMode(int mode)
        {
            if ((int)SpellFxSettings.Mode == mode) return;
            SpellFxSettings.Mode = (SpellFxMode)mode;
            _dirty = true;
        }

        private void SetSlider(Slider slider, float value)
        {
            float before = slider.Read();
            slider.Write(value);
            _dirty |= !Mathf.Approximately(before, slider.Read());
        }

        private void Render()
        {
            _lastGamepad = NativeGamepadInput.IsConnected;
            string footer = _lastGamepad
                ? "D-pad Browse / Adjust    ·    A Select    ·    B Close"
                : "↑ / ↓ Browse    ·    ← / → Adjust    ·    F10 / Esc Close";
            _view.Begin(null, Width, Height);
            _view.Card("Spell effects", footer);

            _view.Label(new Rect(26, 82, 148, 38), "Effect detail", _selectedRow == ModeRow ? LGold : LInk, 19, bold: true);
            for (int i = 0; i < ModeNames.Length; i++)
            {
                var bounds = ModeBounds(i);
                bool active = (int)SpellFxSettings.Mode == i;
                _view.Frame(bounds, active ? LGold : LBorder, active ? LSelected : LPanel);
                _view.Label(bounds, ModeNames[i], active ? LGold : LInk, 18, TextAnchor.MiddleCenter);
            }

            for (int i = 0; i < Sliders.Length; i++) RenderSlider(i);
            _view.Label(new Rect(26, 374, 562, 28), "Terrain hazards and status indicators remain visible.", LMuted, 16);
            bool doneSelected = _selectedRow == DoneRow;
            _view.Frame(DoneBounds, doneSelected ? LGold : LBorder, doneSelected ? LSelected : LPanel);
            _view.Label(DoneBounds, "Done", doneSelected ? LGold : LInk, 19, TextAnchor.MiddleCenter, bold: true);
            _view.End();
        }

        private void RenderSlider(int index)
        {
            Slider slider = Sliders[index];
            float value = slider.Read();
            Rect row = SliderRowBounds(index);
            Rect track = SliderTrack(index);
            bool selected = _selectedRow == FirstSliderRow + index;
            Color accent = selected ? LGold : LGreen;
            _view.Box(row, selected ? LSelected : LPanel);
            if (selected) _view.Box(new Rect(row.x, row.y, 3, row.height), LGold);
            _view.Label(new Rect(row.x + 14, row.y, 168, row.height), slider.Label, LInk, 18);
            _view.Box(track, LBorder);
            float filled = track.width * Mathf.InverseLerp(slider.Min, slider.Max, value);
            if (filled > 0) _view.Box(new Rect(track.x, track.y, filled, track.height), accent);
            _view.Box(new Rect(track.x + filled - 4, track.y - 5, 8, track.height + 10), accent);
            string display = index == 0 ? value.ToString("0.00") + "×" : Mathf.RoundToInt(value * 100) + "%";
            _view.Label(new Rect(506, row.y, 68, row.height), display, selected ? LGold : LMuted, 18, TextAnchor.MiddleRight);
        }

        private static Rect ModeBounds(int mode) => new Rect(184 + mode * 136, 82, 132, 38);
        private static Rect SliderRowBounds(int index) => new Rect(26, 139 + index * 55, 562, 48);
        private static Rect SliderTrack(int index) => new Rect(211, 160 + index * 55, 281, 6);
        private static Rect SliderHitBounds(int index) => new Rect(203, 139 + index * 55, 297, 48);
    }
}
