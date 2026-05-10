using UnityEngine;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>
    /// Pass 7 §7B.2 — toggle between hybrid sprite environment and
    /// pure CP437 via hotkey. Default key: backslash `\`. State
    /// persists via PlayerPrefs.
    ///
    /// <para>Mirrors the architectural pattern of
    /// <see cref="CrtToggleController"/> (Pass 4 §4B.2) — same hotkey
    /// + persistence shape, just different target.</para>
    ///
    /// <para>Plan: <c>Docs/GRAPHICS-PASS7.md</c> §7B.2.</para>
    /// </summary>
    public class SpriteEnvToggleController : MonoBehaviour
    {
        public KeyCode ToggleKey = KeyCode.Backslash;
        private const string PREFS_KEY = "CavesOfOoo.SpriteEnvironmentEnabled";

        private EnvironmentSpriteRenderer _renderer;

        private void Awake()
        {
            // Find the renderer (it's added by ZoneRenderer at scene start).
            // FindObjectsByType includes inactive — robust against init order.
            var all = UnityEngine.Object.FindObjectsByType<EnvironmentSpriteRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length > 0) _renderer = all[0];

            // Default ON unless the user previously toggled OFF.
            bool enabled = PlayerPrefs.GetInt(PREFS_KEY, 1) == 1;
            if (_renderer != null) _renderer.RenderingEnabled = enabled;
        }

        private void Update()
        {
            if (_renderer == null) return;
            if (CavesOfOoo.Rendering.InputHelper.GetKeyDown(ToggleKey))
            {
                _renderer.RenderingEnabled = !_renderer.RenderingEnabled;
                PlayerPrefs.SetInt(PREFS_KEY, _renderer.RenderingEnabled ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log($"[SpriteEnvToggle] Sprite environment "
                    + $"{(_renderer.RenderingEnabled ? "ON" : "OFF")}");
            }
        }

        // ── Test seams ───────────────────────────────────────────────────

        public bool TestOnly_Toggle()
        {
            if (_renderer == null) return false;
            _renderer.RenderingEnabled = !_renderer.RenderingEnabled;
            return _renderer.RenderingEnabled;
        }

        public bool TestOnly_IsEnabled => _renderer != null && _renderer.RenderingEnabled;

        public void TestOnly_SetRenderer(EnvironmentSpriteRenderer renderer)
        {
            _renderer = renderer;
        }
    }
}
