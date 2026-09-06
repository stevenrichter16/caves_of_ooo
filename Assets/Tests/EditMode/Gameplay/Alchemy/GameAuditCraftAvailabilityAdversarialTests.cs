using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditCraftAvailabilityAdversarialTests : CraftAvailabilityFixture
    {
        // A fresh game must not depend on another enhancement path warming its registry.
        [TestCase("mod_palesalt_infuse", "PaleSalt", false)] [TestCase("mod_palesalt_infuse", "PaleSalt", true)]
        [TestCase("mod_choiriron_infuse", "ChoirIron", false)] [TestCase("mod_choiriron_infuse", "ChoirIron", true)]
        [TestCase("mod_glowquartz_infuse", "GlowQuartz", false)] [TestCase("mod_glowquartz_infuse", "GlowQuartz", true)]
        public void FirstMineralRecipeInitializesItsOwnEnhancementRegistry(string id, string mineral, bool execute)
        {
            var actor = Crafter(); var item = CarryUnit(actor, "Dagger"); var ingredient = CarryUnit(actor, mineral); Fund(actor, id);
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(id, out var recipe)); EnhancementFactory.ForceReinitialize();
            if (execute) { Assert.IsTrue(TinkeringService.TryApplyModification(actor, id, item, out var why), why); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(item)); Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(ingredient)); }
            else { Assert.IsTrue(TinkeringService.CanApplyModificationTarget(recipe, item, out var why), why); Assert.AreEqual(0, ItemEnhancing.CountEnhancements(item)); Assert.Contains(ingredient, actor.GetPart<InventoryPart>().Objects); }
            Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('C'));
        }
        [Test] public void ExplicitlySuppressedEnhancementRegistryStillRefusesWithoutPayment()
        {
            var actor = Crafter(); var item = CarryUnit(actor, "Dagger"); var ingredient = CarryUnit(actor, "PaleSalt"); EnhancementFactory.ResetForTests();
            Assert.IsFalse(TinkeringService.TryApplyModification(actor, "mod_palesalt_infuse", item, out var why)); StringAssert.Contains("not registered", why);
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(item)); Assert.Contains(ingredient, actor.GetPart<InventoryPart>().Objects);
        }
        // Preview validity means a brew, not every executable experimentation outcome.
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] public void ActualMishapOnlyConsumesAndHurtsWithAPositiveUnit(int count)
        {
            var actor = Crafter(); var reagent = CarryUnit(actor, "BlastcapSpore", count); var zone = ForgeZone(actor, Item("AlchemyStill")); int hp = actor.GetStat("Hitpoints").Value;
            Assert.IsFalse(BrewingService.PreviewBrew(new[] { reagent }).IsValid);
            var result = InventorySystem.ExecuteCommand(new BrewReagentsCommand(new[] { reagent }, Factory), actor, zone);
            Assert.AreEqual(count > 0, result.Success); Assert.AreEqual(count > 0 ? Math.Max(1, hp - BrewReagentsCommand.MishapDamageMax) : hp, actor.GetStat("Hitpoints").Value);
            Assert.AreEqual(count <= 0, actor.GetPart<InventoryPart>().Objects.Contains(reagent)); Assert.AreEqual(count, Quantity(reagent)); Assert.IsNull(actor.GetPart<BrewKnowledgePart>());
        }
        [TestCase("Dagger", "mod_sharp_melee", 0)] [TestCase("Dagger", "mod_sharp_melee", 1)]
        [TestCase("LeatherArmor", "mod_reinforced_plating_armor", 0)] [TestCase("LeatherArmor", "mod_reinforced_plating_armor", 1)]
        public void EquippedSingletonModificationsRemainAvailable(string bp, string recipe, int count)
        {
            var actor = Crafter(); var item = CarryUnit(actor, bp); Assert.IsTrue(InventorySystem.Equip(actor, item)); item.GetPart<StackerPart>().StackCount = count;
            var inv = actor.GetPart<InventoryPart>(); var equipped = inv.GetAllEquipped().ToArray(); var owner = item.GetPart<PhysicsPart>().Equipped; Fund(actor, recipe);
            Assert.AreEqual(count > 0, TinkeringService.TryApplyModification(actor, recipe, item, out var why), why);
            Assert.AreEqual(count, item.GetIntProperty("ModificationCount")); CollectionAssert.AreEquivalent(equipped, inv.GetAllEquipped()); Assert.AreSame(owner, item.GetPart<PhysicsPart>().Equipped);
            Assert.AreEqual(count > 0 ? 1 : 2, actor.GetPart<BitLockerPart>().GetBitCount('B'));
        }
        [TestCase("brew")] [TestCase("disassemble")] [TestCase("modify")]
        public void MissingStackerStillPaysOneActualItem(string action)
        {
            var actor = Crafter(); var item = CarryUnit(actor, action == "brew" ? "GlimmerBrine" : "Dagger"); item.RemovePart(item.GetPart<StackerPart>());
            if (action == "brew") Assert.IsTrue(BrewingService.TryBrew(actor, Factory, new[] { item }, out _, out _, out var why), why);
            else if (action == "disassemble") Assert.IsTrue(TinkeringService.TryDisassemble(actor, item, out _, out var why), why);
            else { Fund(actor, "mod_sharp_melee"); Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_sharp_melee", item, out var why), why); }
            Assert.AreEqual(action == "modify", actor.GetPart<InventoryPart>().Objects.Contains(item)); Assert.IsNull(item.GetPart<StackerPart>());
        }
        [TestCase("brew")] [TestCase("disassemble")] [TestCase("mark")]
        public void AnotherActorsPositiveItemCannotBecomePayment(string action)
        {
            var actor = Crafter(); var other = Crafter(); var item = CarryUnit(other, action == "disassemble" ? "Dagger" : "GlimmerBrine", 2);
            bool ok = action == "brew" ? BrewingService.TryBrew(actor, Factory, new[] { item }, out _, out _, out _)
                : action == "disassemble" ? TinkeringService.TryDisassemble(actor, item, out _, out _)
                : InventorySystem.ExecuteCommand(new ToggleCraftMarkCommand(item), actor).Success;
            Assert.IsFalse(ok); Assert.AreEqual(2, Quantity(item)); Assert.AreSame(other, item.GetPart<PhysicsPart>().InInventory); Assert.Contains(item, other.GetPart<InventoryPart>().Objects); Assert.IsEmpty(actor.GetPart<InventoryPart>().Objects);
        }
        [TestCase(-1, false)] [TestCase(-1, true)] [TestCase(0, false)] [TestCase(0, true)]
        public void InvalidBatchSelectionNeverPartiallyPaysOrLosesMarks(int count, bool reverse)
        {
            var actor = Crafter(); var good = CarryUnit(actor, "GlimmerBrine", 3); var empty = CarryUnit(actor, "SparkRoot", count);
            CraftingMarkPart.Toggle(good); CraftingMarkPart.Toggle(empty); var marks = new[] { good.GetPart<CraftingMarkPart>(), empty.GetPart<CraftingMarkPart>() };
            var mix = reverse ? new[] { empty, good } : new[] { good, empty };
            Assert.IsFalse(BrewingService.TryBrewBatch(actor, Factory, mix, 3, out var outputs, out var results, out int made, out _));
            Assert.AreEqual(0, made); Assert.IsEmpty(outputs); Assert.IsEmpty(results); Assert.AreEqual(3, Quantity(good)); Assert.AreEqual(count, Quantity(empty));
            Assert.AreSame(marks[0], good.GetPart<CraftingMarkPart>()); Assert.AreSame(marks[1], empty.GetPart<CraftingMarkPart>()); Assert.IsNull(actor.GetPart<BrewKnowledgePart>());
        }
        [TestCase("SteelBladeComponent", 0)] [TestCase("SteelBladeComponent", 1)] [TestCase("Dagger", 0)] [TestCase("Dagger", 1)]
        public void CachedAddMarkRevalidatesBeforeEvictingExclusiveSibling(string bp, int finalCount)
        {
            var actor = Crafter(); var first = CarryUnit(actor, bp); CraftingMarkPart.Toggle(first); var marker = first.GetPart<CraftingMarkPart>();
            first.GetPart<StackerPart>().MaxStack = 1; var next = CarryUnit(actor, bp); var command = new ToggleCraftMarkCommand(next); var context = new InventoryContext(actor);
            Assert.IsTrue(command.Validate(context).IsValid); next.GetPart<StackerPart>().StackCount = finalCount; var tx = new InventoryTransaction();
            var result = command.Execute(context, tx); tx.Commit(); Assert.AreEqual(finalCount > 0, result.Success);
            Assert.AreEqual(finalCount > 0, CraftingMarkPart.IsMarked(next)); Assert.AreEqual(finalCount <= 0, CraftingMarkPart.IsMarked(first));
            if (finalCount <= 0) Assert.AreSame(marker, first.GetPart<CraftingMarkPart>());
        }
        [Test] public void RepeatedActorlessQueriesArePureAndDoNotInventOwnership()
        {
            var reagent = Item("GlimmerBrine"); var weapon = Item("Dagger"); CraftingMarkPart.Toggle(reagent); var mark = reagent.GetPart<CraftingMarkPart>();
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("mod_sharp_melee", out var recipe)); MessageLog.Clear(); Diag.ResetAll();
            for (int i = 0; i < 4; i++) { Assert.IsTrue(BrewingService.PreviewBrew(new[] { reagent }).IsValid); Assert.AreEqual(1, BrewingService.GetMaxBatchCount(new[] { reagent })); Assert.IsTrue(TinkeringService.CanApplyModificationTarget(recipe, weapon, out _)); Assert.IsTrue(TinkeringService.CanDisassemble(weapon, out _)); }
            Assert.IsNull(reagent.GetPart<PhysicsPart>().InInventory); Assert.IsNull(weapon.GetPart<PhysicsPart>().InInventory); Assert.AreSame(mark, reagent.GetPart<CraftingMarkPart>());
            Assert.AreEqual(1, Quantity(reagent)); Assert.AreEqual(1, Quantity(weapon)); Assert.IsEmpty(MessageLog.GetMessages()); Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter()).Count);
        }
    }
    public class GameAuditCraftAvailabilityUiAdversarialTests : ActionFeedbackFixture
    {
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] public void SameNameCleanupRowsIdentifyOnlyTheEmptyPick(int count)
        {
            var first = Carry("GlimmerBrine", 1); first.GetPart<StackerPart>().MaxStack = 1; var second = Carry("GlimmerBrine", 1);
            first.GetPart<StackerPart>().StackCount = count; CraftingMarkPart.Toggle(first); CraftingMarkPart.Toggle(second); BrewMode();
            var rows = (IList)Get("_craftRows"); string firstText = (string)Field(rows[Row(first)], "Text"), secondText = (string)Field(rows[Row(second)], "Text");
            Assert.AreEqual(count <= 0, firstText.Contains("(empty; remove pick)")); Assert.IsFalse(secondText.Contains("(empty; remove pick)"));
            var actions = WorldInteractionSystem.GatherActions(Item("AlchemyStill"), Player);
            var a = actions.Single(x => x.Command == CraftingMarkPart.ToggleCommandPrefix + first.ID);
            var b = actions.Single(x => x.Command == CraftingMarkPart.ToggleCommandPrefix + second.ID);
            Assert.AreEqual(count <= 0, a.Display.Contains("(empty; remove pick)")); Assert.IsFalse(b.Display.Contains("(empty; remove pick)"));
        }
        [TestCase(false)] [TestCase(true)] public void EmptyPickedPopupOrStationAllowsCleanup(bool station)
        {
            var item = Carry("GlimmerBrine", 0); var other = Carry("SparkRoot", 2); CraftingMarkPart.Toggle(item); CraftingMarkPart.Toggle(other);
            if (station)
            { var row = WorldInteractionSystem.GatherActions(Item("AlchemyStill"), Player).Single(a => a.Command == CraftingMarkPart.ToggleCommandPrefix + item.ID); Assert.IsTrue(InventorySystem.ExecuteCommand(new ToggleCraftMarkCommand(item), Player, Zone).Success); }
            else { OpenAction(item, "toggle_craftmark", out int index); Call("ExecuteItemAction", index); }
            Assert.IsFalse(CraftingMarkPart.IsMarked(item)); Assert.IsTrue(CraftingMarkPart.IsMarked(other)); Assert.AreEqual(0, item.GetPart<StackerPart>().StackCount);
            Assert.IsFalse(WorldInteractionSystem.GatherActions(Item("AlchemyStill"), Player).Any(a => a.Command == CraftingMarkPart.ToggleCommandPrefix + item.ID));
        }
        [Test] public void ClearPicksRemovesStaleMarksAcrossCraftModes()
        {
            Steel.GetPart<StackerPart>().StackCount = 0; var reagent = Carry("GlimmerBrine", 0); CraftingMarkPart.Toggle(Steel); CraftingMarkPart.Toggle(reagent); CraftingMarkPart.Toggle(Oak);
            BrewMode(); Call("ClearCraftPicks"); Assert.IsFalse(CraftingMarkPart.IsMarked(Steel)); Assert.IsFalse(CraftingMarkPart.IsMarked(reagent)); Assert.IsFalse(CraftingMarkPart.IsMarked(Oak)); Assert.IsEmpty((IEnumerable)Get("_pickedReagents"));
        }
        [TestCase(false)] [TestCase(true)] public void CachedPositiveRowRefusesEmptyAndRecoversAfterRefill(bool popup)
        {
            var item = Carry("GlimmerBrine", 1); BrewMode(); int row = Row(item), action = -1;
            if (popup) OpenAction(item, "toggle_craftmark", out action); item.GetPart<StackerPart>().StackCount = 0;
            if (popup) Call("ExecuteItemAction", action); else { Set("_craftCursorIndex", row); Call("ToggleCraftPickUnderCursor"); }
            Assert.IsFalse(CraftingMarkPart.IsMarked(item)); item.GetPart<StackerPart>().StackCount = 1; Set("_itemActionPopup", null); Call("Rebuild"); Pick(item); Assert.IsTrue(CraftingMarkPart.IsMarked(item));
        }
        [TestCase(-1)] [TestCase(0)] public void TinkerAvailabilityFindsPositiveLaterSibling(int count)
        {
            var empty = Carry("PaleSalt", 1); empty.GetPart<StackerPart>().MaxStack = 1; var positive = Carry("PaleSalt", 2); empty.GetPart<StackerPart>().StackCount = count; Carry("Dagger", 1);
            SelectRecipe("mod_palesalt_infuse", true); var selected = ((IList)Get("_tinkerRows"))[(int)Get("_tinkerCursorIndex")]; Assert.IsTrue((bool)Field(selected, "HasIngredient"));
            Assert.AreEqual(count, empty.GetPart<StackerPart>().StackCount); Assert.AreEqual(2, positive.GetPart<StackerPart>().StackCount);
        }
    }
}
