using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Actual baseline has long '=' bands. Admission must replace only the real,
    // non-solid ledge; a shared blueprint name cannot grant neighboring sites art.
    public sealed class FlatPixelStillleafLedgeTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Entity ledge; Cell cell;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=manager.GetZone("Overworld.2.4.1");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            cell=zone.Cells.Cast<Cell>().First(c=>c.Objects.Any(e=>e.BlueprintName=="DescentLedge")
                &&c.Objects.All(e=>e.BlueprintName=="SandstoneFloor"||e.BlueprintName=="DescentLedge"));
            ledge=cell.Objects.First(e=>e.BlueprintName=="DescentLedge");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,cell.X,cell.Y,out var v));return v;}
        void Native(){Assert.AreSame(ledge,View().Owner);Assert.True(View().NativeForeground,"Actual Stillleaf ledge remains ASCII");}
        [Test] public void AuthoredLedgesReuseApprovedNonSolidArtWithoutChangingTheWorld()
        {
            int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();Native();var v=View();
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),v.ForegroundTile.sprite.texture);
            Assert.That(v.ForegroundTile.sprite.rect.x/16,Is.InRange(10,11));Assert.True(v.LocalGround);
            Assert.False(v.BlocksMovement);Assert.False(ledge.GetPart<PhysicsPart>().Solid);
            Assert.AreEqual(233,zone.Cells.Cast<Cell>().Sum(c=>c.Objects.Count(e=>e.BlueprintName=="DescentLedge")));
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
            Assert.AreSame(v.ForegroundTile,View().ForegroundTile,"Reuse warm cached tile");
        }
        [Test] public void WorldAndCoveredExamineKeepTheSameLedgeIdentity()
        {
            Native();var sprite=View().ForegroundTile.sprite;
            Assert.True(world.TryGetPresentationSubjectSprite(zone,ledge,cell.X,cell.Y,out var shown,out _));Assert.AreSame(sprite,shown);
            var p=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(p,cell.X,cell.Y));
            Assert.AreSame(p,View().Owner);Assert.True(View().LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,ledge,cell.X,cell.Y,out shown,out _));Assert.AreSame(sprite,shown);
        }
        [TestCase("name")][TestCase("poi")][TestCase("profile")][TestCase("absent")][TestCase("graph")][TestCase("map")]
        public void WarmCurrentAuthorityChangesRevokeLedgeArt(string change)
        {
            Native();var p=manager.WorldMap.GetPOI(2,4);
            switch(change){case "name":p.Profile=null;p.Name="different site";break;case "poi":p.Type=POIType.MerchantCamp;break;
                case "profile":p.Profile=SinkholeSites.FoundingVillageProfile;break;case "absent":manager.WorldMap.SetPOI(2,4,null);break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;case "map":manager.WorldMap.Tiles=new BiomeType[1,1];break;}
            Refuses();
        }
        [Test] public void PersistedLibraryProfilePermitsStoryRename()
        {
            Native();var p=manager.WorldMap.GetPOI(2,4);p.Profile=SinkholeSites.SealedLibraryProfile;p.Name="renamed archive";Native();
            p.Profile=SinkholeSites.FoundingVillageProfile;Refuses();
        }
        [Test] public void SavedLairAuthorityWinsOverRestoredStillleafLabel()
        {
            Native();var p=manager.WorldMap.GetPOI(2,4);manager.WorldMap.SetPOI(2,4,new PointOfInterest(POIType.Lair,"retained",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(2,4,p);Refuses();
        }
        [TestCase("color")][TestCase("solid")][TestCase("visual")][TestCase("coating")][TestCase("hidden")][TestCase("memory")]
        public void ChangedMeaningOrDisclosureFallsBack(string change)
        {
            Native();switch(change){case "color":ledge.GetPart<RenderPart>().ColorString="&R";break;
                case "solid":ledge.GetPart<PhysicsPart>().Solid=true;break;case "visual":ledge.GetPart<RenderPart>().VisualID="unknown";break;
                case "coating":zone.TileState.WriteCoating(cell.X,cell.Y,"oil",5);break;
                case "hidden":ledge.GetPart<RenderPart>().Visible=false;break;case "memory":cell.IsVisible=false;break;}Refuses();
        }
        void Refuses()
        {
            if(world.TryResolvePresentation(zone,cell.X,cell.Y,out var v)&&ReferenceEquals(v.Owner,ledge))Assert.False(v.NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,ledge,cell.X,cell.Y,out var s,out _))
                Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),s.texture);
        }
        [Test] public void ForeignOrRemovedReaderCannotBorrowLedgeArt()
        {
            Native();var clone=content.Factory.CreateEntity("DescentLedge");
            if(world.TryGetPresentationSubjectSprite(zone,clone,cell.X,cell.Y,out var s,out _))Assert.AreNotSame(View().ForegroundTile.sprite,s);
            Assert.True(zone.RemoveEntity(ledge));Refuses();
        }
        [TestCase("Overworld.02.4.1")][TestCase("Overworld.2.4.0")][TestCase("Overworld.2.4.2")][TestCase("Overworld.2.7.1")]
        public void EqualBlueprintDoesNotExpandThisCatalog(string id)
        {
            using(var selector=new FlatPixelFallbackCatalog())using(var art=Art(selector))
            {
                zone=new Zone(id);manager.SetActiveZone(zone);cell=zone.GetCell(10,10);cell.Explored=cell.IsVisible=true;
                ledge=content.Factory.CreateEntity("DescentLedge");
                Assert.True(zone.AddEntity(ledge,10,10));Assert.False(art.TryResolveForeground(zone,10,10,ledge,out _));
            }
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedOptionalAtlasDeclines(bool malformed)
        {
            var bad=malformed?new Texture2D(16,24):null;
            try{using(var selector=new FlatPixelFallbackCatalog())using(var art=new FlatPixelStumpMaterialsCatalog(bad,null,null,selector))
            {Admit(art);Assert.False(art.TryResolveForeground(zone,cell.X,cell.Y,ledge,out _));}}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [TestCase(false)][TestCase(true)] public void CoveredReaderRevokesAndRecoversWithDisclosure(bool memory)
        {
            Native();var sprite=View().ForegroundTile.sprite;
            Assert.True(zone.AddEntity(content.Factory.CreateEntity("Player"),cell.X,cell.Y));
            using(var selector=new FlatPixelFallbackCatalog())using(var art=Art(selector))
            {
                Assert.True(art.TryGetSubjectSprite(zone,ledge,cell.X,cell.Y,out var s));Assert.AreSame(sprite.texture,s.texture);
                if(memory)cell.IsVisible=false;else ledge.GetPart<RenderPart>().Visible=false;
                Assert.False(art.TryGetSubjectSprite(zone,ledge,cell.X,cell.Y,out _));
                cell.IsVisible=true;ledge.GetPart<RenderPart>().Visible=true;
                Assert.True(art.TryGetSubjectSprite(zone,ledge,cell.X,cell.Y,out var recovered));Assert.AreSame(s,recovered);
            }
        }
        [Test] public void PilotCannotEnableOtherStumpFamiliesAndDisposesOwnedViewsOnly()
        {
            using(var selector=new FlatPixelFallbackCatalog())using(var art=Art(selector))
            {
                Assert.True(art.TryResolveForeground(zone,cell.X,cell.Y,ledge,out var tile));var texture=tile.sprite.texture;
                Assert.True(zone.RemoveEntity(ledge));foreach(var name in new[]{"TepuiWall","GrainRidge","StoneDome","Tree","Bush","SprayPool","TepuiStone"})
                {var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,cell.X,cell.Y));
                    Assert.False(art.TryResolveForeground(zone,cell.X,cell.Y,e,out _),name);
                    Assert.False(art.TryResolveGround(zone,cell.X,cell.Y,out _,out _),name);
                    Assert.False(art.TryResolveWaterGround(zone,cell.X,cell.Y,out _,out _),name);Assert.True(zone.RemoveEntity(e));}
                Assert.True(zone.AddEntity(ledge,cell.X,cell.Y));art.Dispose();Assert.False(art.TryResolveForeground(zone,cell.X,cell.Y,ledge,out _));Assert.True(texture!=null);
            }
        }
        static FlatPixelStumpMaterialsCatalog Art(FlatPixelFallbackCatalog selector)
        {var art=new FlatPixelStumpMaterialsCatalog(selector);Admit(art);return art;}
        static void Admit(FlatPixelStumpMaterialsCatalog art)
        {var m=typeof(FlatPixelStumpMaterialsCatalog).GetMethod("AdmittingStillleafLedges",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(m);m.Invoke(art,null);}
    }
}
