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
        const string Timber = "SalvagedTimber";
        const string TakeUnit = "TakePublicCargoUnit|";

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

        Entity StoreTwoTimber(Entity crate)
        {
            var timber = Carry(Timber);
            timber.GetPart<StackerPart>().StackCount = 2;
            Assert.True(InventorySystem.PutInContainer(Actor, crate, timber));
            return timber;
        }

        InventoryAction[] CargoUnitRows() => CellMenu(11, 10).Where(row => row.Command.StartsWith(TakeUnit, StringComparison.Ordinal)).ToArray();

        InventoryAction CargoUnitChoice()
        {
            var rows = CargoUnitRows().Where(row => !row.IsDisabled).ToArray();
            Assert.That(rows, Has.Length.EqualTo(1), "The released crate should offer to take a single timber.");
            return rows[0];
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CargoUnitTransfersOnePoundWhenTheWholeStackWillNotFit(bool merge)
        {
            var crate = Crate(11, 10);
            var container = crate.GetPart<ContainerPart>();
            var stock = StoreTwoTimber(crate);
            var resident = merge ? Carry(Timber) : null;
            int carriedBefore = Pack.GetCarriedWeight();
            int loadedBefore = HandlingService.GetWeight(crate);
            Pack.MaxWeight = carriedBefore + 1;

            Assert.AreEqual(ShellWeight + 2, loadedBefore);
            Assert.That(CellMenu(11, 10).Any(row => row.Command == "OpenContainer"), Is.True);
            Assert.False(InventorySystem.TakeFromContainer(Actor, crate, stock));
            Assert.That(container.Contents.Single(), Is.SameAs(stock));
            Assert.AreEqual(2, stock.GetPart<StackerPart>().StackCount);
            var row = CargoUnitChoice();
            Assert.That(row.Display.ToLowerInvariant(), Does.Contain("take one salvaged timber"));
            Assert.True(ChooseRow(row, 11, 10));

            Assert.That(container.Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.AreEqual(1, stock.GetPart<StackerPart>().StackCount);
            var carried = Pack.Objects.Single(e => e.BlueprintName == Timber);
            Assert.That(carried.ID, Is.Not.EqualTo(stock.ID));
            Assert.That(carried.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.True(carried.GetPart<StackerPart>().CanStackWith(stock));
            Assert.AreEqual(1, HandlingService.GetWeight(carried));
            Assert.AreEqual(merge ? 2 : 1, carried.GetPart<StackerPart>().StackCount);
            if (merge) Assert.That(carried, Is.SameAs(resident));
            Assert.AreEqual(loadedBefore - 1, HandlingService.GetWeight(crate));
            Assert.AreEqual(carriedBefore + 1, Pack.GetCarriedWeight());
            Assert.IsEmpty(CargoUnitRows().Where(choice => !choice.IsDisabled));
            Assert.False(Act(row.Via ?? crate, row.Command));
        }

        [Test]
        public void CargoUnitKeepsItsLabelSeparateFromDifferentlyLabeledTimberInThePack()
        {
            var crate = Crate(11, 10);
            var stock = StoreTwoTimber(crate);
            var stockLabel = new StorageLabelPart { Mode = (int)StorageLabelMode.Text, ColorIndex = 3, Text = "gate bracing" };
            stock.AddPart(stockLabel);
            var resident = Carry(Timber);
            var packLabel = new StorageLabelPart { Mode = (int)StorageLabelMode.ColorMark, ColorIndex = 1 };
            resident.AddPart(packLabel);
            int weight = Pack.GetCarriedWeight();
            Pack.MaxWeight = weight + 1;
            Assert.False(resident.GetPart<StackerPart>().CanStackWith(stock));

            Assert.True(ChooseRow(CargoUnitChoice(), 11, 10));
            var boards = Pack.Objects.Where(item => item.BlueprintName == Timber).ToArray();
            Assert.That(boards, Has.Length.EqualTo(2));
            var unit = boards.Single(item => item != resident);
            var unitLabel = unit.GetPart<StorageLabelPart>();
            Assert.NotNull(unitLabel);
            Assert.That(unitLabel, Is.Not.SameAs(stockLabel));
            Assert.That(unitLabel.ParentEntity, Is.SameAs(unit));
            Assert.That(stock.GetPart<StorageLabelPart>(), Is.SameAs(stockLabel));
            Assert.That(resident.GetPart<StorageLabelPart>(), Is.SameAs(packLabel));
            Assert.AreEqual((stockLabel.Mode, stockLabel.ColorIndex, stockLabel.Text), (unitLabel.Mode, unitLabel.ColorIndex, unitLabel.Text));
            Assert.AreEqual(1, resident.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, stock.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, unit.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(weight + 1, Pack.GetCarriedWeight());
            Assert.AreEqual(ShellWeight + 1, HandlingService.GetWeight(crate));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CargoUnitLateContextChangeConservesBothBoards(bool renameRecipient)
        {
            var crate = Crate(11, 10);
            var contents = crate.GetPart<ContainerPart>();
            var stock = StoreTwoTimber(crate);
            var originalPack = Pack.Objects.ToArray();
            int weight = Pack.GetCarriedWeight();
            Pack.MaxWeight = weight + 1;
            var row = CargoUnitChoice();
            string lastMessage = MessageLog.GetLast();
            var tx = new InventoryTransaction();
            var action = GameEvent.New("InventoryAction");
            var newId = Guid.NewGuid().ToString("N");
            Entity recipient = null;
            try
            {
                action.SetParameter("Actor", (object)Actor);
                action.SetParameter("Zone", (object)Zone);
                action.SetParameter("Command", row.Command);
                action.SetParameter("InventoryTransaction", (object)tx);
                (row.Via ?? crate).FireEvent(action);
                Assert.True(action.Handled);
                Assert.AreEqual(1, stock.GetPart<StackerPart>().StackCount);
                recipient = Pack.Objects.Single(item => item.BlueprintName == Timber);
                Assert.AreEqual(1, recipient.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(weight + 1, Pack.GetCarriedWeight());
                Assert.AreEqual(lastMessage, MessageLog.GetLast());

                if (renameRecipient) recipient.ID = newId;
                else contents.Locked = true;
                Assert.Throws<InvalidOperationException>(tx.Commit);
                Assert.False(tx.ActionCommitted);
            }
            finally
            {
                action.Release();
                tx.Rollback();
            }

            Assert.True(tx.IsRolledBack);
            Assert.That(contents.IsLocked, Is.EqualTo(!renameRecipient));
            Assert.That(contents.Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            int remaining = stock.GetPart<StackerPart>().StackCount;
            Assert.AreEqual(renameRecipient ? 1 : 2, remaining);
            if (renameRecipient)
            {
                Assert.That(Pack.Objects.Single(item => item.BlueprintName == Timber), Is.SameAs(recipient));
                Assert.That(recipient.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
                Assert.AreEqual(newId, recipient.ID);
                Assert.AreEqual(1, recipient.GetPart<StackerPart>().StackCount);
            }
            else CollectionAssert.AreEqual(originalPack, Pack.Objects);
            Assert.AreEqual(2, remaining + Pack.Objects.Where(item => item.BlueprintName == Timber)
                .Sum(item => item.GetPart<StackerPart>().StackCount));
            Assert.AreEqual(ShellWeight + remaining, HandlingService.GetWeight(crate));
            Assert.AreEqual(weight + (renameRecipient ? 1 : 0), Pack.GetCarriedWeight());
            Assert.AreEqual(lastMessage, MessageLog.GetLast());
        }

        [TestCase("capacity")]
        [TestCase("locked")]
        [TestCase("gripped")]
        [TestCase("moved")]
        [TestCase("equivalent replacement")]
        public void CargoUnitRefusesAStaleChoiceWithoutTakingAnyStock(string change)
        {
            var crate = Crate(11, 10);
            var container = crate.GetPart<ContainerPart>();
            var stock = StoreTwoTimber(crate);
            int carriedBefore = Pack.GetCarriedWeight();
            Pack.MaxWeight = carriedBefore + 1;
            var row = CargoUnitChoice();
            var remaining = stock;
            switch (change)
            {
                case "capacity": Pack.MaxWeight = carriedBefore; break;
                case "locked": container.Locked = true; break;
                case "gripped": Assert.That(DragSystem.TryGrab(Actor, crate, Zone), Is.EqualTo(DragVerdict.Ok)); break;
                case "moved":
                    Show(10, 11);
                    Assert.True(Zone.MoveEntity(crate, 10, 11));
                    break;
                case "equivalent replacement":
                    remaining = Factory.CreateEntity(Timber);
                    remaining.ID = stock.ID;
                    remaining.GetPart<StackerPart>().StackCount = 2;
                    Assert.True(container.RemoveItem(stock));
                    Assert.True(container.AddItem(remaining));
                    break;
            }
            int tick = Clock.TickCount;

            Assert.False(Act(row.Via ?? crate, row.Command));
            Assert.That(container.Contents.Single(), Is.SameAs(remaining));
            Assert.That(remaining.GetPart<PhysicsPart>().InInventory, Is.SameAs(crate));
            Assert.AreEqual(2, remaining.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(ShellWeight + 2, HandlingService.GetWeight(crate));
            Assert.AreEqual(carriedBefore, Pack.GetCarriedWeight());
            Assert.AreEqual(tick, Clock.TickCount);
            if (change != "equivalent replacement")
                Assert.IsEmpty(CargoUnitRows().Where(choice => !choice.IsDisabled));
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
