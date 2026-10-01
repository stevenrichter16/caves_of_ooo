using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BiomeCropContentTests : CultivatedCropTestBase
    {
        public static readonly string[] Species = { "Claspbean", "Pitchpod", "Drawgourd", "Wickrush", "Marlroot", "Sumpsieve", "Drowsebell", "Chillcress", "Slipsedge", "Peatlantern", "Sunbladder", "Shalebean", "Shadefan", "Cinderpea", "Spurgrass", "Choirwick", "Knitmoss", "Sourmantle", "Murmurpod", "Sealbark", "Absentmint", "Margincress", "Binderroot", "Greybladder", "Hollowchime", "Raingourd", "Prismreed", "ScarletSundew", "Cloudwick", "Gripfrond", "Lampvein", "Knucklecap", "Sootroot", "Veilpuff", "Brinebutton" };
        [Test] public void CatalogueHasExactlyFiveSpeciesInSevenLiveEcologies()
        {
            var type=typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.BiomeCropCatalog");
            Assert.NotNull(type,"A single validated catalogue must own the 35 original species.");
            var all=(System.Collections.IEnumerable)type.GetProperty("All").GetValue(null);
            var rows=all.Cast<object>().ToArray(); Assert.AreEqual(35,rows.Length);
            var groups=rows.GroupBy(r=>r.GetType().GetProperty("Biome").GetValue(r));
            CollectionAssert.AreEquivalent(new[]{BiomeType.Cave,BiomeType.Spread,BiomeType.Sodden,BiomeType.Beating,BiomeType.Grovelands,BiomeType.Overwrit,BiomeType.Stump},groups.Select(g=>g.Key));
            foreach(var group in groups)Assert.AreEqual(5,group.Count());
        }
        [TestCaseSource(nameof(Species))] public void RealSeedGrowsAndHarvestsItsOwnUsefulProductAndOneReturnedSeed(string stem)
        {
            Assert.True(Factory.Blueprints.ContainsKey(stem+"Seed"),"Actual seed must be obtainable content: "+stem);
            Assert.True(Factory.Blueprints.ContainsKey(stem+"Crop"));
            Cultivate();var seed=Factory.CreateEntity(stem+"Seed");Assert.True(Actor.GetPart<InventoryPart>().AddObject(seed));
            Assert.True(Plant(seed).Success);var entity=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName==stem+"Crop");var crop=entity.GetPart<CropPart>();
            Assert.True(crop.HarvestAtMaturity);Assert.AreEqual(stem+"Seed",crop.SeedYieldBlueprint);Assert.AreEqual(1,crop.SeedYieldCount);
            Tick(2);Assert.Zero(crop.TicksInStage,"Dry ground must not advance growth.");crop.Water(crop.TicksPerStage*2);Tick(crop.TicksPerStage*2);
            Assert.AreEqual(2,crop.GrowthStage);Assert.NotNull(Zone.GetEntityCell(entity));Assert.AreEqual(0,Count(crop.YieldBlueprint));
            Assert.True(Harvest(entity).Success);Assert.IsNull(Zone.GetEntityCell(entity));Assert.AreEqual(crop.YieldCount,Count(crop.YieldBlueprint));Assert.AreEqual(1,Count(stem+"Seed"));
            var outputs=Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName==crop.YieldBlueprint).ToArray();
            foreach(var output in outputs){Assert.True(output.GetPart<PhysicsPart>().Takeable);Assert.False(output.HasPart<FoodPart>());Assert.False(output.HasPart<CookablePart>());Assert.IsEmpty(output.GetPart<TonicPart>()?.StatBoost??"");}
            Assert.True(Soil.HasPart<CultivatedSoilPart>());Assert.False(Harvest(entity).Success);Assert.AreEqual(crop.YieldCount,Count(crop.YieldBlueprint));
        }
    }
}
