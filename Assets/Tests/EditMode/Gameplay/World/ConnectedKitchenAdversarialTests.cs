using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedKitchenAdversarialTests
    {
        sealed class NestedReconcile : Part
        {
            internal ConnectedKitchenFixture Fixture;
            internal bool NestedStarted;
            public override string Name => "KitchenNestedReconcile";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "AfterInventoryAction") return true;
                Fixture.Clock.AdvanceClock(120);
                ConnectedKitchenFixture.CallStatic("ReconcileZone", Fixture.Zone);
                NestedStarted = Fixture.Start(); return true;
            }
        }
        sealed class ThrowAfter : Part
        {
            public override string Name => "KitchenThrowAfter";
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("Outer action failed."); return true; }
        }
        [TestCase("actor-far")][TestCase("dead-actor")][TestCase("dead-worker")][TestCase("duplicate-grain")]
        [TestCase("equipped-grain")][TestCase("wrong-station-id")][TestCase("wrong-worker-id")][TestCase("foreign-zone")]
        [TestCase("escrow-full")][TestCase("output-occupied")][TestCase("receiver-overflow")][TestCase("missing-pulp")]
        public void InvalidStartCannotSpendAnyAvailableInput(string fault)
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var grain = f.Pack.Objects.Single(e => e.BlueprintName == "Emberwheat");
                if (fault == "actor-far") f.Zone.MoveEntity(f.Player, 30, 20);
                if (fault == "dead-actor") f.Player.GetStat("Hitpoints").BaseValue = 0;
                if (fault == "dead-worker") f.Worker.GetStat("Hitpoints").BaseValue = 0;
                if (fault == "duplicate-grain") f.Pack.Objects.Add(grain);
                if (fault == "equipped-grain") grain.GetPart<PhysicsPart>().Equipped = f.Player;
                if (fault == "wrong-station-id") f.Batch.GetType().GetField("StationID").SetValue(f.Batch, "forged");
                if (fault == "wrong-worker-id") f.Batch.GetType().GetField("WorkerID").SetValue(f.Batch, "forged");
                if (fault == "foreign-zone") { f.Zone.RemoveEntity(f.Pan); new Zone(f.Zone.ZoneID).AddEntity(f.Pan, 10, 10); }
                if (fault == "escrow-full") f.Stored.MaxItems = 0;
                if (fault == "output-occupied") f.Finished.AddItem(f.Factory.CreateEntity("FireClay"));
                if (fault == "receiver-overflow") f.Worker.IntProperties[TradeSystem.CURRENCY_PROP] = int.MaxValue;
                if (fault == "missing-pulp") f.Pack.RemoveObject(f.Pack.Objects.Single(e => e.BlueprintName == "ClaspbeanPulp"));
                int count = f.Units("Emberwheat"), pulp = f.Units("ClaspbeanPulp");
                Assert.False(f.Start()); Assert.AreEqual(count, f.Units("Emberwheat")); Assert.AreEqual(pulp, f.Units("ClaspbeanPulp"));
                Assert.AreEqual(10, TradeSystem.GetDrams(f.Player)); Assert.AreEqual("Idle", f.State); Assert.IsEmpty(f.Stored.Contents);
            }
        }
        [Test] public void OuterStartFailureRestoresFoodFeeAndIdleState()
        {
            using (var f = new ConnectedKitchenFixture())
            { f.Player.AddPart(new ThrowAfter()); Assert.False(f.Start()); f.Unspent(); }
        }
        [Test] public void OuterEatFailureRestoresTheSameBleedMealAndHealth()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var meal = f.Give("FieldMeal", 1); f.Player.GetStat("Hitpoints").BaseValue = 10;
                var status = new StatusEffectsPart(); f.Player.AddPart(status); var bleed = new BleedingEffect(18, "1d3"); status.ApplyEffect(bleed);
                f.Player.AddPart(new ThrowAfter());
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(meal, "Eat"), f.Player, f.Zone).Success);
                Assert.AreEqual(10, f.Player.GetStatValue("Hitpoints")); Assert.AreSame(bleed, status.GetEffect<BleedingEffect>());
                Assert.AreEqual(1, f.Units("FieldMeal")); Assert.AreSame(f.Player, meal.GetPart<PhysicsPart>().InInventory);
            }
        }
        [Test] public void AnAlreadyWorkingStationRefusesAnotherPaymentEvenWithEnoughFood()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Give("Emberwheat", 2); f.Give("ClaspbeanPulp", 1);
                Assert.False(f.Start()); Assert.AreEqual(8, TradeSystem.GetDrams(f.Player)); Assert.AreEqual(2, f.Units("Emberwheat"));
            }
        }
        [Test] public void MissingMealDefinitionBlocksWithoutDestroyingEscrow()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); var bp = f.Factory.Blueprints["FieldMeal"]; f.Factory.Blueprints.Remove("FieldMeal");
                f.Advance(120); Assert.AreEqual("Working", f.State); Assert.IsNotEmpty(f.Stored.Contents);
                f.Factory.Blueprints["FieldMeal"] = bp; f.Advance(1); Assert.AreEqual("Ready", f.State);
            }
        }
        [Test] public void UnrelatedOwnerInvalidationDoesNotResolveThePaidJob()
        {
            using (var f = new ConnectedKitchenFixture())
            { Assert.True(f.Start()); f.Invalidate(f.Player); Assert.AreEqual("Working", f.State); Assert.IsNotEmpty(f.Stored.Contents); }
        }
        [Test] public void RepeatedUnfinishedInvalidationDoesNotDuplicateSalvage()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Invalidate(f.Pan); f.Invalidate(f.Pan); f.Advance(1000);
                Assert.IsEmpty(f.Stored.Contents); Assert.IsEmpty(f.Finished.Contents);
                Assert.AreEqual(3, f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Emberwheat" || e.BlueprintName == "ClaspbeanPulp").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            }
        }
        [Test] public void TamperedEscrowQuantityDoesNotProduceAMealOrRecreateMissingFood()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Stored.Contents.Single(e => e.BlueprintName == "Emberwheat").GetPart<StackerPart>().StackCount = 1;
                f.Advance(120); Assert.IsEmpty(f.Finished.Contents);
                Assert.AreEqual(1, f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Emberwheat").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            }
        }
        [Test] public void DeadActorCannotConsumeThePreparedMeal()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var meal = f.Give("FieldMeal", 1); f.Player.GetStat("Hitpoints").BaseValue = 0;
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(meal, "Eat"), f.Player, f.Zone).Success);
                Assert.AreEqual(1, f.Units("FieldMeal")); Assert.AreEqual(0, f.Player.GetStatValue("Hitpoints"));
            }
        }
        [Test] public void CallbackCannotCompleteAnUncommittedJobOrChargeAnotherBatch()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var probe = new NestedReconcile { Fixture = f }; f.Player.AddPart(probe);
                Assert.True(f.Start()); Assert.False(probe.NestedStarted); Assert.AreEqual("Working", f.State);
                Assert.IsEmpty(f.Finished.Contents); Assert.AreEqual(8, TradeSystem.GetDrams(f.Player));
                f.Player.RemovePart(probe); f.Advance(1); Assert.AreEqual("Ready", f.State);
            }
        }
        [Test] public void ZoneReconciliationMayPublishSalvageWithoutInvalidatingItsOwnIteration()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Worker.GetStat("Hitpoints").BaseValue = 0;
                Assert.DoesNotThrow(() => ConnectedKitchenFixture.CallStatic("ReconcileZone", f.Zone));
                Assert.IsEmpty(f.Stored.Contents); Assert.IsEmpty(f.Finished.Contents);
                Assert.AreEqual(3, f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Emberwheat" || e.BlueprintName == "ClaspbeanPulp").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            }
        }
        [Test] public void OversizedGrainStackLeavesItsUncommittedRemainderInThePack()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var grain = f.Pack.Objects.Single(e => e.BlueprintName == "Emberwheat"); grain.GetPart<StackerPart>().StackCount = 5;
                Assert.True(f.Start()); Assert.AreEqual(3, f.Units("Emberwheat"));
                f.Advance(120); Assert.AreEqual(3, f.Units("Emberwheat")); Assert.AreEqual(1, f.Finished.Contents.Count);
            }
        }
        [Test] public void FinishedParcelKeepsItsCommissionIdentityAfterTheCookDies()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Advance(120); var meal = f.Finished.Contents.Single();
                f.Invalidate(f.Worker); f.Zone.RemoveEntity(f.Worker);
                f.Finished.RemoveItem(meal); f.Pack.AddObject(meal);
                Assert.True((bool)ConnectedKitchenFixture.Call(f.Batch, "OwnsCommissionedMeal", f.Player, meal, f.Zone));
                Assert.False((bool)ConnectedKitchenFixture.Call(f.Batch, "OwnsCommissionedMeal", f.Player, f.Factory.CreateEntity("FieldMeal"), f.Zone));
            }
        }
        [Test] public void DestroyedEscrowFoodIsLostRatherThanReturnedOrCooked()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); var grain = f.Stored.Contents.Single(e => e.BlueprintName == "Emberwheat");
                grain.AddPart(new DestructiblePart { HP = 0, MaxHP = 10, Gone = true });
                f.Advance(120); Assert.IsEmpty(f.Finished.Contents); Assert.IsEmpty(f.Stored.Contents);
                Assert.False(f.Zone.GetReadOnlyEntities().Contains(grain));
                Assert.AreEqual(1, f.Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "ClaspbeanPulp"));
            }
        }
    }
}
