using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static registry for quality/fact definitions loaded from
    /// Resources/Content/Data/Qualities/*.json.
    ///
    /// Qualities give facts human-readable names and optional metadata
    /// (description, category, display range). Conversation predicates and
    /// UI code can look up display info here without coupling to raw key strings.
    ///
    /// A missing quality definition is never an error — facts can exist without
    /// registered definitions (raw key strings are always valid).
    /// </summary>
    public static class QualityRegistry
    {
        private static readonly Dictionary<string, QualityDefinition> _definitions =
            new Dictionary<string, QualityDefinition>(StringComparer.Ordinal);

        private static bool _loaded;

        public static void LoadAll()
        {
            _definitions.Clear();
            var assets = Resources.LoadAll<TextAsset>("Content/Data/Qualities");
            foreach (var asset in assets)
                LoadFromJson(asset.text, asset.name);
            _loaded = true;
        }

        public static void LoadFromJson(string json, string sourceName = null)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var file = JsonUtility.FromJson<QualityDefinitionFile>(json);
                if (file?.Qualities == null) return;
                foreach (var def in file.Qualities)
                {
                    if (!string.IsNullOrEmpty(def.Key))
                        _definitions[def.Key] = def;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QualityRegistry] Failed to parse '{sourceName}': {ex.Message}");
            }
        }

        public static QualityDefinition Get(string key)
        {
            _definitions.TryGetValue(key, out var def);
            return def;
        }

        public static bool TryGet(string key, out QualityDefinition def) =>
            _definitions.TryGetValue(key, out def);

        public static void Register(QualityDefinition def)
        {
            if (def != null && !string.IsNullOrEmpty(def.Key))
                _definitions[def.Key] = def;
        }

        public static void Reset()
        {
            _definitions.Clear();
            _loaded = false;
        }

        public static bool IsLoaded => _loaded;
    }

    [Serializable]
    public class QualityDefinitionFile
    {
        public List<QualityDefinition> Qualities;
    }

    [Serializable]
    public class QualityDefinition
    {
        public string Key;
        public string DisplayName;
        public string Description;
        public string Category;
    }
}
