using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class RectangularSpellFxProjectionTests
    {
        private GameObject _gridRoot, _fxParent;
        private Grid _grid;
        private Tilemap _tilemap;
        private SpriteSpellFxRenderer _sprites;
        private WorldFxCoordinator _world;
        private Zone _zone;
        private SpellFxMode _savedMode;
        private float _savedSpeed;

        [SetUp]
        public void SetUp()
        {
            _savedMode = SpellFxSettings.Mode;
            _savedSpeed = SpellFxSettings.AnimationSpeed;
            SpellFxSettings.Mode = SpellFxMode.Full;
            SpellFxSettings.AnimationSpeed = 1f;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            _gridRoot = new GameObject("Spell projection grid", typeof(Grid));
            _grid = _gridRoot.GetComponent<Grid>();
            _grid.cellSize = new Vector3(1f, 1.5f, 1f);
            _gridRoot.transform.position = new Vector3(3.25f, -4.5f, 7f);
            var map = new GameObject("Projected spell cells", typeof(Tilemap));
            map.transform.SetParent(_gridRoot.transform, false);
            map.transform.localPosition = new Vector3(-1f, 2.25f, 3f);
            _tilemap = map.GetComponent<Tilemap>();
            _fxParent = new GameObject("Unrelated spell parent");
            _fxParent.transform.position = new Vector3(61f, -19f, 4f);
            _zone = new Zone("RectangularSpellProjection");
            Reveal();
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _sprites?.Dispose();
            Object.DestroyImmediate(_fxParent);
            Object.DestroyImmediate(_gridRoot);
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            SpellFxSettings.Mode = _savedMode;
            SpellFxSettings.AnimationSpeed = _savedSpeed;
        }

        [Test]
        public void ProjectedCast_UsesTilemapWorldCenterInsteadOfFxParentLocalUnits()
        {
            CreateProjectedSprites();
            PlayCast();
            AssertPosition(Center(5, 5), Drawn().Single().transform.position);
            Assert.AreEqual(Vector3.one, Drawn().Single().transform.localScale,
                "Rectangular cell spacing must not stretch the existing square pixels.");
        }

        [Test]
        public void OmittedTilemap_PreservesLegacyParentLocalCellCoordinates()
        {
            _sprites = new SpriteSpellFxRenderer(_fxParent.transform);
            _sprites.SetZone(_zone);
            PlayCast();
            var expected = _fxParent.transform.TransformPoint(new Vector3(5.5f, Zone.Height - 5.5f, -.005f));
            AssertPosition(expected, Drawn().Single().transform.position);
            Assert.Greater(Vector3.Distance(Center(5, 5), expected), 1f,
                "The control parent intentionally differs from the projection source.");
        }

        [Test]
        public void ActiveCast_ReprojectsWithoutAllocatingWhenGridGeometryChanges()
        {
            CreateProjectedSprites(); PlayCast();
            var original = Drawn().Single();
            _grid.cellSize = new Vector3(1f, 2f, 1f);
            _tilemap.transform.localPosition += new Vector3(2f, -3f, 0f);
            _sprites.Update(0f);
            Assert.AreSame(original, Drawn().Single());
            AssertPosition(Center(5, 5), original.transform.position);
        }

        [Test]
        public void PooledCast_UsesCurrentProjectionAndReleasesOnPresentationHide()
        {
            CreateProjectedSprites(); PlayCast();
            var original = Drawn().Single();
            int allocated = _sprites.AllocatedSpriteCount;
            _sprites.ClearAll();
            Assert.AreEqual(0, Drawn().Length);
            _gridRoot.transform.position += new Vector3(-2f, 5f, 0f);
            PlayCast();
            Assert.AreSame(original, Drawn().Single());
            Assert.AreEqual(allocated, _sprites.AllocatedSpriteCount);
            AssertPosition(Center(5, 5), original.transform.position);
            _sprites.SetPresentationVisible(false);
            Assert.AreEqual(0, Drawn().Length);
            Assert.IsFalse(_sprites.HasBlockingFx);
        }

        [Test]
        public void ProjectedDiagonalProjectile_UsesDirectionBetweenRenderedCells()
        {
            CreateProjectedSprites();
            var definition = DetailDefinition("Projectile");
            definition.CastDuration = 0f; definition.ChargeDuration = 0f;
            var sequence = new SpellFxSequence(definition.ID, _zone, null, new Point(5, 5),
                path: new[] { new Point(6, 6) });
            Assert.Greater(_sprites.Play(sequence, definition), 0f);
            _sprites.Update(.001f);
            var head = Drawn().Single();
            AssertPosition(Center(6, 6), head.transform.position);
            float expectedAngle = Mathf.Atan2(-1.5f, 1f) * Mathf.Rad2Deg;
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(expectedAngle, head.transform.eulerAngles.z)), .001f);
        }

        [Test]
        public void ExpandedImpact_RemainsContinuousNativePixelMosaicAtProjectedTarget()
        {
            CreateProjectedSprites();
            PlayImpact();
            AssertImpactMosaic(Center(7, 5));
        }

        [Test]
        public void OmittedTilemap_PreservesExistingImpactMosaicAndParentTranslation()
        {
            _sprites = new SpriteSpellFxRenderer(_fxParent.transform);
            _sprites.SetZone(_zone);
            PlayImpact();
            AssertImpactMosaic(_fxParent.transform.TransformPoint(new Vector3(7.5f, Zone.Height - 5.5f, -.005f)));
        }

        private void AssertImpactMosaic(Vector3 target)
        {
            var impacts = Drawn().Where(view => view.sprite.texture.name.EndsWith("_impact")).ToArray();
            Assert.AreEqual(9, impacts.Length);
            foreach (float x in new[] { -.75f, 0f, .75f })
                foreach (float y in new[] { -.75f, 0f, .75f })
                    Assert.That(impacts.Count(view =>
                        Mathf.Abs(view.transform.position.x - target.x - x) < .001f &&
                        Mathf.Abs(view.transform.position.y - target.y - y) < .001f), Is.EqualTo(1),
                        "The 32px impact must not break into separated rows on 24px cells.");
            Assert.That(impacts.Min(view => view.bounds.min.x), Is.EqualTo(target.x - 1f).Within(.001f));
            Assert.That(impacts.Max(view => view.bounds.max.x), Is.EqualTo(target.x + 1f).Within(.001f));
            Assert.That(impacts.Min(view => view.bounds.min.y), Is.EqualTo(target.y - 1f).Within(.001f));
            Assert.That(impacts.Max(view => view.bounds.max.y), Is.EqualTo(target.y + 1f).Within(.001f));
        }

        [Test]
        public void ProjectedImpact_DoesNotExposeHiddenTargetOrFogNeighbors()
        {
            CreateProjectedSprites();
            foreach (var cell in _zone.Cells) cell.IsVisible = false;
            _zone.GetCell(7, 5).IsVisible = true;
            PlayImpact();
            var center = Drawn().Single();
            Vector3 target = Center(7, 5);
            AssertPosition(target, center.transform.position);
            Assert.GreaterOrEqual(center.bounds.min.x, target.x - .5f - .001f);
            Assert.LessOrEqual(center.bounds.max.x, target.x + .5f + .001f);
            Assert.GreaterOrEqual(center.bounds.min.y, target.y - .75f - .001f);
            Assert.LessOrEqual(center.bounds.max.y, target.y + .75f + .001f);

            Reveal(); _zone.GetCell(7, 5).IsVisible = false;
            _sprites.Update(0f);
            Assert.AreEqual(0, Drawn().Length, "Visible neighbors cannot retain an impact with a hidden target.");
        }

        [Test]
        public void Coordinator_ForwardsProjectionToSpriteFallback()
        {
            _world = CreateProjectedWorld(new AsciiFxRenderer(_tilemap), _fxParent.transform, null, _tilemap);
            _world.SetZone(_zone);
            var playback = _world.Play(CastSequence());
            _world.Update(0f);
            Assert.AreEqual(WorldFxPlaybackState.Playing, playback.State);
            Assert.AreEqual(0, _world.NativeRenderer.ActiveCount);
            Assert.Greater(_world.SpriteRenderer.ActiveSpriteCount, 0);
            AssertPosition(Center(5, 5), Drawn().Single().transform.position);
            _world.CancelAll();
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, playback.State);
            Assert.AreEqual(0, Drawn().Length);
        }

        [Test]
        public void Coordinator_WithProjectedFallback_StillPrefersAvailableNativeSurface()
        {
            using (var fixture = new NativeSpellFixture())
            using (var world = CreateProjectedWorld(new AsciiFxRenderer(_tilemap), fixture.Host.transform,
                fixture.Library, _tilemap))
            {
                world.SetZone(fixture.Zone);
                world.SetNativeSurface(fixture.Surface);
                var playback = world.Play(fixture.Sequence());
                world.Update(.33f);
                Assert.AreEqual(WorldFxPlaybackState.Playing, playback.State);
                Assert.Greater(world.NativeRenderer.ActiveMeshCount, 0);
                Assert.AreEqual(0, world.SpriteRenderer.ActiveCount,
                    "The optional flat source must not replace native mesh projection.");
                var mesh = world.NativeRenderer.Root.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(view => view.enabled && view.gameObject.activeInHierarchy);
                AssertPosition(Village3DProjection.CellCentre(14, 10) + Vector3.up * .3f,
                    mesh.transform.position);
            }
        }

        private void CreateProjectedSprites()
        {
            // Reflection keeps the baseline assembly compilable, making the
            // missing opt-in API an explicit RED assertion rather than a stale run.
            var constructor = typeof(SpriteSpellFxRenderer).GetConstructor(new[] { typeof(Transform), typeof(Tilemap) });
            Assert.NotNull(constructor, "Sprite spells need an explicit optional Tilemap projection source.");
            _sprites = (SpriteSpellFxRenderer)constructor.Invoke(new object[] { _fxParent.transform, _tilemap });
            _sprites.SetZone(_zone);
        }

        private static WorldFxCoordinator CreateProjectedWorld(AsciiFxRenderer ascii, Transform parent,
            NativeSpellFxLibrary nativeLibrary, Tilemap map)
        {
            var constructor = typeof(WorldFxCoordinator).GetConstructor(new[] {
                typeof(AsciiFxRenderer), typeof(Transform), typeof(NativeSpellFxLibrary), typeof(Tilemap) });
            Assert.NotNull(constructor, "Coordinator must forward the optional projection without changing native-library argument order.");
            return (WorldFxCoordinator)constructor.Invoke(new object[] { ascii, parent, nativeLibrary, map });
        }

        private void PlayCast()
        {
            Assert.Greater(_sprites.Play(CastSequence(), SpellFxCatalog.Find("Pyromancy_Kindle")), 0f);
            _sprites.Update(0f);
            Assert.AreEqual(1, Drawn().Length, "Only the immediate cast frame should be drawn.");
        }

        private SpellFxSequence CastSequence() => new SpellFxSequence("Pyromancy_Kindle", _zone, null,
            new Point(5, 5));

        private void PlayImpact()
        {
            var definition = SpellFxCatalog.Find("Cryomancy_Frostbind");
            var target = new Point(7, 5);
            var sequence = new SpellFxSequence(definition.ID, _zone, null, new Point(5, 5),
                targets: new[] { new SpellFxTargetResult("target", target, target, damage: 2) });
            Assert.Greater(_sprites.Play(sequence, definition), 0f);
            _sprites.Update(definition.CastDuration + definition.ChargeDuration + .01f);
        }

        private static SpellFxDefinition DetailDefinition(string family)
        {
            var source = SpellFxCatalog.Find("Pyromancy_Kindle");
            return new SpellFxDefinition { ID = source.ID, School = source.School, Family = family,
                DetailSheet = source.DetailSheet, ImpactSheet = source.ImpactSheet,
                CastDuration = .1f, ChargeDuration = 0f, StepDuration = .05f,
                ImpactDuration = .2f, AftermathDuration = 0f };
        }

        private void Reveal()
        { foreach (var cell in _zone.Cells) { cell.Explored = true; cell.IsVisible = true; } }

        private SpriteRenderer[] Drawn() => _fxParent.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(view => view.enabled && view.sprite != null && view.gameObject.activeInHierarchy).ToArray();

        private Vector3 Center(int x, int y)
        {
            var origin = _gridRoot.transform.position + _tilemap.transform.localPosition;
            return new Vector3(origin.x + (x + .5f) * _grid.cellSize.x,
                origin.y + (Zone.Height - y - .5f) * _grid.cellSize.y, -y * .001f);
        }

        private static void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(.001f), "world x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(.001f), "world y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(.001f), "sorting depth");
        }
    }

    public sealed class RectangularSpellFxProjectionWiringTests : CameraCleanupFixture
    {
        [Test]
        public void OrdinaryZoneRenderer_ForwardsItsRealTilemapToSpellPresentation()
        {
            GridObject.GetComponent<Grid>().cellSize = new Vector3(1f, 1.5f, 1f);
            GridObject.transform.position = new Vector3(3.25f, -4.5f, 0f);
            var sequence = new SpellFxSequence("Pyromancy_Kindle", Zone, null, new Point(5, 5));
            World.Play(sequence);
            World.Update(0f);
            var cast = GridObject.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(view => view.gameObject.name == "Spell FX Pixel" && view.enabled);
            Assert.That(cast.transform.position.x, Is.EqualTo(8.75f).Within(.001f));
            Assert.That(cast.transform.position.y,
                Is.EqualTo(-4.5f + (CavesOfOoo.Core.Zone.Height - 5.5f) * 1.5f).Within(.001f));
        }
    }
}
