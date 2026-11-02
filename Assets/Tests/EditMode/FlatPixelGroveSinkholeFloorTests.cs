using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelGroveSinkholeFloorTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Cell cell; Entity floor;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            Use("Overworld.2.7.0");world=new FlatPixelGreyboxCatalog();
        }
        void Use(string id)
        {
            zone=manager.GetZone(id);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            cell=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="SandstoneFloor");floor=cell.Objects[0];
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,cell.X,cell.Y,out var v));return v;}
        void Native(){Assert.True(View().LocalGround,"Grovelands sinkhole sandstone remains glyph ground");}
        [TestCase("Overworld.2.7.0")][TestCase("Overworld.5.4.0")]
        public void GeneratedSurfaceUsesApprovedSandstoneWithoutChangingState(string id)
        {
            Use(id);int v=zone.EntityVersion;string state=zone.TileState.ToSaveString();Native();
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),View().GroundTile.sprite.texture);
            Assert.That(View().GroundTile.sprite.rect.x/16,Is.InRange(12,15));Assert.False(View().BlocksMovement);
            Assert.False(FlatPixelShallowCavesCatalog.IsSupportedZone(zone));
            Assert.AreEqual(v,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
            var p=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(p,cell.X,cell.Y));Native();
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,cell.X,cell.Y,out var s,out _));Assert.AreSame(View().GroundTile.sprite,s);
        }
        [TestCase("poi")][TestCase("name")][TestCase("profile")][TestCase("biome")]
        [TestCase("absent")][TestCase("graph")][TestCase("tiles")][TestCase("pois")]
        public void WarmAuthorityRevokes(string change)
        {
            Native();var p=manager.WorldMap.GetPOI(2,7);
            switch(change){
                case "poi":p.Type=POIType.MerchantCamp;break;
                case "name":p.Name="unrelated site";break;
                case "profile":p.Profile=SinkholeSites.SealedLibraryProfile;break;
                case "biome":manager.WorldMap.Tiles[2,7]=BiomeType.Spread;break;
                case "absent":manager.WorldMap.SetPOI(2,7,null);break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "tiles":manager.WorldMap.Tiles=new BiomeType[1,1];break;
                case "pois":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
            }
            if(world.TryResolvePresentation(zone,cell.X,cell.Y,out var r))Assert.False(r.LocalGround);
        }
        [Test] public void SavedLairClaimRevokesEvenAfterLabelRestoration()
        {
            Native();var p=manager.WorldMap.GetPOI(2,7);
            manager.WorldMap.SetPOI(2,7,new PointOfInterest(POIType.Lair,"retained",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.2.7.0"));
            manager.WorldMap.SetPOI(2,7,p);Assert.False(View().LocalGround);
        }
        [TestCase("hidden")][TestCase("memory")][TestCase("changed")][TestCase("oil")]
        [TestCase("water")][TestCase("duplicate")][TestCase("grass")][TestCase("vent")]
        public void DisclosureAndForeignMeaningRefuse(string change)
        {
            Native();switch(change){
                case "hidden":floor.GetPart<RenderPart>().Visible=false;break;
                case "memory":cell.IsVisible=false;break;
                case "changed":floor.GetPart<RenderPart>().ColorString="&R";break;
                case "oil":case "water":zone.TileState.WriteCoating(cell.X,cell.Y,change,5);break;
                case "duplicate":zone.AddEntity(content.Factory.CreateEntity("SandstoneFloor"),cell.X,cell.Y);break;
                case "grass":zone.AddEntity(content.Factory.CreateEntity("Grass"),cell.X,cell.Y);break;
                case "vent":zone.AddEntity(content.Factory.CreateEntity("SteamVent"),cell.X,cell.Y);break;
            }
            if(world.TryResolvePresentation(zone,cell.X,cell.Y,out var r))Assert.False(r.LocalGround);
        }
        [TestCase("hidden")][TestCase("changed")][TestCase("authority")]
        public void WarmCoveredReaderRevokesWithWorld(string change)
        {
            Native();zone.AddEntity(content.Factory.CreateEntity("Player"),cell.X,cell.Y);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,cell.X,cell.Y,out var original,out _));
            Assert.AreSame(View().GroundTile.sprite,original);
            if(change=="hidden")floor.GetPart<RenderPart>().Visible=false;
            else if(change=="changed")floor.GetPart<RenderPart>().ColorString="&R";
            else manager.WorldMap.GetPOI(2,7).Type=POIType.MerchantCamp;
            Assert.False(View().LocalGround);
            if(world.TryGetPresentationSubjectSprite(zone,floor,cell.X,cell.Y,out var revised,out _))
                Assert.AreNotSame(original.texture,revised.texture);
        }
        [TestCase("Grass")][TestCase("SteamVent")]
        public void HiddenCompetitorCannotChangeFloorPresentation(string blueprint)
        {
            Native();var before=View().GroundTile;var hidden=content.Factory.CreateEntity(blueprint);
            hidden.GetPart<RenderPart>().Visible=false;zone.AddEntity(hidden,cell.X,cell.Y);
            Assert.AreSame(before,View().GroundTile);Assert.True(View().LocalGround);
        }
        [Test] public void OptionalFloorResourceAndFloorOnlyContract()
        {
            Native();using(var selector=new FlatPixelFallbackCatalog())
            using(var art=new FlatPixelShallowCavesCatalog(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),null,null,null,Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),selector))
            {
                var method=typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingGroveSinkholeFloors",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.NotNull(method);method.Invoke(art,null);
                Assert.False(art.TryResolveGround(zone,cell.X,cell.Y,out _,out _));
                var stairs=content.Factory.CreateEntity("StairsDown");zone.AddEntity(stairs,cell.X,cell.Y);
                Assert.False(art.TryResolveForeground(zone,cell.X,cell.Y,stairs,out _));
                zone.RemoveEntity(stairs);zone.RemoveEntity(floor);
                Assert.False(art.TryResolveCellBackdrop(zone,cell.X,cell.Y,out _,out _));
                Assert.True(selector.TryResolve(zone,cell.X,cell.Y,out _));
            }
            using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null))
            {Assert.True(empty.TryResolvePresentation(zone,cell.X,cell.Y,out var r));Assert.False(r.LocalGround);}
        }
        [TestCase("Overworld.2.7.1")][TestCase("Overworld.5.4.1")][TestCase("Overworld.2.4.0")]
        [TestCase("Overworld.02.7.0")]
        public void SameMaterialCannotExpandAddress(string id)
        {
            Native();zone=new Zone(id);manager.SetActiveZone(zone);cell=zone.GetCell(10,10);cell.Explored=cell.IsVisible=true;
            floor=content.Factory.CreateEntity("SandstoneFloor");zone.AddEntity(floor,10,10);
            // Other independently admitted site catalogs may legitimately render this address.
            // Pin this pilot's boundary directly rather than requiring every family to stay glyphs.
            using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelShallowCavesCatalog(selector))
            {
                var method=typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingGroveSinkholeFloors",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.NotNull(method);method.Invoke(art,null);
                Assert.False(art.TryResolveGround(zone,cell.X,cell.Y,out _,out _));
            }
        }
    }
}
