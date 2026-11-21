using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    // ── rows (JsonUtility: [Serializable] classes, arrays, strings, ints, bools; unknown keys are silently dropped, so forbidden
    //    fields are DECLARED here and rejected by the validator) ──

    [Serializable] internal sealed class InstallationRow
    {
        public string Id, Place, Blueprint, Retention, Purpose, Alternative, Basis, Absolute, Rng, Terms, Site, Kind;
        public string[] Near, Ground;
        public int Radius, MaxProbe, Revision, MinRadius;
        public bool Required;
    }
    [Serializable] public sealed class InstallationExemption { public string Place, Reason, Source; }
    [Serializable] public sealed class InstallationPending { public string Place, Owner, Note; }
    [Serializable] internal sealed class InstallationFile
    {
        public int Version;
        public string[] ForbiddenFactions;
        public InstallationRow[] Installations;
        public InstallationExemption[] Exemptions;
        public InstallationPending[] Pending;
    }

    /// <summary>Immutable view of one validated installation row: a deterministic, optional, fresh-only placement of an existing blueprint.</summary>
    public sealed class InstallationSpec
    {
        public string Id { get; }
        public string Place { get; }
        public string Blueprint { get; }
        /// <summary>True when the placed entity is state-bearing: a cached village holding it is never unloaded for regeneration.</summary>
        public bool Retain { get; }
        /// <summary>Anchor blueprints, best first; "Well" is always the last fallback so a spec cannot fail for lack of an anchor.</summary>
        public IReadOnlyList<string> Near { get; }
        public int Radius { get; }
        /// <summary>Candidates closer than this (Chebyshev) to the anchor are skipped, keeping the ring around a square's centre open. 0 in data means the default 2.</summary>
        public int MinRadius { get; }
        public int MaxProbe { get; }
        public IReadOnlyList<string> Ground { get; }
        public int Revision { get; }
        public string Purpose { get; }
        public string Alternative { get; }
        public string Basis { get; }
        /// <summary>For a world object that is the body of one of the place's repair sites (PlaceSites.json): the site id. Empty for an ordinary machine. A site object is the town's own, so it is not a row of the every-town census.</summary>
        public string Site { get; }
        /// <summary>"Works" for a place's own working fixture that is neither a forge nor the body of a repair site (a Sorters' frame, say): fresh-only and stateful like a site object, and likewise not a row of the every-town forge census. Empty for a forge.</summary>
        public string Kind { get; }

        internal InstallationSpec(InstallationRow row)
        {
            Id = row.Id; Place = row.Place; Blueprint = row.Blueprint; Retain = row.Retention == "Zone";
            var near = (row.Near ?? new string[0]).Where(n => n != "Well").ToList(); near.Add("Well"); Near = near.AsReadOnly();
            Radius = row.Radius; MinRadius = row.MinRadius > 0 ? row.MinRadius : 2; MaxProbe = row.MaxProbe; Revision = row.Revision;
            Ground = (row.Ground == null || row.Ground.Length == 0 ? InstallationRegistry.DefaultGround : row.Ground).ToList().AsReadOnly();
            Purpose = row.Purpose; Alternative = row.Alternative; Basis = row.Basis; Site = row.Site ?? ""; Kind = row.Kind ?? "";
        }
    }

    public sealed class CoverageEntry
    {
        public string Place, Kind, Detail;
    }

    /// <summary>"Every town" as a measurable ratchet: each authored place is Installed, Present, Exempt (with a source) or Pending (with an owner).</summary>
    public sealed class CoverageReport
    {
        public readonly List<CoverageEntry> Entries = new List<CoverageEntry>();
        /// <summary>Data-level contradictions: a place in two classes. Always empty when the data is consistent.</summary>
        public readonly List<string> Conflicts = new List<string>();
        public int Count(string kind) => Entries.Count(e => e.Kind == kind);
        public string KindOf(string place) => Entries.FirstOrDefault(e => e.Place == place)?.Kind;
        public string ToText()
        {
            var sb = new StringBuilder();
            foreach (var e in Entries) sb.Append(e.Place).Append(" | ").Append(e.Kind).Append(" | ").Append(e.Detail).Append('\n');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Validated installation specs and the every-town census (Docs/MACHINE-INSTALLATIONS.md, spec contract C3). Syntax loading is
    /// separate from factory admission (<see cref="Validate"/>). One bad optional row never cancels the others, unlike the process
    /// catalogue, because an installation is optional by construction (R6).
    /// </summary>
    public static class InstallationRegistry
    {
        public const int MaximumRows = 64;
        public const string WorksKind = "Works";
        public static readonly string[] DefaultGround = { "Grass", "StoneFloor", "Floor" };
        /// <summary>Places whose existing stations are pinned by a test (the start town and Cinderhold generate a forge already).</summary>
        public static readonly string[] PresentPlaces = { "Sill", "Cinderhold" };
        /// <summary>The three catacomb villages are zone ids, not authored map places.</summary>
        public static readonly string[] CatacombZones = { "Overworld.4.6.2", "Overworld.12.3.2", "Overworld.16.4.2" };

        static readonly List<InstallationSpec> Specs = new List<InstallationSpec>();
        static readonly List<InstallationExemption> Exemptions = new List<InstallationExemption>();
        static readonly List<InstallationPending> PendingRows = new List<InstallationPending>();
        static readonly List<string> Forbidden = new List<string>();
        static bool loaded;

        public static IReadOnlyList<InstallationSpec> All { get { LoadDefaults(); return Specs.AsReadOnly(); } }
        public static IReadOnlyList<string> ForbiddenFactions { get { LoadDefaults(); return Forbidden.AsReadOnly(); } }

        public static void LoadDefaults()
        {
            if (loaded) return;
            var asset = Resources.Load<TextAsset>("Content/Data/Installations");
            InitializeFromJson(asset == null ? null : asset.text);
        }
        public static void ResetForTests() { Specs.Clear(); Exemptions.Clear(); PendingRows.Clear(); Forbidden.Clear(); loaded = false; }

        public static IReadOnlyList<InstallationSpec> For(string place)
        {
            LoadDefaults();
            return place == null ? new InstallationSpec[0] : Specs.Where(s => s.Place == place).ToList().AsReadOnly();
        }
        public static InstallationExemption ExemptionFor(string place) { LoadDefaults(); return Exemptions.FirstOrDefault(e => e.Place == place); }
        public static InstallationPending PendingFor(string place) { LoadDefaults(); return PendingRows.FirstOrDefault(p => p.Place == place); }

        static bool Token(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 64) return false;
            foreach (char c in text) if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return false;
            return true;
        }
        static bool Text(string s) => !string.IsNullOrWhiteSpace(s) && s.Length <= 400;

        static string FactionOf(string place)
        {
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == place) return p.Faction;
            return null;
        }
        static bool KnownPlace(string place) => !string.IsNullOrEmpty(place) && (WorldMapAuthoring.Places.Any(p => p.Name == place) || CatacombZones.Contains(place));

        /// <summary>Loads and validates. Returns error codes "E04:row-id" (or "E01:file" for a file-level failure, which clears everything).
        /// A file-level failure leaves nothing loaded; a row-level failure drops only that row.</summary>
        public static IReadOnlyList<string> InitializeFromJson(string json)
        {
            loaded = true; Specs.Clear(); Exemptions.Clear(); PendingRows.Clear(); Forbidden.Clear();
            var errors = new List<string>(); InstallationFile file = null;
            try { if (!string.IsNullOrWhiteSpace(json)) file = JsonUtility.FromJson<InstallationFile>(json); }
            catch (Exception) { file = null; }
            if (file == null || file.Version != 1 || (file.Installations != null && file.Installations.Length > MaximumRows))
            { errors.Add("E01:file"); return errors.AsReadOnly(); }

            if (file.ForbiddenFactions != null) Forbidden.AddRange(file.ForbiddenFactions.Where(f => !string.IsNullOrWhiteSpace(f)));

            if (file.Exemptions != null)
                foreach (var ex in file.Exemptions)
                {
                    if (ex == null || !Text(ex.Reason) || !Text(ex.Source) || string.IsNullOrWhiteSpace(ex.Place)) { errors.Add("E13:" + (ex?.Place ?? "exemption")); continue; }
                    Exemptions.Add(ex);
                }
            if (file.Pending != null)
                foreach (var pending in file.Pending)
                {
                    if (pending == null || string.IsNullOrWhiteSpace(pending.Owner) || string.IsNullOrWhiteSpace(pending.Place)) { errors.Add("E13:" + (pending?.Place ?? "pending")); continue; }
                    PendingRows.Add(pending);
                }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in file.Installations ?? new InstallationRow[0])
            {
                string code = RowError(row, seen, file);
                if (code != null) { errors.Add(code + ":" + (row?.Id ?? "<null>")); continue; }
                seen.Add(row.Id); Specs.Add(new InstallationSpec(row));
            }
            return errors.AsReadOnly();
        }

        static string RowError(InstallationRow row, HashSet<string> seen, InstallationFile file)
        {
            if (row == null) return "E01";
            if (!Token(row.Id) || !Token(row.Blueprint) || (row.Near ?? new string[0]).Any(n => !Token(n)) || (row.Ground ?? new string[0]).Any(g => !Token(g))) return "E03";
            if (seen.Contains(row.Id)) return "E02";
            if (!KnownPlace(row.Place)) return "E04";
            if (CatacombZones.Contains(row.Place)) return "E12";
            if (file.Exemptions != null && file.Exemptions.Any(e => e != null && e.Place == row.Place)) return "E05";
            string faction = FactionOf(row.Place);
            if (faction != null && file.ForbiddenFactions != null && file.ForbiddenFactions.Contains(faction)) return "E06";
            if (row.Retention != "None" && row.Retention != "Zone") return "E07";
            if (row.Radius < 1 || row.Radius > 12 || row.MaxProbe < 1 || row.MaxProbe > 128 || row.Revision < 1 || (row.Near != null && row.Near.Length > 4)
                || row.MinRadius < 0 || row.MinRadius > row.Radius) return "E08";
            if (!Text(row.Purpose) || !Text(row.Alternative) || !Text(row.Basis)) return "E09";
            if (row.Required || !string.IsNullOrEmpty(row.Absolute) || !string.IsNullOrEmpty(row.Rng)) return "E10";
            if (!string.IsNullOrEmpty(row.Terms)) return "E11";
            // A works fixture is its own kind: never also a repair-site body, and the only kind besides the default forge.
            if (!string.IsNullOrEmpty(row.Kind) && (row.Kind != WorksKind || !string.IsNullOrEmpty(row.Site))) return "E16";
            // A site object names a repair site of its own place that exists, and (being the town's, with state in the settlement manager) needs no census row but must be retained like any stateful part.
            if (!string.IsNullOrEmpty(row.Site) && (!Token(row.Site) || PlaceSiteCatalog.Get(row.Place, row.Site) == null)) return "E15";
            return null;
        }

        /// <summary>The only Part types known to carry no finite state. Admission is an allowlist: any other Part (or one that cannot be resolved) is treated as
        /// stateful and forces <c>Retention: Zone</c>, so a new Part can never enter placement unclassified. Growing this list is a deliberate, reviewed change.</summary>
        static readonly Type[] Stateless =
            { typeof(RenderPart), typeof(PhysicsPart), typeof(ExaminablePart), typeof(ForgePart), typeof(AlchemyStillPart), typeof(LiquidSettlingPart), typeof(InstallationMarkerPart) };

        /// <summary>Blueprint-level admission of one spec (E07 state-bearing with Retention None; E14 not a placeable machine). Creates nothing.
        /// Null means admissible; otherwise the issue text, prefixed with its code.</summary>
        internal static string AdmissionIssue(EntityFactory factory, InstallationSpec spec)
        {
            if (factory?.Blueprints == null || spec == null) return "E14:no content factory";
            if (!factory.Blueprints.TryGetValue(spec.Blueprint, out var bp) || bp?.Parts == null) return "E14:" + spec.Id + ": missing blueprint " + spec.Blueprint;
            bool bad = bp.Tags != null && bp.Tags.ContainsKey("Creature"), stateBearing = false;
            foreach (var part in bp.Parts)
            {
                if (!factory.TryGetRegisteredPartType(part.Key, out var type)) { stateBearing = true; continue; }   // unresolved: fail closed
                // Brain/Body/Trader make a creature or merchant; Stacker and Takeable make an item; a footprint is a multi-cell prop; a Loadout draws from a static random stream.
                if (type == typeof(BrainPart) || type == typeof(Body) || type == typeof(SpatialFootprintPart) || type == typeof(TraderPart) || type == typeof(StackerPart) || type == typeof(LoadoutPart)) bad = true;
                if (!Stateless.Contains(type)) stateBearing = true;
                if (type == typeof(PhysicsPart) && part.Value != null && part.Value.TryGetValue("Takeable", out var takeable) && takeable == "true") bad = true;
            }
            if (bad) return "E14:" + spec.Id + ": " + spec.Blueprint + " is not a placeable machine (creature, trader, stack, loadout, footprint or takeable)";
            if (stateBearing && !spec.Retain) return "E07:" + spec.Id + ": " + spec.Blueprint + " carries state and needs Retention Zone";
            return null;
        }

        /// <summary>Blueprint-level admission against a content factory. Creates nothing. An empty list means every loaded row is admissible.</summary>
        public static IReadOnlyList<string> Validate(EntityFactory factory, WorldMap map = null)
        {
            LoadDefaults(); var issues = new List<string>();
            if (factory?.Blueprints == null) { issues.Add("E14:no content factory"); return issues.AsReadOnly(); }
            foreach (var spec in Specs) { var issue = AdmissionIssue(factory, spec); if (issue != null) issues.Add(issue); }
            return issues.AsReadOnly();
        }

        /// <summary>Classifies the 17 authored places and the three catacomb villages into exactly one class each.</summary>
        public static CoverageReport Coverage(WorldMap map = null)
        {
            LoadDefaults(); var report = new CoverageReport();
            var universe = WorldMapAuthoring.Places.Select(p => p.Name).Concat(CatacombZones).ToList();
            foreach (var place in universe)
            {
                var classes = new List<CoverageEntry>();
                var specs = Specs.Where(s => s.Place == place && string.IsNullOrEmpty(s.Site) && string.IsNullOrEmpty(s.Kind)).ToList();   // a site object or a works fixture is the town's own, not a forge installed in it
                var exempt = Exemptions.FirstOrDefault(e => e.Place == place);
                var pending = PendingRows.FirstOrDefault(p => p.Place == place);
                if (exempt != null) classes.Add(new CoverageEntry { Place = place, Kind = "Exempt", Detail = exempt.Reason + " [" + exempt.Source + "]" });
                if (specs.Count > 0) classes.Add(new CoverageEntry { Place = place, Kind = "Installed", Detail = string.Join(",", specs.Select(s => s.Id)) });
                if (PresentPlaces.Contains(place)) classes.Add(new CoverageEntry { Place = place, Kind = "Present", Detail = "legacy forge" });
                if (pending != null) classes.Add(new CoverageEntry { Place = place, Kind = "Pending", Detail = pending.Owner + ": " + pending.Note });
                if (classes.Count == 0) classes.Add(new CoverageEntry { Place = place, Kind = "Unclassified", Detail = "" });
                if (classes.Count > 1) report.Conflicts.Add(place + " is in " + string.Join("+", classes.Select(c => c.Kind)));
                report.Entries.Add(classes[0]);
            }
            return report;
        }
    }
}
