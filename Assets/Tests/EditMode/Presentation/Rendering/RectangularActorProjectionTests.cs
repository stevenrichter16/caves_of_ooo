using System;
using System.Collections;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class RectangularActorProjectionTests
    {
        private GameObject _gridRoot;
        private GameObject _rendererRoot;
        private Grid _grid;
        private Tilemap _tilemap;
        private AnimatedEntityRenderer _renderer;
        private Zone _zone;
        private Entity _actor;
        private Action _restoreHooks;

        [SetUp]
        public void SetUp()
        {
            var moved = EntityVisualHooks.MovedCallback;
            var attack = EntityVisualHooks.AttackCallback;
            var interaction = EntityVisualHooks.InteractionCallback;
            var selfUse = EntityVisualHooks.SelfUseCallback;
            var cast = EntityVisualHooks.CastCallback;
            var damage = EntityVisualHooks.DamageCallback;
            var death = EntityVisualHooks.DeathCallback;
            _restoreHooks = () =>
            {
                EntityVisualHooks.MovedCallback = moved;
                EntityVisualHooks.AttackCallback = attack;
                EntityVisualHooks.InteractionCallback = interaction;
                EntityVisualHooks.SelfUseCallback = selfUse;
                EntityVisualHooks.CastCallback = cast;
                EntityVisualHooks.DamageCallback = damage;
                EntityVisualHooks.DeathCallback = death;
            };
            EntityVisualHooks.Reset();
            _gridRoot = new GameObject("Rectangular actor grid", typeof(Grid));
            _grid = _gridRoot.GetComponent<Grid>();
            _grid.cellSize = new Vector3(1f, 1.5f, 1f);
            _gridRoot.transform.position = new Vector3(3.25f, -4.5f, 5f);
            var mapRoot = new GameObject("World tiles", typeof(Tilemap));
            mapRoot.transform.SetParent(_gridRoot.transform, false);
            mapRoot.transform.localPosition = new Vector3(-1f, 2.25f, 3f);
            _tilemap = mapRoot.GetComponent<Tilemap>();
            _rendererRoot = new GameObject("Projected actors");
            _rendererRoot.transform.position = new Vector3(93f, -42f, 6f);
            _renderer = _rendererRoot.AddComponent<AnimatedEntityRenderer>();
            _zone = new Zone("rectangular-projection");
            _actor = new Entity { ID = "rectangular-player", BlueprintName = "Player" };
            _actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            _actor.AddPart(new PhysicsPart { Solid = true });
            _actor.AddPart(new RenderPart { RenderString = "@", RenderLayer = 20 });
            _zone.AddEntity(_actor, 8, 9);
            Reveal(8, 9);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rendererRoot);
            Object.DestroyImmediate(_gridRoot);
            _restoreHooks?.Invoke();
        }

        [Test]
        public void Spawn_UsesRectangularTileBottomCenterAndWorldTranslation()
        {
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);

            Assert.AreEqual(1, _renderer.ActiveViewCount);
            Assert.NotNull(Body().GetComponent<SpriteRenderer>().sprite);
            AssertPosition(Anchor(8, 9), View().position);
            Assert.AreNotEqual(new Vector3(8.5f, Zone.Height - 1 - 9, -0.009f), View().position,
                "A translated rectangular world must not silently retain unit-cell placement.");
            Assert.AreEqual(AnimatedEntityRenderer.BodySortingOrder, Body().GetComponent<SpriteRenderer>().sortingOrder);
        }

        [Test]
        public void SquareIdentityTilemap_PreservesExistingUnitAnchor()
        {
            _grid.cellSize = Vector3.one;
            _gridRoot.transform.position = Vector3.zero;
            _tilemap.transform.localPosition = Vector3.zero;
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);

            AssertPosition(new Vector3(8.5f, Zone.Height - 1 - 9, -0.009f), View().position);
        }

        [Test]
        public void NoTilemap_PreservesLegacyAnchorDespiteUnrelatedRectangularGrid()
        {
            _renderer.Init((_, __, ___) => Color.white);
            _renderer.SyncZone(_zone);

            AssertPosition(new Vector3(8.5f, Zone.Height - 1 - 9, -0.009f), View().position);
            Assert.AreNotEqual(Anchor(8, 9), View().position,
                "A nearby grid must not implicitly change a renderer without a projection source.");
        }

        [Test]
        public void SyncAfterProjectionChange_ReanchorsTheExistingViewWithoutRebuildingIt()
        {
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);
            Transform original = View();
            _grid.cellSize = new Vector3(1.5f, 2.25f, 1f);
            _tilemap.transform.localPosition += new Vector3(2f, -3f, 7f);

            _renderer.SyncZone(_zone);

            Assert.AreSame(original, View());
            AssertPosition(Anchor(8, 9), View().position);
        }

        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(-1, -1)]
        public void NativeMovement_InterpolatesBetweenProjectedCellsAndEndsAtDestination(int dx, int dy)
        {
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);
            Vector3 start = Anchor(8, 9);
            Vector3 end = Anchor(8 + dx, 9 + dy);
            Reveal(8 + dx, 9 + dy);
            Assert.IsTrue(EntityVisualCatalog.TryGetAsset(_actor, out var asset));
            float duration = asset.Definition.GetDuration(EntityVisualState.Walk);

            Assert.IsTrue(MovementSystem.TryMove(_actor, _zone, dx, dy));
            Assert.AreEqual((8 + dx, 9 + dy), _zone.GetEntityPosition(_actor),
                "Simulation moves immediately; presentation alone interpolates.");
            _renderer.SyncZone(_zone);
            Assert.That(View().position.x, Is.EqualTo(start.x).Within(0.0001f));
            Assert.That(View().position.y, Is.EqualTo(start.y).Within(0.0001f));
            Tick(duration * 0.5f);
            Vector3 midpoint = (start + end) * 0.5f;
            midpoint.x = Mathf.Round(midpoint.x * 16f) / 16f;
            midpoint.y = Mathf.Round(midpoint.y * 16f) / 16f;
            AssertPosition(midpoint, View().position);

            Tick(duration * 0.5f);
            AssertPosition(end, View().position);
            _renderer.SyncZone(_zone);
            AssertPosition(end, View().position);
        }

        [Test]
        public void LethalDamageDuringMovement_AnchorsDeathAtTheActualDestinationCell()
        {
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);
            Reveal(8, 10);
            Assert.IsTrue(MovementSystem.TryMove(_actor, _zone, 0, 1));

            CombatSystem.ApplyDamage(_actor, 10, null, _zone);

            Assert.IsNull(_zone.GetEntityCell(_actor));
            AssertPosition(Anchor(8, 10), View().position);
            _renderer.SyncZone(_zone);
            Assert.AreEqual(1, _renderer.ActiveViewCount, "The removed actor retains only its short death presentation.");
            Tick(0.05f);
            AssertPosition(Anchor(8, 10), View().position);
        }

        [Test]
        public void ForeignZoneMovementAndDeath_DoNotMoveTheCurrentView()
        {
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);
            Vector3 original = View().position;
            var foreign = new Zone("other-zone");

            EntityVisualHooks.EmitMoved(_actor, foreign, 8, 9, 15, 18);
            EntityVisualHooks.EmitDeath(_actor, null, foreign, 15, 18);
            Tick(0.1f);

            AssertPosition(original, View().position);
            Assert.AreEqual((8, 9), _zone.GetEntityPosition(_actor));
        }

        [Test]
        public void HiddenActorHasNoView_VisibleActorUsesProjection_ThenReleasesWhenHidden()
        {
            Initialize(_tilemap);
            _zone.GetCell(8, 9).IsVisible = false;
            _renderer.SyncZone(_zone);
            Assert.AreEqual(0, _renderer.ActiveViewCount);

            _zone.GetCell(8, 9).IsVisible = true;
            _renderer.SyncZone(_zone);
            Assert.AreEqual(1, _renderer.ActiveViewCount);
            AssertPosition(Anchor(8, 9), View().position);

            _zone.GetCell(8, 9).IsVisible = false;
            _renderer.SyncZone(_zone);
            Assert.AreEqual(0, _renderer.ActiveViewCount);
            Assert.IsFalse(View().gameObject.activeSelf);
        }

        [Test]
        public void PooledViewReacquiredAfterProjectionChange_UsesCurrentGeometry()
        {
            Initialize(_tilemap);
            _renderer.SyncZone(_zone);
            Transform original = View();
            _renderer.ClearViews();
            Assert.AreEqual(1, _renderer.PooledViewCount);
            _grid.cellSize = Vector3.one;
            _tilemap.transform.localPosition += new Vector3(4f, 5f, 0f);

            _renderer.SyncZone(_zone);

            Assert.AreSame(original, View());
            Assert.AreEqual(0, _renderer.PooledViewCount);
            Assert.IsTrue(View().gameObject.activeSelf);
            AssertPosition(Anchor(8, 9), View().position);
        }

        private void Initialize(Tilemap source)
        {
            _renderer.Init((_, __, ___) => Color.white, source);
        }

        private void Reveal(int x, int y)
        {
            _zone.GetCell(x, y).Explored = true;
            _zone.GetCell(x, y).IsVisible = true;
        }

        private Vector3 Anchor(int x, int y)
        {
            // Independent expected geometry for this axis-aligned fixture (including parent translation).
            Vector3 origin = _gridRoot.transform.position + _tilemap.transform.localPosition;
            return new Vector3(origin.x + (x + 0.5f) * _grid.cellSize.x,
                origin.y + (Zone.Height - 1 - y) * _grid.cellSize.y, -y * 0.001f);
        }

        private Transform View() => _rendererRoot.transform.Find("Visual_Player");
        private Transform Body() => View().Find("Body");

        private void Tick(float deltaTime)
        {
            // Advance presentation time deterministically; movement/death enter through real gameplay hooks.
            var views = (IDictionary)typeof(AnimatedEntityRenderer)
                .GetField("_views", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_renderer);
            object view = views[_actor];
            Assert.NotNull(view);
            typeof(AnimatedEntityRenderer).GetMethod("TickView", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_renderer, new[] { view, (object)deltaTime });
        }

        private static void AssertPosition(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f), "world x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f), "world y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f), "existing zone-row sorting depth");
        }
    }

    public sealed class RectangularActorProjectionWiringTests : CameraCleanupFixture
    {
        [TestCase(1f, 0f, 0f)]
        [TestCase(1.5f, 3.25f, -4.5f)]
        public void NativeZoneRenderer_ProjectsItsActorViewsThroughTheWorldTilemap(
            float rowHeight, float offsetX, float offsetY)
        {
            GridObject.GetComponent<Grid>().cellSize = new Vector3(1f, rowHeight, 1f);
            GridObject.transform.position = new Vector3(offsetX, offsetY, 0f);
            var actor = new Entity { ID = "wired-projection-player", BlueprintName = "Player" };
            actor.AddPart(new RenderPart { RenderString = "@", RenderLayer = 20 });
            Zone.AddEntity(actor, 8, 9);

            Renderer.RenderZone();

            var actors = Read<AnimatedEntityRenderer>(Renderer, "_entityVisualRenderer");
            Assert.NotNull(actors);
            Assert.AreEqual(1, actors.ActiveViewCount,
                "The ordinary full render must reach the actual Awake-created actor presenter.");
            Transform view = actors.transform.Find("Visual_Player");
            Assert.NotNull(view);
            Assert.That(view.position.x, Is.EqualTo(offsetX + 8.5f).Within(0.0001f));
            Assert.That(view.position.y,
                Is.EqualTo(offsetY + (CavesOfOoo.Core.Zone.Height - 1 - 9) * rowHeight).Within(0.0001f));
            Assert.That(view.position.z, Is.EqualTo(-0.009f).Within(0.0001f));
        }
    }
}
