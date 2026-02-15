using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Data
{
    /// <summary>
    /// Loads conversation JSON files from Resources/Content/Conversations/ and caches them by ID.
    /// </summary>
    public static class ConversationLoader
    {
        private static Dictionary<string, ConversationData> _cache
            = new Dictionary<string, ConversationData>();

        private static bool _loaded;

        /// <summary>
        /// Get a conversation by ID. Loads all conversations on first access.
        /// </summary>
        public static ConversationData Get(string conversationID)
        {
            if (!_loaded)
                LoadAll();

            if (_cache.TryGetValue(conversationID, out var data))
                return data;

            Debug.LogWarning($"[Conversation] Conversation '{conversationID}' not found.");
            return null;
        }

        /// <summary>
        /// Load all conversation JSON files from Resources/Content/Conversations/.
        /// </summary>
        public static void LoadAll()
        {
            _cache.Clear();
            _loaded = true;

            var assets = Resources.LoadAll<TextAsset>("Content/Conversations");
            for (int i = 0; i < assets.Length; i++)
            {
                LoadFromJson(assets[i].text, assets[i].name);
            }

            Debug.Log($"[Conversation] Loaded {_cache.Count} conversations from {assets.Length} files.");
        }

        /// <summary>
        /// Parse a JSON string and add conversations to the cache.
        /// </summary>
        public static void LoadFromJson(string json, string sourceName = null)
        {
            var fileData = JsonUtility.FromJson<ConversationFileData>(json);
            if (fileData?.Conversations == null) return;

            _loaded = true;

            for (int i = 0; i < fileData.Conversations.Count; i++)
            {
                var conv = fileData.Conversations[i];
                if (string.IsNullOrEmpty(conv.ID))
                {
                    Debug.LogWarning($"[Conversation] Skipping conversation with no ID in {sourceName}");
                    continue;
                }
                _cache[conv.ID] = conv;
            }
        }

        /// <summary>
        /// Register a conversation directly (useful for tests).
        /// </summary>
        public static void Register(ConversationData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ID)) return;
            _cache[data.ID] = data;
            _loaded = true;
        }

        /// <summary>
        /// Clear the cache (for test isolation).
        /// </summary>
        public static void Reset()
        {
            _cache.Clear();
            _loaded = false;
        }
    }
}
