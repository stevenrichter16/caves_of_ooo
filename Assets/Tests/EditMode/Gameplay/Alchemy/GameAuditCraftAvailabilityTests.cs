using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public abstract class CraftAvailabilityFixture : WeaponUnitFixture
    {
        protected Entity CarryUnit(Entity actor, string bp, int count = 1)
        { var item = Item(bp); item.GetPart<StackerPart>().StackCount = count; Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item)); Assert.Contains(item, actor.GetPart<InventoryPart>().Objects); return item; }
        protected static void Fund(Entity actor, string recipe)
        { var bits = actor.GetPart<BitLockerPart>(); bits.LearnRecipe(recipe); bits.AddBits("BBCC"); }
        protected (Entity empty, Entity positive) MixedIngredient(Entity actor, int count)
        {
            var empty = CarryUnit(actor, "PaleSalt"); var stack = empty.GetPart<StackerPart>(); int max = stack.MaxStack; stack.MaxStack = 1;
            var positive = CarryUnit(actor, "PaleSalt", 2); stack.MaxStack = max; stack.StackCount = count; return (empty, positive);
        }
    }
    public class GameAuditCraftAvailabilityTests : CraftAvailabilityFixture
    {
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void BrewPaysOneActualPositiveUnit(int count)
        {
            var actor = Crafter(); var reagent = CarryUnit(actor, "GlimmerBrine", count); var inv = actor.GetPart<InventoryPart>();
            bool ok = BrewingService.TryBrew(actor, Factory, new[] { reagent }, out var made, out var result, out var why);
            Assert.AreEqual(count > 0, ok, why);
            if (count <= 0) { Assert.IsNull(made); Assert.IsNull(result); Assert.IsNotEmpty(why); Assert.IsNull(actor.GetPart<BrewKnowledgePart>()); CollectionAssert.AreEqual(new[] { reagent }, inv.Objects); Assert.AreEqual(count, Quantity(reagent)); }
            else { Assert.NotNull(made); Assert.IsTrue(result.IsBrew); Assert.AreEqual(count > 1, inv.Objects.Contains(reagent)); Assert.AreEqual(count > 1 ? count - 1 : 1, Quantity(reagent)); }
        }
        [TestCase(-1, false)] [TestCase(-1, true)] [TestCase(0, false)] [TestCase(0, true)]
        public void InvalidExplicitMixRefusesWholeSelectionBeforeResolution(int emptyCount, bool reverse)
        {
            var actor = Crafter(); var empty = CarryUnit(actor, "GlimmerBrine", emptyCount); var valid = CarryUnit(actor, "SparkRoot", 2);
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); var mix = reverse ? new[] { valid, empty } : new[] { empty, valid };
            Assert.IsFalse(BrewingService.TryBrew(actor, Factory, mix, out var made, out var result, out var why));
            Assert.IsNull(made); Assert.IsNull(result); Assert.IsNotEmpty(why); CollectionAssert.AreEqual(before, inv.Objects);
            Assert.AreEqual(emptyCount, Quantity(empty)); Assert.AreEqual(2, Quantity(valid)); Assert.IsNull(actor.GetPart<BrewKnowledgePart>());
        }
        [TestCase(-1, 0)] [TestCase(0, 0)] [TestCase(1, 1)] [TestCase(2, 2)] [TestCase(9, 3)]
        public void ActorlessPreviewAndBatchAgreeAboutQuantity(int count, int batch)
        {
            var first = Item("GlimmerBrine"); var second = Item("SparkRoot"); first.GetPart<StackerPart>().StackCount = count; second.GetPart<StackerPart>().StackCount = 3;
            Assert.AreEqual(batch, BrewingService.GetMaxBatchCount(new[] { first, second }));
            Assert.AreEqual(count > 0, BrewingService.PreviewBrew(new[] { first, second }).IsValid);
            Assert.IsNull(first.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(count, Quantity(first));
        }
        [Test] public void UnstackedReagentIsOnePreviewableUnit()
        {
            var item = Item("GlimmerBrine"); item.RemovePart(item.GetPart<StackerPart>());
            Assert.AreEqual(1, BrewingService.GetMaxBatchCount(new[] { item })); Assert.IsTrue(BrewingService.PreviewBrew(new[] { item }).IsValid);
        }
        [Test] public void DuplicatePreviewSelectionCannotPromiseAnUnexecutableMix()
        {
            var item = Item("GlimmerBrine"); item.GetPart<StackerPart>().StackCount = 2;
            Assert.AreEqual(0, BrewingService.GetMaxBatchCount(new[] { item, item })); Assert.IsFalse(BrewingService.PreviewBrew(new[] { item, item }).IsValid);
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MineralInfusionPaysOnePositiveIngredientAndNoBits(int count)
        {
            var actor = Crafter(); var target = CarryUnit(actor, "Dagger"); var mineral = CarryUnit(actor, "PaleSalt", count); Fund(actor, "mod_palesalt_infuse");
            Assert.AreEqual(count > 0, TinkeringService.TryApplyModification(actor, "mod_palesalt_infuse", target, out _));
            Assert.AreEqual(count > 0, target.HasPart<EnhancementPaleSalt>()); Assert.AreEqual(count > 1 || count <= 0, actor.GetPart<InventoryPart>().Objects.Contains(mineral));
            Assert.AreEqual(count > 1 ? count - 1 : count, Quantity(mineral)); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('C'));
        }
        [TestCase(-1)] [TestCase(0)] public void AutomaticInfusionSkipsEmptyFirstMatch(int count)
        {
            var actor = Crafter(); var target = CarryUnit(actor, "Dagger"); var pair = MixedIngredient(actor, count);
            Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_palesalt_infuse", target, out var why), why);
            Assert.AreEqual(count, Quantity(pair.empty)); Assert.Contains(pair.empty, actor.GetPart<InventoryPart>().Objects); Assert.AreEqual(1, Quantity(pair.positive)); Assert.IsTrue(target.HasPart<EnhancementPaleSalt>());
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void SharpQueryAndExecutionRejectNonpositiveTargets(int count)
        {
            var actor = Crafter(); var target = CarryUnit(actor, "Dagger", count); Fund(actor, "mod_sharp_melee"); int pen = target.GetPart<MeleeWeaponPart>().PenBonus;
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("mod_sharp_melee", out var recipe));
            Assert.AreEqual(count == 1, TinkeringService.CanApplyModificationTarget(recipe, target, out _));
            Assert.AreEqual(count == 1, TinkeringService.TryApplyModification(actor, recipe.ID, target, out _));
            Assert.AreEqual(pen + (count == 1 ? 1 : 0), target.GetPart<MeleeWeaponPart>().PenBonus); Assert.AreEqual(count == 1 ? 1 : 2, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(count, Quantity(target));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void DisassemblyOnlyYieldsBitsForPositiveCarriedUnit(int count)
        {
            var actor = Crafter(); var target = CarryUnit(actor, "Dagger", count); var bits = actor.GetPart<BitLockerPart>(); bits.RestoreBitsAndRecipes(new System.Collections.Generic.Dictionary<char, int>(), Array.Empty<string>());
            Assert.AreEqual(count > 0, TinkeringService.CanDisassemble(target, out _));
            Assert.AreEqual(count > 0, TinkeringService.TryDisassemble(actor, target, out var yielded, out _));
            if (count <= 0) { Assert.IsEmpty(yielded); Assert.AreEqual(0, bits.GetBitCount('B')); Assert.Contains(target, actor.GetPart<InventoryPart>().Objects); Assert.AreEqual(count, Quantity(target)); }
            else { Assert.IsNotEmpty(yielded); Assert.Greater(bits.GetBitCount('B'), 0); Assert.AreEqual(count > 1, actor.GetPart<InventoryPart>().Objects.Contains(target)); }
        }
        [TestCase(-1, false)] [TestCase(-1, true)] [TestCase(0, false)] [TestCase(0, true)] [TestCase(1, false)] [TestCase(1, true)]
        public void MarkingRequiresAUnitButUnmarkingAlwaysAllowsOwnedCleanup(int count, bool marked)
        {
            var actor = Crafter(); var item = CarryUnit(actor, "GlimmerBrine", count); if (marked) CraftingMarkPart.Toggle(item);
            var result = InventorySystem.ExecuteCommand(new ToggleCraftMarkCommand(item), actor, null);
            Assert.AreEqual(marked || count > 0, result.Success); Assert.AreEqual(!marked && count > 0, CraftingMarkPart.IsMarked(item)); Assert.AreEqual(count, Quantity(item));
        }
    }
}
