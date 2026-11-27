using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LocalPorterOrderTests : FiftyWorldFixture
    {
        [Test]
        public void OneMeasureHomeFreightCostsOneDramLessOnTheSameWalk()
        {
            using var content = new HaulingContentScope();
            var (ledger, clay, _) = ArrangeHomeFreight(content);
            var porter = ledger.Worker.GetPart<LocalPorterPart>();
            var additional = content.Factory.CreateEntity("FireClay");
            Assert.True(Pack.AddObject(additional));
            var packed = InventorySystem.ExecuteCommand(new PutInContainerCommand(ledger.Box, additional), Actor, Zone);
            Assert.True(packed.Success, packed.ErrorMessage);
            Assert.True(porter.TryQuote(Actor, ledger.Box, ledger.Shelf, Zone, out var two, out var refusal), refusal);
            var old = HireRow(ledger);
            Assert.That(old.Hint, Does.Contain("5 lb boxed").And.Contain("2 lb loose"));
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(ledger.Box, additional), Actor, Zone).Success);

            Assert.True(porter.TryQuote(Actor, ledger.Box, ledger.Shelf, Zone, out var one, out refusal), refusal);
            Assert.AreEqual(5, two.LoadedWeight);
            Assert.AreEqual(4, one.LoadedWeight);
            Assert.AreEqual(1, one.LooseWeight);
            Assert.That(one.Steps, Is.EqualTo(two.Steps));
            Assert.AreEqual(two.Fare - 1, one.Fare);
            var fresh = HireRow(ledger);
            Assert.That(fresh.Display, Is.EqualTo($"hire porter ({one.Fare} drams)"));
            Assert.That(fresh.Hint, Does.Contain("4 lb boxed").And.Contain("1 lb loose"));
            Assert.That(fresh.Command, Is.Not.EqualTo(old.Command));
            Assert.False(Act(ledger.ParentEntity, old.Command));
            Assert.AreEqual(50, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(0, TradeSystem.GetDrams(ledger.ParentEntity));
            Assert.AreEqual(LocalPorterPhase.Open, ledger.Phase);
            Assert.False(LocalPorterReservationPart.Blocks(ledger.Box));
            Assert.That(ledger.Box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(clay));
            Assert.That(Pack.Objects, Does.Contain(additional));
        }

        [Test]
        public void OneMeasureHomeFreightMovesTheExactBoxThroughPickupSaveAndWithdrawal()
        {
            using var content = new HaulingContentScope();
            var (ledger, clay, manager) = ArrangeHomeFreight(content);
            var box = ledger.Box;
            var worker = ledger.Worker;
            var marker = ledger.ParentEntity;
            Assert.False(LocalPorterReservationPart.Blocks(box));
            string command = Hire(ledger);
            Assert.True(Act(marker, command));
            int fare = ledger.Fare;
            string order = ledger.OrderID;
            Assert.AreEqual(LocalPorterPhase.Booked, ledger.Phase);
            Assert.AreEqual(50 - fare, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(fare, TradeSystem.GetDrams(marker));
            Assert.That(box.GetPart<LocalPorterReservationPart>().Ledger, Is.SameAs(marker));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(clay));
            Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(box, clay), Actor, Zone).Success);

            Assert.True(ledger.TryAct(Zone));
            Assert.AreEqual(LocalPorterPhase.Carrying, ledger.Phase);
            Assert.That(box.GetPart<PhysicsPart>().InInventory, Is.SameAs(worker));
            Assert.AreEqual(fare - 1, TradeSystem.GetDrams(marker));
            Assert.AreEqual(8, TradeSystem.GetDrams(worker));

            using var stream = new MemoryStream();
            GameSessionState.Capture("gleaners-one-measure", "carrying", manager, null, Actor).Save(new SaveWriter(stream));
            stream.Position = 0;
            var saved = GameSessionState.Load(new SaveReader(stream, content.Factory));
            Zone = saved.ZoneManager.ActiveZone;
            Actor = saved.Player;
            SettlementRuntime.ActiveZone = Zone;
            var returned = Zone.GetReadOnlyEntities().Single(e => e.ID == marker.ID).GetPart<LocalPorterLedgerPart>();
            Assert.AreEqual(order, returned.OrderID);
            Assert.AreEqual(LocalPorterPhase.Carrying, returned.Phase);
            Assert.That(returned.Payer, Is.SameAs(Actor));
            Assert.AreEqual(box.ID, returned.Box.ID);
            var destination = returned.Shelf;
            var returnedClay = returned.Box.GetPart<ContainerPart>().Contents.Single();
            Assert.AreEqual(clay.ID, returnedClay.ID);
            Assert.That(returned.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(returned.Worker));

            WorkUntil(returned, LocalPorterPhase.Delivered);
            Assert.That(destination.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(returned.Box));
            Assert.That(returned.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(destination));
            Assert.False(LocalPorterReservationPart.Blocks(returned.Box));
            Assert.AreEqual(7 + fare, TradeSystem.GetDrams(returned.Worker));
            Assert.AreEqual(50 - fare, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(0, TradeSystem.GetDrams(returned.ParentEntity));
            Assert.False(returned.TryAct(Zone));
            Assert.AreEqual(7 + fare, TradeSystem.GetDrams(returned.Worker));

            var at = Zone.GetEntityCell(destination);
            Assert.True(Zone.MoveEntity(Actor, at.X, at.Y));
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(destination, returned.Box), Actor, Zone).Success);
            Assert.True(InventorySystem.ExecuteCommand(new DropCommand(returned.Box), Actor, Zone).Success);
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(returned.Box, returnedClay), Actor, Zone).Success);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "PreparedLiningClay"), Is.SameAs(returnedClay));
            Assert.AreEqual(1, returnedClay.GetPart<StackerPart>().StackCount);
            Assert.That(destination.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(returned.Box.GetPart<ContainerPart>().Contents, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OneMeasureHomeFreightCancellationReturnsTheSameStockAndClosesTheQuote(bool afterPickup)
        {
            using var content = new HaulingContentScope();
            var (ledger, clay, _) = ArrangeHomeFreight(content);
            var box = ledger.Box;
            var origin = Zone.GetEntityCell(box);
            var worker = ledger.Worker;
            string previous = Hire(ledger);
            Assert.True(Act(ledger.ParentEntity, previous));
            if (afterPickup)
            {
                Assert.True(ledger.TryAct(Zone));
                Assert.True(ledger.TryAct(Zone));
                Assert.AreEqual(LocalPorterPhase.Carrying, ledger.Phase);
            }
            Assert.True(Act(ledger.ParentEntity, Cancel(ledger)));
            if (afterPickup) WorkUntil(ledger, LocalPorterPhase.Cancelled);

            Assert.AreEqual(LocalPorterPhase.Cancelled, ledger.Phase);
            Assert.That(Zone.GetEntityCell(box), Is.SameAs(origin));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(clay));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.AreEqual(1, clay.GetPart<StackerPart>().StackCount);
            Assert.That(ledger.Shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.False(LocalPorterReservationPart.Blocks(box));
            Assert.False(Act(ledger.ParentEntity, previous));
            Assert.AreEqual(afterPickup ? 49 : 50, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(afterPickup ? 8 : 7, TradeSystem.GetDrams(worker));
            Assert.AreEqual(0, TradeSystem.GetDrams(ledger.ParentEntity));
        }

        [Test]
        public void OneMeasureHomeFreightReturnsTheSameBoxWhenTheShelfDisappearsInTransit()
        {
            using var content = new HaulingContentScope();
            var (ledger, clay, _) = ArrangeHomeFreight(content);
            var box = ledger.Box;
            var origin = Zone.GetEntityCell(box);
            var shelf = ledger.Shelf;
            Assert.True(Act(ledger.ParentEntity, Hire(ledger)));
            Assert.True(ledger.TryAct(Zone));
            Assert.True(ledger.TryAct(Zone));
            Assert.AreEqual(LocalPorterPhase.Carrying, ledger.Phase);
            Assert.That(box.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Worker));
            Assert.True(Zone.RemoveEntity(shelf));

            Assert.True(ledger.TryAct(Zone));
            Assert.AreEqual(LocalPorterPhase.Returning, ledger.Phase);
            Assert.That(box.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Worker));
            WorkUntil(ledger, LocalPorterPhase.Cancelled);
            Assert.That(Zone.GetEntityCell(box), Is.SameAs(origin));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(clay));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.AreEqual(1, clay.GetPart<StackerPart>().StackCount);
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.False(LocalPorterReservationPart.Blocks(box));
            Assert.AreEqual(49, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(8, TradeSystem.GetDrams(ledger.Worker));
            Assert.AreEqual(0, TradeSystem.GetDrams(ledger.ParentEntity));
        }

        [Test]
        public void OneMeasureHomeFreightDoesNotRelaxANoncanonicalLoadingPost()
        {
            var ledger = Arrange();
            var stock = ledger.Box.GetPart<ContainerPart>().Contents.Single();
            string normal = Hire(ledger);
            stock.GetPart<StackerPart>().StackCount = 1;

            Assert.False(ledger.Worker.GetPart<LocalPorterPart>().TryQuote(Actor, ledger.Box, ledger.Shelf, Zone, out _, out _));
            Assert.False(Act(ledger.ParentEntity, normal));
            Assert.AreEqual(LocalPorterPhase.Open, ledger.Phase);
            Assert.AreEqual(50, TradeSystem.GetDrams(Actor));
            Assert.False(LocalPorterReservationPart.Blocks(ledger.Box));
            Assert.That(stock.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Box));
        }

        private (LocalPorterLedgerPart ledger, Entity clay, OverworldZoneManager manager) ArrangeHomeFreight(HaulingContentScope content)
        {
            content.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(GleanersDistrict.SurfaceID);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            var marker = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "GleanersLoadingMarker");
            var ledger = marker.GetPart<LocalPorterLedgerPart>();
            Assert.That(ledger.Shelf.BlueprintName, Is.EqualTo("LiningClayReceivingShelf"));
            var post = Zone.GetEntityCell(marker);
            // Generated owners and test participants share one sequence of physical IDs.
            Actor = content.Factory.CreateEntity("Player");
            Assert.NotNull(Actor);
            Assert.True(Zone.AddEntity(Actor, post.X, post.Y));
            Pack.MaxWeight = 200;
            var clay = content.Factory.CreateEntity("PreparedLiningClay");
            Assert.NotNull(clay);
            Assert.True(Pack.AddObject(clay));
            Assert.AreEqual(1, clay.GetPart<StackerPart>().StackCount);
            var packed = InventorySystem.ExecuteCommand(new PutInContainerCommand(ledger.Box, clay), Actor, Zone);
            Assert.True(packed.Success, packed.ErrorMessage);
            TradeSystem.SetDrams(Actor, 50);
            TradeSystem.SetDrams(ledger.Worker, 7);
            return (ledger, clay, manager);
        }

        [Test]
        public void TheLoadingGuideExplainsFreeSmallCarriesAndTheActualWalkingFare()
        {
            var ledger = Arrange();
            var lines = new List<string>();
            int tick = Clock.TickCount;
            string quote = Hire(ledger);

            Assert.True(UseGuides.Collect(ledger.ParentEntity, Zone, Zone.GetEntityCell(ledger.ParentEntity), Actor, lines));
            Assert.That(string.Join(" ", lines), Does.Contain("one measure at a time or both loose")
                .And.Contain("shorter clear walk").And.Contain("hire porter (5 drams)")
                .And.Contain("The porter pauses while you are away from this area."));
            Assert.That(Hire(ledger), Is.EqualTo(quote));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Open));

            ledger.Worker = null;
            lines.Clear();
            Assert.False(UseGuides.Collect(ledger.ParentEntity, Zone, Zone.GetEntityCell(ledger.ParentEntity), Actor, lines));
            Assert.That(lines, Is.Empty);
        }

        [Test]
        public void HireHintComparesTheActualBoxLooseLoadAndRemainingPackRoom()
        {
            var ledger = Arrange();
            var workerPack = ledger.Worker.GetPart<InventoryPart>();
            var workerStock = Factory.CreateEntity("SalvagedTimber");
            workerStock.GetPart<StackerPart>().StackCount = 3;
            workerPack.MaxWeight = 19;
            Assert.True(workerPack.AddObject(workerStock));
            var ownStock = Carry("SalvagedTimber");
            ownStock.GetPart<StackerPart>().StackCount = 4;
            Pack.MaxWeight = 18;
            Assert.That(Pack.MaxWeight - Pack.GetCarriedWeight(), Is.EqualTo(14));
            var porter = ledger.Worker.GetPart<LocalPorterPart>();
            Assert.True(porter.TryQuote(Actor, ledger.Box, ledger.Shelf, Zone, out var quote, out var refusal), refusal);
            Assert.That(quote.FreeBefore, Is.EqualTo(16));
            Assert.That(quote.Steps.Count, Is.EqualTo(7));
            int tick = Clock.TickCount;

            var hire = HireRow(ledger);
            Assert.That(hire.Display, Is.EqualTo("hire porter (5 drams)"));
            Assert.That(hire.Hint, Does.Contain("5 lb boxed").And.Contain("2 lb loose").And.Contain("7 steps"));
            Assert.That(hire.Hint, Does.Match(@"(?i)porter room[^0-9]*16 lb"));
            Assert.That(hire.Hint, Does.Match(@"(?i)hard pack room[^0-9]*14 lb"));
            Assert.That(string.Join(" ", CavesOfOoo.Rendering.WorldActionMenuUI.WrapHint(hire.Hint, 44, 3)),
                Does.Contain("1 action").And.Contain("Rest paid or refunded.").And.Not.Contain("~"));
            Assert.That(hire.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Open));

            Pack.MaxWeight = -1;
            var uncapped = HireRow(ledger);
            Assert.That(uncapped.Command, Is.EqualTo(hire.Command));
            Assert.That(uncapped.Hint, Does.Contain("Hard pack room unlimited"));
            Assert.That(string.Join(" ", CavesOfOoo.Rendering.WorldActionMenuUI.WrapHint(uncapped.Hint, 44, 3)),
                Does.Contain("Rest paid or refunded.").And.Not.Contain("~"));
        }

        [Test]
        public void ChangedBoxWeightInvalidatesTheOldHireEvenAtTheSameFareAndDistance()
        {
            var ledger = Arrange();
            var box = ledger.Box;
            var cargo = box.GetPart<ContainerPart>().Contents.Single();
            var porter = ledger.Worker.GetPart<LocalPorterPart>();
            var old = HireRow(ledger);
            Assert.True(porter.TryQuote(Actor, box, ledger.Shelf, Zone, out var earlier, out var refusal), refusal);
            Assert.That(earlier.LoadedWeight, Is.EqualTo(5));

            box.AddPart(new HandlingPart { Weight = 4, Carryable = true });
            Assert.True(porter.TryQuote(Actor, box, ledger.Shelf, Zone, out var reweighed, out refusal), refusal);
            Assert.That(reweighed.LoadedWeight, Is.EqualTo(6));
            Assert.That(reweighed.Fare, Is.EqualTo(earlier.Fare));
            Assert.That(reweighed.Steps.Count, Is.EqualTo(earlier.Steps.Count));

            Assert.False(Act(ledger.ParentEntity, old.Command));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.Zero);
            Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(7));
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Open));
            Assert.That(Zone.GetEntityPosition(box), Is.EqualTo((10, 11)));
            Assert.That(box.GetPart<LocalPorterReservationPart>(), Is.Null);
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(cargo));
            Assert.That(cargo.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(cargo.GetPart<StackerPart>().StackCount, Is.EqualTo(2));

            var fresh = HireRow(ledger);
            Assert.That(fresh.Command, Is.Not.EqualTo(old.Command));
            Assert.That(fresh.Hint, Does.Contain("6 lb boxed").And.Contain("2 lb loose"));
            Assert.True(Act(ledger.ParentEntity, fresh.Command));
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Booked));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50 - reweighed.Fare));
            Assert.That(TradeSystem.GetDrams(ledger.ParentEntity), Is.EqualTo(reweighed.Fare));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearingASolidCellLowersTheNextFareAndNeverRepricesABookedParcel(bool reblock)
        {
            var ledger = Arrange();
            Assert.True(Zone.MoveEntity(ledger.Shelf, 20, 10));
            var wall = Place("Wall", 15, 10);
            var stock = ledger.Box.GetPart<ContainerPart>().Contents.Single();
            var worker = ledger.Worker;
            var porter = worker.GetPart<LocalPorterPart>();
            var oldQuote = Hire(ledger);
            Assert.True(porter.TryQuote(Actor, ledger.Box, ledger.Shelf, Zone, out var intact, out var reason), reason);
            Assert.That(intact.Steps.Count, Is.EqualTo(9));
            Assert.That(intact.Fare, Is.EqualTo(6));

            var oldFactory = DestructionSystem.EntityFactoryRef;
            DestructionSystem.EntityFactoryRef = Factory;
            try
            {
                Assert.That(DestructionSystem.Damage(wall, 1, Actor, Zone), Is.EqualTo(DestroyVerdict.Damaged));
                Assert.That(Hire(ledger), Is.EqualTo(oldQuote));
                Assert.That(DestructionSystem.Damage(wall, 100, Actor, Zone), Is.EqualTo(DestroyVerdict.Destroyed));
            }
            finally { DestructionSystem.EntityFactoryRef = oldFactory; }

            Assert.That(Zone.GetCell(15, 10).Objects.Count(e => e.BlueprintName == "Rubble"), Is.EqualTo(1));
            Assert.That(Act(ledger.ParentEntity, oldQuote), Is.False);
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
            Assert.True(porter.TryQuote(Actor, ledger.Box, ledger.Shelf, Zone, out var open, out reason), reason);
            Assert.That(open.Steps.Count, Is.EqualTo(8));
            Assert.That(open.Fare, Is.EqualTo(5));
            Assert.That(open.LoadedWeight, Is.EqualTo(intact.LoadedWeight));
            Assert.True(Act(ledger.ParentEntity, Hire(ledger)));
            Assert.True(ledger.TryAct(Zone));
            if (reblock) Place("Wall", 15, 10);

            bool crossed = false;
            for (int i = 0; i < 16 && ledger.Phase != LocalPorterPhase.Delivered; i++)
            {
                Assert.True(ledger.TryAct(Zone));
                crossed |= Zone.GetEntityPosition(worker) == (15, 10);
            }
            Assert.That(crossed, Is.EqualTo(!reblock));
            Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Delivered));
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(45));
            Assert.That(TradeSystem.GetDrams(worker), Is.EqualTo(12));
            Assert.That(ledger.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(ledger.Box));
            Assert.That(ledger.Box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stock));
        }

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

        [Test]
        public void CommittedPorterDiagnosticsCorrelateOneOrderThroughItsActualPayout()
        {
            var expected = new[] { "PorterBooked", "PorterPickedUp", "PorterSettled" };
            var prior = Diag.Snapshot(Diag.BufferCapacity).Select(e => e.TraceId).ToHashSet();
            bool previous = Diag.IsChannelEnabled("ai");
            Diag.Entry[] Recorded() => Diag.Snapshot(Diag.BufferCapacity)
                .Where(e => !prior.Contains(e.TraceId) && e.Category == "ai" && expected.Contains(e.Kind)).ToArray();

            Diag.SetChannel("ai", true);
            try
            {
                var ledger = Arrange();
                var marker = ledger.ParentEntity;
                var box = ledger.Box;
                var shelf = ledger.Shelf;
                string command = Hire(ledger);
                Assert.True(UseGuides.Collect(marker, Zone, Zone.GetEntityCell(marker), Actor, new List<string>()));
                Assert.That(Hire(ledger), Is.EqualTo(command));
                Assert.That(Recorded(), Is.Empty, "Reading and quoting do not book or pay for a delivery.");

                var failure = new FiftyWorldThrowAfterPart();
                Actor.AddPart(failure);
                try { Assert.False(Act(marker, command)); }
                finally { Actor.RemovePart(failure); }
                Assert.That(ledger.Phase, Is.EqualTo(LocalPorterPhase.Open));
                Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50));
                Assert.That(TradeSystem.GetDrams(marker), Is.Zero);
                Assert.That(Recorded(), Is.Empty, "A rolled-back booking publishes no committed record.");

                Assert.True(Act(marker, command));
                string order = ledger.OrderID;
                int fare = ledger.Fare;
                Assert.That(order, Is.Not.Null.And.Not.Empty);
                void AssertCommits(int count)
                {
                    var records = Recorded();
                    Assert.That(records.Select(r => r.Kind), Is.EqualTo(expected.Take(count)));
                    foreach (var record in records)
                    {
                        var payload = UnityEngine.JsonUtility.FromJson<PorterTrace>(record.PayloadJson);
                        Assert.That(payload.orderId, Is.EqualTo(order), record.Kind);
                        Assert.That(payload.boxId, Is.EqualTo(box.ID), record.Kind);
                        Assert.That(payload.shelfId, Is.EqualTo(shelf.ID), record.Kind);
                    }
                }

                Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50 - fare));
                Assert.That(TradeSystem.GetDrams(marker), Is.EqualTo(fare));
                Assert.False(Act(marker, command));
                AssertCommits(1);

                Assert.True(ledger.TryAct(Zone));
                Assert.That(box.GetPart<PhysicsPart>().InInventory, Is.SameAs(ledger.Worker));
                Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(8));
                Assert.That(TradeSystem.GetDrams(marker), Is.EqualTo(fare - 1));
                AssertCommits(2);

                WorkUntil(ledger, LocalPorterPhase.Delivered);
                Assert.That(shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(box));
                Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(50 - fare));
                Assert.That(TradeSystem.GetDrams(ledger.Worker), Is.EqualTo(7 + fare));
                Assert.That(TradeSystem.GetDrams(marker), Is.Zero);
                Assert.False(ledger.TryAct(Zone));
                AssertCommits(3);
            }
            finally { Diag.SetChannel("ai", previous); }
        }

        [System.Serializable]
        private sealed class PorterTrace
        {
            public string orderId = "";
            public string boxId = "";
            public string shelfId = "";
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

        private InventoryAction HireRow(LocalPorterLedgerPart ledger) =>
            Actions(ledger.ParentEntity).Single(a => a.Command.StartsWith(LocalPorterLedgerPart.HirePrefix));

        private string Hire(LocalPorterLedgerPart ledger) => HireRow(ledger).Command;

        private string Cancel(LocalPorterLedgerPart ledger) =>
            Actions(ledger.ParentEntity).Single(a => a.Command.StartsWith(LocalPorterLedgerPart.CancelPrefix)).Command;

        private void WorkUntil(LocalPorterLedgerPart ledger, LocalPorterPhase target)
        {
            for (int i = 0; i < 38 && ledger.Phase != target; i++) ledger.TryAct(Zone);
            Assert.That(ledger.Phase, Is.EqualTo(target));
        }
    }
}
