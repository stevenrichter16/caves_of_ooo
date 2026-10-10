using System;
using System.Collections;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _graphicsOnly, _graphicsComplete;
        private Action _graphicsCleanup;
        public void ConfigureGraphicsAudit() => _graphicsOnly = true;

        private IEnumerator RunGraphicsAudit()
        {
            // This audit shares the launcher's disposable save and input scope.
            // Graphics menu actions persist preferences, so restore those too.
            string[] keys = { Village3DSettings.PreferenceKey, Village3DSettings.LowDetailPreferenceKey,
                Village3DSettings.WorldResolutionPreferenceKey, Village3DSettings.ShadowsPreferenceKey,
                SpellFxSettings.ModePreferenceKey, "CavesOfOoo.Fx.Speed", "CavesOfOoo.Fx.Flash",
                "CavesOfOoo.Fx.SoundVolume", "CavesOfOoo.Fx.Shake" };
            bool[] floats = { false, false, true, false, false, true, true, true, true };
            bool[] had = new bool[keys.Length];
            var floatValues = new float[keys.Length]; var intValues = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                had[i] = PlayerPrefs.HasKey(keys[i]);
                if (floats[i]) floatValues[i] = PlayerPrefs.GetFloat(keys[i]);
                else intValues[i] = PlayerPrefs.GetInt(keys[i]);
            }
            var previousPad = Gamepad.current;
            var pad = InputSystem.AddDevice<Gamepad>();
            _graphicsCleanup = () =>
            {
                if (pad.added) { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.RemoveDevice(pad); }
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                for (int i = 0; i < keys.Length; i++)
                    if (!had[i]) PlayerPrefs.DeleteKey(keys[i]);
                    else if (floats[i]) PlayerPrefs.SetFloat(keys[i], floatValues[i]);
                    else PlayerPrefs.SetInt(keys[i], intValues[i]);
                PlayerPrefs.Save(); Village3DSettings.Load(); SpellFxSettings.Load();
                bool restored = true;
                for (int i = 0; i < keys.Length; i++)
                    restored &= PlayerPrefs.HasKey(keys[i]) == had[i]
                        && (!had[i] || (floats[i] ? PlayerPrefs.GetFloat(keys[i]).Equals(floatValues[i])
                            : PlayerPrefs.GetInt(keys[i]) == intValues[i]));
                Check("graphics_preferences_restored", restored);
                Check("graphics_gamepad_removed", !pad.added);
                Check("graphics_previous_gamepad_restored", previousPad == null || !previousPad.added
                    || ReferenceEquals(Gamepad.current, previousPad));
            };
            try
            {
                var menu = FindFirstObjectByType<PauseMenuUI>();
                Require(menu != null, "native pause UI");
                yield return PadTap(pad, GamepadButton.Start);
                Check("gamepad_opens_pause", menu.Controller.IsOpen);
                for (int i = 0; i < PauseMenuController.GraphicsIndex; i++) yield return PadTap(pad, GamepadButton.DpadDown);
                yield return PadTap(pad, GamepadButton.South);
                Check("gamepad_opens_graphics", menu.Controller.IsGraphicsOpen);
                yield return PadTap(pad, GamepadButton.South);
                Check("handheld_preset_applied", Village3DSettings.LowDetail
                    && Mathf.Approximately(.75f, Village3DSettings.WorldResolutionScale)
                    && !Village3DSettings.ShadowsEnabled && SpellFxSettings.Mode == SpellFxMode.Reduced);
                yield return Capture("02-handheld-graphics-menu");
                yield return PadTap(pad, GamepadButton.East);
                Check("back_returns_to_pause", menu.Controller.IsOpen && !menu.Controller.IsGraphicsOpen);
                yield return PadTap(pad, GamepadButton.East);
                Check("back_resumes_world", !menu.Controller.IsOpen);
                yield return Capture("03-handheld-world");
                var presenter = FindFirstObjectByType<SpawnRing3DPresenter>();
                Check("handheld_native_world_visible", presenter != null && presenter.PresentationVisible);
                yield return PadTap(pad, GamepadButton.Start);
                for (int i = 0; i < PauseMenuController.GraphicsIndex; i++) yield return PadTap(pad, GamepadButton.DpadDown);
                yield return PadTap(pad, GamepadButton.South);
                yield return PadTap(pad, GamepadButton.DpadDown);
                yield return PadTap(pad, GamepadButton.South);
                Check("full_preset_applied", !Village3DSettings.LowDetail
                    && Mathf.Approximately(1f, Village3DSettings.WorldResolutionScale)
                    && Village3DSettings.ShadowsEnabled && SpellFxSettings.Mode == SpellFxMode.Full);
                yield return Capture("04-full-graphics-menu");
                yield return PadTap(pad, GamepadButton.Start);
                Check("menu_button_closes_all_pages", !menu.Controller.IsOpen);
                yield return Capture("05-full-world");
            }
            finally
            {
                _graphicsCleanup?.Invoke(); _graphicsCleanup = null;
            }
            _graphicsComplete = true;
        }

        private IEnumerator PadTap(Gamepad pad, GamepadButton button)
        {
            Require(_clock.Elapsed.TotalSeconds < 240, "finite graphics audit deadline");
            pad.MakeCurrent();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
            yield return new WaitForSecondsRealtime(.15f);
        }
    }
}
