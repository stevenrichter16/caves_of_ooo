using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow hypotheses: real descriptions must fit the actual
    /// 45-row overlay, and inspecting a shorter item must erase the old one.
    /// No hypothetical pagination API; these exercise current rendered tiles.</summary>
    public sealed class DensityExamineLayoutTests
    {
        private HotbarSaveFixture scope;
        private GameObject host;
        private AnnouncementUI ui;
        private EntityFactory factory;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            host = new GameObject("Density examine layout");
            host.transform.SetParent(scope.Root.transform, false);
            host.AddComponent<Grid>();
            ui = host.AddComponent<AnnouncementUI>();
            ui.Tilemap = Tiles("foreground");
            ui.BgTilemap = Tiles("background");
            factory = new EntityFactory();
            factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
        }

        [TearDown]
        public void Cleanup()
        {
            if (ui != null) ui.Close();
            if (host != null) Object.DestroyImmediate(host);
            scope?.Dispose();
        }

        [Test]
        public void EveryShippedSupportedItemFitsTheActualPopupViewport()
        {
            int checkedItems = 0, maximumRows = 0;
            string longest = null;
            foreach (var bp in factory.Blueprints.Values.Where(b => b.Tags.ContainsKey("Item")))
            {
                var item = factory.CreateEntity(bp.Name);
                if (!ItemExamineService.TryDescribeDetails(item, out _)) continue;
                string text = item.GetPart<ExaminablePart>().BuildExamineLine();
                ui.Open(text);
                AssertVisible(bp.Name);
                int rows = (int)typeof(AnnouncementUI).GetField("_popupH", Private).GetValue(ui);
                if (rows > maximumRows) { maximumRows = rows; longest = bp.Name; }
                checkedItems++;
            }
            Assert.That(checkedItems, Is.GreaterThan(50), "the content census must not be vacuous");
            TestContext.WriteLine("Inspected " + checkedItems + " items; largest popup=" + maximumRows + " rows (" + longest + ").");
        }

        [TestCase("BreacherCleaver")]
        [TestCase("FlamingSword")]
        public void TwoValidEnhancementsAndLiveAfflictionsRemainVisible(string blueprint)
        {
            var item = factory.CreateEntity(blueprint);
            var serrated = new EnhancementSerrated(); serrated.ApplyTier(2);
            var glow = new EnhancementGlowQuartz(); glow.ApplyTier(2);
            Assert.That(serrated.Applicable(item) && glow.Applicable(item), Is.True);
            item.AddPart(serrated); item.AddPart(glow);
            Assert.That(ItemEnhancing.CountEnhancements(item), Is.EqualTo(ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM));
            Assert.That(item.ApplyEffect(new PoisonedEffect(7, "1d3")), Is.True);
            Assert.That(item.ApplyEffect(new WetEffect(0.5f)), Is.True);
            string text = item.GetPart<ExaminablePart>().BuildExamineLine();
            StringAssert.Contains("Afflicted:", text);
            StringAssert.Contains(serrated.GetEffectDescription(), text);
            StringAssert.Contains(glow.GetEffectDescription(), text);
            ui.Open(text);
            AssertVisible(blueprint);
        }

        [Test]
        public void LongThenShortDescriptionLeavesNoOldTilesAndCloseErasesBothLayers()
        {
            // Keep the cleanup probe independent of changes to authored item copy.
            // Both fixtures still render through the production reader and tilemaps.
            const string shortText = "A short inspection.";
            string longer = string.Join("\n", Enumerable.Repeat("A longer inspection line occupies the reader.", 12));
            ui.Open(shortText);
            AssertVisible("short cleanup fixture");
            var expectedFg = Occupied(ui.Tilemap); var expectedBg = Occupied(ui.BgTilemap);
            ui.Open(longer);
            AssertVisible("long cleanup fixture");
            Assert.That(Occupied(ui.Tilemap).Count, Is.GreaterThan(expectedFg.Count), "The long fixture must paint extra foreground tiles.");
            Assert.That(Occupied(ui.BgTilemap).Count, Is.GreaterThan(expectedBg.Count), "The long fixture must paint extra background tiles.");
            ui.Open(shortText);
            CollectionAssert.AreEquivalent(expectedFg, Occupied(ui.Tilemap));
            CollectionAssert.AreEquivalent(expectedBg, Occupied(ui.BgTilemap));
            ui.Close();
            Assert.That(Occupied(ui.Tilemap), Is.Empty);
            Assert.That(Occupied(ui.BgTilemap), Is.Empty);
        }

        private void AssertVisible(string label)
        {
            var fg = Occupied(ui.Tilemap); var bg = Occupied(ui.BgTilemap);
            Assert.That(fg.Count, Is.GreaterThan(0), label);
            Assert.That(bg.Count, Is.GreaterThan(0), label);
            foreach (var p in fg.Concat(bg))
            {
                Assert.That(p.x, Is.InRange(0, CenteredPopupLayout.GridWidth - 1), label + " column");
                Assert.That(p.y, Is.InRange(0, CenteredPopupLayout.GridHeight - 1), label + " row");
            }
        }

        private Tilemap Tiles(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(host.transform, false);
            var map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); return map;
        }
        private static List<Vector3Int> Occupied(Tilemap map)
        {
            var cells = new List<Vector3Int>();
            foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) cells.Add(p);
            return cells;
        }
    }
}
