using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Paint = CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    [TestFixture(BiomeType.Spread)]
    [TestFixture(BiomeType.Beating)]
    [TestFixture(BiomeType.Sodden)]
    public sealed class FlatPixelWorldMapTests
    {
        readonly BiomeType biome;
        public FlatPixelWorldMapTests(BiomeType biome) { this.biome=biome; }
        string ResourcePath => biome==BiomeType.Spread ? FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath : biome==BiomeType.Beating ? FlatPixelBeatingMaterialsCatalog.ResourcePath : FlatPixelWorldMapCatalog.SoddenResourcePath;
        const int X = 40, Y = 13, WX = 10, WY = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Entity floor;
        FlatPixelGreyboxCatalog art;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            manager.WorldMap.Tiles[WX, WY] = biome; manager.WorldMap.SetPOI(WX, WY, null);
            zone = new Zone("WorldMap"); manager.SetActiveZone(zone);
            new WorldMapZoneBuilder(manager.WorldMap).BuildZone(zone, content.Factory, new System.Random(65));
            foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
            floor = zone.GetCell(X, Y).Objects[0]; art = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup() { art?.Dispose(); content?.Dispose(); }
        FlatPixelGreyboxCell On()
        {
            Assert.True(art.TryResolvePresentation(zone, X, Y, out var p));
            Assert.True(p.LocalGround, "Ordinary supported map terrain needs native ground");
            Assert.AreSame(Resources.Load<Texture2D>(ResourcePath), p.GroundTile.sprite.texture);
            Assert.AreEqual(new Vector2(16,24), p.GroundTile.sprite.rect.size);
            Assert.AreEqual(biome==BiomeType.Beating ? new Color(.45f,.45f,.45f,1f) : new Color(.65f,.65f,.65f,1f),p.GroundColor); return p;
        }
        FlatPixelWorldMapCatalog ForResource(Texture2D source) => biome==BiomeType.Spread
            ? new FlatPixelWorldMapCatalog(source,null,null) : biome==BiomeType.Beating
            ? new FlatPixelWorldMapCatalog(null,source,null) : new FlatPixelWorldMapCatalog(null,null,source);
        [Test] public void OneOptionalBiomeResourceDoesNotDependOnTheOther()
        {
            using(var catalog=ForResource(Resources.Load<Texture2D>(ResourcePath)))
            {
                Assert.True(catalog.TryResolveGround(zone,X,Y,out _,out _));
                manager.WorldMap.Tiles[WX,WY]=biome==BiomeType.Spread ? BiomeType.Beating : BiomeType.Spread;Refresh();
                Assert.False(catalog.TryResolveGround(zone,X,Y,out _,out _));
            }
        }
        void Off()
        {
            if (art.TryResolvePresentation(zone, X, Y, out var p)) Assert.False(p.LocalGround);
        }
        void Refresh() => new WorldMapZoneBuilder(manager.WorldMap).RefreshAppearance(zone);
        [Test] public void OrdinaryTerrainAndExactReaderUseSameNativeFrameWithoutMutatingWorld()
        {
            int version = zone.EntityVersion; string state = zone.TileState.ToSaveString();
            var p = On(); Assert.AreSame(floor,p.Owner); Assert.IsNull(p.ForegroundTile); Assert.False(p.BlocksMovement);
            Assert.True(art.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var sprite,out var tint));
            Assert.AreSame(p.GroundTile.sprite,sprite); Assert.AreEqual(biome==BiomeType.Beating ? new Color(.45f,.45f,.45f,1f) : new Color(.65f,.65f,.65f,1f),tint);
            Assert.AreEqual(tint,p.GroundColor);
            Assert.AreEqual(version,zone.EntityVersion); Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void HiddenSinkholeMatchesOrdinaryTerrainUntilDiscovery()
        {
            var original = On(); manager.WorldMap.SetPOI(WX,WY,new PointOfInterest(POIType.Sinkhole,"secret"));
            manager.WorldMap.Visited[WX,WY] = false; Refresh();
            Assert.AreSame(original.GroundTile,On().GroundTile);
            Assert.AreEqual(WorldMapZoneBuilder.GetBiomeRender(biome).displayName,floor.GetPart<RenderPart>().DisplayName);
            manager.WorldMap.MarkVisited(WX,WY); Refresh(); Assert.AreSame(original.GroundTile,On().GroundTile);
            Assert.NotNull(On().ForegroundTile);
            Assert.AreEqual("o",floor.GetPart<RenderPart>().RenderString);
        }
        [TestCase(POIType.Village)] [TestCase(POIType.Lair)] [TestCase(POIType.MerchantCamp)]
        public void DisclosedSitesKeepTheirMarkers(POIType type)
        {
            On(); manager.WorldMap.SetPOI(WX,WY,new PointOfInterest(type,"site")); Refresh(); var p = On(); Assert.NotNull(p.ForegroundTile);
            Assert.True(art.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var marker,out _));
            Assert.AreSame(p.ForegroundTile.sprite,marker);
            Assert.AreNotSame(Resources.Load<Texture2D>(ResourcePath),p.ForegroundTile.sprite.texture);
        }
        [TestCase("hidden")] [TestCase("memory")] [TestCase("unexplored")] [TestCase("coordinates")]
        [TestCase("duplicate-owner")] [TestCase("duplicate-part")] [TestCase("variant")] [TestCase("color")]
        [TestCase("glyph")] [TestCase("solid")] [TestCase("inventory")] [TestCase("graph")]
        [TestCase("biome")] [TestCase("map")] [TestCase("state")] [TestCase("name")]
        public void ChangedAuthorityOrMeaningDeclines(string change)
        {
            On();
            switch(change)
            {
                case "hidden": floor.GetPart<RenderPart>().Visible=false; break;
                case "memory": zone.GetCell(X,Y).IsVisible=false; break;
                case "unexplored": zone.GetCell(X,Y).Explored=false; break;
                case "coordinates": floor.GetPart<WorldMapCellPart>().WorldX++; break;
                case "duplicate-owner": var copy=new Entity{BlueprintName="WorldMapCell"};copy.AddPart(new RenderPart());copy.AddPart(new WorldMapCellPart());zone.AddEntity(copy,X,Y);break;
                case "duplicate-part": floor.AddPart(new WorldMapCellPart{WorldX=WX,WorldY=WY});break;
                case "variant": floor.GetPart<RenderPart>().VisualVariant="foreign";break;
                case "color": floor.GetPart<RenderPart>().ColorString="&R";break;
                case "glyph": floor.GetPart<RenderPart>().RenderString="!";break;
                case "solid": floor.GetPart<PhysicsPart>().Solid=true;break;
                case "inventory": floor.GetPart<PhysicsPart>().InInventory=new Entity();break;
                case "graph": manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "biome": manager.WorldMap.Tiles[WX,WY]=BiomeType.Stump;break;
                case "map": manager.WorldMap.POIs=new PointOfInterest[1,1];break;
                case "state": zone.TileState.AddHeat(X,Y,1);break;
                case "name": floor.GetPart<RenderPart>().DisplayName="foreign";break;
            }
            Off();
        }
        [Test] public void CoveredPlayerKeepsGroundAndIncrementalFullRepaintParity()
        {
            using(var paint=new Paint(art,zone))
            {
                paint.Step(); On(); var ground=paint.GroundAt(X,Y);
                var player=content.Factory.CreateEntity("Player");zone.AddEntity(player,X,Y);paint.Dirty(X,Y);paint.Step();
                Assert.AreSame(ground,paint.GroundAt(X,Y));Assert.AreSame(player,On().Owner);Assert.NotNull(paint.At(X,Y));
                Assert.True(art.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var sprite,out _));Assert.AreSame(ground.sprite,sprite);
                zone.RemoveEntity(player);paint.Dirty(X,Y);paint.Step();Assert.AreSame(ground,paint.GroundAt(X,Y));
                paint.Renderer.MarkDirty("map full parity");paint.Step();Assert.AreSame(ground,paint.GroundAt(X,Y));
                paint.Renderer.FlatPixelModeEnabled=false;paint.Step();Assert.False(paint.Renderer.FlatPixelModeActive);
            }
        }
        [TestCase(false)] [TestCase(true)] public void MissingOrMalformedResourceDeclinesWithoutReload(bool malformed)
        {
            On(); Texture2D source = malformed ? new Texture2D(16,24) : null;
            var replacement = ForResource(source);
            var field=typeof(FlatPixelGreyboxCatalog).GetField("_worldMapArt",BindingFlags.NonPublic|BindingFlags.Instance);
            ((FlatPixelWorldMapCatalog)field.GetValue(art)).Dispose(); field.SetValue(art,replacement);
            Off(); if(source!=null)Object.DestroyImmediate(source);
        }
        [Test] public void DisposalOwnsOnlyViewsAndIsIdempotent()
        {
            var source=Resources.Load<Texture2D>(ResourcePath);
            var catalog=ForResource(source);
            Assert.True(catalog.TryResolveGround(zone,X,Y,out var tile,out _));var sprite=tile.sprite;
            catalog.Dispose();catalog.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(source!=null);
            Assert.False(catalog.TryResolveGround(zone,X,Y,out _,out _));
        }
        [Test] public void HiddenCompetitorDoesNotRevealItselfThroughGround()
        {
            var original=On().GroundTile;var hidden=new Entity{BlueprintName="WorldMapCell"};
            hidden.AddPart(new RenderPart{Visible=false});hidden.AddPart(new WorldMapCellPart());zone.AddEntity(hidden,X,Y);
            Assert.AreSame(original,On().GroundTile);hidden.GetPart<RenderPart>().Visible=true;Off();
            zone.RemoveEntity(hidden);Assert.AreSame(original,On().GroundTile);
        }
        [Test] public void WarmGroundResolutionDoesNotAllocate()
        {
            using(var catalog=new FlatPixelWorldMapCatalog())
            {
                var probe=new byte[32][];long start=System.GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<probe.Length;i++)probe[i]=new byte[64];
                long control=System.GC.GetAllocatedBytesForCurrentThread()-start;System.GC.KeepAlive(probe);
                if(control<=0)Assert.Inconclusive("Allocation counter cannot detect positive control.");
                for(int i=0;i<64;i++)catalog.TryResolveGround(zone,X,Y,out _,out _);
                bool all=true;start=System.GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<1024;i++)all &= catalog.TryResolveGround(zone,X,Y,out _,out _);
                long bytes=System.GC.GetAllocatedBytesForCurrentThread()-start;
                Assert.True(all);Assert.AreEqual(0,bytes);
            }
        }
        [Test] public void SoddenUsesOnlyBanklessFramesWithoutReadingNeighbors()
        {
            if(biome!=BiomeType.Sodden)return;
            var p=On();Assert.AreEqual(0,(int)(p.GroundTile.sprite.rect.x/16)%16);
            var prior=p.GroundTile;zone.GetCell(X+1,Y).IsVisible=false;
            manager.WorldMap.Tiles[WX+1,WY]=BiomeType.Beating;
            Assert.AreSame(prior,On().GroundTile);
        }
        [Test] public void OtherBiomesAndBorderRemainOutsidePilot()
        {
            On(); manager.WorldMap.Tiles[WX,WY]=BiomeType.Stump;Refresh();Off();
            Assert.True(art.TryResolvePresentation(zone,29,13,out var p));Assert.False(p.LocalGround);
        }
    }
}
