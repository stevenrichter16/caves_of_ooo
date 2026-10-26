using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // One existing material on one already-admitted graph. The public world
    // route supplies the RED gate; queries never create water or alter its use.
    public sealed class FlatPixelNorthSurfaceWaterTests
    {
        const string North = "Overworld.11.9.0";
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Texture2D atlas;
        FlatPixelFallbackCatalog selector;
        FlatPixelOpeningGroundCatalog ground;
        FlatPixelGreyboxCatalog world;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            selector = new FlatPixelFallbackCatalog(); atlas = new Texture2D(336, 24);
            ground = new FlatPixelOpeningGroundCatalog(atlas, selector); zone = Attach(North);
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); ground?.Dispose(); selector?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); }
            finally { world = null; ground = null; selector = null; atlas = null; content?.Dispose(); }
        }
        Zone Attach(string id)
        {
            var target = new Zone(id); manager.SetActiveZone(target);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                target.GetCell(x, y).Explored = target.GetCell(x, y).IsVisible = true;
            return target;
        }
        Entity Add(string blueprint)
        { var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, X, Y)); return owner; }
        void Water(int x = X, int y = Y, int turns = ZoneTileState.Permanent)
            => zone.TileState.WriteCoating(x, y, "water", turns);
        static Rect Frame(int mask) => new Rect(16 * mask, 0, 16, 24);
        Tile Expect(int mask)
        {
            Assert.True(ground.TryResolve(zone, X, Y, out var tile, out var tint), "Exact North surface water still has no shared bank material.");
            Assert.AreSame(atlas, tile.sprite.texture); Assert.AreEqual(Frame(mask), tile.sprite.rect);
            Assert.AreEqual(Color.white, tint); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(TileFlags.None, tile.flags);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); return tile;
        }
        FlatPixelGreyboxCell Visible()
        { Assert.True(world.TryResolvePresentation(zone, X, Y, out var result)); return result; }
        Texture2D Resource()
        { var texture = Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath); Assert.NotNull(texture); return texture; }

        [TestCase(64)] [TestCase(65)] [TestCase(200893250)]
        public void OrdinaryWorldReusesTheApprovedWaterAtlasOnExactNorth(int seed)
        {
            manager = OverworldZoneManager.CreateDetached(content.Factory, seed, true); zone = Attach(North);
            var actor = Add("Player"); Water(); Water(X + 1, Y);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone)); world = new FlatPixelGreyboxCatalog();
            var result = Visible(); Assert.AreSame(actor, result.Owner); Assert.True(result.LocalGround);
            Assert.AreEqual(FlatPixelGreyboxGround.Water, result.GroundKind);
            Assert.AreSame(Resource(), result.GroundTile.sprite.texture, "Ordinary presentation must reach the existing atlas without injection.");
            Assert.AreEqual(Frame(13), result.GroundTile.sprite.rect); Assert.AreEqual(Color.white, result.GroundColor);
            Assert.AreEqual(FilterMode.Point, Resource().filterMode); Assert.AreEqual(TextureWrapMode.Clamp, Resource().wrapMode);
            Assert.AreEqual(1, Resource().mipmapCount);
            Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
            Assert.AreSame(actor, Visible().Owner); Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
        }

        [Test] public void EveryCardinalShorelineUsesItsExistingFrameWithoutChangingTheWorld()
        {
            var frames = new HashSet<Tile>();
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(North); Water();
                if ((mask & 1) == 0) Water(X, Y - 1);
                if ((mask & 2) == 0) Water(X + 1, Y);
                if ((mask & 4) == 0) Water(X, Y + 1);
                if ((mask & 8) == 0) Water(X - 1, Y);
                Water(X - 1, Y - 1); Water(X + 2, Y); // Diagonal/distant water cannot change a cardinal seam.
                string state = zone.TileState.ToSaveString(); int entities = zone.EntityVersion;
                var tile = Expect(mask); Assert.True(frames.Add(tile)); Assert.AreSame(tile, Expect(mask));
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(entities, zone.EntityVersion);
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y).IsWall());
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("ice")] [TestCase("expired-water")]
        public void UnknownOrCoatedNeighborsDoNotInventDryBanks(string fault)
        {
            Water(); Water(X, Y - 1); Water(X, Y + 1); Water(X - 1, Y); Expect(2);
            var east = zone.GetCell(X + 1, Y);
            if (fault == "hidden") east.IsVisible = false;
            else if (fault == "unexplored") east.Explored = false;
            else
            {
                zone.TileState.WriteCoating(X + 1, Y, fault == "ice" ? "ice" : "water", 2);
                if (fault == "expired-water") zone.TileState.Get(X + 1, Y).Coatings[0].Turns = 0;
            }
            Expect(0); east.IsVisible = east.Explored = true;
            if (fault == "ice" || fault == "expired-water")
                Assert.True(zone.TileState.RemoveCoating(X + 1, Y, fault == "ice" ? "ice" : "water"));
            Expect(2);
        }

        [TestCase(false)] [TestCase(true)]
        public void HiddenCenterDoesNotDiscloseLiveWaterOrBankState(bool explored)
        {
            Add("Grass"); Water(); Expect(15); world = new FlatPixelGreyboxCatalog();
            var cell = zone.GetCell(X, Y); cell.IsVisible = false; cell.Explored = explored;
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
            bool found = world.TryResolvePresentation(zone, X, Y, out var result);
            if (explored) { Assert.True(found); Assert.True(result.Remembered); Assert.AreNotSame(Resource(), result.GroundTile.sprite.texture); }
            else Assert.False(found);
            cell.IsVisible = cell.Explored = true; Expect(15);
        }

        [TestCase("oil")] [TestCase("ice")] [TestCase("brine")]
        [TestCase("duplicate-water")] [TestCase("expired-extra")]
        public void MixedCoatingsAndFrozenWaterKeepTheExistingHazardGround(string fault)
        {
            Water(); Expect(15); var state = zone.TileState.Get(X, Y);
            if (fault == "duplicate-water") state.Coatings.Add(new ZoneTileState.Layer { Id = "water", Turns = 3 });
            else
            {
                zone.TileState.WriteCoating(X, Y, fault == "expired-extra" ? "oil" : fault, 3);
                if (fault == "expired-extra") state.Coatings[1].Turns = 0;
            }
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); world = new FlatPixelGreyboxCatalog();
            Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
            state.Coatings.RemoveAt(1); Expect(15);
            Assert.True(zone.TileState.RemoveCoating(X, Y, "water")); zone.TileState.WriteCoating(X, Y, "ice", 3);
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
        }

        [Test] public void DryRoadFieldFloorAndPreparedSoilReturnToTheirExistingPresentation()
        {
            world = new FlatPixelGreyboxCatalog();
            foreach (string name in new[] { "RoadStone", "CropRow", "RipeCropRow", "Floor", "Grass" })
            {
                zone = Attach(North); var owner = Add(name);
                if (name == "Grass") { owner.SetTag("Plantable"); owner.AddPart(new CultivatedSoilPart()); }
                var before = Visible(); Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
                Water(); Expect(15); Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
                Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); var after = Visible();
                Assert.AreSame(before.Owner, after.Owner); Assert.AreEqual(before.GroundKind, after.GroundKind);
                Assert.AreEqual(before.GroundColor, after.GroundColor); Assert.AreSame(before.GroundTile, after.GroundTile);
                Assert.AreSame(before.ForegroundTile, after.ForegroundTile);
            }
        }

        [Test] public void GraphBiomePoiAndCanonicalNorthAddressStillControlAdmission()
        {
            Water(); Expect(15); world = new FlatPixelGreyboxCatalog();
            manager.CachedZones[North] = new Zone(North);
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            manager.CachedZones[North] = zone; Expect(15);
            manager.WorldMap.SetPOI(11, 9, new PointOfInterest(POIType.Village, "scope counter", null, 1));
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
            manager.WorldMap.SetPOI(11, 9, null); Expect(15);
            manager.WorldMap.Tiles[11, 9] = BiomeType.Sodden;
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
            manager.WorldMap.Tiles[11, 9] = BiomeType.Spread; Expect(15);
            var foreign = new Zone(North); foreign.GetCell(X, Y).Explored = foreign.GetCell(X, Y).IsVisible = true;
            foreign.TileState.WriteCoating(X, Y, "water", 4);
            Assert.False(ground.TryResolve(foreign, X, Y, out _, out _));
        }

        [Test] public void NorthDoesNotGrantTheMaterialToOtherGraphsOrDisableApprovedWaterSites()
        {
            world = new FlatPixelGreyboxCatalog();
            foreach (string id in new[] { "Overworld.12.11.0", GleanersCellarBuilder.ZoneID, "Overworld.11.7.0" })
            {
                zone = Attach(id); Water(); Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
                Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
            }
            foreach (string id in new[] { ReferenceGladePlan.ZoneID, "Overworld.11.8.0", "Overworld.12.10.0", North })
            { zone = Attach(id); Water(); Expect(15); Assert.AreSame(Resource(), Visible().GroundTile.sprite.texture); }
        }

        [Test] public void StillForageAndAnimalForegroundRemainTheSameAboveWater()
        {
            world = new FlatPixelGreyboxCatalog();
            foreach (string name in new[] { "AlchemyStill", "StoneburrPatch", "Viper" })
            {
                zone = Attach(North); var owner = Add(name); var before = Visible();
                Assert.AreSame(owner, before.Owner); Assert.True(before.NativeForeground);
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath), before.ForegroundTile.sprite.texture);
                bool blocks = zone.GetCell(X, Y).BlocksMovement(); Water(); var wet = Visible();
                Assert.AreSame(Resource(), wet.GroundTile.sprite.texture); Assert.AreSame(before.ForegroundTile, wet.ForegroundTile);
                Assert.AreSame(owner, wet.Owner); Assert.AreEqual(blocks, zone.GetCell(X, Y).BlocksMovement());
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _));
                Assert.AreSame(before.ForegroundTile.sprite, sprite);
                Assert.True(zone.TileState.RemoveCoating(X, Y, "water")); Assert.AreSame(before.ForegroundTile, Visible().ForegroundTile);
            }
        }

        [Test] public void ExpiryAndReadditionRecomputeTheBankWithoutRefreshingOrRecreatingOwners()
        {
            var actor = Add("Player"); Water(); Water(X + 1, Y, 1); world = new FlatPixelGreyboxCatalog();
            Expect(13); var foreground = Visible().ForegroundTile; int version = zone.EntityVersion;
            zone.TileState.Tick(); Expect(15); Assert.AreSame(foreground, Visible().ForegroundTile);
            Water(X + 1, Y, 2); Expect(13); Assert.AreSame(actor, Visible().Owner); Assert.AreEqual(version, zone.EntityVersion);
            zone.TileState.RemoveCoating(X, Y, "water"); Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
            Assert.AreNotSame(Resource(), Visible().GroundTile.sprite.texture);
        }

        [Test] public void ActualPeatBogKeepsItsOwnerStockAndSourceWhileWarmGroundQueriesAllocateNothing()
        {
            Water(); Expect(15); var pool = Add("PeatBog"); var liquid = pool.GetPart<LiquidPoolPart>();
            var source = pool.GetPart<TileStateSourcePart>(); Assert.NotNull(source);
            Assert.AreEqual("water", liquid.LiquidId); Assert.AreEqual(70, liquid.Volume);
            Assert.AreEqual("water", source.Coating); Assert.AreEqual(4, source.CoatingTurns);
            world = new FlatPixelGreyboxCatalog(); var visible = Visible();
            Assert.AreSame(pool, visible.Owner); Assert.AreSame(Resource(), visible.GroundTile.sprite.texture);
            int volume = liquid.Volume, version = zone.EntityVersion; string state = zone.TileState.ToSaveString();
            for (int i = 0; i < 256; i++) ground.TryResolve(zone, X, Y, out _, out _);
            long start = GC.GetAllocatedBytesForCurrentThread(); bool all = true;
            for (int i = 0; i < 1024; i++) all &= ground.TryResolve(zone, X, Y, out _, out _);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.True(all); Assert.Zero(bytes); Assert.AreEqual(state, zone.TileState.ToSaveString());
            Assert.AreEqual(volume, liquid.Volume); Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreSame(pool, source.ParentEntity); Assert.AreEqual("water", source.Coating); Assert.AreEqual(4, source.CoatingTurns);
            Assert.AreSame(pool, Visible().Owner); Assert.AreSame(visible.ForegroundTile, Visible().ForegroundTile);
            Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _), "The pool entity alone is not a surface coating.");
            Assert.AreEqual(volume, liquid.Volume); Assert.AreSame(zone.GetCell(X, Y), zone.GetEntityCell(pool));
        }

        [Test] public void MissingOptionalAtlasKeepsTheExistingFlatWaterFallback()
        {
            Water(); Expect(15);
            using (var absent = new FlatPixelOpeningGroundCatalog(null, selector))
                Assert.False(absent.TryResolve(zone, X, Y, out _, out _));
            using (var injected = new FlatPixelGreyboxCatalog((Texture2D)null))
            {
                Assert.True(injected.TryResolvePresentation(zone, X, Y, out var fallback));
                Assert.AreEqual(FlatPixelGreyboxGround.Water, fallback.GroundKind);
                Assert.AreEqual((Color)new Color32(12, 67, 68, 255), fallback.GroundColor);
                Assert.AreNotSame(Resource(), fallback.GroundTile.sprite.texture);
            }
            Assert.True(selector.TryResolve(zone, X, Y, out _)); Assert.True(atlas != null);
        }
    }
}
