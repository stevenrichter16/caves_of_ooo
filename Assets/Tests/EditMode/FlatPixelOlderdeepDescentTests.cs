using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    [TestFixture(1)][TestFixture(2)]
    public sealed class FlatPixelOlderdeepDescentTests
    {
        readonly int depth; public FlatPixelOlderdeepDescentTests(int d){depth=d;}
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Entity floor, wall; Cell fc,wc;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=manager.GetZone("Overworld.4.6."+depth);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();
            fc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="StoneFloor");floor=fc.Objects[0];
            wc=zone.Cells.Cast<Cell>().First(c=>c.Objects.Any(e=>e.BlueprintName=="SandstoneWall"));wall=wc.Objects.First(e=>e.BlueprintName=="SandstoneWall");
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(Cell c){Assert.True(world.TryResolvePresentation(zone,c.X,c.Y,out var v));return v;}
        void Native()
        {
            Assert.True(View(fc).LocalGround,"Olderdeep stone floor remains glyphs");
            Assert.True(View(wc).NativeForeground,"Olderdeep sandstone wall remains glyphs");
        }
        [Test] public void ActualAuthoredDescentReusesApprovedMaterialsAndPreservesGeometry()
        {
            int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();Native();
            Assert.False(FlatPixelShallowCavesCatalog.IsSupportedZone(zone),"ordinary cave scope stays separate");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath),View(fc).GroundTile.sprite.texture);
            Assert.That(View(fc).GroundTile.sprite.rect.x/16,Is.InRange(4,7));
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
        [Test] public void StoryRenameRetainsFoundingProfileAuthority()
        {Native();manager.WorldMap.GetPOI(4,6).Name="renamed site";Native();}
        [TestCase("poi")] [TestCase("profile")] [TestCase("absent")]
        [TestCase("graph")] [TestCase("tiles")] [TestCase("pois")] [TestCase("biome")] [TestCase("unknown-profile")]
        public void WarmAdmissionRevokesWhenCurrentAuthorityChanges(string change)
        {
            Native();var p=manager.WorldMap.GetPOI(4,6);
            switch(change)
            {
                case "poi":p.Type=POIType.MerchantCamp;break;
                case "profile":p.Profile=SinkholeSites.SealedLibraryProfile;break;
                case "absent":manager.WorldMap.SetPOI(4,6,null);break;
                case "unknown-profile":p.Profile="Unrelated";break;case "biome":manager.WorldMap.Tiles[4,6]=BiomeType.Spread;break;case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "tiles":manager.WorldMap.Tiles=new BiomeType[1,1];break;
                case "pois":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
            }
            if(world.TryResolvePresentation(zone,fc.X,fc.Y,out var ground))Assert.False(ground.LocalGround);
            if(world.TryResolvePresentation(zone,wc.X,wc.Y,out var body))Assert.False(body.NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,wall,wc.X,wc.Y,out var sprite,out _))
                Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),sprite.texture);
        }
        [TestCase("Overworld.04.6.1")] [TestCase("Overworld.4.6.0")] [TestCase("Overworld.4.6.3")]
        [TestCase("Overworld.2.4.1")]
        public void EqualMaterialNamesCannotExpandPilotAddress(string id)
        {
            Native();zone=new Zone(id);manager.SetActiveZone(zone);fc=zone.GetCell(10,10);fc.Explored=fc.IsVisible=true;
            floor=content.Factory.CreateEntity("StoneFloor");zone.AddEntity(floor,10,10);
            // Other independently admitted site catalogs may legitimately render this address.
            // Pin this pilot's boundary directly rather than requiring every family to stay glyphs.
            using(var selector=new FlatPixelFallbackCatalog())using(var art=StoneArt(selector,Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
            {
                var method=typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingOlderdeepDescent",BindingFlags.Instance|BindingFlags.NonPublic);
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
        [Test] public void FoundingProfileIsRequiredAndRestoresBothMaterials()
        {
            Native();var p=manager.WorldMap.GetPOI(4,6);p.Profile=null;
            Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
            p.Profile=SinkholeSites.FoundingVillageProfile;Native();
        }
        [Test] public void SpecialDescentLedgesKeepTheirOwnTerrainMeaning()
        {
            Native();var ledge=content.Factory.CreateEntity("DescentLedge");Assert.NotNull(ledge);
            Assert.True(zone.AddEntity(ledge,fc.X,fc.Y));Assert.True(View(fc).LocalGround);
            Assert.AreSame(ledge,View(fc).Owner);Assert.AreEqual(depth==1,View(fc).NativeForeground);
            Assert.False(View(fc).BlocksMovement);
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
            Native();var p=manager.WorldMap.GetPOI(4,6);
            manager.WorldMap.SetPOI(4,6,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.4.6.0"));
            manager.WorldMap.SetPOI(4,6,p);Assert.False(View(fc).LocalGround);Assert.False(View(wc).NativeForeground);
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
            if(world.TryGetPresentationSubjectSprite(zone,floor,fc.X,fc.Y,out s,out _))Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath),s.texture);
        }
        [TestCase(false)][TestCase(true)] public void MissingGroupsFailIndependently(bool missingFloor)
        {
            Native();using(var selector=new FlatPixelFallbackCatalog())
            using(var art=StoneArt(selector,missingFloor?Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath):null,missingFloor?null:Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
            {
                Admit(art);Assert.AreEqual(!missingFloor,art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));
                Assert.AreEqual(missingFloor,art.TryResolveForeground(zone,wc.X,wc.Y,wall,out _));
            }
        }
        [Test] public void AvailableOtherGroupsCannotExpandStructuralPilot()
        {
            Native();using(var selector=new FlatPixelFallbackCatalog())using(var art=StoneArt(selector,Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
            {
                Admit(art);foreach(var name in new[]{"StairsDown","StairsUp","Rubble","Stalagmite","DescentLedge","RopeAnchor","SubstrateVault"})
                {
                    var other=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(other,fc.X,fc.Y));
                    Assert.False(art.TryResolveForeground(zone,fc.X,fc.Y,other,out _),name);Assert.True(zone.RemoveEntity(other));
                }
                Assert.True(zone.RemoveEntity(floor));Assert.False(art.TryResolveCellBackdrop(zone,fc.X,fc.Y,out _,out _));
            }
        }
        [Test] public void CoveredRubbleCannotBorrowArtEvenWhenResourcesAreAvailable()
        {
            Native();var rubble=content.Factory.CreateEntity("Rubble");Assert.True(zone.AddEntity(rubble,fc.X,fc.Y));
            Assert.True(zone.AddEntity(content.Factory.CreateEntity("Player"),fc.X,fc.Y));
            using(var selector=new FlatPixelFallbackCatalog())using(var art=StoneArt(selector,Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
            {Admit(art);Assert.False(art.TryGetSubjectSprite(zone,rubble,fc.X,fc.Y,out _));}
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedWallDoesNotBorrowOtherMaterials(bool malformed)
        {
            var bad=malformed?new Texture2D(16,24):null;
            try{using(var selector=new FlatPixelFallbackCatalog())using(var art=StoneArt(selector,bad,Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
                {Admit(art);Assert.False(art.TryResolveForeground(zone,wc.X,wc.Y,wall,out _));Assert.True(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));}}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [TestCase("StoneFloor",false)][TestCase("StoneFloor",true)]
        [TestCase("SandstoneFloor",false)][TestCase("SandstoneFloor",true)]
        public void DuplicateAndMixedFloorsRefuseUnlessCompetitorHidden(string name,bool hidden)
        {
            Native();var other=content.Factory.CreateEntity(name);other.GetPart<RenderPart>().Visible=!hidden;
            Assert.True(zone.AddEntity(other,fc.X,fc.Y));Assert.AreEqual(hidden,View(fc).LocalGround);
        }
        [Test] public void SyntheticSandstoneConnectorUsesItsOwnPaletteInsteadOfStoneFloorArt()
        {
            Native();Assert.True(zone.RemoveEntity(floor));floor=content.Factory.CreateEntity("SandstoneFloor");Assert.True(zone.AddEntity(floor,fc.X,fc.Y));
            Assert.True(View(fc).LocalGround);Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),View(fc).GroundTile.sprite.texture);
            Assert.That(View(fc).GroundTile.sprite.rect.x/16,Is.InRange(12,15));
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedSandstoneArtKeepsStoneIndependent(bool malformed)
        {
            var bad=malformed?new Texture2D(16,24):null;
            try{using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelShallowCavesCatalog(
                Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),bad,null,null,null,null,selector,
                Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
            {Admit(art);Assert.True(art.TryResolveGround(zone,fc.X,fc.Y,out var tile,out var actual));Assert.AreSame(floor,actual);
             Assert.AreSame(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath),tile.sprite.texture);
             Assert.True(zone.RemoveEntity(floor));Assert.True(zone.AddEntity(content.Factory.CreateEntity("SandstoneFloor"),fc.X,fc.Y));
             Assert.False(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));}}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedStoneArtDoesNotBorrowSandstone(bool malformed)
        {
            var bad=malformed?new Texture2D(16,24):null;
            try{using(var selector=new FlatPixelFallbackCatalog())using(var art=StoneArt(selector,Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),bad))
            {Admit(art);Assert.False(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));Assert.True(zone.RemoveEntity(floor));
             var sand=content.Factory.CreateEntity("SandstoneFloor");Assert.True(zone.AddEntity(sand,fc.X,fc.Y));Assert.True(art.TryResolveGround(zone,fc.X,fc.Y,out var tile,out _));Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),tile.sprite.texture);}}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [TestCase("glyph")][TestCase("background")][TestCase("layer")][TestCase("variants")][TestCase("solid")]
        public void ChangedStoneFloorMeaningFallsBack(string fault)
        {
            Native();var r=floor.GetPart<RenderPart>();switch(fault){case "glyph":r.RenderString="!";break;case "background":r.BackgroundColor="^R";break;case "layer":r.RenderLayer=2;break;case "variants":r.GlyphVariants="..";break;case "solid":floor.GetPart<PhysicsPart>().Solid=true;break;}
            Assert.False(View(fc).LocalGround);
        }
        [Test] public void StoneOptionalGroupRequiresOlderdeepAdmissionAndDisposesOnlyViews()
        {
            Native();using(var selector=new FlatPixelFallbackCatalog())using(var art=StoneArt(selector,Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)))
            {Assert.False(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));Admit(art);Assert.True(art.TryResolveGround(zone,fc.X,fc.Y,out var tile,out _));var texture=tile.sprite.texture;art.Dispose();Assert.False(art.TryResolveGround(zone,fc.X,fc.Y,out _,out _));Assert.True(texture!=null);}
        }
        static FlatPixelShallowCavesCatalog StoneArt(FlatPixelFallbackCatalog selector,Texture2D walls,Texture2D stone)
        {
            var ctor=typeof(FlatPixelShallowCavesCatalog).GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(FlatPixelFallbackCatalog),typeof(Texture2D)});Assert.NotNull(ctor);
            return (FlatPixelShallowCavesCatalog)ctor.Invoke(new object[]{walls,Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),Resources.Load<Texture2D>("Sprites/Environment/rubble"),Resources.Load<Texture2D>("Sprites/Environment/stalagmite"),Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),selector,stone});
        }
        static void Admit(FlatPixelShallowCavesCatalog art)
        {
            var method=typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingOlderdeepDescent",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(method);method.Invoke(art,null);
        }
    }
}
