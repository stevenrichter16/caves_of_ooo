using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class StumpCompositionTests
    {
        [Test] public void AmbientLandmarkCatalogKeepsItsSemanticsAndAddsAReservedWalkableApron()
        {
            var native=StampCatalog.For(BiomeType.Stump);var padded=StumpCompositionBuilder.CreateLandmarkCatalog();
            Assert.AreEqual(native.Count,padded.Count);
            for(int i=0;i<native.Count;i++)
            {
                var a=native[i];var b=padded[i];Assert.AreNotSame(a,b);Assert.AreNotSame(a.Rows,b.Rows);
                Assert.AreNotSame(a.Legend,b.Legend);
                Assert.AreEqual(a.Name,b.Name);Assert.AreEqual(a.Chance,b.Chance);Assert.AreEqual(a.MinTier,b.MinTier);Assert.AreEqual(a.ClearsVegetation,b.ClearsVegetation);
                CollectionAssert.AreEquivalent(a.Legend,b.Legend);
                Assert.AreEqual(a.Width+2,b.Width);Assert.AreEqual(a.Height+2,b.Height);
                Assert.IsTrue(b.Rows[0].All(c=>c=='.'));Assert.IsTrue(b.Rows[b.Height-1].All(c=>c=='.'));
                for(int y=0;y<a.Height;y++)Assert.AreEqual("."+a.Rows[y].PadRight(a.Width,'.')+".",b.Rows[y+1]);
            }
        }
        [Test] public void StumpLandmarkGuardRejectsBlockedOutsideApronThatLegacyStampWouldAccept()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.1.0");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)z.AddEntity(f.CreateEntity("TepuiStone"),x,y);
            var method=typeof(LandmarkBuilder).GetMethod("FootprintClear",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            var native=StampCatalog.For(BiomeType.Stump)[0];var padded=StumpCompositionBuilder.CreateLandmarkCatalog()[0];
            Assert.IsTrue((bool)method.Invoke(null,new object[]{z,padded,5,5}));
            var wall=f.CreateEntity("TepuiWall");z.AddEntity(wall,5,5);
            Assert.IsFalse((bool)method.Invoke(null,new object[]{z,padded,5,5}));
            Assert.IsTrue((bool)method.Invoke(null,new object[]{z,native,6,6}));
            z.RemoveEntity(wall);Assert.IsTrue((bool)method.Invoke(null,new object[]{z,padded,5,5}));
        }
        private sealed class FocalRandom:Random
        {
            public override int Next(int maxValue)=>0;
            public override int Next(int minValue,int maxValue)=>maxValue==Zone.Width-1?41:10;
        }
        [Test] public void LaterHaulableDressingRespectsReservedTravelAndHabitatCells()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.2.0");
            var terrain=new StumpCompositionBuilder(1729);Assert.IsTrue(terrain.BuildZone(z,f,new Random(1)));
            z.GenReservedCells.Add((41,10));
            var b=new HaulablePropBuilder(BiomeType.Stump){ChancePerMille=1000};
            Assert.IsTrue(b.BuildZone(z,f,new FocalRandom()));
            Assert.IsFalse(z.GetCell(41,10).Objects.Any(e=>e.BlueprintName=="MillStone"));
            z.GenReservedCells.Remove((41,10));Assert.IsTrue(b.BuildZone(z,f,new FocalRandom()));
            Assert.IsTrue(z.GetCell(41,10).Objects.Any(e=>e.BlueprintName=="MillStone"));
        }
        public static readonly string[] Ids={"Overworld.2.1.0","Overworld.2.2.0","Overworld.4.5.0","Overworld.3.2.0","Overworld.2.3.0"};
        [Test] public void SameSeedRepeatsAndDifferentSeedChangesLandforms()
        {
            foreach(string id in Ids)
            {
                var a=StumpCompositionPlan.Create(id,64);
                Assert.AreEqual(a.Signature(),StumpCompositionPlan.Create(id,64).Signature());
                Assert.AreNotEqual(a.Signature(),StumpCompositionPlan.Create(id,65).Signature());
                var xy=WorldMap.FromZoneID(id);
                Assert.AreEqual(StumpBands.BandAt(xy.x,xy.y),a.Band);
                Assert.AreEqual(FormationSelector.ForStump(a.Band,id),a.Formation);
            }
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void AdjacentBandPortalsMatch(int seed)
        {
            var a=StumpCompositionPlan.Create("Overworld.2.2.0",seed);
            Assert.AreEqual(a.EastY,StumpCompositionPlan.Create("Overworld.3.2.0",seed).WestY);
            Assert.AreEqual(a.SouthX,StumpCompositionPlan.Create("Overworld.2.3.0",seed).NorthX);
        }
        [TestCase(0,"SprayPool")] [TestCase(1,"GrainRidge")] [TestCase(2,"GrainRidge")]
        [TestCase(3,"StoneDome")] [TestCase(4,"Tree")]
        public void NativeLandformHasOpenRoutesAndItsBandResources(int index,string signature)
        {
            var f=GrovelandsCompositionTests.Factory();
            for(int seed=0;seed<16;seed++)
            {
                var z=new Zone(Ids[index]);var builder=new StumpCompositionBuilder(seed);
                Assert.IsTrue(builder.BuildZone(z,f,new Random(seed)));
                var p=builder.Plan;
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==signature));
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),Ids[index]+" seed"+seed);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    Assert.AreEqual("TepuiStone",z.GetCell(x,y).Objects[0].BlueprintName);
                    if(!p.IsApproach(x,y))continue;
                    Assert.IsFalse(z.GetCell(x,y).BlocksMovement());
                    Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                    Assert.IsFalse(z.GetCell(x,y).Objects.Any(e=>e.BlueprintName=="SprayPool"));
                }
                int veins=z.GetAllEntities().Count(e=>e.BlueprintName=="TepuiboneVein");
                if(p.Band==StumpBand.Slopes)Assert.That(veins,Is.InRange(3,12));else Assert.AreEqual(0,veins);
                if(p.Band==StumpBand.Summit)
                {
                    Assert.GreaterOrEqual(z.GetAllEntities().Count(e=>e.BlueprintName=="Tree"),4);
                    Assert.GreaterOrEqual(z.GetAllEntities().Count(e=>e.BlueprintName=="TankBrocchinia"),4);
                }
            }
        }
    }
}
