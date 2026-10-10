using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class SteamDeckGraphicsSettingsTests
    {
        private const string ResolutionKey = "CavesOfOoo.Village3D.WorldResolutionScale";
        private const string ShadowsKey = "CavesOfOoo.Village3D.Shadows";
        private const string FxKey = "CavesOfOoo.Fx.Mode";
        private const string Hint = "COO_HANDHELD";
        private readonly Dictionary<string, int?> _ints = new Dictionary<string, int?>();
        private readonly Dictionary<string, float?> _floats = new Dictionary<string, float?>();
        private bool _oldEnabled, _oldLow;
        private SpellFxMode _oldFx;
        private float _oldSpeed, _oldFlash, _oldShake, _oldVolume;
        private object _oldResolutionOverride, _oldShadowsOverride;
        private string _oldHint;
        private Action<string> _oldDirty;

        [SetUp]
        public void SetUp()
        {
            foreach (var key in new[] { Village3DSettings.PreferenceKey, Village3DSettings.LowDetailPreferenceKey, ShadowsKey, FxKey })
                _ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            foreach (var key in new[] { ResolutionKey, "CavesOfOoo.Fx.Speed", "CavesOfOoo.Fx.Flash", "CavesOfOoo.Fx.Shake", "CavesOfOoo.Fx.SoundVolume" })
                _floats[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : (float?)null;
            _oldHint = Environment.GetEnvironmentVariable(Hint);
            Environment.SetEnvironmentVariable(Hint, null);
            _oldEnabled = Village3DSettings.Enabled;
            _oldLow = Village3DSettings.LowDetail;
            _oldFx = SpellFxSettings.Mode;
            _oldSpeed = SpellFxSettings.AnimationSpeed;
            _oldFlash = SpellFxSettings.FlashIntensity;
            _oldShake = SpellFxSettings.ShakeIntensity;
            _oldVolume = SpellFxSettings.SoundVolume;
            _oldResolutionOverride = OverrideField("resolutionOverride")?.GetValue(null);
            _oldShadowsOverride = OverrideField("shadowsOverride")?.GetValue(null);
            _oldDirty = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.FullDirtyCallback = null;
            PlayerPrefs.DeleteKey(ResolutionKey);
            PlayerPrefs.DeleteKey(ShadowsKey);
            PlayerPrefs.DeleteKey(Village3DSettings.LowDetailPreferenceKey);
            PlayerPrefs.DeleteKey(FxKey);
            Village3DSettings.Load();
        }

        [TearDown]
        public void TearDown()
        {
            ZoneRenderHooks.FullDirtyCallback = null;
            Village3DSettings.Enabled = _oldEnabled;
            Village3DSettings.LowDetail = _oldLow;
            SpellFxSettings.Mode = _oldFx;
            SpellFxSettings.AnimationSpeed = _oldSpeed;
            SpellFxSettings.FlashIntensity = _oldFlash;
            SpellFxSettings.ShakeIntensity = _oldShake;
            SpellFxSettings.SoundVolume = _oldVolume;
            OverrideField("resolutionOverride")?.SetValue(null, _oldResolutionOverride);
            OverrideField("shadowsOverride")?.SetValue(null, _oldShadowsOverride);
            foreach (var pair in _ints)
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in _floats)
                if (pair.Value.HasValue) PlayerPrefs.SetFloat(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
            Environment.SetEnvironmentVariable(Hint, _oldHint);
            ZoneRenderHooks.FullDirtyCallback = _oldDirty;
        }

        [Test]
        public void GraphicsEntry_OpensSubmenuWithoutSavingLoadingOrQuitting()
        {
            var controller = new PauseMenuController();
            var service = new CountingSaveLoadService();
            int quits = 0;
            controller.RequestQuit = () => quits++;
            controller.Open();
            controller.ClickSelect(Constant("GraphicsIndex"), service, null);
            Assert.IsTrue(controller.IsOpen);
            Assert.IsTrue(Get<bool>(controller, "IsGraphicsOpen"));
            Assert.AreEqual(7, Get<int>(controller, "VisibleItemCount"));
            Assert.AreEqual(0, service.Saves + service.Loads + quits);
        }

        [Test]
        public void GraphicsBack_ReturnsToMain_ButTabClosesWholeMenu()
        {
            var controller = GraphicsMenu();
            controller.Tick(new KeyProbe(KeyCode.Escape), null, null);
            Assert.IsTrue(controller.IsOpen);
            Assert.IsFalse(Get<bool>(controller, "IsGraphicsOpen"));
            Assert.AreEqual(Constant("GraphicsIndex"), controller.SelectedIndex);
            controller.ClickSelect(Constant("GraphicsIndex"), null, null);
            controller.Tick(new KeyProbe(KeyCode.Tab), null, null);
            Assert.IsFalse(controller.IsOpen);
            Assert.IsFalse(Get<bool>(controller, "IsGraphicsOpen"));
        }

        [Test]
        public void HandheldPreset_ChangesQualityAndPersists_WithoutChangingWorldMode()
        {
            Village3DSettings.Enabled = false;
            var controller = GraphicsMenu();
            controller.ClickSelect(Constant("HandheldPresetIndex"), null, null);
            Assert.IsTrue(controller.IsOpen);
            Assert.IsTrue(Village3DSettings.LowDetail);
            Assert.AreEqual(.75f, Setting<float>("WorldResolutionScale"));
            Assert.IsFalse(Setting<bool>("ShadowsEnabled"));
            Assert.AreEqual(SpellFxMode.Reduced, SpellFxSettings.Mode);
            Assert.IsFalse(Village3DSettings.Enabled);
            Assert.AreEqual(.75f, PlayerPrefs.GetFloat(ResolutionKey, -1));
            Assert.AreEqual(0, PlayerPrefs.GetInt(ShadowsKey, -1));
            Assert.AreEqual((int)SpellFxMode.Reduced, PlayerPrefs.GetInt(FxKey, -1));
        }

        [Test]
        public void FullPreset_RestoresAllQualityDimensions()
        {
            var controller = GraphicsMenu();
            controller.ClickSelect(Constant("HandheldPresetIndex"), null, null);
            controller.ClickSelect(Constant("FullPresetIndex"), null, null);
            Assert.IsFalse(Village3DSettings.LowDetail);
            Assert.AreEqual(1f, Setting<float>("WorldResolutionScale"));
            Assert.IsTrue(Setting<bool>("ShadowsEnabled"));
            Assert.AreEqual(SpellFxMode.Full, SpellFxSettings.Mode);
        }

        [Test]
        public void IndependentWorldChoices_DoNotChangeDetailOrEffects()
        {
            Village3DSettings.LowDetail = false;
            SpellFxSettings.Mode = SpellFxMode.Full;
            SetSetting("WorldResolutionScale", .5f);
            SetSetting("ShadowsEnabled", false);
            Assert.IsFalse(Village3DSettings.LowDetail);
            Assert.AreEqual(SpellFxMode.Full, SpellFxSettings.Mode);
            Village3DSettings.LowDetail = true;
            Assert.AreEqual(.5f, Setting<float>("WorldResolutionScale"));
            Assert.IsFalse(Setting<bool>("ShadowsEnabled"));
            SetSetting("ShadowsEnabled", true);
            Assert.AreEqual(.5f, Setting<float>("WorldResolutionScale"));
        }

        [Test]
        public void MissingIndependentPreferences_PreserveLegacyLowDetailFallback()
        {
            PlayerPrefs.SetInt(Village3DSettings.LowDetailPreferenceKey, 1);
            Village3DSettings.Load();
            Assert.AreEqual(.75f, Setting<float>("WorldResolutionScale"));
            Assert.IsFalse(Setting<bool>("ShadowsEnabled"));
            Village3DSettings.LowDetail = false;
            Assert.AreEqual(1f, Setting<float>("WorldResolutionScale"));
            Assert.IsTrue(Setting<bool>("ShadowsEnabled"));
            Assert.IsFalse(PlayerPrefs.HasKey(ResolutionKey));
            Assert.IsFalse(PlayerPrefs.HasKey(ShadowsKey));
        }

        [Test]
        public void IndependentPreferences_RoundTripWithoutOverwritingOtherDimensions()
        {
            SetSetting("WorldResolutionScale", .5f);
            SetSetting("ShadowsEnabled", true);
            Village3DSettings.LowDetail = true;
            Village3DSettings.Save();
            SetSetting("WorldResolutionScale", 1f);
            SetSetting("ShadowsEnabled", false);
            Village3DSettings.Load();
            Assert.AreEqual(.5f, Setting<float>("WorldResolutionScale"));
            Assert.IsTrue(Setting<bool>("ShadowsEnabled"));
            Assert.IsTrue(Village3DSettings.LowDetail);
        }

        [Test]
        public void DistributionHint_DefaultsNewPreferencesToHandheld_WithoutWritingKeys()
        {
            Environment.SetEnvironmentVariable(Hint, "1");
            Village3DSettings.Load();
            SpellFxSettings.Load();
            Assert.IsTrue(Village3DSettings.LowDetail);
            Assert.AreEqual(.75f, Setting<float>("WorldResolutionScale"));
            Assert.IsFalse(Setting<bool>("ShadowsEnabled"));
            Assert.AreEqual(SpellFxMode.Reduced, SpellFxSettings.Mode);
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.LowDetailPreferenceKey));
            Assert.IsFalse(PlayerPrefs.HasKey(FxKey));
        }

        [Test]
        public void DistributionHint_PreservesSavedFullDetailAndEffects()
        {
            Environment.SetEnvironmentVariable(Hint, "1");
            PlayerPrefs.SetInt(Village3DSettings.LowDetailPreferenceKey, 0);
            PlayerPrefs.SetInt(FxKey, (int)SpellFxMode.Full);
            Village3DSettings.Load();
            SpellFxSettings.Load();
            Assert.IsFalse(Village3DSettings.LowDetail);
            Assert.AreEqual(1f, Setting<float>("WorldResolutionScale"));
            Assert.IsTrue(Setting<bool>("ShadowsEnabled"));
            Assert.AreEqual(SpellFxMode.Full, SpellFxSettings.Mode);
        }

        [Test]
        public void GraphicsDirectionalChanges_UseCurrentRowWithoutDispatchingSave()
        {
            var controller = GraphicsMenu();
            var service = new CountingSaveLoadService();
            controller.HoverSelect(Constant("WorldResolutionIndex"));
            controller.Tick(new KeyProbe(KeyCode.RightArrow), service, null);
            float afterRight = Setting<float>("WorldResolutionScale");
            Assert.AreNotEqual(1f, afterRight);
            controller.Tick(new KeyProbe(KeyCode.LeftArrow), service, null);
            Assert.AreEqual(1f, Setting<float>("WorldResolutionScale"));
            Assert.AreEqual(0, service.Saves + service.Loads);
            Assert.IsTrue(controller.IsOpen);
        }

        public static int Constant(string name)
        {
            var field = typeof(PauseMenuController).GetField(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "Pause menu must expose " + name);
            return (int)field.GetRawConstantValue();
        }
        public static T Get<T>(object target, string name)
        {
            var property = target.GetType().GetProperty(name);
            Assert.IsNotNull(property, "Missing menu state " + name);
            return (T)property.GetValue(target);
        }
        public static T Setting<T>(string name)
        {
            var property = typeof(Village3DSettings).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(property, "Missing independent graphics setting " + name);
            return (T)property.GetValue(null);
        }
        public static void SetSetting(string name, object value)
        {
            var property = typeof(Village3DSettings).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(property, "Missing independent graphics setting " + name);
            property.SetValue(null, value);
        }
        public static PauseMenuController GraphicsMenu()
        {
            var controller = new PauseMenuController();
            controller.Open();
            controller.ClickSelect(Constant("GraphicsIndex"), null, null);
            return controller;
        }
        private static FieldInfo OverrideField(string name) =>
            typeof(Village3DSettings).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);

        public sealed class KeyProbe : IInputProbe
        {
            private readonly KeyCode _key;
            public KeyProbe(KeyCode key) { _key = key; }
            public bool GetKeyDown(KeyCode key) => key == _key;
        }
        private sealed class CountingSaveLoadService : ISaveLoadService
        {
            public int Saves, Loads;
            public bool BeginNewGame() => true;
            public bool QuickSave() { Saves++; return true; }
            public bool QuickLoad() { Loads++; return true; }
            public bool HasQuickSave() => true;
        }
    }
}
