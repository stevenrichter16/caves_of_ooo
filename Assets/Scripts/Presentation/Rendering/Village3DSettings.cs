using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public static class Village3DSettings
    {
        public const string PreferenceKey = "CavesOfOoo.Village3D";
        public const string LowDetailPreferenceKey = "CavesOfOoo.Village3D.LowDetail";
        private static bool enabled = true;
        private static bool lowDetail;

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

        /// <summary>Reduce village decoration, shadow and render-target cost.
        /// Native owners, terrain, action reach and the UI keep their full behavior.</summary>
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

        public static void Load()
        {
            Enabled = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
            LowDetail = PlayerPrefs.GetInt(LowDetailPreferenceKey, 0) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(PreferenceKey, Enabled ? 1 : 0);
            PlayerPrefs.SetInt(LowDetailPreferenceKey, LowDetail ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
