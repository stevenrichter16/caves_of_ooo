using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Public blueprint parameter contract, independent of the new pallet.</summary>
    public sealed class ConnectedHarvestActionTextTests
    {
        [TestCase(false)] [TestCase(true)]
        public void ConfiguredDisplayUsesTheExistingHarvestCommandAndSurvivesSave(bool custom)
        {
            using (var scope = new DensityLootTestScope())
            {
                string expected = custom ? "dismantle for timber (2)" : "harvest";
                if (custom) scope.Factory.Blueprints["RepairTimberPile"].Parts["Harvestable"]["ActionText"] = expected;
                var owner = scope.Factory.CreateEntity("RepairTimberPile");
                var actor = scope.Factory.CreateEntity("Player");
                foreach (var source in new[] { owner, PartRoundTripHelper.RoundTripEntity(owner) })
                {
                    var action = WorldInteractionSystem.GatherActions(source, actor).Single(a => a.Command == "Harvest");
                    Assert.AreEqual(expected, action.Display);
                    Assert.AreEqual("Harvest", action.Name); Assert.AreEqual('h', action.Key);
                    source.GetPart<HarvestablePart>().Harvested = true;
                    Assert.False(WorldInteractionSystem.GatherActions(source, actor).Any(a => a.Command == "Harvest"));
                }
            }
        }
    }
}
