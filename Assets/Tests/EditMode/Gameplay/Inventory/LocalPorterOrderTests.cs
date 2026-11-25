using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LocalPorterOrderTests : FiftyWorldFixture
    {
        [Test]
        public void ABookedTransferHoldsRealCargoAndPaysOnceAtReceipt()
        {
            var ledger = Arrange();
            var box = ledger.Box;
            var stock = box.GetPart<ContainerPart>().Contents.Single();
            var command = Hire(ledger);
            Assert.That(Act(ledger.ParentEntity, command), Is.True);
            int fare = ledger.Fare;
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Booked));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50 - fare));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.EqualTo(fare));
            Assert.That(Zone.GetEntityPosition(box), Is.EqualTo((10, 11)));
            Assert.That(Act(ledger.ParentEntity, command), Is.False);
            Assert.That(InventorySystem.ExecuteCommand(new PickupCommand(box), Actor, Zone).Success, Is.False);
            Assert.That(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(box, stock), Actor, Zone).Success, Is.False);
            Assert.That(TradeSystem.CanBeTraded(box, Actor, ledger.Worker, "Sell"), Is.False);

            Assert.That(ledger.TryAct(Zone), Is.True);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Carrying));
            Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(8));
            Assert.That(box.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Worker));

            WorkUntil(ledger, LocalPorterPhase.Delivered);
            Assert.That(ledger.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(box));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
            Assert.That(stock.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(7 + fare));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            Assert.That(ledger.TryAct(Zone), Is.False);
            Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(7 + fare));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellationRefundsOnlyUnearnedFareAndReleasesTheSameBox(bool afterPickup)
        {
            var ledger = Arrange();
            var box = ledger.Box;
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            if (afterPickup) Assert.That(ledger.TryAct(Zone), Is.True);

            Assert.That(Act(ledger.ParentEntity, Cancel(ledger)), Is.True);
            if (afterPickup) WorkUntil(ledger, LocalPorterPhase.Cancelled);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Cancelled));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(afterPickup ? 49 : 50));
            Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(afterPickup ? 8 : 7));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            Assert.That(Zone.GetEntityPosition(box), Is.EqualTo((10, 11)));
            Assert.That(InventorySystem.ExecuteCommand(new PickupCommand(box), Actor, Zone).Success, Is.True);
        }

        [Test]
        public void AFullReceiverPausesWithTheLoadedBoxAndNoFinalPayment()
        {
            var ledger = Arrange();
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            Assert.That(ledger.TryAct(Zone), Is.True);
            var obstacle = Factory.CreateEntity("LiningClayParcel");
            Assert.That(ledger.Shelf.GetPart<ContainerPart>().AddItem(obstacle), Is.True);
            for (int i = 0; i < 36; i++) ledger.TryAct(Zone);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Carrying));
            Assert.That(ledger.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Worker));
            Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(8));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.EqualTo(ledger.Fare - 1));
            Assert.That(ledger.Shelf.GetPart<ContainerPart>().RemoveItem(obstacle), Is.True);
            WorkUntil(ledger, LocalPorterPhase.Delivered);
        }

        [Test]
        public void MovedReceiverReturnsTheParcelInsteadOfDeliveringElsewhere()
        {
            var ledger = Arrange();
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            Assert.That(ledger.TryAct(Zone), Is.True);
            Assert.That(Zone.MoveEntity(ledger.Shelf, 20, 10), Is.True);
            WorkUntil(ledger, LocalPorterPhase.Cancelled);
            Assert.That(Zone.GetEntityPosition(ledger.Box), Is.EqualTo((10, 11)));
            Assert.That(ledger.Shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(49));
        }

        [Test]
        public void AnIncapacitatedWorkerPausesWithoutLosingTheOrder()
        {
            var ledger = Arrange();
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            Assert.That(ledger.TryAct(Zone), Is.True);
            var here = Zone.GetEntityPosition(ledger.Worker);
            Assert.That(ledger.Worker.ApplyEffect(new RootedEffect(), Actor, Zone), Is.True);
            Assert.That(ledger.TryAct(Zone), Is.False);
            Assert.That(Zone.GetEntityPosition(ledger.Worker), Is.EqualTo(here));
            Assert.That(ledger.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Worker));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.EqualTo(ledger.Fare - 1));
            ledger.Worker.RemoveEffect<RootedEffect>();
            WorkUntil(ledger, LocalPorterPhase.Delivered);
        }

        [Test]
        public void AnInactiveZoneCannotMoveOrSettleTheCarrier()
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.11.10.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var ledger = Arrange();
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            Assert.That(ledger.TryAct(Zone), Is.True);
            var here = Zone.GetEntityPosition(ledger.Worker);
            manager.SetActiveZone(new Zone("Overworld.11.11.0"));
            Assert.That(ledger.TryAct(Zone), Is.False);
            Assert.That(Zone.GetEntityPosition(ledger.Worker), Is.EqualTo(here));
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Carrying));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.EqualTo(ledger.Fare - 1));
            manager.SetActiveZone(Zone);
            WorkUntil(ledger, LocalPorterPhase.Delivered);
        }

        [Test]
        public void ADeadPorterReleasesTheActualSpillAndRefundsOnce()
        {
            var ledger = Arrange();
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            Assert.That(ledger.TryAct(Zone), Is.True);
            ledger.Worker.GetStat("Hitpoints").BaseValue = 0;
            CombatSystem.HandleDeath(ledger.Worker, null, Zone);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(49));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            var box = ledger.Box;
            var holder = box.GetPart<PhysicsPart>().InInventory;
            Assert.That(holder == null ? Zone.GetEntityCell(box) != null : holder.GetPart<ContainerPart>()?.Contents.Contains(box) == true, Is.True);
            Assert.That(LocalPorterReservationPart.Blocks(box), Is.False);
            Assert.That(box.GetPart<ContainerPart>().Contents.Count, Is.EqualTo(1));
        }

        [Test]
        public void AChangedLoadRefusesTheStaleOfferWithoutTakingMoney()
        {
            var ledger = Arrange();
            string command = Hire(ledger);
            var stored = ledger.Box.GetPart<ContainerPart>().Contents.Single();
            stored.GetPart<StackerPart>().StackCount = 1;
            Assert.That(Act(ledger.ParentEntity, command), Is.False);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Open));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            Assert.That(LocalPorterReservationPart.Blocks(ledger.Box), Is.False);
        }

        [Test]
        public void AStaleQuoteCannotRebookIdenticalCargoAfterCancellation()
        {
            var ledger = Arrange();
            string oldQuote = Hire(ledger);
            Assert.That(Act(ledger.ParentEntity, oldQuote), Is.True);
            Assert.That(Act(ledger.ParentEntity, Cancel(ledger)), Is.True);

            Assert.That(Act(ledger.ParentEntity, oldQuote), Is.False);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Cancelled));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
        }

        [Test]
        public void AStaleCancellationCannotCancelTheNextBooking()
        {
            var ledger = Arrange();
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            string oldCancellation = Cancel(ledger);
            Assert.That(Act(ledger.ParentEntity, oldCancellation), Is.True);
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            string currentID = ledger.OrderID;

            Assert.That(Act(ledger.ParentEntity, oldCancellation), Is.False);
            Assert.That(ledger.OrderID, Is.EqualTo(currentID));
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Booked));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50 - ledger.Fare));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.EqualTo(ledger.Fare));
            Assert.That(LocalPorterReservationPart.Blocks(ledger.Box), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FreshContinueKeepsTheSingleBoxAndTheAgreedEscrow(bool carryBeforeSave)
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.11.10.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var ledger = Arrange();
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            Assert.That(Act(ledger.ParentEntity, Hire(ledger)), Is.True);
            if (carryBeforeSave)
            {
                Assert.That(AIBoredEvent.Check(ledger.Worker), Is.False);
                Assert.That(AIBoredEvent.Check(ledger.Worker), Is.False);
            }
            string boardID = ledger.ParentEntity.ID, boxID = ledger.Box.ID, orderID = ledger.OrderID;
            int fare = ledger.Fare;
            using var stream = new MemoryStream();
            GameSessionState.Capture("local-porter", "porter-order", manager, null, Actor).Save(new SaveWriter(stream));
            stream.Position = 0;
            var saved = GameSessionState.Load(new SaveReader(stream, Factory));
            Zone = saved.ZoneManager.ActiveZone;
            Actor = saved.Player;
            var fresh = Zone.GetReadOnlyEntities().Single(e => e.ID == boardID).GetPart<LocalPorterLedgerPart>();
            Assert.That(fresh.Phase, Is.EqualTo(carryBeforeSave ? LocalPorterPhase.Carrying : LocalPorterPhase.Booked));
            Assert.That(fresh.OrderID, Is.EqualTo(orderID));
            Assert.That(fresh.Payer, Is.SameAs(Actor));
            Assert.That(fresh.Box.ID, Is.EqualTo(boxID));
            Assert.That(fresh.Box.GetPart<LocalPorterReservationPart>().Ledger, Is.SameAs(fresh.ParentEntity));
            Assert.That(TradeSystem.GetDrams(fresh.ParentEntity), Is.EqualTo(fare - (carryBeforeSave ? 1 : 0)));
            WorkUntil(fresh, LocalPorterPhase.Delivered);
            Assert.That(TradeSystem.GetDrams(fresh.Worker), Is.EqualTo(7 + fare));
            Assert.That(fresh.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(fresh.Box));
            Assert.That(LocalPorterReservationPart.Blocks(fresh.Box), Is.False);
        }

        private LocalPorterLedgerPart Arrange()
        {
            var board = Place("Signpost", 10, 11);
            var parcel = Place("LiningClayParcel", 10, 11);
            var shelf = Place("LiningClayReceivingShelf", 19, 10);
            var worker = Place("Creature", 11, 10);
            worker.SetTag("Faction", "Villagers");
            worker.GetStat("Hitpoints").BaseValue = 20;
            worker.GetStat("Strength").BaseValue = 16;
            worker.GetPart<BrainPart>().Passive = true;
            worker.GetPart<BrainPart>().Staying = true;
            worker.GetPart<BrainPart>().Wanders = false;
            worker.AddPart(new LocalPorterPart { Ledger = board });
            var ledger = new LocalPorterLedgerPart { Worker = worker, Box = parcel, Shelf = shelf };
            board.AddPart(ledger);
            var clay = Carry("PreparedLiningClay");
            clay.GetPart<StackerPart>().StackCount = 2;
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(parcel, clay), Actor, Zone).Success, Is.True);
            TradeSystem.SetDrams(Actor, 50);
            TradeSystem.SetDrams(worker, 7);
            return ledger;
        }

        private string Hire(LocalPorterLedgerPart ledger) =>
            Actions(ledger.ParentEntity).Single(a => a.Command.StartsWith(LocalPorterLedgerPart.HirePrefix)).Command;

        private string Cancel(LocalPorterLedgerPart ledger) =>
            Actions(ledger.ParentEntity).Single(a => a.Command.StartsWith(LocalPorterLedgerPart.CancelPrefix)).Command;

        private void WorkUntil(LocalPorterLedgerPart ledger, LocalPorterPhase target)
        {
            for (int i = 0; i < 38 && ledger.Phase != target; i++) ledger.TryAct(Zone);
            Assert.That(ledger.Phase, Is.EqualTo(target));
        }
    }
}
