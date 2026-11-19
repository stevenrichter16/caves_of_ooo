using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    [Serializable] internal sealed class PackableFixtureRow
    {
        public string Kind, Class, Blueprint, GroundClass, Voice;
        public bool Solid;
        public int Rounds;
        public string[] Protects, Manifest;
    }
    [Serializable] internal sealed class PackableFixtureFile { public int Version; public PackableFixtureRow[] Fixtures; }

    /// <summary>
    /// One validated kind of set-down fixture (Docs/HOME-BASE-DESIGN.md section 5.1). Definitions are data and are never saved per instance.
    /// </summary>
    public sealed class PackableFixtureDefinition
    {
        public const string Carried = "Carried", Hauled = "Hauled";
        public string Kind { get; }
        /// <summary><see cref="Carried"/> (a kit set down with the verbs) or <see cref="Hauled"/> (the rig: not installable here).</summary>
        public string Class { get; }
        public string Blueprint { get; }
        public bool Solid { get; }
        public string GroundClass { get; }
        public int Rounds { get; }
        /// <summary>Dormant until protection tags exist (design 7.3); stored so a later slice needs no data migration.</summary>
        public IReadOnlyList<string> Protects { get; }
        /// <summary>Dormant: an explicit finite salvage manifest for a reclaim verb; empty by default.</summary>
        public IReadOnlyList<string> Manifest { get; }
        /// <summary>An intent-card id for strings, never the strings themselves.</summary>
        public string Voice { get; }
        public PlacementSpec Spec => new PlacementSpec { Solid = Solid, GroundClass = GroundClass };

        internal PackableFixtureDefinition(PackableFixtureRow row)
        {
            Kind = row.Kind; Class = row.Class; Blueprint = row.Blueprint; Solid = row.Solid; GroundClass = string.IsNullOrEmpty(row.GroundClass) ? PlacementCheck.OpenGround : row.GroundClass;
            Rounds = row.Rounds > 0 ? row.Rounds : 1; Voice = row.Voice ?? "";
            Protects = (row.Protects ?? new string[0]).ToList().AsReadOnly(); Manifest = (row.Manifest ?? new string[0]).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// The set-down fixture definitions (data: <c>Content/Data/PackableFixtures.json</c>). A bad row is dropped and reported; it never disables another row or the game.
    /// A saved kind that no longer resolves leaves its fixture inert but retrievable (take-up refuses with <c>unknown-kind</c>; hand-over works).
    /// </summary>
    public static class PackableFixtureRegistry
    {
        static readonly Dictionary<string, PackableFixtureDefinition> Kinds = new Dictionary<string, PackableFixtureDefinition>(StringComparer.Ordinal);
        static bool loaded;

        public static IReadOnlyCollection<PackableFixtureDefinition> All { get { LoadDefaults(); return Kinds.Values; } }

        public static void LoadDefaults()
        {
            if (loaded) return; loaded = true;
            var asset = Resources.Load<TextAsset>("Content/Data/PackableFixtures");
            if (asset != null) InitializeFromJson(asset.text);
        }

        public static void ResetForTests() { Kinds.Clear(); loaded = false; }

        public static PackableFixtureDefinition Get(string kind)
        {
            LoadDefaults();
            return !string.IsNullOrEmpty(kind) && Kinds.TryGetValue(kind, out var def) ? def : null;
        }

        /// <summary>Loads rows from JSON text, replacing any loaded rows. Returns one error line per dropped row (code, kind, reason); malformed JSON drops everything.</summary>
        public static IReadOnlyList<string> InitializeFromJson(string json)
        {
            loaded = true; Kinds.Clear(); var errors = new List<string>();
            PackableFixtureFile file;
            try { file = JsonUtility.FromJson<PackableFixtureFile>(json); }
            catch (Exception) { errors.Add("P00 <file>: malformed JSON"); return errors; }
            if (file?.Fixtures == null) return errors;
            foreach (var row in file.Fixtures)
            {
                string kind = row?.Kind ?? "<null>";
                if (row == null || string.IsNullOrWhiteSpace(row.Kind)) { errors.Add("P01 " + kind + ": missing Kind"); continue; }
                if (Kinds.ContainsKey(row.Kind)) { errors.Add("P02 " + row.Kind + ": duplicate Kind"); continue; }
                if (row.Class != PackableFixtureDefinition.Carried && row.Class != PackableFixtureDefinition.Hauled) { errors.Add("P03 " + row.Kind + ": unknown Class '" + row.Class + "'"); continue; }
                if (row.Rounds < 0) { errors.Add("P04 " + row.Kind + ": negative Rounds"); continue; }
                string ground = string.IsNullOrEmpty(row.GroundClass) ? PlacementCheck.OpenGround : row.GroundClass;
                if (ground != PlacementCheck.OpenGround && ground != PlacementCheck.BareGround) { errors.Add("P05 " + row.Kind + ": unknown GroundClass '" + ground + "'"); continue; }
                if (string.IsNullOrWhiteSpace(row.Blueprint)) { errors.Add("P06 " + row.Kind + ": missing Blueprint"); continue; }
                Kinds[row.Kind] = new PackableFixtureDefinition(row);
            }
            return errors;
        }

        /// <summary>Blueprint-level consistency of every loaded row (the structural checks ran at load). A row whose blueprint is missing is skipped by reduced content packs.</summary>
        public static IReadOnlyList<string> Validate(EntityFactory factory)
        {
            LoadDefaults(); var errors = new List<string>();
            foreach (var def in Kinds.Values)
            {
                if (factory == null || !factory.Blueprints.ContainsKey(def.Blueprint)) { errors.Add("P07 " + def.Kind + ": blueprint '" + def.Blueprint + "' does not exist"); continue; }
                var probe = factory.CreateEntity(def.Blueprint);
                var physics = probe?.GetPart<PhysicsPart>();
                if (def.Class == PackableFixtureDefinition.Carried && (physics == null || !physics.Takeable)) errors.Add("P08 " + def.Kind + ": a Carried kit's blueprint must be Takeable");
                if (def.Class == PackableFixtureDefinition.Hauled && physics != null && physics.Takeable) errors.Add("P09 " + def.Kind + ": a Hauled fixture's blueprint must not be Takeable");
                if (def.Class == PackableFixtureDefinition.Carried && def.Solid && probe?.GetPart<PhysicsPart>() == null) errors.Add("P10 " + def.Kind + ": a solid fixture needs a PhysicsPart");
                if (def.Class == PackableFixtureDefinition.Carried && probe != null && probe.HasPart<ContainerPart>() && !probe.HasPart<ContainerLoadPart>())
                    errors.Add("P13 " + def.Kind + ": a container kit without ContainerLoad would launder its contents' weight");
                if (probe != null && probe.GetPart<PackableFixturePart>() == null) errors.Add("P11 " + def.Kind + ": blueprint carries no PackableFixture Part");
                else if (probe != null && probe.GetPart<PackableFixturePart>().Kind != def.Kind) errors.Add("P12 " + def.Kind + ": blueprint's PackableFixture Kind is '" + probe.GetPart<PackableFixturePart>().Kind + "'");
            }
            return errors;
        }
    }

    /// <summary>
    /// The only Part home/base owns (design 5.2): the definition key and the one timestamp readers need. Primitives and strings only. It hosts the verbs:
    /// a carried kit offers "set down" in the pack menu, a standing fixture offers "take up" and "hand over" in the world menu.
    /// </summary>
    public sealed class PackableFixturePart : Part, IDescribeState, IStationReadinessGuard
    {
        public string Kind = "";

        /// <summary>
        /// A carried-class kit that is not set down is inert: carried, dropped, or taken up it is no station and offers no work rows. Only the set-down state (and a handed-over
        /// fixture, which stays installed) makes it a station. A kind that no longer resolves is not treated as a kit, so old data cannot brick an object.
        /// </summary>
        public static bool IsInertKit(Entity owner)
        {
            var part = owner?.GetPart<PackableFixturePart>(); if (part == null) return false;
            var def = PackableFixtureRegistry.Get(part.Kind); if (def == null || def.Class != PackableFixtureDefinition.Carried) return false;
            return owner.GetPart<InstallablePart>()?.State != InstallablePart.Installed;
        }

        public bool TryBlock(Entity station, Zone zone, out string code)
        { code = IsInertKit(station) ? "loose" : null; return code != null; }
        /// <summary>The turn tick of the set-down; -1 while carried.</summary>
        public int SetTick = -1;

        // ── reader contract (Docs/MACHINE-READERS.md): one state line in both the quick and the full Examine ──
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 80;
        public string DescribeState(DescribeContext context) => HeldFixtureReader.Describe(ParentEntity, this);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                HeldFixtureActions.AddActions(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (!HeldFixtureActions.IsCommand(command)) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone");
                var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                if (HeldFixtureActions.TryAct(actor, ParentEntity, this, zone, command, transaction)) { e.Handled = true; return false; }
                return true;
            }
            return true;
        }
    }
}
