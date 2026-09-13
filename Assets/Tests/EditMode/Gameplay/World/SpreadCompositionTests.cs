using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SpreadCompositionTests
    {
        public const string Id="Overworld.8.4.0";
        [Test] public void PlanRepeatsButDifferentSeedsChangeFields()
        {
            var a=SpreadCompositionPlan.Create(Id,64);
            Assert.AreEqual(a.Signature(),SpreadCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(a.Signature(),SpreadCompositionPlan.Create(Id,65).Signature());
            Assert.AreEqual(FormationSelector.For(BiomeType.Spread,Id),a.Formation);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void NeighborDryGatesMatch(int seed)
        {
            var a=SpreadCompositionPlan.Create(Id,seed);
            Assert.AreEqual(a.EastY,SpreadCompositionPlan.Create("Overworld.9.4.0",seed).WestY);
            Assert.AreEqual(a.SouthX,SpreadCompositionPlan.Create("Overworld.8.5.0",seed).NorthX);
            Assert.IsTrue(a.IsApproach(0,a.WestY));
            Assert.IsTrue(a.IsApproach(a.NorthX,0));
        }
        [TestCase(Formation.Hedgerow,"Hedge")]
        [TestCase(Formation.FieldStrips,"CropRow")]
        [TestCase(Formation.OldRoad,"RoadStone")]
        [TestCase(Formation.FlowerMeadow,"FlowerField")]
        [TestCase(Formation.Fallow,"Bush")]
        [TestCase(Formation.RiverMeadow,"Reeds")]
        public void NativeSignaturesAndDryApproaches(Formation formation,string signature)
        {
            var f=GrovelandsCompositionTests.Factory();
            for(int seed=0;seed<16;seed++)
            {
                var z=new Zone(Id);var b=new SpreadCompositionBuilder(seed){FormationOverride=formation};
                Assert.IsTrue(b.BuildZone(z,f,new Random(seed)));
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==signature),formation+" seed "+seed);
                for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
                    if(b.Plan.IsApproach(x,y))
                    {
                        Assert.IsFalse(z.GetCell(x,y).BlocksMovement(),formation+" approach "+x+","+y);
                        Assert.IsFalse(CavesOfOoo.Rendering.SpawnRing3DRecipes.HasPermanentWater(z,x,y));
                    }
                FormationReachability.FloodFromWest(z,out bool crossed);Assert.IsTrue(crossed);
                Assert.Less(z.GetAllEntities().Count(e=>e.BlueprintName=="CropRow"),260);
                Assert.Less(z.GetAllEntities().Count(e=>e.BlueprintName=="Tree"),65);
                if(formation!=Formation.FieldStrips)Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="CropRow"));
                if(formation!=Formation.FlowerMeadow)Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="FlowerField"));
            }
        }
    }
}
