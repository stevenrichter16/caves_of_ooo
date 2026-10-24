using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Presentation.Effects;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 7 §7C.1 — tests for SpriteEnvToggleController.
    /// EditMode-only; full hotkey + Renderer integration requires
    /// Play mode. Tests cover the toggle + persistence contract.
    /// </summary>
    public class SpriteEnvToggleControllerTests
    {
        private GameObject _toggleGo;
        private SpriteEnvToggleController _toggle;
        private GameObject _rendererGo;
        private EnvironmentSpriteRenderer _renderer;
        private WorldPresentationMode _oldMode;
        private bool _oldSprites, _hadPreference;
        private int _oldPreference;
        private System.Action<string> _oldDirty;

        [SetUp]
        public void Setup()
        {
            _oldDirty = CavesOfOoo.Core.ZoneRenderHooks.FullDirtyCallback; CavesOfOoo.Core.ZoneRenderHooks.FullDirtyCallback = null;
            _oldMode = WorldPresentationSettings.Mode; _oldSprites = WorldPresentationSettings.LegacySpritesEnabled;
            _hadPreference = PlayerPrefs.HasKey(WorldPresentationSettings.LegacySpritesPreferenceKey);
            _oldPreference = PlayerPrefs.GetInt(WorldPresentationSettings.LegacySpritesPreferenceKey);
            WorldPresentationSettings.Mode = WorldPresentationMode.Previous;
            WorldPresentationSettings.LegacySpritesEnabled = true;
            _toggleGo = new GameObject("ToggleTest"); _toggleGo.SetActive(false);
            _toggle = _toggleGo.AddComponent<SpriteEnvToggleController>();
            _rendererGo = new GameObject("RendererTest"); _rendererGo.SetActive(false);
            var owner = _rendererGo.AddComponent<ZoneRenderer>();
            _renderer = _rendererGo.AddComponent<EnvironmentSpriteRenderer>();
            typeof(ZoneRenderer).GetField("_envSpriteRenderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(owner, _renderer);
            _toggle.TestOnly_SetRenderer(_renderer);
        }

        [TearDown]
        public void TearDown()
        {
            if (_toggleGo != null) Object.DestroyImmediate(_toggleGo);
            if (_rendererGo != null) Object.DestroyImmediate(_rendererGo);
            WorldPresentationSettings.Mode = _oldMode; WorldPresentationSettings.LegacySpritesEnabled = _oldSprites;
            if (_hadPreference) PlayerPrefs.SetInt(WorldPresentationSettings.LegacySpritesPreferenceKey, _oldPreference);
            else PlayerPrefs.DeleteKey(WorldPresentationSettings.LegacySpritesPreferenceKey);
            PlayerPrefs.Save();
            CavesOfOoo.Core.ZoneRenderHooks.FullDirtyCallback = _oldDirty;
        }

        [Test]
        public void Toggle_FromOn_TurnsOff()
        {
            _renderer.RenderingEnabled = true;
            bool result = _toggle.TestOnly_Toggle();
            Assert.IsFalse(result);
            Assert.IsFalse(_renderer.RenderingEnabled);
            Assert.IsFalse(_toggle.TestOnly_IsEnabled);
        }

        [Test]
        public void Toggle_FromOff_TurnsOn()
        {
            WorldPresentationSettings.LegacySpritesEnabled = false;
            _renderer.RenderingEnabled = false;
            bool result = _toggle.TestOnly_Toggle();
            Assert.IsTrue(result);
            Assert.IsTrue(_toggle.TestOnly_IsEnabled);
        }

        [Test]
        public void Toggle_TwoTimes_ReturnsToOriginal()
        {
            bool start = _toggle.TestOnly_IsEnabled;
            _toggle.TestOnly_Toggle();
            _toggle.TestOnly_Toggle();
            Assert.AreEqual(start, _toggle.TestOnly_IsEnabled);
        }

        [Test]
        public void NoRenderer_DoesNotCrash()
        {
            _toggle.TestOnly_SetRenderer(null);
            Assert.DoesNotThrow(() => _toggle.TestOnly_Toggle());
            Assert.IsFalse(_toggle.TestOnly_IsEnabled,
                "Without a renderer, IsEnabled returns false defensively.");
        }
    }
}
