using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedRequiredServiceTests
    {
        [Test] public void FreshSeedSixtyKeepsTheRequiredKitchenAddressForItsConnectedService()
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(60);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 60, true);
                var kitchen = manager.Exploration.Find(KitchenBatchPart.KitchenZoneID);
                Assert.NotNull(kitchen);
                Assert.AreEqual(SpreadExplorationFamily.WaysideKitchen, kitchen.Family,
                    "Seed 60's rare pair must not silently erase the required kitchen chain.");
                Assert.True(kitchen.PlacementEligible);
            }
        }

        [Test] public void FreshSeedSixtyActuallyGeneratesTheBoundCookAndRepairablePan()
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(60);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 60, true);
                var zone = manager.GetZone(KitchenBatchPart.KitchenZoneID);
                Assert.NotNull(zone, "The unmodified native pipeline must admit this real new-world address.");
                var pan = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "ConnectedBatchPan");
                Assert.NotNull(pan, "A frozen address alone is not a playable connected kitchen.");
                var cook = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "SpreadWaysideCook");
                Assert.NotNull(cook);
                Assert.True(pan.GetPart<KitchenBatchPart>().Configured);
                Assert.AreSame(cook, pan.GetPart<KitchenBatchPart>().Worker);
                Assert.True(RepairablePart.BlocksFunction(pan));
                Assert.AreSame(pan, cook.GetPart<CookIntroductionPart>().Pan);
                Assert.NotNull(zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "Oven")?.GetPart<CampfirePart>());
            }
        }

        [Test] public void SavedRarePairAtTheKitchenAddressRemainsLiteral()
        {
            var world = new Entity { ID = "saved-rare-selection", BlueprintName = "World" };
            world.Properties[SpreadRareEncounterPlan.PropertyKey] = "1|" + KitchenBatchPart.KitchenZoneID;
            world.Properties[SpreadRareEncounterPlan.ViperPropertyKey] = "1|";
            var restored = SpreadRareEncounterPlan.Restore(world);
            Assert.True(restored.Initialized);
            Assert.AreEqual(KitchenBatchPart.KitchenZoneID, restored.PairZoneID,
                "Protecting new worlds cannot reroll an existing world's saved rare placement.");
        }
    }
}
