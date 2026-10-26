using System;
using System.Collections.Generic;
using System.Linq;
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
    // Existing input boundary supplies RED before the new camera helper exists.
    // Pixel assertions measure projection; actual legibility remains a Play gate.
    public sealed class FlatPixelZoomInputTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Object> owned = new List<Object>();
        readonly InputTestFixture devices = new InputTestFixture();
        HotbarSaveFixture globals;
        Keyboard keyboard;
        InputHandler input;
        CameraFollow follow;
        Camera camera;
        Entity player;
        Zone zone;
        TurnManager turns;

        [SetUp] public void Setup()
        { globals = new HotbarSaveFixture(false, false); devices.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); }
        [TearDown] public void Cleanup()
        {
            try
            {
                foreach (var value in owned)
                    if (value is GameObject host && host != null && host.TryGetComponent<Camera>(out var view)) view.targetTexture = null;
                for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
                owned.Clear();
            }
            finally { devices.TearDown(); globals?.Dispose(); globals = null; }
        }
        T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        void Create(int width = 1280, int height = 800, bool native = true)
        {
            var target = Own(new RenderTexture(width, height, 0));
            var cameraHost = Own(new GameObject("Owned zoom input camera")); camera = cameraHost.AddComponent<Camera>();
            camera.targetTexture = target; camera.orthographic = true; camera.aspect = 1.97277653f;
            camera.transform.position = new Vector3(0, 0, -10);
            var grid = Own(new GameObject("Owned zoom grid", typeof(Grid))); grid.GetComponent<Grid>().cellSize = new Vector3(1, 1.5f, 1);
            var mapHost = new GameObject("Owned zoom world", typeof(Tilemap)); mapHost.transform.SetParent(grid.transform, false);
            player = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "Player" };
            player.SetTag("Player"); player.SetTag("Creature"); player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new RenderPart { DisplayName = "you" }); player.AddPart(new InventoryPart());
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            zone = new Zone("FlatPixelZoomInput"); Assert.True(zone.AddEntity(player, 40, 12));
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true;
            turns = new TurnManager(); turns.AddEntity(player); turns.ProcessUntilPlayerTurn();
            follow = cameraHost.AddComponent<CameraFollow>(); follow.Player = player; follow.CurrentZone = zone;
            follow.WorldTilemap = mapHost.GetComponent<Tilemap>(); follow.WorldPixelsPerUnit = native ? 16 : 0;
            follow.TargetVisibleTileRows = 34; follow.GameplayZoomMultiplier = 1.2f;
            var inputHost = Own(new GameObject("Owned inactive zoom input")); inputHost.SetActive(false);
            input = inputHost.AddComponent<InputHandler>(); input.PlayerEntity = player; input.CurrentZone = zone;
            input.TurnManager = turns; input.CameraFollow = follow;
            follow.SnapToPlayer();
        }
        object Call(string name, params object[] args)
        {
            var method = typeof(InputHandler).GetMethod(name, I); Assert.NotNull(method, name);
            try { return method.Invoke(input, args); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        void Zoom(bool closer) => Call("ControllerZoom", closer ? .85f : 1f / .85f);
        float Scale => camera.pixelRect.height / (2f * camera.orthographicSize * 16f);
        void ExpectScale(int value) => Assert.AreEqual(value, Scale, .001f);
        void Aligned()
        {
            var rect = camera.pixelRect;
            float px = rect.width / (2f * camera.orthographicSize * camera.aspect);
            float py = rect.height / (2f * camera.orthographicSize);
            Assert.AreEqual(px, py, .001f); Assert.AreEqual(Mathf.Round(Scale), Scale, .001f);
            Assert.GreaterOrEqual(Scale, 1f);
            float left = camera.transform.position.x * px - rect.width * .5f - rect.x;
            float bottom = camera.transform.position.y * py - rect.height * .5f - rect.y;
            Assert.AreEqual(Mathf.Round(left), left, .001f); Assert.AreEqual(Mathf.Round(bottom), bottom, .001f);
        }
        void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
            typeof(InputHandler).GetField("_lastMoveTime", I).SetValue(input, -999f);
            Call("UpdateInput");
        }
        void Mode(string state)
        { var field = typeof(InputHandler).GetField("_inputState", I); field.SetValue(input, Enum.Parse(field.FieldType, state)); }
        void NoTurn(int tick, int energy)
        { Assert.AreEqual(tick, turns.TickCount); Assert.AreEqual(energy, turns.GetEnergy(player)); Assert.AreEqual((40, 12), zone.GetEntityPosition(player)); }
        void Pad(Gamepad pad, GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state); InputSystem.Update();
            typeof(InputHandler).GetField("_lastMoveTime", I).SetValue(input, -999f);
            typeof(InputHandler).GetField("_lastWaitTime", I).SetValue(input, -999f);
            Call("Update"); // Includes the real controller context and held-input quarantine.
        }

        [Test] public void FirstControllerZoomAtDeckResolutionImmediatelyReachesTwoNativePixels()
        {
            Create(); ExpectScale(1); int tick = turns.TickCount, energy = turns.GetEnergy(player);
            Zoom(true); ExpectScale(2); Aligned();
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier); NoTurn(tick, energy);
        }

        [Test] public void RepeatedControllerPressesClampAtFourAndReverseOneStepBackToOne()
        {
            Create(); ExpectScale(1);
            foreach (int expected in new[] { 2, 3, 4, 4, 4 }) { Zoom(true); ExpectScale(expected); }
            foreach (int expected in new[] { 3, 2, 1, 1, 1 }) { Zoom(false); ExpectScale(expected); }
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier); Zoom(true); ExpectScale(2);
        }

        [TestCase(1280, 800)] [TestCase(1281, 801)] [TestCase(1920, 1080)] [TestCase(3840, 2160)]
        public void IntegerZoomKeepsSquarePixelsAndAlignedEdgesAtCenterAndZoneCorners(int width, int height)
        {
            Create(width, height); Aligned(); int initial = Mathf.RoundToInt(Scale);
            Zoom(true); ExpectScale(Mathf.Min(4, initial + 1)); Aligned();
            Assert.True(zone.MoveEntity(player, 0, 0)); follow.SnapToPlayer(); Aligned();
            Assert.True(zone.MoveEntity(player, Zone.Width - 1, Zone.Height - 1)); follow.SnapToPlayer(); Aligned();
            Zoom(false); ExpectScale(Mathf.Max(1, Mathf.Min(4, initial + 1) - 1)); Aligned();
        }

        [Test] public void FirstStepStartsFromTheActuallyDisplayedAutomaticScale()
        {
            Create(); follow.TargetVisibleTileRows = 10; follow.SnapToPlayer(); ExpectScale(2);
            Zoom(true); ExpectScale(3); Zoom(false); ExpectScale(2);
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier);
        }

        [Test] public void AutomaticScaleAboveManualCapNeverReversesZoomDirection()
        {
            Create(3840, 2160); follow.TargetVisibleTileRows = 4; follow.SnapToPlayer();
            int initial = Mathf.RoundToInt(Scale); Assert.Greater(initial, 4);
            Zoom(true); ExpectScale(initial);
            Zoom(false); ExpectScale(initial - 1); Aligned();
        }

        [TestCase("ui")] [TestCase("fullscreen")] [TestCase("centered")]
        public void PausedUiCannotZoomOrSilentlyChangeTheReturningWorld(string mode)
        {
            Create(); ExpectScale(1);
            if (mode == "ui") follow.SetUIView(80, 45);
            else if (mode == "fullscreen") follow.SetFullscreenOverlayView();
            else follow.SetCenteredPopupOverlayView();
            float size = camera.orthographicSize, aspect = camera.aspect;
            var rect = camera.rect; var position = camera.transform.position;
            Zoom(true); Zoom(true); Zoom(false);
            Assert.AreEqual(size, camera.orthographicSize); Assert.AreEqual(aspect, camera.aspect);
            Assert.AreEqual(rect, camera.rect); Assert.AreEqual(position, camera.transform.position);
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier);
            follow.RestoreGameView(); ExpectScale(1); Zoom(true); ExpectScale(2);
        }

        [Test] public void LegacyContinuousZoomStillMultipliesAndClampsItsExistingRange()
        {
            Create(native: false); Assert.AreEqual(30.6f, camera.orthographicSize, .001f);
            Zoom(true); Assert.AreEqual(1.2f * .85f, follow.GameplayZoomMultiplier, .0001f);
            Assert.AreEqual(30.6f * .85f, camera.orthographicSize, .001f);
            Zoom(false); Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier, .0001f);
            for (int i = 0; i < 12; i++) Zoom(true);
            Assert.AreEqual(.5f, follow.GameplayZoomMultiplier, .0001f);
            for (int i = 0; i < 15; i++) Zoom(false);
            Assert.AreEqual(1.5f, follow.GameplayZoomMultiplier, .0001f);
        }

        [Test] public void NativeZoomDoesNotRewriteTheLegacyWorldPreference()
        {
            Create(); Zoom(true); Zoom(true); ExpectScale(3);
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier);
            follow.WorldPixelsPerUnit = 0; follow.SnapToPlayer();
            Assert.AreEqual(30.6f, camera.orthographicSize, .001f);
        }

        [Test] public void MissingCameraControllerInputIsAHarmlessNoOp()
        { Create(); input.CameraFollow = null; Assert.DoesNotThrow(() => Zoom(true)); ExpectScale(1); }

        [Test] public void RealKeyboardEdgesZoomOnceAndPageDownReversesWithoutATurn()
        {
            Create(); int tick = turns.TickCount, energy = turns.GetEnergy(player); Keys(); ExpectScale(1);
            Keys(Key.PageUp); ExpectScale(2);
            Keys(Key.PageUp); Keys(Key.PageUp); ExpectScale(2); // Held state, not a second press.
            Keys(); Keys(Key.PageUp); ExpectScale(3);
            Keys(); Keys(Key.PageDown); ExpectScale(2); Keys(); Keys(Key.PageDown); ExpectScale(1);
            NoTurn(tick, energy); Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier);
        }

        [TestCase(Key.Equals)] [TestCase(Key.Minus)]
        public void ExistingSidebarPlusAndMinusDoNotBecomeZoomKeys(Key key)
        {
            Create(); int tick = turns.TickCount, energy = turns.GetEnergy(player);
            Keys(); Keys(key); ExpectScale(1); Keys(); Keys(Key.LeftShift, key); ExpectScale(1);
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier); NoTurn(tick, energy);
        }

        [Test] public void LookModeOwnsItsInputAndDoesNotChangeWorldZoom()
        {
            Create(); int tick = turns.TickCount, energy = turns.GetEnergy(player);
            Assert.True((bool)Call("EnterLookMode")); Keys(); Keys(Key.PageUp); ExpectScale(1);
            Keys(); Keys(Key.PageDown); ExpectScale(1); NoTurn(tick, energy);
            Assert.AreEqual("LookMode", typeof(InputHandler).GetField("_inputState", I).GetValue(input).ToString());
        }

        [Test] public void AnnouncementPageKeysStillTurnPagesInsteadOfZoomingTheWorld()
        {
            Create(); int tick = turns.TickCount, energy = turns.GetEnergy(player);
            var ui = input.gameObject.AddComponent<AnnouncementUI>(); input.AnnouncementUI = ui;
            var grid = Own(new GameObject("Owned zoom announcement grid", typeof(Grid)));
            var foreground = new GameObject("Owned zoom announcement text", typeof(Tilemap)); foreground.transform.SetParent(grid.transform, false);
            var background = new GameObject("Owned zoom announcement background", typeof(Tilemap)); background.transform.SetParent(grid.transform, false);
            ui.Tilemap = foreground.GetComponent<Tilemap>(); ui.BgTilemap = background.GetComponent<Tilemap>();
            ui.Open(string.Join("\n", Enumerable.Range(0, 55).Select(i => "Owned reader line " + i)));
            Assert.Greater(ui.PageCount, 1); Mode("AnnouncementOpen"); follow.SetUIView(80, 45);
            float size = camera.orthographicSize; var rect = camera.rect;
            Keys(); Keys(Key.PageDown); Assert.AreEqual(1, ui.CurrentPage);
            Keys(); Keys(Key.PageUp); Assert.AreEqual(0, ui.CurrentPage);
            Assert.AreEqual(size, camera.orthographicSize); Assert.AreEqual(rect, camera.rect); Assert.True(ui.IsOpen);
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier); NoTurn(tick, energy);
            follow.RestoreGameView(); ExpectScale(1);
        }

        [Test] public void ActualGamepadZoomChordUsesEdgesAndDoesNotMoveOrRepeatWhileHeld()
        {
            Create(); int tick = turns.TickCount, energy = turns.GetEnergy(player);
            var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent(); Pad(pad, new GamepadState());
            var closer = new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.DpadLeft);
            Pad(pad, closer); ExpectScale(2); Pad(pad, closer); Pad(pad, closer); ExpectScale(2);
            Pad(pad, new GamepadState { leftTrigger = 1 });
            Pad(pad, new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.DpadRight)); ExpectScale(1);
            Pad(pad, new GamepadState());
            Pad(pad, new GamepadState().WithButton(GamepadButton.DpadLeft)); ExpectScale(1);
            NoTurn(tick, energy); Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier);
        }

        [Test] public void SelectedScaleSurvivesOverlayResizeZoneTransitionAndLegacyRoundTrip()
        {
            Create(); Zoom(true); ExpectScale(2); follow.SetFullscreenOverlayView();
            camera.targetTexture = Own(new RenderTexture(1281, 801, 0));
            follow.RestoreGameView(); ExpectScale(2); Aligned();
            Assert.True(zone.RemoveEntity(player)); var destination = new Zone("OwnedZoomDestination");
            Assert.True(destination.AddEntity(player, 7, 18)); zone = destination;
            input.CurrentZone = zone; follow.CurrentZone = zone; follow.SnapToPlayer(); ExpectScale(2); Aligned();
            follow.WorldPixelsPerUnit = 0; follow.SnapToPlayer();
            Assert.AreEqual(30.6f, camera.orthographicSize, .001f);
            follow.WorldPixelsPerUnit = 16; follow.SnapToPlayer(); ExpectScale(2); Aligned();
            Assert.AreEqual(1.2f, follow.GameplayZoomMultiplier);
        }

        [Test] public void SimultaneousZoomAndDirectionDoesNotAlsoSpendAMovementTurn()
        {
            Create(); int tick = turns.TickCount, energy = turns.GetEnergy(player); Keys();
            Keys(Key.PageUp, Key.RightArrow); ExpectScale(2); NoTurn(tick, energy);
            Keys(); Keys(Key.RightArrow); // Same direction works when no zoom edge owns the input.
            Assert.AreEqual((41, 12), zone.GetEntityPosition(player)); Assert.Greater(turns.TickCount, tick); ExpectScale(2);
        }

        [Test] public void ZoomPreservesHotbarSelectionEvenWhenSelectionKeyArrivesOnTheSameFrame()
        {
            Create(); var abilities = new ActivatedAbilitiesPart(); player.AddPart(abilities);
            var first = abilities.AddAbility("First owned zoom ability", "OwnedZoomFirst", "test");
            var second = abilities.AddAbility("Second owned zoom ability", "OwnedZoomSecond", "test");
            abilities.AssignAbilityToSlot(first, 1); abilities.AssignAbilityToSlot(second, 4);
            var selected = typeof(InputHandler).GetField("_selectedHotbarSlot", I); selected.SetValue(input, 4);
            int tick = turns.TickCount, energy = turns.GetEnergy(player); Keys();
            Keys(Key.PageUp, Key.LeftBracket); ExpectScale(2); Assert.AreEqual(4, selected.GetValue(input));
            Keys(); Keys(Key.PageDown); ExpectScale(1); Assert.AreEqual(4, selected.GetValue(input));
            Assert.AreEqual(0, abilities.GetAbilityBySlot(1).CooldownRemaining);
            Assert.AreEqual(0, abilities.GetAbilityBySlot(4).CooldownRemaining); NoTurn(tick, energy);
            Keys(); Keys(Key.LeftBracket); Assert.AreEqual(1, selected.GetValue(input)); NoTurn(tick, energy);
        }
    }
}
