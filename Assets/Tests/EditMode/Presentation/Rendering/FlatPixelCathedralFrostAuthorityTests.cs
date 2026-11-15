using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelCathedralFrostAuthorityTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelFallbackCatalog selector; FlatPixelFrostVentCatalog art; Entity vent;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope(); manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            selector=new FlatPixelFallbackCatalog(); art=new FlatPixelFrostVentCatalog(selector);
            Place("Overworld.5.4.1");
            Assert.AreEqual(BiomeType.Grovelands,manager.WorldMap.GetBiome(5,4));
            Assert.AreEqual("the Deepest Cathedral",manager.WorldMap.GetPOI(5,4).Name);
            Assert.AreEqual(POIType.Sinkhole,manager.WorldMap.GetPOI(5,4).Type);
        }
        [TearDown] public void Cleanup() { art?.Dispose();selector?.Dispose();content?.Dispose(); }
        Entity Add(string name) { var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,10,10));return e; }
        void Place(string id)
        {
            zone=new Zone(id);manager.SetActiveZone(zone);
            zone.GetCell(10,10).IsVisible=zone.GetCell(10,10).Explored=true;
            Add("SandstoneFloor");vent=Add("FrostVent");vent.GetPart<TileStateSourcePart>().Seed(zone,10,10);
        }
        void Native(bool expected)
        {
            Assert.AreEqual(expected,art.TryResolve(zone,10,10,vent,out _));
            Assert.AreEqual(expected,art.TryCold(zone,10,10,out _));
        }
        [TestCase("profile")][TestCase("biome")][TestCase("name")][TestCase("type")]
        [TestCase("cache")][TestCase("detached")][TestCase("lair")][TestCase("nullPoi")]
        [TestCase("shortTiles")][TestCase("shortPois")][TestCase("unexplored")]
        public void WarmCurrentAuthorityLossRefusesBothBodyAndCold(string loss)
        {
            Native(true);var map=manager.WorldMap;var poi=map.GetPOI(5,4);
            switch(loss)
            {
                case "profile":poi.Profile="other";break;
                case "biome":map.Tiles[5,4]=BiomeType.Sodden;break;
                case "name":poi.Name="other";break;
                case "type":poi.Type=POIType.Village;break;
                case "cache":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "detached":manager.CachedZones.Remove(zone.ZoneID);break;
                case "nullPoi":map.SetPOI(5,4,null);break;
                case "shortTiles":map.Tiles=new BiomeType[6,5];break;
                case "shortPois":map.POIs=new PointOfInterest[6,5];break;
                case "unexplored":zone.GetCell(10,10).Explored=false;break;
                case "lair":map.SetPOI(5,4,new PointOfInterest(POIType.Lair,"legacy",null,1,"DesertProwler"));LairStacks.Restore(manager,null);map.SetPOI(5,4,poi);break;
            }
            Native(false);
        }
        [TestCase("Overworld.5.4.0")][TestCase("Overworld.5.4.2")]
        [TestCase("Overworld.05.4.1")][TestCase("Overworld.4.6.1")]
        public void OtherDepthAddressAndSinkholeNeverBorrowVentArt(string id) { Place(id);Native(false); }
        [TestCase("StoneFloor")][TestCase("DescentLedge")][TestCase("foreign")]
        public void DifferentOrForeignFloorDoesNotSupportNativeVent(string kind)
        {
            Native(true);Entity floor=null;foreach(var e in zone.GetCell(10,10).Objects)if(e.BlueprintName=="SandstoneFloor")floor=e;
            if(kind=="foreign")floor.GetPart<PhysicsPart>().ParentEntity=vent;
            else { zone.RemoveEntity(floor);Add(kind); }
            Native(false);
        }
        [Test] public void NullAndEmptyProfileRecoverAfterAuthorityLoss()
        {
            var poi=manager.WorldMap.GetPOI(5,4);poi.Profile=null;Native(true);
            poi.Profile="other";Native(false);poi.Profile="";Native(true);
        }
    }
}
