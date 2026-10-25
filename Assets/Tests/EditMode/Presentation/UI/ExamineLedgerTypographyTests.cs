using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>Measures the painted pixels after the real tile transform. Logical
    /// cell bounds alone miss enlarged letters colliding in the Deck's narrow grid.</summary>
    public sealed class ExamineLedgerTypographyTests
    {
        GameObject host;
        Grid grid;
        AnnouncementUI ui;

        [SetUp] public void Setup()
        {
            host = new GameObject("Ledger typography test");
            grid = host.AddComponent<Grid>();
            ui = host.AddComponent<AnnouncementUI>();
            ui.Tilemap = Tiles("fg");
            ui.BgTilemap = Tiles("bg");
        }

        [TearDown] public void Cleanup()
        {
            ui.Close();
            Object.DestroyImmediate(host);
        }

        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        public void EnlargedTitleLettersHaveSeparateInkAtActualGameplayAspect(int width, int height)
        {
            ConfigureDisplay(width, height);
            ui.OpenDocument(Document("MMMMMMMM", "Readable body."));
            var letters = InkOnRow('M', Field("_worldTopY") - 3);
            Assert.AreEqual(8, letters.Count, "The full title is present; hiding a colliding glyph is not a fix.");
            AssertSeparate(letters);
        }

        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        public void BodyLettersStillFitTheirRowsAndRemainInsideThePanel(int width, int height)
        {
            ConfigureDisplay(width, height);
            ui.OpenDocument(Document("A short title", "HHHHHHHH"));
            var letters = InkOnRow('H', Field("_worldTopY") - 8);
            Assert.AreEqual(8, letters.Count);
            AssertSeparate(letters);
            float left = ui.Tilemap.CellToWorld(new Vector3Int(Field("_worldOriginX") + 1, 0, 0)).x;
            float right = ui.Tilemap.CellToWorld(new Vector3Int(Field("_worldOriginX") + 55, 0, 0)).x;
            foreach (var ink in letters)
            {
                Assert.GreaterOrEqual(ink.xMin, left);
                Assert.Less(ink.xMax, right);
                Assert.Less(ink.height, 1f, "Adjacent body rows must retain vertical breathing room.");
            }
        }

        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        public void LongTitleInkStaysClearOfPortraitAndSecondTitleRow(int width, int height)
        {
            ConfigureDisplay(width, height);
            ui.OpenDocument(Document(new string('M', 80), "Readable body."));
            int top = Field("_worldTopY");
            var first = InkOnRow('M', top - 3);
            var second = InkOnRow('M', top - 5);
            Assert.Greater(first.Count, 0);
            Assert.Greater(second.Count, 0);
            float portraitLeft = ui.Tilemap.CellToWorld(new Vector3Int(Field("_worldOriginX") + 43, top, 0)).x;
            foreach (var ink in first) Assert.Less(ink.xMax, portraitLeft);
            foreach (var ink in second) Assert.Less(ink.xMax, portraitLeft);
            float firstBottom = float.PositiveInfinity, secondTop = float.NegativeInfinity;
            foreach (var ink in first) firstBottom = Mathf.Min(firstBottom, ink.yMin);
            foreach (var ink in second) secondTop = Mathf.Max(secondTop, ink.yMax);
            Assert.Greater(firstBottom, secondTop, "The two title lines must not collide vertically.");
        }

        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        public void AdjacentBodyInkHasReadableLeadingWithoutShrinkingOrDroppingGlyphs(int width, int height)
        {
            ConfigureDisplay(width, height);
            const string text = "HHggjjpp";
            ui.OpenDocument(Document("Readability", text + "\n" + text));
            CollectionAssert.AreEqual(new[] { text, text }, ui.VisibleLines,
                "Visual leading must not insert blank document lines.");
            int row = Field("_worldTopY") - 8;
            Rect first = TextInk(row, 2, text);
            Rect second = TextInk(row - 1, 2, text);
            Assert.That(first.yMin - second.yMax, Is.GreaterThanOrEqualTo(.25f),
                "Adjacent body lines need at least a quarter-cell of clear ink; one-pixel leading is too tight.");
            Assert.That(ui.Tilemap.GetTransformMatrix(new Vector3Int(Field("_worldOriginX") + 2, row, 0)).m13,
                Is.EqualTo(0f).Within(.0001f), "The first body line keeps its original baseline.");
        }

        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        public void FactLabelsValuesAndDividersShareTheSpacedBaseline(int width, int height)
        {
            ConfigureDisplay(width, height);
            ui.OpenDocument(new AnnouncementDocument("Facts", "EXAMINE", facts: new[]
            {
                new AnnouncementFact("H", "H"), new AnnouncementFact("H", "H")
            }));
            CollectionAssert.AreEqual(new[] { "H: H", "H: H" }, ui.VisibleLines);
            int firstRow = Field("_worldTopY") - 8;
            Rect first = TextInk(firstRow, 2, "H");
            Rect second = TextInk(firstRow - 1, 2, "H");
            Assert.That(first.yMin - second.yMax, Is.GreaterThanOrEqualTo(.25f),
                "Facts need the same readable leading as ordinary prose.");
            for (int i = 0; i < 2; i++)
            {
                int row = firstRow - i;
                Rect label = TextInk(row, 2, "H"), value = TextInk(row, 17, "H");
                Assert.That(value.yMin, Is.EqualTo(label.yMin).Within(.0001f));
                Assert.That(value.yMax, Is.EqualTo(label.yMax).Within(.0001f));
                var labelCell = new Vector3Int(Field("_worldOriginX") + 2, row, 0);
                var dividerCell = new Vector3Int(Field("_worldOriginX") + 16, row, 0);
                Assert.AreSame(CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.BoxVertical),
                    ui.Tilemap.GetTile(dividerCell), "The real fact divider must still be present.");
                float shift = ui.Tilemap.GetTransformMatrix(labelCell).m13;
                Assert.That(ui.Tilemap.GetTransformMatrix(dividerCell).m13,
                    Is.EqualTo(shift).Within(.0001f), "Fact dividers must move with their text, not stay on old rows.");
                if (i == 0) Assert.That(shift, Is.EqualTo(0f).Within(.0001f));
                else Assert.Less(shift, 0f, "The later fact really uses the added visual leading.");
            }
        }

        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        public void FullReadablePageKeepsLastInkAboveFooterAndWithinTheProtectedPanel(int width, int height)
        {
            ConfigureDisplay(width, height);
            var lines = new string[20];
            for (int i = 0; i < lines.Length; i++) lines[i] = "HHggjjpp";
            ui.OpenDocument(Document("Panel", string.Join("\n", lines)));
            Assert.AreEqual(1, ui.PageCount);
            CollectionAssert.AreEqual(lines, ui.VisibleLines);
            int top = Field("_worldTopY"), origin = Field("_worldOriginX"), panelHeight = Field("_popupH");
            Assert.LessOrEqual(panelHeight, 37, "More leading must not cover the underlying inventory action rows.");
            Rect last = TextInk(top - 8 - 19, 2, lines[19]);
            var ruleCell = new Vector3Int(origin + 2, top - (panelHeight - 4), 0);
            var ruleSprite = ui.Tilemap.GetSprite(ruleCell);
            Assert.NotNull(ruleSprite, "The footer separator must still be painted.");
            Rect rule = PaintedBounds(ruleCell, ruleSprite);
            Assert.Greater(last.yMin, rule.yMax, "The translated last line must stay above the footer separator.");
            float panelBottom = ui.Tilemap.CellToWorld(new Vector3Int(origin, top - panelHeight + 1, 0)).y;
            float panelTop = ui.Tilemap.CellToWorld(new Vector3Int(origin, top + 1, 0)).y;
            foreach (var cell in ui.Tilemap.cellBounds.allPositionsWithin)
            {
                if (!ui.Tilemap.HasTile(cell)) continue;
                Assert.That(cell.y, Is.InRange(4, CenteredPopupLayout.GridHeight - 1));
                Assert.True(ui.BgTilemap.HasTile(cell), "Each submitted foreground cell retains its opaque backdrop.");
                Rect ink = PaintedBounds(cell, ui.Tilemap.GetSprite(cell));
                Assert.GreaterOrEqual(ink.yMin + .0001f, panelBottom);
                Assert.LessOrEqual(ink.yMax - .0001f, panelTop);
            }
        }

        // Check the actual submitted glyph identities and transforms as well as
        // their alpha bounds: shrinking or omitting text must not satisfy leading.
        Rect TextInk(int row, int column, string text)
        {
            float left = float.PositiveInfinity, right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
            for (int i = 0; i < text.Length; i++)
            {
                var cell = new Vector3Int(Field("_worldOriginX") + column + i, row, 0);
                var glyph = HudPixelFont.GetTile(text[i]);
                Assert.AreSame(glyph, ui.Tilemap.GetTile(cell), "Every expected body glyph remains submitted at " + cell);
                var transform = ui.Tilemap.GetTransformMatrix(cell);
                Assert.That(transform.m00, Is.EqualTo(1.6f).Within(.0001f), "Preserve the approved body font width.");
                Assert.That(transform.m11, Is.EqualTo(1.6f).Within(.0001f), "Preserve the approved body font height.");
                Rect ink = PaintedBounds(cell, glyph.sprite);
                left = Mathf.Min(left, ink.xMin); right = Mathf.Max(right, ink.xMax);
                bottom = Mathf.Min(bottom, ink.yMin); top = Mathf.Max(top, ink.yMax);
            }
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        void ConfigureDisplay(int width, int height)
        {
            var layout = GameplayViewportLayout.Measure((float)width / height, 20f, 20f, 34, 6);
            grid.cellSize = new Vector3(CenteredPopupLayout.ComputeCellWidth(layout.GameplayAspect), 1f, 0f);
        }

        static AnnouncementDocument Document(string title, string body)
            => new AnnouncementDocument(title, "NOTICE", new[] { new AnnouncementSection("", body) });

        Tilemap Tiles(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(host.transform, false);
            return child.AddComponent<Tilemap>();
        }

        int Field(string name) => (int)typeof(AnnouncementUI).GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);

        List<Rect> InkOnRow(char character, int row)
        {
            var result = new List<Rect>();
            var glyph = HudPixelFont.GetTile(character);
            foreach (var cell in ui.Tilemap.cellBounds.allPositionsWithin)
                if (cell.y == row && ui.Tilemap.GetTile(cell) == glyph)
                    result.Add(PaintedBounds(cell, glyph.sprite));
            result.Sort((a, b) => a.xMin.CompareTo(b.xMin));
            return result;
        }

        Rect PaintedBounds(Vector3Int cell, Sprite sprite)
        {
            var rect = sprite.rect;
            var pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            int left = (int)rect.width, right = -1, bottom = (int)rect.height, top = -1;
            for (int y = 0; y < (int)rect.height; y++)
                for (int x = 0; x < (int)rect.width; x++)
                    if (pixels[y * (int)rect.width + x].a > 0f)
                    {
                        left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                    }
            Assert.GreaterOrEqual(right, left, "The sampled glyph contains visible ink.");
            float minX = (left - sprite.pivot.x) / sprite.pixelsPerUnit;
            float maxX = (right + 1 - sprite.pivot.x) / sprite.pixelsPerUnit;
            float minY = (bottom - sprite.pivot.y) / sprite.pixelsPerUnit;
            float maxY = (top + 1 - sprite.pivot.y) / sprite.pixelsPerUnit;
            var transform = ui.Tilemap.GetTransformMatrix(cell);
            var center = ui.Tilemap.GetCellCenterLocal(cell);
            var a = ui.Tilemap.transform.TransformPoint(center + transform.MultiplyPoint3x4(new Vector3(minX, minY, 0)));
            var b = ui.Tilemap.transform.TransformPoint(center + transform.MultiplyPoint3x4(new Vector3(maxX, maxY, 0)));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static void AssertSeparate(List<Rect> letters)
        {
            for (int i = 1; i < letters.Count; i++)
                Assert.Greater(letters[i].xMin, letters[i - 1].xMax + .001f,
                    "Adjacent painted glyphs need a visible gap after the actual tile transform.");
        }
    }
}
