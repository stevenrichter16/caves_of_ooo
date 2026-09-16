using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class WellmeetCompositionTests
    {
        [Test] public void ExactWellmeetPlanIsDeterministicAndSeedChangesItsArchitecture()
        {
            const string id="Overworld.8.16.0";Assert.IsTrue(WellmeetCompositionPlan.IsSupportedZone(id));
            Assert.AreEqual(WellmeetCompositionPlan.Create(id,64).Signature(),WellmeetCompositionPlan.Create(id,64).Signature());
            Assert.AreNotEqual(WellmeetCompositionPlan.Create(id,64).Signature(),WellmeetCompositionPlan.Create(id,1729).Signature());
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.8.16.1")] [TestCase("Overworld.5.17.0")]
        [TestCase("Overworld.8.16.00")] [TestCase(" Overworld.8.16.0")] [TestCase("Overworld.10.10.0")]
        public void ScopeRejectsOtherSettlementsDepthsAndAliases(string id)
        {Assert.IsFalse(WellmeetCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>WellmeetCompositionPlan.Create(id,64));}
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FiveUnequalSemanticTentsHaveNativeShadeReachableDoorsAndAFreeWellCommons(int seed)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(seed);
            Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;
            Assert.NotNull(p);Assert.AreEqual(5,p.Tents.Count);Assert.GreaterOrEqual(p.Tents.Select(t=>t.Role).Distinct().Count(),3);
            Assert.GreaterOrEqual(p.Tents.Select(t=>t.Width*t.Height).Distinct().Count(),3);
            foreach(var t in p.Tents)
            {
                Assert.IsFalse(z.GetCell(t.DoorX,t.DoorY).BlocksMovement());Assert.IsTrue(p.IsApproach(t.DoorX,t.DoorY));
                int shades=0;for(int y=t.Y+1;y<t.Y+t.Height-1;y++)for(int x=t.X+1;x<t.X+t.Width-1;x++)
                {Assert.IsTrue(z.GetCell(x,y).IsInterior);Assert.IsTrue(z.GetCell(x,y).Objects.Any(e=>e.BlueprintName=="StoneFloor"));shades++;}
                Assert.GreaterOrEqual(shades,8);
            }
            foreach(var c in new[]{(40,12),(39,12),(41,12),(40,11),(40,13)})
            {Assert.IsFalse(z.GetCell(c.Item1,c.Item2).BlocksMovement());Assert.IsFalse(z.GetCell(c.Item1,c.Item2).IsInterior);}
            Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)));
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var c=z.GetCell(x,y);Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                Assert.IsFalse(c.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                if(p.IsApproach(x,y)){Assert.IsFalse(c.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
            }
        }
        [Test] public void DedicatedLateProfileOwnsExactlyOneNativeHostSaltMasterClothAndAdditionalWell()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="TentRightHost"));
            var late=new WellmeetProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            foreach(var bp in new[]{"TentRightHost","SaltMaster","GuestClothPole","Well"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp));
            Assert.IsFalse(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="Well"),"The later native VillagePopulation owns the repairable main well.");
            var owners=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(owners,z.GetAllEntities());
        }
        [Test] public void BaseAndLateBuilderCannotRebuildOrCrossApplyToAnotherZoneInstance()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var owners=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(owners,z.GetAllEntities());
            var other=new Zone(z.ZoneID);Assert.IsFalse(new WellmeetProfileBuilder(b).BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void ManagerKeepsNativeVillageServicesAndProfileButOmitsOnlyWellmeetUnmappedRiver(int seed)
        {
            var f=GrovelandsCompositionTests.Factory();var m=new OverworldZoneManager(f,seed);var z=m.GetZone("Overworld.8.16.0");
            Assert.IsFalse(WorldMapAuthoring.IsRiver(8,16));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="WaterPuddle"));
            foreach(var bp in new[]{"TentRightHost","SaltMaster","GuestClothPole","Elder","Merchant","Quartermaster"})
                Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="Well"));
            var well=z.GetCell(40,12).Objects.Single(e=>e.BlueprintName=="Well");Assert.NotNull(well.GetPart<WellSitePart>());
            Assert.AreEqual(z.ZoneID,well.GetProperty("SettlementId"));Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Shrine"));
            var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(m,new object[]{z.ZoneID});
            Assert.IsTrue(pipeline.Builders.OfType<CaveEntranceBuilder>().Any(),"Preserve the existing cave entrance roll, not a fabricated guaranteed stair.");
            Assert.IsTrue(m.GetZone("Overworld.5.17.0").GetAllEntities().Any(e=>e.BlueprintName=="WaterPuddle"),"Other village river behavior remains unchanged.");
        }
        [Test] public void RepairTrackingIsExactWellmeetTentCampAndKeepsAllThreeNativeSites()
        {
            var poi=new PointOfInterest(POIType.Village,"Renamed camp",profile:"TentCamp");
            Assert.IsTrue(SettlementSiteDefinitions.IsTrackedVillage(WellmeetCompositionPlan.ZoneID,poi));
            CollectionAssert.AreEquivalent(new[]{"MainWell","VillageOven","VillageLantern"},
                SettlementSiteDefinitions.CreateInitialSites(WellmeetCompositionPlan.ZoneID,poi).Select(s=>s.SiteId));
            Assert.IsFalse(SettlementSiteDefinitions.IsTrackedVillage("Overworld.5.17.0",poi));
            Assert.IsFalse(SettlementSiteDefinitions.IsTrackedVillage("Overworld.8.16.1",poi));
            Assert.IsFalse(SettlementSiteDefinitions.IsTrackedVillage(WellmeetCompositionPlan.ZoneID,new PointOfInterest(POIType.Village,"Wellmeet",profile:"TentCampFirst")));
            Assert.IsFalse(SettlementSiteDefinitions.IsTrackedVillage(WellmeetCompositionPlan.ZoneID,new PointOfInterest(POIType.Lair,"Wellmeet",profile:"TentCamp")));
            Assert.IsFalse(SettlementSiteDefinitions.IsTrackedVillage(WellmeetCompositionPlan.ZoneID,null));
        }
        [Test] public void NativePipelineAttachesWellOvenAndLanternToActualRepairRecords()
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var z=m.GetZone(WellmeetCompositionPlan.ZoneID);
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.HasPart<WellSitePart>()));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.HasPart<OvenSitePart>()));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.HasPart<LanternSitePart>()));
            foreach(var id in new[]{"MainWell","VillageOven","VillageLantern"})Assert.NotNull(m.SettlementManager.GetSite(z.ZoneID,id),id);
            Assert.IsNull(m.GetZone("Overworld.5.17.0").GetAllEntities().FirstOrDefault(e=>e.HasPart<WellSitePart>()));
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void OutdoorRoadsForkWithoutAnUnbrokenCrossAndPreserveStoneOnlyForShade(int seed)
        {
            var p=WellmeetCompositionPlan.Create(WellmeetCompositionPlan.ZoneID,seed);int roads=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                if(p.IsInterior(x,y))Assert.AreEqual("StoneFloor",p.GroundAt(x,y));
                else Assert.AreNotEqual("StoneFloor",p.GroundAt(x,y),"Outdoor routes are packed earth, not indoor floor.");
                if(p.GroundAt(x,y)=="RoadStone")roads++;
            }
            Assert.That(roads,Is.InRange(160,370));
            Assert.IsTrue(Enumerable.Range(3,74).Any(x=>!p.IsApproach(x,12)),"The whole east-west row must not remain one straight paved axis.");
            Assert.IsTrue(Enumerable.Range(2,21).Any(y=>!p.IsApproach(40,y)),"The north-south axis must bend around the court.");
        }
        [Test] public void WorkAndSaltSheltersReadDifferentlyFromBedsAndSparseFringesFrameCamp()
        {
            var p=WellmeetCompositionPlan.Create(WellmeetCompositionPlan.ZoneID,64);
            foreach(var role in new[]{"ServiceShelter","SaltExchange"})
            {
                var t=p.Tents.Single(a=>a.Role==role);var ids=Enumerable.Range(t.Y,t.Height).SelectMany(y=>Enumerable.Range(t.X,t.Width).Select(x=>p.ObjectAt(x,y))).ToArray();
                Assert.IsFalse(ids.Contains("Bed"),role);Assert.IsTrue(ids.Contains("Crate"),role);
            }
            int brush=0,rock=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                string bp=p.ObjectAt(x,y);if(bp=="DryBrush")brush++;if(bp=="Rock")rock++;
                if(bp=="DryBrush"||bp=="Rock"){Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));}
            }
            Assert.That(brush,Is.InRange(16,55));Assert.That(rock,Is.InRange(6,16));
        }
    }
}
