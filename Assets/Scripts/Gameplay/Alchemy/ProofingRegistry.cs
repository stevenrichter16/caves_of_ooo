using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>One element a garment can be proofed against: which reagent property carries it, which resistance stat it raises and which liquid the bottled form is. Data: Content/Data/Alchemy/Proofing.json.</summary>
    [Serializable]
    public class ProofingElement
    {
        public string Id = "", Label = "", Property = "", Stat = "", Liquid = "", Alias = "", Status = "";

        /// <summary>What a settling tray catches for this element (a reagent blueprint carrying its property), and where it catches it: a terrain declaring a steady "heat" or "cold" (<see cref="SourceTile"/>), or a pool of one of the comma-separated <see cref="SourceLiquids"/>.</summary>
        public string Collects = "", SourceTile = "", SourceLiquids = "";
    }

    /// <summary>
    /// The proofing rules the game reads (Docs/MACHINE-PROOFING.md). A garment is proofed against an element by the reagent that carries it: a reagent's own property (heat, cold, conductive, corrosive)
    /// names the element and its level is how many coats it makes when pressed into a flask. A new reagent with one of those properties needs no new recipe, and a new element is one row here, one
    /// liquid and one resistance stat. Numbers that are not per-element (how strong a coat is, how long it lasts, what the permanent work costs) are <see cref="ProofingRules"/>.
    /// </summary>
    public static class ProofingRegistry
    {
        [Serializable] class FileData { public List<ProofingElement> Elements; }

        const string ResourcePath = "Content/Data/Alchemy/Proofing";
        static readonly List<ProofingElement> Elements = new List<ProofingElement>();
        static bool loaded;

        static void Ensure()
        {
            if (loaded) return; loaded = true;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text)) { Debug.LogWarning("ProofingRegistry: no data at '" + ResourcePath + "'."); return; }
            Load(asset.text);
        }

        /// <summary>Replaces the table from JSON text. A row with no id, property, stat or liquid, or one whose id, alias, property or liquid repeats an earlier row's, is dropped; malformed JSON leaves the table empty.</summary>
        public static void InitializeFromJson(string json) { loaded = true; Load(json); }

        static void Load(string json)
        {
            Elements.Clear();
            FileData data; try { data = JsonUtility.FromJson<FileData>(json); } catch (Exception) { return; }
            if (data?.Elements == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in data.Elements)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Id) || string.IsNullOrWhiteSpace(row.Property) || string.IsNullOrWhiteSpace(row.Stat) || string.IsNullOrWhiteSpace(row.Liquid)) continue;
                string[] keys = string.IsNullOrWhiteSpace(row.Alias) ? new[] { "id:" + row.Id, "p:" + row.Property, "l:" + row.Liquid } : new[] { "id:" + row.Id, "id:" + row.Alias, "p:" + row.Property, "l:" + row.Liquid };
                bool clash = false; foreach (var key in keys) if (seen.Contains(key)) clash = true;
                if (clash) continue;
                foreach (var key in keys) seen.Add(key);
                Elements.Add(row);
            }
        }

        public static void ResetForTests() { Elements.Clear(); loaded = false; }

        public static IReadOnlyList<ProofingElement> All { get { Ensure(); return Elements; } }

        /// <summary>The element with this id or alias (case-insensitive; the clay-wash's saved "fire" is heat), or null.</summary>
        public static ProofingElement ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var e in All) if (string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase) || (e.Alias.Length > 0 && string.Equals(e.Alias, id, StringComparison.OrdinalIgnoreCase))) return e;
            return null;
        }

        public static ProofingElement ByLiquid(string liquidId)
        {
            if (string.IsNullOrEmpty(liquidId)) return null;
            foreach (var e in All) if (string.Equals(e.Liquid, liquidId, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }

        public static ProofingElement ByProperty(string property)
        {
            if (string.IsNullOrEmpty(property)) return null;
            foreach (var e in All) if (string.Equals(e.Property, property, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }

        /// <summary>The element id a saved kind names; an unknown or missing kind reads as acid, as every save from before kinds existed did.</summary>
        public static string Canonical(string kind) => ById(kind)?.Id ?? "acid";

        /// <summary>The resistance stat a saved kind raises (acid's when the kind is unknown).</summary>
        public static string StatFor(string kind) => ById(kind)?.Stat ?? "AcidResistance";

        /// <summary>The elements a reagent carries, by its own properties; empty for a thing that is not a reagent or carries none.</summary>
        public static List<ProofingElement> ElementsOf(Entity reagent)
        {
            var result = new List<ProofingElement>(); var part = reagent?.GetPart<ReagentPart>(); if (part == null) return result;
            foreach (var property in part.GetProperties()) { var e = ByProperty(property.Property); if (e != null && !result.Contains(e)) result.Add(e); }
            return result;
        }

        /// <summary>How many coats pressing one of this reagent makes for the element: the level of the property that names it (0 when it does not carry it).</summary>
        public static int Units(Entity reagent, ProofingElement element)
        {
            var part = reagent?.GetPart<ReagentPart>(); if (part == null || element == null) return 0; int level = 0;
            foreach (var property in part.GetProperties()) if (string.Equals(property.Property, element.Property, StringComparison.OrdinalIgnoreCase)) level = Math.Max(level, property.Potency);
            return level;
        }
    }

    /// <summary>The numbers of proofing that are not per element (Docs/MACHINE-PROOFING.md). A coat is the temporary tier: stronger than the permanent one and short. The permanent tier is a smaller, lasting bonus from a machine.</summary>
    public static class ProofingRules
    {
        /// <summary>One coat: this much resistance for this many worn turns, topped up by another coat of the same element to at most <see cref="CoatMaxTurns"/>.</summary>
        public const int CoatAmount = 35, CoatTurns = 100, CoatMaxTurns = 300;

        /// <summary>The permanent proofing a machine gives: this much resistance, for these units of the element's proofing and this much fire clay. Below a purchased +50 garment on purpose; it stacks with a coat, a meal and gear toward immunity.</summary>
        public const int PermanentAmount = 25, PermanentUnits = 4, PermanentClay = 2;
    }
}
