using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Flood water (2026-10-30). Spread and Sodden grass lying under a permanent, ownerless water coating
    /// (e.g. all 606 such cells in Sodden 18.10, about 180 in Spread 6.8 and 8.6) drew a black '.' field,
    /// while Grovelands already draws the identical case as Fen water. The same rule and the same approved
    /// water views now apply in the Spread and the Sodden.
    /// </summary>
    public sealed class FlatPixelFloodWaterTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
        }
        [SetUp] public void Setup() { manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog(); }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        Zone Visit(string id)
        {
            var zone = manager.GetZone(id); Assert.NotNull(zone, id); manager.SetActiveZone(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
            return zone;
        }
        static bool Flooded(Zone zone, int x, int y)
        {
            var st = zone.TileState.Get(x, y);
            return st != null && st.Coatings.Count == 1 && st.Coatings[0].Id == "water" && st.Coatings[0].Turns == ZoneTileState.Permanent
                && string.IsNullOrEmpty(st.Coatings[0].ProjectionOwnerId) && zone.GetCell(x, y).Objects.All(o => o.BlueprintName == "Grass");
        }
        bool NativeWater(Zone zone, int x, int y)
            => world.TryResolvePresentation(zone, x, y, out var v) && v.LocalGround && v.GroundKind == FlatPixelGreyboxGround.Water
                && v.ForegroundTile == null && v.GroundTile?.sprite != null
                && ReferenceEquals(v.GroundTile.sprite.texture, Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath))
                && v.GroundTile.sprite.rect.x / 16 < 16;

        [TestCase("Overworld.18.10.0")] [TestCase("Overworld.6.8.0")] [TestCase("Overworld.8.6.0")]
        public void FloodedGrassShowsTheApprovedWater(string id)
        {
            var zone = Visit(id);
            int total = 0, native = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (Flooded(zone, x, y)) { total++; if (NativeWater(zone, x, y)) native++; }
            Assert.Greater(total, 100, "precondition: flooded grass");
            Assert.GreaterOrEqual(native, total * 95 / 100, id + ": " + native + " of " + total);
        }

        [Test] public void PassingWaterMixedStateAndPoolsKeepTheirOwnPresentation()
        {
            var zone = Visit("Overworld.18.10.0");
            var dry = Enumerable.Range(0, Zone.Width * Zone.Height).Select(i => (x: i % Zone.Width, y: i / Zone.Width))
                .First(c => zone.TileState.Get(c.x, c.y) == null && zone.GetCell(c.x, c.y).Objects.Count == 1 && zone.GetCell(c.x, c.y).Objects[0].BlueprintName == "Grass");
            zone.TileState.WriteCoating(dry.x, dry.y, "water", 4);
            Assert.False(NativeWater(zone, dry.x, dry.y), "a passing splash is not standing water");
            var wet = Enumerable.Range(0, Zone.Width * Zone.Height).Select(i => (x: i % Zone.Width, y: i / Zone.Width)).First(c => Flooded(zone, c.x, c.y));
            zone.TileState.WriteCoating(wet.x, wet.y, "oil", 4);
            Assert.False(NativeWater(zone, wet.x, wet.y), "mixed liquids are not plain water");
        }

        [Test] public void GrovelandsKeepsItsOwnFenWaterAndOtherBiomesAreNotAdmitted()
        {
            var zone = Visit("Overworld.5.11.0");
            Assert.False(FlatPixelFloodWaterCatalog.IsSupportedZone(Visit("Overworld.6.6.0")), "a settlement is the settlement adapter's");
            Assert.True(FlatPixelFloodWaterCatalog.IsSupportedZone(zone), "ordinary Spread wilderness");
        }
    }
}
