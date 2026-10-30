using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Settlement ground (2026-10-29). A detached seed-64 census found the fifteen ordinary authored
    /// settlements (every Place except Morrowfast's 3D scene and Sill's own adapter) drawing almost
    /// entirely glyph ground in the flat 16x24 view: e.g. Posy 89 of 2,000 native cells. Their Grass,
    /// Sand, StoneFloor and RoadStone now reuse the approved biome art the surrounding wilderness and
    /// Sill already use; anything wet, stacked, hidden or remembered keeps its fallback.
    /// </summary>
    public sealed class FlatPixelSettlementGroundTests
    {
        const string SillLawnAndFloors = "Sprites/Live16x24/sill-environment";
        const string ForestFloor = "Sprites/Live16x24/grovelands-ground";
        const string Beating = "Sprites/Live16x24/beating-materials";
        const string OpeningGround = "Sprites/Live16x24/opening-ground";
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup() { manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog(); }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        Zone Visit(string id)
        {
            var zone = manager.GetZone(id); Assert.NotNull(zone, id); manager.SetActiveZone(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
            return zone;
        }
        IEnumerable<(int x, int y, FlatPixelGreyboxCell view)> Cells(Zone zone, string blueprint)
        {
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (world.TryResolvePresentation(zone, x, y, out var v) && v.Owner?.BlueprintName == blueprint) yield return (x, y, v);
        }
        static bool NativeGround(FlatPixelGreyboxCell v, string resource)
            => v.LocalGround && v.GroundTile != null && v.GroundTile.sprite != null
                && ReferenceEquals(v.GroundTile.sprite.texture, Resources.Load<Texture2D>(resource)) && v.ForegroundTile == null;

        // ════════════════ the settlements ════════════════

        [TestCase("Overworld.5.9.0", "Grass", SillLawnAndFloors)]       // Posy, Spread
        [TestCase("Overworld.16.11.0", "Grass", SillLawnAndFloors)]     // Slip, Spread
        [TestCase("Overworld.16.1.0", "Grass", SillLawnAndFloors)]      // the Quiet's Door, Sodden
        [TestCase("Overworld.6.6.0", "Grass", ForestFloor)]             // Cinderhold, Grovelands
        [TestCase("Overworld.8.16.0", "Sand", Beating)]                 // Wellmeet, Beating
        [TestCase("Overworld.5.17.0", "Sand", Beating)]                 // the First Tent, Beating
        [TestCase("Overworld.10.14.0", "StoneFloor", SillLawnAndFloors)] // Tally
        [TestCase("Overworld.10.14.0", "RoadStone", OpeningGround)]     // Tally, Spread road
        [TestCase("Overworld.8.16.0", "RoadStone", Beating)]            // Wellmeet, Beating road
        public void SettlementGroundUsesTheApprovedArtOfItsBiome(string id, string blueprint, string resource)
        {
            var zone = Visit(id); Assert.True(FlatPixelSettlementGroundCatalog.IsSupportedZone(zone), id + " is an ordinary settlement");
            var cells = Cells(zone, blueprint).ToList();
            Assert.Greater(cells.Count, 20, "precondition: the settlement has visible " + blueprint);
            int native = cells.Count(c => NativeGround(c.view, resource));
            Assert.GreaterOrEqual(native, cells.Count * 95 / 100, blueprint + " in " + id + ": " + native + " of " + cells.Count + " native");
            foreach (var c in cells.Where(c => NativeGround(c.view, resource)).Take(25))
            {
                Assert.AreEqual(new Vector2(16, 24), c.view.GroundTile.sprite.rect.size);
                Assert.AreEqual(Tile.ColliderType.None, c.view.GroundTile.colliderType);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, c.view.Owner, c.x, c.y, out var portrait, out _), "the reader shows the same ground");
                Assert.AreSame(c.view.GroundTile.sprite.texture, portrait.texture); Assert.AreEqual(c.view.GroundTile.sprite.rect, portrait.rect);
            }
        }

        [Test] public void SettlementFloorsAndLawnUseTheirOwnFrames()
        {
            // Lawn frames 0–3 and room floors 4–7 of the Sill settlement atlas; never borrowed across.
            var zone = Visit("Overworld.10.14.0");
            var floors = Cells(zone, "StoneFloor").Where(c => NativeGround(c.view, SillLawnAndFloors)).ToList();
            Assert.IsNotEmpty(floors, "precondition: native room floors");
            foreach (var c in floors) Assert.That((int)c.view.GroundTile.sprite.rect.x / 16, Is.InRange(4, 7));
            zone = Visit("Overworld.5.9.0");
            var lawn = Cells(zone, "Grass").Where(c => NativeGround(c.view, SillLawnAndFloors)).ToList();
            Assert.IsNotEmpty(lawn, "precondition: native lawn");
            foreach (var c in lawn) Assert.That((int)c.view.GroundTile.sprite.rect.x / 16, Is.InRange(0, 3));
        }

        // ════════════════ counter-checks ════════════════

        [TestCase("Overworld.10.10.0")] [TestCase("Overworld.3.6.0")] [TestCase("Overworld.5.11.0")]
        public void SillMorrowfastAndWildernessAreNotThisAdaptersToDraw(string id)
            => Assert.False(FlatPixelSettlementGroundCatalog.IsSupportedZone(Visit(id)), id);

        [Test] public void WetHiddenAndStackedGroundKeepTheirFallback()
        {
            var zone = Visit("Overworld.5.9.0");
            var cells = Cells(zone, "Grass").Where(c => NativeGround(c.view, SillLawnAndFloors)).Take(3).ToList();
            Assert.AreEqual(3, cells.Count, "precondition");
            var (wx, wy, _) = cells[0]; zone.TileState.WriteCoating(wx, wy, "water", 4);
            Assert.True(world.TryResolvePresentation(zone, wx, wy, out var wet)); Assert.False(NativeGround(wet, SillLawnAndFloors), "a wet cell does not read as dry lawn");
            var (hx, hy, _) = cells[1]; zone.GetCell(hx, hy).IsVisible = false;
            Assert.True(world.TryResolvePresentation(zone, hx, hy, out var hidden)); Assert.False(NativeGround(hidden, SillLawnAndFloors), "remembered ground is not redrawn live");
            var (sx, sy, _) = cells[2]; Assert.True(zone.AddEntity(factory.CreateEntity("Sand"), sx, sy));
            Assert.True(world.TryResolvePresentation(zone, sx, sy, out var stacked)); Assert.False(NativeGround(stacked, SillLawnAndFloors), "two ground owners are ambiguous");
        }

        [Test] public void TheWildernessAroundAPlaceIsUnchanged()
        {
            // Counter-check: an ordinary Spread wilderness zone keeps its own adapter's turf.
            var zone = Visit("Overworld.5.11.0");
            var cells = Cells(zone, "Grass").ToList(); Assert.Greater(cells.Count, 100);
            Assert.GreaterOrEqual(cells.Count(c => c.view.LocalGround), cells.Count * 95 / 100);
        }
    }
}
