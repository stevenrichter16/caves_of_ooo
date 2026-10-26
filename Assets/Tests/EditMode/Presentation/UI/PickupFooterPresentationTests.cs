using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Presentation only: real pickup draw/clear paths, with owned Tilemaps.
    /// Existing command/input fixtures cover transfer semantics and key dispatch.</summary>
    public sealed class PickupFooterPresentationTests
    {
        const int Width = 46;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<GameObject> _objects = new List<GameObject>();
        PickupUI _ui;

        T Component<T>() where T : Component
        {
            var host = new GameObject(typeof(T).Name);
            _objects.Add(host);
            return host.AddComponent<T>();
        }

        Tilemap Map()
        {
            var grid = Component<Grid>();
            var map = Component<Tilemap>();
            map.transform.SetParent(grid.transform);
            return map;
        }

        [SetUp]
        public void SetUp()
        {
            _ui = Component<PickupUI>();
            _ui.Tilemap = Map(); _ui.BgTilemap = Map();
            Assert.NotNull(CP437TilesetGenerator.GetUiTile('['), "Real UI glyphs must be available.");
            Assert.NotNull(CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var host in _objects) if (host != null) Object.DestroyImmediate(host);
            _objects.Clear();
        }

        static List<Entity> Items(int count)
        {
            return Enumerable.Range(0, count).Select(i =>
            {
                var item = new Entity { BlueprintName = "FooterItem" + i };
                item.AddPart(new RenderPart { DisplayName = "Item " + i, RenderString = "!", ColorString = "&y" });
                return item;
            }).ToList();
        }

        object Get(string name) => typeof(PickupUI).GetField(name, Private).GetValue(_ui);
        void Set(string name, object value) => typeof(PickupUI).GetField(name, Private).SetValue(_ui, value);
        void Render() => typeof(PickupUI).GetMethod("Render", Private).Invoke(_ui, null);
        Vector3Int Cell(int x, int y) => new Vector3Int((int)Get("_worldOriginX") + x, (int)Get("_worldTopY") - y, 0);
        static HashSet<Vector3Int> Painted(Tilemap map)
        {
            var result = new HashSet<Vector3Int>();
            foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) result.Add(p);
            return result;
        }

        void AssertBacking(int count)
        {
            int visible = Math.Min(count, 20), height = visible + 6;
            Assert.AreEqual(height, Get("_popupH"), "Existing popup geometry must not change.");
            Assert.AreEqual(Width * height, Painted(_ui.BgTilemap).Count);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < Width; x++)
                {
                    Assert.True(_ui.BgTilemap.HasTile(Cell(x, y)), "Missing black backing at " + x + "," + y);
                    Assert.AreEqual(new Color(0, 0, 0, 1), _ui.BgTilemap.GetColor(Cell(x, y)));
                }
            Assert.False(_ui.BgTilemap.HasTile(Cell(0, height)), "Do not cover world below the owned footprint.");
            Assert.False(_ui.BgTilemap.HasTile(Cell(-1, height - 1)));
            Assert.False(_ui.BgTilemap.HasTile(Cell(Width, height - 1)));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(20)]
        [TestCase(26)]
        public void FooterAndReservedStatusHaveOpaqueBackingForSmallFullAndScrolledLists(int count)
        {
            _ui.Open(Items(count));
            if (count > 20) { Set("_scrollOffset", count - 20); Set("_cursorIndex", count - 1); Render(); }
            AssertBacking(count);
            int footer = Math.Min(count, 20) + 4;
            Assert.AreSame(CP437TilesetGenerator.GetUiTile('['), _ui.Tilemap.GetTile(Cell(1, footer)));
            Assert.AreEqual(QudColorParser.Gray, _ui.Tilemap.GetColor(Cell(1, footer)),
                "Actionable hints use the same light gray as the backed action modal.");
            Assert.AreEqual(QudColorParser.DarkGray, _ui.Tilemap.GetColor(Cell(32, 1)),
                "The secondary title hint keeps its original hierarchy.");
        }

        [Test]
        public void StatusRowIsBackedAndLongRefusalStaysInsideTheExistingWidth()
        {
            _ui.Open(Items(3));
            Set("_statusMessage", new string('X', 100)); Render();
            AssertBacking(3);
            Assert.AreSame(CP437TilesetGenerator.GetUiTile('X'), _ui.Tilemap.GetTile(Cell(44, 8)));
            Assert.AreEqual(QudColorParser.BrightRed, _ui.Tilemap.GetColor(Cell(1, 8)));
            Assert.False(_ui.Tilemap.HasTile(Cell(45, 8)), "Refusal text is clipped before the right edge.");
            Set("_statusMessage", null); Render();
            Assert.False(_ui.Tilemap.HasTile(Cell(1, 8)), "Cleared refusal text must not linger.");
            Assert.True(_ui.BgTilemap.HasTile(Cell(1, 8)), "The reserved status backing stays stable.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReaderHideRestoreAndCloseClearTheWholeOwnedFooterWithoutTouchingOtherTiles(bool container)
        {
            var items = Items(3);
            var chest = container ? new Entity { BlueprintName = "FooterContainer" } : null;
            var sentinel = new Vector3Int(-100, -100, 0);
            var tile = CP437TilesetGenerator.GetUiTile('!');
            _ui.Tilemap.SetTile(sentinel, tile); _ui.BgTilemap.SetTile(sentinel, tile);
            _ui.Open(items, chest);
            var fg = Painted(_ui.Tilemap); var bg = Painted(_ui.BgTilemap);
            _ui.HideForDecisionReader();
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(_ui.Tilemap));
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(_ui.BgTilemap));
            Assert.True(_ui.IsOpen); CollectionAssert.AreEqual(items, (List<Entity>)Get("_items"));
            Assert.AreSame(chest, Get("_sourceContainer"));
            _ui.RestoreAfterDecisionReader();
            CollectionAssert.AreEquivalent(fg, Painted(_ui.Tilemap));
            CollectionAssert.AreEquivalent(bg, Painted(_ui.BgTilemap));
            _ui.Close(); _ui.RestoreAfterDecisionReader();
            Assert.False(_ui.IsOpen);
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(_ui.Tilemap));
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(_ui.BgTilemap));
        }

        [Test]
        public void LargeToSmallRedrawClearsPreviousFooterRatherThanLeavingBlackStrips()
        {
            _ui.Open(Items(26));
            var oldFg = Painted(_ui.Tilemap); var oldBg = Painted(_ui.BgTilemap);
            int oldTop = (int)Get("_worldTopY");
            _ui.Open(Items(1));
            Assert.AreNotEqual(oldTop, Get("_worldTopY"), "The centered origin actually moves.");
            int x = (int)Get("_worldOriginX"), top = (int)Get("_worldTopY"), h = (int)Get("_popupH");
            Func<Vector3Int, bool> outside = p => p.x < x || p.x >= x + Width || p.y > top || p.y <= top - h;
            Assert.True(oldBg.Any(outside));
            foreach (var p in oldFg.Where(outside)) Assert.False(_ui.Tilemap.HasTile(p));
            foreach (var p in oldBg.Where(outside)) Assert.False(_ui.BgTilemap.HasTile(p));
            AssertBacking(1);
        }

        [Test]
        public void OptionalMissingBackgroundStillAllowsNormalForegroundAndClose()
        {
            _ui.BgTilemap = null;
            _ui.Open(Items(3));
            Assert.True(_ui.IsOpen);
            Assert.AreSame(CP437TilesetGenerator.GetUiTile('['), _ui.Tilemap.GetTile(Cell(1, 7)));
            _ui.HideForDecisionReader(); Assert.IsEmpty(Painted(_ui.Tilemap));
            _ui.RestoreAfterDecisionReader(); Assert.IsNotEmpty(Painted(_ui.Tilemap));
            _ui.Close(); Assert.IsEmpty(Painted(_ui.Tilemap));
        }
    }
}
