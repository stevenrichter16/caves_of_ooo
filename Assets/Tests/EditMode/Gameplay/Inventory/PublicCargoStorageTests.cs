using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PublicCargoStorageTests : FiftyWorldFixture
    {
        const int ShellWeight = 12;

        Entity Crate(int x, int y, bool publicCargo = true)
        {
            var crate = Place("Crate", x, y);
            if (publicCargo)
            {
                crate.AddPart(new HandlingPart
                {
                    Carryable = false, Throwable = false, GripType = GripType.TwoHand, Weight = ShellWeight
                });
                crate.AddPart(new ContainerLoadPart());
                // Leave the fixture runnable against the pre-feature runtime so
                // its red run exercises the actual old menu and transfer paths.
                var marker = typeof(Part).Assembly.GetType("CavesOfOoo.Core.PublicCargoStoragePart");
                if (marker != null) crate.AddPart((Part)Activator.CreateInstance(marker, nonPublic: true));
            }
            Show(x, y);
            Show(10, 10);
            return crate;
        }

        void Show(int x, int y)
        {
            var cell = Zone.GetCell(x, y);
            cell.Explored = cell.IsVisible = true;
        }

        Entity FillSlotsWithMergeRoom(ContainerPart container)
        {
            var resident = Factory.CreateEntity("FireClay");
            resident.GetPart<StackerPart>().StackCount = 2;
            Assert.True(container.AddItem(resident));
            for (int slot = 1; slot < container.MaxItems; slot++)
            {
                var cord = Factory.CreateEntity("KnotflaxCord");
                cord.GetPart<StackerPart>().MaxStack = 1;
                Assert.True(container.AddItem(cord));
            }
            Assert.That(container.Contents.Count, Is.EqualTo(8));
            return resident;
        }

        [TestCase(11, 10)]
        [TestCase(11, 11)]
        public void AdjacentPublicCargoOffersPutAndOpenWithExactLoadedWeight(int x, int y)
        {
            var crate = Crate(x, y);
            var container = crate.GetPart<ContainerPart>();
            var clay = Carry("FireClay");
            clay.GetPart<StackerPart>().StackCount = 3;
            var hatchet = Carry("Hatchet");
            int hatchetWeight = HandlingService.GetWeight(hatchet);

            CollectionAssert.Contains(InventorySystem.GetDepositContainers(Actor, Zone), crate);
            Assert.True(CellMenu(x, y).Any(row => row.Command == "OpenContainer"));
            Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(ShellWeight));
            Assert.True(InventorySystem.PutInContainer(Actor, crate, clay));
            Assert.True(InventorySystem.PutInContainer(Actor, crate, hatchet));
            Assert.That(container.Contents, Is.EqualTo(new[] { clay, hatchet }));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.That(hatchet.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.False(Pack.Contains(clay) || Pack.Contains(hatchet));
            Assert.That(DragRules.WeightOf(crate), Is.EqualTo(ShellWeight + 3 + hatchetWeight));

            Assert.True(InventorySystem.TakeFromContainer(Actor, crate, clay));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(clay.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(container.Contents.Single(), Is.SameAs(hatchet));
            Assert.That(DragRules.WeightOf(crate), Is.EqualTo(ShellWeight + hatchetWeight));
        }

        [Test]
        public void AdjacentUnmarkedCrateStillOpensAndGivesStockWithoutOfferingDeposit()
        {
            var crate = Crate(11, 10, publicCargo: false);
            Assert.True(crate.GetPart<PhysicsPart>().Solid);
            var contents = crate.GetPart<ContainerPart>();
            var stock = Factory.CreateEntity("FireClay");
            Assert.True(contents.AddItem(stock));

            CollectionAssert.DoesNotContain(InventorySystem.GetDepositContainers(Actor, Zone), crate);
            Assert.True(CellMenu(11, 10).Any(row => row.Command == "OpenContainer"));
            Assert.True(InventorySystem.TakeFromContainer(Actor, crate, stock));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(contents.Contents, Is.Empty);
        }

        [TestCase(24, true)]
        [TestCase(25, false)]
        public void CargoAllowsAtMostTwentyFourPoundsAboveTheShell(int quantity, bool accepted)
        {
            var crate = Crate(11, 10);
            var clay = Carry("FireClay");
            var stack = clay.GetPart<StackerPart>();
            Assert.That(stack.MaxStack, Is.GreaterThanOrEqualTo(quantity));
            stack.StackCount = quantity;
            var contents = crate.GetPart<ContainerPart>();

            Assert.That(InventorySystem.PutInContainer(Actor, crate, clay), Is.EqualTo(accepted));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(accepted ? crate : Actor));
            Assert.That(stack.StackCount, Is.EqualTo(quantity));
            Assert.That(Pack.Contains(clay), Is.EqualTo(!accepted));
            Assert.That(contents.Contents.Count, Is.EqualTo(accepted ? 1 : 0));
            Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(ShellWeight + (accepted ? quantity : 0)));
        }

        [Test]
        public void WeightAllowanceDoesNotAddANinthPhysicalSlot()
        {
            var crate = Crate(11, 10);
            var contents = crate.GetPart<ContainerPart>();
            Assert.That(contents.MaxItems, Is.EqualTo(8));
            for (int i = 0; i < contents.MaxItems; i++)
            {
                var clay = Factory.CreateEntity("FireClay");
                clay.GetPart<StackerPart>().MaxStack = 1;
                Assert.True(contents.AddItem(clay));
            }
            var original = contents.Contents.ToArray();
            var cord = Carry("KnotflaxCord");

            Assert.False(InventorySystem.PutInContainer(Actor, crate, cord));
            CollectionAssert.AreEqual(original, contents.Contents);
            Assert.That(cord.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(ShellWeight + 8));
        }

        [Test]
        public void CompatibleStackUsesExistingSlotInFullPublicCargo()
        {
            var crate = Crate(11, 10);
            var container = crate.GetPart<ContainerPart>();
            var resident = FillSlotsWithMergeRoom(container);
            var originalContents = container.Contents.ToArray();
            var incoming = Carry("FireClay");
            incoming.GetPart<StackerPart>().StackCount = 3;
            int before = HandlingService.GetWeight(crate);

            Assert.True(InventorySystem.PutInContainer(Actor, crate, incoming));
            CollectionAssert.AreEqual(originalContents, container.Contents);
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(5));
            Assert.That(resident.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.That(incoming.GetPart<StackerPart>().StackCount, Is.Zero);
            Assert.That(incoming.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.False(Pack.Contains(incoming));
            Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(before + 3));
        }

        [TestCase("lock", false)]
        [TestCase("move", true)]
        [TestCase("load-part", false)]
        public void LateCargoChangeRollsBackExactMergeWithoutNarratingSuccess(string change, bool seal)
        {
            var crate = Crate(11, 10);
            var container = crate.GetPart<ContainerPart>();
            var load = crate.GetPart<ContainerLoadPart>();
            var resident = FillSlotsWithMergeRoom(container);
            var originalContents = container.Contents.ToArray();
            var incoming = Carry("FireClay");
            incoming.GetPart<StackerPart>().StackCount = 3;
            var originalPack = Pack.Objects.ToArray();
            var context = new InventoryContext(Actor, Zone);
            var deposit = new PutInContainerCommand(crate, incoming);
            var transaction = new InventoryTransaction();
            string lastMessage = MessageLog.GetLast();

            Assert.True(deposit.Validate(context).IsValid);
            try
            {
                Assert.True(deposit.Execute(context, transaction).Success);
                Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(5));
                Assert.That(incoming.GetPart<StackerPart>().StackCount, Is.Zero);
                Assert.False(Pack.Contains(incoming));
                Assert.That(MessageLog.GetLast(), Is.EqualTo(lastMessage));

                switch (change)
                {
                    case "lock": container.Locked = true; break;
                    case "move":
                        Show(10, 11);
                        Assert.True(Zone.MoveEntity(crate, 10, 11));
                        break;
                    case "load-part": Assert.True(crate.RemovePart(load)); break;
                }

                if (seal) Assert.Throws<InvalidOperationException>(transaction.SealAction);
                else Assert.Throws<InvalidOperationException>(transaction.Commit);
                Assert.False(transaction.ActionCommitted);
            }
            finally
            {
                transaction.Rollback();
            }

            Assert.True(transaction.IsRolledBack);
            Assert.False(transaction.IsCommitted);
            CollectionAssert.AreEqual(originalPack, Pack.Objects);
            CollectionAssert.AreEqual(originalContents, container.Contents);
            Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(resident.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.That(incoming.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(incoming.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(MessageLog.GetLast(), Is.EqualTo(lastMessage));
            if (change == "lock") Assert.True(container.IsLocked);
            if (change == "move") Assert.That(Zone.GetEntityCell(crate), Is.SameAs(Zone.GetCell(10, 11)));
            if (change == "load-part") Assert.IsNull(crate.GetPart<ContainerLoadPart>());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WithdrawalRejectsLateCustodyChangeOfTheActualPackRecipient(bool merge)
        {
            var crate = Crate(11, 10);
            var container = crate.GetPart<ContainerPart>();
            var stored = Factory.CreateEntity("FireClay");
            stored.GetPart<StackerPart>().StackCount = 3;
            Assert.True(container.AddItem(stored));
            var resident = merge ? Carry("FireClay") : null;
            if (resident != null) resident.GetPart<StackerPart>().StackCount = 2;
            var receiver = resident ?? stored;
            var originalPack = Pack.Objects.ToArray();
            var context = new InventoryContext(Actor, Zone);
            var withdrawal = new TakeFromContainerCommand(crate, stored);
            var transaction = new InventoryTransaction();
            string lastMessage = MessageLog.GetLast();

            Assert.True(withdrawal.Validate(context).IsValid);
            try
            {
                Assert.True(withdrawal.Execute(context, transaction).Success);
                Assert.That(container.Contents, Is.Empty);
                Assert.True(Pack.Contains(receiver));
                Assert.That(receiver.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
                Assert.That(receiver.GetPart<StackerPart>().StackCount, Is.EqualTo(merge ? 5 : 3));

                // A callback can corrupt the new pack owner without changing
                // the crate's weight or putting its former entry back.
                receiver.GetPart<PhysicsPart>().InInventory = crate;
                Assert.Throws<InvalidOperationException>(transaction.Commit);
            }
            finally
            {
                transaction.Rollback();
            }

            Assert.True(transaction.IsRolledBack);
            CollectionAssert.AreEqual(originalPack, Pack.Objects);
            Assert.That(container.Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            if (resident != null)
            {
                Assert.That(resident.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
                Assert.That(resident.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            }
            Assert.That(MessageLog.GetLast(), Is.EqualTo(lastMessage));
        }

        [Test]
        public void DepositRejectsSameWeightReplacementOfTheActualCrateRecipient()
        {
            var crate = Crate(11, 10);
            var container = crate.GetPart<ContainerPart>();
            var carried = Carry("FireClay");
            var substitute = Factory.CreateEntity("FireClay");
            var context = new InventoryContext(Actor, Zone);
            var deposit = new PutInContainerCommand(crate, carried);
            var transaction = new InventoryTransaction();
            string lastMessage = MessageLog.GetLast();

            Assert.True(deposit.Validate(context).IsValid);
            try
            {
                Assert.True(deposit.Execute(context, transaction).Success);
                Assert.That(container.Contents.Single(), Is.SameAs(carried));
                Assert.That(carried.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));

                Assert.True(container.RemoveItem(carried));
                Assert.True(container.AddItem(substitute));
                Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(ShellWeight + 1));
                Assert.Throws<InvalidOperationException>(transaction.Commit);
            }
            finally
            {
                transaction.Rollback();
            }

            Assert.True(transaction.IsRolledBack);
            Assert.That(container.Contents.Single(), Is.SameAs(substitute));
            Assert.That(substitute.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.That(carried.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(carried.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.True(Pack.Contains(carried));
            Assert.That(MessageLog.GetLast(), Is.EqualTo(lastMessage));
        }

        [TestCase("adjacent-move")]
        [TestCase("beyond-reach")]
        [TestCase("replaced")]
        [TestCase("hidden")]
        [TestCase("locked")]
        public void StaleOrInaccessibleCargoLeavesBothExactOwnersAlone(string change)
        {
            var crate = Crate(11, 10);
            var contents = crate.GetPart<ContainerPart>();
            var carried = Carry("KnotflaxCord");
            var stored = Factory.CreateEntity("FireClay");
            Assert.True(contents.AddItem(stored));
            var deposit = new PutInContainerCommand(crate, carried);
            var withdrawal = new TakeFromContainerCommand(crate, stored);
            var context = new InventoryContext(Actor, Zone);
            Assert.True(deposit.Validate(context).IsValid);
            Assert.True(withdrawal.Validate(context).IsValid);

            switch (change)
            {
                case "adjacent-move":
                    Show(10, 11);
                    Assert.True(Zone.MoveEntity(crate, 10, 11));
                    break;
                case "beyond-reach":
                    Show(12, 10);
                    Assert.True(Zone.MoveEntity(crate, 12, 10));
                    CollectionAssert.DoesNotContain(InventorySystem.GetDepositContainers(Actor, Zone), crate);
                    break;
                case "replaced":
                    Assert.True(Zone.RemoveEntity(crate));
                    var replacement = Crate(11, 10);
                    replacement.ID = crate.ID;
                    break;
                case "hidden":
                    Zone.GetCell(11, 10).IsVisible = false;
                    break;
                case "locked":
                    contents.Locked = true;
                    break;
            }

            Assert.False(InventorySystem.ExecuteCommand(deposit, Actor, Zone).Success);
            Assert.False(InventorySystem.ExecuteCommand(withdrawal, Actor, Zone).Success);
            Assert.That(contents.Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.That(carried.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(ShellWeight + 1));
            if (change == "adjacent-move")
                Assert.True(InventorySystem.PutInContainer(Actor, crate, carried), "A fresh choice uses the current adjacent position.");
        }

        [TestCase("nested")]
        [TestCase("reserved")]
        [TestCase("rented")]
        [TestCase("quest")]
        [TestCase("wrong-holder")]
        public void RestrictedOrInexactGoodsCannotEnterPublicCargo(string fault)
        {
            var crate = Crate(11, 10);
            var item = Carry(fault == "nested" ? "FieldSatchel" : "FireClay");
            var physical = item.GetPart<PhysicsPart>();
            switch (fault)
            {
                case "reserved": item.AddPart(new LocalPorterReservationPart()); break;
                case "rented": item.AddPart(new RentalPart()); break;
                case "quest": item.SetTag("QuestItem"); break;
                case "wrong-holder": physical.InInventory = crate; break;
            }
            var originalOwner = physical.InInventory;

            Assert.False(InventorySystem.PutInContainer(Actor, crate, item));
            Assert.That(crate.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(HandlingService.GetWeight(crate), Is.EqualTo(ShellWeight));
            Assert.True(Pack.Contains(item));
            Assert.That(physical.InInventory, Is.SameAs(originalOwner));
        }
    }
}
