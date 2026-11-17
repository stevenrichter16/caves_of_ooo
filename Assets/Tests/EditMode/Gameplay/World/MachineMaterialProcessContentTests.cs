using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineMaterialProcessContentTests
    {
        [TestCase(64)] [TestCase(704860946)] [TestCase(1729)]
        public void FreshDiscardIsIndependentOfAcceptedServicesAndKeepsRoutes(int seed)
        {
            using(var scope=new DensityLootTestScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                var find=zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="ReleasedExtractionTool");Assert.NotNull(find);
                var work=CinderholdCompositionPlan.Create(zone.ZoneID,seed).Rooms.Single(r=>r.Role=="ForgeWorkshop");
                Assert.AreEqual((work.X+12,work.Y+6),zone.GetEntityPosition(find));Assert.True(zone.GenReservedCells.Contains((work.X+12,work.Y+6)));
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="CinderholdOilRecoveryTray"));
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TinkersForge"));
                var actors=zone.GetReadOnlyEntities().Where(e=>e.HasTag("Creature")&&e.GetPart<PhysicsPart>()?.Solid==true).ToArray();foreach(var a in actors)a.GetPart<PhysicsPart>().Solid=false;
                try{CinderholdCompositionTests.AssertConnected(zone);}finally{foreach(var a in actors)a.GetPart<PhysicsPart>().Solid=true;}
                string text=find.GetPart<ExaminablePart>().Text;StringAssert.Contains("released",text);StringAssert.Contains("binding",text);
            }
        }
        [Test] public void InvalidOptionalFindDoesNotRemoveTheTrayOrRetrofitAnExistingTown()
        {
            using(var scope=new DensityLootTestScope())
            {
                scope.Factory.Blueprints.Remove("ReleasedExtractionTool");var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="ReleasedExtractionTool"));
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="CinderholdOilRecoveryTray"));int before=zone.EntityCount;
                Assert.False(new CinderholdCompositionBuilder(64).BuildZone(zone,scope.Factory,new Random(64)));Assert.AreEqual(before,zone.EntityCount);
            }
        }
        [TestCase("CinderhookAxeHeadComponent")] [TestCase("FieldHaftComponent")] [TestCase("SalvagedTimber")] [TestCase("IronSpikeComponent")]
        public void MissingProcessDependencyOmitsOnlyTheOptionalFind(string missing)
        {
            using(var scope=new DensityLootTestScope())
            {
                // Regional head is mandatory smith stock; preserve that service
                // while making only its processing creation contract unavailable.
                scope.Factory.Blueprints[missing].Parts["UnsupportedProcessingBehavior"]=new System.Collections.Generic.Dictionary<string,string>();
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="ReleasedExtractionTool"));
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="CinderholdOilRecoveryTray"));Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TinkersForge"));
            }
        }
        [Test] public void NonPortableDeclaredProductOmitsOnlyTheOptionalFind()
        {
            using(var scope=new DensityLootTestScope())
            {
                scope.Factory.Blueprints["IronSpikeComponent"].Parts["Physics"]["Takeable"]="false";
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="ReleasedExtractionTool"));
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="CinderholdOilRecoveryTray"));
            }
        }
        [Test] public void DefaultDefinitionsAreValidRealPortableContent()
        {
            using(var scope=new DensityLootTestScope())
            {
                var reset=typeof(MaterialProcessRegistry).GetMethod("ResetForTests",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(reset);reset.Invoke(null,null);
                try{Assert.NotNull(MaterialProcessRegistry.Get("reclaim"));Assert.AreEqual(0,MaterialProcessRegistry.Validate(scope.Factory).Count);}
                finally{reset.Invoke(null,null);}
            }
        }
    }
}
