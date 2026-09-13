using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SoddenCompositionTests
    {
        public const string Id="Overworld.16.3.0";
        public static readonly Formation[] Forms={Formation.OpenMire,Formation.PeatCuts,Formation.ReedMaze,Formation.DrownedCopse,Formation.Causeway,Formation.BogFace};
        [Test] public void WaterlineAndIslandsRepeatButDifferentSeedChangesComposition()
        {
            var a=SoddenCompositionPlan.Create(Id,64);
            Assert.AreEqual(a.Signature(),SoddenCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(a.Signature(),SoddenCompositionPlan.Create(Id,65).Signature());
            Assert.AreEqual(FormationSelector.For(BiomeType.Sodden,Id),a.Formation);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void AdjacentDryPortalsMatch(int seed)
        {
            var a=SoddenCompositionPlan.Create(Id,seed);
            Assert.AreEqual(a.EastY,SoddenCompositionPlan.Create("Overworld.17.3.0",seed).WestY);
            Assert.AreEqual(a.SouthX,SoddenCompositionPlan.Create("Overworld.16.4.0",seed).NorthX);
        }
        [TestCase(Formation.OpenMire,"MirePool")]
        [TestCase(Formation.PeatCuts,"PeatBank")]
        [TestCase(Formation.ReedMaze,"Reeds")]
        [TestCase(Formation.DrownedCopse,"DeadTree")]
        [TestCase(Formation.Causeway,"Duckboard")]
        [TestCase(Formation.BogFace,"PeatBank")]
        public void NativeSignatureWithConnectedDryApproaches(Formation form,string signature)
        {
            var f=GrovelandsCompositionTests.Factory();
            for(int seed=0;seed<16;seed++)
            {
                var z=new Zone(Id);var b=new SoddenCompositionBuilder(seed){FormationOverride=form};
                Assert.IsTrue(b.BuildZone(z,f,new Random(seed)));
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==signature),form+" seed "+seed);
                int wet=0;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var cell=z.GetCell(x,y);
                    Assert.LessOrEqual(cell.Objects.Count(e=>e.HasPart<LiquidPoolPart>()),1);
                    if(b.Plan.IsWet(x,y))wet++;
                    if(!b.Plan.IsApproach(x,y))continue;
                    Assert.IsFalse(cell.BlocksMovement(),form+" approach "+x+","+y);
                    Assert.IsFalse(cell.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
                    Assert.IsFalse(b.Plan.IsWet(x,y));
                    Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                }
                Assert.That(wet,Is.InRange(40,1100),form+" waterline seed "+seed);
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),form+" pockets seed "+seed);
                int toads=z.GetAllEntities().Count(e=>e.BlueprintName=="MawToad");
                if(form==Formation.DrownedCopse)Assert.That(toads,Is.InRange(1,2));else Assert.AreEqual(0,toads);
                if(form!=Formation.Causeway)Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="Duckboard"));
                foreach(var body in z.GetAllEntities().Where(e=>e.BlueprintName=="BogTakenBody"))
                {
                    Assert.IsTrue(form==Formation.PeatCuts||form==Formation.BogFace);
                    var c=z.GetEntityCell(body);
                    Assert.IsTrue(new[]{(1,0),(-1,0),(0,1),(0,-1)}.Any(d=>z.GetCell(c.X+d.Item1,c.Y+d.Item2)?.Objects.Any(e=>e.BlueprintName=="PeatBank")==true));
                }
                Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="PreFellingBody"));
            }
        }
    }
}
