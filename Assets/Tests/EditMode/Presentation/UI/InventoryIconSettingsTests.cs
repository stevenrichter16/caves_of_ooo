using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class InventoryIconSettingsTests
    {
        const string Key = "CavesOfOoo.InventoryIcons";
        readonly Dictionary<string, int?> ints = new Dictionary<string, int?>();
        readonly Dictionary<string, float?> floats = new Dictionary<string, float?>();
        bool oldIcons, oldSprites, old3D, oldLow;
        WorldPresentationMode oldMode;
        SpellFxMode oldFx;
        object oldResolution, oldShadows;
        Action<string> oldDirty;

        [SetUp]
        public void SetUp()
        {
            foreach (string key in new[] { Key, WorldPresentationSettings.PreferenceKey,
                WorldPresentationSettings.LegacySpritesPreferenceKey, Village3DSettings.PreferenceKey,
                Village3DSettings.LowDetailPreferenceKey, Village3DSettings.ShadowsPreferenceKey,
                SpellFxSettings.ModePreferenceKey })
                ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            foreach (string key in new[] { Village3DSettings.WorldResolutionPreferenceKey,
                "CavesOfOoo.Fx.Speed", "CavesOfOoo.Fx.Flash", "CavesOfOoo.Fx.Shake", "CavesOfOoo.Fx.SoundVolume" })
                floats[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : (float?)null;
            oldIcons = InventoryIconSettings.Enabled;
            oldMode = WorldPresentationSettings.Mode;
            oldSprites = WorldPresentationSettings.LegacySpritesEnabled;
            old3D = Village3DSettings.Enabled;
            oldLow = Village3DSettings.LowDetail;
            oldFx = SpellFxSettings.Mode;
            oldResolution = QualityField("resolutionOverride").GetValue(null);
            oldShadows = QualityField("shadowsOverride").GetValue(null);
            oldDirty = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.FullDirtyCallback = null;
            PlayerPrefs.DeleteKey(Key);
            InventoryIconSettings.Enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                ZoneRenderHooks.FullDirtyCallback = null;
                InventoryIconSettings.Enabled = oldIcons;
                WorldPresentationSettings.Mode = oldMode;
                WorldPresentationSettings.LegacySpritesEnabled = oldSprites;
                Village3DSettings.Enabled = old3D;
                Village3DSettings.LowDetail = oldLow;
                QualityField("resolutionOverride").SetValue(null, oldResolution);
                QualityField("shadowsOverride").SetValue(null, oldShadows);
                SpellFxSettings.Mode = oldFx;
                foreach (var pair in ints)
                    if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
                foreach (var pair in floats)
                    if (pair.Value.HasValue) PlayerPrefs.SetFloat(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
                PlayerPrefs.Save();
            }
            finally { ZoneRenderHooks.FullDirtyCallback = oldDirty; }
        }

        [Test]
        public void MissingPreferenceDefaultsOnWithoutWritingAnyPreference()
        {
            Assert.AreEqual(Key, InventoryIconSettings.PreferenceKey);
            InventoryIconSettings.Enabled = false;
            InventoryIconSettings.Load();
            Assert.True(InventoryIconSettings.Enabled);
            Assert.False(PlayerPrefs.HasKey(Key));
            InventoryIconSettings.Load();
            Assert.True(InventoryIconSettings.Enabled);
            Assert.False(PlayerPrefs.HasKey(Key));
            AssertOtherSettingsUnchanged();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitChoiceSurvivesRuntimeResetAndReload(bool enabled)
        {
            InventoryIconSettings.Enabled = enabled;
            InventoryIconSettings.Save();
            Assert.AreEqual(enabled ? 1 : 0, PlayerPrefs.GetInt(Key, -1));
            InventoryIconSettings.Enabled = !enabled;
            InventoryIconSettings.Load();
            Assert.AreEqual(enabled, InventoryIconSettings.Enabled);
            InventoryIconSettings.Load();
            Assert.AreEqual(enabled, InventoryIconSettings.Enabled);
            Assert.AreEqual(enabled ? 1 : 0, PlayerPrefs.GetInt(Key, -1));
            AssertOtherSettingsUnchanged();
        }

        [Test]
        public void RuntimeAssignmentDoesNotCreateOrOverwriteSavedChoice()
        {
            InventoryIconSettings.Enabled = false;
            Assert.False(PlayerPrefs.HasKey(Key));
            PlayerPrefs.SetInt(Key, 0);
            InventoryIconSettings.Enabled = true;
            Assert.AreEqual(0, PlayerPrefs.GetInt(Key));
            InventoryIconSettings.Load();
            Assert.False(InventoryIconSettings.Enabled);
            AssertOtherSettingsUnchanged();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualMenuClickTogglesBothWaysAndOnlyPersistsItemIcons(bool initial)
        {
            InventoryIconSettings.Enabled = initial;
            InventoryIconSettings.Save();
            var service = new CountingSaveLoadService();
            var menu = GraphicsMenu(service);
            int applies = 0, dirties = 0, logs = 0;
            menu.WorldPresentationChanged = () => applies++;
            ZoneRenderHooks.FullDirtyCallback = _ => dirties++;
            int revision = menu.ViewRevision;
            Assert.AreEqual("Item icons: " + (initial ? "On" : "Off"), menu.GetItemLabel(PauseMenuController.ItemIconsIndex));
            menu.ClickSelect(PauseMenuController.ItemIconsIndex, service, _ => logs++);
            Assert.AreEqual(!initial, InventoryIconSettings.Enabled);
            Assert.AreEqual(initial ? 0 : 1, PlayerPrefs.GetInt(Key, -1));
            Assert.AreEqual(revision + 1, menu.ViewRevision);
            Assert.AreEqual("Item icons: " + (initial ? "Off" : "On"), menu.GetItemLabel(PauseMenuController.ItemIconsIndex));
            menu.ClickSelect(PauseMenuController.ItemIconsIndex, service, _ => logs++);
            Assert.AreEqual(initial, InventoryIconSettings.Enabled);
            Assert.AreEqual(initial ? 1 : 0, PlayerPrefs.GetInt(Key, -1));
            Assert.AreEqual(revision + 2, menu.ViewRevision);
            Assert.True(menu.IsOpen && menu.IsGraphicsOpen);
            Assert.Zero(service.Calls + applies + dirties + logs);
            AssertOtherSettingsUnchanged();
        }

        [TestCase(KeyCode.Return)]
        [TestCase(KeyCode.LeftArrow)]
        [TestCase(KeyCode.RightArrow)]
        public void ActualMenuKeyboardOptionPersistsOffAndOn(KeyCode key)
        {
            var menu = GraphicsMenu(null);
            menu.MoveSelectionForTest(PauseMenuController.ItemIconsIndex);
            Assert.True(menu.Tick(new KeyProbe(key), null, null));
            Assert.False(InventoryIconSettings.Enabled);
            Assert.AreEqual(0, PlayerPrefs.GetInt(Key, -1));
            Assert.True(menu.Tick(new KeyProbe(key), null, null));
            Assert.True(InventoryIconSettings.Enabled);
            Assert.AreEqual(1, PlayerPrefs.GetInt(Key, -1));
            AssertOtherSettingsUnchanged();
        }

        [Test]
        public void NavigationBackAndClosedClickDoNotChangeOrPersistIcons()
        {
            var menu = GraphicsMenu(null);
            Assert.AreEqual(11, menu.VisibleItemCount);
            menu.HoverSelect(PauseMenuController.ItemIconsIndex);
            Assert.True(menu.Tick(new KeyProbe(KeyCode.DownArrow), null, null));
            Assert.AreEqual(PauseMenuController.GraphicsBackIndex, menu.SelectedIndex);
            Assert.AreEqual("Back", menu.GetItemLabel(menu.SelectedIndex));
            menu.ClickSelect(PauseMenuController.GraphicsBackIndex, null, null);
            Assert.True(menu.IsOpen);
            Assert.False(menu.IsGraphicsOpen);
            Assert.AreEqual(PauseMenuController.GraphicsIndex, menu.SelectedIndex);
            menu.Close();
            menu.ClickSelect(PauseMenuController.ItemIconsIndex, null, null);
            Assert.True(InventoryIconSettings.Enabled);
            Assert.False(PlayerPrefs.HasKey(Key));
            AssertOtherSettingsUnchanged();
        }

        void AssertOtherSettingsUnchanged()
        {
            foreach (var pair in ints)
            {
                if (pair.Key == Key) continue;
                Assert.AreEqual(pair.Value.HasValue, PlayerPrefs.HasKey(pair.Key), pair.Key);
                if (pair.Value.HasValue) Assert.AreEqual(pair.Value.Value, PlayerPrefs.GetInt(pair.Key), pair.Key);
            }
            foreach (var pair in floats)
            {
                Assert.AreEqual(pair.Value.HasValue, PlayerPrefs.HasKey(pair.Key), pair.Key);
                if (pair.Value.HasValue) Assert.AreEqual(pair.Value.Value, PlayerPrefs.GetFloat(pair.Key), pair.Key);
            }
            Assert.AreEqual(oldMode, WorldPresentationSettings.Mode);
            Assert.AreEqual(oldSprites, WorldPresentationSettings.LegacySpritesEnabled);
            Assert.AreEqual(old3D, Village3DSettings.Enabled);
            Assert.AreEqual(oldLow, Village3DSettings.LowDetail);
            Assert.AreEqual(oldFx, SpellFxSettings.Mode);
            Assert.AreEqual(oldResolution, QualityField("resolutionOverride").GetValue(null));
            Assert.AreEqual(oldShadows, QualityField("shadowsOverride").GetValue(null));
        }

        static FieldInfo QualityField(string name) => typeof(Village3DSettings).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        static PauseMenuController GraphicsMenu(ISaveLoadService service)
        {
            var menu = new PauseMenuController();
            menu.Open();
            menu.ClickSelect(PauseMenuController.GraphicsIndex, service, null);
            Assert.True(menu.IsGraphicsOpen);
            return menu;
        }
        sealed class KeyProbe : IInputProbe
        {
            readonly KeyCode key;
            public KeyProbe(KeyCode key) { this.key = key; }
            public bool GetKeyDown(KeyCode candidate) => candidate == key;
        }
        sealed class CountingSaveLoadService : ISaveLoadService
        {
            public int Calls;
            public bool BeginNewGame() { Calls++; return true; }
            public bool QuickSave() { Calls++; return true; }
            public bool QuickLoad() { Calls++; return true; }
            public bool HasQuickSave() { Calls++; return true; }
        }
    }
}
