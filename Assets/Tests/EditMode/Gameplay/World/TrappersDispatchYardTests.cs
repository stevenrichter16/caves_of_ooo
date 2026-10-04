using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class TrappersDispatchYardTests
    {
        const string RoleKey = "SpreadWorksite.Role";
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        sealed class StockRoll : Random
        {
            public override int Next(int max) => max > 1 ? 1 : 0;
            public override int Next(int min, int max) => min;
            public override double NextDouble() => .1;
        }
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone = new Zone("Overworld.11.11.0");
            internal readonly SpreadCompositionBuilder Terrain = new SpreadCompositionBuilder(64) { FormationOverride = Formation.OldRoad };
            internal readonly PopulationBuilder Population;
            internal readonly ContainerBuilder Containers = new ContainerBuilder(BiomeType.Spread, 1, ContainerPlacementService.ZoneKind.Wilderness) { CaptureSourceReceipts = true };
            internal Entity[] Owners;
            internal Func<bool> Final;
            internal Fixture(int count = 2)
            {
                Scope.Seed(64);
                Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                // Controlled bare ground for geometry assertions. Native seed cases below
                // separately require unedited ordinary generation to accept the yard.
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => !(bool)typeof(DoorPart).GetMethod("IsBareGround", All).Invoke(null, new object[] { e })).ToArray()) Zone.RemoveEntity(e);
                Zone.GenReservedCells.Clear();
                Population = new PopulationBuilder(new PopulationTable { Name = "SpreadTier1", Entries = new List<PopulationEntry> {
                    new PopulationEntry { BlueprintName = "Viper", EncounterGroup = "SpreadTier1Encounter", MinCount = count, MaxCount = count } } }) { CaptureSourceReceipts = true };
                Assert.True(Population.BuildZone(Zone, Factory, new Random(17)));
                Assert.True(Containers.BuildZone(Zone, Factory, new StockRoll()));
                Assert.True(Population.SourceReceipt.IsCurrent);
                Assert.True(Containers.SourceReceipt.IsCurrent);
            }
            internal bool Place(string family = "TrappersStore", Func<bool> authority = null)
                => SpreadExplorationWorksites.TryPlace(Zone, Factory, Terrain, Population, Containers,
                    (SpreadExplorationFamily)Enum.Parse(typeof(SpreadExplorationFamily), family), authority ?? (() => true), out Owners, out Final);
            internal Entity Role(string role)
            {
                var result = Owners?.SingleOrDefault(e => e.GetProperty(RoleKey) == role);
                Assert.NotNull(result, "The generated yard must own its real " + role + ".");
                return result;
            }
            internal Entity Cache => Owners.Single(e => e.HasPart<ContainerPart>());
            internal Func<bool> Proof(IEnumerable<Entity> owners) => (Func<bool>)typeof(SpreadGenerationReceipt)
                .GetMethod("CaptureFinalState", All).Invoke(null, new object[] { Zone, owners });
            public void Dispose() => Scope.Dispose();
        }

        static HashSet<(int x, int y)> Reach(Zone zone, (int x, int y) start, Entity safeTrap = null, Entity ignoredBeam = null, (int x, int y)? parked = null, bool diagonals = true)
        {
            var reached = new HashSet<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();
            bool Walk(int x, int y)
            {
                var cell = zone.GetCell(x, y);
                if (cell == null || parked == (x, y)) return false;
                foreach (var e in cell.Objects)
                {
                    if (ReferenceEquals(e, ignoredBeam) || e.HasTag("Creature")) continue;
                    if (e.GetPart<PhysicsPart>()?.Solid == true || e.HasTag("Solid") || e.GetPart<DoorPart>()?.IsClosed == true) return false;
                    if (e.HasPart<TriggerOnStepPart>() && !ReferenceEquals(e, safeTrap)) return false;
                }
                return true;
            }
            void Add(int x, int y) { if (Walk(x, y) && reached.Add((x, y))) queue.Enqueue((x, y)); }
            Add(start.x, start.y);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && (diagonals || dx == 0 || dy == 0)) Add(p.x + dx, p.y + dy);
            }
            return reached;
        }
        static bool CanTouch(HashSet<(int x, int y)> reachable, (int x, int y) owner)
            => reachable.Any(p => Math.Max(Math.Abs(p.x - owner.x), Math.Abs(p.y - owner.y)) <= 1);
        static Entity Hauler(Zone zone, (int x, int y) at, int strength)
        {
            var e = new Entity { ID = "yard-hauler", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new PhysicsPart { Takeable = false });
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = strength, Min = 0, Max = 100 };
            e.Statistics["Speed"] = new Stat { Owner = e, Name = "Speed", BaseValue = 100, Min = 0, Max = 200 };
            Assert.True(zone.AddEntity(e, at.x, at.y));
            return e;
        }

        [Test]
        public void ClosedYardLeavesShallowForageReachableButCannotLootDeepStockEvenDiagonally()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place());
                var reach = Reach(f.Zone, (40, 12));
                Assert.True(CanTouch(reach, f.Zone.GetEntityPosition(f.Role("cold-forage"))));
                Assert.False(CanTouch(reach, f.Zone.GetEntityPosition(f.Cache)), "The old free walk-around must not silently erase the new material/hauling choice.");
                Assert.True(f.Final());
            }
        }

        [TestCase("jam")]
        [TestCase("haul")]
        public void EachEntranceIndependentlyExposesTheSameOriginalFiniteStock(string route)
        {
            using (var f = new Fixture())
            {
                var original = f.Containers.SourceReceipt.Owners.ToArray();
                var stock = original.SelectMany(e => e.GetPart<ContainerPart>().Contents).ToArray();
                Assert.IsNotEmpty(stock, "Controlled source actually contains goods.");
                Assert.True(f.Place());
                var beam = f.Role("haul-bypass"); var trap = f.Role("trap");
                var b = f.Zone.GetEntityPosition(beam); var c = f.Zone.GetEntityPosition(f.Cache);
                var parked = (x: b.x + 2 * Math.Sign(b.x - c.x), y: b.y + 2 * Math.Sign(b.y - c.y));
                Assert.False(CanTouch(Reach(f.Zone, (40, 12)), c));
                var opened = route == "jam" ? Reach(f.Zone, (40, 12), safeTrap: trap, diagonals: false)
                    : Reach(f.Zone, (40, 12), ignoredBeam: beam, parked: parked, diagonals: false);
                Assert.True(CanTouch(opened, c), "Either physical route must work on its own.");
                Assert.Contains(f.Cache, original);
                CollectionAssert.AreEquivalent(stock, original.SelectMany(e => e.GetPart<ContainerPart>().Contents));
                foreach (var container in original) foreach (var item in container.GetPart<ContainerPart>().Contents)
                    Assert.AreSame(container, item.GetPart<PhysicsPart>().InInventory);
                Assert.False(f.Owners.Any(e => e.BlueprintName == "SalvagedTimber" || e.BlueprintName == "RepairTimberPile" || e.BlueprintName == "GleanersTimberPallet"));
            }
        }

        [TestCase(7, false)]
        [TestCase(8, true)]
        public void ActualBeamHaulUsesStrengthAndSpeedAndLeavesAPassableReleasedEntrance(int strength, bool allowed)
        {
            using (var f = new Fixture(1))
            {
                Assert.True(f.Place()); var beam = f.Role("haul-bypass");
                var b = f.Zone.GetEntityPosition(beam); var c = f.Zone.GetEntityPosition(f.Cache);
                var d = (x: Math.Sign(b.x - c.x), y: Math.Sign(b.y - c.y));
                var grab = (x: b.x + d.x, y: b.y + d.y);
                var pull = (x: grab.x + d.x, y: grab.y + d.y);
                var aside = (x: pull.x - d.y, y: pull.y + d.x);
                var actor = Hauler(f.Zone, grab, strength);
                Assert.AreEqual(60, DragRules.WeightOf(beam));
                Assert.Null(beam.GetPart<HarvestablePart>()); Assert.Null(beam.GetPart<DestructiblePart>());
                Assert.AreEqual(allowed ? DragVerdict.Ok : DragVerdict.TooHeavy, DragSystem.TryGrab(actor, beam, f.Zone));
                if (allowed)
                {
                    Assert.AreEqual(76, actor.GetStatValue("Speed"));
                    MovementSystem.TryMoveTo(actor, f.Zone, pull.x, pull.y);
                    Assert.AreEqual(pull, f.Zone.GetEntityPosition(actor));
                    Assert.AreEqual(grab, f.Zone.GetEntityPosition(beam));
                    Assert.True(MovementSystem.TryMoveTo(actor, f.Zone, aside.x, aside.y));
                    Assert.AreEqual(pull, f.Zone.GetEntityPosition(beam));
                    DragSystem.Release(actor);
                    Assert.AreEqual(100, actor.GetStatValue("Speed"));
                    Assert.False(DragSystem.IsBeingDragged(beam));
                    Assert.True(CanTouch(Reach(f.Zone, aside, diagonals: false), c), "The two-step haul leaves a cardinal walk-around route.");
                    foreach (var step in new[] { (grab.x - d.y, grab.y + d.x), grab, b, (b.x - d.x, b.y - d.y), (b.x - 2 * d.x, b.y - 2 * d.y) })
                        Assert.True(MovementSystem.TryMoveTo(actor, f.Zone, step.Item1, step.Item2), "The real actor must walk the cleared lane.");
                    Assert.LessOrEqual(SpatialQuery.Distance(f.Zone, actor, f.Cache), 1);
                    Assert.AreEqual(pull, f.Zone.GetEntityPosition(beam), "Released timber must stay put after walking away.");
                }
                else
                {
                    Assert.AreEqual(b, f.Zone.GetEntityPosition(beam));
                    Assert.AreEqual(100, actor.GetStatValue("Speed"));
                    Assert.False(CanTouch(Reach(f.Zone, grab), c));
                }
            }
        }

        [Test]
        public void VisibleDescriptionsExplainTheBlockedServiceOpeningAndFiniteMaterialChoice()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place());
                string trap = f.Role("trap").GetPart<ExaminablePart>().Text.ToLowerInvariant();
                string beam = f.Role("haul-bypass").GetPart<ExaminablePart>().Text.ToLowerInvariant();
                StringAssert.Contains("timber", trap); StringAssert.Contains("beam", trap);
                StringAssert.DoesNotContain("open gap leads around", trap);
                StringAssert.Contains("haul", beam); StringAssert.Contains("opening", beam);
            }
        }

        [TestCase(64)]
        [TestCase(1729)]
        public void ActualSouthernGenerationContainsBothRealEntrancesAndRetainsTheirOwners(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                const string id = "Overworld.11.11.0";
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
                var zone = manager.GetZone(id); Assert.NotNull(zone);
                Assert.AreEqual(2, manager.Exploration.DispositionFor(id));
                var beam = zone.GetReadOnlyEntities().SingleOrDefault(e => e.GetProperty(RoleKey) == "haul-bypass");
                Assert.NotNull(beam, "The new yard must actually fit at its ordinary southern address.");
                Assert.AreEqual("FallenBeam", beam.BlueprintName);
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.GetProperty(RoleKey) == "trap"));
                Assert.AreSame(zone, manager.GetZone(id));
                Assert.AreSame(beam, zone.GetReadOnlyEntities().Single(e => e.ID == beam.ID));
            }
        }

        [TestCase("FieldAlembic")]
        [TestCase("TemperingShelter")]
        public void OtherWorksitesKeepTheirOpenAccessAndDoNotAcquireTheYard(string family)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place(family)); Assert.True(f.Final());
                Assert.False(f.Owners.Any(e => e.BlueprintName == "FallenBeam" || e.BlueprintName == "SpikeTrap"));
                var service = f.Owners.Single(e => e.BlueprintName == "AlchemyStill" || e.BlueprintName == "TinkersForge");
                Assert.True(CanTouch(Reach(f.Zone, (40, 12)), f.Zone.GetEntityPosition(service)));
            }
        }

        [TestCase("rooted")]
        [TestCase("weight")]
        [TestCase("lift-strength")]
        [TestCase("carryable")]
        [TestCase("harvestable")]
        [TestCase("destructible")]
        [TestCase("authority")]
        public void MalformedOrRevokedBeamFactoryCannotReplaceTheOriginalSourceGraph(string fault)
        {
            using (var f = new Fixture())
            {
                f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                f.Factory.Blueprints["FallenBeam"].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old);
                bool allowed = true; int calls = 0;
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                {
                    calls++;
                    if (fault == "rooted") e.RemovePart(e.GetPart<HandlingPart>());
                    if (fault == "weight") e.GetPart<HandlingPart>().Weight = 600;
                    if (fault == "lift-strength") e.GetPart<HandlingPart>().MinLiftStrength = 9;
                    if (fault == "carryable") e.GetPart<HandlingPart>().Carryable = true;
                    if (fault == "harvestable") e.AddPart(new HarvestablePart());
                    if (fault == "destructible") e.AddPart(new DestructiblePart());
                    if (fault == "authority") allowed = false;
                };
                try
                {
                    Assert.False(f.Place(authority: () => allowed));
                    Assert.AreEqual(1, calls, "The selected beam factory was actually reached.");
                    Assert.True(proof()); CollectionAssert.AreEquivalent(old, f.Zone.GetReadOnlyEntities());
                    Assert.Null(f.Owners); Assert.Null(f.Final);
                    Assert.True(f.Population.SourceReceipt.IsCurrent); Assert.True(f.Containers.SourceReceipt.IsCurrent);
                }
                finally { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null; }
            }
        }

        [TestCase("beam-weight")]
        [TestCase("beam-rooted")]
        [TestCase("wall-collision")]
        public void LaterFactoryCannotChangeAnAlreadyStagedMechanismOrOpenTheCertifiedRing(string fault)
        {
            using (var f = new Fixture())
            {
                f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                foreach (string blueprint in new[] { "FallenBeam", "StoneWall" })
                    f.Factory.Blueprints[blueprint].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                var old = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(old);
                Entity priorBeam = null, priorWall = null; bool changed = false;
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                {
                    if (e.BlueprintName == "FallenBeam") priorBeam = e;
                    if (e.BlueprintName != "StoneWall" || changed) return;
                    if (fault == "wall-collision")
                    {
                        if (priorWall == null) { priorWall = e; return; }
                        priorWall.Tags.Remove("Solid"); priorWall.GetPart<PhysicsPart>().Solid = false;
                    }
                    else
                    {
                        Assert.NotNull(priorBeam);
                        if (fault == "beam-weight") priorBeam.GetPart<HandlingPart>().Weight = 600;
                        else priorBeam.RemovePart(priorBeam.GetPart<HandlingPart>());
                    }
                    changed = true;
                };
                try
                {
                    Assert.False(f.Place()); Assert.True(changed, "A later factory actually altered an earlier staged owner.");
                    Assert.True(proof()); CollectionAssert.AreEquivalent(old, f.Zone.GetReadOnlyEntities());
                    Assert.Null(f.Owners); Assert.Null(f.Final);
                    Assert.True(f.Population.SourceReceipt.IsCurrent); Assert.True(f.Containers.SourceReceipt.IsCurrent);
                }
                finally { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null; }
            }
        }

        [TestCase("interior")]
        [TestCase("grab")]
        [TestCase("pull")]
        [TestCase("turn")]
        [TestCase("direct")]
        public void FinalPlacementProofRejectsANewIndependentBlockerInRequiredWorkingSpace(string location)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); Assert.True(f.Final());
                var b = f.Zone.GetEntityPosition(f.Role("haul-bypass"));
                var c = f.Zone.GetEntityPosition(f.Cache);
                var d = (x: Math.Sign(b.x - c.x), y: Math.Sign(b.y - c.y));
                var p = location == "interior" ? (b.x - d.x, b.y - d.y)
                    : location == "grab" ? (b.x + d.x, b.y + d.y)
                    : location == "pull" ? (b.x + 2 * d.x, b.y + 2 * d.y)
                    : location == "turn" ? (b.x + 2 * d.x - d.y, b.y + 2 * d.y + d.x)
                    : (c.x - 4 * d.x, c.y - 4 * d.y);
                Assert.False(f.Zone.GetCell(p.Item1, p.Item2).BlocksMovement());
                var independent = f.Factory.CreateEntity("StoneWall");
                Assert.True(f.Zone.AddEntity(independent, p.Item1, p.Item2));
                Assert.False(f.Final(), "Late callbacks may not erase a promised interior/apron/approach.");
                Assert.AreSame(f.Zone.GetCell(p.Item1, p.Item2), f.Zone.GetEntityCell(independent), "A read-only validator cannot clean up another system's owner.");
                Assert.Contains(independent, f.Zone.GetCell(p.Item1, p.Item2).Objects);
            }
        }
    }
}
