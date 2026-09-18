using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Bounded PaintTileStateMark unit tests. Ready presenter fields are
    /// supplied explicitly around a real borrowed native surface, without generating
    /// a whole town. These exercise the actual visibility/ClaimsCell predicates and
    /// tilemap output; they do not claim to verify authored world generation.</summary>
    public sealed class NativeGroundStateReadabilityTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const int X = 40, Y = 12;
        static readonly Vector3Int At = new Vector3Int(X, Zone.Height - 1 - Y, 0);
        static readonly string[] States = { "charge", "water", "oil", "heat", "cold", "embers", "cloud" };
        static readonly char[] Glyphs = { '*', '~', '~', '^', '^', '"', '\'' };
        static readonly Color[] Colors = { new Color(1, .95f, .35f), new Color(.30f, .55f, .95f),
            new Color(.35f, .25f, .45f), new Color(1, .35f, .15f), new Color(.65f, .90f, 1),
            new Color(1, .45f, .10f), new Color(.80f, .80f, .85f) };
        bool oldEnabled;
        SpellFxMode oldMode;
        Action<string> oldFullDirty;

        [SetUp] public void Setup()
        {
            oldEnabled = Village3DSettings.Enabled; oldMode = SpellFxSettings.Mode;
            oldFullDirty = ZoneRenderHooks.FullDirtyCallback; ZoneRenderHooks.FullDirtyCallback = null;
            Village3DSettings.Enabled = true; SpellFxSettings.Mode = SpellFxMode.Full;
        }
        [TearDown] public void Teardown()
        {
            Village3DSettings.Enabled = oldEnabled; SpellFxSettings.Mode = oldMode;
            ZoneRenderHooks.FullDirtyCallback = oldFullDirty;
        }

        [TestCase(false)] [TestCase(true)]
        public void VisibleNativeGroundStates_KeepTheirGlyphAndTintInACompactCellCorner(bool ring)
        {
            using (var f = new Fixture(ring))
            {
                Assert.IsTrue(f.Claims(), "The actual native visibility predicate must accept this fixture.");
                for (int i = 0; i < States.Length; i++)
                {
                    f.Write(States[i]); f.Paint(); AssertMark(f, Glyphs[i], Colors[i], true);
                }
            }
        }

        [TestCase("none")] [TestCase("felling-sprites")] [TestCase("morrowfast-sprites")]
        public void FallbackAndAuthoredTwoDimensionalScenes_KeepFullCellMarkers(string view)
        {
            using (var f = new Fixture(false))
            {
                Set(f.Renderer, "_village3DPresenter", null);
                if (view != "none")
                {
                    bool felling = view == "felling-sprites";
                    var type = felling ? typeof(FellingScenePresenter) : typeof(MorrowfastScenePresenter);
                    var presenter = (MonoBehaviour)f.Native.Host.AddComponent(type);
                    Property(presenter, "CurrentZone", f.Zone); Property(presenter, "IsReady", true);
                    Set(f.Renderer, felling ? "_fellingScenePresenter" : "_morrowfastScenePresenter", presenter);
                    Assert.IsTrue((bool)type.GetMethod("ClaimsCell").Invoke(presenter, new object[] { X, Y }),
                        "An authored 2D positive claim must not accidentally authorize 3D marker scaling.");
                }
                for (int i = 0; i < States.Length; i++) { f.Write(States[i]); f.Paint(); AssertMark(f, Glyphs[i], Colors[i], false); }
            }
        }

        [TestCase(false, "hidden")] [TestCase(false, "disabled-mode")] [TestCase(true, "wrong-zone")]
        public void NativeReadinessLoss_RepaintsExistingCellAtIdentity(bool ring, string loss)
        {
            using (var f = new Fixture(ring))
            {
                Assert.IsTrue(f.Claims()); f.Write("charge"); f.Paint(); AssertTransform(f, true);
                string before = f.State(); var sourceState = f.Zone.TileState.Get(X, Y); int writes = 0;
                f.Zone.TileState.OnCellChanged = (x, y) => writes++;
                switch (loss)
                {
                    case "hidden": Set(f.Presenter, "requestedVisible", false); break;
                    case "disabled-mode": Village3DSettings.Enabled = false; break;
                    case "wrong-zone": Property(f.Presenter, "CurrentZone", new Zone(f.Zone.ZoneID)); break;
                }
                if (loss != "wrong-zone") Assert.IsFalse(f.Claims());
                else Assert.IsTrue(f.Claims(), "A stale presenter can still claim cells, so zone identity must also be checked.");
                f.Paint(); AssertTransform(f, false);
                Assert.AreSame(sourceState, f.Zone.TileState.Get(X, Y)); Assert.AreEqual(before, f.State()); Assert.AreEqual(0, writes);
            }
        }

        [TestCase(false, false)] [TestCase(true, true)]
        public void HiddenOrUnexploredState_RemainsInvisibleAndReappearsCompactWithoutChangingGameplay(bool ring, bool explored)
        {
            using (var f = new Fixture(ring))
            {
                f.Write("heat"); string before = f.State(); f.Paint(); AssertTransform(f, true);
                f.Zone.GetCell(X, Y).IsVisible = false; f.Zone.GetCell(X, Y).Explored = explored; f.Paint();
                Assert.IsFalse(f.Marks.HasTile(At)); Assert.AreEqual(before, f.State());
                f.Zone.GetCell(X, Y).IsVisible = true; f.Zone.GetCell(X, Y).Explored = true; f.Paint();
                AssertTransform(f, true); Assert.AreEqual(before, f.State());
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void RemovingAndRewritingGroundState_DoesNotLeaveAGhostOrAStaleTransform(bool ring)
        {
            using (var f = new Fixture(ring))
            {
                f.Write("water"); f.Paint(); AssertTransform(f, true);
                Assert.AreEqual(1, f.Zone.TileState.Clear(X, Y)); f.Paint(); Assert.IsFalse(f.Marks.HasTile(At));
                Assert.AreEqual(0, f.Zone.TileState.WrittenCount);
                Set(f.Presenter, "requestedVisible", false); f.Write("charge"); f.Paint(); AssertTransform(f, false);
                Set(f.Presenter, "requestedVisible", true); f.Paint(); AssertTransform(f, true);
            }
        }

        [TestCase(false)]
        public void RepaintingMixedGroundState_PreservesLayerPriorityIdentityDurationsAndCallbacks(bool ring)
        {
            using (var f = new Fixture(ring))
            {
                f.Zone.TileState.WriteResidue(X, Y, "embers", 4); f.Zone.TileState.WriteCoating(X, Y, "water", 6);
                f.Zone.TileState.WriteCoating(X, Y, "oil", 9); f.Zone.TileState.AddHeat(X, Y, 3);
                f.Zone.TileState.AddCold(X, Y, 2); f.Zone.TileState.AddCharge(X, Y, 7); f.Zone.TileState.WriteCloud(X, Y, "steam", 5);
                var state = f.Zone.TileState.Get(X, Y); string before = f.State(); int writes = 0;
                f.Zone.TileState.OnCellChanged = (x, y) => writes++;
                for (int i = 0; i < 5; i++) { f.Paint(); AssertMark(f, '"', Colors[5], true); }
                Assert.AreSame(state, f.Zone.TileState.Get(X, Y)); Assert.AreEqual(before, f.State()); Assert.AreEqual(0, writes);
                Assert.AreEqual(7, f.Zone.TileState.CountLayers(X, Y)); Assert.AreEqual(1, f.Zone.TileState.WrittenCount);
            }
        }

        [TestCase(true)]
        public void TransientEffectModeDoesNotRemoveOrEnlargePersistentNativeGroundState(bool ring)
        {
            using (var f = new Fixture(ring))
            {
                f.Write("charge"); string before = f.State();
                foreach (var mode in new[] { SpellFxMode.Full, SpellFxMode.Reduced, SpellFxMode.Off })
                { SpellFxSettings.Mode = mode; f.Paint(); AssertTransform(f, true); Assert.AreEqual(before, f.State()); }
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void SameZoneCameraLossAndRecovery_InvalidatesExistingMarksWithoutAWorldWrite(bool ring)
        {
            using (var f = new Fixture(ring))
            {
                f.Sync(); Assert.IsTrue(f.Claims()); f.Write("charge"); f.Paint();
                string before = f.State(); int writes = 0; f.Zone.TileState.OnCellChanged = (x, y) => writes++;
                Set(f.Renderer, "_fullDirty", false); f.Sync(); Assert.IsFalse(f.Dirty, "An unchanged surface must not request a full redraw every idle frame.");
                Set(f.Renderer, "_mainCamera", null); f.Sync(); Assert.IsFalse(f.Claims());
                Assert.IsTrue(f.Dirty, "Losing the borrowed source changes the meaning of already drawn markers without a gameplay dirty cell.");
                f.Paint(); AssertTransform(f, false);
                Set(f.Renderer, "_fullDirty", false); f.Sync(); Assert.IsFalse(f.Dirty);
                Set(f.Renderer, "_mainCamera", f.Native.Source); f.Sync(); Assert.IsTrue(f.Claims());
                Assert.IsTrue(f.Dirty, "Recovery must repaint full-size fallback marks into native corner markers.");
                f.Paint(); AssertTransform(f, true); Assert.AreEqual(before, f.State()); Assert.AreEqual(0, writes);
                Set(f.Renderer, "_fullDirty", false); f.Sync(); Assert.IsFalse(f.Dirty);
            }
        }

        static void AssertMark(Fixture f, char glyph, Color color, bool compact)
        { Assert.AreSame(CP437TilesetGenerator.GetTile(glyph), f.Marks.GetTile(At)); Assert.AreEqual(color, f.Marks.GetColor(At)); AssertTransform(f, compact); }
        static void AssertTransform(Fixture f, bool compact)
        {
            Assert.IsTrue(f.Marks.HasTile(At), "Require a real drawn state marker before inspecting its matrix.");
            var expected = compact ? Matrix4x4.TRS(new Vector3(.28f, .28f, 0), Quaternion.identity, new Vector3(.35f, .35f, 1)) : Matrix4x4.identity;
            var actual = f.Marks.GetTransformMatrix(At);
            for (int row = 0; row < 4; row++) for (int column = 0; column < 4; column++)
                Assert.That(actual[row, column], Is.EqualTo(expected[row, column]).Within(.0001f), "Marker matrix[" + row + "," + column + "]");
        }
        static void Set(object target, string field, object value)
        { var info = target.GetType().GetField(field, Private); Assert.NotNull(info, field); info.SetValue(target, value); }
        static void Property(object target, string name, object value)
        { var info = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public); Assert.NotNull(info, name); info.GetSetMethod(true).Invoke(target, new[] { value }); }

        sealed class Fixture : IDisposable
        {
            public readonly NativeSpellFixture Native;
            public readonly ZoneRenderer Renderer;
            public readonly Tilemap Marks;
            public readonly MonoBehaviour Presenter;
            readonly GameObject root;
            public Zone Zone => Native.Zone;
            public bool Dirty => (bool)typeof(ZoneRenderer).GetField("_fullDirty", Private).GetValue(Renderer);
            public Fixture(bool ring)
            {
                Native = new NativeSpellFixture();
                root = new GameObject("Ground mark paint unit", typeof(Grid)); root.SetActive(false);
                var go = new GameObject("Ground state tiles", typeof(Tilemap)); go.transform.SetParent(root.transform, false);
                Marks = go.GetComponent<Tilemap>(); Renderer = go.AddComponent<ZoneRenderer>();
                Property(Renderer, "CurrentZone", Native.Zone); Set(Renderer, "_tileStateTilemap", Marks); Set(Renderer, "_mainCamera", Native.Source);
                Presenter = (MonoBehaviour)Native.Host.AddComponent(ring ? typeof(SpawnRing3DPresenter) : typeof(Village3DPresenter));
                Property(Presenter, "CurrentZone", Native.Zone); Property(Presenter, "IsReady", true);
                Set(Presenter, "source", Native.Source); Set(Presenter, "surface", Native.Surface);
                Set(Renderer, ring ? "_spawnRing3DPresenter" : "_village3DPresenter", Presenter);
                Assert.IsTrue(Native.Surface.IsVisible); Assert.IsTrue(Claims());
            }
            public bool Claims() => (bool)Presenter.GetType().GetMethod("ClaimsCell").Invoke(Presenter, new object[] { X, Y });
            public void Sync() => typeof(ZoneRenderer).GetMethod("SyncVillagePresentation", Private).Invoke(Renderer, new object[] { true });
            public void Paint() => typeof(ZoneRenderer).GetMethod("PaintTileStateMark", Private).Invoke(Renderer, new object[] { X, Y, At, Zone.GetCell(X, Y) });
            public string State() => JsonUtility.ToJson(Zone.TileState.Get(X, Y));
            public void Write(string state)
            {
                Zone.TileState.Clear(X, Y);
                switch (state)
                {
                    case "charge": Zone.TileState.AddCharge(X, Y, 3); break;
                    case "water": Zone.TileState.WriteCoating(X, Y, "water", 6); break;
                    case "oil": Zone.TileState.WriteCoating(X, Y, "oil", 7); break;
                    case "heat": Zone.TileState.AddHeat(X, Y, 4); break;
                    case "cold": Zone.TileState.AddCold(X, Y, 5); break;
                    case "embers": Zone.TileState.WriteResidue(X, Y, "embers", 4); break;
                    case "cloud": Zone.TileState.WriteCloud(X, Y, "steam", 5); break;
                    default: throw new ArgumentOutOfRangeException(nameof(state));
                }
            }
            public void Dispose()
            {
                // Surface lifetime belongs to NativeSpellFixture, not these manually supplied presenter fields.
                Set(Presenter, "surface", null);
                Set(Renderer, "_village3DPresenter", null); Set(Renderer, "_spawnRing3DPresenter", null);
                Set(Renderer, "_fellingScenePresenter", null); Set(Renderer, "_morrowfastScenePresenter", null);
                Object.DestroyImmediate(root); Native.Dispose();
            }
        }
    }
}
