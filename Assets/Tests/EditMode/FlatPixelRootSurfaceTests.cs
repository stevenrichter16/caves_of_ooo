using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelRootSurfaceTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Cell fc,wc,bc,tc; Entity floor,wall,bush,tree;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=manager.GetZone("Overworld.3.3.0");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();
            fc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="TepuiStone");floor=fc.Objects[0];
            wall=zone.GetAllEntities().First(e=>e.BlueprintName=="TepuiWall");wc=zone.GetEntityCell(wall);
            bush=zone.GetAllEntities().First(e=>e.BlueprintName=="Bush");bc=zone.GetEntityCell(bush);
            tree=zone.GetAllEntities().First(e=>e.BlueprintName=="Tree");tc=zone.GetEntityCell(tree);
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(Cell c){Assert.True(world.TryResolvePresentation(zone,c.X,c.Y,out var v));return v;}
        void Native(){Assert.True(View(fc).LocalGround,"Root surface remains glyph ground");Assert.True(View(wc).NativeForeground,"Root cliff remains glyphs");}
        static void Admit(FlatPixelStumpMaterialsCatalog art)
        {var m=typeof(FlatPixelStumpMaterialsCatalog).GetMethod("AdmittingRootSurface",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(m);m.Invoke(art,null);}
        [Test] public void ActualRootSurfaceReusesMaterialsWithoutChangingItsOwners()
        {
            int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();var owners=zone.GetAllEntities().ToArray();Native();
            Assert.False(FlatPixelStumpMaterialsCatalog.IsSupportedZone(zone));Assert.False(FlatPixelSiteAdmission.IsOrdinarySite(zone,BiomeType.Stump));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),View(fc).GroundTile.sprite.texture);
            Assert.That(View(fc).GroundTile.sprite.rect.x/16,Is.InRange(0,3));Assert.True(View(wc).BlocksMovement);
            Assert.False(View(fc).BlocksMovement);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());CollectionAssert.AreEquivalent(owners,zone.GetAllEntities());
        }
        [TestCase("Bush")][TestCase("Tree")][TestCase("TepuiWall")]
        public void ActualVegetationAndCliffHaveMatchingNativeReaders(string name)
        {
            Native();var e=name=="Bush"?bush:name=="Tree"?tree:wall;var c=zone.GetEntityCell(e);Assert.True(View(c).NativeForeground);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,c.X,c.Y,out var s,out _));Assert.AreSame(View(c).ForegroundTile.sprite,s);
        }
        [Test] public void BushUnderPlayerRetainsItsOwnReader()
        {
            Native();Assert.True(zone.AddEntity(content.Factory.CreateEntity("Player"),bc.X,bc.Y));Assert.True(View(bc).LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,bush,bc.X,bc.Y,out var s,out _));Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),s.texture);
        }
        [TestCase("name")][TestCase("poi")][TestCase("profile")][TestCase("absent")][TestCase("graph")][TestCase("tiles")][TestCase("pois")][TestCase("biome")]
        public void WarmCurrentAuthorityRevokesBothWorldAndReader(string change)
        {
            Native();var p=manager.WorldMap.GetPOI(3,3);switch(change){case "name":p.Name="another place";break;case "poi":p.Type=POIType.MerchantCamp;break;case "profile":p.Profile="foreign";break;case "absent":manager.WorldMap.SetPOI(3,3,null);break;case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;case "tiles":manager.WorldMap.Tiles=new BiomeType[1,1];break;case "pois":manager.WorldMap.POIs=new PointOfInterest[1,1];break;case "biome":manager.WorldMap.Tiles[3,3]=BiomeType.Spread;break;}
            if(world.TryResolvePresentation(zone,fc.X,fc.Y,out var g))Assert.False(g.LocalGround);
            if(world.TryResolvePresentation(zone,wc.X,wc.Y,out var v))Assert.False(v.NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,wall,wc.X,wc.Y,out var s,out _))Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),s.texture);
        }
        [Test] public void SavedLairAuthorityWinsEvenAfterRootLabelRestored()
        {
            Native();var p=manager.WorldMap.GetPOI(3,3);manager.WorldMap.SetPOI(3,3,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler"));LairStacks.Restore(manager,null);
            Assert.NotNull(LairStacks.Inspect(manager,"Overworld.3.3.0"));manager.WorldMap.SetPOI(3,3,p);Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
        }
        [TestCase("Overworld.03.3.0")][TestCase("Overworld.3.3.1")][TestCase("Overworld.3.3.2")][TestCase("Overworld.2.3.0")][TestCase("Overworld.4.4.0")][TestCase("foreign")]
        public void EndingAndOtherAddressesCannotBorrowThisInstance(string id)
        {zone.ZoneID=id;using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelStumpMaterialsCatalog(selector)){Admit(art);Assert.False(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));Assert.False(art.TryResolveForeground(zone,wc.X,wc.Y,wall,out _));}}
        [TestCase("hidden")][TestCase("memory")][TestCase("color")][TestCase("coating")][TestCase("duplicate")][TestCase("solid")]
        public void ChangedOrUndisclosedGroundRemainsTruthful(string fault)
        {Native();switch(fault){case "hidden":floor.GetPart<RenderPart>().Visible=false;break;case "memory":fc.IsVisible=false;break;case "color":floor.GetPart<RenderPart>().ColorString="&R";break;case "coating":zone.TileState.WriteCoating(fc.X,fc.Y,"oil",5);break;case "duplicate":zone.AddEntity(content.Factory.CreateEntity("TepuiStone"),fc.X,fc.Y);break;case "solid":floor.GetPart<PhysicsPart>().Solid=true;break;}if(world.TryResolvePresentation(zone,fc.X,fc.Y,out var v))Assert.False(v.LocalGround);}
        [TestCase(false)][TestCase(true)] public void HiddenCompetingGroundIsNotDisclosed(bool hidden)
        {Native();var e=content.Factory.CreateEntity("Grass");e.GetPart<RenderPart>().Visible=!hidden;Assert.True(zone.AddEntity(e,fc.X,fc.Y));Assert.AreEqual(hidden,View(fc).LocalGround);}
        [TestCase("hidden")][TestCase("memory")][TestCase("color")][TestCase("material")]
        public void WallStateRevokesLiveArt(string fault)
        {Native();switch(fault){case "hidden":wall.GetPart<RenderPart>().Visible=false;break;case "memory":wc.IsVisible=false;break;case "color":wall.GetPart<RenderPart>().ColorString="&R";break;case "material":wall.GetPart<MaterialPart>().MaterialID="Wood";break;}if(world.TryResolvePresentation(zone,wc.X,wc.Y,out var v))Assert.False(v.NativeForeground);}
        [Test] public void OptionalWorldArtMissingKeepsFallback()
        {using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null)){Assert.True(empty.TryResolvePresentation(zone,fc.X,fc.Y,out var v));Assert.False(v.LocalGround);}}
        [TestCase(false)][TestCase(true)] public void AtlasAndBushGroupsFailIndependently(bool missingAtlas)
        {using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelStumpMaterialsCatalog(missingAtlas?null:Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),null,missingAtlas?Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath):null,selector)){Admit(art);Assert.AreEqual(!missingAtlas,art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));Assert.AreEqual(missingAtlas,art.TryResolveForeground(zone,bc.X,bc.Y,bush,out _));}}
        [TestCase(RootSiteBuilder.FaceBlueprint)][TestCase("TheRooted")][TestCase("FoundingPlume")][TestCase("Rock")][TestCase("BrinePool")][TestCase("SteamVent")][TestCase("Tepuibone")][TestCase("StairsDown")]
        public void CompleteAtlasDoesNotPromoteUnrelatedOwners(string name)
        {using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelStumpMaterialsCatalog(selector)){Admit(art);var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.AreEqual(name,e.BlueprintName);Assert.True(zone.AddEntity(e,fc.X,fc.Y));Assert.False(art.TryResolveForeground(zone,fc.X,fc.Y,e,out _));}}
        [TestCase("RootFace",false)][TestCase("RootFace",true)]
        [TestCase("SeventhPosition",false)][TestCase("SeventhPosition",true)]
        [TestCase("HearthPatch",false)][TestCase("HearthPatch",true)]
        [TestCase("FoundingPlume",false)][TestCase("FoundingPlume",true)]
        public void EndingActionsNeverMasqueradeAsPlainStone(string part,bool onWall)
        {Native();var e=onWall?wall:floor;var c=onWall?wc:fc;switch(part){case "RootFace":e.AddPart(new RootFacePart());break;case "SeventhPosition":e.AddPart(new SeventhPositionPart());break;case "HearthPatch":e.AddPart(new HearthPatchPart());break;case "FoundingPlume":e.AddPart(new FoundingPlumePart());break;}var v=View(c);if(onWall)Assert.False(v.NativeForeground);else Assert.False(v.LocalGround);
         if(world.TryGetPresentationSubjectSprite(zone,e,c.X,c.Y,out var s,out _))Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),s.texture);}
        [Test] public void NativeEndingChamberHasNoRootSurfaceGrant()
        {Native();var lower=manager.GetZone("Overworld.3.3.1");foreach(var c in lower.Cells)c.Explored=c.IsVisible=true;using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelStumpMaterialsCatalog(selector)){Admit(art);foreach(var c in lower.Cells){Assert.False(art.TryResolveGround(lower,c.X,c.Y,out _,out _));foreach(var e in c.Objects)Assert.False(art.TryResolveForeground(lower,c.X,c.Y,e,out _));}}}
    }
}
