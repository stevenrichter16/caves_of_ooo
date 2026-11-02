using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelGinmereDescentTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Entity floor, wall; Cell fc,wc;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=manager.GetZone("Overworld.2.7.1");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();
            fc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="SandstoneFloor");floor=fc.Objects[0];
            wc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Any(e=>e.BlueprintName=="SandstoneWall"));wall=wc.Objects.First(e=>e.BlueprintName=="SandstoneWall");
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(Cell c){Assert.True(world.TryResolvePresentation(zone,c.X,c.Y,out var v));return v;}
        void Native()
        {
            Assert.True(View(fc).LocalGround,"Ginmere sandstone floor remains glyphs");
            Assert.True(View(wc).NativeForeground,"Ginmere sandstone wall remains glyphs");
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
        [Test] public void DifferentSiteLabelRevokesBothMaterials()
        {Native();manager.WorldMap.GetPOI(2,7).Name="renamed site";Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);}
        [TestCase("poi")] [TestCase("profile")] [TestCase("absent")]
        [TestCase("graph")] [TestCase("tiles")] [TestCase("pois")]
        public void WarmAdmissionRevokesWhenCurrentAuthorityChanges(string change)
        {
            Native();var p=manager.WorldMap.GetPOI(2,7);
            switch(change)
            {
                case "poi":p.Type=POIType.MerchantCamp;break;
                case "profile":p.Profile=SinkholeSites.SealedLibraryProfile;break;
                case "absent":manager.WorldMap.SetPOI(2,7,null);break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "tiles":manager.WorldMap.Tiles=new BiomeType[1,1];break;
                case "pois":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
            }
            if(world.TryResolvePresentation(zone,fc.X,fc.Y,out var ground))Assert.False(ground.LocalGround);
            if(world.TryResolvePresentation(zone,wc.X,wc.Y,out var body))Assert.False(body.NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,wall,wc.X,wc.Y,out var sprite,out _))
                Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),sprite.texture);
        }
        [TestCase("Overworld.02.7.1")] [TestCase("Overworld.2.7.0")] [TestCase("Overworld.2.7.2")]
        [TestCase("Overworld.2.4.1")]
        public void EqualMaterialNamesCannotExpandPilotAddress(string id)
        {
            Native();zone=new Zone(id);manager.SetActiveZone(zone);fc=zone.GetCell(10,10);fc.Explored=fc.IsVisible=true;
            floor=content.Factory.CreateEntity("SandstoneFloor");zone.AddEntity(floor,10,10);
            // Other independently admitted site catalogs may legitimately render this address.
            // Pin this pilot's boundary directly rather than requiring every family to stay glyphs.
            using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelShallowCavesCatalog(selector))
            {
                var method=typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingGinmereDescent",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.NotNull(method);method.Invoke(art,null);
                Assert.False(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));
            }
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
            var p=manager.WorldMap.GetPOI(2,7);p.Profile=null;p.Name="Ginmere";Native();
            p.Profile=SinkholeSites.SealedLibraryProfile;Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
            p.Profile=null;Native();
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
        [Test] public void SavedLairColumnRevokesEvenWithRestoredSiteLabel()
        {
            Native();var p=manager.WorldMap.GetPOI(2,7);
            manager.WorldMap.SetPOI(2,7,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.2.7.0"));
            manager.WorldMap.SetPOI(2,7,p);Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
        }
        [Test] public void MissingOptionalArtKeepsFallback()
        {using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null)){Assert.True(empty.TryResolvePresentation(zone,fc.X,fc.Y,out var v));Assert.False(v.LocalGround);}}
        [TestCase(false)][TestCase(true)] public void CompetingMaterialHonorsDisclosure(bool hidden)
        {
            Native();var grass=content.Factory.CreateEntity("Grass");grass.GetPart<RenderPart>().Visible=!hidden;
            Assert.True(zone.AddEntity(grass,fc.X,fc.Y));Assert.AreEqual(hidden,View(fc).LocalGround);
        }
        [Test] public void ChangedWallStateAndCoveredReaderRevoke()
        {
            Native();wall.GetPart<RenderPart>().ColorString="&R";Assert.False(View(wc).NativeForeground);
            var player=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,fc.X,fc.Y));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,fc.X,fc.Y,out var s,out _));
            floor.GetPart<RenderPart>().Visible=false;
            if(world.TryGetPresentationSubjectSprite(zone,floor,fc.X,fc.Y,out s,out _))Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),s.texture);
        }
        [TestCase(false)][TestCase(true)] public void MissingGroupsFailIndependently(bool missingFloor)
        {
            Native();using(var selector=new FlatPixelFallbackCatalog())
            using(var art=new FlatPixelShallowCavesCatalog(missingFloor?Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath):null,
                missingFloor?null:Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),null,null,null,null,selector))
            {
                Admit(art);Assert.AreEqual(!missingFloor,art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));
                Assert.AreEqual(missingFloor,art.TryResolveForeground(zone,wc.X,wc.Y,wall,out _));
            }
        }
        [Test] public void AvailableOtherGroupsCannotExpandStructuralPilot()
        {
            Native();using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelShallowCavesCatalog(selector))
            {
                Admit(art);foreach(var name in new[]{"StairsDown","StairsUp","Rubble","Stalagmite"})
                {
                    var other=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(other,fc.X,fc.Y));
                    Assert.False(art.TryResolveForeground(zone,fc.X,fc.Y,other,out _),name);Assert.True(zone.RemoveEntity(other));
                }
                Assert.True(zone.RemoveEntity(floor));Assert.False(art.TryResolveCellBackdrop(zone,fc.X,fc.Y,out _,out _));
            }
        }
        static void Admit(FlatPixelShallowCavesCatalog art)
        {
            var method=typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingGinmereDescent",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(method);method.Invoke(art,null);
        }
    }
}
