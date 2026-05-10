// Pass 4 Volume-profile fix utility.
// MCP's volume_add_effect didn't persist effect components to disk
// (left null fileID:0 references in the .asset YAML). This editor
// menu item populates both profiles programmatically via the proper
// VolumeProfile.Add<T>() API and saves the assets.
// One-shot fix; safe to delete after profiles are populated.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Pass4VolumeFix
{
    [MenuItem("Caves Of Ooo/Tools/Fix Volume Profiles (Pass 1 + 4B)")]
    public static void Apply()
    {
        FixGlobalProfile();
        FixCrtProfile();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Pass4VolumeFix] Done. Reload the scene to see effects "
            + "applied at runtime, or re-enter Play mode.");
    }

    private static void FixGlobalProfile()
    {
        const string PATH = "Assets/Settings/CavesOfOoo_VolumeProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PATH);
        if (profile == null) { Debug.LogError("Global profile not found"); return; }
        StripExistingEffects(profile, PATH);

        // Bloom — Pass 1 tunings
        // BLOOM TUNING NOTE — see Docs/GRAPHICS-PASS4.md fix for context.
        // Tilemap.SetColor clamps to Color32 (LDR), so HDR codes from
        // QudColorParser get clipped to [0, 1] before reaching the
        // framebuffer. The brightest pixel a normal tilemap glyph
        // can produce is roughly (1.0, 0.33, 0.33) (BrightRed). With
        // threshold=1.05 (the original HDR-only target), bloom would
        // never fire on tilemap-rendered glyphs at all. We lower
        // threshold to 0.80 so SDR-bright glyphs (BrightRed,
        // BrightYellow, etc.) bloom subtly. Scatter trimmed to 0.5
        // to keep halos tight at this lower threshold.
        var bloom = AddPersistedComponent<Bloom>(profile, PATH);
        bloom.threshold.overrideState = true; bloom.threshold.value = 0.80f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.55f;
        bloom.scatter.overrideState = true; bloom.scatter.value = 0.5f;
        bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.92f, 0.78f, 1f);

        var vig = AddPersistedComponent<Vignette>(profile, PATH);
        vig.intensity.overrideState = true; vig.intensity.value = 0.32f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.45f;
        vig.color.overrideState = true; vig.color.value = Color.black;
        vig.rounded.overrideState = true; vig.rounded.value = false;

        var ca = AddPersistedComponent<ColorAdjustments>(profile, PATH);
        ca.contrast.overrideState = true; ca.contrast.value = 8f;
        ca.saturation.overrideState = true; ca.saturation.value = 8f;
        ca.colorFilter.overrideState = true;
        ca.colorFilter.value = new Color(1f, 0.97f, 0.91f, 1f);

        var tm = AddPersistedComponent<Tonemapping>(profile, PATH);
        tm.mode.overrideState = true; tm.mode.value = TonemappingMode.Neutral;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        Debug.Log("[Pass4VolumeFix] Global profile populated with "
            + profile.components.Count + " effects.");
    }

    private static void FixCrtProfile()
    {
        const string PATH = "Assets/Settings/CavesOfOoo_CrtVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PATH);
        if (profile == null) { Debug.LogError("CRT profile not found"); return; }
        StripExistingEffects(profile, PATH);

        var vig = AddPersistedComponent<Vignette>(profile, PATH);
        vig.intensity.overrideState = true; vig.intensity.value = 0.55f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.7f;
        vig.color.overrideState = true; vig.color.value = Color.black;
        vig.rounded.overrideState = true; vig.rounded.value = true;

        var ld = AddPersistedComponent<LensDistortion>(profile, PATH);
        ld.intensity.overrideState = true; ld.intensity.value = 0.06f;
        ld.scale.overrideState = true; ld.scale.value = 1.02f;

        var fg = AddPersistedComponent<FilmGrain>(profile, PATH);
        fg.intensity.overrideState = true; fg.intensity.value = 0.3f;
        fg.response.overrideState = true; fg.response.value = 0.8f;

        var chrom = AddPersistedComponent<ChromaticAberration>(profile, PATH);
        chrom.intensity.overrideState = true; chrom.intensity.value = 0.1f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        Debug.Log("[Pass4VolumeFix] CRT profile populated with "
            + profile.components.Count + " effects.");
    }

    /// <summary>
    /// Create a VolumeComponent of the given type, add it as a
    /// SUB-ASSET of the profile asset (so it persists to YAML), AND
    /// register it on the profile's components list. This is the
    /// piece that the MCP volume_add_effect tool was missing —
    /// without AddObjectToAsset, the component is in memory only and
    /// the YAML keeps {fileID: 0} null references.
    /// </summary>
    private static T AddPersistedComponent<T>(VolumeProfile profile, string path)
        where T : VolumeComponent
    {
        var component = ScriptableObject.CreateInstance<T>();
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        AssetDatabase.AddObjectToAsset(component, profile);
        profile.components.Add(component);
        return component;
    }

    private static void StripExistingEffects(VolumeProfile profile, string path)
    {
        // Remove any pre-existing components AND their sub-asset
        // entries so re-running is idempotent.
        for (int i = profile.components.Count - 1; i >= 0; i--)
        {
            var c = profile.components[i];
            if (c != null) Object.DestroyImmediate(c, true);
        }
        profile.components.Clear();
        // Also strip orphaned sub-assets (in case earlier broken
        // components stayed as sub-assets without being in the list).
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in subAssets)
        {
            if (asset is VolumeComponent && asset != profile)
                Object.DestroyImmediate(asset, true);
        }
    }
}
