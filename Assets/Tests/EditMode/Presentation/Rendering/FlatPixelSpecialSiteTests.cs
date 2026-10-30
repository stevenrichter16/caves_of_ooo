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
    /// Special-site surfaces (2026-10-30). A seed-64 census found the surface graphs of the world's
    /// sinkholes (Stillleaf, Ginmere, the Deepest Cathedral, Lampwell, Spivenor) almost entirely glyph:
    /// every biome adapter admits only point-of-interest-free wilderness. Their ordinary biome materials
    /// (grass, stone, bushes, trees, tepui stone, mire) are now drawn by the same biome adapter that
    /// draws them in the wilderness, admitted for that biome's own sites only.
    /// </summary>
    public sealed class FlatPixelSpecialSiteTests
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
        int Native(Zone zone)
        {
            int n = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (world.TryResolvePresentation(zone, x, y, out var v) && (v.NativeForeground || v.LocalGround && v.ForegroundTile == null)) n++;
            return n;
        }

        [TestCase("Overworld.2.4.0", BiomeType.Stump)]       // Stillleaf
        [TestCase("Overworld.2.7.0", BiomeType.Grovelands)]  // Ginmere
        [TestCase("Overworld.5.4.0", BiomeType.Grovelands)]  // the Deepest Cathedral
        [TestCase("Overworld.12.3.0", BiomeType.Spread)]     // Lampwell
        [TestCase("Overworld.16.4.0", BiomeType.Sodden)]     // Spivenor
        public void SinkholeSurfacesUseTheirBiomesApprovedMaterials(string id, BiomeType biome)
        {
            var zone = Visit(id);
            Assert.True(FlatPixelSiteAdmission.IsOrdinarySite(zone, biome), id + " is an ordinary " + biome + " site");
            int native = Native(zone);
            Assert.GreaterOrEqual(native, Zone.Width * Zone.Height * 3 / 4, id + ": " + native + " of 2000 native");
        }

        // The ring of solid SinkholeLip around each mouth was a grey '#' field; it now draws the original
        // sinkhole-lip rim block (moss cap, strata, undercut base), still solid.
        [TestCase("Overworld.2.4.0")] [TestCase("Overworld.2.7.0")] [TestCase("Overworld.5.4.0")]
        [TestCase("Overworld.12.3.0")] [TestCase("Overworld.16.4.0")]
        public void SinkholeLipsDrawTheOriginalRimRock(string id)
        {
            var zone = Visit(id); var lip = Resources.Load<Texture2D>(FlatPixelSinkholeLipCatalog.ResourcePath);
            int total = 0, native = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                if (!world.TryResolvePresentation(zone, x, y, out var v) || v.Owner?.BlueprintName != "SinkholeLip") continue;
                total++;
                if (!v.NativeForeground || !ReferenceEquals(v.ForegroundTile.sprite.texture, lip)) continue;
                native++; Assert.True(v.BlocksMovement, "the lip stays solid");
                Assert.True(world.TryGetPresentationSubjectSprite(zone, v.Owner, x, y, out var portrait, out _)); Assert.AreSame(lip, portrait.texture);
            }
            Assert.Greater(total, 30, "precondition: a lip ring"); Assert.GreaterOrEqual(native, total * 95 / 100, id + ": " + native + " of " + total);
        }

        [Test] public void ALipOutsideASiteOrRecolouredKeepsItsGlyph()
        {
            var zone = Visit("Overworld.5.11.0");
            var open = Enumerable.Range(1, 70).Select(x => (x, y: 12)).First(c => zone.GetCell(c.x, c.y).IsPassable());
            Assert.True(zone.AddEntity(factory.CreateEntity("SinkholeLip"), open.x, open.y));
            Assert.True(world.TryResolvePresentation(zone, open.x, open.y, out var wild)); Assert.False(wild.NativeForeground && wild.Owner?.BlueprintName == "SinkholeLip", "wilderness is not a sinkhole site");
            zone = Visit("Overworld.5.4.0");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                var lip = zone.GetCell(x, y).Objects.FirstOrDefault(o => o.BlueprintName == "SinkholeLip"); if (lip == null) continue;
                lip.GetPart<RenderPart>().ColorString = "&R";
                Assert.True(world.TryResolvePresentation(zone, x, y, out var odd)); Assert.False(odd.NativeForeground, "a recoloured lip is not the plain rim"); return;
            }
            Assert.Fail("precondition: a lip");
        }

        [Test] public void ASiteIsOnlyItsOwnBiomesToDraw()
        {
            var zone = Visit("Overworld.12.3.0");
            Assert.False(FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Grovelands), "a Spread site is never a Grovelands site");
            Assert.False(FlatPixelSpreadEnvironmentCatalog.IsSupportedZone(zone), "the wilderness admission itself is unchanged");
        }

        [TestCase("Overworld.5.9.0")]  // Posy, a settlement
        [TestCase("Overworld.8.12.0")] // Spider Nest, a lair
        [TestCase("Overworld.5.11.0")] // ordinary wilderness
        [TestCase("Overworld.3.3.0")]  // the Root
        public void SettlementsLairsWildernessAndUniqueSitesAreNotSites(string id)
        {
            var zone = Visit(id);
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
                Assert.False(FlatPixelSiteAdmission.IsOrdinarySite(zone, biome), id + " as " + biome);
        }

        [Test] public void TheCaveBelowASiteIsNotTheSite()
        {
            var below = manager.GetZone("Overworld.12.3.1"); Assert.NotNull(below);
            Assert.False(FlatPixelSiteAdmission.IsOrdinarySite(below, BiomeType.Spread));
        }
    }
}
