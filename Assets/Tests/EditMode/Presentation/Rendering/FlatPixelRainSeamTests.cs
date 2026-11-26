using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelRainSeamTests : FiftyWorldFixture
    {
        [Test]
        public void KnotflaxShowsDistinctStemsSeedHeadsAndDormantStalks()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 65, true);
            Zone = manager.GetZone(RepairCultivationSite.ZoneID);
            manager.SetActiveZone(Zone);
            var stand = Zone.GetReadOnlyEntities().Single(owner => owner.BlueprintName == "LateKnotflaxStand");
            var cell = Zone.GetEntityCell(stand);
            cell.Explored = cell.IsVisible = true;
            string originalGlyph = stand.GetPart<RenderPart>().RenderString;

            using var flat = new FlatPixelGreyboxCatalog();
            Tile Draw()
            {
                Assert.That(flat.TryResolvePresentation(Zone, cell.X, cell.Y, out var view), Is.True);
                Assert.That(view.Owner, Is.SameAs(stand));
                return view.ForegroundTile;
            }

            Clock.AdvanceClock(480);
            var stems = Draw();
            Clock.AdvanceClock(9600);
            var seeds = Draw();
            Clock.AdvanceClock(9600);
            var dormant = Draw();
            Assert.That(new[] { stems, seeds, dormant }.Distinct().Count(), Is.EqualTo(3));
            Assert.That(stand.GetPart<RenderPart>().RenderString, Is.EqualTo(originalGlyph));
            foreach (char glyph in new[] { '|', '*', ';' })
                Assert.False(EnvironmentSpriteRenderer.GlyphClaimAllowed(glyph, stand.BlueprintName));

            cell.IsVisible = false;
            Assert.That(flat.TryResolvePresentation(Zone, cell.X, cell.Y, out var memory), Is.True);
            Assert.That(memory.Owner, Is.Not.SameAs(stand));
        }

        [Test]
        public void VisibleBankChangesShapeInBothMapModesAndDisappearsIntoFog()
        {
            // This layout also has an earned native route with the optional seam.
            const int seed = 2;
            var manager = OverworldZoneManager.CreateDetached(Factory, seed, true);
            Zone = manager.GetZone(RepairCultivationSite.ZoneID);
            manager.SetActiveZone(Zone);
            var seam = Zone.GetReadOnlyEntities().SingleOrDefault(owner => owner.BlueprintName == "RainClaySeam");
            Assert.That(seam, Is.Not.Null, "The ordinary allotment must contain the visible bank.");
            var cell = Zone.GetEntityCell(seam);
            cell.Explored = cell.IsVisible = true;
            var position = new Vector3Int(cell.X, Zone.Height - 1 - cell.Y, 0);

            bool Rain(int window) => RegionalWeather.IsRainWindow(seed, BiomeType.Grovelands, window);
            int wetWindow = Enumerable.Range(1, RegionalWeather.CycleWindows - 2)
                .First(window => !Rain(window - 1) && Rain(window) && !Rain(window + 1));
            int start = wetWindow * RegionalWeather.WindowTicks;
            var root = new GameObject("Rain seam grid", typeof(Grid));
            root.SetActive(false);
            var host = new GameObject("World cells", typeof(Tilemap));
            host.transform.SetParent(root.transform, false);
            try
            {
                using var flat = new FlatPixelGreyboxCatalog();
                var tilemap = host.GetComponent<Tilemap>();
                var renderer = host.AddComponent<ZoneRenderer>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer, Zone);
                typeof(ZoneRenderer).GetField("_tilemap", flags).SetValue(renderer, tilemap);
                var renderCell = typeof(ZoneRenderer).GetMethod("RenderCellCore", flags);
                (TileBase ascii, Tile flat, Color tone) Paint()
                {
                    renderCell.Invoke(renderer, new object[] { cell.X, cell.Y });
                    Assert.That(flat.TryResolvePresentation(Zone, cell.X, cell.Y, out var pixels), Is.True);
                    Assert.That(pixels.Owner, Is.SameAs(seam));
                    return (tilemap.GetTile(position), pixels.ForegroundTile, pixels.ForegroundColor);
                }

                Clock.AdvanceClock(start - 1);
                var dormant = Paint();
                Clock.AdvanceClock(1);
                var loose = Paint();
                Clock.AdvanceClock(RegionalWeather.WindowTicks);
                var closing = Paint();
                Assert.That(new[] { dormant.ascii, loose.ascii, closing.ascii }.Distinct().Count(), Is.EqualTo(3));
                Assert.That(new[] { dormant.flat, loose.flat, closing.flat }.Distinct().Count(), Is.EqualTo(3));
                Assert.That(loose.tone, Is.Not.EqualTo(dormant.tone));

                cell.IsVisible = false;
                renderCell.Invoke(renderer, new object[] { cell.X, cell.Y });
                Assert.That(tilemap.GetTile(position), Is.Not.SameAs(closing.ascii));
                Assert.That(flat.TryResolvePresentation(Zone, cell.X, cell.Y, out var remembered), Is.True);
                Assert.That(remembered.Owner, Is.Not.SameAs(seam));
                cell.IsVisible = true;
                Assert.That(Paint().flat, Is.SameAs(closing.flat));

                // Paint only. Product transfer, failure and saved spent state have
                // independent gameplay fixtures.
                seam.GetPart<HarvestablePart>().Harvested = true;
                var spent = Paint();
                Assert.That(new[] { dormant.ascii, loose.ascii, closing.ascii, spent.ascii }.Distinct().Count(), Is.EqualTo(4));
                Assert.That(new[] { dormant.flat, loose.flat, closing.flat, spent.flat }.Distinct().Count(), Is.EqualTo(4));
                Assert.That(seam.GetPart<RenderPart>().RenderString, Is.EqualTo("_"));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
