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
                if (ground != PlacementCheck.OpenGround && ground != PlacementCheck.BareGround && ground != PlacementCheck.ShoreGround) { errors.Add("P05 " + row.Kind + ": unknown GroundClass '" + ground + "'"); continue; }
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

    /// <summary>The kinds the camp verbs know by name ("make camp" lays them out in this order, the readers say what a hearth or box holds). Data in PackableFixtures.json; named once here.</summary>
    public static class CampKinds
    {
        public const string Hearth = "camp-hearth", Still = "camp-still", Bench = "camp-bench", Box = "camp-box", FishTrap = "camp-fishtrap", Awning = "camp-awning", Tray = "camp-tray";
    }

    /// <summary>
    /// The only Part home/base owns (design 5.2): the definition key and the one timestamp readers need. Primitives and strings only. It hosts the verbs:
    /// a carried kit offers "set down" in the pack menu, a standing fixture offers "take up" and "hand over" in the world menu.
    /// </summary>
    public sealed class PackableFixturePart : Part, IDescribeState, IStationReadinessGuard, IDescribeUse
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

        /// <summary>
        /// The name the player gave the camp this fixture stands in ("Home", "Lookout"), or empty. It is only a label for the readers and notes: no rule reads it. "Name this camp"
        /// writes the same name onto every fixture of the camp (the same cluster the arrival note and map notes use), so the camp keeps its name if one piece is taken up and set
        /// down elsewhere within reach. Additive saved field: a save without it loads as unnamed.
        /// </summary>
        public string CampName = "";

        // ── reader contract (Docs/MACHINE-READERS.md): one state line in both the quick and the full Examine ──
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 80;
        public string DescribeState(DescribeContext context) => HeldFixtureReader.Describe(ParentEntity, this);

        /// <summary>A kit being carried explains how to set it down first; a standing one explains what it does first and how to move it last.</summary>
        int IDescribeUse.Order => HeldFixtureActions.IsPlayerHeld(ParentEntity) ? 100 : 0;

        /// <summary>
        /// The kit's own how-to (the owner, 2026-11-21): how to set it down, what ground it needs, and how to take it up, move it or hand it over. What the kit does once it stands is its own Parts' guide. The "right
        /// now" line is built from <see cref="HeldFixtureActions.AddActions"/>'s rows, so it names the cells that would take it or the one reason none will.
        /// </summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            var def = PackableFixtureRegistry.Get(Kind); if (def == null || def.Class != PackableFixtureDefinition.Carried) return;
            var installable = ParentEntity?.GetPart<InstallablePart>();
            bool installed = installable != null && installable.State == InstallablePart.Installed;
            if (installed && !HeldFixtureActions.IsPlayerHeld(ParentEntity)) return;   // a fixture of the world, or one you handed over: it cannot be taken up, and its own Parts say what it does
            if (!installed)
            {
                guide.How.Add("Press C on an open cell beside you and choose set down, or choose set down from the pack. With two or more kits, make camp here sets down every kit you carry that has room.");
                guide.Needs.Add(GroundNeeds(def));
                guide.Limits.Add("It does its work only where it is set down. You can keep " + PlacementCheck.ZoneCap + " things standing in one area and " + PlacementCheck.WorldCap + " in the world.");
                if (ParentEntity?.SpatialZone != null) guide.WhyNot = "it lies on the ground: pick it up first";
                return;
            }
            guide.How.Add("To take it up, move it, name this camp or hand it over, press C beside it. Pack up camp takes up every kit of yours within " + HeldFixtureActions.PackUpReach + " cells in one action.");
            if (ParentEntity.GetPart<ContainerPart>() is ContainerPart box) guide.How.Add("Open it from its menu to put things in or take them out" + (box.MaxItems >= 0 ? " (it takes up to " + box.MaxItems + " separate things; a stack counts as one)." : "."));
            if (Kind == CampKinds.Box && ParentEntity.BlueprintName == "CampBox")
            {
                guide.How.Add("For material work, press C beside the box. Work rows use only its direct contents; keep required tools with you. Each row shows where the result goes. A forge process still needs a nearby working forge.");
                guide.How.Add("An oil flask stored here can refill an unlit torch in your pack. The flask stays in the box.");
                guide.Limits.Add("Each shown work action spends its declared materials and one action.");
            }
            guide.Limits.Add("Taking it up keeps its contents and fuel. Handing it over leaves it standing, and you can no longer take it up.");
            guide.WhyNot = "you are not beside it";
        }

        /// <summary>What the ground must be, from the placement rules' own constants.</summary>
        static string GroundNeeds(PackableFixtureDefinition def)
        {
            string country = string.Join(", ", HeldPlaceZones.AllowedBiomes.Select(b => "the " + b)); int comma = country.LastIndexOf(", ", StringComparison.Ordinal); if (comma >= 0) country = country.Substring(0, comma) + " or" + country.Substring(comma + 1);
            string common = "a cell with nothing on it but ground, and nothing solid or hazardous on " + (def.Solid ? "any of the eight cells around it" : "the four cells beside it") + "; not a road; not within " + PlacementCheck.DoorStairReach + " cells of a door, stair or well; at least " + PlacementCheck.EdgeCells + " cells from the edge of the area; in " + country + "; not in a village or other protected ground, and not underground";
            if (def.GroundClass == PlacementCheck.ShoreGround) return "ground right beside water it can fish (a wide pool of falling water, such as a cascade basin): " + common;
            if (def.GroundClass == PlacementCheck.BareGround) return "bare ground with nothing growing on it: " + common;
            return "open ground: " + common;
        }

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
