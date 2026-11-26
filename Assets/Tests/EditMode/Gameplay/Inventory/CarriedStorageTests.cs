using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CarriedStorageTests : FiftyWorldFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ExactCampBoxMovesTheSameItemAndRetainsTheSameCarriedMassAndPenalty(bool hardLimit)
        {
            var box = Carry("CampBox");
            var item = Loose("test-bulky-good", 5);
            item.AddPart(new HandlingPart { CarryMovePenalty = 3 });
            item.AddPart(new TakenCounterPart());
            Assert.True(Pack.AddObject(item));
            int total = Pack.GetCarriedWeight();
            int speed = Actor.GetStat("Speed").Penalty;
            Pack.MaxWeight = total;

            Assert.True(CarriedStorage.TryDeposit(Actor, box, item, Zone, out string failure), failure);
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(item));
            Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.False(Pack.Objects.Contains(item));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(total));
            Assert.That(Actor.GetStat("Speed").Penalty, Is.EqualTo(speed));

            // Reorganization remains possible when a saved pack already
            // exceeds a reduced capacity.
            Pack.MaxWeight = hardLimit ? total - 1 : -1;
            if (!hardLimit)
            {
                Actor.GetStat("Strength").BaseValue = 1;
                Assert.That(Pack.GetMaxCarryWeight(), Is.LessThan(total));
            }
            Assert.True(CarriedStorage.TryWithdraw(Actor, box, item, Zone, out failure), failure);
            Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(Pack.Objects.Single(owner => owner.ID == item.ID), Is.SameAs(item));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(total));
            Assert.That(Actor.GetStat("Speed").Penalty, Is.EqualTo(speed));
            Assert.That(item.GetPart<TakenCounterPart>().Count, Is.Zero);
        }

        [Test]
        public void FullStorageUsesTheCombinedAllowanceRatherThanTheOrdinarySingleLoadLift()
        {
            Actor.GetStat("Strength").BaseValue = 6;
            Pack.MaxWeight = -1;
            var satchel = Satchel(2);
            var stock = Loose("test-stored-good", 20);
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(stock));
            Assert.True(Zone.AddEntity(satchel, 10, 10));
            Assert.False(HandlingService.CanLift(Actor, satchel, out _));
            Assert.That(Pack.GetCarriedWeight() + InventoryPart.GetItemWeight(satchel), Is.LessThan(Pack.GetMaxCarryWeight()));
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(satchel), Actor, Zone).Success);
            Assert.True(CarriedStorage.IsCarried(Actor, satchel));

            var loose = Loose("test-heavy-loose-good", 20);
            Assert.True(Zone.AddEntity(loose, 10, 10));
            Assert.False(InventorySystem.ExecuteCommand(new PickupCommand(loose), Actor, Zone).Success);
        }

        [Test]
        public void OrdinaryPackInsertionAlsoHonorsTheVesselCarryLimit()
        {
            Actor.GetStat("Strength").BaseValue = 6;
            Pack.MaxWeight = -1;
            int room = Pack.GetMaxCarryWeight() - Pack.GetCarriedWeight();
            Assert.Greater(room, 1);
            Assert.True(Pack.AddObject(Loose("test-existing-load", room - 1)));
            Assert.True(Pack.AddObject(Satchel(2)));
            Assert.AreEqual(Pack.GetMaxCarryWeight(), Pack.GetCarriedWeight());
            var excess = Satchel(2);
            Assert.False(Pack.AddObject(excess));
            Assert.IsNull(excess.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void TakingStorageFromAChestUsesTheSameCombinedAllowance()
        {
            Actor.GetStat("Strength").BaseValue = 6;
            Pack.MaxWeight = -1;
            var chest = Place("Chest");
            var container = chest.GetPart<ContainerPart>();
            var satchel = Satchel(2);
            var stock = Loose("test-stored-good", 20);
            var loose = Loose("test-heavy-loose-good", 20);
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(stock));
            Assert.True(container.AddItem(satchel));
            Assert.True(container.AddItem(loose));
            Assert.False(HandlingService.CanLift(Actor, satchel, out _));
            int limit = Pack.GetMaxCarryWeight();
            var ballast = Loose("test-pack-ballast", limit - Pack.GetCarriedWeight() - 20);
            Assert.True(Pack.AddObject(ballast));

            TakeFromContainerCommand Take(Entity item) => new TakeFromContainerCommand(chest, item);
            Assert.False(InventorySystem.ExecuteCommand(Take(satchel), Actor, Zone).Success);
            Assert.That(satchel.GetPart<PhysicsPart>().InInventory, Is.SameAs(chest));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(satchel));
            ballast.GetPart<PhysicsPart>().Weight--;
            Assert.True(Zone.MoveEntity(Actor, 20, 20));
            Assert.False(InventorySystem.ExecuteCommand(Take(satchel), Actor, Zone).Success);
            Assert.True(Zone.MoveEntity(Actor, 10, 10));
            Assert.True(InventorySystem.ExecuteCommand(Take(satchel), Actor, Zone).Success);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(limit));
            Assert.True(CarriedStorage.IsCarried(Actor, satchel));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(satchel));
            Assert.False(InventorySystem.ExecuteCommand(Take(loose), Actor, Zone).Success);
            Assert.That(container.Contents.Single(), Is.SameAs(loose));
        }

        [Test]
        public void ExistingEntryLimitAllowsOnlyAFullCompatibleMerge()
        {
            var satchel = CarrySatchel(1);
            var original = Loose("test-merge-good", 2, 2);
            var second = Loose("test-merge-good", 2, 3);
            var different = Loose("test-different-good", 2);
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(original));
            Assert.True(Pack.AddObject(second));
            Assert.True(Pack.AddObject(different));
            int total = Pack.GetCarriedWeight();
            Assert.False(CarriedStorage.CanDeposit(Actor, satchel, different, Zone, out _));
            Assert.True(CarriedStorage.TryDeposit(Actor, satchel, second, Zone, out string failure), failure);
            Assert.That(satchel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(original));
            Assert.That(original.GetPart<StackerPart>().StackCount, Is.EqualTo(5));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(total));
        }

        [Test]
        public void APostMergeMassChangeRollsBackBothExactStacks()
        {
            var satchel = CarrySatchel(1);
            var original = Loose("test-merge-good", 4, 2);
            var second = Loose("test-merge-good", 2, 3);
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(original));
            Assert.True(Pack.AddObject(second));
            Assert.True(original.GetPart<StackerPart>().CanStackWith(second));
            int total = Pack.GetCarriedWeight();

            Assert.False(CarriedStorage.TryDeposit(Actor, satchel, second, Zone, out _));
            Assert.That(satchel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(original));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(total));
            Assert.That(original.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(second.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(second.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
        }

        [Test]
        public void FailedWithdrawalRestoresContentsAndTheMovementPenalty()
        {
            var satchel = CarrySatchel(2);
            var resident = Loose("test-merge-good", 4, 2);
            var stored = Loose("test-merge-good", 2, 3);
            resident.AddPart(new HandlingPart { CarryMovePenalty = 3 });
            stored.AddPart(new HandlingPart { CarryMovePenalty = 3 });
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(stored));
            Assert.True(Pack.AddObject(resident));
            Assert.True(resident.GetPart<StackerPart>().CanStackWith(stored));
            int mass = Pack.GetCarriedWeight();
            int speed = Actor.GetStat("Speed").Penalty;

            Assert.False(CarriedStorage.TryWithdraw(Actor, satchel, stored, Zone, out _));
            Assert.That(satchel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(satchel));
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(mass));
            Assert.That(Actor.GetStat("Speed").Penalty, Is.EqualTo(speed));
        }

        [Test]
        public void StaleLockWrongZoneNestedStorageAndAliasAreRefused()
        {
            var satchel = CarrySatchel(6);
            var item = Loose("test-good", 2);
            Assert.True(Pack.AddObject(item));
            Assert.True(CarriedStorage.CanDeposit(Actor, satchel, item, Zone, out _));
            Assert.False(CarriedStorage.TryDeposit(Actor, satchel, item, new Zone(Zone.ZoneID), out _));
            Assert.False(CarriedStorage.TryDeposit(Actor, satchel, satchel, Zone, out _));
            Assert.False(CarriedStorage.TryDeposit(Actor, satchel, CarrySatchel(2), Zone, out _));
            Assert.False(CarriedStorage.IsEligible(Carry("LiningClayParcel")));
            satchel.GetPart<ContainerPart>().Locked = true;
            Assert.False(CarriedStorage.TryDeposit(Actor, satchel, item, Zone, out _));
            satchel.GetPart<ContainerPart>().Locked = false;
            Assert.True(CarriedStorage.TryDeposit(Actor, satchel, item, Zone, out string failure), failure);
            Pack.Objects.Add(item);
            Assert.False(CarriedStorage.IsCarried(Actor, satchel));
            Assert.False(CarriedStorage.TryWithdraw(Actor, satchel, item, Zone, out _));
            Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(satchel));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OrdinaryPickupCountsShellAndContentsAtTheLowerApplicableLimit(bool hardLimit)
        {
            var satchel = Satchel(2);
            var stock = Loose("test-stored-good", 4);
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(stock));
            Assert.True(Zone.AddEntity(satchel, 10, 10));
            int strengthLimit = Pack.GetMaxCarryWeight();
            Assert.That(strengthLimit, Is.GreaterThan(11));
            int limit = hardLimit ? strengthLimit - 5 : strengthLimit;
            Pack.MaxWeight = hardLimit ? limit : -1;
            var weights = Loose("test-pack-ballast", limit - Pack.GetCarriedWeight() - 4);
            Assert.True(Pack.AddObject(weights));

            Assert.False(InventorySystem.ExecuteCommand(new PickupCommand(satchel), Actor, Zone).Success);
            Assert.That(Zone.GetEntityCell(satchel), Is.Not.Null);
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(satchel));
            weights.GetPart<PhysicsPart>().Weight--;
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(satchel), Actor, Zone).Success);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(limit));
            Assert.True(CarriedStorage.IsCarried(Actor, satchel));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CampBoxTakeUpUsesTheSameLowerLimitWithoutSpilling(bool hardLimit)
        {
            var box = Place("CampBox");
            var stock = Loose("test-stored-good", 4);
            Assert.True(box.GetPart<ContainerPart>().AddItem(stock));
            Stand(box);
            int boxWeight = InventoryPart.GetItemWeight(box);
            int strengthLimit = Pack.GetMaxCarryWeight();
            int limit = hardLimit ? strengthLimit - 5 : strengthLimit;
            Pack.MaxWeight = hardLimit ? limit : -1;
            var weights = Loose("test-pack-ballast", limit - Pack.GetCarriedWeight() - boxWeight + 1);
            Assert.True(Pack.AddObject(weights));
            string command = "TakeUp|" + Uri.EscapeDataString(Zone.ZoneID) + "|" + box.ID;

            Assert.False(Act(box, command));
            Assert.That(Zone.GetEntityCell(box), Is.Not.Null);
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
            weights.GetPart<PhysicsPart>().Weight--;
            Assert.True(Act(box, command));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(limit));
            Assert.True(CarriedStorage.IsCarried(Actor, box));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
        }

        [Test]
        public void PackingAWholeCampWithStorageUsesTheTotalLoadRegardlessOfFixtureOrder()
        {
            var box = Place("CampBox", 11, 10);
            var bench = Place("CampBench", 12, 10);
            Stand(box);
            Stand(bench);
            int limit = Pack.GetMaxCarryWeight();
            int campWeight = InventoryPart.GetItemWeight(box) + InventoryPart.GetItemWeight(bench);
            Pack.MaxWeight = -1;
            var ballast = Loose("test-pack-ballast", limit - Pack.GetCarriedWeight() - campWeight + 1);
            Assert.True(Pack.AddObject(ballast));
            string command = "PackUp|" + Uri.EscapeDataString(Zone.ZoneID) + "|" + box.ID;

            Assert.False(Act(box, command));
            Assert.That(Zone.GetEntityCell(box), Is.Not.Null);
            Assert.That(Zone.GetEntityCell(bench), Is.Not.Null);
            ballast.GetPart<PhysicsPart>().Weight--;
            Assert.True(Act(box, command));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(limit));
            Assert.That(Pack.Objects, Does.Contain(bench));
            Assert.True(CarriedStorage.IsCarried(Actor, box));
        }

        [Test]
        public void SaturatedWeightDoesNotWrapIntoRoomForAnotherItem()
        {
            Pack.MaxWeight = -1;
            Assert.True(Pack.AddObject(Loose("test-huge-stack", int.MaxValue, 2)));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(int.MaxValue));
            Pack.MaxWeight = int.MaxValue;
            Assert.False(Pack.AddObject(Loose("test-one-more", 1)));
        }

        Entity CarrySatchel(int slots)
        {
            var result = Satchel(slots);
            Assert.True(Pack.AddObject(result));
            return result;
        }

        static Entity Satchel(int slots)
        {
            var vessel = Loose("FieldSatchel", 1);
            vessel.AddPart(new ContainerPart { MaxItems = slots });
            vessel.AddPart(new ContainerLoadPart());
            return vessel;
        }

        static void Stand(Entity vessel)
        {
            vessel.GetPart<PhysicsPart>().Solid = true;
            vessel.GetPart<PhysicsPart>().Takeable = false;
            vessel.AddPart(new InstallablePart { State = InstallablePart.Installed });
            vessel.AddPart(new InstallationMarkerPart { Origin = HeldFixtureActions.PlayerOrigin, RetainZone = true });
        }

        static Entity Loose(string blueprint, int weight, int count = 1)
        {
            var good = new Entity { BlueprintName = blueprint, ID = Guid.NewGuid().ToString("N") };
            good.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            if (count > 1) good.AddPart(new StackerPart { StackCount = count, MaxStack = 20 });
            return good;
        }

        sealed class TakenCounterPart : Part
        {
            public int Count;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Taken") Count++;
                return true;
            }
        }
    }
}
