using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class EastTemperingGroundTests
    {
        const string East = "Overworld.12.10.0", GroundRole = "yard-ground";
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        // Independent design contract; do not read production's footprint array.
        static readonly (int x, int y)[] Pad = {
            (0,-2), (1,-2), (-2,-1), (-1,-1), (0,-1), (1,-1),
            (-3,0), (-2,0), (-1,0), (-2,1), (-1,1), (0,1)
        };
        static readonly (int x, int y)[] Functional = {
            (0,0), (2,1), (-2,-2), (-1,-2), (2,-2), (2,-1), (-1,1)
        };

        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone;
            internal readonly SpreadCompositionBuilder Terrain = new SpreadCompositionBuilder(64) { FormationOverride = Formation.OldRoad };
            internal readonly PopulationBuilder Population;
            internal readonly int Rotation;
            internal Entity[] Owners;
            internal Func<bool> Final;

            internal Fixture(int count = 1, int rotation = 0, string id = East)
            {
                Rotation = rotation; Scope.Seed(64); Zone = new Zone(id);
                Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                // Controlled substrate precedes real population capture. Native
                // world/seed coverage remains in the existing generation fixtures.
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => e.BlueprintName != "Grass").ToArray()) Zone.RemoveEntity(e);
                Zone.ForEachCell((cell, x, y) => {
                    if (cell.Objects.Count == 0) Assert.True(Zone.AddEntity(Factory.CreateEntity("Grass"), x, y));
                });
                Zone.GenReservedCells.Clear();
                Population = new PopulationBuilder(new PopulationTable {
                    Name = "SpreadTier1", Entries = new List<PopulationEntry> {
                        new PopulationEntry { BlueprintName = "Viper", EncounterGroup = "SpreadTier1Encounter", MinCount = count, MaxCount = count }
                    }
                }) {
                    CaptureSourceReceipts = true,
                    HabitatFilter = (bp, cell) => cell.Y == 5 && (cell.X == 60 || cell.X == 65)
                };
                Assert.True(Population.BuildZone(Zone, Factory, new Random(17)));
                Assert.AreEqual(count, Population.SourceReceipt.Owners.Count);
                Assert.True(Population.SourceReceipt.IsCurrent);
                // These exact allowed cells admit one service origin/orientation.
                // Reservations do not block movement or manufacture a safe route.
                var allowed = new HashSet<(int x, int y)>(Pad.Concat(Functional).Select(p => At(p.x, p.y)));
                foreach (var source in Population.SourceReceipt.Owners) allowed.Add(Zone.GetEntityPosition(source));
                Zone.ForEachCell((cell, x, y) => { if (!allowed.Contains((x, y))) Zone.GenReservedCells.Add((x, y)); });
            }
            internal (int x, int y) At(int x, int y) => Rotation == 0 ? (20 + x, 12 + y) : (20 - y, 12 + x);
            internal bool Place(Func<bool> authority = null, EntityFactory factory = null, SpreadExplorationFamily family = SpreadExplorationFamily.TemperingShelter)
                => SpreadExplorationWorksites.TryPlace(Zone, factory ?? Factory, Terrain, Population, null, family,
                    authority ?? (() => true), out Owners, out Final);
            internal Entity[] Ground => Owners.Where(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == GroundRole).ToArray();
            internal Func<bool> Proof(IEnumerable<Entity> owners) => (Func<bool>)typeof(SpreadGenerationReceipt)
                .GetMethod("CaptureFinalState", All).Invoke(null, new object[] { Zone, owners });
            internal void ObserveGround(Action<Entity> callback)
            {
                Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                Factory.Blueprints["RoadStone"].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = callback;
            }
            public void Dispose() { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null; Scope.Dispose(); }
        }

        static string[] FunctionalShape(Fixture f) => f.Owners.Where(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) != GroundRole)
            .Select(e => e.BlueprintName + ":" + e.GetProperty(SpreadExplorationWorksites.RoleKey) + ":" + f.Zone.GetEntityPosition(e)
                + ":" + string.Join(",", DensityLootTestScope.Gear(e).Select(i => i.BlueprintName + "*" + (i.GetPart<StackerPart>()?.StackCount ?? 1)).OrderBy(x => x)))
            .OrderBy(s => s).ToArray();
        static HashSet<(int x, int y)> Reach(Zone z)
        {
            var reached = new HashSet<(int x, int y)>(); var queue = new Queue<(int x, int y)>();
            void Add(int x, int y)
            {
                var c = z.GetCell(x, y);
                if (c == null || c.BlocksMovement() || z.TileState.Get(x, y)?.IsEmpty == false
                    || c.Objects.Any(e => e.HasTag("Creature") || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>())) return;
                if (reached.Add((x, y))) queue.Enqueue((x, y));
            }
            Add(40, 12);
            while (queue.Count > 0) { var p = queue.Dequeue(); Add(p.x - 1, p.y); Add(p.x + 1, p.y); Add(p.x, p.y - 1); Add(p.x, p.y + 1); }
            return reached;
        }

        [TestCase(1, 0)] [TestCase(1, 1)] [TestCase(2, 0)] [TestCase(2, 1)]
        public void ExactSmallPadPreservesOwnersResourcesBudgetAndFourPhysicalRoutes(int count, int rotation)
        {
            using (var f = new Fixture(count, rotation))
            {
                var sources = f.Population.SourceReceipt.Owners.ToArray();
                var old = f.Zone.GetReadOnlyEntities().ToArray();
                var preserved = count == 1 ? old : old.Except(sources).ToArray(); var proof = f.Proof(preserved);
                var beforeReach = Reach(f.Zone);
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof());
                var expected = Pad.Where(p => count != 2 || p != (-1, 1)).Select(p => f.At(p.x, p.y)).ToArray();
                CollectionAssert.AreEquivalent(expected, f.Ground.Select(f.Zone.GetEntityPosition));
                Assert.AreEqual(count == 1 ? 12 : 11, f.Ground.Length);
                Assert.AreEqual(old.Length + (count == 1 ? 17 : 16), f.Zone.EntityCount);
                Assert.AreEqual(count, f.Zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
                Assert.AreEqual(f.At(0, 0), f.Zone.GetEntityPosition(f.Owners.Single(e => e.BlueprintName == "TinkersForge")));
                var forage = f.Owners.Single(e => e.BlueprintName == "StoneburrPatch");
                Assert.AreEqual(f.At(2, 1), f.Zone.GetEntityPosition(forage));
                var harvest = forage.GetPart<HarvestablePart>(); Assert.False(harvest.Harvested);
                Assert.AreEqual("StoneburrSeed", harvest.YieldBlueprint); Assert.AreEqual(100, harvest.YieldChance);
                Assert.That(harvest.YieldMin, Is.InRange(1, 2)); Assert.That(harvest.YieldMax, Is.InRange(harvest.YieldMin, 2));
                CollectionAssert.AreEquivalent(new[] { f.At(-2, -2), f.At(-1, -2), f.At(2, -2) },
                    f.Owners.Where(e => e.BlueprintName == "StoneWall").Select(f.Zone.GetEntityPosition));
                if (count == 1) Assert.True(sources.All(e => f.Zone.GetEntityCell(e) != null));
                else {
                    Assert.True(sources.All(e => f.Zone.GetEntityCell(e) == null));
                    Assert.AreEqual(f.At(-1, 1), f.Zone.GetEntityPosition(f.Owners.Single(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "melee")));
                    var caster = f.Owners.Single(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "ranged");
                    Assert.AreEqual(f.At(2, -1), f.Zone.GetEntityPosition(caster)); Assert.True(caster.GetPart<CombatTacticsPart>().RepositionForShot);
                }
                for (int y = 0; y <= 2; y++) for (int x = 1; x <= 3; x++)
                    Assert.False(f.Ground.Any(e => f.Zone.GetEntityPosition(e) == f.At(x, y)), "The full forage margin stays unpaved.");
                foreach (var floor in f.Ground) {
                    Assert.AreEqual("RoadStone", floor.BlueprintName); Assert.False(floor.GetPart<PhysicsPart>().Solid); Assert.False(floor.GetPart<PhysicsPart>().Takeable);
                    var cell = f.Zone.GetEntityCell(floor); Assert.AreEqual(2, cell.Objects.Count);
                    var grass = cell.Objects.Single(e => e != floor); Assert.AreEqual("Grass", grass.BlueprintName); Assert.Contains(grass, old);
                    Assert.False(cell.IsInterior); Assert.False(f.Zone.GenReservedCells.Contains((cell.X, cell.Y)));
                }
                var reached = Reach(f.Zone); var plan = f.Terrain.Plan;
                foreach (var port in new[] { (0, plan.WestY), (Zone.Width - 1, plan.EastY), (plan.NorthX, 0), (plan.SouthX, Zone.Height - 1) })
                { Assert.True(beforeReach.Contains(port)); Assert.True(reached.Contains(port)); }
                foreach (var service in f.Owners.Where(e => e.HasPart<ForgePart>() || e.HasPart<HarvestablePart>())) {
                    var p = f.Zone.GetEntityPosition(service);
                    Assert.GreaterOrEqual(new[] { (p.x - 1, p.y), (p.x + 1, p.y), (p.x, p.y - 1), (p.x, p.y + 1) }.Count(reached.Contains), 2);
                }
                Assert.True(f.Ground.All(e => reached.Contains(f.Zone.GetEntityPosition(e))));
            }
        }

        [TestCase(1)] [TestCase(2)]
        public void MissingOptionalRoadRetainsExactServicePositionsAndLoadouts(int count)
        {
            string[] baseline; int nextLoadoutRoll;
            using (var f = new Fixture(count)) {
                f.Factory.Blueprints.Remove("RoadStone"); Assert.True(f.Place()); Assert.True(f.Final()); Assert.IsEmpty(f.Ground);
                baseline = FunctionalShape(f); nextLoadoutRoll = LoadoutPart.Rng.Next();
            }
            using (var f = new Fixture(count)) {
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.IsNotEmpty(f.Ground);
                CollectionAssert.AreEqual(baseline, FunctionalShape(f));
                Assert.AreEqual(nextLoadoutRoll, LoadoutPart.Rng.Next(), "Optional paving must not reroll the retained functional packet.");
            }
        }

        [TestCase(1)] [TestCase(2)]
        public void NoEligibleGroundStillCommitsTheExactUnpavedFunctionalPacket(int count)
        {
            string[] baseline; int nextLoadoutRoll;
            using (var f = new Fixture(count)) {
                f.Factory.Blueprints.Remove("RoadStone"); Assert.True(f.Place()); Assert.True(f.Final());
                baseline = FunctionalShape(f); nextLoadoutRoll = LoadoutPart.Rng.Next();
            }
            using (var f = new Fixture(count)) {
                foreach (var p in Pad.Select(p => f.At(p.x, p.y)))
                    f.Zone.GetCell(p.x, p.y).Objects.Single().AddPart(new CultivatedSoilPart());
                var preserved = f.Zone.GetReadOnlyEntities().Except(count == 2 ? f.Population.SourceReceipt.Owners : Array.Empty<Entity>()).ToArray();
                var proof = f.Proof(preserved);
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.IsEmpty(f.Ground); Assert.True(proof());
                CollectionAssert.AreEqual(baseline, FunctionalShape(f));
                Assert.AreEqual(nextLoadoutRoll, LoadoutPart.Rng.Next());
                Assert.AreEqual(count, f.Zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
            }
        }

        [TestCase("Overworld.15.10.0")] [TestCase("Overworld.12.11.0")]
        public void OtherForgeAddressesKeepTheirExistingServiceWithoutThisPad(string id)
        { using (var f = new Fixture(id: id)) { Assert.True(f.Place()); Assert.True(f.Final()); Assert.IsEmpty(f.Ground); } }

        [Test]
        public void EastAddressAloneDoesNotPaveADifferentWorksiteFamily()
        {
            using (var f = new Fixture()) {
                Assert.True(f.Place(family: SpreadExplorationFamily.FieldAlembic)); Assert.True(f.Final()); Assert.IsEmpty(f.Ground);
                Assert.AreEqual(1, f.Owners.Count(e => e.HasPart<AlchemyStillPart>()));
            }
        }

        [TestCase("soil")] [TestCase("claimed")] [TestCase("reserved")] [TestCase("interior")] [TestCase("coating")]
        [TestCase("barren")] [TestCase("mixed")] [TestCase("empty")] [TestCase("road")] [TestCase("crop")]
        public void OneIneligibleSubstrateIsSkippedWithoutMovingTheAcceptedForge(string fault)
        {
            using (var f = new Fixture())
            {
                var p = f.At(0, -2); var cell = f.Zone.GetCell(p.x, p.y); var grass = cell.Objects.Single();
                if (fault == "soil") grass.AddPart(new CultivatedSoilPart());
                if (fault == "claimed") grass.Tags["Owned"] = "";
                if (fault == "reserved") f.Zone.GenReservedCells.Add(p);
                if (fault == "interior") cell.IsInterior = true;
                if (fault == "coating") f.Zone.TileState.WriteCoating(p.x, p.y, "water", 5);
                if (fault == "barren") grass.Tags["Barren"] = "";
                if (fault == "mixed") Assert.True(f.Zone.AddEntity(f.Factory.CreateEntity("Grass"), p.x, p.y));
                if (fault == "empty" || fault == "road") Assert.True(f.Zone.RemoveEntity(grass));
                if (fault == "road") Assert.True(f.Zone.AddEntity(f.Factory.CreateEntity("RoadStone"), p.x, p.y));
                if (fault == "crop") {
                    var crop = f.Factory.CreateEntity("Grass"); crop.Tags.Remove("Terrain"); crop.AddPart(new CropPart());
                    Assert.True(f.Zone.AddEntity(crop, p.x, p.y));
                }
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old);
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof());
                Assert.AreEqual(f.At(0, 0), f.Zone.GetEntityPosition(f.Owners.Single(e => e.HasPart<ForgePart>())));
                CollectionAssert.AreEquivalent(Pad.Select(o => f.At(o.x, o.y)).Where(o => o != p), f.Ground.Select(f.Zone.GetEntityPosition));
                Assert.AreEqual(11, f.Ground.Length);
            }
        }

        [TestCase("authority")] [TestCase("capture")] [TestCase("factory")] [TestCase("source-health")]
        public void AVisibleSourceActorIsNotEnoughWithoutCurrentAuthority(string fault)
        {
            using (var f = new Fixture())
            {
                if (fault == "capture") f.Population.CaptureSourceReceipts = false;
                if (fault == "source-health") f.Population.SourceReceipt.Owners.Single().GetStat("Hitpoints").BaseValue--;
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old);
                Assert.False(f.Place(() => fault != "authority", fault == "factory" ? new EntityFactory() : null));
                Assert.True(proof()); CollectionAssert.AreEquivalent(old, f.Zone.GetReadOnlyEntities()); Assert.Null(f.Owners); Assert.Null(f.Final);
            }
        }

        static void Corrupt(Entity e, string fault)
        {
            if (fault == "solid") e.GetPart<PhysicsPart>().Solid = true;
            if (fault == "takeable") e.GetPart<PhysicsPart>().Takeable = true;
            if (fault == "harvest") e.AddPart(new HarvestablePart { YieldBlueprint = "StoneburrSeed", YieldChance = 100, YieldMin = 1, YieldMax = 1 });
            if (fault == "liquid") e.AddPart(new LiquidPoolPart());
            if (fault == "stairs") e.AddPart(new StairsDownPart());
            if (fault == "shared-physics") e.GetPart<PhysicsPart>().ParentEntity = new Entity();
            if (fault == "terrain") e.Tags.Remove("Terrain");
        }

        [TestCase("solid")] [TestCase("takeable")] [TestCase("harvest")] [TestCase("liquid")]
        [TestCase("stairs")] [TestCase("shared-physics")] [TestCase("terrain")]
        public void LaterFloorFactoryCannotRedefineAnEarlierStagedPayload(string fault)
        {
            using (var f = new Fixture(2))
            {
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old); Entity first = null; int calls = 0;
                f.ObserveGround(e => { calls++; if (first == null) first = e; else Corrupt(first, fault); });
                Assert.False(f.Place()); Assert.Greater(calls, 1, "The actual later floor factory must execute the mutation.");
                Assert.True(proof()); CollectionAssert.AreEquivalent(old, f.Zone.GetReadOnlyEntities());
                Assert.True(f.Population.SourceReceipt.IsCurrent); Assert.Null(f.Owners); Assert.Null(f.Final);
            }
        }

        [Test]
        public void HarmlessFactoryObservationKeepsAllElevenTwoSourceFloors()
        {
            using (var f = new Fixture(2)) {
                var observed = new List<Entity>(); f.ObserveGround(observed.Add);
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.AreEqual(11, observed.Count);
                CollectionAssert.AreEqual(observed, f.Ground);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void AuthorityLossAfterActualGroundPlacementRollsBackOnlyThisPacket(bool late)
        {
            using (var f = new Fixture(2))
            {
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old); bool revoked = false;
                bool Authority() {
                    if (late && f.Zone.GetReadOnlyEntities().Any(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == GroundRole)) revoked = true;
                    return !revoked;
                }
                Assert.AreEqual(!late, f.Place(Authority));
                if (!late) { Assert.True(f.Final()); Assert.AreEqual(11, f.Ground.Length); return; }
                Assert.True(revoked, "Loss must occur after a real floor enters the zone, not before preflight.");
                Assert.True(proof()); CollectionAssert.AreEquivalent(old, f.Zone.GetReadOnlyEntities()); Assert.Null(f.Owners); Assert.Null(f.Final);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void LateIndependentCoatingRejectsFinalGroundWithoutErasingTheCoating(bool coat)
        {
            using (var f = new Fixture(2))
            {
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old);
                bool observed = false; int mostFloors = 0; (int x, int y) changed = default;
                bool Authority() {
                    var floors = f.Zone.GetReadOnlyEntities().Where(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == GroundRole).ToArray();
                    mostFloors = Math.Max(mostFloors, floors.Length);
                    if (!observed && floors.Length > 0) {
                        observed = true; changed = f.Zone.GetEntityPosition(floors[0]);
                        if (coat) f.Zone.TileState.WriteCoating(changed.x, changed.y, "water", 5);
                    }
                    return true; // Source authority remains valid; the final substrate gate must reject the changed cell.
                }
                Assert.AreEqual(!coat, f.Place(Authority)); Assert.True(observed);
                Assert.AreEqual(11, mostFloors, "Exercise the late final gate after the entire floor packet is placed.");
                if (!coat) { Assert.True(f.Final()); Assert.AreEqual(11, f.Ground.Length); Assert.False(f.Zone.TileState.HasCoating(changed.x, changed.y, "water")); return; }
                Assert.True(proof()); CollectionAssert.AreEquivalent(old, f.Zone.GetReadOnlyEntities());
                Assert.AreEqual(5, f.Zone.TileState.CoatingTurns(changed.x, changed.y, "water"));
                Assert.Null(f.Owners); Assert.Null(f.Final);
                Assert.False(f.Zone.GetReadOnlyEntities().Any(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == GroundRole));
            }
        }

        [TestCase("floor")] [TestCase("substrate")] [TestCase("reserved")] [TestCase("coating")] [TestCase("solid")]
        public void FinalReceiptRejectsChangedGroundOrSubstrate(string fault)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.AreEqual(12, f.Ground.Length);
                var floor = f.Ground[0]; var cell = f.Zone.GetEntityCell(floor);
                if (fault == "floor") Assert.True(f.Zone.RemoveEntity(floor));
                if (fault == "substrate") cell.Objects.Single(e => e != floor).AddPart(new CultivatedSoilPart());
                if (fault == "reserved") f.Zone.GenReservedCells.Add((cell.X, cell.Y));
                if (fault == "coating") f.Zone.TileState.WriteCoating(cell.X, cell.Y, "water", 5);
                if (fault == "solid") floor.GetPart<PhysicsPart>().Solid = true;
                Assert.False(f.Final());
            }
        }
    }
}
