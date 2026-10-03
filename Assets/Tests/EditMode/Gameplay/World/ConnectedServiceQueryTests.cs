using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Public menu reads tolerate the absent contexts their API permits,
    /// and malformed saved service bindings refuse instead of crashing the menu.</summary>
    public sealed class ConnectedServiceQueryTests : ConnectedInkFixture
    {
        [Test] public void LiveInkDeskStillOffersItsServiceToTheAdjacentPlayer()
        {
            ConfigureDesk();
            Assert.True(WorldInteractionSystem.GatherActions(Desk, Actor).Any(a => a.Command == Prepare));
            Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(7, TradeSystem.GetDrams(Worker));
        }

        [Test] public void InkDeskActorlessPublicMenuQueryIsReadOnlyAndDoesNotThrow()
        {
            ConfigureDesk();
            List<InventoryAction> actions = null;
            Assert.DoesNotThrow(() => actions = WorldInteractionSystem.GatherActions(Desk));
            Assert.False(actions.Any(a => a.Command == Prepare));
            Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(7, TradeSystem.GetDrams(Worker));
        }

        [Test] public void SavedInkDeskMissingItsWorkerRefusesTheMenuWithoutThrowing()
        {
            var part = (BotanicalInkDeskPart)ConfigureDesk();
            part.Worker = null; // Missing saved reference; the old worker ID remains.
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Desk);
            Assert.True(loaded.GetPart<BotanicalInkDeskPart>().Configured);
            Assert.True(Zone.RemoveEntity(Desk));
            Assert.True(Zone.AddEntity(loaded, 5, 4));
            List<InventoryAction> actions = null;
            Assert.DoesNotThrow(() => actions = WorldInteractionSystem.GatherActions(loaded, Actor));
            Assert.False(actions.Any(a => a.Command == Prepare));
            Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
        }

        [Test] public void KitchenConfigureWithoutAWorkerRefusesWithoutThrowing()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                f.Pan.RemovePart(f.Batch);
                var batch = new KitchenBatchPart(); f.Pan.AddPart(batch);
                bool configured = true;
                Assert.DoesNotThrow(() => configured = batch.Configure(f.Zone, null, f.Escrow, f.Pickup));
                Assert.False(configured); Assert.False(batch.Configured);
                Assert.IsEmpty(f.Stored.Contents); Assert.IsEmpty(f.Finished.Contents);
                Assert.AreEqual(10, TradeSystem.GetDrams(f.Player));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void SavedKitchenWithMissingPickupCanBeReadButCannotOfferWork(bool ready)
        {
            using (var f = new ConnectedKitchenFixture())
            {
                if (ready) { Assert.True(f.Start()); f.Advance(KitchenBatchPart.PreparationTicks); Assert.AreEqual("Ready", f.State); }
                var batch = (KitchenBatchPart)f.Batch;
                batch.Pickup = null; batch.PickupID = null;
                var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.Pan);
                var restored = loaded.GetPart<KitchenBatchPart>();
                Assert.True(f.Zone.RemoveEntity(f.Pan));
                Assert.True(f.Zone.AddEntity(loaded, 10, 10));
                // Supply the exact restored references, so only the missing pickup
                // invalidates this saved graph rather than an unrelated stale worker.
                Assert.True(f.Zone.RemoveEntity(f.Worker)); Assert.True(f.Zone.RemoveEntity(f.Escrow));
                Assert.True(f.Zone.AddEntity(restored.Worker, 10, 9));
                Assert.True(f.Zone.AddEntity(restored.Escrow, 11, 10));
                Assert.DoesNotThrow(() => restored.Describe());
                List<InventoryAction> actions = null;
                Assert.DoesNotThrow(() => actions = WorldInteractionSystem.GatherActions(loaded, f.Player));
                Assert.False(actions.Any(a => a.Command == KitchenBatchPart.StartCommand));
                Assert.AreEqual(ready ? "Ready" : "Idle", restored.State, "A menu cannot resolve or replace the saved job.");
            }
        }
    }

    public sealed class ConnectedReserveQueryTests : ConnectedReserveTestBase
    {
        [Test] public void SavedReserveMissingOneSoilBindingRefusesWithoutCrashingTheMenu()
        {
            var original = (LocalGatheringClaimPart)Claim;
            original.FirstSoil = null; original.FirstSoilID = null;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Keeper);
            var restored = loaded.GetPart<LocalGatheringClaimPart>();
            var zone = new Zone(Reserve.ZoneID);
            Manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
            Assert.True(Reserve.RemoveEntity(Player)); Assert.True(zone.AddEntity(Player, 5, 6));
            Assert.True(zone.AddEntity(loaded, 5, 5));
            Assert.True(zone.AddEntity(restored.SecondSoil, restored.SecondX, restored.SecondY));
            Assert.True(zone.AddEntity(restored.Tray, restored.TrayX, restored.TrayY));
            List<InventoryAction> actions = null;
            Assert.DoesNotThrow(() => actions = WorldInteractionSystem.GatherActions(loaded, Player));
            Assert.False(actions.Any(a => LocalGatheringClaimPart.IsCommand(a.Command)));
            Assert.AreEqual(40, TradeSystem.GetDrams(Player));
            Assert.AreEqual(ReserveAccessState.Unknown, restored.GetState(Player));
        }
    }
}
