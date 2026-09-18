using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>F10 opens the presentation preferences without spending a turn.</summary>
    public sealed class SpellFxSettingsPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private Rect _window;
        private void Awake() { SpellFxSettings.Load(); }
        private void OnDisable() { IsOpen = false; }
        private void Update()
        {
            if (InputHelper.GetKeyDown(KeyCode.F10))
            {
                IsOpen = !IsOpen;
                if (!IsOpen) SpellFxSettings.Save();
            }
        }
        private void OnGUI()
        {
            if (!IsOpen) return;
            _window = new Rect((Screen.width - 390) / 2f, (Screen.height - 310) / 2f, 390, 310);
            GUI.Window(GetInstanceID(), _window, Draw, "Spell animation · F10");
        }
        private void Draw(int id)
        {
            GUILayout.Space(12);
            SpellFxSettings.Mode = (SpellFxMode)GUILayout.Toolbar((int)SpellFxSettings.Mode,
                new[] { "Off", "Reduced", "Full" });
            GUILayout.Space(10);
            GUILayout.Label("Animation speed  " + SpellFxSettings.AnimationSpeed.ToString("0.00") + "×");
            SpellFxSettings.AnimationSpeed = GUILayout.HorizontalSlider(SpellFxSettings.AnimationSpeed, .25f, 4f);
            GUILayout.Label("Flash  " + Mathf.RoundToInt(SpellFxSettings.FlashIntensity * 100) + "%");
            SpellFxSettings.FlashIntensity = GUILayout.HorizontalSlider(SpellFxSettings.FlashIntensity, 0f, 1f);
            GUILayout.Label("Shake  " + Mathf.RoundToInt(SpellFxSettings.ShakeIntensity * 100) + "%");
            SpellFxSettings.ShakeIntensity = GUILayout.HorizontalSlider(SpellFxSettings.ShakeIntensity, 0f, 1f);
            GUILayout.Space(12);
            GUILayout.Label("Terrain hazards and status indicators remain visible.");
            if (GUILayout.Button("Done")) { SpellFxSettings.Save(); IsOpen = false; }
        }
    }
}
