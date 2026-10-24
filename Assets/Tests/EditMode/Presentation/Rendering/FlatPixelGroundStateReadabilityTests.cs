using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Bounded tests of the shipping ground-mark painter and its real
    /// flat-mode admission predicate. The catalog supplies a positive water-ground
    /// control; these do not claim to exercise full world generation or camera output.</summary>
    public sealed class FlatPixelGroundStateReadabilityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int X = 4, Y = 5;
        private static readonly Vector3Int At = new Vector3Int(X, Zone.Height - 1 - Y, 0);
        private static readonly Color WaterColor = new Color(.30f, .55f, .95f);
        private GameObject _root;
        private ZoneRenderer _renderer;
        private EnvironmentSpriteRenderer _environment;
        private Tilemap _marks;
        private Zone _zone;
        private FlatPixelGreyboxCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Flat ground mark unit", typeof(Grid));
            _root.SetActive(false);
            _root.GetComponent<Grid>().cellSize = new Vector3(1, 1.5f, 0);
            var host = new GameObject("Ground state tiles", typeof(Tilemap));
            host.transform.SetParent(_root.transform, false);
            _marks = host.GetComponent<Tilemap>();
            _renderer = host.AddComponent<ZoneRenderer>();
            _environment = host.AddComponent<EnvironmentSpriteRenderer>();
            _environment.FlatPixelGreyboxEnabled = true;
            _zone = new Zone(ReferenceGladePlan.ZoneID);
            SetZone(_zone);
            Set("_tileStateTilemap", _marks);
            Set("_envSpriteRenderer", _environment);
            Set("_flatPixelModeEnabled", true);
            _catalog = new FlatPixelGreyboxCatalog();
            Assert.That(_renderer.FlatPixelModeActive, Is.True, "Require actual admitted flat-mode ownership.");
        }

        [TearDown]
        public void TearDown()
        {
            _catalog?.Dispose();
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [TestCase(6)]
        [TestCase(ZoneTileState.Permanent)]
        public void SoleLiveWaterUsesCatalogGroundWithoutLegacyStripes(int turns)
        {
            Water(turns);
            AssertCatalogWater();
            Paint();
            Assert.That(_marks.HasTile(At), Is.False, "The flat catalog already depicts this water.");
        }

        [TestCase("mode-off")]
        [TestCase("sprites-off")]
        [TestCase("foreign-zone")]
        public void SameWaterKeepsStripesWhenFlatGroundDoesNotRepresentIt(string loss)
        {
            if (loss == "mode-off") Set("_flatPixelModeEnabled", false);
            else if (loss == "sprites-off") _environment.RenderingEnabled = false;
            else SetZone(new Zone("Overworld.12.10.0"));
            Assert.That(_renderer.FlatPixelModeActive, Is.EqualTo(loss == "foreign-zone"));
            Water();
            Paint();
            AssertMark('~', WaterColor);
        }

        [TestCase("charge", '*')]
        [TestCase("heat", '^')]
        [TestCase("cold", '^')]
        [TestCase("cloud", '\'')]
        [TestCase("veil-mist", '\'')]
        [TestCase("embers", '"')]
        public void WaterGroundDoesNotHideAnotherLiveHazard(string hazard, char expected)
        {
            Water();
            switch (hazard)
            {
                case "charge": _zone.TileState.AddCharge(X, Y, 3); break;
                case "heat": _zone.TileState.AddHeat(X, Y, 3); break;
                case "cold": _zone.TileState.AddCold(X, Y, 3); break;
                case "cloud": _zone.TileState.WriteCloud(X, Y, "steam", 3); break;
                case "veil-mist": _zone.TileState.WriteCloud(X, Y, "veil-mist", 3); break;
                case "embers": _zone.TileState.WriteResidue(X, Y, "embers", 3); break;
            }
            AssertCatalogWater();
            Paint();
            Assert.That(_marks.GetTile(At), Is.SameAs(CP437TilesetGenerator.GetTile(expected)));
            Assert.That(_marks.GetColor(At), Is.Not.EqualTo(WaterColor));
        }

        [TestCase("oil", false)]
        [TestCase("ice", false)]
        [TestCase("acid", false)]
        [TestCase("oil", true)]
        [TestCase("ice", true)]
        [TestCase("acid", true)]
        public void NonwaterAndMixedCoatingsKeepTheirFallbackCue(string liquid, bool mixed)
        {
            if (mixed) Water();
            _zone.TileState.WriteCoating(X, Y, liquid, 7);
            Paint();
            Color color = liquid == "oil" ? new Color(.35f, .25f, .45f)
                : liquid == "ice" ? new Color(.85f, .95f, 1) : WaterColor;
            AssertMark('~', color);
        }

        [Test]
        public void RepeatedModeChangesEraseOldWaterMarksWithoutChangingWorldState()
        {
            Water();
            var state = _zone.TileState.Get(X, Y);
            string before = _zone.TileState.ToSaveString();
            int writes = 0;
            _zone.TileState.OnCellChanged = (x, y) => writes++;
            for (int visit = 0; visit < 2; visit++)
            {
                Set("_flatPixelModeEnabled", false);
                Paint();
                AssertMark('~', WaterColor);
                Set("_flatPixelModeEnabled", true);
                Assert.That(_renderer.FlatPixelModeActive, Is.True);
                Paint();
                Assert.That(_marks.HasTile(At), Is.False);
            }
            Assert.That(_zone.TileState.Get(X, Y), Is.SameAs(state));
            Assert.That(_zone.TileState.ToSaveString(), Is.EqualTo(before));
            Assert.That(writes, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FogClearsExistingHazardAndVisibleReentryRestoresIt(bool explored)
        {
            Water();
            _zone.TileState.AddCharge(X, Y, 4);
            Paint();
            Assert.That(_marks.GetTile(At), Is.SameAs(CP437TilesetGenerator.GetTile('*')));
            _zone.GetCell(X, Y).IsVisible = false;
            _zone.GetCell(X, Y).Explored = explored;
            Paint();
            Assert.That(_marks.HasTile(At), Is.False);
            _zone.GetCell(X, Y).Explored = true;
            _zone.GetCell(X, Y).IsVisible = true;
            Paint();
            Assert.That(_marks.GetTile(At), Is.SameAs(CP437TilesetGenerator.GetTile('*')));
        }

        private void Water(int turns = 6) => _zone.TileState.WriteCoating(X, Y, "water", turns);
        private void AssertCatalogWater()
        {
            Assert.That(_catalog.TryResolve(_zone, X, Y, out var cell), Is.True);
            Assert.That(cell.GroundTile, Is.Not.Null);
            Assert.That(cell.GroundKind, Is.EqualTo(FlatPixelGreyboxGround.Water));
            Assert.That(cell.GroundColor, Is.EqualTo((Color)new Color32(12, 67, 68, 255)));
        }
        private void AssertMark(char glyph, Color color)
        {
            Assert.That(_marks.GetTile(At), Is.SameAs(CP437TilesetGenerator.GetTile(glyph)));
            Assert.That(_marks.GetColor(At), Is.EqualTo(color));
            Assert.That(_marks.GetTransformMatrix(At), Is.EqualTo(Matrix4x4.identity));
        }
        private void SetZone(Zone zone)
        {
            _zone = zone;
            typeof(ZoneRenderer).GetProperty("CurrentZone").GetSetMethod(true).Invoke(_renderer, new object[] { zone });
            zone.GetCell(X, Y).Explored = true;
            zone.GetCell(X, Y).IsVisible = true;
        }
        private void Set(string field, object value) => typeof(ZoneRenderer).GetField(field, Private).SetValue(_renderer, value);
        private void Paint() => typeof(ZoneRenderer).GetMethod("PaintTileStateMark", Private)
            .Invoke(_renderer, new object[] { X, Y, At, _zone.GetCell(X, Y) });
    }
}
