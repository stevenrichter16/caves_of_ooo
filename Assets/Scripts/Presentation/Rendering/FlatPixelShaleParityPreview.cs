using CavesOfOoo.Presentation.Effects;
using CavesOfOoo.Presentation.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Rendering
{
    /// <summary>Explicitly attached isolated-study controller. Owns a reversible
    /// snapshot of runtime presentation fields, never a shared profile or save.</summary>
    public sealed class FlatPixelShaleParityPreview : MonoBehaviour
    {
        ZoneRenderer rendererRef;
        BiomeColorPatcher patcher;
        LightSourceSpriteHook lights;
        ColorAdjustments adjustments;
        bool applied, oldPatcherEnabled, oldTorchEnabled;
        Color oldFilter, oldUnknown;
        float oldContrast, oldSaturation, oldAmbient;

        public void Configure(ZoneRenderer renderer, BiomeColorPatcher biomePatcher,
            LightSourceSpriteHook lightHook, ColorAdjustments runtimeAdjustments)
        {
            Restore();
            rendererRef = renderer; patcher = biomePatcher;
            lights = lightHook; adjustments = runtimeAdjustments;
        }

        void LateUpdate() => Tick();

        public void Tick()
        {
            bool active = rendererRef != null && !rendererRef.Paused
                && patcher != null && lights != null && adjustments != null
                && FlatPixelShaleParityStudy.IsActive(rendererRef.CurrentZone, rendererRef.FlatPixelModeActive);
            if (!active) { Restore(); return; }
            if (applied) return;

            oldPatcherEnabled = patcher.enabled;
            oldFilter = adjustments.colorFilter.value;
            oldContrast = adjustments.contrast.value;
            oldSaturation = adjustments.saturation.value;
            oldAmbient = lights.DungeonAmbientIntensity;
            oldTorchEnabled = lights.PlayerTorchEnabled;
            oldUnknown = rendererRef.UnexploredColor;
            applied = true;
            patcher.enabled = false;
            adjustments.colorFilter.value = Color.white;
            adjustments.contrast.value = 0;
            adjustments.saturation.value = 0;
            lights.DungeonAmbientIntensity = 1;
            lights.PlayerTorchEnabled = false;
            rendererRef.UnexploredColor = new Color(.114f, .085f, .070f, .75f);
            Repaint();
        }

        public void Restore()
        {
            if (!applied) return;
            applied = false;
            if (patcher != null) patcher.enabled = oldPatcherEnabled;
            if (adjustments != null)
            {
                adjustments.colorFilter.value = oldFilter;
                adjustments.contrast.value = oldContrast;
                adjustments.saturation.value = oldSaturation;
            }
            if (lights != null)
            {
                lights.DungeonAmbientIntensity = oldAmbient;
                lights.PlayerTorchEnabled = oldTorchEnabled;
            }
            if (rendererRef != null) rendererRef.UnexploredColor = oldUnknown;
            Repaint();
        }

        void Repaint()
        {
            // Never initialize a renderer for this study. Normal gameplay owns
            // its tilemaps and refreshes fog, actors and fixture lights as usual.
            if (Application.isPlaying && rendererRef != null && rendererRef.isActiveAndEnabled && !rendererRef.Paused)
                rendererRef.RenderZone();
        }

        void OnDisable()
        {
            FlatPixelShaleParityStudy.Enabled = false;
            Restore();
        }
    }
}
