using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public class SteamDeckGraphicsSettingsAdversarialTests
    {
        private readonly SteamDeckGraphicsSettingsTests _scope = new SteamDeckGraphicsSettingsTests();
        private readonly List<GameObject> _objects = new List<GameObject>();
        [SetUp] public void SetUp() => _scope.SetUp();
        [TearDown] public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            _scope.TearDown();
        }

        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(float.NegativeInfinity, 1f)]
        [TestCase(-1f, .5f)]
        [TestCase(0f, .5f)]
        [TestCase(2f, 1f)]
        public void Adversarial_InvalidResolution_IsFiniteAndBounded(float value, float expected)
        {
            Village3DSettings.WorldResolutionScale = value;
            Assert.AreEqual(expected, Village3DSettings.WorldResolutionScale);
        }

        [TestCase(null, false)]
        [TestCase("0", false)]
        [TestCase("true", false)]
        [TestCase("1", true)]
        public void Adversarial_DistributionHint_OnlyExplicitOneChangesAbsentDefaults(string hint, bool handheld)
        {
            Environment.SetEnvironmentVariable("COO_HANDHELD", hint);
            Village3DSettings.Load();
            SpellFxSettings.Load();
            Assert.AreEqual(handheld, Village3DSettings.LowDetail);
            Assert.AreEqual(handheld ? SpellFxMode.Reduced : SpellFxMode.Full, SpellFxSettings.Mode);
        }

        [Test]
        public void Adversarial_Hint_IndependentSavedFullResolutionAndShadowsWin()
        {
            Environment.SetEnvironmentVariable("COO_HANDHELD", "1");
            PlayerPrefs.SetFloat(Village3DSettings.WorldResolutionPreferenceKey, 1f);
            PlayerPrefs.SetInt(Village3DSettings.ShadowsPreferenceKey, 1);
            Village3DSettings.Load();
            Assert.IsTrue(Village3DSettings.LowDetail);
            Assert.AreEqual(1f, Village3DSettings.WorldResolutionScale);
            Assert.IsTrue(Village3DSettings.ShadowsEnabled);
        }

        [Test]
        public void Adversarial_LegacySave_DoesNotInventIndependentOverrides()
        {
            Village3DSettings.ResetQualityOverrides();
            Village3DSettings.LowDetail = true;
            Village3DSettings.Save();
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.WorldResolutionPreferenceKey));
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.ShadowsPreferenceKey));
            Village3DSettings.LowDetail = false;
            Assert.AreEqual(1f, Village3DSettings.WorldResolutionScale);
            Assert.IsTrue(Village3DSettings.ShadowsEnabled);
        }

        [Test]
        public void Adversarial_ResetOverrides_RemovesExplicitKeysAndRestoresDetailFallback()
        {
            Village3DSettings.LowDetail = true;
            Village3DSettings.WorldResolutionScale = 1;
            Village3DSettings.ShadowsEnabled = true;
            Village3DSettings.Save();
            Assert.IsTrue(PlayerPrefs.HasKey(Village3DSettings.WorldResolutionPreferenceKey));
            Village3DSettings.ResetQualityOverrides();
            Village3DSettings.Save();
            Assert.AreEqual(.75f, Village3DSettings.WorldResolutionScale);
            Assert.IsFalse(Village3DSettings.ShadowsEnabled);
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.WorldResolutionPreferenceKey));
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.ShadowsPreferenceKey));
        }

        [Test]
        public void Adversarial_MenuDetailChange_PreservesResolutionShadowsAndEffects()
        {
            Village3DSettings.ResetQualityOverrides();
            Village3DSettings.LowDetail = false;
            SpellFxSettings.Mode = SpellFxMode.Off;
            var menu = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            menu.ClickSelect(PauseMenuController.WorldDetailIndex, null, null);
            Assert.IsTrue(Village3DSettings.LowDetail);
            Assert.AreEqual(1f, Village3DSettings.WorldResolutionScale);
            Assert.IsTrue(Village3DSettings.ShadowsEnabled);
            Assert.AreEqual(SpellFxMode.Off, SpellFxSettings.Mode);
        }

        [Test]
        public void Adversarial_EffectCycle_WrapsAndLeavesWorldPreferencesAlone()
        {
            Village3DSettings.WorldResolutionScale = .5f;
            Village3DSettings.ShadowsEnabled = false;
            SpellFxSettings.Mode = SpellFxMode.Full;
            var menu = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            foreach (var expected in new[] { SpellFxMode.Reduced, SpellFxMode.Off, SpellFxMode.Full })
            {
                menu.ClickSelect(PauseMenuController.SpellEffectsIndex, null, null);
                Assert.AreEqual(expected, SpellFxSettings.Mode);
                Assert.AreEqual(.5f, Village3DSettings.WorldResolutionScale);
                Assert.IsFalse(Village3DSettings.ShadowsEnabled);
            }
        }

        [Test]
        public void Adversarial_SubmenuNavigation_ClampsAtBothEndsAndRejectsInvalidClick()
        {
            var menu = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            menu.Tick(new SteamDeckGraphicsSettingsTests.KeyProbe(KeyCode.UpArrow), null, null);
            Assert.AreEqual(PauseMenuController.HandheldPresetIndex, menu.SelectedIndex);
            menu.HoverSelect(PauseMenuController.GraphicsBackIndex);
            menu.Tick(new SteamDeckGraphicsSettingsTests.KeyProbe(KeyCode.DownArrow), null, null);
            Assert.AreEqual(PauseMenuController.GraphicsBackIndex, menu.SelectedIndex);
            menu.ClickSelect(99, null, null);
            Assert.IsTrue(menu.IsGraphicsOpen);
            Assert.AreEqual(PauseMenuController.GraphicsBackIndex, menu.SelectedIndex);
        }

        [Test]
        public void Adversarial_CloseAndReopen_StartsOnMainSaveRow()
        {
            var menu = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            menu.Close();
            menu.Open();
            Assert.IsFalse(menu.IsGraphicsOpen);
            Assert.AreEqual(PauseMenuController.SaveIndex, menu.SelectedIndex);
            Assert.AreEqual(PauseMenuController.ItemCount, menu.VisibleItemCount);
        }

        [TestCase(PauseMenuController.SaveIndex, false)]
        [TestCase(PauseMenuController.LoadIndex, true)]
        public void Adversarial_MissingSaveService_ReportsFailureWithoutCrashing(int row, bool remainsOpen)
        {
            var menu = new PauseMenuController();
            var messages = new List<string>();
            menu.Open();
            Assert.DoesNotThrow(() => menu.ClickSelect(row, null, messages.Add));
            Assert.AreEqual(remainsOpen, menu.IsOpen);
            Assert.AreEqual(1, messages.Count);
        }

        [Test]
        public void Adversarial_RuntimeSetters_DirtyOnlyOnVisibleChangeAndNeverPersist()
        {
            int dirty = 0;
            ZoneRenderHooks.FullDirtyCallback = _ => dirty++;
            Village3DSettings.WorldResolutionScale = .75f;
            Village3DSettings.WorldResolutionScale = .75f;
            Village3DSettings.ShadowsEnabled = false;
            Village3DSettings.ShadowsEnabled = false;
            Assert.AreEqual(2, dirty);
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.WorldResolutionPreferenceKey));
            Assert.IsFalse(PlayerPrefs.HasKey(Village3DSettings.ShadowsPreferenceKey));
        }

        [Test]
        public void Adversarial_Presets_PreserveSoundAndAccessibilityControls()
        {
            SpellFxSettings.SoundVolume = .3f;
            SpellFxSettings.AnimationSpeed = 2;
            SpellFxSettings.FlashIntensity = .4f;
            SpellFxSettings.ShakeIntensity = .2f;
            var menu = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            foreach (int preset in new[] { PauseMenuController.HandheldPresetIndex, PauseMenuController.FullPresetIndex })
            {
                menu.ClickSelect(preset, null, null);
                Assert.AreEqual(.3f, SpellFxSettings.SoundVolume);
                Assert.AreEqual(2, SpellFxSettings.AnimationSpeed);
                Assert.AreEqual(.4f, SpellFxSettings.FlashIntensity);
                Assert.AreEqual(.2f, SpellFxSettings.ShakeIntensity);
            }
        }

        [Test]
        public void Adversarial_NativeGamepad_OpensChangesAndBacksOutWithoutKeyboard()
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var pad = InputSystem.AddDevice<Gamepad>();
                pad.MakeCurrent();
                var menu = new PauseMenuController();
                var probe = new NativeProbe();
                void State(GamepadState state)
                {
                    InputSystem.QueueStateEvent(pad, state);
                    InputSystem.Update();
                    menu.Tick(probe, null, null);
                }
                void Press(GamepadButton button)
                {
                    State(new GamepadState());
                    State(new GamepadState().WithButton(button));
                }
                Press(GamepadButton.Start);
                for (int i = 0; i < PauseMenuController.GraphicsIndex; i++) Press(GamepadButton.DpadDown);
                Press(GamepadButton.South);
                Assert.IsTrue(menu.IsGraphicsOpen);
                Press(GamepadButton.South);
                Assert.IsTrue(Village3DSettings.LowDetail);
                Assert.AreEqual(SpellFxMode.Reduced, SpellFxSettings.Mode);
                Press(GamepadButton.East);
                Assert.IsTrue(menu.IsOpen);
                Assert.IsFalse(menu.IsGraphicsOpen);
                Press(GamepadButton.East);
                Assert.IsFalse(menu.IsOpen);
            }
            finally { input.TearDown(); }
        }

        [Test]
        public void Adversarial_PopupShrinksAndCloses_ClearsOldBoundsWithoutCreatingCamera()
        {
            var grid = CreateObject("Settings grid");
            grid.AddComponent<Grid>();
            var foreground = CreateObject("Settings text");
            foreground.transform.SetParent(grid.transform);
            var background = CreateObject("Settings background");
            background.transform.SetParent(grid.transform);
            var ui = CreateObject("Settings UI").AddComponent<PauseMenuUI>();
            ui.Tilemap = foreground.AddComponent<Tilemap>();
            ui.BgTilemap = background.AddComponent<Tilemap>();
            ui.Controller = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            int camerasBefore = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            Invoke(ui, "Render");
            int tallBackgroundTiles = CountTiles(ui.BgTilemap);
            ui.Controller.Back();
            Invoke(ui, "Render");
            Assert.Less(CountTiles(ui.BgTilemap), tallBackgroundTiles);
            Assert.AreEqual(28 * (PauseMenuController.ItemCount + 4), CountTiles(ui.BgTilemap));
            ui.Controller.Close();
            Invoke(ui, "ClearAll");
            Assert.AreEqual(0, CountTiles(ui.Tilemap));
            Assert.AreEqual(0, CountTiles(ui.BgTilemap));
            Assert.AreEqual(camerasBefore, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length);
        }

        [TestCase(1f, "100%")]
        [TestCase(.75f, "75%")]
        [TestCase(.5f, "50%")]
        public void Adversarial_ResolutionLabels_ReportActualEffectiveChoice(float scale, string label)
        {
            var menu = SteamDeckGraphicsSettingsTests.GraphicsMenu();
            Village3DSettings.WorldResolutionScale = scale;
            StringAssert.Contains(label, menu.GetItemLabel(PauseMenuController.WorldResolutionIndex));
        }

        private GameObject CreateObject(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }
        private static int CountTiles(Tilemap tilemap)
        {
            int count = 0;
            foreach (var position in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(position)) count++;
            return count;
        }
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        private sealed class NativeProbe : IInputProbe
        {
            public bool GetKeyDown(KeyCode key) => InputHelper.GetKeyDown(key);
        }
    }
}
