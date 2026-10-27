using System;
using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Effects;
using CavesOfOoo.Presentation.Rendering;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Editor
{
    /// <summary>Reversible art experiment on an ordinary isolated saved game.</summary>
    public static class ShaleParityStudyTools
    {
        [MenuItem("Caves Of Ooo/Art/Shale Reference Study/Enable In Isolated Cave")]
        public static void Enable()
        {
            var renderer = UnityEngine.Object.FindFirstObjectByType<ZoneRenderer>();
            if (!EditorApplication.isPlaying || string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride)
                || renderer == null || !renderer.FlatPixelModeActive
                || renderer.CurrentZone?.ZoneID != FlatPixelShaleParityStudy.TargetZoneID
                || !FlatPixelShaleCavesCatalog.IsSupportedZone(renderer.CurrentZone))
                throw new InvalidOperationException("Continue the isolated shale reference checkpoint first.");
            var patcher = UnityEngine.Object.FindFirstObjectByType<BiomeColorPatcher>();
            var lights = UnityEngine.Object.FindFirstObjectByType<LightSourceSpriteHook>();
            Volume volume = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (candidate.name == "Global Volume") { volume = candidate; break; }
            if (patcher == null || lights == null || volume == null || volume.sharedProfile == null
                || !volume.profile.TryGet<ColorAdjustments>(out var adjustments))
                throw new InvalidOperationException("The ordinary runtime presentation is not ready.");
            var atlas = Resources.Load<Texture2D>(FlatPixelShaleParityStudy.ResourcePath);
            if (atlas == null || atlas.width != 16 * FlatPixelShaleParityStudy.FrameCount || atlas.height != 24)
                throw new InvalidOperationException("The complete native study atlas is unavailable.");
            var preview = renderer.GetComponent<FlatPixelShaleParityPreview>();
            if (preview == null) preview = renderer.gameObject.AddComponent<FlatPixelShaleParityPreview>();
            preview.Configure(renderer, patcher, lights, adjustments);
            preview.enabled = true;
            FlatPixelShaleParityStudy.Enabled = true;
            preview.Tick();
        }

        [MenuItem("Caves Of Ooo/Art/Shale Reference Study/Restore Original Presentation")]
        public static void Disable()
        {
            FlatPixelShaleParityStudy.Enabled = false;
            foreach (var preview in UnityEngine.Object.FindObjectsByType<FlatPixelShaleParityPreview>(FindObjectsSortMode.None))
            { preview.Restore(); preview.enabled = false; }
        }
    }
}
