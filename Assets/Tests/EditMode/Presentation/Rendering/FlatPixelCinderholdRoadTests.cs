using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Paint=CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelCinderholdRoadTests
    {
        const int X=10,Y=11;
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;Entity road;
        FlatPixelGreyboxCatalog world;
        [SetUp]public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone(CinderholdCompositionPlan.ZoneID);manager.SetActiveZone(zone);
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
        [Test]public void LiveMemoryAndExactReaderAgreeWithoutStateMutation()
        {
            int version=zone.EntityVersion;var state=zone.TileState.ToSaveString();var p=Road();
            Assert.True(world.TryGetPresentationSubjectSprite(zone,road,X,Y,out var sprite,out _));Assert.AreSame(p.GroundTile.sprite,sprite);
            zone.GetCell(X,Y).IsVisible=false;Assert.AreSame(p.GroundTile,Road().GroundTile);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [TestCase("profile")][TestCase("name")][TestCase("type")][TestCase("graph")][TestCase("lair")]
        public void ChangedSiteCannotUseRegionalRoad(string change)
        {
            Road();var poi=manager.WorldMap.GetPOI(6,6);
            switch(change)
            {
                case "profile":poi.Profile="other";break;
                case "name":poi.Name="other";break;
                case "type":poi.Type=POIType.MerchantCamp;break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "lair":manager.WorldMap.SetPOI(6,6,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(6,6,poi);break;
            }
            if(world.TryResolvePresentation(zone,X,Y,out var p))Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),p.GroundTile.sprite.texture);
        }
        [Test]public void MissingRoadClearsItsShadowWithoutRemovingWall()
        {
            Road();var wall=content.Factory.CreateEntity("SandstoneWall");zone.AddEntity(wall,X,Y-1);
            using(var p=new Paint(world,zone))
            {
                p.Step();Assert.NotNull(p.Shade.GetTile(Paint.Position(X,Y-1)));var body=p.At(X,Y-1);
                var art=typeof(FlatPixelGreyboxCatalog).GetField("_settlementGroundArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
                var field=art.GetType().GetField("beatingRoad",BindingFlags.Instance|BindingFlags.NonPublic);var tiles=field.GetValue(art);
                try{field.SetValue(art,null);p.Renderer.MarkDirty("missing Cinderhold road");p.Step();Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y-1)));Assert.AreSame(body,p.At(X,Y-1));}
                finally{field.SetValue(art,tiles);}p.Renderer.MarkDirty("restore road");p.Step();Assert.NotNull(p.Shade.GetTile(Paint.Position(X,Y-1)));
            }
        }
        [Test]public void PlayerKeepsNativeRoadAndTallyKeepsItsExistingRoad()
        {
            var original=Road().GroundTile;var player=content.Factory.CreateEntity("Player");zone.AddEntity(player,X,Y);
            Assert.AreSame(original,Road().GroundTile);Assert.AreSame(player,Road().Owner);
            var other=new Zone("Overworld.10.14.0");manager.SetActiveZone(other);foreach(var c in other.Cells)c.IsVisible=c.Explored=true;
            zone.RemoveEntity(road);other.AddEntity(road,X,Y);Assert.True(world.TryResolvePresentation(other,X,Y,out var p));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath),p.GroundTile.sprite.texture);
        }
    }
}
