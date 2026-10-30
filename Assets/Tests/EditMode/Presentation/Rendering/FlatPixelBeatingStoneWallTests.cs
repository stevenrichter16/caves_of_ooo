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
    /// Beating stone ruins (2026-10-30). The western Beating's grey StoneWall ruins ('#' &amp;w on sand) were
    /// the largest remaining Beating glyph owner (190 sampled cells). They reuse the approved straight shaded
    /// stone masonry (sill-environment frame 8) that StoneWall already wears in Sill, by the same static
    /// rule shape; sandstone walls keep their own sandstone brick.
    /// </summary>
    public sealed class FlatPixelBeatingStoneWallTests
    {
        const string Masonry = "Sprites/Live16x24/sill-environment";
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

        [TestCase("Overworld.0.16.0")] [TestCase("Overworld.0.17.0")] [TestCase("Overworld.1.15.0")]
        public void BeatingStoneRuinsWearTheApprovedStoneMasonry(string id)
        {
            var zone = Visit(id); var masonry = Resources.Load<Texture2D>(Masonry);
            Assert.True(FlatPixelBeatingMaterialsCatalog.IsSupportedZone(zone), "precondition: ordinary Beating");
            int total = 0, native = 0;
            foreach (var wall in zone.GetAllEntities().Where(e => e.BlueprintName == "StoneWall").ToList())
            {
                var (x, y) = zone.GetEntityPosition(wall);
                if (!world.TryResolvePresentation(zone, x, y, out var v) || !ReferenceEquals(v.Owner, wall)) continue;
                total++;
                if (!v.NativeForeground || !ReferenceEquals(v.ForegroundTile.sprite.texture, masonry)) continue;
                native++;
                Assert.AreEqual(new Rect(128, 0, 16, 24), v.ForegroundTile.sprite.rect, "the straight shaded masonry frame");
                Assert.True(v.BlocksMovement);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, wall, x, y, out var portrait, out _)); Assert.AreSame(masonry, portrait.texture);
            }
            Assert.Greater(total, 10, "precondition: stone ruins"); Assert.GreaterOrEqual(native, total * 95 / 100, id + ": " + native + " of " + total);
        }

        [Test] public void AFrozenOrRecolouredStoneWallKeepsItsFallback()
        {
            var zone = Visit("Overworld.0.16.0");
            var walls = zone.GetAllEntities().Where(e => e.BlueprintName == "StoneWall").Take(2).ToList();
            var (x, y) = zone.GetEntityPosition(walls[0]);
            var thermal = walls[0].GetPart<ThermalPart>();
            if (thermal != null) thermal.Temperature = thermal.FreezeTemperature - 10f; else walls[0].GetPart<RenderPart>().ColorString = "&B";
            Assert.True(world.TryResolvePresentation(zone, x, y, out var a)); Assert.False(a.NativeForeground);
            var (x2, y2) = zone.GetEntityPosition(walls[1]); walls[1].GetPart<RenderPart>().RenderString = "+";
            Assert.True(world.TryResolvePresentation(zone, x2, y2, out var b)); Assert.False(b.NativeForeground);
        }
    }
}
