using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using UnityEngine;

namespace CavesOfOoo.Core
{
    [Serializable]
    public class StartingBuildItem
    {
        public string Blueprint;
        /// <summary>How many. Zero, negative or absent means one (JsonUtility cannot
        /// apply a field initializer to list elements).</summary>
        public int Count = 1;
        /// <summary>Put it on rather than in the pack. Listed order IS equip order.</summary>
        public bool Equip;

        public int EffectiveCount => Count <= 0 ? 1 : Count;
    }

    [Serializable]
    public class StartingBuildAttributes
    {
        public int Strength;
        public int Agility;
        public int Toughness;
        public int Ego;

        public int Sum => Strength + Agility + Toughness + Ego;
    }

    /// <summary>
    /// One starting build: the attributes, worn gear, pack and skills a new
    /// character begins with. Data lives in
    /// <c>Resources/Content/Data/Builds/StartingBuilds.json</c>.
    /// <see cref="Kind"/> "classic" is the original start; its card is metadata
    /// and <see cref="StartingBuildService"/> applies it through the legacy
    /// <see cref="NewGameLoadout"/> and <see cref="StartingSpellKit"/>.
    /// </summary>
    [Serializable]
    public class StartingBuildDef
    {
        public string Id;
        public string Name;
        public string Kind = "build";
        public string Tagline;
        public string Weakness;
        public string FirstBuy;
        public StartingBuildAttributes Attributes = new StartingBuildAttributes();
        /// <summary>Listed in equip order: the weapon must come before any hand-slot
        /// non-weapon (a shield in the primary hand makes the fist the primary attack).</summary>
        public List<StartingBuildItem> Items = new List<StartingBuildItem>();
        /// <summary>Granted in this order; active skills land on hotbar slots 1..n in it.</summary>
        public List<string> Skills = new List<string>();

        public bool IsClassic => string.Equals(Kind, "classic", StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    public class StartingBuildFile
    {
        public List<StartingBuildDef> Builds;
    }

    /// <summary>
    /// The loaded set of starting builds. Mirrors the other data registries:
    /// <c>LoadFromJson</c> replaces (never appends), tolerates null/garbage by
    /// loading nothing, and <c>ResetForTests</c> clears.
    /// <see cref="Validate"/> is the content gate: every shipped rule that a
    /// hand edit of the JSON could silently break is checked there, and
    /// <see cref="StartingBuildService"/> re-runs the per-build rules before it
    /// mutates anything.
    /// </summary>
    public static class StartingBuildRegistry
    {
        public const string ClassicId = "classic";
        public const string ResourcePath = "Content/Data/Builds/StartingBuilds";

        /// <summary>The attribute budget: Strength + Agility + Toughness + Ego.
        /// Today's 18/18/18/16.</summary>
        public const int AttributeBudget = 70;

        /// <summary>The hotbar has this many slots; more skills than that would
        /// leave abilities unbound.</summary>
        public const int MaxSkillRows = ActivatedAbilitiesPart.SlotCount;

        private static List<StartingBuildDef> _all = new List<StartingBuildDef>();

        public static IReadOnlyList<StartingBuildDef> All => _all;

        public static void ResetForTests() => _all = new List<StartingBuildDef>();

        /// <summary>Load from Resources. Returns false (and leaves the set empty)
        /// when the asset is missing or malformed.</summary>
        public static bool LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                _all = new List<StartingBuildDef>();
                return false;
            }
            LoadFromJson(asset.text);
            return _all.Count > 0;
        }

        public static void LoadFromJson(string json)
        {
            var loaded = new List<StartingBuildDef>();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var file = JsonUtility.FromJson<StartingBuildFile>(json);
                    if (file?.Builds != null)
                    {
                        foreach (var b in file.Builds)
                        {
                            if (b == null) continue;
                            if (b.Attributes == null) b.Attributes = new StartingBuildAttributes();
                            if (b.Items == null) b.Items = new List<StartingBuildItem>();
                            if (b.Skills == null) b.Skills = new List<string>();
                            loaded.Add(b);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[StartingBuilds] could not parse the builds file: " + ex.Message);
                    loaded.Clear();
                }
            }
            _all = loaded;
        }

        public static StartingBuildDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _all.Count; i++)
                if (string.Equals(_all[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return _all[i];
            return null;
        }

        /// <summary>Every content problem in the loaded set; empty means shippable.</summary>
        public static List<string> Validate(EntityFactory factory)
        {
            var problems = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _all.Count; i++)
            {
                var b = _all[i];
                if (string.IsNullOrWhiteSpace(b.Id))
                {
                    problems.Add("build #" + (i + 1) + " has no Id");
                    continue;
                }
                if (!seen.Add(b.Id))
                    problems.Add(b.Id + ": duplicate Id");
                problems.AddRange(ValidateOne(b, factory));
            }
            return problems;
        }

        /// <summary>The rules for one build. Classic is exempt: its card is metadata.</summary>
        public static List<string> ValidateOne(StartingBuildDef b, EntityFactory factory)
        {
            var problems = new List<string>();
            if (b == null) { problems.Add("no build"); return problems; }
            if (b.IsClassic) return problems;
            string id = string.IsNullOrWhiteSpace(b.Id) ? "(no id)" : b.Id;

            var a = b.Attributes ?? new StartingBuildAttributes();
            if (a.Sum != AttributeBudget)
                problems.Add(id + ": attributes sum to " + a.Sum + ", the budget is " + AttributeBudget);
            foreach (var pair in new[] { ("Strength", a.Strength), ("Agility", a.Agility), ("Toughness", a.Toughness), ("Ego", a.Ego) })
                if (pair.Item2 % 2 != 0)
                    problems.Add(id + ": " + pair.Item1 + " " + pair.Item2 + " is odd; only even scores change the modifier, so an odd score is a wasted point");

            var skills = b.Skills ?? new List<string>();
            if (skills.Count > MaxSkillRows)
                problems.Add(id + ": " + skills.Count + " skills exceed the " + MaxSkillRows + " hotbar slots");
            var skillSeen = new HashSet<string>();
            foreach (var s in skills)
            {
                if (!SkillsPart.IsKnownSkillClass(s))
                    problems.Add(id + ": unknown skill class '" + s + "'");
                else if (!skillSeen.Add(s))
                    problems.Add(id + ": skill '" + s + "' is listed twice");
            }

            bool weaponEquipped = false;
            foreach (var it in b.Items ?? new List<StartingBuildItem>())
            {
                if (it == null || string.IsNullOrWhiteSpace(it.Blueprint))
                {
                    problems.Add(id + ": an item has no blueprint");
                    continue;
                }
                if (factory == null || !factory.Blueprints.TryGetValue(it.Blueprint, out var bp))
                {
                    problems.Add(id + ": unknown blueprint '" + it.Blueprint + "'");
                    continue;
                }
                if (!it.Equip) continue;

                bool equippable = bp.Parts.TryGetValue("Equippable", out var eq);
                if (!equippable)
                {
                    problems.Add(id + ": '" + it.Blueprint + "' cannot be equipped");
                    continue;
                }
                bool isWeapon = bp.Parts.ContainsKey("MeleeWeapon");
                bool handSlot = eq.TryGetValue("Slot", out var slot) && slot == "Hand";
                if (isWeapon) weaponEquipped = true;
                else if (handSlot && !weaponEquipped && HasEquippedWeaponLater(b, factory, it))
                    problems.Add(id + ": the weapon must be equipped before '" + it.Blueprint
                        + "' (a hand-slot item first makes the fist the primary attack and demotes the weapon to a 15% off-hand)");
            }
            return problems;
        }

        private static bool HasEquippedWeaponLater(StartingBuildDef b, EntityFactory factory, StartingBuildItem after)
        {
            bool past = false;
            foreach (var it in b.Items)
            {
                if (ReferenceEquals(it, after)) { past = true; continue; }
                if (!past || it == null || !it.Equip) continue;
                if (factory.Blueprints.TryGetValue(it.Blueprint ?? "", out var bp) && bp.Parts.ContainsKey("MeleeWeapon"))
                    return true;
            }
            return false;
        }
    }
}
