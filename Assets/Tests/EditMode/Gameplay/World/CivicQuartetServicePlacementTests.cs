using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class CivicQuartetServicePlacementTests
    {
        [SetUp] public void Load()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void Reset()=>LootTableRegistry.ResetForTests();
        [TestCase(false)][TestCase(true)]
        public void OptInServiceAnchorSurvivesEarlierDecorAndResidentRolls(bool useAnchor)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.1.1.0");
            for(int y=0;y<25;y++)for(int x=0;x<80;x++){z.AddEntity(f.CreateEntity("StoneFloor"),x,y);z.GetCell(x,y).IsInterior=true;}
            var anchor=z.GetCell(7,5);var builder=new VillagePopulationBuilder(new PointOfInterest(POIType.Village,"test"));
            if(useAnchor)builder.PreferredServiceCell=(zone,bp)=>bp=="Scribe"?anchor:null;
            Assert.IsTrue(builder.BuildZone(z,f,new Random(64)));var actor=z.GetAllEntities().Single(e=>e.BlueprintName=="Scribe");
            Assert.AreEqual(useAnchor,ReferenceEquals(z.GetEntityCell(actor),anchor));Assert.Greater(actor.GetPart<InventoryPart>().Objects.Count,0);
            Assert.AreEqual("Scribe_1",actor.GetPart<ConversationPart>().ConversationID);Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Quartermaster"));
        }
        [TestCase("foreign")][TestCase("blocked")][TestCase("reserved")][TestCase("water")][TestCase("furniture")]
        public void InvalidPreferredCellNeverTeleportsStacksOrDeletesAService(string invalid)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.1.1.0");
            for(int y=0;y<25;y++)for(int x=0;x<80;x++){z.AddEntity(f.CreateEntity("StoneFloor"),x,y);z.GetCell(x,y).IsInterior=true;}
            var anchor=z.GetCell(7,5);if(invalid=="blocked")z.AddEntity(f.CreateEntity("SandstoneWall"),7,5);
            if(invalid=="reserved")z.GenReservedCells.Add((7,5));if(invalid=="water")z.AddEntity(f.CreateEntity("WaterPuddle"),7,5);if(invalid=="furniture")z.AddEntity(f.CreateEntity("Chair"),7,5);
            var supplied=invalid=="foreign"?new Zone(z.ZoneID).GetCell(7,5):anchor;
            var b=new VillagePopulationBuilder(new PointOfInterest(POIType.Village,"test")){RespectInteriorReservations=true,PreferredServiceCell=(zone,bp)=>bp=="Scribe"?supplied:null};
            Assert.IsTrue(b.BuildZone(z,f,new Random(64)));var actor=z.GetAllEntities().Single(e=>e.BlueprintName=="Scribe");Assert.AreNotSame(anchor,z.GetEntityCell(actor));Assert.AreNotSame(supplied,z.GetEntityCell(actor));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Merchant"));
        }
    }
}
