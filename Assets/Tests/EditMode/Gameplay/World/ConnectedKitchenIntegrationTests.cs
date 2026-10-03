using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Real owner lifecycles must settle kitchen custody before native
    /// death drops or structural spillage. No direct call to the kitchen hook.</summary>
    public sealed class ConnectedKitchenIntegrationTests
    {
        sealed class RefuseDestruction : Part
        {
            public override bool HandleEvent(GameEvent e) => e.ID != "BeforeDestroy";
        }
        public sealed class MealCreatedProbe : Part
        {
            public static Action Callback;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ObjectCreated") Callback?.Invoke();
                return true;
            }
        }
        sealed class DestructionNotifications : Part
        {
            public int Count;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Destroyed") Count++;
                return true;
            }
        }
        static int GroundUnits(ConnectedKitchenFixture f, string blueprint) => f.Zone.GetReadOnlyEntities()
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        static void AssertSettled(ConnectedKitchenFixture f, int tick, bool pickupDestroyed)
        {
            bool due = tick >= 120;
            Assert.IsEmpty(f.Stored.Contents, "Settlement runs while escrow ownership still exists.");
            Assert.AreEqual(due && !pickupDestroyed ? 1 : 0, f.Finished.Contents.Count);
            Assert.AreEqual(due && pickupDestroyed ? 1 : 0, GroundUnits(f, "FieldMeal"));
            Assert.AreEqual(due ? 0 : 2, GroundUnits(f, "Emberwheat"));
            Assert.AreEqual(due ? 0 : 1, GroundUnits(f, "ClaspbeanPulp"));
            Assert.AreEqual(8, TradeSystem.GetDrams(f.Player), "Interruption does not refund the stated service fee.");
            Assert.AreEqual(0, f.Units("Emberwheat")); Assert.AreEqual(0, f.Units("ClaspbeanPulp"));
        }
        [TestCase(119)] [TestCase(120)]
        public void ActualCookDeathSettlesDueWorkBeforeRemovalAndNeverDuplicates(int tick)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                // Native inventory death drops remain distinct from held customer ingredients.
                var marker = f.Factory.CreateEntity("Dagger");
                Assert.True(f.Worker.GetPart<InventoryPart>().AddObject(marker));
                Assert.True(f.Start()); f.Clock.AdvanceClock(tick);
                CombatSystem.HandleDeath(f.Worker, null, f.Zone);
                Assert.Null(f.Zone.GetEntityCell(f.Worker));
                Assert.NotNull(f.Zone.GetEntityCell(marker));
                AssertSettled(f, tick, false);
                CombatSystem.HandleDeath(f.Worker, null, f.Zone);
                KitchenBatchPart.ReconcileZone(f.Zone);
                AssertSettled(f, tick, false);
                Assert.AreEqual(1, f.Zone.GetReadOnlyEntities().Count(e => e == marker));
            }
        }
        [TestCase("pan", 119)] [TestCase("pan", 120)]
        [TestCase("escrow", 119)] [TestCase("escrow", 120)]
        [TestCase("pickup", 119)] [TestCase("pickup", 120)]
        public void ActualDestructionSettlesBeforeSpillageAndRemoval(string owner, int tick)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Entity target = owner == "pan" ? f.Pan : owner == "escrow" ? f.Escrow : f.Pickup;
                target.AddPart(new DestructiblePart());
                Assert.True(f.Start()); f.Clock.AdvanceClock(tick);
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(target, null, f.Zone, "test"));
                Assert.Null(f.Zone.GetEntityCell(target));
                AssertSettled(f, tick, owner == "pickup");
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(target, null, f.Zone, "test-repeat"));
                KitchenBatchPart.ReconcileZone(f.Zone);
                AssertSettled(f, tick, owner == "pickup");
            }
        }
        [TestCase("pan", false)] [TestCase("pan", true)]
        [TestCase("escrow", false)] [TestCase("escrow", true)]
        [TestCase("pickup", false)] [TestCase("pickup", true)]
        public void DueMealCreationCannotReenterItsOwnersNativeDestruction(string owner, bool reenter)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Entity target = owner == "pan" ? f.Pan : owner == "escrow" ? f.Escrow : f.Pickup;
                target.AddPart(new DestructiblePart { WreckageBlueprint = "Rubble" });
                var notifications = new DestructionNotifications(); target.AddPart(notifications);
                var oldFactory = DestructionSystem.EntityFactoryRef;
                int created = 0, reentries = 0;
                DestroyVerdict reenteredVerdict = DestroyVerdict.NoTarget;
                string destructionLine = target.GetDisplayName() + " is destroyed.";
                int oldMessages = MessageLog.GetRecent(100).Count(line => line == destructionLine);
                try
                {
                    DestructionSystem.EntityFactoryRef = f.Factory;
                    f.Factory.RegisterPartType<MealCreatedProbe>("KitchenMealCreatedProbe");
                    f.Factory.Blueprints["FieldMeal"].Parts["KitchenMealCreatedProbe"] = new Dictionary<string, string>();
                    MealCreatedProbe.Callback = () =>
                    {
                        created++;
                        if (!reenter) return;
                        reentries++;
                        reenteredVerdict = DestructionSystem.Destroy(target, null, f.Zone, "meal-callback");
                    };
                    Assert.True(f.Start()); f.Clock.AdvanceClock(120);
                    Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(target, null, f.Zone, "test"));
                    Assert.AreEqual(1, created, "Due settlement must actually cross the factory callback boundary.");
                    Assert.AreEqual(reenter ? 1 : 0, reentries);
                    if (reenter) Assert.AreEqual(DestroyVerdict.Destroyed, reenteredVerdict);
                    Assert.AreEqual(1, notifications.Count, "A meal callback cannot publish a second destruction notification.");
                    Assert.Null(f.Zone.GetEntityCell(target)); Assert.True(target.GetPart<DestructiblePart>().Gone);
                    Assert.AreEqual(1, GroundUnits(f, "Rubble"));
                    Assert.AreEqual(oldMessages + 1, MessageLog.GetRecent(100).Count(line => line == destructionLine));
                    AssertSettled(f, 120, owner == "pickup");
                    Assert.AreEqual(owner == "pickup" ? "Idle" : "Ready", f.State);
                    Assert.AreEqual(37, TradeSystem.GetDrams(f.Worker));
                    DestructionSystem.Destroy(target, null, f.Zone, "test-repeat");
                    KitchenBatchPart.ReconcileZone(f.Zone);
                    Assert.AreEqual(1, notifications.Count); Assert.AreEqual(1, created);
                    Assert.AreEqual(1, GroundUnits(f, "Rubble"));
                    AssertSettled(f, 120, owner == "pickup");
                }
                finally
                {
                    MealCreatedProbe.Callback = null;
                    DestructionSystem.EntityFactoryRef = oldFactory;
                }
            }
        }
        [TestCase(119)] [TestCase(120)]
        public void VetoedNativeDestructionDoesNotInterruptOrPrematurelyPublishWork(int tick)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                f.Pan.AddPart(new DestructiblePart()); f.Pan.AddPart(new RefuseDestruction());
                Assert.True(f.Start()); f.Clock.AdvanceClock(tick);
                var held = f.Stored.Contents.ToArray();
                Assert.AreEqual(DestroyVerdict.Vetoed, DestructionSystem.Destroy(f.Pan, null, f.Zone, "test"));
                Assert.NotNull(f.Zone.GetEntityCell(f.Pan)); Assert.False(f.Pan.GetPart<DestructiblePart>().Gone);
                Assert.AreEqual("Working", f.State); CollectionAssert.AreEqual(held, f.Stored.Contents);
                Assert.IsEmpty(f.Finished.Contents); Assert.AreEqual(0, GroundUnits(f, "Emberwheat"));
                f.Advance(120 - tick); Assert.AreEqual("Ready", f.State);
                Assert.AreEqual(1, f.Finished.Contents.Count);
            }
        }
    }
}
