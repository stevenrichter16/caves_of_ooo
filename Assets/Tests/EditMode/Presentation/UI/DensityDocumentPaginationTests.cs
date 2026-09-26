using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class DensityDocumentPaginationTests
    {
        private HotbarSaveFixture scope;
        private GameObject host;
        private AnnouncementUI ui;

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            host = new GameObject("Document pagination test");
            host.transform.SetParent(scope.Root.transform, false); host.AddComponent<Grid>();
            ui = host.AddComponent<AnnouncementUI>();
            ui.Tilemap = Tiles("text"); ui.BgTilemap = Tiles("backdrop");
        }
        [TearDown] public void Cleanup()
        { ui?.Close(); UnityEngine.Object.DestroyImmediate(host); scope?.Dispose(); }

        [TestCase("01")][TestCase("02")][TestCase("03")][TestCase("04")]
        [TestCase("05")][TestCase("06")][TestCase("07")][TestCase("08")]
        [TestCase("09")][TestCase("10")][TestCase("11")][TestCase("12")][TestCase("13")]
        public void CanonicalArtifact_EveryPageFitsAndTextRemainsComplete(string id)
        {
            var doc = ReadableDocumentCatalog.Get("codex-" + id);
            Assert.IsNotNull(doc);
            ui.Open(doc.Title + "\n\n" + doc.Text);
            AssertVisible();
            Assert.That(PageCount, Is.GreaterThan(0));
            var all = new System.Collections.Generic.List<string>();
            for (int page = 0; page < PageCount; page++)
            {
                Assert.IsTrue(GoToPage(page)); AssertVisible();
                all.AddRange(CurrentLines());
            }
            var wrapped = (System.Collections.Generic.List<string>)typeof(AnnouncementUI)
                .GetField("_wrappedLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
            CollectionAssert.AreEqual(wrapped, all, "no line dropped, duplicated or reordered across pages");
        }

        [Test] public void PageBoundsAndBackwardNavigation_PreserveCurrentMessage()
        {
            ui.Open(string.Join("\n", Enumerable.Range(0, 100).Select(i => "line " + i)));
            AssertVisible(); Assert.That(PageCount, Is.GreaterThan(1));
            Assert.IsFalse(GoToPage(-1)); Assert.IsFalse(GoToPage(PageCount));
            Assert.IsTrue(GoToPage(PageCount - 1)); AssertVisible();
            CollectionAssert.Contains(CurrentLines(), "line 99");
            Assert.IsTrue(GoToPage(0)); CollectionAssert.Contains(CurrentLines(), "line 0");
        }

        [Test] public void ReopenShortText_ResetsPageAndErasesLongFrame()
        {
            ui.Open("short message"); int fg = Count(ui.Tilemap), bg = Count(ui.BgTilemap);
            ui.Open(string.Join("\n", Enumerable.Range(0, 100).Select(i => "line " + i)));
            Assert.IsTrue(GoToPage(PageCount - 1)); ui.Open("short message");
            Assert.AreEqual(1, PageCount); CollectionAssert.AreEqual(new[] { "short message" }, CurrentLines());
            Assert.AreEqual(fg, Count(ui.Tilemap)); Assert.AreEqual(bg, Count(ui.BgTilemap));
            ui.Close(); Assert.AreEqual(0, Count(ui.Tilemap)); Assert.AreEqual(0, Count(ui.BgTilemap));
        }

        [Test] public void LongReader_FooterLeavesTheInventoryActionRowsClear()
        {
            ui.Open(string.Join("\n", Enumerable.Range(0, 100).Select(i => "line " + i)));
            foreach (var p in ui.Tilemap.cellBounds.allPositionsWithin)
                if (ui.Tilemap.HasTile(p)) Assert.That(p.y, Is.GreaterThanOrEqualTo(4));
        }

        [TestCase('—', '-')][TestCase('–', '-')][TestCase('’', '\'')]
        [TestCase('“', '"')][TestCase('”', '"')][TestCase('A', 'A')]
        public void CanonicalPunctuation_UsesExistingReadableGlyphWithoutChangingText(char original, char visible)
        {
            ui.Open(original.ToString());
            int x = (int)typeof(AnnouncementUI).GetField("_worldOriginX", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
            int y = (int)typeof(AnnouncementUI).GetField("_worldTopY", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
            Assert.AreSame(CP437TilesetGenerator.GetUiTile(visible), ui.Tilemap.GetTile(new Vector3Int(x + 2, y - 1, 0)));
            CollectionAssert.AreEqual(new[] { original.ToString() }, CurrentLines());
        }

        private int PageCount
        {
            get { var p = typeof(AnnouncementUI).GetProperty("PageCount");
                Assert.IsNotNull(p, "long text needs an explicit page count"); return (int)p.GetValue(ui); }
        }
        private bool GoToPage(int page)
        {
            var method = typeof(AnnouncementUI).GetMethod("GoToPage");
            Assert.IsNotNull(method, "the reader needs forward/backward page navigation");
            return (bool)method.Invoke(ui, new object[] { page });
        }
        private string[] CurrentLines()
        {
            var p = typeof(AnnouncementUI).GetProperty("VisibleLines");
            Assert.IsNotNull(p); return ((System.Collections.Generic.IReadOnlyList<string>)p.GetValue(ui)).ToArray();
        }
        private void AssertVisible()
        {
            Assert.That(Count(ui.Tilemap), Is.GreaterThan(0));
            foreach (var map in new[] { ui.Tilemap, ui.BgTilemap })
                foreach (var p in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(p))
                    { Assert.That(p.x, Is.InRange(0, 79)); Assert.That(p.y, Is.InRange(0, 44)); }
        }
        private static int Count(Tilemap map)
        { int count = 0; foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) count++; return count; }
        private Tilemap Tiles(string name)
        { var go = new GameObject(name); go.transform.SetParent(host.transform, false); var t = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); return t; }
    }
}
