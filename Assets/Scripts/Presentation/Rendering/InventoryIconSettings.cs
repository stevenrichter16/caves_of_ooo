using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Shared item-row icon preference for inventory and storage, outside saved games.</summary>
    public static class InventoryIconSettings
    {
        public const string PreferenceKey = "CavesOfOoo.InventoryIcons";

        /// <summary>Cached runtime choice. Changes persist only when Save is called.</summary>
        public static bool Enabled { get; set; } = true;

        public static void Load()
        {
            Enabled = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(PreferenceKey, Enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
