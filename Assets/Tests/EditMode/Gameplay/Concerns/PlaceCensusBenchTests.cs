using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Content-wave census (Docs/PLACE-WORK-AND-PROGRESSION.md, W1–W5): for every authored
    /// place on seed 64, who actually lives there (residents that can talk), what stands
    /// there, and — for every shipped concern — whether its owner, its Carry recipients
    /// and its Read documents' places are reachable. Writes Library/ProgressionBench/
    /// place-census.txt. Explicit: it generates the world's places.
    ///
    /// The assertion half is the reachability gate: a concern whose owner does not live
    /// in its place, or whose recipient does not live in the recipient's place, can never
    /// be played, whatever its lint says.
    /// </summary>
    [Explicit("Generates every authored place; run on demand.")]
    [Category("ProgressionBench")]
    public sealed class PlaceCensusBenchTests
    {
        const int Seed = 64;

        static EntityFactory LoadWorldFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            LoadoutPart.Factory = f; LootDropSystem.Factory = f; ContainerPlacementService.Factory = f; TraderPart.Factory = f;
            return f;
        }

        [Test] public void EveryPlacesResidentsAndEveryConcernsPeopleAreThere()
        {
            var factory = LoadWorldFactory();
            var manager = OverworldZoneManager.CreateDetached(factory, Seed);
            var residents = new Dictionary<string, HashSet<string>>();
            var sold = new Dictionary<string, HashSet<string>>();
            var report = new StringBuilder("PLACE CENSUS seed " + Seed + "\n");
            foreach (var p in WorldMapAuthoring.Places)
            {
                string id = WorldMap.ToZoneID(p.X, p.Y, 0);
                var zone = manager.GetZone(id);
                var talkers = new SortedDictionary<string, int>(); var things = new SortedDictionary<string, int>();
                foreach (var e in zone.GetAllEntities())
                {
                    if (e.HasTag("Creature") && e.GetPart<ConversationPart>() != null) { talkers.TryGetValue(e.BlueprintName, out int n); talkers[e.BlueprintName] = n + 1; }
                    else if (!e.HasTag("Terrain") && !e.HasTag("Creature") && e.GetPart<RenderPart>() != null) { things.TryGetValue(e.BlueprintName, out int n); things[e.BlueprintName] = n + 1; }
                }
                residents[p.Name] = new HashSet<string>(talkers.Keys);
                var goods = sold[p.Name] = new HashSet<string>();
                foreach (var e in zone.GetAllEntities())
                {
                    var inv = e.GetPart<InventoryPart>();
                    if (inv != null && e.HasTag("Creature") && !ReferenceEquals(e, null) && e.GetPart<TraderPart>() != null)
                        foreach (var item in inv.Objects) goods.Add(item.BlueprintName);
                    var box = e.GetPart<ContainerPart>();
                    if (box?.Contents != null) foreach (var item in box.Contents) goods.Add(item.BlueprintName);
                    if (!e.HasTag("Creature") && !e.HasTag("Terrain") && e.GetPart<PhysicsPart>()?.Takeable == true) goods.Add(e.BlueprintName);
                }
                report.AppendLine($"== {p.Name} ({id}, tier {WorldMapAuthoring.TierAt(p.X, p.Y)}, {p.Faction})");
                report.AppendLine("  talk: " + string.Join(", ", talkers.Select(kv => kv.Key + (kv.Value > 1 ? "x" + kv.Value : ""))));
                report.AppendLine("  things: " + string.Join(", ", things.Where(kv => kv.Value <= 12).Select(kv => kv.Key + (kv.Value > 1 ? "x" + kv.Value : ""))));
                report.AppendLine("  goods: " + string.Join(", ", goods.OrderBy(g => g)));
            }

            ConcernRegistry.LoadAll();
            var problems = new List<string>();
            report.AppendLine("\nCONCERNS " + ConcernRegistry.All.Count);
            foreach (var c in ConcernRegistry.All)
            {
                bool owner = residents.TryGetValue(c.Place, out var here) && here.Contains(c.Owner);
                if (!owner) problems.Add(c.Id + ": owner " + c.Owner + " does not live in " + c.Place);
                if (c.Owner != ConcernConversation.ElderRole && !(here?.Contains(ConcernConversation.ElderRole) ?? false))
                    report.AppendLine("  note: " + c.Place + " has no Elder to name " + c.Id);
                foreach (var a in c.Approaches)
                    if (a.HasCarry && !(residents.TryGetValue(a.Carry.Place, out var there) && there.Contains(a.Carry.Recipient)))
                        problems.Add(c.Id + "/" + a.Id + ": recipient " + a.Carry.Recipient + " does not live in " + a.Carry.Place);
                report.AppendLine($"  {c.Id} ({c.Place}, tier {c.Tier}, owner {c.Owner}{(owner ? "" : " MISSING")}): " + string.Join(", ", c.Approaches.Select(a => a.Kind)));
            }
            // Every material a concern asks for must be got somewhere: sold or lying in a
            // place of the concern's tier or below.
            foreach (var c in ConcernRegistry.All)
                foreach (var a in c.Approaches)
                {
                    var needs = new List<string>();
                    if (a.HasSupply) needs.Add(a.Supply.Blueprint);
                    if (a.HasRestore && PlaceSiteCatalog.Get(c.Place, a.Restore.Site) is PlaceSiteData site)
                    {
                        needs.AddRange(site.Lasting.Materials.Select(m => m.Blueprint));
                        if (!string.IsNullOrEmpty(site.Lasting.Tool)) needs.Add(site.Lasting.Tool);
                    }
                    foreach (var bp in needs)
                    {
                        var from = WorldMapAuthoring.Places.Where(p => WorldMapAuthoring.TierAt(p.X, p.Y) <= c.Tier && sold[p.Name].Contains(bp)).Select(p => p.Name).ToList();
                        report.AppendLine($"  source {c.Id}/{a.Id} {bp}: " + (from.Count > 0 ? string.Join(", ", from) : "NONE"));
                        if (from.Count == 0) problems.Add(c.Id + "/" + a.Id + ": nobody at tier " + c.Tier + " or below sells or leaves " + bp);
                    }
                }
            string dir = Path.Combine(Application.dataPath, "..", "Library", "ProgressionBench");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "place-census.txt"), report.ToString());
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
