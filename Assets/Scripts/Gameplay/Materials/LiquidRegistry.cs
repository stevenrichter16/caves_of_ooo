using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static registry of <see cref="LiquidDefinition"/>s loaded from
    /// JSON (LQ.2). Mirrors <see cref="MaterialReactionResolver"/>'s
    /// loader shape: <c>Initialize(json)</c> /
    /// <c>InitializeFromJsonSources(...)</c>, a <c>ResetForTests</c>
    /// pollution guard, and Unity <see cref="JsonUtility"/>
    /// deserialization.
    ///
    /// <para>GameBootstrap loads
    /// <c>Resources/Content/Data/LiquidDefinitions/*.json</c> the same
    /// way it loads MaterialReactions (LQ.7 wires the boot call; until
    /// then tests drive Initialize directly).</para>
    /// </summary>
    public static class LiquidRegistry
    {
        private static readonly Dictionary<string, LiquidDefinition> _byId =
            new Dictionary<string, LiquidDefinition>();
        private static bool _initialized;

        public static bool IsInitialized => _initialized;
        public static int Count => _byId.Count;

        /// <summary>Replace the registry from a single JSON document.</summary>
        public static void Initialize(string json)
        {
            _byId.Clear();
            AppendJson(json);
            _initialized = true;
        }

        /// <summary>
        /// Replace the registry by merging multiple JSON documents
        /// (one per file under the LiquidDefinitions folder). Later
        /// sources override earlier ones on Id collision — matches the
        /// MaterialReactions multi-file merge convention.
        /// </summary>
        public static void InitializeFromJsonSources(IEnumerable<string> jsonSources)
        {
            _byId.Clear();
            if (jsonSources != null)
            {
                foreach (var json in jsonSources)
                    AppendJson(json);
            }
            _initialized = true;
        }

        private static void AppendJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            LiquidDefinitionCollection collection;
            try
            {
                collection = JsonUtility.FromJson<LiquidDefinitionCollection>(json);
            }
            catch (System.Exception ex)
            {
                // Malformed JSON must not crash boot/tests — mirror the
                // resilient-load posture of the rest of the content
                // pipeline. Drop the file, log, continue.
                Debug.LogWarning($"[LiquidRegistry] Skipping malformed liquid JSON: {ex.Message}");
                return;
            }

            if (collection?.Liquids == null)
                return;

            for (int i = 0; i < collection.Liquids.Count; i++)
            {
                var def = collection.Liquids[i];
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue; // skip anonymous/garbage rows
                _byId[def.Id] = def; // later wins on collision
            }
        }

        /// <summary>
        /// Look up a liquid by id. Returns null for unknown / null /
        /// empty ids (callers treat null as "no such liquid").
        /// </summary>
        public static LiquidDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return _byId.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary>
        /// TEST ONLY — clear the registry so a test's Initialize()
        /// doesn't leak liquid definitions into a subsequent Play
        /// session or test. Mirrors
        /// <c>TinkerRecipeRegistry.ResetForTests</c>.
        /// </summary>
        public static void ResetForTests()
        {
            _byId.Clear();
            _initialized = false;
        }
    }
}
