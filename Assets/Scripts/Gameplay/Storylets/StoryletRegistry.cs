using System.Collections.Generic;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Loads StoryletData definitions from Resources/Content/Data/Storylets/.
    /// Mirrors HouseDramaLoader's pattern (incl. the _loaded flag fix from
    /// commit 83e9522: Register / LoadFromJson / Reset all set _loaded=true so
    /// that programmatically populated state survives the next Get/GetAll
    /// rather than being wiped by an EnsureLoaded auto-reload).
    ///
    /// On top of the loader pattern, this validates each storylet's trigger
    /// predicate names and effect action names against the conversation
    /// registries at load time — unknown names are rejected with a warning
    /// rather than fail-OPEN at evaluate time (M0 finding C3).
    /// </summary>
    public static class StoryletRegistry
    {
        private static readonly Dictionary<string, StoryletData> _storylets =
            new Dictionary<string, StoryletData>();

        private static bool _loaded;

        public static void LoadAll()
        {
            _storylets.Clear();
            _loaded = false;

            var assets = Resources.LoadAll<TextAsset>("Content/Data/Storylets");
            foreach (var asset in assets)
                LoadFromJson(asset.text, asset.name);

            _loaded = true;
            Debug.Log($"[StoryletRegistry] Loaded {_storylets.Count} storylet(s).");
        }

        public static void LoadFromJson(string json, string sourceName = null)
        {
            if (string.IsNullOrEmpty(json)) return;

            StoryletFileData fileData;
            try
            {
                fileData = JsonUtility.FromJson<StoryletFileData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[StoryletRegistry] Failed to parse '{sourceName}': {ex.Message}");
                return;
            }

            if (fileData?.Storylets == null) return;

            foreach (var storylet in fileData.Storylets)
            {
                if (string.IsNullOrEmpty(storylet.ID))
                {
                    Debug.LogWarning($"[StoryletRegistry] Storylet in '{sourceName}' has no ID; skipped.");
                    continue;
                }

                if (!ValidateNames(storylet, sourceName))
                    continue;

                _storylets[storylet.ID] = storylet;
            }

            _loaded = true;
        }

        public static StoryletData Get(string id)
        {
            EnsureLoaded();
            return _storylets.TryGetValue(id, out var data) ? data : null;
        }

        public static List<StoryletData> GetAll()
        {
            EnsureLoaded();
            return new List<StoryletData>(_storylets.Values);
        }

        public static void Register(StoryletData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ID)) return;
            _storylets[data.ID] = data;
            _loaded = true;
        }

        public static void Reset()
        {
            _storylets.Clear();
            _loaded = true;
        }

        private static void EnsureLoaded()
        {
            if (!_loaded) LoadAll();
        }

        private static bool ValidateNames(StoryletData storylet, string sourceName)
        {
            if (!ValidateParams(storylet.Triggers, isPredicate: true, storylet.ID, sourceName, "Triggers"))
                return false;
            if (!ValidateParams(storylet.Effects, isPredicate: false, storylet.ID, sourceName, "Effects"))
                return false;

            if (storylet.IsQuest)
            {
                for (int i = 0; i < storylet.Quest.Stages.Count; i++)
                {
                    var stage = storylet.Quest.Stages[i];
                    string stageRef = $"Stages[{i}]({stage.ID})";
                    if (!ValidateParams(stage.Triggers, isPredicate: true, storylet.ID, sourceName, $"{stageRef}.Triggers"))
                        return false;
                    if (!ValidateParams(stage.OnEnter, isPredicate: false, storylet.ID, sourceName, $"{stageRef}.OnEnter"))
                        return false;
                }
            }

            return true;
        }

        private static bool ValidateParams(
            List<ConversationParam> parameters,
            bool isPredicate,
            string storyletId,
            string sourceName,
            string sectionLabel)
        {
            if (parameters == null) return true;

            for (int i = 0; i < parameters.Count; i++)
            {
                string name = parameters[i].Key;
                bool registered = isPredicate
                    ? ConversationPredicates.IsRegistered(name)
                    : ConversationActions.IsRegistered(name);

                if (!registered)
                {
                    string kind = isPredicate ? "predicate" : "action";
                    Debug.LogWarning(
                        $"[StoryletRegistry] Storylet '{storyletId}' in '{sourceName}': "
                        + $"unknown {kind} '{name}' in {sectionLabel}; storylet rejected.");
                    return false;
                }
            }

            return true;
        }
    }
}
