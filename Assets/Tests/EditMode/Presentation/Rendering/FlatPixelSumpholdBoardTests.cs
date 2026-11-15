using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSumpholdBoardTests
    {
        const string Warm = "Sprites/Live16x24/sumphold-boards";
        const string Original = "Sprites/Live16x24/sodden-travel";
        const int X = 41, Y = 15;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelFallbackCatalog selector;
        FlatPixelSettlementGroundCatalog art;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = new Zone(SumpholdCompositionPlan.ZoneID); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) c.Explored = c.IsVisible = true;
            selector = new FlatPixelFallbackCatalog(); art = new FlatPixelSettlementGroundCatalog(selector);
        }
        [TearDown] public void Cleanup() { art?.Dispose(); selector?.Dispose(); content?.Dispose(); }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, x, y)); return e; }
        Tile Board(Entity owner, bool warm = true)
        {
            Assert.True(art.TryResolveForeground(zone,X,Y,owner,out var tile));
            Assert.AreSame(Resources.Load<Texture2D>(warm ? Warm : Original),tile.sprite.texture);
            Assert.AreEqual(new Vector2(16,24),tile.sprite.rect.size);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType); return tile;
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        [TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)]
        public void WarmBoardPreservesAllCardinalMasksAndIsolatedReader(int mask)
        {
            Add("Floor"); var owner=Add("Duckboard");
            var offsets=new[]{new Vector2Int(0,-1),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,0)};
            for(int i=0;i<4;i++) if((mask & (1<<i))!=0) Add("Duckboard",X+offsets[i].x,Y+offsets[i].y);
            Add("Duckboard",X+1,Y+1);
            string state=zone.TileState.ToSaveString(); int version=zone.EntityVersion;
            var tile=Board(owner); Assert.AreEqual(mask,(int)tile.sprite.rect.x/16);
            Assert.True(art.TryGetSubjectSprite(zone,owner,X,Y,out var portrait));
            Assert.AreSame(tile.sprite.texture,portrait.texture); Assert.AreEqual(0,portrait.rect.x);
            using(var world=new FlatPixelGreyboxCatalog())
            {
                Assert.True(world.TryResolvePresentation(zone,X,Y,out var view));
                Assert.AreSame(owner,view.Owner); Assert.AreSame(tile.sprite.texture,view.ForegroundTile.sprite.texture);
                Assert.AreEqual(tile.sprite.rect,view.ForegroundTile.sprite.rect); Assert.False(view.BlocksMovement);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var reader,out _));
                Assert.AreSame(tile.sprite.texture,reader.texture); Assert.AreEqual(0,reader.rect.x);
            }
            Assert.AreEqual(state,zone.TileState.ToSaveString()); Assert.AreEqual(version,zone.EntityVersion);
            Assert.AreEqual(8,owner.GetPart<DestructiblePart>().HP);
        }
        [TestCase("profile")][TestCase("biome")][TestCase("name")][TestCase("type")][TestCase("detached")][TestCase("cache")][TestCase("lair")]
        public void CurrentSumpholdAuthorityIsRequired(string loss)
        {
            var b=Add("Duckboard"); Board(b); var poi=manager.WorldMap.GetPOI(15,6);
            if(loss=="profile")poi.Profile="other";
            if(loss=="biome")manager.WorldMap.Tiles[15,6]=BiomeType.Sodden;
            if(loss=="name")poi.Name="other";
            if(loss=="type")manager.WorldMap.SetPOI(15,6,new PointOfInterest(POIType.Lair,"Sumphold",profile:"Boatyard"));
            if(loss=="detached")manager.CachedZones.Remove(zone.ZoneID);
            if(loss=="cache")manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
            if(loss=="lair") { manager.WorldMap.SetPOI(15,6,new PointOfInterest(POIType.Lair,"legacy",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(15,6,poi); }
            if(loss=="profile" || loss=="biome")
            { Board(b,false); Assert.True(art.TryGetSubjectSprite(zone,b,X,Y,out var p)); Assert.AreSame(Resources.Load<Texture2D>(Original),p.texture); }
            else { Assert.False(art.TryResolveForeground(zone,X,Y,b,out _)); Assert.False(art.TryGetSubjectSprite(zone,b,X,Y,out _)); }
        }
        [Test] public void AnotherAuthoredBoatyardRetainsOriginalBoards()
        {
            zone=new Zone("Overworld.13.7.0");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            manager.WorldMap.GetPOI(13,7).Profile="Boatyard"; var b=Add("Duckboard"); Board(b,false);
            Assert.True(art.TryGetSubjectSprite(zone,b,X,Y,out var p)); Assert.AreSame(Resources.Load<Texture2D>(Original),p.texture);
        }
        [TestCase("hidden-cell")][TestCase("hidden-owner")][TestCase("destroyed")][TestCase("frozen")][TestCase("duplicate")][TestCase("variant")]
        public void InvalidBoardKeepsExistingFallbackAndReaderRefusal(string fault)
        {
            var b=Add("Duckboard");Board(b);
            if(fault=="hidden-cell")zone.GetCell(X,Y).IsVisible=false;
            if(fault=="hidden-owner")b.GetPart<RenderPart>().Visible=false;
            if(fault=="destroyed")b.GetPart<DestructiblePart>().HP=0;
            if(fault=="frozen")b.GetPart<ThermalPart>().Temperature=b.GetPart<ThermalPart>().FreezeTemperature;
            if(fault=="duplicate")Add("Duckboard");
            if(fault=="variant")b.GetPart<RenderPart>().VisualVariant="foreign";
            Assert.False(art.TryResolveForeground(zone,X,Y,b,out _)); Assert.False(art.TryGetSubjectSprite(zone,b,X,Y,out _));
        }
        [Test] public void CoveredNeighborStillConnectsButUndisclosedNeighborDoesNot()
        {
            var b=Add("Duckboard"); Add("Duckboard",X+1,Y); Add("Player",X+1,Y);
            Assert.AreEqual(2,(int)Board(b).sprite.rect.x/16);
            zone.GetCell(X+1,Y).IsVisible=false; Assert.AreEqual(0,(int)Board(b).sprite.rect.x/16);
        }
        [Test] public void CoveredReaderRetainsBoardIdentityWithoutTakingActorForeground()
        {
            Add("Floor"); var b=Add("Duckboard"); Board(b); var actor=Add("Player");
            Assert.False(art.TryResolveForeground(zone,X,Y,b,out _));
            Assert.True(art.TryGetSubjectSprite(zone,b,X,Y,out var p)); Assert.AreSame(Resources.Load<Texture2D>(Warm),p.texture);
            using(var world=new FlatPixelGreyboxCatalog())
            { Assert.True(world.TryResolvePresentation(zone,X,Y,out var v)); Assert.AreSame(actor,v.Owner); }
            zone.RemoveEntity(b); Assert.False(art.TryGetSubjectSprite(zone,b,X,Y,out _));
        }
        [Test] public void PartiallyDamagedBoardRetainsExistingNativeWorldAndReader()
        {
            var b=Add("Duckboard");b.GetPart<DestructiblePart>().HP=7;Board(b);
            Assert.True(art.TryGetSubjectSprite(zone,b,X,Y,out var p));Assert.AreSame(Resources.Load<Texture2D>(Warm),p.texture);
        }
        FlatPixelSettlementGroundCatalog Inject(Texture2D optional, Texture2D mandatory)
            => new FlatPixelSettlementGroundCatalog(null,null,null,null,null,null,null,mandatory,null,null,null,null,optional,selector);
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedOptionalArtRetainsOriginalWorldAndReader(bool malformed)
        {
            var b=Add("Duckboard"); var bad=malformed ? new Texture2D(16,24) : null;
            try
            {
                art.Dispose(); art=Inject(bad,Resources.Load<Texture2D>(Original)); Board(b,false);
                Assert.True(art.TryGetSubjectSprite(zone,b,X,Y,out var p)); Assert.AreSame(Resources.Load<Texture2D>(Original),p.texture);
            }
            finally { if(bad!=null)UnityEngine.Object.DestroyImmediate(bad); }
        }
        [Test] public void OptionalArtCannotSupplyMissingRequiredFixturesAndBorrowsOnlyTextures()
        {
            var b=Add("Duckboard"); var texture=Resources.Load<Texture2D>(Warm);
            art.Dispose();art=Inject(texture,null);
            Assert.False(art.TryResolveForeground(zone,X,Y,b,out _));Assert.False(art.TryGetSubjectSprite(zone,b,X,Y,out _));
            art.Dispose();art=Inject(texture,Resources.Load<Texture2D>(Original)); var t=Board(b);var s=t.sprite;
            art.Dispose();Assert.True(t==null);Assert.True(s==null);Assert.True(texture!=null);Assert.True(selector.TryResolve(zone,X,Y,out _));
            Assert.False(art.TryGetSubjectSprite(zone,b,X,Y,out _));
        }
    }
}
