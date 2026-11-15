using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelQuillholdRoadTests
    {
        int X=10; const int Y=11;
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;Entity road;
        FlatPixelGreyboxCatalog world;
        [SetUp]public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone(QuillholdCompositionPlan.ZoneID);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;
            road=content.Factory.CreateEntity("RoadStone");zone.AddEntity(road,X,Y);world=new FlatPixelGreyboxCatalog();
        }
        [TearDown]public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell Road()
        {
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var p));Assert.True(p.LocalGround);
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),p.GroundTile.sprite.texture);
            Assert.AreEqual(new Rect((32+((X+Y)&1))*16,0,16,24),p.GroundTile.sprite.rect);return p;
        }
        [TestCase(10)][TestCase(11)]public void LiveMemoryAndExactReaderAgreeWithoutStateMutation(int column)
        {
            if(column!=X){zone.RemoveEntity(road);X=column;zone.AddEntity(road,X,Y);} int version=zone.EntityVersion;var state=zone.TileState.ToSaveString();var p=Road();
            Assert.True(world.TryGetPresentationSubjectSprite(zone,road,X,Y,out var sprite,out _));Assert.AreSame(p.GroundTile.sprite,sprite);
            zone.GetCell(X,Y).IsVisible=false;Assert.AreSame(p.GroundTile,Road().GroundTile);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [TestCase("profile")][TestCase("name")][TestCase("type")][TestCase("graph")][TestCase("lair")]
        public void ChangedSiteCannotUseRegionalRoad(string change)
        {
            Road();var poi=manager.WorldMap.GetPOI(14,9);
            switch(change)
            {
                case "profile":poi.Profile="other";break;
                case "name":poi.Name="other";break;
                case "type":poi.Type=POIType.MerchantCamp;break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "lair":manager.WorldMap.SetPOI(14,9,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(14,9,poi);break;
            }
            if(world.TryResolvePresentation(zone,X,Y,out var p))Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),p.GroundTile.sprite.texture);
        }
        [Test]public void MissingOptionalRoadDeclinesWithoutBorrowingAnotherMaterial()
        {
            Road();var art=typeof(FlatPixelGreyboxCatalog).GetField("_settlementGroundArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            var field=art.GetType().GetField("beatingRoad",BindingFlags.Instance|BindingFlags.NonPublic);var tiles=field.GetValue(art);
            try{field.SetValue(art,null);if(world.TryResolvePresentation(zone,X,Y,out var p))Assert.False(p.LocalGround);}
            finally{field.SetValue(art,tiles);}Road();
        }
        [Test]public void ForeignSavedBiomeRetainsItsExistingRoad()
        {
            Road();manager.WorldMap.Tiles[14,9]=BiomeType.Grovelands;
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var p));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath),p.GroundTile.sprite.texture);
            manager.WorldMap.Tiles[14,9]=BiomeType.Spread;Road();
        }
        [Test]public void PlayerKeepsNativeRoadAndSlipKeepsItsExistingRoad()
        {
            var original=Road().GroundTile;var player=content.Factory.CreateEntity("Player");zone.AddEntity(player,X,Y);
            Assert.AreSame(original,Road().GroundTile);Assert.AreSame(player,Road().Owner);
            var other=new Zone("Overworld.16.11.0");manager.SetActiveZone(other);foreach(var c in other.Cells)c.IsVisible=c.Explored=true;
            zone.RemoveEntity(road);other.AddEntity(road,X,Y);Assert.True(world.TryResolvePresentation(other,X,Y,out var p));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath),p.GroundTile.sprite.texture);
        }
        [TestCase("Floor")][TestCase("StoneFloor")]
        public void CourtsAndInteriorsKeepTheirExistingMaterial(string blueprint)
        {
            zone.RemoveEntity(road);var floor=content.Factory.CreateEntity(blueprint);zone.AddEntity(floor,X,Y);
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var p));Assert.True(p.LocalGround);
            var expected=Resources.Load<Texture2D>(blueprint=="Floor"?FlatPixelSettlementGroundCatalog.TownFloorResourcePath:FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath);
            Assert.AreSame(expected,p.GroundTile.sprite.texture);
            int frame=(int)p.GroundTile.sprite.rect.x/16;
            Assert.That(frame,Is.InRange(blueprint=="Floor"?0:4,blueprint=="Floor"?3:7));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var sprite,out _));Assert.AreSame(p.GroundTile.sprite,sprite);
            zone.GetCell(X,Y).IsVisible=false;Assert.True(world.TryResolvePresentation(zone,X,Y,out var memory));Assert.AreSame(p.GroundTile,memory.GroundTile);
        }
        [Test]public void ReskinnedRoadKeepsFallbackAndRecovers()
        {
            var original=Road().GroundTile;var render=road.GetPart<RenderPart>();var glyph=render.RenderString;
            render.RenderString="?";Assert.True(world.TryResolvePresentation(zone,X,Y,out var p));Assert.False(p.LocalGround);
            render.RenderString=glyph;Assert.AreSame(original,Road().GroundTile);
        }
        [Test]public void WetRoadKeepsFallbackAndRecovers()
        {
            var original=Road().GroundTile;zone.TileState.WriteCoating(X,Y,"water",4);
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var p));Assert.False(p.LocalGround);
            zone.TileState.Clear(X,Y);Assert.AreSame(original,Road().GroundTile);
        }
    }
}
