using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Separate taxonomy sweep: saved-state replacement, exact names, cell
    // boundaries, multi-instance authority and partial resource destruction.
    // These cases are GREEN-review probes, not independently RED-proven.
    public sealed class FlatPixelEastSteamAdversarialTests
    {
        const string East = "Overworld.12.10.0";
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Texture2D atlas;
        FlatPixelEastSteamCatalog art;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = Fresh(); atlas = new Texture2D(16, 24); art = new FlatPixelEastSteamCatalog(atlas);
        }
        [TearDown] public void Cleanup()
        {
            try { art?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); }
            finally { content?.Dispose(); }
        }
        Zone Fresh()
        {
            var value = new Zone(East); manager.SetActiveZone(value);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                value.GetCell(x, y).IsVisible = value.GetCell(x, y).Explored = true;
            return value;
        }
        Tile Steam(int x = 10, int y = 10, int turns = 2)
        {
            zone.TileState.WriteCloud(x, y, "steam", turns);
            Assert.True(art.TryResolve(zone, zone.GetCell(x, y), out var tile)); Assert.NotNull(tile);
            return tile;
        }

        // Save/load reach: observe the loaded graph and exact remaining lease,
        // not a stale tile-state object cached by the presentation adapter.
        [TestCase(1)] [TestCase(2)] [TestCase(int.MaxValue)]
        public void Adversarial_RealTileStateRoundTripRetainsLeaseWithoutAdvancingIt(int turns)
        {
            var tile = Steam(turns: turns); var prior = zone; string saved = zone.TileState.ToSaveString();
            zone = Fresh(); zone.TileState.LoadFromString(saved);
            Assert.False(art.TryResolve(prior, prior.GetCell(10, 10), out _));
            Assert.AreEqual(turns, zone.TileState.Get(10, 10).CloudTurns);
            Assert.True(art.TryResolve(zone, zone.GetCell(10, 10), out var loaded)); Assert.AreSame(tile, loaded);
            Assert.AreEqual(saved, zone.TileState.ToSaveString());
            zone.TileState.Clear(10, 10); Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out _));
        }

        // Content names are exact protocol identities. A renderer must neither
        // normalize malformed saves into steam nor repair those saved values.
        [TestCase(null)] [TestCase("")] [TestCase(" steam")] [TestCase("steam ")]
        [TestCase("STEAM")] [TestCase("SteamCloud")]
        public void Adversarial_LoadedNonExactNameCannotAcquireNativeSteam(string cloud)
        {
            var tile = Steam(); zone.TileState.Get(10, 10).Cloud = cloud;
            string payload = zone.TileState.ToSaveString(); zone.TileState.LoadFromString(payload);
            string loaded = zone.TileState.ToSaveString();
            Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out var denied)); Assert.IsNull(denied);
            Assert.AreEqual(loaded, zone.TileState.ToSaveString());
            Assert.AreSame(tile, Steam());
        }

        // Four literal map corners exercise canonical indices without a
        // neighboring steam cell being mistaken for the requested one.
        [TestCase(0, 0)] [TestCase(79, 0)] [TestCase(0, 24)] [TestCase(79, 24)]
        public void Adversarial_CornerCloudDoesNotBleedIntoAdjacentCell(int x, int y)
        {
            var tile = Steam(x, y); int adjacent = x == 0 ? 1 : x - 1;
            Assert.False(art.TryResolve(zone, zone.GetCell(adjacent, y), out var empty)); Assert.IsNull(empty);
            string saved = zone.TileState.ToSaveString(); zone.TileState.LoadFromString(saved);
            Assert.True(art.TryResolve(zone, zone.GetCell(x, y), out var loaded)); Assert.AreSame(tile, loaded);
            Assert.False(art.TryResolve(zone, zone.GetCell(adjacent, y), out _));
        }

        // Optional render resources can disappear independently during editor
        // teardown. Refuse incomplete art and dispose remaining owned objects.
        [TestCase("texture")] [TestCase("sprite")] [TestCase("tile")]
        public void Adversarial_PartialResourceDestructionNeverReturnsBrokenArt(string missing)
        {
            var tile = Steam(); var sprite = tile.sprite;
            if (missing == "texture") Object.DestroyImmediate(atlas);
            else if (missing == "sprite") Object.DestroyImmediate(sprite);
            else Object.DestroyImmediate(tile);
            Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out var denied)); Assert.IsNull(denied);
            art.Dispose(); art.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
            if (missing != "texture") Assert.True(atlas != null, "The remaining borrowed texture is not owned.");
        }

        [Test]
        public void Adversarial_SameAddressReplacementUsesItsOwnCloudRatherThanOldSnapshot()
        {
            var tile = Steam(); var prior = zone; zone = Fresh();
            Assert.False(art.TryResolve(prior, prior.GetCell(10, 10), out _));
            Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out _));
            Assert.AreSame(tile, Steam());
            zone.TileState.Clear(10, 10); Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out _));
            Assert.AreEqual("steam", prior.TileState.Get(10, 10).Cloud, "Presentation never repairs the obsolete snapshot.");
        }

        [Test]
        public void Adversarial_ReadsNeverDirtyStateOrDispatchSightChanges()
        {
            var tile = Steam(); string before = zone.TileState.ToSaveString();
            var changed = zone.TileState.OnCellChanged; var sight = zone.TileState.OnSightChanged;
            int writes = 0, sights = 0;
            try
            {
                zone.TileState.OnCellChanged = (x, y) => writes++; zone.TileState.OnSightChanged = () => sights++;
                for (int i = 0; i < 20; i++)
                { Assert.True(art.TryResolve(zone, zone.GetCell(10, 10), out var same)); Assert.AreSame(tile, same); }
                Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.Zero(writes); Assert.Zero(sights);
                zone.TileState.WriteCloud(10, 10, "smoke", 2); Assert.Greater(writes, 0); Assert.Greater(sights, 0);
                Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out _));
            }
            finally { zone.TileState.OnCellChanged = changed; zone.TileState.OnSightChanged = sight; }
        }

        [Test]
        public void Adversarial_ReplacedCloudHasNoStickyNativeAdmission()
        {
            var tile = Steam(); zone.TileState.WriteCloud(10, 10, "veil-mist", 2);
            Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out _));
            Assert.AreSame(tile, Steam(turns: 1)); zone.TileState.Tick();
            Assert.False(art.TryResolve(zone, zone.GetCell(10, 10), out _));
        }

        [Test]
        public void Adversarial_NullContextOrForgedParentCannotBorrowCanonicalState()
        {
            Steam(); Assert.False(art.TryResolve(null, zone.GetCell(10, 10), out var denied)); Assert.IsNull(denied);
            Assert.True(art.TryResolve(zone, zone.GetCell(10, 10), out _));
            var forged = new Cell(10, 10, zone) { Explored = true, IsVisible = true };
            Assert.False(art.TryResolve(zone, forged, out denied)); Assert.IsNull(denied);
            Assert.True(art.TryResolve(zone, zone.GetCell(10, 10), out _));
        }
    }
}
