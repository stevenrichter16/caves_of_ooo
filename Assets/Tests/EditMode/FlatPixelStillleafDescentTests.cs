using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelStillleafDescentTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Entity floor, wall; Cell fc,wc;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=manager.GetZone("Overworld.2.4.1");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();
            fc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="SandstoneFloor");floor=fc.Objects[0];
            wc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Any(e=>e.BlueprintName=="SandstoneWall"));wall=wc.Objects.First(e=>e.BlueprintName=="SandstoneWall");
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(Cell c){Assert.True(world.TryResolvePresentation(zone,c.X,c.Y,out var v));return v;}
        void Native()
        {
            Assert.True(View(fc).LocalGround,"Stillleaf sandstone floor remains glyphs");
            Assert.True(View(wc).NativeForeground,"Stillleaf sandstone wall remains glyphs");
        }
        [Test] public void ActualAuthoredDescentReusesApprovedMaterialsAndPreservesGeometry()
        {
            int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();Native();
            Assert.False(FlatPixelShallowCavesCatalog.IsSupportedZone(zone),"ordinary cave scope stays separate");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),View(fc).GroundTile.sprite.texture);
            Assert.That(View(fc).GroundTile.sprite.rect.x/16,Is.InRange(12,15));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),View(wc).ForegroundTile.sprite.texture);
            Assert.That(View(wc).ForegroundTile.sprite.rect.x/16,Is.InRange(0,1));
            Assert.False(View(fc).BlocksMovement);Assert.True(View(wc).BlocksMovement);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void UnderfootAndWallReadersUseTheirActualWorldIdentity()
        {
            Native();var player=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,fc.X,fc.Y));Assert.True(View(fc).LocalGround);Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,fc.X,fc.Y,out var fs,out _));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,wall,wc.X,wc.Y,out var ws,out _));
            Assert.AreSame(View(fc).GroundTile.sprite,fs);Assert.AreSame(View(wc).ForegroundTile.sprite,ws);
        }
        [Test] public void PersistedLibraryProfileSurvivesAStoryRename()
        {manager.WorldMap.GetPOI(2,4).Name="renamed by story";Native();}
        [TestCase("poi")] [TestCase("profile")] [TestCase("absent")]
        [TestCase("graph")] [TestCase("tiles")] [TestCase("pois")]
        public void WarmAdmissionRevokesWhenCurrentAuthorityChanges(string change)
        {
            Native();var p=manager.WorldMap.GetPOI(2,4);
            switch(change)
            {
                case "poi":p.Type=POIType.MerchantCamp;break;
                case "profile":p.Profile=null;p.Name="Ginmere";break;
                case "absent":manager.WorldMap.SetPOI(2,4,null);break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "tiles":manager.WorldMap.Tiles=new BiomeType[1,1];break;
                case "pois":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
            }
            if(world.TryResolvePresentation(zone,fc.X,fc.Y,out var ground))Assert.False(ground.LocalGround);
            if(world.TryResolvePresentation(zone,wc.X,wc.Y,out var body))Assert.False(body.NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,wall,wc.X,wc.Y,out var sprite,out _))
                Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),sprite.texture);
        }
        [TestCase("Overworld.02.4.1")] [TestCase("Overworld.2.4.0")] [TestCase("Overworld.2.4.2")]
        [TestCase("Overworld.2.7.1")]
        public void EqualMaterialNamesCannotExpandPilotAddress(string id)
        {
            Native();zone=new Zone(id);manager.SetActiveZone(zone);fc=zone.GetCell(10,10);fc.Explored=fc.IsVisible=true;
            floor=content.Factory.CreateEntity("SandstoneFloor");zone.AddEntity(floor,10,10);Assert.False(View(fc).LocalGround);
        }
        [TestCase("hidden")] [TestCase("memory")] [TestCase("changed")] [TestCase("coating")]
        public void DisclosureAndMaterialStateKeepTruthfulFallback(string change)
        {
            Native();switch(change)
            {
                case "hidden":floor.GetPart<RenderPart>().Visible=false;break;
                case "memory":fc.IsVisible=false;break;
                case "changed":floor.GetPart<RenderPart>().ColorString="&R";break;
                case "coating":zone.TileState.WriteCoating(fc.X,fc.Y,"oil",5);break;
            }
            if(world.TryResolvePresentation(zone,fc.X,fc.Y,out var v))Assert.False(v.LocalGround);
        }
        [Test] public void LegacyNameStillAdmitsButConflictingProfileRevokes()
        {
            var p=manager.WorldMap.GetPOI(2,4);p.Profile=null;p.Name="Stillleaf";Native();
            p.Profile=SinkholeSites.FoundingVillageProfile;Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
            p.Profile=SinkholeSites.SealedLibraryProfile;Native();
        }
        [Test] public void SpecialDescentLedgesKeepTheirOwnTerrainMeaning()
        {
            Native();var ledge=content.Factory.CreateEntity("DescentLedge");Assert.NotNull(ledge);
            Assert.True(zone.AddEntity(ledge,fc.X,fc.Y));Assert.True(View(fc).LocalGround);
            Assert.AreSame(ledge,View(fc).Owner);Assert.False(View(fc).NativeForeground);
            Assert.True(zone.RemoveEntity(ledge));Native();
        }
        [TestCase(false)] [TestCase(true)] public void HiddenOrRememberedWallCannotUseLiveSprite(bool memory)
        {
            Native();if(memory)wc.IsVisible=false;else wall.GetPart<RenderPart>().Visible=false;
            if(world.TryResolvePresentation(zone,wc.X,wc.Y,out var v))Assert.False(v.NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,wall,wc.X,wc.Y,out var sprite,out _))
                Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),sprite.texture);
        }
        [Test] public void SavedLairColumnRevokesEvenWithRestoredLibraryLabel()
        {
            Native();var p=manager.WorldMap.GetPOI(2,4);
            manager.WorldMap.SetPOI(2,4,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.2.4.0"));
            manager.WorldMap.SetPOI(2,4,p);Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
        }
        [Test] public void MissingOptionalArtKeepsFallback()
        {using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null)){Assert.True(empty.TryResolvePresentation(zone,fc.X,fc.Y,out var v));Assert.False(v.LocalGround);}}
    }
}
