using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public static class Village3DSettings
    {
        public const string PreferenceKey = "CavesOfOoo.Village3D";
        public const string LowDetailPreferenceKey = "CavesOfOoo.Village3D.LowDetail";
        public const string WorldResolutionPreferenceKey = "CavesOfOoo.Village3D.WorldResolutionScale";
        public const string ShadowsPreferenceKey = "CavesOfOoo.Village3D.Shadows";
        public const string HandheldEnvironmentVariable = "COO_HANDHELD";
        private static bool enabled = true;
        private static bool lowDetail;
        private static float? resolutionOverride;
        private static bool? shadowsOverride;

        // The Deck distribution launcher supplies this explicit hint. It is not
        // OS/hardware detection, and saved preferences always override defaults.
        internal static bool UseHandheldDefaults =>
            Environment.GetEnvironmentVariable(HandheldEnvironmentVariable) == "1";

        /// <summary>Runtime display mode. Assignment redraws the map without changing
        /// gameplay or saved preferences; the player control explicitly persists choices.</summary>
        public static bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value) return;
                enabled = value;
                ZoneRenderHooks.MarkFullDirty("Village3D.Mode");
            }
        }

        /// <summary>Reduce decoration/contact cost. Without independent overrides,
        /// also preserves the legacy 75% world target and disabled-shadow preset.</summary>
        public static bool LowDetail
        {
            get => lowDetail;
            set
            {
                if (lowDetail == value) return;
                lowDetail = value;
                ZoneRenderHooks.MarkFullDirty("Village3D.Detail");
            }
        }

        /// <summary>World-only target scale, independent of HUD/popup resolution.
        /// Finite values clamp to 0.5..1; nonfinite input falls back to 1. Assignment
        /// is a runtime override; Save explicitly persists it. Absent overrides
        /// follow the existing LowDetail preset.</summary>
        public static float WorldResolutionScale
        {
            get => resolutionOverride ?? (LowDetail ? .75f : 1f);
            set
            {
                float before = WorldResolutionScale;
                resolutionOverride = SafeResolution(value);
                if (before != WorldResolutionScale)
                    ZoneRenderHooks.MarkFullDirty("Village3D.Resolution");
            }
        }

        /// <summary>World sunlight shadows. Runtime assignment is independent of
        /// detail/resolution; absent overrides follow the existing detail preset.</summary>
        public static bool ShadowsEnabled
        {
            get => shadowsOverride ?? !LowDetail;
            set
            {
                bool before = ShadowsEnabled;
                shadowsOverride = value;
                if (before != ShadowsEnabled)
                    ZoneRenderHooks.MarkFullDirty("Village3D.Shadows");
            }
        }

        /// <summary>Restore LowDetail-derived resolution/shadows without saving.
        /// Save after this call removes the independent preference keys.</summary>
        public static void ResetQualityOverrides()
        {
            float beforeResolution = WorldResolutionScale;
            bool beforeShadows = ShadowsEnabled;
            resolutionOverride = null;
            shadowsOverride = null;
            if (beforeResolution != WorldResolutionScale || beforeShadows != ShadowsEnabled)
                ZoneRenderHooks.MarkFullDirty("Village3D.QualityReset");
        }

        private static float SafeResolution(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp(value, .5f, 1f);

        public static void Load()
        {
            Enabled = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
            LowDetail = PlayerPrefs.GetInt(LowDetailPreferenceKey, UseHandheldDefaults ? 1 : 0) == 1;
            ResetQualityOverrides();
            if (PlayerPrefs.HasKey(WorldResolutionPreferenceKey))
                WorldResolutionScale = PlayerPrefs.GetFloat(WorldResolutionPreferenceKey, 1f);
            if (PlayerPrefs.HasKey(ShadowsPreferenceKey))
                ShadowsEnabled = PlayerPrefs.GetInt(ShadowsPreferenceKey, 1) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(PreferenceKey, Enabled ? 1 : 0);
            PlayerPrefs.SetInt(LowDetailPreferenceKey, LowDetail ? 1 : 0);
            if (resolutionOverride.HasValue)
                PlayerPrefs.SetFloat(WorldResolutionPreferenceKey, resolutionOverride.Value);
            else
                PlayerPrefs.DeleteKey(WorldResolutionPreferenceKey);
            if (shadowsOverride.HasValue)
                PlayerPrefs.SetInt(ShadowsPreferenceKey, shadowsOverride.Value ? 1 : 0);
            else
                PlayerPrefs.DeleteKey(ShadowsPreferenceKey);
            PlayerPrefs.Save();
        }
    }
}
