using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class RepairCultivationContentTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup(){ scope=new HaulingContentScope(); scope.Seed(64); }
        [TearDown] public void Cleanup(){ scope?.Dispose(); }
        Entity Make(string name){ Assert.True(scope.Factory.Blueprints.ContainsKey(name), "World content must exist: "+name); return scope.Factory.CreateEntity(name); }
        [TestCase("RepairLinedWell","Well")][TestCase("RepairRopeWell","Well")][TestCase("RepairWoodenGate","Door")]
        public void DamagedObjectsHaveIndependentCompositionFaultAndActualFunction(string name,string function)
        {
            var e=Make(name); Assert.True(e.Parts.Any(p=>p.Name=="Composition")); Assert.True(e.Parts.Any(p=>p.Name=="Repairable"));
            Assert.True(e.Parts.Any(p=>p.Name==function)); Assert.False(e.GetPart<PhysicsPart>().Takeable);
            Assert.That(e.GetPart<ExaminablePart>().Text,Is.Not.Empty);
        }
        [TestCase("Knotflax","KnotflaxCord")][TestCase("Hearthbulb","Hearthbulb")][TestCase("Seamleaf","SeamleafSprig")]
        public void EachCultivatedSpeciesHasObtainableSeedGrowingOwnerAndUsefulYield(string species,string yield)
        {
            var seed=Make(species+"Seed"); var crop=Make(species+"Crop"); var output=Make(yield);
            Assert.AreEqual(crop.BlueprintName,seed.GetPart<SeedPart>().CropBlueprint);
            Assert.AreEqual(yield,crop.GetPart<CropPart>().YieldBlueprint); Assert.AreEqual(2,crop.GetPart<CropPart>().YieldCount);
            Assert.True(crop.HasTag("Crop")); Assert.False(crop.GetPart<PhysicsPart>().Takeable); Assert.True(output.GetPart<PhysicsPart>().Takeable);
            var planting=seed.GetPart<SeedPart>(); var require=planting.GetType().GetField("RequireCultivatedSoil"); Assert.NotNull(require); Assert.AreEqual(true,require.GetValue(planting));
            var growing=crop.GetPart<CropPart>(); var harvest=growing.GetType().GetField("HarvestAtMaturity"); Assert.NotNull(harvest); Assert.AreEqual(true,harvest.GetValue(growing));
            Assert.That(output.GetPart<ExaminablePart>().Text,Is.Not.Empty);
        }
        [TestCase("RepairClayBank","FireClay",4)][TestCase("RepairTimberPile","SalvagedTimber",4)][TestCase("RepairCordBundle","KnotflaxCord",2)]
        public void WorldSourcesYieldFiniteRealRepairUnits(string source,string output,int count)
        {
            var e=Make(source); var h=e.GetPart<HarvestablePart>(); Assert.NotNull(h); Assert.AreEqual(output,h.YieldBlueprint);
            Assert.AreEqual(count,h.YieldMin); Assert.AreEqual(count,h.YieldMax); Assert.AreEqual(100,h.YieldChance); Assert.False(h.Harvested);
            Assert.True(Make(output).GetPart<PhysicsPart>().Takeable);
        }
        [Test] public void CropOutputsHaveRealFoodCookingAndBrewingUses()
        {
            Assert.NotNull(Make("Hearthbulb").GetPart<FoodPart>());
            Assert.AreEqual("RoastedHearthbulb",Make("Hearthbulb").GetPart<CookablePart>().Into);
            Assert.NotNull(Make("RoastedHearthbulb").GetPart<FoodPart>());
            StringAssert.Contains("vital",Make("SeamleafSprig").GetPart<ReagentPart>().PropertiesRaw);
        }
        [TestCase(64)][TestCase(1729)]
        public void WesternStartingFieldContainsActualRepairsSourcesAndPlantedBeds(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true); var z=manager.GetZone("Overworld.2.6.0");
            foreach(string bp in new[]{"RepairLinedWell","RepairRopeWell","RepairWoodenGate","RepairClayBank","RepairTimberPile","RepairCordBundle"})
                Assert.AreEqual(1,z.GetReadOnlyEntities().Count(e=>e.BlueprintName==bp),bp);
            foreach(string species in new[]{"Knotflax","Hearthbulb","Seamleaf"})
            {
                var crops=z.GetReadOnlyEntities().Where(e=>e.BlueprintName==species+"Crop").ToArray(); Assert.AreEqual(2,crops.Length,species);
                foreach(var crop in crops)Assert.True(z.GetEntityCell(crop).Objects.Any(e=>e.HasTag("Terrain")&&e.HasTag("Plantable")&&e.Parts.Any(p=>p.Name=="CultivatedSoil")));
                Assert.True(crops.Any(e=>e.GetPart<CropPart>().GrowthStage==2)); Assert.True(crops.Any(e=>e.GetPart<CropPart>().GrowthStage==0));
            }
            Assert.AreEqual(1,z.GetReadOnlyEntities().Count(e=>e.GetIntProperty("MorrowfastDryGoodsCache")==1),"Existing expedition must remain intact.");
            manager.UnloadZone(z.ZoneID); Assert.AreSame(z,manager.GetZone(z.ZoneID),"Repairs and finite sources cannot regenerate.");
        }
    }
}
