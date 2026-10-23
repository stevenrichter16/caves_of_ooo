using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Effects;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 8 §8E.4 — EditMode unit tests for LightSourceSpriteHook.
    /// Full Light2D spawning + flicker behavior requires Play mode +
    /// the URP 2D renderer running, exercised by the visual showcase.
    /// These tests cover the toggle + post-render contract surfaces.
    /// </summary>
    public class LightSourceSpriteHookTests
    {
        private GameObject _hookGo;
        private LightSourceSpriteHook _hook;
        private Tile _fixtureTile;

        [SetUp]
        public void Setup()
        {
            _hookGo = new GameObject("LightHookTest");
            _hook = _hookGo.AddComponent<LightSourceSpriteHook>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hookGo != null) Object.DestroyImmediate(_hookGo);
            if (_fixtureTile != null) Object.DestroyImmediate(_fixtureTile);
        }

        [Test]
        public void Default_LightingEnabled_IsTrue()
        {
            Assert.IsTrue(_hook.TestOnly_IsLightingEnabled,
                "LightSourceSpriteHook ships with lighting ON by default.");
        }

        [TestCase(1f, 0f, 0f)]
        [TestCase(1.5f, 4f, 7f)]
        public void FixtureLight_UsesOverlayCellCenter(float rowHeight, float offsetX, float offsetY)
        {
            var map = CreateWorldMap(rowHeight, offsetX, offsetY);
            _fixtureTile = ScriptableObject.CreateInstance<Tile>();
            _fixtureTile.name = "Campfire";
            map.SetTile(new Vector3Int(3, 4, 0), _fixtureTile);
            _hook.PostRender(8, 8, false);
            var light = map.transform.parent.Find("Light_Campfire_3_4");
            Assert.That(light, Is.Not.Null);
            Assert.That(light.position.x, Is.EqualTo(offsetX + 3.5f).Within(0.0001f));
            Assert.That(light.position.y, Is.EqualTo(offsetY + 4.5f * rowHeight).Within(0.0001f));
            map.SetTile(new Vector3Int(3, 4, 0), null);
            _hook.PostRender(8, 8, false);
            Assert.That(_hook.TestOnly_SpawnedCount, Is.Zero, "Removed fixture cannot leave a light.");
        }

        [Test]
        public void ExistingFixtureLight_ReprojectsWhenCellSpacingChanges()
        {
            var map = CreateWorldMap(1f, 2f, 3f);
            _fixtureTile = ScriptableObject.CreateInstance<Tile>();
            _fixtureTile.name = "Campfire";
            map.SetTile(new Vector3Int(3, 4, 0), _fixtureTile);
            _hook.PostRender(8, 8, false);
            var light = map.transform.parent.Find("Light_Campfire_3_4");
            int originalId = light.GetInstanceID();
            map.GetComponentInParent<Grid>().cellSize = new Vector3(1f, 1.5f, 0f);
            _hook.PostRender(8, 8, false);
            Assert.That(light.position.y, Is.EqualTo(9.75f).Within(0.0001f));
            Assert.That(light.GetInstanceID(), Is.EqualTo(originalId), "Projection changes reuse the light.");
            _hook.LightingEnabled = false;
            _hook.PostRender(8, 8, false);
            Assert.That(_hook.TestOnly_SpawnedCount, Is.Zero);
        }

        [TestCase(1f, 0f, 0f)]
        [TestCase(1.5f, 4f, 7f)]
        public void PlayerTorch_FollowsProjectedFlippedRow(float rowHeight, float offsetX, float offsetY)
        {
            var map = CreateWorldMap(rowHeight, offsetX, offsetY);
            var zone = new Zone("RectangularTorch");
            var player = new Entity { BlueprintName = "Player" };
            zone.AddEntity(player, 3, 4);
            _hook.SetPlayerContext(player, zone);
            _hook.PostRender(8, 8, true);
            var torch = map.transform.parent.Find("PlayerTorchLight");
            Assert.That(torch, Is.Not.Null);
            Assert.That(torch.position.x, Is.EqualTo(offsetX + 3.5f).Within(0.0001f));
            Assert.That(torch.position.y, Is.EqualTo(offsetY + (Zone.Height - 4.5f) * rowHeight).Within(0.0001f));
            _hook.PostRender(8, 8, false);
            Assert.That(map.transform.parent.Find("PlayerTorchLight"), Is.Null, "Outdoor counter-case retires the dungeon torch.");
        }

        private Tilemap CreateWorldMap(float rowHeight, float offsetX, float offsetY)
        {
            var gridObject = new GameObject("ProjectionGrid", typeof(Grid));
            gridObject.transform.SetParent(_hookGo.transform, false);
            gridObject.transform.position = new Vector3(offsetX, offsetY, 0f);
            gridObject.GetComponent<Grid>().cellSize = new Vector3(1f, rowHeight, 0f);
            var mapObject = new GameObject("Overlay", typeof(Tilemap));
            mapObject.transform.SetParent(gridObject.transform, false);
            var map = mapObject.GetComponent<Tilemap>();
            _hook.Init(gridObject.transform, map, null);
            return map;
        }

        [Test]
        public void TestOnly_SetEnabled_FlipsState()
        {
            _hook.TestOnly_SetEnabled(false);
            Assert.IsFalse(_hook.TestOnly_IsLightingEnabled);
            _hook.TestOnly_SetEnabled(true);
            Assert.IsTrue(_hook.TestOnly_IsLightingEnabled);
        }

        [Test]
        public void Init_WithNullArgs_DoesNotCrash()
        {
            // Defensive contract — Init should be safe even if global
            // light or overlay tilemap aren't available yet (e.g. very
            // early bootstrap, or test scenarios with no scene graph).
            Assert.DoesNotThrow(() => _hook.Init(null, null, null));
            Assert.IsTrue(_hook.IsInitialized);
        }

        [Test]
        public void PostRender_WithoutInit_NoOps()
        {
            // Should not crash if PostRender is called before Init.
            Assert.DoesNotThrow(() => _hook.PostRender(80, 25, isDungeon: true));
            Assert.AreEqual(0, _hook.TestOnly_SpawnedCount,
                "Without Init / overlay tilemap, no lights should be spawned.");
        }

        [Test]
        public void PostRender_NullOverlayTilemap_NoSpawn()
        {
            _hook.Init(null, null, null);
            _hook.PostRender(80, 25, isDungeon: true);
            Assert.AreEqual(0, _hook.TestOnly_SpawnedCount,
                "Without an overlay tilemap, the scan early-outs and no lights spawn.");
        }

        [Test]
        public void PlayerTorch_DefaultsEnabled()
        {
            // Pass 9 §9A — torch ships ON so dim dungeons aren't
            // unwalkably dark. A future settings screen can flip it
            // off for "true darkness" purists.
            Assert.IsTrue(_hook.PlayerTorchEnabled,
                "PlayerTorchEnabled defaults to true so dungeons are walkable.");
            Assert.Greater(_hook.PlayerTorchOuterRadius, 0f);
            Assert.Greater(_hook.PlayerTorchIntensity, 0f);
        }

        [Test]
        public void SetPlayerContext_AcceptsNulls_NoCrash()
        {
            // Defensive contract — early-bootstrap calls may push null
            // refs while the Player isn't yet populated.
            Assert.DoesNotThrow(() => _hook.SetPlayerContext(null, null));
        }

        [Test]
        public void Tunables_HaveSaneDefaults()
        {
            // Adversarial: a zero outer-radius would mean an invisible
            // light → always-detectable visual regression. Pin the
            // baselines.
            Assert.Greater(_hook.CampfireOuterRadius, 0f,
                "Campfire light must have a positive outer radius.");
            Assert.Greater(_hook.ShrineOuterRadius, 0f,
                "Shrine light must have a positive outer radius.");
            Assert.Greater(_hook.CampfireBaseIntensity, 0f);
            Assert.Greater(_hook.ShrineBaseIntensity, 0f);
            // Dungeon should be dimmer than outdoor — counter-check the
            // ambient drop's direction.
            Assert.Less(_hook.DungeonAmbientIntensity, _hook.OutdoorAmbientIntensity,
                "DungeonAmbientIntensity must be < OutdoorAmbientIntensity, "
                + "otherwise ambient gets BRIGHTER underground (ambient inversion bug).");
        }
    }
}
