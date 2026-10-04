using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MendleafDryingYardTests
    {
        const string North = "Overworld.11.9.0", Patchbearer = "MarlbackPatchbearer";
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        sealed class EndpointRandom : Random
        {
            readonly bool last; internal EndpointRandom(bool last) { this.last = last; }
            public override int Next(int maxValue) => last ? 0 : Math.Max(0, maxValue - 1);
            public override int Next(int minValue, int maxValue) => minValue;
        }
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone;
            internal readonly SpreadCompositionBuilder Terrain = new SpreadCompositionBuilder(64) { FormationOverride = Formation.OldRoad };
            internal readonly PopulationBuilder Population;
            internal readonly ContainerBuilder Containers = new ContainerBuilder(BiomeType.Spread, 1, ContainerPlacementService.ZoneKind.Wilderness) { CaptureSourceReceipts = true };
            internal Entity[] Owners; internal Func<bool> Final;
            readonly EntityFactory oldHarvest = HarvestablePart.Factory, oldCorpse = CorpsePart.Factory;
            internal Fixture(int count = 1, string id = North, int? rotation = null, bool? farSourceFirst = null, bool oppositeMargin = false)
            {
                Scope.Seed(64); Zone = new Zone(id);
                HarvestablePart.Factory = CorpsePart.Factory = Factory;
                Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => !(bool)typeof(DoorPart).GetMethod("IsBareGround", All).Invoke(null, new object[] { e })).ToArray()) Zone.RemoveEntity(e);
                Zone.GenReservedCells.Clear();
                Population = new PopulationBuilder(new PopulationTable { Name = "SpreadTier1", Entries = new List<PopulationEntry> {
                    new PopulationEntry { BlueprintName = "Viper", EncounterGroup = "SpreadTier1Encounter", MinCount = count, MaxCount = count } } })
                {
                    CaptureSourceReceipts = true,
                    // Known real producer cells keep the untouched actor outside
                    // arrival; no source is moved after its receipt is captured.
                    HabitatFilter = (bp, cell) => oppositeMargin ? (cell.X == 32 && cell.Y == 10 || cell.X == 15 && cell.Y == 0) : farSourceFirst.HasValue
                        ? (cell.X == 43 && cell.Y == 16 || cell.X == 75 && cell.Y == 20)
                        : cell.Y == 5 && (cell.X == 60 || cell.X == 65)
                };
                Assert.True(Population.BuildZone(Zone, Factory, farSourceFirst.HasValue ? new EndpointRandom(farSourceFirst.Value) : new Random(17)));
                Assert.True(Containers.BuildZone(Zone, Factory, new Random(27)));
                Assert.True(Population.SourceReceipt.IsCurrent);
                if (rotation.HasValue)
                {
                    // Reserve outside a work area that admits one orientation.
                    // Reservations stay physically walkable; source cells remain
                    // untouched. The native seed tests use unedited terrain.
                    var source = Population.SourceReceipt.Owners.Select(Zone.GetEntityPosition).ToHashSet();
                    Zone.ForEachCell((cell, x, y) =>
                    {
                        int width = rotation % 2 == 0 ? 8 : 4, height = rotation % 2 == 0 ? 4 : 8;
                        int cx = oppositeMargin ? 12 : 18, cy = oppositeMargin ? 17 : 12;
                        if ((Math.Abs(x - cx) > width || Math.Abs(y - cy) > height) && !source.Contains((x, y))) Zone.GenReservedCells.Add((x, y));
                    });
                }
            }
            internal bool Place(SpreadExplorationFamily family = SpreadExplorationFamily.FieldAlembic)
                => SpreadExplorationWorksites.TryPlace(Zone, Factory, Terrain, Population, Containers, family, () => true, out Owners, out Final);
            internal Entity Owner(string blueprint)
            {
                var owner = Owners?.SingleOrDefault(e => e.BlueprintName == blueprint);
                Assert.NotNull(owner, "The north drying yard must contain its real " + blueprint + ".");
                return owner;
            }
            internal Func<bool> Proof(IEnumerable<Entity> owners) => (Func<bool>)typeof(SpreadGenerationReceipt)
                .GetMethod("CaptureFinalState", All).Invoke(null, new object[] { Zone, owners });
            internal (int x, int y) At(int x, int y)
            {
                var still = Zone.GetEntityPosition(Owner("AlchemyStill")); var medic = Zone.GetEntityPosition(Owner(Patchbearer));
                int dx = (medic.x - still.x) / 6, dy = (medic.y - still.y) / 6;
                return (still.x + dx * x - dy * y, still.y + dy * x + dx * y);
            }
            public void Dispose() { HarvestablePart.Factory = oldHarvest; CorpsePart.Factory = oldCorpse; Scope.Dispose(); }
        }
        static int Units(Entity e) => e.GetPart<StackerPart>()?.StackCount ?? 1;
        static int Packed(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(Units);
        static int Distance((int x, int y) a, (int x, int y) b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        static HashSet<(int x, int y)> Reach(Zone zone, (int x, int y) start, bool avoidThreats)
        {
            var threats = zone.GetReadOnlyEntities().Where(e => e.HasTag("Creature") && !e.HasTag("Player") && e.HasPart<BrainPart>()).ToArray();
            var reached = new HashSet<(int x, int y)>(); var queue = new Queue<(int x, int y)>();
            void Add(int x, int y)
            {
                var cell = zone.GetCell(x, y);
                if (cell == null || cell.BlocksMovement() || zone.TileState.Get(x, y)?.IsEmpty == false
                    || cell.Objects.Any(e => e.HasPart<TriggerOnStepPart>() || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>())) return;
                if (avoidThreats && threats.Any(e => Distance((x, y), zone.GetEntityPosition(e)) <= Math.Max(e.GetPart<BrainPart>().SightRadius, e.GetPart<CombatTacticsPart>()?.AssistRadius ?? 0))) return;
                if (reached.Add((x, y))) queue.Enqueue((x, y));
            }
            Add(start.x, start.y);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (dx != 0 || dy != 0) Add(p.x + dx, p.y + dy);
            }
            return reached;
        }

        [TestCase(64)] [TestCase(1729)]
        public void ActualNorthColdGenerationAddsOneStockedMedicAndFiniteHerbWithoutIncreasingActorCount(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var blueprint = scope.Factory.Blueprints[Patchbearer]; scope.Factory.Blueprints.Remove(Patchbearer);
                scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(North, int.MaxValue)));
                var fallbackManager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                var fallback = fallbackManager.GetZone(North);
                int count = fallback.GetReadOnlyEntities().Count(e => e.HasTag("Creature"));
                Assert.IsTrue(fallback.GetReadOnlyEntities().Any(e => e.BlueprintName == "AlchemyStill"));
                scope.Factory.Blueprints[Patchbearer] = blueprint;
                scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(North, int.MaxValue)));
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var zone = manager.GetZone(North);
                Assert.AreEqual(2, manager.Exploration.DispositionFor(North));
                Assert.AreEqual(count, zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
                var medic = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == Patchbearer);
                var landscape = SpreadCompositionPlan.Create(North, seed, Formation.None, manager.Exploration.Entries.Single(e => e.ZoneID == North).Topology);
                var exits = new[] { (0, landscape.WestY), (Zone.Width - 1, landscape.EastY), (landscape.NorthX, 0), (landscape.SouthX, Zone.Height - 1) };
                var sources = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Viper" || e.BlueprintName == "MarlbackScrabbler");
                var diagnosis = string.Join("; ", sources.Select(e => e.BlueprintName + " " + e.ID + " at " + zone.GetEntityPosition(e)
                    + " sight=" + e.GetPart<BrainPart>()?.SightRadius + " arrival-distance=" + Distance(zone.GetEntityPosition(e), (40, 12))
                    + " border-distances=" + string.Join(",", exits.Select(p => p + ":" + Distance(zone.GetEntityPosition(e), p)))));
                Assert.NotNull(medic, "Actual north generation must realize the new source, not merely retain the old alembic. " + diagnosis);
                Assert.AreEqual(20, medic.GetStatValue("Hitpoints"));
                Assert.AreEqual(1, Packed(medic, "HealingTonic"));
                Assert.AreSame(medic, medic.GetPart<FieldMedicinePart>().FindCarriedMedicine().GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "MendleafPlant"));
                Assert.AreEqual(2, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "AlchemyShelf" && e.GetPart<ContainerPart>().Contents.Count == 0));
                Assert.AreSame(zone, manager.GetZone(North));
            }
        }

        [TestCase(1)] [TestCase(2)]
        public void ExactlyOneActualReceiptActorIsReplacedAndEveryUnselectedOwnerStaysUntouched(int count)
        {
            using (var f = new Fixture(count))
            {
                var source = f.Population.SourceReceipt.Owners.ToArray();
                var proofs = source.ToDictionary(e => e, e => f.Proof(new[] { e }));
                var others = f.Zone.GetReadOnlyEntities().Except(source).ToArray(); var unchanged = f.Proof(others);
                Assert.True(f.Place()); Assert.True(f.Final());
                Assert.AreEqual(1, f.Owners.Count(e => e.BlueprintName == Patchbearer));
                Assert.AreEqual(count, f.Zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
                Assert.AreEqual(1, source.Count(e => f.Zone.GetEntityCell(e) == null));
                foreach (var retained in source.Where(e => f.Zone.GetEntityCell(e) != null)) Assert.True(proofs[retained]());
                Assert.True(unchanged(), "Original terrain and container stock stay literal.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void NearArrivalSourceIsSelectedWithEitherActualReceiptOrderAndFarBorderThreatIsRetained(bool farFirst)
        {
            using (var f = new Fixture(2, farSourceFirst: farFirst))
            {
                var sources = f.Population.SourceReceipt.Owners.ToArray();
                Assert.AreEqual(farFirst ? (75, 20) : (43, 16), f.Zone.GetEntityPosition(sources[0]), "The actual producer exercises both receipt orders.");
                var near = sources.Single(e => f.Zone.GetEntityPosition(e) == (43, 16));
                var far = sources.Single(e => f.Zone.GetEntityPosition(e) == (75, 20)); var proof = f.Proof(new[] { far });
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.NotNull(f.Owner(Patchbearer));
                Assert.Null(f.Zone.GetEntityCell(near)); Assert.True(proof());
                var safe = Reach(f.Zone, (40, 12), true); var herb = f.Zone.GetEntityPosition(f.Owner("MendleafPlant"));
                Assert.GreaterOrEqual(safe.Count(p => Distance(p, herb) == 1), 2);
                var exits = new[] { (0, f.Terrain.Plan.WestY), (Zone.Width - 1, f.Terrain.Plan.EastY), (f.Terrain.Plan.NorthX, 0), (f.Terrain.Plan.SouthX, Zone.Height - 1) };
                Assert.True(exits.Any(safe.Contains), "At least one canonical border permits safe withdrawal.");
                var physical = Reach(f.Zone, (40, 12), false);
                foreach (var exit in exits) Assert.Contains(exit, physical.ToArray(), "Every ordinary border stays physically reachable.");
            }
        }

        [TestCase("Overworld.10.9.0")] [TestCase("Overworld.12.9.0")]
        public void OtherAlembicsRemainThePreviousSmallServiceWithoutMedicineOrActorReplacement(string id)
        {
            using (var f = new Fixture(2, id))
            {
                var proof = f.Proof(f.Zone.GetReadOnlyEntities());
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof());
                Assert.False(f.Owners.Any(e => e.BlueprintName == Patchbearer || e.BlueprintName == "MendleafPlant" || e.BlueprintName == "AlchemyShelf"));
                Assert.NotNull(f.Owner("AlchemyStill")); Assert.NotNull(f.Owner("StoneburrPatch")); Assert.NotNull(f.Owner("FrostLichenPatch"));
            }
        }

        [TestCase("MarlbackPatchbearer")] [TestCase("MendleafPlant")] [TestCase("AlchemyShelf")]
        public void MissingOptionalContentFallsBackBeforeClaimingSourcesAndKeepsTheOriginalAlembic(string missing)
        {
            using (var f = new Fixture(2))
            {
                f.Factory.Blueprints.Remove(missing); var proof = f.Proof(f.Zone.GetReadOnlyEntities());
                Assert.True(f.Place(), "Reduced optional content must not erase the existing still service.");
                Assert.True(f.Final()); Assert.True(proof());
                Assert.NotNull(f.Owner("AlchemyStill"));
                Assert.False(f.Owners.Any(e => e.BlueprintName == Patchbearer || e.BlueprintName == "MendleafPlant" || e.BlueprintName == "AlchemyShelf"));
            }
        }

        [Test]
        public void SpaceForOnlyTheEarlierAlembicKeepsItsServiceAndEverySourceOwner()
        {
            using (var f = new Fixture(2))
            {
                var source = f.Population.SourceReceipt.Owners.Select(f.Zone.GetEntityPosition).ToHashSet();
                f.Zone.ForEachCell((cell, x, y) =>
                {
                    if ((Math.Abs(x - 18) > 3 || Math.Abs(y - 12) > 2) && !source.Contains((x, y))) f.Zone.GenReservedCells.Add((x, y));
                });
                var proof = f.Proof(f.Zone.GetReadOnlyEntities());
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof());
                Assert.NotNull(f.Owner("AlchemyStill")); Assert.AreEqual(6, f.Owners.Length);
                Assert.False(f.Owners.Any(e => e.BlueprintName == Patchbearer || e.BlueprintName == "MendleafPlant"));
            }
        }

        [Test]
        public void NeutralAmbientMagpieNearArrivalDoesNotVetoTheHostileSourceYard()
        {
            using (var f = new Fixture())
            {
                var magpie = f.Factory.CreateEntity("Magpie"); var player = f.Factory.CreateEntity("Player");
                Assert.True(magpie.GetPart<BrainPart>().Passive);
                Assert.False(FactionManager.IsHostile(magpie, player));
                Assert.False(f.Zone.GetCell(41, 11).BlocksMovement()); Assert.False(f.Zone.GetCell(40, 12).BlocksMovement());
                Assert.True(f.Zone.AddEntity(magpie, 41, 11)); var proof = f.Proof(new[] { magpie });
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof());
                Assert.NotNull(f.Owner(Patchbearer), "A nearby passive ambient bird is not a hostile source envelope.");
                Assert.NotNull(f.Owner("MendleafPlant")); Assert.AreEqual((41, 11), f.Zone.GetEntityPosition(magpie));
            }
        }

        [TestCase("medicine")]
        [TestCase("harvest")]
        [TestCase("shelf")]
        public void LaterWindbreakFactoryCannotAcceptADepletedMedicineSourceOrFalseCover(string fault)
        {
            using (var f = new Fixture())
            {
                f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                foreach (var blueprint in new[] { Patchbearer, "MendleafPlant", "AlchemyShelf", "StoneWall" })
                    f.Factory.Blueprints[blueprint].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                var prior = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(prior);
                Entity medic = null, herb = null, shelf = null; bool changed = false;
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                {
                    if (e.BlueprintName == Patchbearer) medic = e;
                    if (e.BlueprintName == "MendleafPlant") herb = e;
                    if (e.BlueprintName == "AlchemyShelf") shelf = e;
                    if (e.BlueprintName != "StoneWall" || medic == null || changed) return;
                    if (fault == "medicine") medic.GetPart<FieldMedicinePart>().FindCarriedMedicine().GetPart<TonicPart>().Healing = "";
                    if (fault == "harvest") herb.GetPart<HarvestablePart>().Harvested = true;
                    if (fault == "shelf") shelf.GetPart<PhysicsPart>().Solid = false;
                    changed = true;
                };
                try
                {
                    Assert.False(f.Place()); Assert.True(changed, "The later windbreak creation actually altered its earlier owner.");
                    Assert.True(proof()); CollectionAssert.AreEquivalent(prior, f.Zone.GetReadOnlyEntities());
                    Assert.True(f.Population.SourceReceipt.IsCurrent); Assert.Null(f.Owners); Assert.Null(f.Final);
                }
                finally { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null; }
            }
        }

        [Test]
        public void ObservingTheActualStagedFactoryOwnersPreservesAnOtherwiseValidPlacement()
        {
            using (var f = new Fixture())
            {
                var observations = new List<string>();
                f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                foreach (var blueprint in new[] { "AlchemyStill", "StoneburrPatch", "FrostLichenPatch", "StoneWall", "MendleafPlant", "AlchemyShelf", Patchbearer })
                    f.Factory.Blueprints[blueprint].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                {
                    var p = e.GetPart<PhysicsPart>(); var h = e.GetPart<HarvestablePart>(); var m = e.GetPart<FieldMedicinePart>();
                    observations.Add(e.BlueprintName + " solid=" + p?.Solid + "/" + e.HasTag("Solid") + " takeable=" + p?.Takeable
                        + " hp=" + e.GetStatValue("Hitpoints") + "/" + e.GetStat("Hitpoints")?.Max + " sight=" + e.GetPart<BrainPart>()?.SightRadius
                        + " assist=" + e.GetPart<CombatTacticsPart>()?.AssistRadius + " harvest=" + h?.YieldBlueprint + ":" + h?.YieldMin + "-" + h?.YieldMax
                        + " medicine=" + m?.FindCarriedMedicine()?.GetPart<TonicPart>()?.Healing + " inventory=" + string.Join(",", e.GetPart<InventoryPart>()?.Objects.Select(i => i.BlueprintName + ":" + Units(i)) ?? Enumerable.Empty<string>()));
                };
                try { Assert.True(f.Place(), string.Join("\n", observations)); Assert.True(f.Final()); }
                finally { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null; }
            }
        }

        [TestCase(0)] [TestCase(1)]
        public void EitherAxisHasAnIndependentShallowWithdrawalAndPhysicalDryingBays(int rotation)
        {
            using (var f = new Fixture(2, rotation: rotation))
            {
                Assert.True(f.Place()); var medic = f.Owner(Patchbearer); var herb = f.Owner("MendleafPlant");
                var still = f.Zone.GetEntityPosition(f.Owner("AlchemyStill")); var m = f.Zone.GetEntityPosition(medic);
                Assert.AreEqual(rotation == 0 ? (6, 0) : (0, 6), (Math.Abs(m.x - still.x), Math.Abs(m.y - still.y)));
                Assert.AreEqual(f.At(-7, 2), f.Zone.GetEntityPosition(herb));
                var safe = Reach(f.Zone, (40, 12), true); var h = f.Zone.GetEntityPosition(herb);
                Assert.GreaterOrEqual(safe.Count(p => Distance(p, h) == 1), 2);
                var exits = new[] { (0, f.Terrain.Plan.WestY), (Zone.Width - 1, f.Terrain.Plan.EastY), (f.Terrain.Plan.NorthX, 0), (f.Terrain.Plan.SouthX, Zone.Height - 1) };
                Assert.True(exits.Any(safe.Contains), "The shallow margin has at least one safe canonical withdrawal.");
                Assert.False(safe.Contains(f.At(4, 0)), "The partial route does not need the occupied aisle.");
                var full = Reach(f.Zone, (40, 12), false);
                foreach (var exit in exits) Assert.Contains(exit, full.ToArray(), "Every ordinary border remains physically reachable.");
                foreach (var local in new[] { (3, 0), (4, 0), (5, 0), (6, -1), (6, 1), (7, 0) }) Assert.Contains(f.At(local.Item1, local.Item2), full.ToArray());
                var shelves = f.Owners.Where(e => e.BlueprintName == "AlchemyShelf").ToArray(); Assert.AreEqual(2, shelves.Length);
                foreach (var shelf in shelves) { Assert.True(shelf.GetPart<PhysicsPart>().Solid); Assert.IsEmpty(shelf.GetPart<ContainerPart>().Contents); }
                var a = f.At(3, -1); var b = f.At(5, -1);
                Assert.False(AIHelpers.HasLineOfSight(f.Zone, a.x, a.y, b.x, b.y), "The normal factory lifecycle also makes a solid shelf opaque.");
                a = f.At(4, -4); b = f.At(4, -2);
                Assert.False(AIHelpers.HasLineOfSight(f.Zone, a.x, a.y, b.x, b.y), "Actual stone windbreak blocks this sight line.");
            }
        }

        [Test]
        public void OppositeFacingKeepsHerbOnArrivalSideOfTheUnchangedHostileCurtain()
        {
            using (var f = new Fixture(2, rotation: 2, oppositeMargin: true))
            {
                var retained = f.Population.SourceReceipt.Owners.Single(e => f.Zone.GetEntityPosition(e) == (15, 0));
                var proof = f.Proof(new[] { retained });
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof());
                var still = f.Zone.GetEntityPosition(f.Owner("AlchemyStill")); var medic = f.Zone.GetEntityPosition(f.Owner(Patchbearer));
                Assert.AreEqual((still.x - 6, still.y), medic, "The blocked east-facing shallow margin requires the opposite orientation.");
                var safe = Reach(f.Zone, (40, 12), true); var herb = f.Zone.GetEntityPosition(f.Owner("MendleafPlant"));
                Assert.AreEqual((still.x + 7, still.y - 2), herb);
                Assert.GreaterOrEqual(safe.Count(p => Distance(p, herb) == 1), 2);
                Assert.False(safe.Contains(f.At(4, 0)), "The shallow outcome does not require entering the medic's aisle.");
                var full = Reach(f.Zone, (40, 12), false);
                foreach (var local in new[] { (3, 0), (4, 0), (5, 0), (6, -1), (6, 1), (7, 0) }) Assert.Contains(f.At(local.Item1, local.Item2), full.ToArray());
            }
        }

        [Test]
        public void ActualShallowHarvestIsFiniteAndAnEarnedSprigBrewsOneWeakTonicAwayFromTheStill()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); var herb = f.Owner("MendleafPlant"); var harvest = herb.GetPart<HarvestablePart>();
                Assert.AreEqual("MendleafSprig", harvest.YieldBlueprint); Assert.AreEqual(100, harvest.YieldChance);
                Assert.AreEqual(1, harvest.YieldMin); Assert.AreEqual(2, harvest.YieldMax);
                var player = f.Factory.CreateEntity("Player"); var h = f.Zone.GetEntityPosition(herb);
                var approach = Reach(f.Zone, (40, 12), true).First(p => Distance(p, h) == 1);
                Assert.True(f.Zone.AddEntity(player, approach.x, approach.y));
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(herb, "Harvest"), player, f.Zone).Success);
                int count = Packed(player, "MendleafSprig"); Assert.That(count, Is.InRange(1, 2));
                Assert.True(harvest.Harvested); Assert.Null(f.Zone.GetEntityCell(herb));
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(herb, "Harvest"), player, f.Zone).Success);
                Assert.AreEqual(count, Packed(player, "MendleafSprig"));
                var sprig = player.GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == "MendleafSprig");
                Assert.Greater(SpatialQuery.Distance(f.Zone, player, f.Owner("AlchemyStill")), 1);
                Assert.True(BrewRuleRegistry.TryGetRule("brew_mending", out _));
                Assert.True(InventorySystem.ExecuteCommand(new BrewReagentsCommand(new[] { sprig }, f.Factory), player, f.Zone).Success);
                Assert.AreEqual(count - 1, Packed(player, "MendleafSprig"));
                var output = player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "BrewedTonic");
                Assert.AreEqual("1d4", output.GetPart<TonicPart>().Healing);
                Assert.AreSame(player, output.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(1, Packed(f.Owner(Patchbearer), "HealingTonic"), "Shallow preparation cannot spend or duplicate enemy stock.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualGeneratedMedicineHasOnlyItsOriginalUnusedOrConsumedDeathOutcome(bool used)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); var medic = f.Owner(Patchbearer); var medicine = medic.GetPart<FieldMedicinePart>();
                var tonic = medicine.FindCarriedMedicine(); Assert.NotNull(tonic); Assert.AreEqual("4d6+4", tonic.GetPart<TonicPart>().Healing);
                var player = f.Factory.CreateEntity("Player"); var p = f.At(5, 0); Assert.True(f.Zone.AddEntity(player, p.x, p.y));
                var brain = medic.GetPart<BrainPart>(); brain.CurrentZone = f.Zone; brain.SetPersonallyHostile(player);
                Assert.False(medicine.TryUseMedicine(player, f.Zone, new FieldMedicineFixture.MedicineRandom()), "Full health keeps the actual bottle.");
                if (used)
                {
                    // Controlled integration fixture uses the real damage and
                    // medicine paths; the ordinary native route must earn this.
                    CombatSystem.ApplyDamage(medic, new Damage(12), player, f.Zone);
                    Assert.AreEqual(8, medic.GetStatValue("Hitpoints"));
                    Assert.True(medicine.TryUseMedicine(player, f.Zone, new FieldMedicineFixture.MedicineRandom()));
                    Assert.AreEqual(16, medic.GetStatValue("Hitpoints")); Assert.Null(medicine.FindCarriedMedicine());
                }
                CombatSystem.ApplyDamage(medic, new Damage(1000), player, f.Zone);
                Assert.Null(f.Zone.GetEntityCell(medic));
                Assert.AreEqual(!used, f.Zone.GetReadOnlyEntities().Contains(tonic));
                Assert.AreEqual(used ? 0 : 1, f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "HealingTonic").Sum(Units));
            }
        }
    }
}
