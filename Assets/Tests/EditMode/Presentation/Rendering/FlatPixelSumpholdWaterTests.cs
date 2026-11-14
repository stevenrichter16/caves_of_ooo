using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSumpholdWaterTests
    {
        const string Quiet = "Sprites/Live16x24/sodden-flood-water";
        const string Original = "Sprites/Live16x24/opening-ground";
        const int X = 33, Y = 15;
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
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(15, 6));
            selector = new FlatPixelFallbackCatalog(); art = new FlatPixelSettlementGroundCatalog(selector);
        }
        [TearDown] public void Cleanup() { art?.Dispose(); selector?.Dispose(); content?.Dispose(); }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, x, y)); return e; }
        Entity Pool(int x = X, int y = Y) { Add("Floor", x, y); return Add("WaterPuddle", x, y); }
        Tile Water(Entity owner, bool quiet = true, int x = X, int y = Y)
        {
            Assert.True(art.TryResolveWaterGround(zone, x, y, out var tile, out var source));
            Assert.AreSame(owner, source); Assert.AreSame(Resources.Load<Texture2D>(quiet ? Quiet : Original), tile.sprite.texture);
            Assert.AreEqual(new Vector2(16,24), tile.sprite.rect.size); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
            return tile;
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        [TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)]
        public void QuietSurfacePreservesAllDisclosedDryBanksAndReader(int mask)
        {
            var owner = Pool();
            var offsets = new[] { new Vector2Int(0,-1), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,0) };
            for (int i=0;i<4;i++) { var q=offsets[i]; if((mask & (1<<i))!=0) Add("Floor",X+q.x,Y+q.y); else Pool(X+q.x,Y+q.y); }
            var tile=Water(owner); Assert.AreEqual(mask, (int)tile.sprite.rect.x / 16 % 16);
            using(var world=new FlatPixelGreyboxCatalog())
            {
                Assert.True(world.TryResolvePresentation(zone,X,Y,out var view));
                Assert.AreEqual(FlatPixelGreyboxGround.Water,view.GroundKind); Assert.AreEqual(tile.sprite.rect,view.GroundTile.sprite.rect);
                Assert.AreSame(tile.sprite.texture,view.GroundTile.sprite.texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var portrait,out _));
                Assert.AreSame(tile.sprite.texture,portrait.texture); Assert.AreEqual(tile.sprite.rect,portrait.rect);
            }
            Assert.AreEqual(120,owner.GetPart<LiquidPoolPart>().Volume);
        }
        [Test] public void SurfaceVariesWithoutChangingPoolsAndDryMaterials()
        {
            var variants=new HashSet<int>();
            for(int x=20;x<36;x++) { var p=Pool(x,20); variants.Add((int)Water(p,true,x,20).sprite.rect.x/256); Assert.AreEqual(120,p.GetPart<LiquidPoolPart>().Volume); }
            Assert.Greater(variants.Count,3);
            using(var world=new FlatPixelGreyboxCatalog())
            {
                Add("Floor",40,10);Add("StoneFloor",42,10);Add("Floor",44,10);Add("Duckboard",44,10);
                foreach(int x in new[]{40,42,44}) { Assert.True(world.TryResolvePresentation(zone,x,10,out var v)); Assert.AreNotSame(Resources.Load<Texture2D>(Quiet),v.GroundTile.sprite.texture); }
            }
        }
        [TestCase("profile")][TestCase("biome")][TestCase("name")][TestCase("type")][TestCase("detached")][TestCase("cache")][TestCase("lair")]
        public void CurrentSumpholdAuthorityIsRequired(string loss)
        {
            var p=Pool(); Water(p);var poi=manager.WorldMap.GetPOI(15,6);
            if(loss=="profile")poi.Profile="other";
            if(loss=="biome")manager.WorldMap.Tiles[15,6]=BiomeType.Sodden;
            if(loss=="name")poi.Name="other";
            if(loss=="type")manager.WorldMap.SetPOI(15,6,new PointOfInterest(POIType.Lair,"Sumphold",profile:"Boatyard"));
            if(loss=="detached")manager.CachedZones.Remove(zone.ZoneID);
            if(loss=="cache")manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
            if(loss=="lair") { manager.WorldMap.SetPOI(15,6,new PointOfInterest(POIType.Lair,"legacy",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(15,6,poi); }
            if(loss=="profile" || loss=="biome") Water(p,false);
            else Assert.False(art.TryResolveWaterGround(zone,X,Y,out _,out _));
        }
        [Test] public void AnotherAuthoredBoatyardDoesNotBorrowSumpholdSurface()
        {
            zone=new Zone("Overworld.13.7.0");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            manager.WorldMap.GetPOI(13,7).Profile="Boatyard";
            Water(Pool(),false);
        }
        [TestCase("hidden")][TestCase("depleted")][TestCase("frozen")][TestCase("duplicate")][TestCase("mixed")][TestCase("temporary")][TestCase("projected")]
        public void InvalidPoolStillRefusesNativeWater(string fault)
        {
            var p=Pool();Water(p);
            if(fault=="hidden")zone.GetCell(X,Y).IsVisible=false;
            if(fault=="depleted")p.GetPart<LiquidPoolPart>().Volume=0;
            if(fault=="frozen")p.GetPart<ThermalPart>().Temperature=p.GetPart<ThermalPart>().FreezeTemperature;
            if(fault=="duplicate")Add("WaterPuddle");
            if(fault=="mixed")zone.TileState.WriteCoating(X,Y,"oil",3);
            if(fault=="temporary")zone.TileState.Get(X,Y).Coatings[0].Turns=2;
            if(fault=="projected")zone.TileState.Get(X,Y).Coatings[0].ProjectionOwnerId=p.ID;
            Assert.False(art.TryResolveWaterGround(zone,X,Y,out _,out _));
        }

        FlatPixelSettlementGroundCatalog Inject(Texture2D optional, Texture2D mandatory)
            => new FlatPixelSettlementGroundCatalog(null,null,null,mandatory,null,null,null,null,null,null,null,optional,selector);

        [TestCase(false)][TestCase(true)] public void MissingOrMalformedOptionalSurfaceKeepsOriginalWater(bool malformed)
        {
            var p=Pool();var bad=malformed ? new Texture2D(16,24) : null;
            try { art.Dispose();art=Inject(bad,Resources.Load<Texture2D>(Original));Water(p,false); }
            finally { if(bad!=null)UnityEngine.Object.DestroyImmediate(bad); }
        }
        [Test] public void OptionalSurfaceDoesNotReplaceMissingRequiredWaterAndOwnsOnlyItsViews()
        {
            var p=Pool();var texture=Resources.Load<Texture2D>(Quiet);
            art.Dispose();art=Inject(texture,null);Assert.False(art.TryResolveWaterGround(zone,X,Y,out _,out _));
            art.Dispose();art=Inject(texture,Resources.Load<Texture2D>(Original));var t=Water(p);var sprite=t.sprite;
            art.Dispose();Assert.True(t==null);Assert.True(sprite==null);Assert.True(texture!=null);
            Assert.True(selector.TryResolve(zone,X,Y,out _));
        }
        [Test] public void ActorAndIndependentHazardKeepTheirMeaningAboveQuietWater()
        {
            var p=Pool();var actor=Add("Player");zone.TileState.AddCharge(X,Y,1);
            string state=zone.TileState.ToSaveString();Water(p);
            using(var world=new FlatPixelGreyboxCatalog())
            {
                Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));Assert.AreSame(actor,v.Owner);
                Assert.AreEqual(FlatPixelGreyboxGround.Water,v.GroundKind);Assert.AreSame(Resources.Load<Texture2D>(Quiet),v.GroundTile.sprite.texture);
            }
            Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
    }
}
