using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Dedicated failure and replay gate, separate from the normal
    /// world-action tests. Every refusal retains both the owner and the yield.</summary>
    public sealed class BiomeAffordanceAdversarialTests
    {
        private EntityFactory factory, oldFactory;
        private Zone oldActive;
        [SetUp] public void Setup()
        {
            factory=GrovelandsCompositionTests.Factory();oldFactory=HarvestablePart.Factory;
            HarvestablePart.Factory=factory;oldActive=SettlementRuntime.ActiveZone;SettlementRuntime.ActiveZone=null;
        }
        [TearDown] public void Cleanup()
        {HarvestablePart.Factory=oldFactory;SettlementRuntime.ActiveZone=oldActive;}
        private Entity Row(Zone zone,int x=11,int y=10)
        {var row=factory.CreateEntity("RipeCropRow");Assert.NotNull(row);Assert.IsTrue(zone.AddEntity(row,x,y));return row;}

        [TestCase("actor-null")] [TestCase("zone-null")] [TestCase("foreign-zone")]
        [TestCase("removed-owner")] [TestCase("removed-actor")] [TestCase("far-actor")]
        [TestCase("missing-factory")] [TestCase("missing-blueprint")] [TestCase("empty-blueprint")]
        [TestCase("null-blueprint")] [TestCase("zero-count")] [TestCase("negative-count")]
        [TestCase("wrong-command")]
        public void InvalidOrStaleRequestNeverSpendsTheRowOrMintsFood(string fault)
        {
            var zone=new Zone(SpreadCompositionTests.Id);var actor=BiomeAffordanceTests.Actor(zone);var row=Row(zone);
            var part=row.GetPart<FieldHarvestPart>();Entity requestActor=actor;Zone requestZone=zone;string command="Harvest";
            switch(fault)
            {
                case "actor-null":requestActor=null;break;
                case "zone-null":requestZone=null;break;
                case "foreign-zone":requestZone=new Zone(zone.ZoneID);break;
                case "removed-owner":zone.RemoveEntity(row);break;
                case "removed-actor":zone.RemoveEntity(actor);break;
                case "far-actor":Assert.IsTrue(zone.MoveEntity(actor,1,1));break;
                case "missing-factory":HarvestablePart.Factory=null;break;
                case "missing-blueprint":part.YieldBlueprint="DefinitelyMissingFieldYield";break;
                case "empty-blueprint":part.YieldBlueprint="";break;
                case "null-blueprint":part.YieldBlueprint=null;break;
                case "zero-count":part.YieldCount=0;break;
                case "negative-count":part.YieldCount=-3;break;
                case "wrong-command":command="DrawWaterAtWell";break;
            }
            int count=zone.EntityCount,version=zone.EntityVersion;string name=row.GetDisplayName();
            Assert.IsFalse(BiomeAffordanceTests.Act(row,requestActor,requestZone,command));
            Assert.IsFalse(part.Harvested);Assert.AreEqual(name,row.GetDisplayName());
            Assert.AreEqual(count,zone.EntityCount);Assert.AreEqual(version,zone.EntityVersion);
            Assert.AreEqual(0,BiomeAffordanceTests.Grain(actor));
            Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="Emberwheat"));
        }
        [TestCase(0,0)] [TestCase(1,0)] [TestCase(-1,0)] [TestCase(0,1)] [TestCase(0,-1)]
        [TestCase(1,1)] [TestCase(-1,-1)]
        public void SameCellAndImmediateNeighboursCanGlean(int dx,int dy)
        {
            var zone=new Zone(SpreadCompositionTests.Id);var actor=BiomeAffordanceTests.Actor(zone);var row=Row(zone,10+dx,10+dy);
            Assert.IsTrue(BiomeAffordanceTests.Act(row,actor,zone));Assert.AreEqual(1,BiomeAffordanceTests.Grain(actor));
        }
        [Test] public void ExplicitWrongZoneCannotBeLaunderedThroughActiveZone()
        {
            var zone=new Zone(SpreadCompositionTests.Id);var actor=BiomeAffordanceTests.Actor(zone);var row=Row(zone);
            SettlementRuntime.ActiveZone=zone;
            Assert.IsFalse(BiomeAffordanceTests.Act(row,actor,new Zone(zone.ZoneID)));
            Assert.IsFalse(row.GetPart<FieldHarvestPart>().Harvested);
            Assert.IsTrue(BiomeAffordanceTests.Act(row,actor,null));Assert.AreEqual(1,BiomeAffordanceTests.Grain(actor));
        }
        [Test] public void TwoActorsCannotReplayTheSameGleaningButOtherRowsRemainReady()
        {
            var z=new Zone(SpreadCompositionTests.Id);var first=BiomeAffordanceTests.Actor(z);
            var second=BiomeAffordanceTests.Actor(z,11,11);var row=Row(z);var other=Row(z,12,11);
            Assert.IsTrue(BiomeAffordanceTests.Act(row,first,z));
            for(int n=0;n<4;n++)Assert.IsFalse(BiomeAffordanceTests.Act(row,second,z));
            Assert.AreEqual(1,BiomeAffordanceTests.Grain(first));Assert.AreEqual(0,BiomeAffordanceTests.Grain(second));
            Assert.IsFalse(other.GetPart<FieldHarvestPart>().Harvested);
            Assert.IsTrue(BiomeAffordanceTests.Act(other,second,z));Assert.AreEqual(1,BiomeAffordanceTests.Grain(second));
        }
        [TestCase(0)] [TestCase(500)] public void ConfiguredYieldIsNeitherLostNorDuplicatedAtTheCapacityBoundary(int capacity)
        {
            var z=new Zone(SpreadCompositionTests.Id);var actor=BiomeAffordanceTests.Actor(z,capacity:capacity);var row=Row(z);
            row.GetPart<FieldHarvestPart>().YieldCount=3;Assert.IsTrue(BiomeAffordanceTests.Act(row,actor,z));
            int ground=z.GetEntityCell(row).Objects.Where(e=>e.BlueprintName=="Emberwheat").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            Assert.AreEqual(3,ground+BiomeAffordanceTests.Grain(actor));Assert.AreEqual(capacity==0?3:0,ground);
            Assert.IsFalse(BiomeAffordanceTests.Act(row,actor,z));
        }
        [Test] public void AForeignActorInventoryDoesNotGrantReach()
        {
            var z=new Zone(SpreadCompositionTests.Id);var row=Row(z);
            var actor=BiomeAffordanceTests.Actor(new Zone(z.ZoneID));
            Assert.IsFalse(BiomeAffordanceTests.Act(row,actor,z));Assert.IsFalse(row.GetPart<FieldHarvestPart>().Harvested);
        }
        [Test] public void MissingRipeContentRejectsBeforeAnyGroundIsPlaced()
        {
            factory.Blueprints.Remove("RipeCropRow");var z=new Zone(SpreadCompositionTests.Id);
            Assert.IsFalse(new SpreadCompositionBuilder(64){FormationOverride=Formation.FieldStrips}.BuildZone(z,factory,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);
            Assert.IsTrue(new SpreadCompositionBuilder(64){FormationOverride=Formation.Hedgerow}.BuildZone(z,factory,new Random(1)));
        }
        [Test] public void FieldRowsNeverTickIntoAnotherHarvestOrAutomaticProduce()
        {
            var z=new Zone(SpreadCompositionTests.Id);var actor=BiomeAffordanceTests.Actor(z);var row=Row(z);
            Assert.IsTrue(BiomeAffordanceTests.Act(row,actor,z));
            for(int n=0;n<100;n++)CropSystem.OnTickEnd(z);
            Assert.IsTrue(row.GetPart<FieldHarvestPart>().Harvested);Assert.AreSame(z.GetCell(11,10),z.GetEntityCell(row));
            Assert.AreEqual(1,BiomeAffordanceTests.Grain(actor));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="Emberwheat"));
        }
    }
}
