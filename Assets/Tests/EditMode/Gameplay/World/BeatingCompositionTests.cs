using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class BeatingCompositionTests
    {
        public const string Id="Overworld.16.16.0";
        public static readonly Formation[] Forms={Formation.SaltPan,Formation.RuinField,Formation.DuneBelt,Formation.CaravanRoad,Formation.WindBarrens,Formation.BrineLens};
        [Test] public void LayoutRepeatsButNewSeedsChangeWindAndLandmarks()
        {
            var a=BeatingCompositionPlan.Create(Id,64);
            Assert.AreEqual(a.Signature(),BeatingCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(a.Signature(),BeatingCompositionPlan.Create(Id,65).Signature());
            Assert.AreEqual(FormationSelector.For(BiomeType.Beating,Id),a.Formation);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void AdjacentDryPortalsMatch(int seed)
        {
            var a=BeatingCompositionPlan.Create(Id,seed);
            Assert.AreEqual(a.EastY,BeatingCompositionPlan.Create("Overworld.17.16.0",seed).WestY);
            Assert.AreEqual(a.SouthX,BeatingCompositionPlan.Create("Overworld.16.17.0",seed).NorthX);
        }
        [TestCase(Formation.SaltPan,"SaltCrust")]
        [TestCase(Formation.RuinField,"SandstoneWall")]
        [TestCase(Formation.DuneBelt,"DuneCrest")]
        [TestCase(Formation.CaravanRoad,"RoadStone")]
        [TestCase(Formation.WindBarrens,"DryBrush")]
        [TestCase(Formation.BrineLens,"BrinePool")]
        public void SignatureRoutesAndNativeResourcesRemainUsable(Formation form,string signature)
        {
            var f=GrovelandsCompositionTests.Factory();
            for(int seed=0;seed<24;seed++)
            {
                var zone=new Zone(Id);var builder=new BeatingCompositionBuilder(seed){FormationOverride=form};
                Assert.IsTrue(builder.BuildZone(zone,f,new Random(seed)));
                Assert.IsTrue(zone.GetAllEntities().Any(e=>e.BlueprintName==signature),form+" seed "+seed);
                Assert.IsTrue(FormationReachability.FullyReached(zone,FormationReachability.FloodFromWest(zone)),form+" pockets seed "+seed);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(!builder.Plan.IsApproach(x,y))continue;
                    Assert.IsFalse(zone.GetCell(x,y).BlocksMovement(),form+" approach "+x+","+y);
                    Assert.IsFalse(zone.GetCell(x,y).Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
                    Assert.IsTrue(zone.GenReservedCells.Contains((x,y)));
                }
                var veins=zone.GetAllEntities().Where(e=>e.BlueprintName=="PaleSaltVein").ToArray();
                if(form==Formation.SaltPan)
                {
                    Assert.That(veins.Length,Is.InRange(2,4));
                    Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="Rock"||e.BlueprintName=="Cactus"||e.BlueprintName=="DryBrush"));
                    foreach(var vein in veins)Assert.NotNull(vein.GetPart<HarvestablePart>());
                }
                else Assert.AreEqual(0,veins.Length);
                if(form!=Formation.BrineLens)Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="BrinePool"));
                if(form==Formation.RuinField)
                {
                    Assert.AreEqual(3,builder.Plan.RuinCount);
                    for(int i=0;i<builder.Plan.RuinCount;i++)
                    {
                        var room=builder.Plan.GetRuin(i);
                        Assert.AreEqual("SandstoneFloor",builder.Plan.GroundAt(room.X+room.Width/2,room.Y+room.Height/2));
                        Assert.IsFalse(zone.GetCell(room.X+room.Width/2,room.Y+room.Height/2).IsInterior,"Roofless ruins do not promise glare shelter.");
                    }
                }
            }
        }
    }
}
