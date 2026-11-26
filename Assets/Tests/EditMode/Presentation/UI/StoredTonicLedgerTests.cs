using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ExpandedSatchelDrinksTheExactStoredTonicAndQueuesOneTurn(bool cancel)
        {
            using var r = new Rig();
            var tonic = r.Factory.CreateEntity("HealingTonic");
            var looseTonic = r.Factory.CreateEntity("HealingTonic");
            var vessel = r.Factory.CreateEntity("FieldSatchel");
            tonic.GetPart<StackerPart>().StackCount = 2;
            looseTonic.GetPart<StackerPart>().StackCount = 2;
            var container = vessel.GetPart<ContainerPart>();
            Assert.True(container.AddItem(tonic));
            Assert.True(r.Pack.AddObject(vessel));
            Assert.True(r.Pack.AddObject(looseTonic));
            Assert.True(tonic.GetPart<TonicPart>().Drink);

            r.Open();
            r.Tab(1);
            r.ActivatePainted(r.FindHit("Item", r.InventoryRow(vessel)));
            Assert.AreEqual(r.InventoryRow(vessel) + 1, r.InventoryRow(tonic));
            r.ActivatePainted(r.FindHit("Item", r.InventoryRow(tonic)));

            var options = ((IList)Get(r.UI, "_ledgerCommands")).Cast<object>().ToArray();
            var drinkRows = Enumerable.Range(0, options.Length).Where(index =>
                ((string)Get(options[index], "Label")).StartsWith("Drink", StringComparison.Ordinal)).ToArray();
            Assert.That(drinkRows, Has.Length.EqualTo(1), "The expanded child action menu must offer Drink.");
            Assert.False(r.UI.ConsumePendingEverydayTurn(), "Opening the menu is free.");

            if (cancel)
                Call(r.UI, "CancelLedgerPopup");
            else
                r.ActivatePainted(r.FindHit("PopupRow", drinkRows[0]));

            Assert.AreEqual(cancel ? 2 : 1, tonic.GetPart<StackerPart>().StackCount);
            Assert.AreSame(vessel, tonic.GetPart<PhysicsPart>().InInventory);
            CollectionAssert.AreEqual(new[] { tonic }, container.Contents);
            Assert.AreEqual(2, looseTonic.GetPart<StackerPart>().StackCount);
            Assert.AreSame(r.Player, looseTonic.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(cancel, r.UI.IsOpen);
            Assert.AreEqual(!cancel, r.UI.ConsumePendingEverydayTurn());
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }

        [TestCase("complete")]
        [TestCase("changed quantity")]
        [TestCase("late lock")]
        [TestCase("holder dropped")]
        public void StoredTonicKeepsScopedPaymentAndUsesHealingOnlyOnCommit(string outcome)
        {
            using var r = new Rig();
            var tonic = r.Factory.CreateEntity("HealingTonic");
            var vessel = r.Factory.CreateEntity("FieldSatchel");
            var container = vessel.GetPart<ContainerPart>();
            var stack = tonic.GetPart<StackerPart>();
            stack.StackCount = outcome == "changed quantity" || outcome == "holder dropped" ? 2 : 1;
            Assert.True(container.AddItem(tonic));
            Assert.True(r.Pack.AddObject(vessel));
            var hp = r.Player.GetStat("Hitpoints");
            hp.BaseValue = 10;
            int originalHp = hp.Value;
            if (outcome != "complete")
                r.Player.AddPart(new FiftyWorldEventProbe
                {
                    Callback = e =>
                    {
                        if (e.ID != "AfterInventoryAction") return;
                        hp.BaseValue -= 2;
                        if (outcome == "late lock") container.Locked = true;
                        else if (outcome == "holder dropped")
                        {
                            Assert.True(r.Pack.RemoveObject(vessel));
                            Assert.True(r.UI.CurrentZone.AddEntity(vessel, 11, 10));
                        }
                        else stack.StackCount++;
                    }
                });

            r.Open();
            r.Tab(1);
            r.ActivatePainted(r.FindHit("Item", r.InventoryRow(vessel)));
            r.ActivatePainted(r.FindHit("Item", r.InventoryRow(tonic)));
            var options = ((IList)Get(r.UI, "_ledgerCommands")).Cast<object>().ToArray();
            int drink = Array.FindIndex(options, row => ((string)Get(row, "Label")).StartsWith("Drink", StringComparison.Ordinal));
            Assert.That(drink, Is.GreaterThanOrEqualTo(0));
            r.ActivatePainted(r.FindHit("PopupRow", drink));

            if (outcome == "complete")
            {
                Assert.IsEmpty(container.Contents);
                Assert.IsNull(tonic.GetPart<PhysicsPart>().InInventory);
                Assert.That(hp.Value, Is.GreaterThan(originalHp));
                Assert.True(r.UI.ConsumePendingEverydayTurn());
            }
            else
            {
                Assert.AreEqual(originalHp - 2, hp.Value, "The independent injury remains and tonic healing was never applied.");
                Assert.AreSame(vessel, tonic.GetPart<PhysicsPart>().InInventory);
                CollectionAssert.AreEqual(new[] { tonic }, container.Contents);
                int expectedUnits = outcome == "late lock" ? 1 : outcome == "holder dropped" ? 2 : 3;
                Assert.AreEqual(expectedUnits, stack.StackCount);
                if (outcome == "holder dropped")
                {
                    Assert.False(r.Pack.Contains(vessel));
                    Assert.AreSame(r.UI.CurrentZone.GetCell(11, 10), r.UI.CurrentZone.GetEntityCell(vessel));
                }
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [TestCase("2147483649d6")]
        [TestCase("2d2147483647")]
        [TestCase("1d4-9")]
        public void AnInvalidHealingPayloadRetainsTakeWithoutOfferingDirectUse(string dice)
        {
            using var r = new Rig();
            var tonic = r.Factory.CreateEntity("HealingTonic");
            tonic.GetPart<TonicPart>().Healing = dice;
            var vessel = r.Factory.CreateEntity("FieldSatchel");
            Assert.True(vessel.GetPart<ContainerPart>().AddItem(tonic));
            Assert.True(r.Pack.AddObject(vessel));
            r.Open();
            r.Tab(1);
            r.ActivatePainted(r.FindHit("Item", r.InventoryRow(vessel)));
            r.ActivatePainted(r.FindHit("Item", r.InventoryRow(tonic)));
            var options = ((IList)Get(r.UI, "_ledgerCommands")).Cast<object>()
                .Select(row => (string)Get(row, "Label")).ToArray();
            CollectionAssert.Contains(options, "Take to pack");
            Assert.False(options.Any(label => label.StartsWith("Drink", StringComparison.Ordinal)));
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }
    }
}
