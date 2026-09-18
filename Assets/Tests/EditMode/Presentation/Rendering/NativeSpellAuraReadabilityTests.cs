using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Native scenery must remain readable beneath persistent aura decoration,
    /// without shrinking explicit feedback or changing the aura's simulation and fog rules.</summary>
    public sealed class NativeSpellAuraReadabilityTests
    {
        readonly List<Fixture> fixtures = new List<Fixture>();
        SpellFxMode previousMode;

        [SetUp] public void Setup()
        {
            previousMode = SpellFxSettings.Mode;
            SpellFxSettings.Mode = SpellFxMode.Full;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
        }
        [TearDown] public void Teardown()
        {
            foreach (var fixture in fixtures) fixture.Dispose();
            fixtures.Clear(); AsciiFxBus.Clear(); SpellFxBus.Clear(); SpellFxSettings.Mode = previousMode;
        }

        Fixture Create(bool compact = false)
        {
            var fixture = new Fixture(); fixtures.Add(fixture); fixture.SetCompact(compact); return fixture;
        }

        [Test]
        public void CompactAuraPresentation_IsExplicitAndDefaultsToFallbackSize()
        {
            var f = Create();
            var property = typeof(AsciiFxRenderer).GetProperty("CompactAuras", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property, "The coordinator needs an explicit native aura presentation setting.");
            Assert.IsTrue(property.CanRead && property.CanWrite);
            Assert.AreEqual(typeof(bool), property.PropertyType);
            Assert.IsFalse((bool)property.GetValue(f.Renderer));
        }

        [TestCase(false, AsciiFxTheme.Fire)] [TestCase(true, AsciiFxTheme.Fire)]
        [TestCase(false, AsciiFxTheme.Campfire)] [TestCase(true, AsciiFxTheme.Campfire)]
        public void GeneratedAuraParticles_UseCompactScaleOnlyInNativeView(bool compact, AsciiFxTheme theme)
        {
            var f = Create(compact); f.StartAndEmit(theme);
            Assert.Greater(f.Renderer.ActiveParticleCount, 0, "Exercise real emitted decoration, not an empty tilemap.");
            Assert.Greater(f.Cells().Count, 0);
            foreach (var cell in f.Cells()) AssertScale(f.Tiles, cell, compact ? .35f : 1f);
            Assert.IsFalse(f.Renderer.HasBlockingFx, "Aura decoration never consumes a gameplay turn.");
        }

        [TestCase(false)] [TestCase(true)]
        public void ExplicitPlusParticle_KeepsFullScaleEvenThoughItsGlyphMatchesFireAura(bool compact)
        {
            var f = Create(compact); f.Particle(7, 8, '+'); f.Renderer.Update(0);
            AssertScale(f.Tiles, Fixture.At(7, 8), 1);
        }

        [TestCase(false)] [TestCase(true)]
        public void RealFloatingDamageNumberDigits_RetainFullScaleAndTheirRise(bool compact)
        {
            var f = Create(compact);
            AsciiFxBus.EmitFloatingNumber(f.Zone, 20, 12, 37, "&R"); f.Renderer.Update(0);
            Assert.AreEqual(2, f.Renderer.ActiveParticleCount);
            Assert.AreSame(CP437TilesetGenerator.GetTile('3'), f.Tiles.GetTile(Fixture.At(20, 11)));
            Assert.AreSame(CP437TilesetGenerator.GetTile('7'), f.Tiles.GetTile(Fixture.At(21, 11)));
            AssertScale(f.Tiles, Fixture.At(20, 11), 1); AssertScale(f.Tiles, Fixture.At(21, 11), 1);
            f.Renderer.Update(.16f);
            Assert.IsFalse(f.Tiles.HasTile(Fixture.At(20, 11)));
            AssertScale(f.Tiles, Fixture.At(20, 10), 1); AssertScale(f.Tiles, Fixture.At(21, 10), 1);
        }

        [TestCase(false)] [TestCase(true)]
        public void OffMode_RetainsTheStaticAuraStateMarkerAtPresentationScale(bool compact)
        {
            SpellFxSettings.Mode = SpellFxMode.Off;
            var f = Create(compact); f.Start(AsciiFxTheme.Fire); f.Renderer.Update(.5f);
            Assert.AreEqual(1, f.Renderer.ActiveAuraCount); Assert.AreEqual(0, f.Renderer.ActiveParticleCount);
            Assert.AreEqual(1, f.Cells().Count); AssertScale(f.Tiles, Fixture.At(10, 10), compact ? .35f : 1f);
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void NativeAuraDecoration_RemainsClippedToVisibleExploredCells(bool off, bool memory)
        {
            SpellFxSettings.Mode = off ? SpellFxMode.Off : SpellFxMode.Full;
            var f = Create(true);
            if (off) { f.Start(AsciiFxTheme.Fire); f.Renderer.Update(0); }
            else f.StartAndEmit(AsciiFxTheme.Fire);
            Assert.Greater(f.Cells().Count, 0, "Visible positive control must exist before fog changes.");
            foreach (var cell in f.Zone.Cells) { cell.IsVisible = false; cell.Explored = memory; }
            f.Renderer.Update(0); Assert.AreEqual(0, f.Cells().Count);
            Assert.AreEqual(1, f.Renderer.ActiveAuraCount, "Fog hides presentation without removing its state owner.");
            foreach (var cell in f.Zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
            f.Renderer.Update(0); Assert.Greater(f.Cells().Count, 0);
            foreach (var cell in f.Cells()) AssertScale(f.Tiles, cell, .35f);
        }

        [TestCase(AsciiFxTheme.Fire)] [TestCase(AsciiFxTheme.Campfire)]
        public void NativeUnbind_ResizesAlreadyEmittedAuraParticlesWithoutRespawningThem(AsciiFxTheme theme)
        {
            var f = Create(true); f.StartAndEmit(theme); var cells = f.Cells(); int count = f.Renderer.ActiveParticleCount;
            foreach (var cell in cells) AssertScale(f.Tiles, cell, .35f);
            f.SetCompact(false); f.Renderer.Update(0);
            CollectionAssert.AreEquivalent(cells, f.Cells()); Assert.AreEqual(count, f.Renderer.ActiveParticleCount);
            foreach (var cell in cells) AssertScale(f.Tiles, cell, 1);
            f.SetCompact(true); f.Renderer.Update(0);
            foreach (var cell in cells) AssertScale(f.Tiles, cell, .35f);
        }

        [TestCase(false)] [TestCase(true)]
        public void ClearedOrReboundTilemap_DoesNotLeakAuraMatrixIntoOrdinaryFeedback(bool rebind)
        {
            var f = Create(true); f.StartAndEmit(AsciiFxTheme.Fire); var at = f.Cells()[0]; AssertScale(f.Tiles, at, .35f);
            if (rebind) f.Renderer.SetZone(f.Zone); else f.Renderer.ClearAll();
            Assert.AreEqual(0, f.Cells().Count);
            f.SetCompact(false); f.Particle(at.x, Zone.Height - 1 - at.y, '+'); f.Renderer.Update(0);
            Assert.AreEqual(0, f.Renderer.ActiveAuraCount); AssertScale(f.Tiles, at, 1);
        }

        [Test]
        public void SameCellExplicitFeedback_ResetsAuraMatrixAndAuraReturnsCompactAfterItExpires()
        {
            var f = Create(true); f.StartAndEmit(AsciiFxTheme.Fire); var at = f.Cells()[0]; AssertScale(f.Tiles, at, .35f);
            f.Particle(at.x, Zone.Height - 1 - at.y, '7', .01f); f.Renderer.Update(0);
            Assert.AreSame(CP437TilesetGenerator.GetTile('7'), f.Tiles.GetTile(at)); AssertScale(f.Tiles, at, 1);
            f.Renderer.Update(.02f); AssertScale(f.Tiles, at, .35f);
        }

        [TestCase(AsciiFxTheme.Fire)] [TestCase(AsciiFxTheme.Campfire)]
        public void CompactPresentation_DoesNotChangeAuraRngCellsColorsLifetimeOrMotion(AsciiFxTheme theme)
        {
            var compact = Create(true); var fallback = Create(false);
            compact.StartAndEmit(theme); fallback.StartAndEmit(theme);
            CompareDecoration(compact, fallback);
            compact.Stop(theme); fallback.Stop(theme);
            compact.Renderer.Update(.04f); fallback.Renderer.Update(.04f); CompareDecoration(compact, fallback);
            compact.Renderer.Update(.12f); fallback.Renderer.Update(.12f); CompareDecoration(compact, fallback);
            compact.Renderer.Update(.5f); fallback.Renderer.Update(.5f);
            Assert.AreEqual(0, compact.Renderer.ActiveAuraCount); Assert.AreEqual(0, fallback.Renderer.ActiveAuraCount);
            Assert.AreEqual(0, compact.Renderer.ActiveParticleCount); Assert.AreEqual(0, fallback.Renderer.ActiveParticleCount);
            Assert.AreEqual(0, compact.Cells().Count); Assert.AreEqual(0, fallback.Cells().Count);
        }

        [Test]
        public void Coordinator_VisibleNativeBindingEnablesCompactAurasBeforeTheFirstCast()
        {
            using (var f = new NativeSpellFixture())
            using (var world = World(f, out var ascii, out _))
            {
                Assert.IsFalse(Compact(ascii)); world.SetNativeSurface(f.Surface);
                Assert.IsTrue(Compact(ascii)); Assert.AreEqual(0, world.ActiveSequenceCount);
            }
        }

        [Test]
        public void Coordinator_UnbindingNativeSurfaceReturnsToFullSizeFeedback()
        {
            using (var f = new NativeSpellFixture())
            using (var world = World(f, out var ascii, out _))
            {
                world.SetNativeSurface(f.Surface); Assert.IsTrue(Compact(ascii));
                world.SetNativeSurface(null); Assert.IsFalse(Compact(ascii));
            }
        }

        [TestCase(true, false)] [TestCase(false, true)] [TestCase(false, false)]
        public void Coordinator_HiddenOrAsciiOnlyViewClearsCompactSettingAndCanRestoreIt(bool sprites, bool visible)
        {
            using (var f = new NativeSpellFixture())
            using (var world = World(f, out var ascii, out _))
            {
                world.SetNativeSurface(f.Surface); Assert.IsTrue(Compact(ascii));
                world.Update(0, sprites, visible); Assert.IsFalse(Compact(ascii));
                world.Update(0, true, true); Assert.IsTrue(Compact(ascii));
            }
        }

        [Test]
        public void Coordinator_SurfaceVisibilityLossResetsScaleWithoutNeedingANewSurfaceReference()
        {
            using (var f = new NativeSpellFixture())
            using (var world = World(f, out var ascii, out _))
            {
                world.SetNativeSurface(f.Surface); Assert.IsTrue(Compact(ascii));
                f.Surface.Sync(f.Source, false, false); world.Update(0);
                Assert.IsFalse(Compact(ascii));
                f.Surface.Sync(f.Source, true, false); world.Update(0);
                Assert.IsTrue(Compact(ascii));
            }
        }

        sealed class StateAura : Effect, IAuraProvider
        {
            public override string DisplayName => "Native aura readability state";
            public AsciiFxTheme GetAuraTheme() => AsciiFxTheme.Fire;
        }
        [Test]
        public void Coordinator_OffModeKeepsACompactNativeStateMarkerAndRestoresFallbackSize()
        {
            using (var f = new NativeSpellFixture())
            {
                var entity = new Entity(); var effects = new StatusEffectsPart(); entity.AddPart(effects);
                effects.RestoreEffectsForLoad(new List<Effect> { new StateAura() }); Assert.IsTrue(f.Zone.AddEntity(entity, 10, 10));
                using (var world = World(f, out var ascii, out var tiles))
                {
                    world.SetNativeSurface(f.Surface); SpellFxSettings.Mode = SpellFxMode.Off; world.Update(0);
                    Assert.IsTrue(Compact(ascii)); Assert.AreEqual(1, ascii.ActiveAuraCount);
                    Assert.AreEqual(0, ascii.ActiveParticleCount); AssertScale(tiles, Fixture.At(10, 10), .35f);
                    world.Update(0, false, true);
                    Assert.IsFalse(Compact(ascii)); Assert.AreEqual(1, ascii.ActiveAuraCount);
                    AssertScale(tiles, Fixture.At(10, 10), 1);
                }
            }
        }

        static WorldFxCoordinator World(NativeSpellFixture f, out AsciiFxRenderer ascii, out Tilemap tiles)
        {
            var go = new GameObject("Coordinated aura tiles", typeof(Tilemap)); go.transform.SetParent(f.Host.transform, false);
            tiles = go.GetComponent<Tilemap>(); ascii = new AsciiFxRenderer(tiles);
            var world = new WorldFxCoordinator(ascii, f.Host.transform, f.Library); world.SetZone(f.Zone); return world;
        }
        static bool Compact(AsciiFxRenderer renderer) => (bool?)typeof(AsciiFxRenderer)
            .GetProperty("CompactAuras", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(renderer) ?? false;

        static void CompareDecoration(Fixture compact, Fixture fallback)
        {
            CollectionAssert.AreEquivalent(fallback.Cells(), compact.Cells());
            Assert.AreEqual(fallback.Renderer.ActiveParticleCount, compact.Renderer.ActiveParticleCount);
            foreach (var at in fallback.Cells())
            {
                Assert.AreSame(fallback.Tiles.GetTile(at), compact.Tiles.GetTile(at));
                Assert.AreEqual(fallback.Tiles.GetColor(at), compact.Tiles.GetColor(at));
                AssertScale(compact.Tiles, at, .35f); AssertScale(fallback.Tiles, at, 1);
            }
        }

        static void AssertScale(Tilemap tiles, Vector3Int cell, float expected)
        {
            Assert.IsTrue(tiles.HasTile(cell), "Scale assertions require a drawn tile at " + cell);
            var matrix = tiles.GetTransformMatrix(cell);
            var wanted = Matrix4x4.Scale(new Vector3(expected, expected, 1));
            for (int row = 0; row < 4; row++) for (int column = 0; column < 4; column++)
                Assert.That(matrix[row, column], Is.EqualTo(wanted[row, column]).Within(.0001f),
                    "Cell " + cell + " matrix[" + row + "," + column + "] must preserve the cell centre and only shrink aura art.");
        }

        sealed class Fixture : IDisposable
        {
            readonly GameObject root;
            readonly Entity anchor;
            public readonly Tilemap Tiles;
            public readonly Zone Zone;
            public readonly AsciiFxRenderer Renderer;
            public Fixture()
            {
                root = new GameObject("Native aura readability fixture", typeof(Grid));
                var map = new GameObject("Aura tiles", typeof(Tilemap)); map.transform.SetParent(root.transform, false);
                Tiles = map.GetComponent<Tilemap>(); Renderer = new AsciiFxRenderer(Tiles);
                Zone = new Zone("NativeAuraReadability-" + Guid.NewGuid().ToString("N"));
                foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
                anchor = new Entity { ID = Guid.NewGuid().ToString("N") }; Assert.IsTrue(Zone.AddEntity(anchor, 10, 10));
                Renderer.SetZone(Zone);
            }
            public void SetCompact(bool value)
            {
                // Before production exists, leave the renderer unchanged so RED is
                // the real full-size rendered matrix, not a reflection exception.
                typeof(AsciiFxRenderer).GetProperty("CompactAuras", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(Renderer, value);
            }
            public void Start(AsciiFxTheme theme) => Renderer.AcceptRequest(new AsciiFxRequest { Type = AsciiFxRequestType.AuraStart, Zone = Zone, Anchor = anchor, Theme = theme });
            public void Stop(AsciiFxTheme theme) => Renderer.AcceptRequest(new AsciiFxRequest { Type = AsciiFxRequestType.AuraStop, Zone = Zone, Anchor = anchor, Theme = theme });
            public void StartAndEmit(AsciiFxTheme theme) { Start(theme); Renderer.Update(.14f); Renderer.Update(.02f); }
            public void Particle(int x, int y, char glyph, float lifetime = .5f) => Renderer.AcceptRequest(new AsciiFxRequest { Type = AsciiFxRequestType.Particle, Zone = Zone, X = x, Y = y, Glyph = glyph, ColorString = "&R", Lifetime = lifetime });
            public List<Vector3Int> Cells()
            {
                var result = new List<Vector3Int>();
                foreach (var cell in Tiles.cellBounds.allPositionsWithin) if (Tiles.HasTile(cell)) result.Add(cell);
                return result;
            }
            public static Vector3Int At(int x, int y) => new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0);
            public void Dispose() { Renderer.ClearAll(); Object.DestroyImmediate(root); }
        }
    }
}
