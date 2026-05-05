using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Registry of skill-tree definitions loaded from
    /// <c>Resources/Content/Data/Skills/*.json</c>. Mirrors Qud's
    /// <c>SkillFactory</c> (XRL.World.Skills/SkillFactory.cs) — singleton
    /// loaded at first access, exposes by-name + by-class lookups for
    /// both skills and the powers they contain.
    ///
    /// <para><b>Design mirrors Qud's 4-dict shape</b> (SkillFactory.cs:12-18):
    /// <see cref="_skillsByName"/>, <see cref="_skillsByClass"/>,
    /// <see cref="_powersByClass"/>, <see cref="_entriesByClass"/>. Names
    /// are looked up case-insensitively (StringComparer.OrdinalIgnoreCase) —
    /// same precedent as <c>MutationRegistry</c>.</para>
    ///
    /// <para><b>v1 scope (ST.2):</b> data layer only. No behavior.
    /// Subsequent sub-milestones add the runtime <c>SkillsPart</c> (ST.3),
    /// SP economy (ST.4), first concrete passive skill (ST.5), purchase
    /// gating (ST.6), UI overlay (ST.7), and showcase scenario (ST.8).
    /// See Docs/SKILL-TREE-QUD-PARITY.md.</para>
    /// </summary>
    public static class SkillRegistry
    {
        // ── JSON file shape ──────────────────────────────────────────────

        /// <summary>
        /// Top-level shape JsonUtility deserializes. Each Skills JSON file
        /// declares one or more skill trees in a "Skills" array.
        /// </summary>
        [Serializable]
        private class SkillRegistryFileData
        {
            public List<SkillData> Skills;
        }

        // ── Lookup tables (mirrors Qud's SkillFactory dict shape) ─────────

        /// <summary>By skill display-name (e.g. "Acrobatics").</summary>
        private static readonly Dictionary<string, SkillData> _skillsByName =
            new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);

        /// <summary>By skill class (e.g. "AcrobaticsSkill" — the runtime Part name).</summary>
        private static readonly Dictionary<string, SkillData> _skillsByClass =
            new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);

        /// <summary>By power class (e.g. "AcrobaticsDodgePower"). Cross-tree:
        /// any power's class can be looked up without knowing which skill
        /// tree it belongs to.</summary>
        private static readonly Dictionary<string, PowerData> _powersByClass =
            new Dictionary<string, PowerData>(StringComparer.OrdinalIgnoreCase);

        /// <summary>By any class (skill OR power). Provides the "is this
        /// class name owned?" check for the Requires/Exclusion gating.
        /// Mirrors Qud's EntriesByClass at SkillFactory.cs:18.</summary>
        private static readonly Dictionary<string, object> _entriesByClass =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;

        // ── Lifecycle ────────────────────────────────────────────────────

        /// <summary>
        /// Lazy-load all Skills JSON files from Resources on first access.
        /// Subsequent calls are no-ops. Pattern mirrors
        /// <c>MutationRegistry.EnsureInitialized</c>.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // Load every JSON file under Content/Data/Skills/. Mirrors the
            // StoryletRegistry / HouseDramaLoader convention. Empty
            // directory = empty registry; missing files are not an error.
            TextAsset[] assets = Resources.LoadAll<TextAsset>("Content/Data/Skills");
            if (assets == null) return;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] == null || string.IsNullOrWhiteSpace(assets[i].text))
                    continue;
                try
                {
                    LoadFromJson(assets[i].text, sourceLabel: assets[i].name);
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"SkillRegistry: failed to parse Content/Data/Skills/{assets[i].name}.json: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Test entry point. Wipes existing state and loads from a single
        /// JSON string. Same shape as <c>MutationRegistry.InitializeFromJson</c>.
        /// </summary>
        public static void InitializeFromJson(string json)
        {
            ResetForTests();
            _initialized = true;
            LoadFromJson(json, sourceLabel: "test");
        }

        /// <summary>
        /// Test helper: drop all cached state so the next query
        /// re-initializes (or remains empty until a test seeds it).
        /// </summary>
        public static void ResetForTests()
        {
            _initialized = false;
            _skillsByName.Clear();
            _skillsByClass.Clear();
            _powersByClass.Clear();
            _entriesByClass.Clear();
        }

        /// <summary>
        /// Runs on every Play-mode entry (and on player start in a build).
        /// Resets the static registry state so the next query lazy-reloads
        /// from <c>Resources/Content/Data/Skills/</c>.
        ///
        /// <para><b>Why this is needed:</b> the registry's <c>_initialized</c>
        /// flag and the four backing dictionaries are static, and Unity in
        /// the editor doesn't reload the C# domain between EditMode test
        /// runs and entering Play mode. Tests use
        /// <see cref="InitializeFromJson"/> with synthetic JSON to seed
        /// specific fixtures; that state can leak into Play mode and the
        /// player sees only whatever the last EditMode test left behind
        /// (the bug that surfaced as "I see only Dodge in the popup").
        /// New JSON content added between Play sessions is also picked up
        /// here — the registry can't have stale "1 tree" content when 5
        /// JSON files are on disk.</para>
        ///
        /// <para><c>SubsystemRegistration</c> stage runs early in the Play
        /// sequence, before any MonoBehaviour <c>Awake</c>, so the reset
        /// lands before <c>GameBootstrap</c> or any popup-driver could
        /// query a stale registry.</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart()
        {
            _initialized = false;
            _skillsByName.Clear();
            _skillsByClass.Clear();
            _powersByClass.Clear();
            _entriesByClass.Clear();
        }

        // ── Loading ──────────────────────────────────────────────────────

        /// <summary>
        /// Parse one Skills JSON document and merge into the registry.
        /// Duplicate skill names within or across files overwrite — last
        /// one wins. Per-skill / per-power entries with a missing Class
        /// field are skipped with a warning (mirrors Qud's silent-skip
        /// for malformed entries).
        /// </summary>
        public static void LoadFromJson(string json, string sourceLabel)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            SkillRegistryFileData data = JsonUtility.FromJson<SkillRegistryFileData>(json);
            if (data == null || data.Skills == null) return;

            for (int i = 0; i < data.Skills.Count; i++)
            {
                SkillData skill = data.Skills[i];
                if (skill == null) continue;

                if (string.IsNullOrWhiteSpace(skill.Class))
                {
                    Debug.LogWarning(
                        $"SkillRegistry [{sourceLabel}]: skipping skill at index {i} — missing 'Class' field.");
                    continue;
                }

                // By-name and by-class registration. Empty-name skills
                // are accepted (some tools may ship name-less data); only
                // Class is required.
                if (!string.IsNullOrWhiteSpace(skill.Name))
                    _skillsByName[skill.Name] = skill;
                _skillsByClass[skill.Class] = skill;
                _entriesByClass[skill.Class] = skill;

                // Powers: register each by its own class, set the parent
                // back-reference, and add to the cross-class lookup.
                if (skill.Powers != null)
                {
                    for (int j = 0; j < skill.Powers.Count; j++)
                    {
                        PowerData power = skill.Powers[j];
                        if (power == null) continue;
                        if (string.IsNullOrWhiteSpace(power.Class))
                        {
                            Debug.LogWarning(
                                $"SkillRegistry [{sourceLabel}]: skipping power '{power.Name}' under skill '{skill.Name}' — missing 'Class' field.");
                            continue;
                        }
                        power.ParentSkillName = skill.Name;
                        _powersByClass[power.Class] = power;
                        _entriesByClass[power.Class] = power;
                    }
                }
            }
        }

        // ── Lookups ──────────────────────────────────────────────────────

        public static bool TryGetSkillByName(string name, out SkillData skill)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(name)) { skill = null; return false; }
            return _skillsByName.TryGetValue(name, out skill);
        }

        public static bool TryGetSkillByClass(string className, out SkillData skill)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(className)) { skill = null; return false; }
            return _skillsByClass.TryGetValue(className, out skill);
        }

        public static bool TryGetPowerByClass(string className, out PowerData power)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(className)) { power = null; return false; }
            return _powersByClass.TryGetValue(className, out power);
        }

        /// <summary>
        /// Returns true if the registry knows of any entry (skill or
        /// power) with the given class name. Used by purchase-time
        /// gating to validate Requires / Exclusion entries.
        /// </summary>
        public static bool HasEntry(string className)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(className)) return false;
            return _entriesByClass.ContainsKey(className);
        }

        /// <summary>
        /// All skill trees currently registered. Iteration order is
        /// dictionary order (insertion order in modern .NET runtimes —
        /// stable enough for UI listing).
        /// </summary>
        public static IEnumerable<SkillData> GetAllSkills()
        {
            EnsureInitialized();
            return _skillsByClass.Values;
        }
    }
}
