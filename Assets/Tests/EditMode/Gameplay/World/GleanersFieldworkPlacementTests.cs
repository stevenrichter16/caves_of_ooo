using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Scratch-only proposed RED fixture. Existing public APIs and string blueprint
    // names let this compile before fieldwork production exists.
    public sealed class GleanersFieldworkPlacementTests
    {
        const string WicketBlueprint = "GleanersBuckledWicket";
        const string PalletBlueprint = "GleanersTimberPallet";
        HaulingContentScope content;

        [SetUp]
        public void Setup()
        {
            content = new HaulingContentScope();
            content.Seed(64);
            RepairRecipeRegistry.ResetForTests();
        }

        [TearDown]
        public void Cleanup()
        {
            RepairRecipeRegistry.ResetForTests();
            content.Dispose();
        }

        static void Restore(OverworldZoneManager manager, Entity world)
        {
            var method = typeof(SpreadExplorationPlan).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static);
            var plan = method.Invoke(null, new object[] { manager, world });
            typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, plan);
        }

        static void RestoreOld(OverworldZoneManager manager, string mode)
        {
            if (mode == "missing") { Restore(manager, new Entity { BlueprintName = "World" }); return; }
            if (mode == "v10")
            {
                var old = new Entity { BlueprintName = "World" };
                old.Properties[SpreadExplorationPlan.PropertyKey] = "10|64|0";
                Restore(manager, old);
                return;
            }
            // A real freshly encoded old-generation assignment, with its actual
            // key and literal rows. v12 must still recognize v11 during load.
            var world = SpreadExplorationPlan.BindForSave(manager, null);
            string wire = world.Properties[SpreadExplorationPlan.PropertyKey];
            world.Properties[SpreadExplorationPlan.PropertyKey] = "11" + wire.Substring(wire.IndexOf('|'));
            Restore(manager, world);
            Assert.AreEqual(11, manager.Exploration.Version);
        }

        [TestCase(WicketBlueprint)]
        [TestCase(PalletBlueprint)]
        public void FieldworkHasActualFunctionalBlueprints(string name)
        {
            Assert.True(content.Factory.Blueprints.ContainsKey(name), "Missing authored fieldwork content: " + name);
            var owner = content.Factory.CreateEntity(name);
            Assert.NotNull(owner.GetPart<PhysicsPart>());
            Assert.False(owner.GetPart<PhysicsPart>().Takeable);
            if (name == WicketBlueprint)
            {
                Assert.True(owner.GetPart<DoorPart>().IsClosed);
                Assert.False(owner.GetPart<RepairablePart>().Repaired);
                var recipe = RepairRecipeRegistry.Get(owner.GetPart<RepairablePart>().RecipeId);
                Assert.NotNull(recipe);
                Assert.AreEqual("SalvagedTimber", recipe.MaterialBlueprint);
                Assert.AreEqual(2, recipe.Quantity);
                Assert.True(owner.GetPart<CompositionPart>().Contains("Wood"));
            }
            else
            {
                Assert.NotNull(owner.GetPart<HandlingPart>());
                var harvest = owner.GetPart<HarvestablePart>();
                Assert.NotNull(harvest);
                Assert.AreEqual("SalvagedTimber", harvest.YieldBlueprint);
                Assert.AreEqual(2, harvest.YieldMin);
                Assert.AreEqual(2, harvest.YieldMax);
                Assert.AreEqual(100, harvest.YieldChance);
            }
        }

        [TestCase(64)]
        [TestCase(1729)]
        public void FreshGladeHasOneClosedWicketAndAnInitiallyRipeUsefulGarden(int seed)
        {
            content.Seed(seed);
            var manager = OverworldZoneManager.CreateDetached(content.Factory, seed, true);
            var zone = manager.GetZone(ReferenceGladePlan.ZoneID);
            var wicket = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == WicketBlueprint);
            Assert.NotNull(wicket, "Every supported new glade needs its working-yard route, not another rare selection.");
            Assert.AreEqual((46, 6), zone.GetEntityPosition(wicket));
            Assert.True(wicket.GetPart<DoorPart>().IsClosed);
            Assert.True(zone.GetCell(45, 6).Objects.Any(e => e.BlueprintName == "Wall"));
            Assert.True(zone.GetCell(47, 6).Objects.Any(e => e.BlueprintName == "Wall"));
            var garden = zone.GetCell(46, 4).Objects.SingleOrDefault(e => e.BlueprintName == "DrawgourdCrop");
            Assert.NotNull(garden);
            Assert.AreEqual(2, garden.GetPart<CropPart>().GrowthStage);
            Assert.AreEqual(0, garden.GetPart<CropPart>().MoistureTicks);
            Assert.True(zone.GetCell(46, 4).Objects.Any(e => e.HasPart<CultivatedSoilPart>() && e.HasTag("Plantable")));
            Assert.False(zone.GetCell(46, 4).Objects.Any(e => e.BlueprintName == "Rubble"), "Garden must be planned before gravel dressing.");
            Assert.AreEqual(3, zone.GetReadOnlyEntities().Count(e => e.HasPart<FieldHarvestPart>()));
            Assert.AreEqual(3, zone.GetReadOnlyEntities().Count(e => e.BlueprintName.StartsWith("Marlback", StringComparison.Ordinal)));
            Assert.True(GleanersDistrict.CanBuildCellar(manager));
        }

        [TestCase("missing")]
        [TestCase("v10")]
        [TestCase("v11")]
        public void OldManifestKeepsOriginalWallAndDoesNotReceiveNewGardenOrPallet(string mode)
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            RestoreOld(manager, mode);
            var surface = manager.GetZone(ReferenceGladePlan.ZoneID);
            Assert.True(surface.GetCell(46, 6).Objects.Any(e => e.BlueprintName == "Wall"));
            Assert.False(surface.GetReadOnlyEntities().Any(e => e.BlueprintName == WicketBlueprint || e.BlueprintName == "DrawgourdCrop"));
            var cellar = manager.GetZone(GleanersCellarBuilder.ZoneID);
            Assert.False(cellar.GetReadOnlyEntities().Any(e => e.BlueprintName == PalletBlueprint));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealRepairEnablesAUsefulLocalShortcutWhileUnpaidRepairKeepsTheBypass(bool supplyTimber)
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            var zone = manager.GetZone(ReferenceGladePlan.ZoneID);
            var wicket = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == WicketBlueprint);
            Assert.NotNull(wicket);
            var actor = content.Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(actor, 46, 7));
            var before = FindPath.Search(zone, 46, 7, 46, 5);
            Assert.True(before.Usable, "The broken wicket cannot make repair mandatory.");
            Assert.GreaterOrEqual(before.Steps.Count, 8, "Use actual eight-direction navigation, not an invented cardinal detour.");
            Assert.False(wicket.GetPart<DoorPart>().TrySetOpen(actor, zone, true));
            if (supplyTimber)
                for (int i = 0; i < 2; i++) Assert.True(actor.GetPart<InventoryPart>().AddObject(content.Factory.CreateEntity("SalvagedTimber")));
            Assert.AreEqual(supplyTimber, wicket.GetPart<RepairablePart>().TryRepair(actor, zone));
            Assert.AreEqual(supplyTimber, wicket.GetPart<DoorPart>().TrySetOpen(actor, zone, true));
            var after = FindPath.Search(zone, 46, 7, 46, 5);
            Assert.True(after.Usable);
            Assert.AreEqual(supplyTimber ? 2 : before.Steps.Count, after.Steps.Count);
            if (supplyTimber)
            {
                Assert.True(MovementSystem.TryMove(actor, zone, 0, -1));
                Assert.True(MovementSystem.TryMove(actor, zone, 0, -1));
                Assert.AreEqual((46, 5), zone.GetEntityPosition(actor));
            }
        }

        [Test]
        public void ActualCellarPalletIsFiniteAndDoesNotReplaceOriginalGuardBeamOrSupplies()
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            manager.GetZone(ReferenceGladePlan.ZoneID);
            var zone = manager.GetZone(GleanersCellarBuilder.ZoneID);
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == PalletBlueprint));
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "guard"));
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "beam"));
            var store = zone.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "supplies");
            Assert.AreEqual(2, store.GetPart<ContainerPart>().Contents.Where(e => e.BlueprintName == "FireClay").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
        }

        [Test]
        public void CachedGladeIsReturnedLiterallyEvenInANewWorld()
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            var zone = new Zone(ReferenceGladePlan.ZoneID);
            var owner = content.Factory.CreateEntity("Grass");
            Assert.True(zone.AddEntity(owner, 40, 12));
            manager.CachedZones[zone.ZoneID] = zone;
            Assert.AreSame(zone, manager.GetZone(zone.ZoneID));
            CollectionAssert.AreEquivalent(new[] { owner }, zone.GetReadOnlyEntities());
        }
    }
}
