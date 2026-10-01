using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class CropDiscoveryGuidanceTests
    {
        [TestCase("Morrowfast_Sella")][TestCase("Morrowfast_Orrit")]
        public void TownDirectionsAreReachableWithoutAQuestOrPayment(string id)
        {
            var c=JsonUtility.FromJson<ConversationFileData>(Resources.Load<TextAsset>("Content/Conversations/Morrowfast").text).Conversations.Single(x=>x.ID==id);
            var choice=c.GetNode("Start").Choices.SingleOrDefault(x=>x.Target=="GrowingBeds");
            Assert.NotNull(choice,"Ordinary conversation needs a visible crop lead.");Assert.IsEmpty(choice.Predicates);Assert.IsEmpty(choice.Actions);
            var node=c.GetNode("GrowingBeds");Assert.NotNull(node);StringAssert.Contains("west",node.Text.ToLowerInvariant());
            Assert.True(node.Choices.Any(x=>x.Target=="Start"));Assert.False(node.Text.Contains("Overworld."),"Keep coordinates out of dialogue.");
        }
        [Test] public void ProvisionerNamesExistingSeedsAndExplainsHarvestedSeedPickup()
        {
            var c=JsonUtility.FromJson<ConversationFileData>(Resources.Load<TextAsset>("Content/Conversations/Morrowfast").text).Conversations.Single(x=>x.ID=="Morrowfast_Sella");
            foreach(string plant in new[]{"knotflax","hearthbulb","seamleaf"})StringAssert.Contains(plant,c.GetNode("Stock").Text);
            var text=c.GetNode("GrowingBeds")?.Text;Assert.NotNull(text);StringAssert.Contains("pick up",text);StringAssert.Contains("rain",text);
        }
        [Test] public void DirectionsPointAtTheRealAdjacentExistingCropSite()
        {
            var p=WorldMap.FromZoneID(RepairCultivationSite.ZoneID);Assert.AreEqual(2,p.x);Assert.AreEqual(6,p.y);Assert.AreEqual(0,p.z);
            using(var scope=new HotbarSaveFixture(false,false))
            {
                var factory=new EntityFactory();factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                var manager=new OverworldZoneManager(factory,64);var zone=manager.GetZone(RepairCultivationSite.ZoneID);
                foreach(string name in new[]{"KnotflaxCrop","HearthbulbCrop","SeamleafCrop"})
                    Assert.True(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName==name&&e.GetPart<CropPart>().GrowthStage==2),name);
            }
        }
    }
}
