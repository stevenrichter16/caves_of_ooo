using UnityEngine;
using UnityEngine.Rendering;

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
        // Default = Backquote (`/~ key, top-left of keyboard).
        // Originally F12, but F12 on macOS is the Volume Up function
        // key AND Unity Editor uses it for "Maximize Game View" — so
        // it never reached the Update handler. Backquote has zero
        // OS or editor conflicts and is easy to find.
        public KeyCode ToggleKey = KeyCode.BackQuote;
        public string CrtVolumeGameObjectName = "CRT Volume";

        private const string PREFS_KEY = "CavesOfOoo.CrtEnabled";

        private GameObject _crtVolume;
        private bool _warnedMissing;

        private void Awake()
        {
            // CRITICAL: GameObject.Find DOES NOT return inactive
            // GameObjects. The CRT Volume defaults to disabled (so
            // post-processing doesn't fire until toggled on), so
            // GameObject.Find always returns null on Bootstrap →
            // _crtVolume stays null → hotkey is a permanent no-op.
            // Use FindObjectsByType with FindObjectsInactive.Include
            // to walk all Volumes regardless of active state.
            var allVolumes = UnityEngine.Object.FindObjectsByType<Volume>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var v in allVolumes)
            {
                if (v.gameObject.name == CrtVolumeGameObjectName)
                {
                    _crtVolume = v.gameObject;
                    break;
                }
            }
            if (_crtVolume == null)
            {
                if (!_warnedMissing)
                {
                    Debug.LogWarning($"[CrtToggle] No GameObject named "
                        + $"'{CrtVolumeGameObjectName}' found in scene "
                        + "(checked active + inactive). Toggle hotkey is "
                        + "a no-op.");
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
