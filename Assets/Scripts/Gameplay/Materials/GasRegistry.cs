using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static registry of <see cref="GasDefinition"/>s loaded from JSON
    /// (G.2). Direct mirror of <see cref="LiquidRegistry"/>'s loader
    /// shape: <c>Initialize(json)</c> / <c>InitializeFromJsonSources(...)</c>,
    /// a <c>ResetForTests</c> pollution guard, and Unity
    /// <see cref="JsonUtility"/> deserialization.
    ///
    /// <para>GameBootstrap loads
    /// <c>Resources/Content/Data/GasDefinitions/*.json</c> the same
    /// way it loads LiquidDefinitions (G.2 wires the boot call;
    /// tests drive Initialize directly).</para>
    /// </summary>
    public static class GasRegistry
    {
        private static readonly Dictionary<string, GasDefinition> _byId =
            new Dictionary<string, GasDefinition>();
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
        /// (one per file under the GasDefinitions folder). Later
        /// sources override earlier ones on Id collision — matches the
        /// LiquidRegistry / MaterialReactions multi-file merge convention.
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

            GasDefinitionCollection collection;
            try
            {
                collection = JsonUtility.FromJson<GasDefinitionCollection>(json);
            }
            catch (System.Exception ex)
            {
                // Malformed JSON must not crash boot/tests — mirror the
                // resilient-load posture of the rest of the content
                // pipeline. Drop the file, log, continue.
                Debug.LogWarning($"[GasRegistry] Skipping malformed gas JSON: {ex.Message}");
                return;
            }

            if (collection?.Gases == null)
                return;

            for (int i = 0; i < collection.Gases.Count; i++)
            {
                var def = collection.Gases[i];
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue; // skip anonymous/garbage rows
                _byId[def.Id] = def; // later wins on collision
            }
        }

        /// <summary>
        /// Look up a gas by id. Returns null for unknown / null /
        /// empty ids (callers treat null as "no such gas").
        /// </summary>
        public static GasDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return _byId.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary>
        /// TEST ONLY — clear the registry so a test's Initialize()
        /// doesn't leak gas definitions into a subsequent Play session
        /// or test. Mirrors <see cref="LiquidRegistry.ResetForTests"/>.
        /// </summary>
        public static void ResetForTests()
        {
            _byId.Clear();
            _initialized = false;
        }
    }
}
