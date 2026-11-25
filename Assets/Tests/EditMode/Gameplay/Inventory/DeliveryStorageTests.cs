using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DeliveryStorageTests : FiftyWorldFixture
    {
        private const string Parcel = "LiningClayParcel";
        private const string Shelf = "LiningClayReceivingShelf";
        private const string Prepared = "PreparedLiningClay";
        private const string Tray = "LiningClaySupplyTray";
        private const string TakeOnePrefix = "TakeOneLooseClay|";

        [Test]
        public void MixedClayRemainsPhysicalThroughSelfCarryAndRestoresTheWellAfterRetrieval()
        {
            var parcel = Place(Parcel, 10, 10);
            var shelf = Place(Shelf, 21, 10);
            var well = Place("RepairLinedWell", 20, 9);
            var prepared = Carry(Prepared);
            var fireClay = Carry("FireClay");
            Assert.That(parcel.GetPart<DeliveryStoragePart>().ReceivesParcels, Is.False);
            Assert.That(shelf.GetPart<DeliveryStoragePart>().ReceivesParcels, Is.True);
            Assert.That(parcel.GetPart<ContainerLoadPart>(), Is.Not.Null);
            Assert.That(HandlingService.GetWeight(parcel), Is.EqualTo(3));
            Assert.That(Put(parcel, prepared).Success, Is.True);
            Assert.That(Put(parcel, fireClay).Success, Is.True);
            Assert.That(HandlingService.GetWeight(parcel), Is.EqualTo(5));
            Assert.That(prepared.GetPart<PhysicsPart>().InInventory, Is.SameAs(parcel));
            Assert.That(fireClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(parcel));
            Assert.That(Pick(parcel).Success, Is.True);
            Assert.That(Zone.MoveEntity(Actor, 20, 10), Is.True);

            Assert.That(Put(shelf, parcel).Success, Is.True);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.EqualTo(new[] { parcel }));
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.EquivalentTo(new[] { prepared, fireClay }));
            Assert.That(parcel.GetPart<PhysicsPart>().InInventory, Is.SameAs(shelf));
            Assert.That(Pack.Contains(parcel), Is.False);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);

            Assert.That(Take(shelf, parcel).Success, Is.True);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(InventorySystem.ExecuteCommand(new DropCommand(parcel), Actor, Zone).Success, Is.True);
            Assert.That(Take(parcel, prepared).Success, Is.True);
            Assert.That(Take(parcel, fireClay).Success, Is.True);
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(prepared.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(fireClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.True);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.True);
            Assert.That(Pack.Contains(prepared) || Pack.Contains(fireClay), Is.False);
        }

        [TestCase("DryClaySpoil", 1)]
        [TestCase(Prepared, 3)]
        public void ParcelRefusesInvalidOrOversizedCargoAndKeepsTheExactOwner(string blueprint, int quantity)
        {
            var parcel = Place(Parcel, 10, 10);
            var stock = Carry(blueprint);
            stock.GetPart<StackerPart>().StackCount = quantity;

            Assert.That(Put(parcel, stock).Success, Is.False);
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(stock.GetPart<StackerPart>().StackCount, Is.EqualTo(quantity));
        }

        [Test]
        public void ShelfRefusesPartialParcelAndLooseClay()
        {
            var parcel = Place(Parcel, 10, 10);
            var shelf = Place(Shelf, 11, 10);
            var packed = Carry(Prepared);
            Assert.That(Put(parcel, packed).Success, Is.True);
            Assert.That(Pick(parcel).Success, Is.True);
            var loose = Carry(Prepared);

            Assert.That(Put(shelf, parcel).Success, Is.False);
            Assert.That(Put(shelf, loose).Success, Is.False);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(packed.GetPart<PhysicsPart>().InInventory, Is.SameAs(parcel));
            Assert.That(loose.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(parcel.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
        }

        [Test]
        public void LooseTrayMergesTwoMeasuresAndNeverConsumesAThird()
        {
            var tray = PlaceLooseTray();
            var storage = tray.GetPart<DeliveryStoragePart>();
            var container = tray.GetPart<ContainerPart>();
            var first = Carry(Prepared);
            Assert.That(Put(tray, first).Success, Is.True);
            var second = Carry(Prepared);
            Assert.That(Put(tray, second).Success, Is.True);
            Assert.That(container.Contents.Single(), Is.SameAs(first));
            Assert.That(first.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(first.GetPart<PhysicsPart>().InInventory, Is.SameAs(tray));
            Assert.That(second.GetPart<StackerPart>().StackCount, Is.Zero);
            Assert.That(second.GetPart<PhysicsPart>().InInventory, Is.Null);

            var resident = Carry(Prepared);
            Assert.That(Put(tray, resident).Success, Is.False, "The existing stack occupies both measures.");
            Assert.That(container.Contents.Single(), Is.SameAs(first));
            Assert.That(first.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(resident.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == Prepared), Is.False);

            int normalCapacity = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight() + 1;
            Assert.That(Take(tray, first).Success, Is.False);
            Assert.That(container.Contents.Single(), Is.SameAs(first));
            Assert.That(first.GetPart<PhysicsPart>().InInventory, Is.SameAs(tray));
            Assert.That(first.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(storage.ContentsWithdrawn, Is.False);

            Pack.MaxWeight = normalCapacity;
            Assert.That(Take(tray, first).Success, Is.True);
            Assert.That(Take(tray, first).Success, Is.False);
            Assert.That(container.Contents, Is.Empty);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == Prepared), Is.SameAs(resident));
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(3));

            var split = new SeparateOneCommand(resident);
            Assert.That(InventorySystem.ExecuteCommand(split, Actor, Zone).Success, Is.True);
            var returned = split.SeparatedItem;
            Assert.That(Put(tray, returned).Success, Is.True);
            Assert.That(Take(tray, returned).Success, Is.True);
            Assert.That(container.Contents, Is.Empty);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == Prepared), Is.SameAs(resident));
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(storage.ContentsWithdrawn, Is.False, "The tray does not track continuity of sealed cargo.");
        }

        [Test]
        public void LooseTrayAcceptsMixedLiningClayWhileRefusingSpoilAndPackedParcels()
        {
            var tray = PlaceLooseTray();
            var container = tray.GetPart<ContainerPart>();
            var well = Place("RepairLinedWell", 10, 11);
            var spoil = Carry("DryClaySpoil");
            var parcel = PackAndPickTwoMeasures();
            var parcelStock = parcel.GetPart<ContainerPart>().Contents.Single();

            Assert.That(Put(tray, spoil).Success, Is.False);
            Assert.That(Put(tray, parcel).Success, Is.False);
            Assert.That(container.Contents, Is.Empty);
            Assert.That(spoil.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(parcel.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(parcel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(parcelStock));
            Assert.That(parcelStock.GetPart<PhysicsPart>().InInventory, Is.SameAs(parcel));
            Assert.That(parcelStock.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(Zone.GetReadOnlyEntities().Any(e => e == spoil || e == parcel || e == parcelStock), Is.False);

            var prepared = Carry(Prepared);
            var fireClay = Carry("FireClay");
            Assert.That(Put(tray, prepared).Success, Is.True);
            Assert.That(Put(tray, fireClay).Success, Is.True);
            Assert.That(container.Contents, Is.EquivalentTo(new[] { prepared, fireClay }));
            Assert.That(container.Contents.All(e => e.GetPart<PhysicsPart>().InInventory == tray), Is.True);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);
            Assert.That(Take(tray, prepared).Success, Is.True);
            Assert.That(Take(tray, fireClay).Success, Is.True);
            Assert.That(container.Contents, Is.Empty);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.True);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.True);
            Assert.That(parcel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(parcelStock));
            Assert.That(parcelStock.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
        }

        [Test]
        public void OnePoundOfRoomTakesOneOriginalMarkedMeasureAndLeavesTheOtherInTheTray()
        {
            using var content = new HaulingContentScope();
            content.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            var surface = manager.GetZone(GleanersDistrict.SurfaceID);
            var cellar = manager.GetZone(GleanersCellarBuilder.ZoneID);
            var cache = cellar.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "supplies");
            var stock = cache.GetPart<ContainerPart>().Contents.Single(e => e.BlueprintName == "FireClay");
            var origin = stock.GetPart<ConnectedClayOriginPart>();
            Assert.That(origin.Units, Is.EqualTo(3));
            Assert.That(origin.WorldKey, Is.EqualTo(manager.Exploration.WorldKey));
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = cellar;
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            var cacheCell = cellar.GetEntityPosition(cache);
            Assert.True(cellar.AddEntity(Actor, cacheCell.x - 1, cacheCell.y));
            Assert.True(Take(cache, stock).Success);
            Assert.True(cellar.TryTransferEntityTo(Actor, surface, 42, 10));
            Zone = surface;
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            var tray = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Tray);
            var trayCell = Zone.GetEntityCell(tray);
            trayCell.IsVisible = trayCell.Explored = true;
            var contents = tray.GetPart<ContainerPart>().Contents;
            Assert.True(Put(tray, stock).Success);
            Pack.MaxWeight = Pack.GetCarriedWeight() + 1;
            int weightBefore = Pack.GetCarriedWeight();
            int money = TradeSystem.GetDrams(Actor);
            int experience = Actor.GetStatValue("Experience");
            Assert.That(CellMenu(42, 9).Any(a => a.Command == "OpenContainer"), Is.True);
            Assert.False(Take(tray, stock).Success, "Ordinary take still needs room for both pounds.");

            var row = TakeOneRows(42, 9).Single();
            Assert.That(row.Display + " " + row.Hint, Does.Contain("1 lb"));
            Assert.True(ChooseRow(row, 42, 9));
            Assert.That(contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(tray));
            Assert.That(stock.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(origin.OwnerID, Is.EqualTo(stock.ID));
            Assert.That(origin.Units, Is.EqualTo(2));
            var carried = Pack.Objects.Single(e => e.BlueprintName == "FireClay");
            var carriedOrigin = carried.GetPart<ConnectedClayOriginPart>();
            Assert.That(carried.ID, Is.Not.EqualTo(stock.ID));
            Assert.That(carried.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(carried.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(carriedOrigin.OwnerID, Is.EqualTo(carried.ID));
            Assert.That(carriedOrigin.WorldKey, Is.EqualTo(origin.WorldKey));
            Assert.That(carriedOrigin.Units, Is.EqualTo(1));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(weightBefore + 1));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(money));
            Assert.That(Actor.GetStatValue("Experience"), Is.EqualTo(experience));
            Assert.That(tray.GetPart<DeliveryStoragePart>().ContentsWithdrawn, Is.False);
            Assert.That(TakeOneRows(42, 9), Is.Empty);
            Assert.That(CellMenu(42, 9).Any(a => a.Command == "OpenContainer"), Is.True);
        }

        [Test]
        public void TakingOneMeasureMergesIntoTheExistingPackStackWithoutChangingTotalMass()
        {
            var tray = Place(Tray, 11, 10);
            var trayCell = Zone.GetEntityCell(tray);
            trayCell.IsVisible = trayCell.Explored = true;
            var stock = Carry(Prepared);
            stock.GetPart<StackerPart>().StackCount = 2;
            Assert.True(Put(tray, stock).Success);
            var resident = Carry(Prepared);
            int carriedBefore = Pack.GetCarriedWeight();
            int totalBefore = carriedBefore + InventoryPart.GetItemWeight(stock);
            Pack.MaxWeight = carriedBefore + 1;

            var row = TakeOneRows(11, 10).Single();
            Assert.True(ChooseRow(row, 11, 10));
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == Prepared), Is.SameAs(resident));
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(resident.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(resident.GetPart<ConnectedClayOriginPart>(), Is.Null);
            Assert.That(tray.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(tray));
            Assert.That(stock.GetPart<ConnectedClayOriginPart>(), Is.Null);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(carriedBefore + 1));
            Assert.That(Pack.GetCarriedWeight() + InventoryPart.GetItemWeight(stock), Is.EqualTo(totalBefore));
        }

        [Test]
        public void AlteredPreparedGradeHidesTakeOneAndRefusesThePreviouslyValidRow()
        {
            var tray = Place(Tray, 11, 10);
            var trayCell = Zone.GetEntityCell(tray);
            trayCell.IsVisible = trayCell.Explored = true;
            var stock = Carry(Prepared);
            stock.GetPart<StackerPart>().StackCount = 2;
            Assert.True(Put(tray, stock).Success);
            var row = TakeOneRows(11, 10).Single();
            stock.GetPart<RepairMaterialPart>().Grade = "raw";

            Assert.That(TakeOneRows(11, 10), Is.Empty);
            Assert.False(ChooseRow(row, 11, 10));
            Assert.That(tray.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(stock.GetPart<RepairMaterialPart>().Grade, Is.EqualTo("raw"));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(tray));
            Assert.That(Pack.Objects.Any(e => e.BlueprintName == Prepared), Is.False);
        }

        [Test]
        public void AlteredRawWeightKeepsOrdinaryTakeButHidesTheOneMeasureQuote()
        {
            var tray = Place(Tray, 11, 10);
            var cell = Zone.GetCell(11, 10);
            cell.IsVisible = cell.Explored = true;
            var stock = Carry("FireClay");
            stock.GetPart<StackerPart>().StackCount = 2;
            Assert.True(Put(tray, stock).Success);
            var old = TakeOneRows(11, 10).Single();

            stock.GetPart<PhysicsPart>().Weight = 2;
            Assert.That(TakeOneRows(11, 10), Is.Empty);
            Assert.False(ChooseRow(old, 11, 10));
            Assert.That(tray.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.True(Take(tray, stock).Success);
        }

        [Test]
        public void OldTakeOneQuoteCannotWithdrawFromAnEquivalentReplacementStack()
        {
            var tray = Place(Tray, 11, 10);
            var trayCell = Zone.GetEntityCell(tray);
            trayCell.IsVisible = trayCell.Explored = true;
            var original = Carry(Prepared);
            original.GetPart<StackerPart>().StackCount = 2;
            Assert.True(Put(tray, original).Success);
            var old = TakeOneRows(11, 10).Single();
            Assert.True(Take(tray, original).Success);
            Assert.True(InventorySystem.ExecuteCommand(new DropCommand(original), Actor, Zone).Success);
            var replacement = Carry(Prepared);
            replacement.GetPart<StackerPart>().StackCount = 2;
            Assert.True(Put(tray, replacement).Success);

            Assert.False(ChooseRow(old, 11, 10));
            Assert.That(tray.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(replacement));
            Assert.That(replacement.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(replacement.GetPart<PhysicsPart>().InInventory, Is.SameAs(tray));
            Assert.That(original.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(Zone.GetEntityCell(original), Is.SameAs(Zone.GetEntityCell(Actor)));
            Assert.That(Pack.Objects.Any(e => e.BlueprintName == Prepared), Is.False);
            var fresh = TakeOneRows(11, 10).Single();
            Assert.That(fresh.Command, Is.Not.EqualTo(old.Command));
            Assert.True(ChooseRow(fresh, 11, 10));
            Assert.That(replacement.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void PackedBoxAndReceivingShelfKeepTheirOrdinaryOpenRowsWithoutLooseTakeOne()
        {
            var parcel = Place(Parcel, 10, 10);
            var shelf = Place(Shelf, 11, 10);
            var stock = Carry(Prepared);
            stock.GetPart<StackerPart>().StackCount = 2;
            Assert.True(Put(parcel, stock).Success);
            Assert.That(CellMenu(10, 10).Any(a => a.Command == "OpenContainer"), Is.True);
            Assert.That(TakeOneRows(10, 10), Is.Empty);
            Assert.True(Pick(parcel).Success);
            Assert.True(Put(shelf, parcel).Success);
            Assert.That(CellMenu(11, 10).Any(a => a.Command == "OpenContainer"), Is.True);
            Assert.That(TakeOneRows(11, 10), Is.Empty);
            Assert.That(shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(parcel));
            Assert.That(parcel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DirectContainerCommandsRefuseRemoteOrMismatchedZoneAccess(bool wrongZone)
        {
            var parcel = Place(Parcel, 10, 10);
            var stocked = Carry(Prepared);
            Assert.That(Put(parcel, stocked).Success, Is.True);
            var spare = Carry("FireClay");
            var commandZone = Zone;
            if (wrongZone)
                commandZone = new Zone(Zone.ZoneID);
            else
                Assert.That(Zone.MoveEntity(Actor, 20, 10), Is.True);

            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(parcel, spare), Actor, commandZone).Success, Is.False);
            Assert.That(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(parcel, stocked), Actor, commandZone).Success, Is.False);
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.EqualTo(new[] { stocked }));
            Assert.That(spare.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
        }

        [Test]
        public void FullShelfKeepsTheFirstParcelAndItsCargo()
        {
            var shelf = Place(Shelf, 11, 10);
            var first = PackAndPickTwoMeasures();
            var second = PackAndPickTwoMeasures();
            var firstClay = first.GetPart<ContainerPart>().Contents[0];
            var secondClay = second.GetPart<ContainerPart>().Contents[0];
            Assert.That(Put(shelf, first).Success, Is.True);

            Assert.That(Put(shelf, second).Success, Is.False);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.EqualTo(new[] { first }));
            Assert.That(firstClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(first));
            Assert.That(secondClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(second));
            Assert.That(second.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
        }

        [Test]
        public void PackingRefusesClayWithSameIdGroundAliasElsewhere()
        {
            var parcel = Place(Parcel, 10, 10);
            var carried = Carry(Prepared);
            var alias = Factory.CreateEntity(Prepared);
            alias.ID = carried.ID;
            Assert.That(Zone.AddEntity(alias, 20, 10), Is.True);

            Assert.That(Put(parcel, carried).Success, Is.False);
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(carried.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(Pack.Objects.Single(e => e.ID == carried.ID), Is.SameAs(carried));
            Assert.That(Zone.GetCell(20, 10).Objects.Single(e => e.ID == carried.ID), Is.SameAs(alias));
        }

        [Test]
        public void ThrowingTakenObserverCannotUndoTheParcelDeathDrop()
        {
            var shelf = Place(Shelf, 11, 10);
            var parcel = PackAndPickTwoMeasures();
            var stock = parcel.GetPart<ContainerPart>().Contents.Single();
            Assert.That(Put(shelf, parcel).Success, Is.True);
            var dropCell = Zone.GetEntityCell(Actor);
            bool observedInPack = false;
            parcel.AddPart(new TakenObserver(() =>
            {
                observedInPack = parcel.GetPart<PhysicsPart>().InInventory == Actor;
                CombatSystem.HandleDeath(Actor, null, Zone);
                throw new InvalidOperationException("parcel observer failed after death");
            }));

            Assert.That(Take(shelf, parcel).Success, Is.True);
            Assert.That(observedInPack, Is.True);
            Assert.That(CombatSystem.IsDeathHandled(Actor), Is.True);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(Pack.Contains(parcel), Is.False);
            Assert.That(Zone.GetEntityCell(parcel), Is.SameAs(dropCell));
            Assert.That(Zone.GetReadOnlyEntities().Single(e => e.ID == parcel.ID), Is.SameAs(parcel));
            Assert.That(parcel.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(parcel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(parcel));
        }

        [Test]
        public void FreshGleanersReceivesTheParcelAndSaveKeepsItsExactContents()
        {
            using var content = new HaulingContentScope();
            content.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            Zone = manager.GetZone(GleanersDistrict.SurfaceID);
            Assert.That(Zone, Is.Not.Null);
            manager.SetActiveZone(Zone);
            var owners = Zone.GetReadOnlyEntities();
            var parcel = owners.Single(e => e.BlueprintName == Parcel);
            var shelf = owners.Single(e => e.BlueprintName == Shelf);
            var well = owners.Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "well");
            var stairs = owners.Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "stairs");
            var source = Zone.GetEntityPosition(parcel);
            var destination = Zone.GetEntityPosition(shelf);
            Assert.That(Zone.GetEntityPosition(well), Is.EqualTo((41, 8)));
            Assert.That(Zone.GetEntityPosition(stairs), Is.EqualTo((GleanersDistrict.EntranceX, GleanersDistrict.EntranceY)));
            Assert.That(Distance(source, Zone.GetEntityPosition(stairs)), Is.EqualTo(1));
            Assert.That(Distance(destination, Zone.GetEntityPosition(well)), Is.EqualTo(1));
            Assert.That(FindPath.Search(Zone, source.x, source.y, destination.x, destination.y).Usable, Is.True);
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(well.GetPart<RepairablePart>().RecipeId, Is.EqualTo("clay-well-lining"));

            Actor = content.Factory.CreateEntity("Player");
            Assert.That(Zone.AddEntity(Actor, source.x, source.y), Is.True);
            var clay = content.Factory.CreateEntity(Prepared);
            clay.GetPart<StackerPart>().StackCount = 2;
            Assert.That(Pack.AddObject(clay), Is.True);
            Assert.That(Put(parcel, clay).Success, Is.True);
            Assert.That(Pick(parcel).Success, Is.True);
            Assert.That(Zone.MoveEntity(Actor, destination.x, destination.y), Is.True);
            Assert.That(Put(shelf, parcel).Success, Is.True);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);

            using var stream = new MemoryStream();
            var session = GameSessionState.Capture("delivery-storage", "native-receipt", manager, null, Actor);
            session.Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, content.Factory));
            Zone = loaded.ZoneManager.ActiveZone;
            Actor = loaded.Player;
            var savedShelf = Zone.GetReadOnlyEntities().Single(e => e.ID == shelf.ID);
            var savedParcel = savedShelf.GetPart<ContainerPart>().Contents.Single();
            var savedClay = savedParcel.GetPart<ContainerPart>().Contents.Single();
            Assert.That(savedParcel.ID, Is.EqualTo(parcel.ID));
            Assert.That(savedClay.ID, Is.EqualTo(clay.ID));
            Assert.That(savedClay.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(savedParcel.GetPart<PhysicsPart>().InInventory, Is.SameAs(savedShelf));
            Assert.That(savedClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(savedParcel));
            Assert.That(HandlingService.GetWeight(savedParcel), Is.EqualTo(5));
            Assert.That(Zone.GetEntityPosition(savedShelf), Is.EqualTo(destination));
            Assert.That(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == Parcel), Is.False);
            Assert.That(Zone.GetReadOnlyEntities().Single(e => e.ID == well.ID).GetPart<WellPart>().IsUsable, Is.False);
            Assert.That(Take(savedShelf, savedParcel).Success, Is.True);
            Assert.That(savedParcel.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(savedShelf.GetPart<ContainerPart>().Contents, Is.Empty);
        }

        private static int Distance((int x, int y) a, (int x, int y) b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));

        private InventoryAction[] TakeOneRows(int x, int y) => CellMenu(x, y)
            .Where(a => a.Command?.StartsWith(TakeOnePrefix, StringComparison.Ordinal) == true).ToArray();

        private Entity PlaceLooseTray()
        {
            var tray = Place(Shelf, 11, 10);
            tray.GetPart<ContainerPart>().MaxItems = 2;
            var storage = tray.GetPart<DeliveryStoragePart>();
            storage.ReceivesParcels = false;
            Assert.That(tray.GetPart<PhysicsPart>().Takeable, Is.False);
            var mode = typeof(DeliveryStoragePart).GetField("AcceptsLooseStock");
            Assert.That(mode, Is.Not.Null, "Fixed loose storage needs its own explicit admission mode.");
            mode.SetValue(storage, true);
            return tray;
        }

        private Entity PackAndPickTwoMeasures()
        {
            var parcel = Place(Parcel, 10, 10);
            var clay = Carry(Prepared);
            clay.GetPart<StackerPart>().StackCount = 2;
            Assert.That(Put(parcel, clay).Success, Is.True);
            Assert.That(Pick(parcel).Success, Is.True);
            return parcel;
        }

        private InventoryCommandResult Put(Entity container, Entity item) =>
            InventorySystem.ExecuteCommand(new PutInContainerCommand(container, item), Actor, Zone);

        private InventoryCommandResult Take(Entity container, Entity item) =>
            InventorySystem.ExecuteCommand(new TakeFromContainerCommand(container, item), Actor, Zone);

        private InventoryCommandResult Pick(Entity item) =>
            InventorySystem.ExecuteCommand(new PickupCommand(item, autoEquip: false), Actor, Zone);

        private sealed class TakenObserver : Part
        {
            private readonly Action callback;

            public TakenObserver(Action callback) => this.callback = callback;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Taken") callback();
                return true;
            }
        }
    }
}
