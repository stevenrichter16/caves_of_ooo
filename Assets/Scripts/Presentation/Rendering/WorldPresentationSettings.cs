using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Stable persisted values; previous graphics retains its own settings.</summary>
    public enum WorldPresentationMode { Flat16x24 = 0, Previous = 1 }

    /// <summary>Presentation preference authority. Runtime changes never persist
    /// implicitly, and selecting flat graphics never overwrites previous quality.</summary>
    public static class WorldPresentationSettings
    {
        public const string PreferenceKey = "CavesOfOoo.WorldPresentation";
        public const string LegacySpritesPreferenceKey = "CavesOfOoo.SpriteEnvironmentEnabled";
        private static WorldPresentationMode mode = WorldPresentationMode.Flat16x24;
        private static bool legacySpritesEnabled = true;

        public static WorldPresentationMode Mode
        {
            get => mode;
            set
            {
                var next = value == WorldPresentationMode.Previous
                    ? WorldPresentationMode.Previous : WorldPresentationMode.Flat16x24;
                if (mode == next) return;
                mode = next;
                ZoneRenderHooks.MarkFullDirty("WorldPresentation.Mode");
            }
        }

        /// <summary>The retained sprite preference, not the effective flat-world flag.</summary>
        public static bool LegacySpritesEnabled
        {
            get => legacySpritesEnabled;
            set
            {
                if (legacySpritesEnabled == value) return;
                legacySpritesEnabled = value;
                ZoneRenderHooks.MarkFullDirty("WorldPresentation.PreviousSprites");
            }
        }

        /// <summary>Read only these two choices; old 3D quality loads independently.</summary>
        public static void Load()
        {
            Mode = (WorldPresentationMode)PlayerPrefs.GetInt(PreferenceKey, 0);
            LegacySpritesEnabled = PlayerPrefs.GetInt(LegacySpritesPreferenceKey, 1) == 1;
        }

        /// <summary>Normal bootstrap calls once, before its first zone binding.
        /// QuickLoad deliberately retains the current runtime choice instead.</summary>
        public static void LoadForLaunch()
        {
            Load();
#if UNITY_EDITOR
            var forced = EditorLaunchModeResolver?.Invoke();
            if (forced.HasValue) Mode = forced.Value;
#endif
        }

#if UNITY_EDITOR
        /// <summary>Installed by the editor's existing NativeSaveIsolation owner.
        /// Its resolver validates active owner/token/root on every launch; normal
        /// player builds have neither this callback nor an audit override.</summary>
        public static Func<WorldPresentationMode?> EditorLaunchModeResolver;
#endif

        /// <summary>The shared menu/F11 operation. Apply before saving; an absent
        /// owner or failed apply cannot persist a presentation that was not applied.</summary>
        public static bool Toggle(Action apply)
        {
            if (apply == null) return false;
            var previous = Mode;
            Mode = previous == WorldPresentationMode.Flat16x24
                ? WorldPresentationMode.Previous : WorldPresentationMode.Flat16x24;
            try { apply(); }
            catch { Mode = previous; throw; }
            SaveMode();
            return true;
        }

        public static void SaveMode()
        {
            PlayerPrefs.SetInt(PreferenceKey, (int)Mode);
            PlayerPrefs.Save();
        }

        public static void SaveLegacySprites()
        {
            PlayerPrefs.SetInt(LegacySpritesPreferenceKey, LegacySpritesEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
