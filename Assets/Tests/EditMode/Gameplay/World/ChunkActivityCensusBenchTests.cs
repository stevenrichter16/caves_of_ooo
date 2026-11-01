using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Chunk activity census (Docs/CHUNK-ACTIVITY.md): for every surface zone of seed 64, what a
    /// player can actually do there, measured the way the game offers it. Every visible
    /// non-terrain owner is asked for its world actions (WorldInteractionSystem.GatherActions,
    /// the 'interact' menu's own query) with a player as the actor.
    ///
    /// <para>Counts per zone: owners, actionable owners (one offered verb beyond look, examine and
    /// pick up), distinct verbs, hostiles, talkers, harvests, containers, stations, readables,
    /// hazards, ground items, scenery. Writes Library/ArtCensus/chunk-activity.txt with biome
    /// means, the opening as benchmark, and the barest zones. Explicit: it generates the world.</para>
    /// </summary>
    [Explicit("whole-world census; run deliberately")]
    public sealed class ChunkActivityCensusBenchTests
    {
        const int Seed = 64;
        static readonly HashSet<string> Generic = new HashSet<string> { "Look", "Examine", "Get", "Take", "PickUp", "Pickup", "Inspect", "Read description" };
        /// <summary>Verbs every solid or heavy thing offers; they change the space, not what the chunk is for.</summary>
        static readonly HashSet<string> Structural = new HashSet<string> { "Break", "Haul", "Throw", "Door" };
        static readonly HashSet<string> Opening = new HashSet<string> { "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0", "Overworld.11.10.0" };

        sealed class Row
        {
            public string Id, Biome, Kind, Formation, Family = "None";
            public int Purposeful;
            public HashSet<string> PurposeVerbs = new HashSet<string>();
            public int Owners, Actionable, Hostiles, Talkers, Harvests, Containers, Stations, Readables, Hazards, Items, Scenery;
            public HashSet<string> Verbs = new HashSet<string>(), Things = new HashSet<string>();
        }

        static EntityFactory LoadFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            LoadoutPart.Factory = f; LootDropSystem.Factory = f; ContainerPlacementService.Factory = f; TraderPart.Factory = f;
            return f;
        }

        /// <summary>The object's world actions as a player standing beside it is offered them: the
        /// census player is moved to a free neighbouring cell (or the object's own), and the query
        /// carries the zone, as the interact menu's does. Parts that need nearness answer honestly.</summary>
        static List<InventoryAction> ActionsBeside(Entity e, Entity player, Zone zone)
        {
            var at = zone.GetEntityCell(e);
            if (at != null)
            {
                Cell stand = null;
                for (int dy = -1; dy <= 1 && stand == null; dy++) for (int dx = -1; dx <= 1 && stand == null; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var c = zone.GetCell(at.X + dx, at.Y + dy);
                    if (c != null && !c.BlocksMovement(player)) stand = c;
                }
                stand = stand ?? at;
                if (zone.GetEntityCell(player) == null) zone.AddEntity(player, stand.X, stand.Y);
                else zone.MoveEntity(player, stand.X, stand.Y);
            }
            var list = new InventoryActionList();
            var ev = GameEvent.New("GetInventoryActions");
            ev.SetParameter("Actions", list); ev.SetParameter("Actor", (object)player); ev.SetParameter("Zone", (object)zone);
            e.FireEventAndRelease(ev);
            return list.Actions;
        }

        static bool Station(Entity e)
            => e.HasPart<WellPart>() || e.HasPart<CampfirePart>() || e.HasPart<SanctuaryPart>() || e.HasPart<ForgePart>()
               || e.HasPart<AlchemyStillPart>() || e.HasPart<StairsDownPart>() || e.HasPart<StairsUpPart>() || e.HasPart<BedPart>();

        [Test] public void WhatEachChunkOffersAPlayer()
        {
            var factory = LoadFactory();
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);   // as a fresh game does
            var player = factory.CreateEntity("Player");
            var rows = new List<Row>();
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
            {
                if (!WorldMapAuthoring.InBounds(x, y)) continue;
                string id = WorldMap.ToZoneID(x, y, 0);
                Zone zone;
                try { zone = manager.GetZone(id); } catch { continue; }
                if (zone == null) continue;
                manager.SetActiveZone(zone);
                var poi = manager.WorldMap.GetPOI(x, y);
                var place = WorldMapAuthoring.PlaceAt(x, y);
                var row = new Row
                {
                    Id = id, Biome = WorldMapAuthoring.BiomeAt(x, y).ToString(),
                    Kind = Opening.Contains(id) ? "opening" : place.HasValue ? "place" : poi != null ? poi.Type.ToString().ToLowerInvariant() : "wild",
                    // The selector's formation; a composition override (rare: exploration fixtures) is not seen here.
                    Formation = (WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump
                        ? FormationSelector.ForStump(StumpBands.BandAt(x, y), id) : FormationSelector.For(WorldMapAuthoring.BiomeAt(x, y), id)).ToString()
                };
                if (manager.Exploration != null && manager.Exploration.TryGetPlacement(manager, id, out var entry)) row.Family = entry.Family.ToString();
                foreach (var e in zone.GetAllEntities())
                {
                    var r = e.GetPart<RenderPart>();
                    // Terrain is ground, except what a player uses: a campfire inherits Terrain.
                    if (ReferenceEquals(e, player) || r == null || !r.Visible
                        || e.HasTag("Terrain") && !e.HasPart<HarvestablePart>() && !e.HasPart<LiquidPoolPart>() && !Station(e)) continue;
                    row.Owners++;
                    var p = e.GetPart<PhysicsPart>();
                    bool item = p != null && p.Takeable && p.InInventory == null;
                    if (e.HasTag("Creature"))
                    {
                        if (e.GetPart<ConversationPart>() != null) row.Talkers++;
                        else if (FactionManager.IsHostile(e, player)) row.Hostiles++;
                    }
                    if (e.HasPart<HarvestablePart>() || e.HasPart<FieldHarvestPart>() || e.HasPart<CropPart>()) row.Harvests++;
                    if (e.HasPart<ContainerPart>() && !e.HasTag("Creature")) row.Containers++;
                    if (Station(e)) row.Stations++;
                    if (e.HasPart<RegionalSignpostPart>() || e.HasPart<ReadableDocumentPart>()) row.Readables++;
                    if (e.HasPart<LiquidPoolPart>() || e.HasPart<TriggerOnStepPart>() || e.HasPart<GasPoolPart>()) row.Hazards++;
                    if (item) row.Items++;
                    var actions = ActionsBeside(e, player, zone);
                    bool actionable = false, purposeful = false;
                    foreach (var a in actions)
                    {
                        if (a == null || string.IsNullOrEmpty(a.Name) || Generic.Contains(a.Name)) continue;
                        actionable = true; row.Verbs.Add(a.Name);
                        if (!Structural.Contains(a.Name)) { purposeful = true; row.PurposeVerbs.Add(a.Name); }
                    }
                    if (actionable && !item) row.Actionable++;
                    if (purposeful && !item) { row.Purposeful++; row.Things.Add(e.BlueprintName); }
                    if (!actionable && !item && !e.HasTag("Creature")) row.Scenery++;
                }
                if (zone.GetEntityCell(player) != null) zone.RemoveEntity(player);
                rows.Add(row);
            }

            var report = new StringBuilder($"CHUNK ACTIVITY seed {Seed}: {rows.Count} surface zones\n\n");
            report.AppendLine("group                 zones owners actionable purposeful pverbs hostiles talkers harvests containers stations readables hazards items scenery  zero-purposeful");
            void Line(string name, List<Row> g)
            {
                if (g.Count == 0) return;
                double M(System.Func<Row, double> f) => g.Average(f);
                int zero = g.Count(r => r.Purposeful == 0);
                report.AppendLine($"{name,-21} {g.Count,5} {M(r => r.Owners),6:F0} {M(r => r.Actionable),10:F1} {M(r => r.Purposeful),10:F1} {M(r => r.PurposeVerbs.Count),6:F1} {M(r => r.Hostiles),8:F1} {M(r => r.Talkers),7:F1} {M(r => r.Harvests),8:F1} {M(r => r.Containers),10:F1} {M(r => r.Stations),8:F1} {M(r => r.Readables),9:F1} {M(r => r.Hazards),7:F1} {M(r => r.Items),5:F1} {M(r => r.Scenery),7:F0}  {zero}");
            }
            Line("OPENING (benchmark)", rows.Where(r => r.Kind == "opening").ToList());
            foreach (var biome in rows.Select(r => r.Biome).Distinct().OrderBy(b => b))
            {
                Line(biome + " wild", rows.Where(r => r.Biome == biome && r.Kind == "wild").ToList());
                Line(biome + " sites", rows.Where(r => r.Biome == biome && r.Kind != "wild" && r.Kind != "place" && r.Kind != "opening").ToList());
                Line(biome + " places", rows.Where(r => r.Biome == biome && r.Kind == "place").ToList());
            }
            report.AppendLine("\nWILD BY FORMATION (owners: mean, median, min; zones under 60):");
            foreach (var g in rows.Where(r => r.Kind == "wild").GroupBy(r => r.Biome + " " + r.Formation).OrderBy(g => g.Key))
            {
                var o = g.Select(r => r.Owners).OrderBy(v => v).ToList();
                report.AppendLine($"  {g.Key,-28} zones={o.Count,3} mean={o.Average(),6:F0} median={o[o.Count / 2],4} min={o[0],4} under60={o.Count(v => v < 60),3} purposeful={g.Average(r => r.Purposeful),5:F1} scenery={g.Average(r => r.Scenery),5:F0} explorationFamilies={g.Count(r => r.Family != "None"),3}");
            }
            report.AppendLine("\nVERBS ANYWHERE: " + string.Join(", ", rows.SelectMany(r => r.Verbs).GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())));
            foreach (var biome in rows.Select(r => r.Biome).Distinct().OrderBy(b => b))
            {
                var wild = rows.Where(r => r.Biome == biome && r.Kind == "wild").ToList();
                if (wild.Count == 0) continue;
                report.AppendLine($"\n{biome} wild purposeful verbs (zones offering): " + string.Join(", ", wild.SelectMany(r => r.PurposeVerbs)
                    .GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())));
                report.AppendLine($"{biome} wild purposeful owners: " + string.Join(", ", wild.SelectMany(r => r.Things)
                    .GroupBy(v => v).OrderByDescending(g => g.Count()).Take(25).Select(g => g.Key + " " + g.Count())));
            }
            report.AppendLine("\nBAREST WILD ZONES (fewest purposeful owners, then purposeful verbs):");
            foreach (var r in rows.Where(r => r.Kind == "wild").OrderBy(r => r.Purposeful).ThenBy(r => r.PurposeVerbs.Count).Take(40))
                report.AppendLine($"  {r.Id,-20} {r.Biome,-10} owners={r.Owners} purposeful={r.Purposeful} pverbs={r.PurposeVerbs.Count} hostiles={r.Hostiles} things={string.Join(" ", r.Things.Take(8))}");
            report.AppendLine("\nPER-ZONE (wild):");
            foreach (var r in rows.Where(r => r.Kind == "wild").OrderBy(r => r.Biome).ThenBy(r => r.Id))
                report.AppendLine($"  {r.Id,-20} {r.Biome,-10} {r.Formation,-14} {r.Family,-16} owners={r.Owners} purposeful={r.Purposeful} pverbs={string.Join("/", r.PurposeVerbs)} hostiles={r.Hostiles} harvests={r.Harvests} containers={r.Containers} stations={r.Stations} readables={r.Readables} hazards={r.Hazards} things={string.Join(" ", r.Things.Take(10))}");
            string dir = Path.Combine(Application.dataPath, "..", "Library", "ArtCensus");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "chunk-activity.txt"), report.ToString());
            TestContext.WriteLine(report.ToString().Substring(0, System.Math.Min(4000, report.Length)));
            Assert.Greater(rows.Count, 300, "the census walked the surface");
        }
    }
}
