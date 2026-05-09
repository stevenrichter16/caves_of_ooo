using UnityEngine;

namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>
    /// Pass 4 §4B: toggles the CRT Volume on/off via a hotkey.
    /// Default key is F12 (configurable). State persists across
    /// sessions via PlayerPrefs — display preference, NOT a save-file
    /// concern.
    ///
    /// <para>Drop this MonoBehaviour anywhere; it locates the
    /// "CRT Volume" GameObject by name in the scene on Awake. If
    /// not found, the controller is a no-op (logs a warning once).</para>
    ///
    /// <para>See <c>Docs/GRAPHICS-PASS4.md</c> §4B.2 for design.</para>
    /// </summary>
    public class CrtToggleController : MonoBehaviour
    {
        public KeyCode ToggleKey = KeyCode.F12;
        public string CrtVolumeGameObjectName = "CRT Volume";

        private const string PREFS_KEY = "CavesOfOoo.CrtEnabled";

        private GameObject _crtVolume;
        private bool _warnedMissing;

        private void Awake()
        {
            _crtVolume = GameObject.Find(CrtVolumeGameObjectName);
            if (_crtVolume == null)
            {
                if (!_warnedMissing)
                {
                    Debug.LogWarning($"[CrtToggle] No GameObject named "
                        + $"'{CrtVolumeGameObjectName}' found in scene. "
                        + "Toggle hotkey is a no-op. (Pass 4 §4B.1 ships "
                        + "this GameObject in SampleScene; if you're in "
                        + "a different scene, the toggle won't have a "
                        + "Volume to flip.)");
                    _warnedMissing = true;
                }
                return;
            }

            // Apply persisted preference.
            bool enabled = PlayerPrefs.GetInt(PREFS_KEY, 0) == 1;
            _crtVolume.SetActive(enabled);
        }

        private void Update()
        {
            if (_crtVolume == null) return;
            if (Input.GetKeyDown(ToggleKey))
            {
                bool newState = !_crtVolume.activeSelf;
                _crtVolume.SetActive(newState);
                PlayerPrefs.SetInt(PREFS_KEY, newState ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log($"[CrtToggle] CRT overlay {(newState ? "ON" : "OFF")}");
            }
        }

        // ── Test seams ───────────────────────────────────────────────────

        /// <summary>
        /// Test seam: simulate the toggle hotkey press.
        /// </summary>
        public bool TestOnly_Toggle()
        {
            if (_crtVolume == null) return false;
            bool newState = !_crtVolume.activeSelf;
            _crtVolume.SetActive(newState);
            return newState;
        }

        /// <summary>
        /// Test seam: peek at whether the CRT volume is active.
        /// </summary>
        public bool TestOnly_IsCrtActive => _crtVolume != null && _crtVolume.activeSelf;

        /// <summary>
        /// Test seam: inject a synthetic CRT volume gameObject for
        /// EditMode tests that don't have access to the real
        /// SampleScene.
        /// </summary>
        public void TestOnly_SetCrtVolume(GameObject volume)
        {
            _crtVolume = volume;
        }
    }
}
