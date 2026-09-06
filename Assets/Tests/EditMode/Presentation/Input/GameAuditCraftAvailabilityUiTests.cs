using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditCraftAvailabilityUiTests : ActionFeedbackFixture
    {
        private bool HasCraftRow(Entity item) => ((IList)Get("_craftRows")).Cast<object>().Any(row => ReferenceEquals(Field(row, "Item"), item));
        [TestCase(-1, false)] [TestCase(-1, true)] [TestCase(0, false)] [TestCase(0, true)] [TestCase(1, false)] [TestCase(1, true)]
        public void EmptySelectionsCanBeRemovedButNotNewlyPicked(int count, bool marked)
        {
            var item = Carry("GlimmerBrine", count); if (marked) CraftingMarkPart.Toggle(item); BrewMode();
            Assert.AreEqual(marked || count > 0, HasCraftRow(item));
            var actions = WorldInteractionSystem.GatherActions(Item("AlchemyStill"), Player);
            Assert.AreEqual(marked || count > 0, actions.Any(a => a.Command == CraftingMarkPart.ToggleCommandPrefix + item.ID));
            Assert.IsTrue(UI.ReopenItemActionPopupFor(item)); var popup = Get("_itemActionPopup");
            Assert.AreEqual(marked || count > 0, ((IList)Field(popup, "Actions")).Cast<object>().Any(a => (string)Field(a, "Command") == "toggle_craftmark"));
        }
        [TestCase(-1)] [TestCase(0)] public void RemovingOnlyStalePickRecoversTheExactRemainingMix(int count)
        {
            var stale = Carry("SparkRoot", count); var good = Carry("GlimmerBrine", 2); CraftingMarkPart.Toggle(stale); CraftingMarkPart.Toggle(good); BrewMode();
            CollectionAssert.AreEquivalent(new[] { stale, good }, (IEnumerable)Get("_pickedReagents")); Assert.IsFalse(((BrewPreview)Get("_brewPreview")).IsValid); Assert.AreEqual(0, Get("_craftBatchMax"));
            var before = Inventory.Objects.ToArray(); Call("ExecuteCraft", false); CollectionAssert.AreEqual(before, Inventory.Objects); Assert.AreEqual(2, good.GetPart<StackerPart>().StackCount); Assert.IsNotEmpty(Status);
            Pick(stale); Assert.IsFalse(HasCraftRow(stale)); Assert.IsFalse(CraftingMarkPart.IsMarked(stale)); Assert.IsTrue(CraftingMarkPart.IsMarked(good));
            CollectionAssert.AreEqual(new[] { good }, (IEnumerable)Get("_pickedReagents")); Assert.IsTrue(((BrewPreview)Get("_brewPreview")).IsValid);
            Call("ExecuteCraft", false); Assert.AreEqual(1, good.GetPart<StackerPart>().StackCount); Assert.AreEqual(count, stale.GetPart<StackerPart>().StackCount); Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] public void TinkerIngredientIndicatorAgreesWithPositivePayment(int count)
        {
            Carry("Dagger", 1); Carry("PaleSalt", count); SelectRecipe("mod_palesalt_infuse", true);
            var selected = ((IList)Get("_tinkerRows"))[(int)Get("_tinkerCursorIndex")]; Assert.AreEqual(count > 0, Field(selected, "HasIngredient"));
        }
    }
}
