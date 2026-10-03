using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SoddenDistrictGenerationAdversarialTests
    {
        [TearDown] public void ClearProbe(){CreationProbe.Remembered=null;}
        [TestCase("Grass")][TestCase("StoneWall")][TestCase("MirePool")][TestCase("Duckboard")]
        [TestCase("SoddenRouteNotice")][TestCase("SoddenDressingBench")][TestCase("SoddenWorksSalvage")][TestCase("SoddenWorksLocker")]
        [TestCase("LeatherBoots")][TestCase("Buckler")][TestCase("KnotflaxCord")][TestCase("SumpsieveCrop")][TestCase("SoddenFieldDressing")]
        [TestCase("Bed")][TestCase("Chair")]
        public void MissingRequiredContentRefusesAnEntireNewSiteBeforeAnyPublication(string blueprint)
        {
            var f=SoddenDistrictGenerationTests.Factory();f.Blueprints.Remove(blueprint);
            foreach(string id in SoddenDistrictGenerationTests.Sites)
            {
                var z=new Zone(id);var b=new SoddenDistrictBuilder(64);
                Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);
                Assert.IsEmpty(z.GenReservedCells);Assert.IsFalse(b.ValidateFinal(z));
            }
        }
        [TestCase("solid-water")][TestCase("passable-bank")][TestCase("invisible-notice")][TestCase("takeable-wall")]
        [TestCase("missing-container")][TestCase("wrong-water")][TestCase("nonportable-reward")][TestCase("missing-armor")]
        public void MalformedNativeOwnerCannotPublishAPartialOrMisleadingDestination(string mode)
        {
            var f=SoddenDistrictGenerationTests.Factory();
            if(mode=="solid-water")f.Blueprints["MirePool"].Parts["Physics"]["Solid"]="true";
            if(mode=="passable-bank"){f.Blueprints["PeatBank"].Parts["Physics"]["Solid"]="false";f.Blueprints["PeatBank"].Tags.Remove("Solid");}
            if(mode=="invisible-notice")f.Blueprints["SoddenRouteNotice"].Parts["Render"]["Visible"]="false";
            if(mode=="takeable-wall")f.Blueprints["StoneWall"].Parts["Physics"]["Takeable"]="true";
            if(mode=="missing-container")f.Blueprints["SoddenWorksLocker"].Parts.Remove("Container");
            if(mode=="wrong-water")f.Blueprints["MirePool"].Parts["LiquidPool"]["LiquidId"]="water";
            if(mode=="nonportable-reward")f.Blueprints["Buckler"].Parts["Physics"]["Takeable"]="false";
            if(mode=="missing-armor")f.Blueprints["LeatherBoots"].Parts.Remove("Armor");
            var z=new Zone(SoddenDistrictPlan.WorksZoneID);var b=new SoddenDistrictBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)),mode);Assert.AreEqual(0,z.EntityCount);Assert.IsEmpty(z.GenReservedCells);
        }
        [Test] public void BenchWithoutItsActualPreparationOwnerRefusesTheSite()
        {
            var f=SoddenDistrictGenerationTests.Factory();f.Blueprints["SoddenDressingBench"].Parts.Remove("SoddenPreparation");
            var z=new Zone(SoddenDistrictPlan.StopZoneID);Assert.IsFalse(new SoddenDistrictBuilder(64).BuildZone(z,f,new Random(1)));Assert.Zero(z.EntityCount);
        }
        [TestCase("Bed",false)][TestCase("Chair",false)][TestCase("Bed",true)][TestCase("Chair",true)]
        public void UnusableShelterFurnitureCannotMasqueradeAsAFreeRestStop(string blueprint,bool occupied)
        {
            var f=SoddenDistrictGenerationTests.Factory();
            if(occupied)f.Blueprints[blueprint].Parts[blueprint]["Occupied"]="true";
            else f.Blueprints[blueprint].Parts.Remove(blueprint);
            var z=new Zone(SoddenDistrictPlan.StopZoneID);Assert.IsFalse(new SoddenDistrictBuilder(64).BuildZone(z,f,new Random(1)));Assert.Zero(z.EntityCount);
        }
        [TestCase("change-first-ground")][TestCase("change-first-identity")]
        public void LaterCreationHookCannotChangeAnEarlierStagedOwner(string mode)
        {
            var f=SoddenDistrictGenerationTests.Factory();f.RegisterPartType<CreationProbe>();
            f.Blueprints["Grass"].Parts["CreationProbe"]=new Dictionary<string,string>{{"Mode","remember-first"}};
            f.Blueprints["SoddenWorksLocker"].Parts["CreationProbe"]=new Dictionary<string,string>{{"Mode",mode}};
            var z=new Zone(SoddenDistrictPlan.WorksZoneID);var b=new SoddenDistrictBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);
        }
        [TestCase("stock")][TestCase("pool")][TestCase("position")][TestCase("alias")]
        public void FinalReceiptRejectsChangedStockHazardPositionOrAnotherGraph(string mutation)
        {
            var f=SoddenDistrictGenerationTests.Factory();var z=new Zone(SoddenDistrictPlan.WorksZoneID);var b=new SoddenDistrictBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(b.ValidateFinal(z));
            if(mutation=="stock")z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SoddenWorksLocker").GetPart<ContainerPart>().Contents.Clear();
            if(mutation=="pool")z.GetReadOnlyEntities().First(e=>e.BlueprintName=="MirePool").GetPart<LiquidPoolPart>().LiquidId="water";
            if(mutation=="position")
            {
                var notice=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SoddenRouteNotice");z.RemoveEntity(notice);z.AddEntity(notice,0,0);
            }
            Assert.IsFalse(b.ValidateFinal(mutation=="alias"?new Zone(z.ZoneID):z));
        }
        [Test] public void ReplayingBuilderCannotReplaceOrRefillTheCurrentGraph()
        {
            var f=SoddenDistrictGenerationTests.Factory();var z=new Zone(SoddenDistrictPlan.WorksZoneID);var b=new SoddenDistrictBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var before=z.GetReadOnlyEntities().ToArray();
            var c=before.Single(e=>e.BlueprintName=="SoddenWorksLocker").GetPart<ContainerPart>();c.Contents.Clear();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(before,z.GetReadOnlyEntities());Assert.IsEmpty(c.Contents);
        }
        public sealed class CreationProbe:Part
        {
            public string Mode;public static Entity Remembered;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID!="ObjectCreated")return true;
                if(Mode=="remember-first"&&Remembered==null)Remembered=ParentEntity;
                if(Mode=="change-first-ground")Remembered.GetPart<PhysicsPart>().Solid=true;
                if(Mode=="change-first-identity")Remembered.ID="late-replacement";
                return true;
            }
        }
    }
}
