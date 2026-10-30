using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class StatModifierTests
    {
        static Entity Actor()
        {
            var actor = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "ModifierTestActor" };
            actor.AddPart(new RenderPart { DisplayName = "modifier wearer" });
            actor.AddPart(new InventoryPart { MaxWeight = 100 });
            AddStat(actor, "Strength", 12); AddStat(actor, "Agility", 10);
            return actor;
        }

        static void AddStat(Entity actor, string name, int value) => actor.Statistics[name] =
            new Stat { Owner = actor, Name = name, BaseValue = value, Min = 0, Max = 30 };

        static Entity Item(string bonuses, string slot = "Head")
        {
            var item = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "ModifierTestItem" };
            item.SetTag("Item");
            item.AddPart(new RenderPart { DisplayName = "modifier item" });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            item.AddPart(new EquippablePart { Slot = slot, EquipBonuses = bonuses });
            return item;
        }

        static void Equip(Entity actor, Entity item)
        {
            Assert.True(actor.GetPart<InventoryPart>().AddObject(item));
            Assert.True(InventorySystem.Equip(actor, item));
        }

        [Test]
        public void ParseTrimsCombinesAndSkipsMalformedEntries()
        {
            var parsed = StatModifiers.Parse(" Strength : +3,Agility:-2,Strength:-1,broken, :4,Speed:nope,Other:0");
            Assert.AreEqual("Agility:-2,Strength:2", StatModifiers.Format(parsed));
            Assert.Throws<OverflowException>(() => StatModifiers.Parse("Strength:2147483647,Strength:1"));
        }

        [Test]
        public void IndependentSignedSourcesRemoveWithoutTouchingBaseOrOtherBonuses()
        {
            var actor = Actor(); actor.GetStat("Strength").Bonus = 4;
            var gear = StatModifiers.Apply(actor, StatModifiers.Parse("Strength:3,Agility:-2"));
            var effect = StatModifiers.Apply(actor, new[] { new StatModifier("Strength", -1) });
            Assert.AreEqual(18, actor.GetStatValue("Strength")); Assert.AreEqual(8, actor.GetStatValue("Agility"));
            StatModifiers.Remove(actor, gear);
            Assert.AreEqual(15, actor.GetStatValue("Strength")); Assert.AreEqual(10, actor.GetStatValue("Agility"));
            StatModifiers.Remove(actor, effect);
            Assert.AreEqual(4, actor.GetStat("Strength").Bonus); Assert.AreEqual(12, actor.GetStat("Strength").BaseValue);
        }

        [Test]
        public void ReplaceIsIdempotentAndChangesOnlyRecordedContribution()
        {
            var actor = Actor(); actor.GetStat("Strength").Bonus = 4;
            var receipt = StatModifiers.Apply(actor, StatModifiers.Parse("Strength:3"));
            receipt = StatModifiers.Replace(actor, receipt, StatModifiers.Parse("Strength:3"));
            Assert.AreEqual(7, actor.GetStat("Strength").Bonus);
            receipt = StatModifiers.Replace(actor, receipt, StatModifiers.Parse("Strength:-2,Agility:1"));
            Assert.AreEqual(2, actor.GetStat("Strength").Bonus); Assert.AreEqual(11, actor.GetStatValue("Agility"));
            StatModifiers.Remove(actor, receipt);
            Assert.AreEqual(4, actor.GetStat("Strength").Bonus); Assert.AreEqual(10, actor.GetStatValue("Agility"));
        }

        [Test]
        public void ClampingDoesNotLoseTheActualContribution()
        {
            var actor = Actor(); actor.GetStat("Strength").BaseValue = 29;
            var receipt = StatModifiers.Apply(actor, StatModifiers.Parse("Strength:5,Agility:-20"));
            Assert.AreEqual(30, actor.GetStatValue("Strength")); Assert.AreEqual(0, actor.GetStatValue("Agility"));
            StatModifiers.Remove(actor, receipt);
            Assert.AreEqual(29, actor.GetStatValue("Strength")); Assert.AreEqual(10, actor.GetStatValue("Agility"));
        }

        [Test]
        public void MissingStatsNeverAcquireAnUnearnedRemovalAndResistancesAreOptIn()
        {
            var actor = Actor();
            var receipt = StatModifiers.Apply(actor, StatModifiers.Parse("Unknown:5,HeatResistance:15"));
            Assert.AreEqual(0, receipt.Count); Assert.Null(actor.GetStat("HeatResistance"));
            AddStat(actor, "Unknown", 7); StatModifiers.Remove(actor, receipt);
            Assert.AreEqual(7, actor.GetStatValue("Unknown"));
            receipt = StatModifiers.Apply(actor, StatModifiers.Parse("HeatResistance:15,HeatResistnce:5"), true);
            Assert.AreEqual(15, actor.GetStatValue("HeatResistance")); Assert.Null(actor.GetStat("HeatResistnce"));
            StatModifiers.Remove(actor, receipt); Assert.AreEqual(0, actor.GetStatValue("HeatResistance"));
        }

        [Test]
        public void OverflowRejectsAllChangesIncludingCreationOfImplicitStats()
        {
            var actor = Actor();
            Assert.Throws<OverflowException>(() => StatModifiers.Apply(actor,
                StatModifiers.Parse("HeatResistance:5,Agility:2,Strength:2147483647"), true));
            Assert.AreEqual(0, actor.GetStat("Strength").Bonus); Assert.AreEqual(0, actor.GetStat("Agility").Bonus);
            Assert.Null(actor.GetStat("HeatResistance"));
        }

        [Test]
        public void EquipmentRoundTripRemovesExactOriginalReceiptAfterDefinitionChanges()
        {
            var actor = Actor(); var item = Item("Strength:3,Agility:-2,Missing:7"); Equip(actor, item);
            item.GetPart<EquippablePart>().EquipBonuses = "Strength:20,Agility:9,Missing:7";
            AddStat(actor, "Missing", 8);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedItem = loaded.GetPart<InventoryPart>().GetAllEquipped().Single();
            Assert.AreEqual(15, loaded.GetStatValue("Strength")); Assert.AreEqual(8, loaded.GetStatValue("Agility"));
            Assert.AreEqual("Agility:-2,Strength:3", loadedItem.GetPart<EquippablePart>().AppliedEquipBonuses);
            Assert.True(InventorySystem.UnequipItem(loaded, loadedItem));
            Assert.AreEqual(12, loaded.GetStatValue("Strength")); Assert.AreEqual(10, loaded.GetStatValue("Agility"));
            Assert.AreEqual(8, loaded.GetStatValue("Missing"));
        }

        [Test]
        public void LegacyEquipmentWithoutReceiptStillUnequipsAfterLoad()
        {
            var actor = Actor(); var item = Item("Strength:3,Agility:-2"); Equip(actor, item);
            item.GetPart<EquippablePart>().AppliedEquipBonuses = null;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.True(InventorySystem.UnequipItem(loaded, loaded.GetPart<InventoryPart>().GetAllEquipped().Single()));
            Assert.AreEqual(12, loaded.GetStatValue("Strength")); Assert.AreEqual(10, loaded.GetStatValue("Agility"));
        }

        [TestCase(false)] [TestCase(true)]
        public void TinkeringEquippedArmorExtendsReceiptAndRemovesAfterLoad(bool legacy)
        {
            var actor = Actor(); var item = Item("Strength:1,Agility:-1", "Body");
            item.AddPart(new ArmorPart { AV = 3 }); Equip(actor, item);
            if (legacy) item.GetPart<EquippablePart>().AppliedEquipBonuses = null;
            Assert.True(new DuelistCutTinkerModification().Apply(item, out string reason), reason);
            Assert.AreEqual(11, actor.GetStatValue("Agility"));
            Assert.AreEqual("Agility:1,Strength:1", item.GetPart<EquippablePart>().AppliedEquipBonuses);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.True(InventorySystem.UnequipItem(loaded, loaded.GetPart<InventoryPart>().GetAllEquipped().Single()));
            Assert.AreEqual(12, loaded.GetStatValue("Strength")); Assert.AreEqual(10, loaded.GetStatValue("Agility"));
        }

        [Test]
        public void EquipmentRepeatedApplyDoesNotStackAndRepeatedRemovalIsQuiet()
        {
            var actor = Actor(); var item = Item("Strength:3,Agility:-2"); var part = item.GetPart<EquippablePart>();
            var utility = typeof(InventorySystem).Assembly.GetType("CavesOfOoo.Core.Inventory.Commands.EquipBonusUtility");
            Assert.NotNull(utility);
            var apply = utility.GetMethod("ApplyEquipBonuses");
            Assert.NotNull(apply);
            void Apply(bool enabled)
            {
                var arguments = apply.GetParameters().Select(p => Type.Missing as object).ToArray();
                arguments[0] = actor; arguments[1] = part; arguments[2] = enabled;
                apply.Invoke(null, arguments);
            }
            Apply(true); Apply(true);
            Assert.AreEqual(15, actor.GetStatValue("Strength")); Assert.AreEqual(8, actor.GetStatValue("Agility"));
            Apply(false); Apply(false);
            Assert.AreEqual(12, actor.GetStatValue("Strength")); Assert.AreEqual(10, actor.GetStatValue("Agility"));
        }

        [TestCase(false)] [TestCase(true)]
        public void AlreadyWornItemCannotAcquireAnotherSlotEvenWhenValidationIsBypassed(bool bypassValidation)
        {
            var actor = Actor(); var item = Item("Strength:3,Agility:-2", "Head"); Equip(actor, item);
            item.GetPart<EquippablePart>().Slot = "Back";
            if (bypassValidation)
            {
                var transaction = new InventoryTransaction();
                Assert.False(new EquipCommand(item).Execute(new InventoryContext(actor), transaction).Success);
                transaction.Rollback();
            }
            else Assert.False(InventorySystem.Equip(actor, item));
            Assert.AreSame(item, actor.GetPart<InventoryPart>().GetEquipped("Head"));
            Assert.Null(actor.GetPart<InventoryPart>().GetEquipped("Back"));
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().EquippedItems.Count);
            Assert.AreEqual(15, actor.GetStatValue("Strength")); Assert.AreEqual(8, actor.GetStatValue("Agility"));
            Assert.True(InventorySystem.UnequipItem(actor, item));
            Assert.AreEqual(12, actor.GetStatValue("Strength")); Assert.AreEqual(10, actor.GetStatValue("Agility"));
        }

        [Test]
        public void CloneDoesNotInheritSourceEquipmentContribution()
        {
            var actor = Actor(); var item = Item("Strength:3"); Equip(actor, item);
            var clone = item.CloneForStack();
            Assert.AreEqual("", clone.GetPart<EquippablePart>().AppliedEquipBonuses);
            Assert.Null(clone.GetPart<PhysicsPart>().Equipped);
            var other = Actor(); Equip(other, clone);
            Assert.AreEqual(15, other.GetStatValue("Strength"));
            Assert.True(InventorySystem.UnequipItem(other, clone)); Assert.AreEqual(12, other.GetStatValue("Strength"));
            Assert.AreEqual(15, actor.GetStatValue("Strength"));
        }

        [Test]
        public void FailedOuterInventoryActionRestoresEquipmentAndItsStatReceipt()
        {
            var actor = Actor(); var item = Item("Strength:3,Agility:-2");
            item.AddPart(new UnequipDuringModifierAction()); Equip(actor, item);
            Assert.False(InventorySystem.ExecuteCommand(new FailAfterModifierAction(item), actor).Success);
            Assert.True(InventorySystem.IsEquipped(actor, item)); Assert.AreEqual(15, actor.GetStatValue("Strength"));
            Assert.AreEqual("Agility:-2,Strength:3", item.GetPart<EquippablePart>().AppliedEquipBonuses);
            Assert.True(InventorySystem.UnequipItem(actor, item));
            Assert.AreEqual(12, actor.GetStatValue("Strength")); Assert.AreEqual(10, actor.GetStatValue("Agility"));
        }

        [Test]
        public void ThrowingAfterUnequipRestoresSlotAndOriginalReceiptDespiteChangedDefinition()
        {
            var actor = Actor(); var item = Item("Strength:3,Agility:-2"); Equip(actor, item);
            item.GetPart<EquippablePart>().EquipBonuses = "Strength:9,Agility:7";
            var observer = new RejectModifierUnequip(); actor.AddPart(observer);
            Assert.False(InventorySystem.UnequipItem(actor, item));
            Assert.True(InventorySystem.IsEquipped(actor, item));
            Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(15, actor.GetStatValue("Strength")); Assert.AreEqual(8, actor.GetStatValue("Agility"));
            Assert.AreEqual("Agility:-2,Strength:3", item.GetPart<EquippablePart>().AppliedEquipBonuses);
            actor.RemovePart(observer);
            Assert.True(InventorySystem.UnequipItem(actor, item));
            Assert.AreEqual(12, actor.GetStatValue("Strength")); Assert.AreEqual(10, actor.GetStatValue("Agility"));
        }

        public sealed class RejectModifierUnequip : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterUnequip") throw new InvalidOperationException("Expected modifier unequip rollback.");
                return true;
            }
        }

        public sealed class UnequipDuringModifierAction : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "TestModifierUnequip") return true;
                bool success = InventorySystem.UnequipItem(e.GetParameter<Entity>("Actor"), ParentEntity);
                e.Handled = success; return !success;
            }
        }

        sealed class FailAfterModifierAction : IInventoryCommand
        {
            readonly Entity item;
            public string Name => "FailAfterModifierAction";
            internal FailAfterModifierAction(Entity item) { this.item = item; }
            public InventoryValidationResult Validate(InventoryContext context) => InventoryValidationResult.Valid();
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = new PerformInventoryActionCommand(item, "TestModifierUnequip").Execute(context, transaction);
                return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Expected test rollback.") : result;
            }
        }
    }
}
