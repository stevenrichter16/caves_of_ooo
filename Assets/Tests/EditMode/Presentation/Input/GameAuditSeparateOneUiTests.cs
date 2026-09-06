using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditSeparateOneUiTests : ActionFeedbackFixture
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void MenuOffersSeparateOnlyForMultipleCarriedUnits(int count)
        {
            var source = Carry("Dagger", count); Assert.IsTrue(UI.ReopenItemActionPopupFor(source));
            var actions = (IList)Field(Get("_itemActionPopup"), "Actions");
            Assert.AreEqual(count > 1 ? 1 : 0, actions.Cast<object>().Count(a => (string)Field(a, "Command") == "separate_one"));
        }
        [TestCase(2)] [TestCase(3)]
        public void MenuSeparatesAndFocusesExactNewSingleton(int count)
        {
            var source = Carry("Dagger", count); OpenAction(source, "separate_one", out int action);
            Call("ExecuteItemAction", action); var unit = Inventory.Objects.Single(e => e.BlueprintName == "Dagger" && e != source);
            var popup = Get("_itemActionPopup"); Assert.NotNull(popup); Assert.AreSame(unit, Field(popup, "Item"));
            var row = ((IList)Get("_rows"))[(int)Get("_cursorIndex")]; Assert.AreSame(unit, Field(Field(row, "Item"), "Item"));
            Assert.AreEqual(1, unit.GetPart<StackerPart>().StackCount); Assert.AreEqual(count - 1, source.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false)] [TestCase(true)]
        public void StaleSplitMenuRetainsExactRefusalContext(bool removed)
        {
            var source = Carry("Dagger", 3); var popup = OpenAction(source, "separate_one", out int action);
            if (removed) Assert.IsTrue(Inventory.RemoveObject(source)); else source.GetPart<StackerPart>().StackCount = 1;
            var before = Inventory.Objects.ToArray(); Call("ExecuteItemAction", action);
            Assert.AreSame(popup, Get("_itemActionPopup")); Assert.AreSame(source, Field(popup, "Item")); Assert.AreEqual(action, Field(popup, "CursorIndex"));
            Assert.IsNotEmpty(Status); CollectionAssert.AreEqual(before, Inventory.Objects);
        }
    }
}
