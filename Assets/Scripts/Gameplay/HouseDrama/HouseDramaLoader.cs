using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Data
{
    /// <summary>
    /// Loads HouseDrama definitions from Resources/Content/Data/HouseDramas/.
    /// Follows the same pattern as ConversationLoader and FactionLoader.
    /// </summary>
    public static class HouseDramaLoader
    {
        private static readonly Dictionary<string, HouseDramaData> _dramas =
            new Dictionary<string, HouseDramaData>();

        private static bool _loaded;

        public static void LoadAll()
        {
            _dramas.Clear();
            _loaded = false;

            var assets = Resources.LoadAll<TextAsset>("Content/Data/HouseDramas");
            foreach (var asset in assets)
                LoadFromJson(asset.text, asset.name);

            _loaded = true;
            Debug.Log($"[HouseDramaLoader] Loaded {_dramas.Count} drama(s).");
        }

        public static void LoadFromJson(string json, string sourceName = null)
        {
            if (string.IsNullOrEmpty(json)) return;

            HouseDramaFileData fileData;
            try
            {
                fileData = JsonUtility.FromJson<HouseDramaFileData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HouseDramaLoader] Failed to parse '{sourceName}': {ex.Message}");
                return;
            }

            if (fileData?.Dramas == null) return;

            foreach (var drama in fileData.Dramas)
            {
                if (string.IsNullOrEmpty(drama.ID))
                {
                    Debug.LogWarning($"[HouseDramaLoader] Drama in '{sourceName}' has no ID; skipped.");
                    continue;
                }

                var errors = drama.Validate();
                foreach (var error in errors)
                    Debug.LogWarning($"[HouseDramaLoader] Drama '{drama.ID}' in '{sourceName}': {error}");

                _dramas[drama.ID] = drama;
            }
        }

        public static HouseDramaData Get(string id)
        {
            EnsureLoaded();
            return _dramas.TryGetValue(id, out var data) ? data : null;
        }

        public static List<HouseDramaData> GetAll()
        {
            EnsureLoaded();
            return new List<HouseDramaData>(_dramas.Values);
        }

        public static void Register(HouseDramaData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ID)) return;
            _dramas[data.ID] = data;
        }

        public static void Reset()
        {
            _dramas.Clear();
            _loaded = false;
        }

        private static void EnsureLoaded()
        {
            if (!_loaded) LoadAll();
        }
    }
}
