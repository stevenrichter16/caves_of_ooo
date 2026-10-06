using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SecondExplorationSitesTests : SecondExplorationFixture
    {
        [TestCase(false)] [TestCase(true)]
        public void RecoverExactJarCreatesHandLightOrRefusesFullPack(bool full)
        {
            var jar = Place("BeetleJar"); Add(jar, "RecoverableLampPart"); var light = jar.GetPart<LightSourcePart>(); Assert.NotNull(light);
            if (full) Pack.MaxWeight = 0;
            Assert.AreEqual(!full, Act(jar, "RecoverLamp"));
            Assert.AreSame(light, jar.GetPart<LightSourcePart>());
            Assert.AreEqual(full, Zone.GetEntityCell(jar) != null); Assert.AreEqual(!full, Pack.Objects.Contains(jar));
            if (!full) { Assert.True(InventorySystem.Equip(Actor, jar)); Assert.NotNull(Pack.FindEquippedBodyPart(jar)); }
        }
        [TestCase(false)] [TestCase(true)]
        public void SalvageOnlyJammedSpikeMechanismOnce(bool armed)
        {
            var trap = Place("SpikeTrap"); Add(trap, "TrapSalvagePart"); trap.GetPart<TrapJammingPart>().Jammed = !armed;
            Assert.AreEqual(!armed, Act(trap, "SalvageJammedTrap"));
            Assert.AreEqual(armed, Zone.GetEntityCell(trap) != null); Assert.AreEqual(armed ? 0 : 1, Count("IronSpikeComponent"));
            Assert.AreEqual(0, Count("SalvagedTimber"));
            if (!armed) { Assert.False(Act(trap, "SalvageJammedTrap")); Assert.AreEqual(1, Count("IronSpikeComponent")); }
        }
        [TestCase(false)] [TestCase(true)]
        public void ClothDismantlingActuallyRemovesCoverUnlessOuterActionRefuses(bool refuse)
        {
            var screen = Place("TentWall"); Add(screen, "ReclaimClothPart"); Assert.True(screen.GetPart<PhysicsPart>().Solid);
            if (refuse) FailAfter(); Assert.AreEqual(!refuse, Act(screen, "StripClothScreen"));
            Assert.AreEqual(refuse, Zone.GetEntityCell(screen) != null); Assert.AreEqual(refuse ? 0 : 2, Count("KnotflaxCord"));
        }
        [Test] public void LoadedContainerHaulWeightTracksRealRemainingContents()
        {
            var chest = Place("Chest"); chest.AddPart(new HandlingPart { Weight = 40, Carryable = false }); Add(chest, "ContainerLoadPart");
            var goods = Factory.CreateEntity("ChoirIron"); goods.GetPart<StackerPart>().StackCount = 3;
            Assert.True(chest.GetPart<ContainerPart>().AddItem(goods));
            int cargo = HandlingService.GetWeight(goods) * 3;
            Assert.AreEqual(40 + cargo, DragRules.WeightOf(chest)); Assert.AreSame(chest, goods.GetPart<PhysicsPart>().InInventory);
            Assert.True(chest.GetPart<ContainerPart>().RemoveItem(goods)); Assert.AreEqual(40, DragRules.WeightOf(chest));
            Assert.AreEqual(3, Units(goods));
        }
        [TestCase(false)] [TestCase(true)]
        public void CollectorBarterTransfersItsExactCarriedFindOnlyWithFood(bool withoutFood)
        {
            var collector = Provider("CollectorBarterPart", "Magpie"); collector.GetPart<BrainPart>().Passive = true;
            var role = collector.GetPart<SpreadCollectorPart>(); if (role == null) { role = new SpreadCollectorPart(); collector.AddPart(role); }
            var home = Place("Chest", 14, 10); var salvage = Place("Hatchet", 12, 10);
            Assert.True(role.Configure(Zone, home, salvage)); Assert.True(Zone.RemoveEntity(salvage));
            Assert.True(collector.GetPart<InventoryPart>().AddObject(salvage)); role.Phase = SpreadCollectorPhase.Carrying;
            Assert.AreSame(salvage, role.CurrentCarriedItem);
            var food = withoutFood ? Factory.CreateEntity("CandyCarrot") : Carry("CandyCarrot");
            Assert.AreEqual(!withoutFood, Act(collector, "BarterCollector|" + Id(food)));
            Assert.AreEqual(!withoutFood, Pack.Objects.Contains(salvage));
            Assert.AreEqual(withoutFood ? SpreadCollectorPhase.Carrying : SpreadCollectorPhase.Stopped, role.Phase);
            Assert.False(home.GetPart<ContainerPart>().Contents.Contains(salvage));
        }
        [TestCase(false)] [TestCase(true)]
        public void FiniteLibraryLoanMovesExactSingletonOrRefusesMissingInk(bool broke)
        {
            var shelf = Place("Chest"); Add(shelf, "LoanShelfPart"); Add(shelf, "RentalDeskPart"); shelf.AddPart(new InventoryPart { MaxWeight = 100 });
            var book = Factory.CreateEntity("WardGleamGrimoire"); book.SetTag("Rentable"); book.GetPart<StackerPart>().MaxStack = 1;
            Assert.True(shelf.GetPart<InventoryPart>().AddObject(book)); if (broke) RentalSystem.SetInk(Actor, 0);
            int before = RentalSystem.GetInk(Actor), cost = RentalSystem.GetRentalCost(book, Actor, shelf); Assert.Greater(cost, 0);
            Assert.AreEqual(!broke, Act(shelf, "BorrowVolume|" + Id(book)));
            Assert.AreEqual(!broke, Pack.Objects.Contains(book)); Assert.AreEqual(broke, shelf.GetPart<InventoryPart>().Objects.Contains(book));
            Assert.AreEqual(before - (broke ? 0 : cost), RentalSystem.GetInk(Actor));
            if (!broke) { Assert.NotNull(book.GetPart<RentalPart>()); Assert.False(Act(shelf, "BorrowVolume|" + Id(book))); }
        }
        [Test] public void UnboundRopeCannotConsumeCordOrCreateCosmeticStairs()
        {
            var anchor = Place("RopeAnchor"); Add(anchor, "RopeShortcutPart"); Carry("KnotflaxCord"); Carry("KnotflaxCord");
            Assert.False(Act(anchor, "RigRopeShortcut")); Assert.AreEqual(2, Count("KnotflaxCord"));
            Assert.False(anchor.HasPart<StairsDownPart>()); Assert.False(anchor.HasPart<StairsUpPart>());
        }
        [Test] public void UnboundPassageCannotSellGlobalPeace()
        {
            var guard = Provider("LocalPassagePermitPart"); int purse = TradeSystem.GetDrams(Actor);
            Assert.False(Act(guard, "PermitPassage")); Assert.AreEqual(purse, TradeSystem.GetDrams(Actor));
        }
    }

    public sealed class SecondExplorationSourceTests
    {
        static bool Has(Entity e, string type) => e.Parts.Any(p => p.GetType().Name == type);
        [TestCase("Overworld.14.9.0", "ScribeCopyServicePart")]
        [TestCase("Overworld.14.9.0", "LoanShelfPart")]
        [TestCase("Overworld.6.6.0", "ArtisanRepairServicePart")]
        [TestCase("Overworld.10.14.0", "RentalDeskPart")]
        [TestCase("Overworld.10.14.0", "BurialPart")]
        [TestCase("Overworld.8.16.0", "GuestLockerPart")]
        [TestCase("Overworld.4.6.1", "RecoverableLampPart")]
        [TestCase("Overworld.2.7.1", "RopeShortcutPart")]
        [TestCase("Overworld.16.7.0", "LocalPassagePermitPart")]
        [TestCase("Overworld.17.7.0", "ContainerLoadPart")]
        [TestCase("Overworld.19.18.0", "ReclaimClothPart")]
        [TestCase("Overworld.19.18.0", "LocksmithServicePart")]
        public void FreshNativeDestinationActuallyContainsItsNewChoice(string zoneId, string type)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var zone = manager.GetZone(zoneId); Assert.NotNull(zone, "Accepted original destination remains available.");
                Assert.True(zone.GetReadOnlyEntities().Any(e => Has(e, type)), zoneId + " must physically offer " + type);
                Assert.AreSame(zone, manager.GetZone(zoneId), "Revisit uses the same graph.");
            }
        }
        [Test] public void FreshPenHasRealClosedGateAndBoundFiniteGrazer()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var zone = manager.GetZone("Overworld.12.10.0"); Assert.NotNull(zone);
                var gate = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "FrontierPenGate"); Assert.NotNull(gate);
                Assert.True(gate.GetPart<DoorPart>().IsClosed); Assert.True(zone.GetEntityCell(gate).BlocksMovement());
                var animal = zone.GetReadOnlyEntities().Single(e => e.GetProperty("SecondExploration.Role") == "pen-grazer");
                var grazing = animal.GetPart<SpreadGrazerPart>(); Assert.True(grazing.Configured);
                Assert.NotNull(zone.GetEntityCell(grazing.Food)); Assert.NotNull(zone.GetEntityCell(grazing.ReservedRow)); Assert.AreNotSame(grazing.Food, grazing.ReservedRow);
            }
        }
        [Test] public void WorksStockContainsOneOfEachPreparationGarmentWithoutSecondCache()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var zone = manager.GetZone(SoddenDistrictPlan.WorksZoneID); Assert.NotNull(zone);
                var locker = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SoddenWorksLocker");
                var contents = locker.GetPart<ContainerPart>().Contents;
                foreach (string blueprint in new[] { "FilterHood", "AcidworkerApron" }) Assert.AreEqual(1, contents.Count(e => e.BlueprintName == blueprint));
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SoddenWorksLocker"));
            }
        }
        [TestCase("Overworld.14.9.0", "LoanShelfPart")]
        [TestCase("Overworld.8.16.0", "GuestLockerPart")]
        [TestCase("Overworld.19.18.0", "ReclaimClothPart")]
        public void LiteralVersion14DoesNotAcquireNewDestinations(string id, string type)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                SoddenDistrictIntegrationTests.Version(manager, 14); var zone = manager.GetZone(id); Assert.NotNull(zone);
                Assert.False(zone.GetReadOnlyEntities().Any(e => Has(e, type)));
            }
        }
        [Test] public void DisabledLegacyManifestDoesNotInstallOrRetainNewSites()
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,false);
                Assert.False(manager.Exploration.Enabled);var zone=manager.GetZone(WellmeetCompositionPlan.ZoneID);
                Assert.False(zone.GetReadOnlyEntities().Any(e=>Has(e,"GuestLockerPart")),"A disabled manifest is not a new v15 world merely because its default version field is current.");
                Assert.False(SecondExplorationSites.Retain(manager,zone.ZoneID));
            }
        }
        [Test] public void CachedLiteralGraphIsNeverRetrofitted()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var zone = new Zone(QuillholdCompositionPlan.ZoneID); var old = scope.Factory.CreateEntity("Signpost"); Assert.True(zone.AddEntity(old, 5, 5));
                manager.CachedZones[zone.ZoneID] = zone; Assert.AreSame(zone, manager.GetZone(zone.ZoneID));
                CollectionAssert.AreEquivalent(new[] { old }, zone.GetReadOnlyEntities());
            }
        }
    }
}
