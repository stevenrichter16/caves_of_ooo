using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>Backslash edits the preserved previous-view sprite choice.
    /// Flat16x24 always retains its required sprite layer. The bootstrap binds
    /// the exact world owner; Awake never searches a scene or reloads preferences.</summary>
    public class SpriteEnvToggleController : MonoBehaviour
    {
        public KeyCode ToggleKey = KeyCode.Backslash;
        private ZoneRenderer _owner;

        private void Awake() { }

        /// <summary>Bind or detach the actual bootstrap renderer, including after
        /// Continue. Binding applies runtime choices without saving or loading them.</summary>
        public void Bind(ZoneRenderer owner)
        {
            _owner = owner;
            ApplyPresentation();
        }

        private void ApplyPresentation()
        {
            if (_owner != null) _owner.ApplyWorldPresentation(WorldPresentationSettings.Mode,
                WorldPresentationSettings.LegacySpritesEnabled);
        }

        private void Update()
        {
            if (_owner == null || !InputHelper.GetKeyDown(ToggleKey)) return;
            Toggle();
            MessageLog.Add("Previous sprites: " + (WorldPresentationSettings.LegacySpritesEnabled ? "on." : "off."));
        }

        private bool Toggle()
        {
            if (_owner == null) return false;
            WorldPresentationSettings.LegacySpritesEnabled = !WorldPresentationSettings.LegacySpritesEnabled;
            ApplyPresentation();
            WorldPresentationSettings.SaveLegacySprites();
            return WorldPresentationSettings.LegacySpritesEnabled;
        }

        // Existing focused tests retain a local-owner seam; it never searches globally.
        public bool TestOnly_Toggle() => Toggle();
        public bool TestOnly_IsEnabled => _owner != null
            && _owner.GetComponent<EnvironmentSpriteRenderer>()?.RenderingEnabled == true;
        public void TestOnly_SetRenderer(EnvironmentSpriteRenderer renderer) => Bind(renderer != null
            ? renderer.GetComponent<ZoneRenderer>() : null);
    }
}
