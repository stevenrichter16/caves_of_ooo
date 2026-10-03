using System;
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
                var successor = f.Item("Dagger"); Assert.IsTrue(f.Inventory.AddObject(successor));
                bool committed = false;
                var observer = new EquipmentLifecycleObserver { After = removed =>
                {
                    Assert.AreSame(screen, removed);
                    if (takeover) committed = InventorySystem.Equip(f.Actor, successor, hand);
                }};
                f.Actor.AddPart(observer);
                var transaction = new InventoryTransaction();
                // Throwing exercises the symmetric UnequipAndRemove helper;
                // its ground-placement rollback must return the real screen.
                IInventoryCommand command = throwing ? (IInventoryCommand)new ThrowItemCommand(screen, 5, 6, new Random(17))
                    : new UnequipCommand(screen);
                try { Assert.IsTrue(command.Execute(new InventoryContext(f.Actor, f.Zone), transaction).Success); }
                finally { transaction.Rollback(); }
                Assert.AreEqual(1, observer.AfterCount); Assert.AreEqual(takeover, committed);
                Assert.AreSame(takeover ? successor : screen, hand._Equipped);
                Assert.AreEqual(takeover ? 0 : 50, f.Actor.GetStatValue("ElectricResistance"));
                Assert.AreEqual(!takeover, screen.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                Assert.AreEqual(takeover ? 0 : 2, screen.GetPart<LightSourcePart>().Radius);
                Assert.AreEqual(!takeover, InventorySystem.IsEquipped(f.Actor, screen));
                Assert.IsNull(f.Zone.GetEntityCell(screen), "The rolled-back throw must not leave a second world owner.");
                if (takeover)
                {
                    f.Detached(screen); Assert.IsTrue(f.Inventory.Objects.Contains(screen));
                    Assert.AreSame(f.Actor, screen.GetPart<PhysicsPart>().InInventory);
                }
                else Assert.IsTrue(f.Inventory.Objects.Contains(successor));
            }
        }
    }
}
