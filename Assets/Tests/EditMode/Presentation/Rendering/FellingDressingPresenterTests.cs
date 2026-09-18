using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingDressingPresenterTests
    {
        private EntityFactory _factory;
        private OverworldZoneManager _manager;
        private Zone _zone;
        private GameObject _go;
        private FellingDressingPresenter _presenter;

        [SetUp] public void SetUp()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            _manager = new OverworldZoneManager(_factory, 64);
            _zone = _manager.GetZone(FellingSiteBuilder.ZoneID);
            Reveal(_zone);
            _go = new GameObject("Supplemental Felling art test");
            _presenter = _go.AddComponent<FellingDressingPresenter>();
            _presenter.Bind(_zone);
            Assert.IsTrue(_presenter.IsReady);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_go); }
        private static void Reveal(Zone zone)
        {
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            { var cell = zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
        }
        private SpriteRenderer View(string id) => _go.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == id);
        private Entity Owner(string id) => FellingScenePopulation.FindDressingOwner(_zone, id);

        [TestCase("mushroom-pink-tall")]
        [TestCase("mushroom-cyan-cluster")]
        [TestCase("rubble-cluster")]
        public void SupplementalSpriteUsesNativePixelsAndActualEntityFeet(string id)
        {
            var spec = FellingScenePopulation.DressingSpecs.Single(s => s.id == id);
            var renderer = View(id);
            var position = _zone.GetEntityPosition(Owner(id));
            Assert.AreEqual(3, _go.GetComponentsInChildren<SpriteRenderer>(true).Length);
            Assert.AreEqual(55, _zone.GetAllEntities().Count(e => e.HasPart<FellingScenePropPart>()));
            Assert.AreEqual(new Vector2(spec.width, spec.height), renderer.sprite.rect.size);
            Assert.AreEqual(32, renderer.sprite.pixelsPerUnit);
            Assert.AreEqual(Vector2.zero, renderer.sprite.pivot);
            Assert.AreEqual(FilterMode.Point, renderer.sprite.texture.filterMode);
            Assert.IsTrue(renderer.sprite.texture.isReadable);
            Assert.AreEqual(position.x + 0.5f, renderer.transform.position.x + spec.footX / 32, 0.0001f);
            Assert.AreEqual(24 - position.y, renderer.transform.position.y + (spec.height - spec.footY) / 32, 0.0001f);
            Assert.AreEqual(-position.y * 0.001f, renderer.transform.position.z, 0.000001f);
            Assert.IsTrue(_presenter.IsRenderedEntity(Owner(id)));
        }

        [Test] public void AlphaPickingUsesActualVisibleOwnerAndFallsThroughTransparentPixels()
        {
            const string id = "mushroom-pink-tall";
            var view = View(id); var pixels = view.sprite.texture.GetPixels32();
            int solid = System.Array.FindIndex(pixels, p => p.a != 0);
            int empty = System.Array.FindIndex(pixels, p => p.a == 0);
            Assert.GreaterOrEqual(solid, 0); Assert.GreaterOrEqual(empty, 0);
            Vector2 At(int index) => (Vector2)view.transform.position + new Vector2(index % view.sprite.texture.width + 0.5f,
                index / view.sprite.texture.width + 0.5f) / 32;
            Assert.IsTrue(_presenter.TryPickWorld(At(solid), out Entity picked, out int x, out int y));
            Assert.AreSame(Owner(id), picked); Assert.AreEqual(_zone.GetEntityPosition(picked), (x, y));
            Assert.IsFalse(_presenter.TryPickWorld(At(empty), out _, out _, out _));
            _zone.GetEntityCell(picked).IsVisible = false; _presenter.Refresh();
            Assert.IsFalse(view.enabled);
            Assert.IsFalse(_presenter.TryPickWorld(At(solid), out _, out _, out _));
        }

        [Test] public void NativeHarvestHidesItsSpriteAndLeavesTheOtherPropsPresent()
        {
            var previousFactory = HarvestablePart.Factory; HarvestablePart.Factory = _factory;
            try
            {
                var owner = Owner("mushroom-pink-tall"); var position = _zone.GetEntityPosition(owner);
                var player = _factory.CreateEntity("Player"); _zone.AddEntity(player, position.x, position.y);
                var action = GameEvent.New("InventoryAction"); action.SetParameter("Command", "Harvest");
                action.SetParameter("Actor", (object)player); action.SetParameter("Zone", (object)_zone);
                action.SetParameter("Random", (object)new System.Random(71)); owner.FireEventAndRelease(action);
                _presenter.Refresh();
                Assert.IsNull(Owner("mushroom-pink-tall")); Assert.IsFalse(View("mushroom-pink-tall").enabled);
                Assert.IsFalse(_presenter.IsRenderedEntity(owner));
                Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "Mushroom"));
                Assert.IsTrue(View("mushroom-cyan-cluster").enabled); Assert.IsTrue(View("rubble-cluster").enabled);
            }
            finally { HarvestablePart.Factory = previousFactory; }
        }

        [Test] public void NativePickupAndDropRelocateTheViewAndSurviveSaveReload()
        {
            const string id = "rubble-cluster";
            var owner = Owner(id); var position = _zone.GetEntityPosition(owner);
            var player = _factory.CreateEntity("Player"); _zone.AddEntity(player, position.x, position.y);
            Assert.IsTrue(InventorySystem.Pickup(player, owner, _zone)); _presenter.Refresh();
            Assert.IsFalse(View(id).enabled); Assert.IsFalse(_presenter.IsRenderedEntity(owner));
            _zone.MoveEntity(player, 40, 20); Assert.IsTrue(InventorySystem.Drop(player, owner, _zone)); _presenter.Refresh();
            var spec = FellingScenePopulation.DressingSpecs.Single(s => s.id == id);
            Assert.IsTrue(View(id).enabled);
            Assert.AreEqual(40.5f, View(id).transform.position.x + spec.footX / 32, 0.0001f);
            _manager.SetActiveZone(_zone); var turns = new TurnManager(); turns.AddEntity(player);
            var captured = GameSessionState.Capture("felling-dressing-view", "test", _manager, turns, player);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            { captured.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, _factory)); }
            _zone = loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID); Reveal(_zone); _presenter.Bind(_zone);
            Assert.IsTrue(View(id).enabled); Assert.AreEqual((40, 20), _zone.GetEntityPosition(Owner(id)));
            Assert.AreEqual(40.5f, View(id).transform.position.x + spec.footX / 32, 0.0001f);
        }

        [Test] public void HiddenArtAndExteriorDropsReleaseTheOrdinaryGlyphFallback()
        {
            var owner = Owner("rubble-cluster");
            _presenter.SetPresentationVisible(false);
            Assert.IsFalse(_presenter.IsRenderedEntity(owner));
            _presenter.SetPresentationVisible(true); Assert.IsTrue(_presenter.IsRenderedEntity(owner));
            _zone.MoveEntity(owner, 8, 20); _presenter.Refresh();
            Assert.IsFalse(View("rubble-cluster").enabled); Assert.IsFalse(_presenter.IsRenderedEntity(owner));
            _presenter.Bind(new Zone("ordinary")); Assert.IsFalse(_presenter.IsReady);
            Assert.AreEqual(0, _go.GetComponentsInChildren<SpriteRenderer>(true).Length);
        }
    }
}
