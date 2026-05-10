using NUnit.Framework;
using UnityEngine;
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
        }

        [Test]
        public void Default_LightingEnabled_IsTrue()
        {
            Assert.IsTrue(_hook.TestOnly_IsLightingEnabled,
                "LightSourceSpriteHook ships with lighting ON by default.");
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
