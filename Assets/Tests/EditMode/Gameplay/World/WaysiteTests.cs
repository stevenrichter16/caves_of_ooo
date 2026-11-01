using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Waysites (Docs/CHUNK-ACTIVITY.md): small authored situations with real verbs in wild chunks.
    /// Selection is a pure function of seed and zone; placement is atomic, never cuts a route, and
    /// never draws from the pipeline's RNG; every placed owner offers the verb its kind promises.
    /// </summary>
    public sealed class WaysiteTests
    {
        static EntityFactory factory;
        const int Seed = 64;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            LoadoutPart.Factory = factory; LootDropSystem.Factory = factory; ContainerPlacementService.Factory = factory;
        }
        [SetUp] public void Setup() { Diag.ResetAll(); Diag.SetChannel("worldgen", true); }
        [TearDown] public void TearDown() { Diag.SetChannel("worldgen", false); }

        static Zone Open(string id = "Overworld.10.11.0")
        {
            var z = new Zone(id);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) z.AddEntity(factory.CreateEntity("Grass"), x, y);
            return z;
        }
        static WaysiteKind Kind(string id, string[] rows, Dictionary<char, string> legend, params string[] notes)
            => new WaysiteKind { Id = id, Weight = 10, Notes = notes, Stamp = new StructureStamp { Name = id, Rows = rows, Legend = legend, Chance = 100 } };
        static int Recorded(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 200 }).Records.Count;

        // ════════════════ the plan ════════════════

        [Test] public void TheOrderIsAPureFunctionOfSeedAndZone()
        {
            var kinds = WaysiteCatalog.For(BiomeType.Spread);
            var a = WaysitePlan.Order(Seed, "Overworld.9.3.0", kinds, 1).Select(k => k.Id).ToList();
            var b = WaysitePlan.Order(Seed, "Overworld.9.3.0", kinds, 1).Select(k => k.Id).ToList();
            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(kinds.Count, a.Distinct().Count(), "every kind, once");
            var firsts = new HashSet<string>();
            for (int x = 2; x < 14; x++) for (int y = 0; y < 14; y++) firsts.Add(WaysitePlan.Order(Seed, WorldMap.ToZoneID(x, y, 0), kinds, 1)[0].Id);
            Assert.AreEqual(kinds.Count, firsts.Count, "across a region every kind leads somewhere");
        }

        [Test] public void ANeighboursFirstPickIsRarelyRepeated()
        {
            var kinds = WaysiteCatalog.For(BiomeType.Spread);
            int same = 0, n = 0;
            for (int x = 1; x < 19; x++) for (int y = 0; y < 19; y++)
            {
                string here = WaysitePlan.Order(Seed, WorldMap.ToZoneID(x, y, 0), kinds, 1)[0].Id;
                string west = WaysitePlan.Order(Seed, WorldMap.ToZoneID(x - 1, y, 0), kinds, 1)[0].Id;
                n++; if (here == west) same++;
            }
            Assert.Less(same, n / 8, "a walk along a row seldom meets the same site twice running");
        }

        [Test] public void BudgetsFollowTheBiomeAndTheOverwritKeepsItsEmptiness()
        {
            Assert.AreEqual(2, WaysiteCatalog.Budget(BiomeType.Spread, 7));
            Assert.AreEqual(2, WaysiteCatalog.Budget(BiomeType.Sodden, 7));
            Assert.AreEqual(1, WaysiteCatalog.Budget(BiomeType.Beating, 7));
            Assert.AreEqual(0, WaysiteCatalog.Budget(BiomeType.Overwrit, 7), "canon: the scraped region stays scraped");
            CollectionAssert.IsEmpty(WaysiteCatalog.For(BiomeType.Overwrit));
        }

        [Test] public void CuratedZonesAreNeverEligible()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            foreach (var id in new[] { "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0", ReferenceGladePlan.ZoneID })
                Assert.False(WaysitePlan.IsEligible(manager, id), id + " is the opening");
            if (manager.Wayhouse?.ZoneID != null) Assert.False(WaysitePlan.IsEligible(manager, manager.Wayhouse.ZoneID));
            var place = WorldMapAuthoring.Places.First();
            Assert.False(WaysitePlan.IsEligible(manager, WorldMap.ToZoneID(place.X, place.Y, 0)), "places have their own content");
            Assert.False(WaysitePlan.IsEligible(manager, "Overworld.10.11.1"), "surface only");
            int eligible = 0;
            for (int x = 0; x < 20; x++) for (int y = 0; y < 20; y++) if (WaysitePlan.IsEligible(manager, WorldMap.ToZoneID(x, y, 0))) eligible++;
            Assert.Greater(eligible, 250, "counter-check: ordinary wilderness is eligible");
        }

        /// <summary>Hypothesis (found by the collector, grazer and snake pipeline pins): a waysite at 3850
        /// moves the ordinary owners an exploration family composes from at 4001/4300, so the family refuses.
        /// A family's zone already has its thing to do; the waysite stays out.</summary>
        [Test] public void AnExplorationFamilysZoneIsNeverEligible()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            int owned = 0, quiet = 0;
            foreach (var entry in manager.Exploration.Entries.Where(e => e.PlacementEligible))
            {
                var (x, y, _) = WorldMap.FromZoneID(entry.ZoneID);
                if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Spread) continue;
                if (entry.Family != SpreadExplorationFamily.None) { owned++; Assert.False(WaysitePlan.IsEligible(manager, entry.ZoneID), entry.ZoneID + " " + entry.Family); }
                else if (WaysitePlan.IsEligible(manager, entry.ZoneID)) quiet++;
            }
            Assert.Greater(owned, 0, "the seed has exploration families");
            Assert.Greater(quiet, 0, "counter-check: a quiet exploration address keeps its waysite");
        }

        // ════════════════ placement ════════════════

        [Test] public void ASiteIsPlacedWholeAndRecorded()
        {
            var zone = Open();
            var kind = Kind("Camp", new[] { "f.b" }, new Dictionary<char, string> { { 'f', "Campfire" }, { 'b', "Bedroll" } });
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.AreEqual(new[] { "Camp" }, builder.Placed);
            Assert.AreEqual(1, zone.GetAllEntities().Count(e => e.BlueprintName == "Campfire"));
            Assert.AreEqual(1, zone.GetAllEntities().Count(e => e.BlueprintName == "Bedroll"));
            Assert.AreEqual(1, Recorded("WaysitePlaced"));
        }

        [Test] public void AnUnknownBlueprintRefusesTheWholeSite()
        {
            var zone = Open(); int before = zone.GetAllEntities().Count;
            var kind = Kind("Broken", new[] { "fx" }, new Dictionary<char, string> { { 'f', "Campfire" }, { 'x', "NoSuchThing" } });
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed);
            Assert.AreEqual(before, zone.GetAllEntities().Count, "nothing partial");
            Assert.AreEqual(1, Recorded("WaysiteRefused"));
        }

        [Test] public void ASiteThatWouldCutTheOnlyRouteIsUndone()
        {
            // A zone of rock with one open row: every anchor lies on the only route across.
            var zone = new Zone("Overworld.10.11.0");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                zone.AddEntity(factory.CreateEntity(y == 12 ? "Grass" : "SandstoneWall"), x, y);
            int reachable = WaysiteBuilder.Reachable(zone); Assume.That(reachable, Is.EqualTo(Zone.Width));
            var kind = Kind("Post", new[] { "h" }, new Dictionary<char, string> { { 'h', "HitchingPost" } });
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed, "a solid post in a one-cell road would split the zone");
            Assert.AreEqual(0, zone.GetAllEntities().Count(e => e.BlueprintName == "HitchingPost"), "undone exactly");
            Assert.AreEqual(Zone.Width, WaysiteBuilder.Reachable(zone));
            Assert.AreEqual(0, zone.GenReservedCells.Count, "reservations released");
        }

        [Test] public void APassableSiteOnTheSameRoadIsKept()
        {
            var zone = new Zone("Overworld.10.11.0");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                zone.AddEntity(factory.CreateEntity(y == 12 ? "Grass" : "SandstoneWall"), x, y);
            var kind = Kind("Roll", new[] { "b" }, new Dictionary<char, string> { { 'b', "Bedroll" } });
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.AreEqual(new[] { "Roll" }, builder.Placed, "counter-check: a bedroll does not block the road");
        }

        [Test] public void ThePipelinesRandomStreamIsNeverTouched()
        {
            var zone = Open(); var rng = new System.Random(99); var twin = new System.Random(99);
            new WaysiteBuilder(BiomeType.Spread, 1, Seed).BuildZone(zone, factory, rng);
            Assert.AreEqual(twin.Next(), rng.Next(), "later builders keep their own draws");
        }

        [Test] public void ReservedRoadsStairsAndWaterAreNeverBuiltOn()
        {
            var zone = Open();
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) if (x != 40) zone.GenReservedCells.Add((x, y));
            var kind = Kind("Camp", new[] { "f.b" }, new Dictionary<char, string> { { 'f', "Campfire" }, { 'b', "Bedroll" } });
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed, "no three-wide unreserved anchor exists");
        }

        // ════════════════ the shipped palettes ════════════════

        [Test] public void EveryShippedKindIsBuildableAndEveryNoteIsARealDocument()
        {
            CollectionAssert.IsEmpty(ReadableDocumentCatalog.Validate());
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
                foreach (var kind in WaysiteCatalog.For(biome))
                {
                    foreach (var kv in kind.Stamp.Legend)
                    {
                        string m = kv.Value;
                        if (m == WaysiteKind.NoteMarker || m == "interior") continue;
                        string bp = m.StartsWith("stock:") ? m.Split(':')[1] : m;
                        Assert.True(factory.Blueprints.ContainsKey(bp), kind.Id + ": " + bp);
                        if (m.StartsWith("stock:")) Assert.NotNull(LootTableRegistry.Get(m.Split(':')[2]), kind.Id + ": table " + m);
                    }
                    bool wantsNote = kind.Stamp.Legend.Values.Contains(WaysiteKind.NoteMarker);
                    Assert.AreEqual(wantsNote, kind.Notes.Length > 0, kind.Id + ": notes iff a note marker");
                    foreach (var note in kind.Notes)
                    {
                        var doc = ReadableDocumentCatalog.All.SingleOrDefault(d => d.Blueprint == note);
                        Assert.NotNull(doc, note + " has its document");
                        Assert.AreEqual(doc.Id, factory.CreateEntity(note).GetPart<ReadableDocumentPart>().DocumentId, note);
                    }
                }
        }

        static List<string> Verbs(Entity e, Entity player, Zone zone)
        {
            var list = new InventoryActionList();
            var ev = GameEvent.New("GetInventoryActions");
            ev.SetParameter("Actions", list); ev.SetParameter("Actor", (object)player); ev.SetParameter("Zone", (object)zone);
            e.FireEventAndRelease(ev);
            return list.Actions.Select(a => a.Name).ToList();
        }

        [TestCase("Bedroll", "Sleep")] [TestCase("Campfire", "Rest")] [TestCase("DroversTrough", "Draw water")]
        [TestCase("Cistern", "Draw water")] [TestCase("TokenDish", "Open")] [TestCase("WaysiteNoteDrover1", "Read")]
        public void EveryPromisedVerbIsReallyOffered(string blueprint, string verb)
        {
            var zone = Open(); var thing = factory.CreateEntity(blueprint); zone.AddEntity(thing, 20, 10);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 21, 10);
            CollectionAssert.Contains(Verbs(thing, player, zone), verb, blueprint);
        }

        [Test] public void ATroughIsOnlyDrunkFromBesideIt()
        {
            var zone = Open(); var trough = factory.CreateEntity("DroversTrough"); zone.AddEntity(trough, 20, 10);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 30, 10);
            CollectionAssert.DoesNotContain(Verbs(trough, player, zone), "Draw water", "counter-check: from across the field, no");
        }

        static bool Draw(Entity well, Entity actor, Zone zone)
        {
            var action = GameEvent.New("InventoryAction");
            try { action.SetParameter("Actor", actor); action.SetParameter("Zone", zone); action.SetParameter("Command", "DrawWaterAtWell"); action.SetParameter("Random", new System.Random(1)); well.FireEvent(action); return action.Handled; }
            finally { action.Release(); }
        }

        [Test] public void ACisternHoldsAFewDrawsThenRunsDry()
        {
            // The Beating's exposure is its point: a waysite cistern catches what little rain falls, it
            // is not a spring. Without a finite pool, half the wild Beating would have endless water.
            var zone = new Zone("Overworld.1.15.0");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) zone.AddEntity(factory.CreateEntity("Sand"), x, y);
            var cistern = factory.CreateEntity("Cistern"); zone.AddEntity(cistern, 20, 10);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 21, 10);
            for (int i = 0; i < WaysiteCatalog.CisternDraws; i++) Assert.True(Draw(cistern, player, zone), "draw " + (i + 1));
            Assert.False(Draw(cistern, player, zone), "then it is dry");
            CollectionAssert.DoesNotContain(Verbs(cistern, player, zone), "Draw water", "and offers none");
            var trough = factory.CreateEntity("DroversTrough"); zone.AddEntity(trough, 30, 10); zone.MoveEntity(player, 31, 10);
            for (int i = 0; i < 10; i++) Assert.True(Draw(trough, player, zone), "counter-check: the spring-fed trough keeps running");
        }

        [Test] public void ACisternFillsAWaterskinOnlyWithWhatItHolds()
        {
            var zone = new Zone("Overworld.1.15.0");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) zone.AddEntity(factory.CreateEntity("Sand"), x, y);
            var cistern = factory.CreateEntity("Cistern"); zone.AddEntity(cistern, 20, 10);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 21, 10);
            var skin = factory.CreateEntity("Waterskin"); player.GetPart<InventoryPart>().AddObject(skin);
            var water = skin.GetPart<WaterskinPart>(); water.Charges = 0;
            var well = cistern.GetPart<WellPart>();
            Assert.AreEqual(WaysiteCatalog.CisternDraws, well.Stock, "the authored cistern holds its draws");
            // A skin holds as much as a whole cistern, so leave less in it than the skin can take:
            // only then does a full skin prove nothing, and a short one prove the limit.
            well.Stock = water.Capacity - 1;
            Assert.True(WaterVesselService.TryAct(player, skin, zone, "FillWaterskin"));
            Assert.AreEqual(water.Capacity - 1, water.Charges, "the skin takes what the cistern holds, not its capacity");
            Assert.AreEqual(0, well.Stock, "and the cistern is dry");
            water.Charges = 0;
            Assert.False(WaterVesselService.TryAct(player, skin, zone, "FillWaterskin"), "nothing left to fill from");
            var trough = factory.CreateEntity("DroversTrough"); zone.AddEntity(trough, 30, 10); zone.MoveEntity(player, 31, 10);
            Assert.True(WaterVesselService.TryAct(player, skin, zone, "FillWaterskin"));
            Assert.AreEqual(water.Capacity, water.Charges, "counter-check: the spring-fed trough fills it");
            Assert.AreEqual(-1, trough.GetPart<WellPart>().Stock, "and stays endless");
        }

        [Test] public void ARealSpreadChunkGetsItsBudgetOfRealSites()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            int zones = 0, placed = 0;
            for (int x = 6; x < 16 && zones < 12; x++) for (int y = 0; y < 16 && zones < 12; y++)
            {
                string id = WorldMap.ToZoneID(x, y, 0);
                if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Spread || !WaysitePlan.IsEligible(manager, id)) continue;
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                manager.GetZone(id); zones++;
                placed += Recorded("WaysitePlaced");
            }
            Assert.AreEqual(12, zones);
            Assert.GreaterOrEqual(placed, 20, "two sites in nearly every Spread chunk");
        }
    }
}
