using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// C3c (Docs/PLACE-WORK-AND-PROGRESSION.md, Restore): the working structures of every
    /// place beyond Sill's hand-built well, oven and lantern, as data
    /// (Content/Data/PlaceSites.json). The settlement manager tracks each as a
    /// <see cref="RepairableSiteType.Structure"/> site of its place's settlement; a lasting
    /// repair needs every material and the tool (kept), a patch holds for
    /// <see cref="PlaceSiteFixData.RelapseTurns"/> turns and relapses.
    /// </summary>
    public static class PlaceSiteCatalog
    {
        public const string ResourcePath = "Content/Data/PlaceSites";

        [Serializable] public class FileData { public List<PlaceSiteData> Sites = new List<PlaceSiteData>(); }

        static List<PlaceSiteData> sites;

        public static IReadOnlyList<PlaceSiteData> All { get { EnsureLoaded(); return sites; } }

        static void EnsureLoaded()
        {
            if (sites != null) return;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            LoadFromJson(asset != null ? asset.text : null);
        }

        public static void LoadFromJson(string json)
        {
            sites = new List<PlaceSiteData>();
            if (string.IsNullOrWhiteSpace(json)) return;
            var file = JsonUtility.FromJson<FileData>(json);
            if (file?.Sites == null) return;
            foreach (var s in file.Sites)
                if (s != null && !string.IsNullOrEmpty(s.Place) && !string.IsNullOrEmpty(s.Id)) sites.Add(s);
        }

        public static void Reset() => sites = null;

        public static PlaceSiteData Get(string place, string id)
        {
            foreach (var s in All) if (s.Place == place && s.Id == id) return s;
            return null;
        }

        static string lastZone, lastPlace;

        /// <summary>The authored place standing at a surface zone id, or null. Trade prices
        /// ask this per row, so the last answer is kept (zone ids are immutable strings).</summary>
        public static string PlaceAt(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return null;
            if (ReferenceEquals(zoneId, lastZone) || zoneId == lastZone) return lastPlace;
            string found = null;
            foreach (var p in WorldMapAuthoring.Places)
                if (WorldMap.ToZoneID(p.X, p.Y, 0) == zoneId) { found = p.Name; break; }
            lastZone = zoneId; lastPlace = found;
            return found;
        }

        public static IEnumerable<PlaceSiteData> ForZone(string zoneId)
        {
            string place = PlaceAt(zoneId);
            if (place == null) yield break;
            foreach (var s in All) if (s.Place == place) yield return s;
        }

        public static RepairableSiteState NewState(PlaceSiteData s) => new RepairableSiteState
        {
            SiteId = s.Id,
            SiteType = RepairableSiteType.Structure,
            ProblemType = RepairProblemType.Structural,
            Stage = RepairStage.Fouled,
            Severity = 1,
            ResolvedByMethod = RepairMethodId.None,
            OutcomeTier = RepairOutcomeTier.None,
            ResolvedAtTurn = -1,
            RelapseAtTurn = null
        };

        public static List<string> Lint(Data.EntityFactory factory)
        {
            var problems = new List<string>();
            var seen = new HashSet<string>();
            foreach (var s in All)
            {
                string who = s.Place + ":" + s.Id;
                if (!seen.Add(who)) problems.Add(who + ": duplicate site");
                if (PlaceZone(s.Place) == null) problems.Add(who + ": unknown place '" + s.Place + "'");
                if (string.IsNullOrWhiteSpace(s.Name) || string.IsNullOrWhiteSpace(s.Broken) || string.IsNullOrWhiteSpace(s.Relapse))
                    problems.Add(who + ": needs a name, a broken description and a relapse line");
                LintFix(s.Lasting, who + "/lasting", factory, problems, required: true);
                if (s.HasPatch) LintFix(s.Patch, who + "/patch", factory, problems, required: false);
                if (s.HasPatch && s.Patch.RelapseTurns < 1) problems.Add(who + ": a patch needs relapse turns");
            }
            return problems;
        }

        static void LintFix(PlaceSiteFixData fix, string who, Data.EntityFactory factory, List<string> problems, bool required)
        {
            if (fix == null || fix.Materials == null || fix.Materials.Count == 0) { if (required) problems.Add(who + ": no materials"); return; }
            if (string.IsNullOrWhiteSpace(fix.Text)) problems.Add(who + ": no text");
            foreach (var m in fix.Materials)
            {
                if (m == null || m.Count < 1) problems.Add(who + ": material count");
                else if (factory != null && !factory.Blueprints.ContainsKey(m.Blueprint ?? "")) problems.Add(who + ": unknown material '" + m.Blueprint + "'");
            }
            if (!string.IsNullOrEmpty(fix.Tool) && factory != null && !factory.Blueprints.ContainsKey(fix.Tool)) problems.Add(who + ": unknown tool '" + fix.Tool + "'");
        }

        static string PlaceZone(string place)
        {
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == place) return WorldMap.ToZoneID(p.X, p.Y, 0);
            return null;
        }
    }

    [Serializable]
    public class PlaceSiteData
    {
        public string Place;
        /// <summary>Stable id, frozen once shipped: it lives in saves.</summary>
        public string Id;
        /// <summary>What it is called in speech ("fishing weir").</summary>
        public string Name;
        /// <summary>How it looks while broken.</summary>
        public string Broken;
        public PlaceSiteFixData Lasting = new PlaceSiteFixData();
        public PlaceSiteFixData Patch = new PlaceSiteFixData();
        /// <summary>The message on returning to find a patch has failed.</summary>
        public string Relapse;

        public bool HasPatch => Patch?.Materials != null && Patch.Materials.Count > 0;
    }

    [Serializable]
    public class PlaceSiteFixData
    {
        public List<PlaceSiteMaterial> Materials = new List<PlaceSiteMaterial>();
        /// <summary>Optional blueprint the player must carry; not consumed.</summary>
        public string Tool;
        public int RelapseTurns;
        public string Text;
    }

    [Serializable]
    public class PlaceSiteMaterial
    {
        public string Blueprint;
        public int Count = 1;
    }
}
