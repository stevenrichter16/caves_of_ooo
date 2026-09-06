using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class SeparateOneFixture : SplitIdentityFixture
    {
        protected static IInventoryCommand Command(Entity source)
        {
            var type = typeof(InventorySystem).Assembly.GetType("CavesOfOoo.Core.Inventory.Commands.SeparateOneCommand");
            Assert.NotNull(type, "Separate one must be available through the inventory command pipeline.");
            return (IInventoryCommand)Activator.CreateInstance(type, source);
        }
        protected static Entity Recipient(IInventoryCommand command)
        {
            var property = command.GetType().GetProperty("SeparatedItem");
            Assert.NotNull(property, "Successful separation must return its exact recipient.");
            return (Entity)property.GetValue(command);
        }
        protected Entity Separate(Entity actor, Entity source)
        {
            var command = Command(source); var result = InventorySystem.ExecuteCommand(command, actor);
            Assert.IsTrue(result.Success, result.ErrorMessage); var unit = Recipient(command); Assert.NotNull(unit); return unit;
        }
    }
    public class GameAuditSeparateOneTests : SeparateOneFixture
    {
        [TestCase(2)] [TestCase(3)] [TestCase(99)]
        public void SeparateOneKeepsSourceAndOneAddressableUnitBesideIt(int count)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", count); var inv = actor.GetPart<InventoryPart>();
            int weight = inv.GetCarriedWeight(); string id = source.ID; var unit = Separate(actor, source);
            Assert.AreEqual(2, inv.Objects.Count); Assert.AreEqual(count - 1, Quantity(source)); Assert.AreEqual(1, Quantity(unit));
            Assert.IsTrue(inv.Objects.Contains(source)); Assert.IsTrue(inv.Objects.Contains(unit));
            Assert.AreEqual(count, inv.Objects.Sum(Quantity)); Assert.AreEqual(weight, inv.GetCarriedWeight());
            Assert.AreEqual(id, source.ID); Fresh(unit, source); Assert.AreSame(actor, unit.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(unit.GetPart<PhysicsPart>().Equipped); Assert.IsFalse(inv.GetAllEquipped().Any());
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)]
        public void NonSplittableQuantityRefusesWithoutMutation(int count)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", count); string id = source.ID;
            var command = Command(source); var result = InventorySystem.ExecuteCommand(command, actor);
            Assert.IsFalse(result.Success); Assert.IsNotEmpty(result.ErrorMessage); Assert.IsNull(Recipient(command));
            Assert.AreSame(source, actor.GetPart<InventoryPart>().Objects.Single()); Assert.AreEqual(count, Quantity(source)); Assert.AreEqual(id, source.ID);
        }
        [TestCase("null")] [TestCase("ground")] [TestCase("foreign")] [TestCase("container")]
        [TestCase("equipped")] [TestCase("no_stacker")]
        public void OnlyOwnedCarriedStacksCanSeparate(string state)
        {
            var actor = Crafter(); var inv = actor.GetPart<InventoryPart>(); var source = Item("Dagger"); source.GetPart<StackerPart>().StackCount = 3;
            var other = Crafter(); var zone = new Zone("SeparateOwnership"); Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            if (state == "ground") Assert.IsTrue(zone.AddEntity(source, 10, 10));
            else if (state == "foreign") Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(source));
            else if (state == "container") Assert.IsTrue(Item("Sack").GetPart<ContainerPart>().AddItem(source));
            else if (state == "equipped") { source.GetPart<StackerPart>().StackCount = 1; Assert.IsTrue(inv.AddObject(source)); Assert.IsTrue(InventorySystem.Equip(actor, source)); source.GetPart<StackerPart>().StackCount = 3; }
            else if (state == "no_stacker") { source.RemovePart(source.GetPart<StackerPart>()); Assert.IsTrue(inv.AddObject(source)); }
            var before = inv.Objects.ToArray(); var owner = source.GetPart<PhysicsPart>().InInventory; var equipped = source.GetPart<PhysicsPart>().Equipped;
            var command = Command(state == "null" ? null : source); var result = InventorySystem.ExecuteCommand(command, actor, zone);
            Assert.IsFalse(result.Success); Assert.IsNull(Recipient(command)); CollectionAssert.AreEqual(before, inv.Objects);
            Assert.AreEqual(state == "no_stacker" ? 1 : 3, Quantity(source)); Assert.AreSame(owner, source.GetPart<PhysicsPart>().InInventory); Assert.AreSame(equipped, source.GetPart<PhysicsPart>().Equipped);
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingActorOrInventoryRefuses(bool missingActor)
        {
            var actor = new Entity(); var source = Item("Dagger"); source.GetPart<StackerPart>().StackCount = 2;
            var command = Command(source); Assert.IsFalse(InventorySystem.ExecuteCommand(command, missingActor ? null : actor).Success);
            Assert.IsNull(Recipient(command)); Assert.AreEqual(2, Quantity(source));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)]
        public void MassAndHandlingDoNotIncreaseEvenInAlreadyOverweightPack(int capacityMode)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); source.GetPart<HandlingPart>().CarryMovePenalty = 2;
            var inv = actor.GetPart<InventoryPart>(); actor.GetStat("Speed").Penalty = 7; inv.RefreshHandlingCarryPenalty();
            int weight = inv.GetCarriedWeight(); int penalty = actor.GetStat("Speed").Penalty;
            inv.MaxWeight = capacityMode < 0 ? -1 : weight - capacityMode;
            Separate(actor, source); Assert.AreEqual(weight, inv.GetCarriedWeight()); Assert.AreEqual(penalty, actor.GetStat("Speed").Penalty);
            inv.RefreshHandlingCarryPenalty(); Assert.AreEqual(penalty, actor.GetStat("Speed").Penalty);
        }
        [TestCase(false)] [TestCase(true)]
        public void SourceCraftingMarkStaysOnSourceAndNeverDuplicates(bool marked)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); if (marked) CraftingMarkPart.Toggle(source);
            var original = source.GetPart<CraftingMarkPart>(); var unit = Separate(actor, source);
            Assert.AreEqual(marked, CraftingMarkPart.IsMarked(source)); Assert.AreSame(original, source.GetPart<CraftingMarkPart>());
            Assert.IsFalse(CraftingMarkPart.IsMarked(unit)); Assert.AreEqual(marked ? 1 : 0, CraftingMarkPart.CollectMarked(actor).Weapons.Count);
        }
        [TestCase(false)] [TestCase(true)]
        public void PaidSharpAppliesOnlyToSeparatedSingleton(bool paid)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); var unit = Separate(actor, source);
            var locker = actor.GetPart<BitLockerPart>(); locker.LearnRecipe("mod_sharp_melee"); if (paid) locker.AddBits("BC");
            int pen = source.GetPart<MeleeWeaponPart>().PenBonus;
            Assert.IsFalse(TinkeringService.TryApplyModification(actor, "mod_sharp_melee", source, out _));
            Assert.AreEqual(paid, TinkeringService.TryApplyModification(actor, "mod_sharp_melee", unit, out _));
            Assert.IsFalse(source.HasTag("ModSharp")); Assert.AreEqual(pen, source.GetPart<MeleeWeaponPart>().PenBonus); Assert.AreEqual(2, Quantity(source));
            Assert.AreEqual(paid, unit.HasTag("ModSharp")); Assert.AreEqual(pen + (paid ? 1 : 0), unit.GetPart<MeleeWeaponPart>().PenBonus);
            Assert.AreEqual(paid ? 1 : 0, unit.GetIntProperty("ModificationCount")); Assert.AreEqual(0, locker.GetBitCount('B')); Assert.AreEqual(0, locker.GetBitCount('C'));
        }
    }
}
