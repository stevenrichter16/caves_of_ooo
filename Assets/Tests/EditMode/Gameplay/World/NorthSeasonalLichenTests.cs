using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class NorthSeasonalLichenTests
    {
        const string North = "Overworld.11.9.0";
        const string Product = "FrostLichen";
        const string SeasonalRole = "seasonal-cold-forage";
        const int Seed = 64;
        const int CoolPeriod = 3;

        static OverworldZoneManager GenerateNorth(HaulingContentScope scope, int? version = null)
        {
            scope.Seed(unchecked(Seed ^ FormationSelector.StableIndex(North, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, Seed, true);
            if (version.HasValue) SoddenDistrictIntegrationTests.Version(manager, version.Value);
            manager.SetActiveZone(North);
            Assert.That(manager.ActiveZone, Is.Not.Null);
            Assert.That(manager.Exploration.Find(North)?.Family, Is.EqualTo(SpreadExplorationFamily.FieldAlembic));
            Assert.That(manager.Exploration.DispositionFor(North), Is.EqualTo(2));
            return manager;
        }

        static Entity[] OwnersWithRole(Zone zone, string role) => zone.GetReadOnlyEntities()
            .Where(entity => entity.GetProperty(SpreadExplorationWorksites.RoleKey) == role).ToArray();

        static Entity FindSeasonalPatch(Zone zone)
        {
            var patches = OwnersWithRole(zone, SeasonalRole);
            Assert.That(patches, Has.Length.EqualTo(1), "Expected one optional patch in the accepted seed 64 North Works.");
            return patches[0];
        }

        static Entity[] OriginalWorksite(Zone zone)
        {
            var still = OwnersWithRole(zone, "still").Single();
            var lichen = OwnersWithRole(zone, "cold-forage").Single();
            var herb = OwnersWithRole(zone, "medicine-forage").Single();
            Assert.That(still.BlueprintName, Is.EqualTo("AlchemyStill"));
            Assert.That(still.GetPart<AlchemyStillPart>()?.ParentEntity, Is.SameAs(still));
            Assert.That(lichen.BlueprintName, Is.EqualTo("FrostLichenPatch"));
            Assert.That(lichen.GetPart<HarvestablePart>(), Is.TypeOf<HarvestablePart>());
            Assert.That(lichen.GetPart<HarvestablePart>().YieldBlueprint, Is.EqualTo(Product));
            Assert.That(lichen.GetPart<HarvestablePart>().Harvested, Is.False);
            Assert.That(herb.BlueprintName, Is.EqualTo("MendleafPlant"));
            Assert.That(herb.GetPart<HarvestablePart>()?.Harvested, Is.False);
            return new[] { still, lichen, herb };
        }

        [Test]
        public void FreshNorthAddsOneFixedSeasonalPatchBesideTheOrdinaryWorksite()
        {
            using var scope = new HaulingContentScope();
            var manager = GenerateNorth(scope);
            var zone = manager.ActiveZone;
            OriginalWorksite(zone);
            var patch = FindSeasonalPatch(zone);
            Assert.That(manager.Exploration.Version, Is.GreaterThanOrEqualTo(31));
            Assert.That(patch.BlueprintName, Is.EqualTo("FrostLichenPatch"));
            Assert.That(patch.Parts.OfType<HarvestablePart>().Single(), Is.TypeOf<SeasonalHarvestPart>());
            var harvest = patch.GetPart<SeasonalHarvestPart>();
            Assert.That(harvest.ParentEntity, Is.SameAs(patch));
            Assert.That(harvest.WorldKey, Is.EqualTo(manager.Exploration.WorldKey).And.Not.Empty);
            Assert.That(harvest.PolicyVersion, Is.EqualTo(RegionalWeather.PolicyVersion));
            Assert.That(harvest.Season, Is.EqualTo(CoolPeriod));
            Assert.That(harvest.Harvested, Is.False);
            Assert.That(harvest.YieldBlueprint, Is.EqualTo(Product));
            Assert.That((harvest.YieldMin, harvest.YieldMax, harvest.YieldChance), Is.EqualTo((1, 1, 100)));

            var cell = zone.GetEntityCell(patch);
            Assert.That(harvest.Ground?.BlueprintName, Is.EqualTo("Grass"));
            Assert.That(zone.GetEntityCell(harvest.Ground), Is.SameAs(cell));
            Assert.That(cell.Occupants, Is.EquivalentTo(new[] { patch, harvest.Ground }));
            Assert.That(cell.IsInterior, Is.False);
            Assert.That(cell.BlocksMovement(), Is.False);
            Assert.That(zone.GenReservedCells.Contains((cell.X, cell.Y)), Is.False);
            Assert.That(zone.TileState.Get(cell.X, cell.Y)?.IsEmpty ?? true, Is.True);
        }

        [Test]
        public void LiteralVersion30KeepsThePriorNorthWorksiteAndDoesNotAdoptCachedStock()
        {
            using var scope = new HaulingContentScope();
            var manager = GenerateNorth(scope, 30);
            var zone = manager.ActiveZone;
            Assert.That(manager.Exploration.Version, Is.EqualTo(30));
            var original = OriginalWorksite(zone);
            Assert.That(OwnersWithRole(zone, SeasonalRole), Is.Empty);
            Assert.That(zone.GetReadOnlyEntities().Any(entity => entity.HasPart<SeasonalHarvestPart>()), Is.False);
            manager.UnloadZone(North);
            Assert.That(manager.GetZone(North), Is.SameAs(zone));
            Assert.That(OriginalWorksite(zone), Is.EquivalentTo(original));
        }

        [Test]
        public void OnlyTheCoolPeriodAllowsOneNormalHarvestAndReloadKeepsThePatchDepleted()
        {
            using var scope = new HaulingContentScope();
            var manager = GenerateNorth(scope);
            var zone = manager.ActiveZone;
            var patch = FindSeasonalPatch(zone);
            var original = OriginalWorksite(zone);
            var harvest = patch.GetPart<SeasonalHarvestPart>();
            var ground = harvest.Ground;
            var cell = zone.GetEntityCell(patch);
            var approach = new[] { zone.GetCell(cell.X - 1, cell.Y), zone.GetCell(cell.X, cell.Y - 1),
                zone.GetCell(cell.X + 1, cell.Y), zone.GetCell(cell.X, cell.Y + 1) }
                .First(candidate => candidate != null && !candidate.BlocksMovement());
            var actor = scope.Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(actor, approach.X, approach.Y));
            cell.Explored = cell.IsVisible = true;
            SettlementRuntime.ActiveZone = zone;
            var clock = new TurnManager();
            var pack = actor.GetPart<InventoryPart>();
            var priorFactory = HarvestablePart.Factory;
            HarvestablePart.Factory = scope.Factory;
            try
            {
                bool Offers(Entity source) => InventorySystem.GetActions(actor, source).Any(action => action.Command == "Harvest");
                bool Harvest() => InventorySystem.PerformAction(actor, patch, "Harvest", zone);
                Assert.That(Offers(original[1]), Is.True);
                Assert.That(Offers(patch), Is.False);
                Assert.That(Harvest(), Is.False);
                Assert.That(harvest.Harvested, Is.False);
                Assert.That(zone.GetEntityCell(patch), Is.SameAs(cell));

                int phaseLength = RegionalWeather.CycleWindows / 4 * RegionalWeather.WindowTicks;
                clock.AdvanceClock(CoolPeriod * phaseLength);
                Assert.That(Offers(original[1]), Is.True);
                Assert.That(Offers(patch), Is.True);
                Assert.That(Harvest(), Is.True);
                Assert.That(Harvest(), Is.False);
                Assert.That(harvest.Harvested, Is.True);
                Assert.That(zone.GetEntityCell(patch), Is.Null);
                Assert.That(zone.GetEntityCell(ground), Is.SameAs(cell));
                var lichen = pack.Objects.Single(item => item.BlueprintName == Product);
                Assert.That(lichen.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
                Assert.That(lichen.GetPart<PhysicsPart>().InInventory, Is.SameAs(actor));

                clock.AdvanceClock(RegionalWeather.CycleWindows * RegionalWeather.WindowTicks);
                manager.UnloadZone(North);
                Assert.That(manager.GetZone(North), Is.SameAs(zone));
                Assert.That(OwnersWithRole(zone, SeasonalRole), Is.Empty);
                Assert.That(OriginalWorksite(zone), Is.EquivalentTo(original));

                using var stream = new MemoryStream();
                GameSessionState.Capture("north-seasonal-lichen", "harvested", manager, clock, actor).Save(new SaveWriter(stream));
                stream.Position = 0;
                var loaded = GameSessionState.Load(new SaveReader(stream, scope.Factory));
                var reloadedNorth = loaded.ZoneManager.GetZone(North);
                Assert.That(reloadedNorth, Is.Not.SameAs(zone));
                Assert.That(OwnersWithRole(reloadedNorth, SeasonalRole), Is.Empty);
                Assert.That(OriginalWorksite(reloadedNorth).Select(entity => entity.ID), Is.EqualTo(original.Select(entity => entity.ID)));
                Assert.That(reloadedNorth.GetReadOnlyEntities().Single(entity => entity.ID == ground.ID).BlueprintName, Is.EqualTo("Grass"));
                var savedLichen = loaded.Player.GetPart<InventoryPart>().Objects.Single(item => item.BlueprintName == Product);
                Assert.That(savedLichen.ID, Is.EqualTo(lichen.ID));
                Assert.That(savedLichen.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
                Assert.That(savedLichen.GetPart<PhysicsPart>().InInventory, Is.SameAs(loaded.Player));
            }
            finally { HarvestablePart.Factory = priorFactory; }
        }
    }
}
