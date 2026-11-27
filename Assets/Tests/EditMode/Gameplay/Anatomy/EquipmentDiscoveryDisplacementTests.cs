using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Displacing old gear must not restore its bonuses onto a lost
    /// limb or a slot that an independent committed action has occupied.</summary>
    public sealed class EquipmentDiscoveryDisplacementTests
    {
        private static Entity EquipScreen(EquipmentLifecycleFixture f)
        {
            var screen = f.Item("GroundwireScreen");
            var glow = new EnhancementGlowQuartz(); glow.ApplyTier(2); screen.AddPart(glow);
            Assert.IsTrue(f.Inventory.AddObject(screen));
            Assert.IsTrue(InventorySystem.Equip(f.Actor, screen, f.LeftHand));
            Assert.AreEqual(50, f.Actor.GetStatValue("ElectricResistance"));
            Assert.IsTrue(glow.AppliedBonus); Assert.AreEqual(2, screen.GetPart<LightSourcePart>().Radius);
            return screen;
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void ReplacementRollbackRestoresOldScreenOnlyWhenItsOriginalLimbSurvives(bool injury, bool throwAfterEvent)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var screen = EquipScreen(f); var arm = f.LeftArm; var hand = f.LeftHand;
                var replacement = f.Item("Dagger"); Assert.IsTrue(f.Inventory.AddObject(replacement));
                bool dismembered = false;
                var observer = new EquipmentDiscoveryDefenseAdversarialTests.AfterEquipReaction { React = () =>
                {
                    Assert.AreSame(replacement, hand._Equipped);
                    Assert.AreEqual(0, f.Actor.GetStatValue("ElectricResistance"), "Displaced screen no longer protects during AfterEquip.");
                    Assert.IsFalse(screen.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                    if (injury) dismembered = f.Body.Dismember(arm, f.Zone);
                    if (throwAfterEvent) throw new InvalidOperationException("Fail after the independent injury.");
                }};
                f.Actor.AddPart(observer);
                if (throwAfterEvent)
                    Assert.IsFalse(InventorySystem.ExecuteCommand(new EquipCommand(replacement, hand), f.Actor, f.Zone).Success);
                else
                {
                    var transaction = new InventoryTransaction();
                    try { Assert.IsTrue(new EquipCommand(replacement, hand).Execute(new InventoryContext(f.Actor, f.Zone), transaction).Success); }
                    finally { transaction.Rollback(); }
                }
                Assert.AreEqual(1, observer.Calls); Assert.AreEqual(injury, dismembered);
                Assert.AreEqual(injury ? 0 : 50, f.Actor.GetStatValue("ElectricResistance"));
                Assert.AreEqual(!injury, screen.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                Assert.AreEqual(injury ? 0 : 2, screen.GetPart<LightSourcePart>().Radius);
                Assert.AreEqual(!injury, InventorySystem.IsEquipped(f.Actor, screen));
                if (injury)
                {
                    Assert.IsNull(arm.ParentPart); Assert.IsNull(hand._Equipped, "The severed hand must not reacquire the old screen.");
                    f.Detached(screen); Assert.IsTrue(f.Inventory.Objects.Contains(screen));
                    Assert.AreSame(f.Actor, screen.GetPart<PhysicsPart>().InInventory); f.Ground(replacement);
                }
                else
                {
                    Assert.NotNull(arm.ParentPart); Assert.AreSame(screen, hand._Equipped);
                    Assert.AreSame(f.Actor, screen.GetPart<PhysicsPart>().Equipped);
                    f.Detached(replacement); Assert.IsTrue(f.Inventory.Objects.Contains(replacement));
                }
            }
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void UnequipRollbackCannotRestoreOldBonusesWhenAnIndependentItemTakesItsSlot(bool takeover, bool throwing)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var screen = EquipScreen(f); var hand = f.LeftHand;
                var screenPhysics = screen.GetPart<PhysicsPart>();
                if (throwing)
                    screen.AddPart(new HandlingPart { Throwable = true, Weight = screenPhysics.Weight });
                var successor = f.Item("Dagger"); Assert.IsTrue(f.Inventory.AddObject(successor));
                var successorPhysics = successor.GetPart<PhysicsPart>();
                bool committed = false;
                var observer = new EquipmentLifecycleObserver { After = removed =>
                {
                    Assert.AreSame(screen, removed);
                    Assert.IsNull(hand._Equipped);
                    Assert.AreSame(f.Actor, screenPhysics.InInventory);
                    Assert.AreEqual(0, f.Actor.GetStatValue("ElectricResistance"));
                    if (takeover) committed = InventorySystem.Equip(f.Actor, successor, hand);
                    if (throwing) Assert.IsTrue(f.Zone.MoveEntity(f.Actor, 6, 5));
                }};
                f.Actor.AddPart(observer);
                var context = new InventoryContext(f.Actor, f.Zone);
                if (throwing)
                {
                    // The callback completes the nested equip and moves the thrower
                    // before launch, invalidating the original launch cell.
                    var command = new ThrowItemCommand(screen, 5, 6, new Random(17));
                    var validation = command.Validate(context);
                    Assert.IsTrue(validation.IsValid, validation.ErrorMessage);
                    var result = InventorySystem.ExecuteCommand(command, f.Actor, f.Zone);
                    Assert.IsFalse(result.Success);
                    Assert.AreEqual(InventoryCommandErrorCode.ExecutionFailed, result.ErrorCode);
                    Assert.IsFalse(result.ActionCommitted);
                    StringAssert.Contains("changed while readying the throw", result.ErrorMessage);
                    Assert.AreEqual((6, 5), f.Zone.GetEntityPosition(f.Actor));
                }
                else
                {
                    var transaction = new InventoryTransaction();
                    try { Assert.IsTrue(new UnequipCommand(screen).Execute(context, transaction).Success); }
                    finally { transaction.Rollback(); }
                }
                Assert.AreEqual(1, observer.AfterCount); Assert.AreEqual(takeover, committed);
                Assert.AreSame(takeover ? successor : screen, hand._Equipped);
                Assert.AreEqual(takeover ? 0 : 50, f.Actor.GetStatValue("ElectricResistance"));
                Assert.AreEqual(!takeover, screen.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                Assert.AreEqual(takeover ? 0 : 2, screen.GetPart<LightSourcePart>().Radius);
                Assert.AreEqual(!takeover, InventorySystem.IsEquipped(f.Actor, screen));
                Assert.AreEqual(takeover, InventorySystem.IsEquipped(f.Actor, successor));
                Assert.AreEqual(takeover ? 1 : 0, f.Inventory.Objects.Count(item => ReferenceEquals(item, screen)));
                Assert.AreEqual(takeover ? 0 : 1, f.Inventory.Objects.Count(item => ReferenceEquals(item, successor)));
                Assert.IsNull(f.Zone.GetEntityCell(screen), "The rolled-back throw must not leave a second world owner.");
                Assert.IsNull(f.Zone.GetEntityCell(successor));
                if (takeover)
                {
                    f.Detached(screen);
                    Assert.AreSame(f.Actor, screenPhysics.InInventory);
                    Assert.AreSame(f.Actor, successorPhysics.Equipped);
                    Assert.IsNull(successorPhysics.InInventory);
                }
                else
                {
                    f.Detached(successor);
                    Assert.AreSame(f.Actor, successorPhysics.InInventory);
                    Assert.AreSame(f.Actor, screenPhysics.Equipped);
                    Assert.IsNull(screenPhysics.InInventory);
                }
            }
        }
    }
}
