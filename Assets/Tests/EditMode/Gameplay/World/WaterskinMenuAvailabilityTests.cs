using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WaterskinMenuAvailabilityTests : FiftyWorldFixture
    {
        Entity Vessel(string blueprint, int charges)
        { var item = Carry(blueprint); item.GetPart<WaterskinPart>().Charges = charges; return item; }
        InventoryAction Row(Entity item, string name) => Actions(item).Single(a => a.Name == name);

        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void NoReachableWaterHasAnExplicitUnavailableFill(string blueprint)
        {
            var item = Vessel(blueprint, 0); var row = Row(item, "FillWater");
            Assert.True(row.IsDisabled); Assert.AreEqual("no-water", row.DisabledCode);
            StringAssert.Contains("no fresh water beside", row.DisabledReason);
            Assert.AreEqual('f', row.Key); Assert.AreEqual(19, row.Priority); Assert.AreEqual(0, Units(item));
        }
        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void AnEmptyVesselDoesNotOfferAnExecutableDrink(string blueprint)
        {
            var item = Vessel(blueprint, 0); var row = Row(item, "DrinkWater");
            Assert.True(row.IsDisabled); Assert.AreEqual("empty", row.DisabledCode);
            Assert.AreEqual('d', row.Key); Assert.AreEqual(20, row.Priority);
        }
        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void AFullVesselExplainsWhyFillingIsUnavailable(string blueprint)
        {
            var item = Vessel(blueprint, 3); var well = Place("Cistern"); var row = Row(item, "FillWater");
            Assert.True(row.IsDisabled); Assert.AreEqual("full", row.DisabledCode);
            Assert.False(Row(item, "DrinkWater").IsDisabled); Assert.AreEqual(3, well.GetPart<WellPart>().Stock);
        }
        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void ReachableWaterKeepsTheNormalCommandAndFinitePayment(string blueprint)
        {
            var item = Vessel(blueprint, 0); var well = Place("Cistern"); var row = Row(item, "FillWater");
            Assert.False(row.IsDisabled); Assert.AreEqual("FillWaterskin", row.Command);
            Assert.AreEqual('f', row.Key); Assert.AreEqual(19, row.Priority);
            Assert.True(Act(item, row.Command)); Assert.AreEqual(3, Units(item)); Assert.AreEqual(0, well.GetPart<WellPart>().Stock);
        }
        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void APreviouslyAvailableFillStillRechecksTheSource(string blueprint)
        {
            var item = Vessel(blueprint, 0); var well = Place("Cistern"); var row = Row(item, "FillWater");
            Assert.False(row.IsDisabled); Assert.True(Zone.MoveEntity(well, 20, 20));
            Assert.False(Act(item, row.Command)); Assert.AreEqual(0, Units(item)); Assert.AreEqual(3, well.GetPart<WellPart>().Stock);
        }
        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void AnotherDrawCanExhaustThePreviouslyOfferedWater(string blueprint)
        {
            var item = Vessel(blueprint, 0); var well = Place("Cistern"); var row = Row(item, "FillWater");
            Assert.False(row.IsDisabled); well.GetPart<WellPart>().Stock = 0;
            Assert.False(Act(item, row.Command)); Assert.AreEqual(0, Units(item)); Assert.AreEqual(0, well.GetPart<WellPart>().Stock);
            Assert.AreEqual("no-water", Row(item, "FillWater").DisabledCode);
        }
        [TestCase("Waterskin")] [TestCase("DrawgourdShell")]
        public void MenuAndReaderSharePureAvailabilityReasons(string blueprint)
        {
            var item = Vessel(blueprint, 0); var part = item.GetPart<WaterskinPart>();
            var reader = new InventoryActionList(); part.AddUseRows(Actor, Zone, reader);
            for (int i = 0; i < 3; i++) foreach (string name in new[] { "FillWater", "DrinkWater" })
            {
                var expected = reader.Actions.Single(a => a.Name == name); var actual = Row(item, name);
                Assert.AreEqual(expected.Command, actual.Command); Assert.AreEqual(expected.DisabledReason, actual.DisabledReason);
            }
            Assert.AreEqual(0, Units(item)); Assert.AreEqual(0, Clock.TickCount); Assert.Contains(item, Pack.Objects);
        }
    }
}
