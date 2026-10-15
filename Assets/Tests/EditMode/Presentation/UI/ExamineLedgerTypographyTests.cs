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
