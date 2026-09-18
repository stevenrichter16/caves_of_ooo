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
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class Village3DControlsTests
    {
        const string ModeKey = "CavesOfOoo.Village3D", DetailKey = "CavesOfOoo.Village3D.LowDetail";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public;
        readonly List<GameObject> objects = new List<GameObject>();
        readonly InputTestFixture input = new InputTestFixture();
        Keyboard keyboard;
        bool hadMode, hadDetail, oldEnabled, oldLow;
        int oldMode, oldDetail, flash, serial;
        Action<string> dirty, message;
        Func<int> tickProvider;
        List<MessageLog.Entry> entries;
        List<string> announcements;

        static PropertyInfo LowProperty => typeof(Village3DSettings).GetProperty("LowDetail", Static);
        static bool Low
        {
            get { Assert.NotNull(LowProperty, "Low-detail runtime preference is required."); return (bool)LowProperty.GetValue(null); }
            set { Assert.NotNull(LowProperty, "Low-detail runtime preference is required."); LowProperty.SetValue(null, value); }
        }
        static void SettingsCall(string method)
        {
            var m = typeof(Village3DSettings).GetMethod(method, Static);
            Assert.NotNull(m, "Missing explicit preference operation: " + method);
            m.Invoke(null, null);
        }
        static void Invoke(Component target, string name)
        {
            var m = target.GetType().GetMethod(name, Instance);
            Assert.NotNull(m, "Actual component lifecycle method required: " + name);
            m.Invoke(target, null);
        }
        [SetUp] public void Setup()
        {
            hadMode = PlayerPrefs.HasKey(ModeKey); oldMode = PlayerPrefs.GetInt(ModeKey);
            hadDetail = PlayerPrefs.HasKey(DetailKey); oldDetail = PlayerPrefs.GetInt(DetailKey);
            oldEnabled = Village3DSettings.Enabled;
            oldLow = LowProperty != null && (bool)LowProperty.GetValue(null);
            dirty = ZoneRenderHooks.FullDirtyCallback; ZoneRenderHooks.FullDirtyCallback = null;
            entries = MessageLog.GetAllEntries(); announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            flash = MessageLog.FlashStamp; serial = MessageLog.NextSerialValue;
            message = MessageLog.OnMessage; tickProvider = MessageLog.TickProvider;
            MessageLog.OnMessage = null; MessageLog.TickProvider = null;
            input.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            Keys();
        }
        [TearDown] public void Teardown()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear(); input.TearDown();
            ZoneRenderHooks.FullDirtyCallback = null;
            Village3DSettings.Enabled = oldEnabled;
            if (LowProperty != null) LowProperty.SetValue(null, oldLow);
            if (hadMode) PlayerPrefs.SetInt(ModeKey, oldMode); else PlayerPrefs.DeleteKey(ModeKey);
            if (hadDetail) PlayerPrefs.SetInt(DetailKey, oldDetail); else PlayerPrefs.DeleteKey(DetailKey);
            PlayerPrefs.Save();
            ZoneRenderHooks.FullDirtyCallback = dirty;
            MessageLog.Restore(entries, announcements, flash, serial);
            MessageLog.OnMessage = message; MessageLog.TickProvider = tickProvider;
        }
        void Keys(params Key[] keys)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        Component Controls()
        {
            var type = typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.Village3DControls");
            Assert.NotNull(type, "The ordinary-game display controller must exist.");
            var go = new GameObject("Village controls owned test"); objects.Add(go);
            var controls = go.AddComponent(type);
            // Explicit EditMode lifecycle invocation; native PlayMode still verifies real boot wiring.
            Invoke(controls, "Awake"); return controls;
        }
        void Press(Component controls, params Key[] keys)
        { Keys(); Keys(keys); Assert.IsTrue(keyboard.f11Key.wasPressedThisFrame); Invoke(controls, "Update"); }
        static Transform Placement(Village3DIntegrationFixture f, string id)
        {
            var matches = f.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name == id).ToArray();
            Assert.AreEqual(1, matches.Length, "Expected one actual authored placement: " + id);
            return matches[0];
        }
        static void AssertTarget(Village3DIntegrationFixture f, float scale)
        {
            var target = f.Presenter.WorldCamera.targetTexture; Assert.NotNull(target);
            Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(f.Source.pixelRect.width * scale)), target.width);
            Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(f.Source.pixelRect.height * scale)), target.height);
            Assert.IsTrue(target.IsCreated(), "The smaller target must actually be allocated.");
        }
        static Village3DManifest Definition => Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath).Definition;

        [Test] public void AbsentPreferencesLoadEnabledFullDetailWithoutCreatingKeys()
        {
            PlayerPrefs.DeleteKey(ModeKey); PlayerPrefs.DeleteKey(DetailKey);
            Village3DSettings.Enabled = false; Low = true; SettingsCall("Load");
            Assert.IsTrue(Village3DSettings.Enabled); Assert.IsFalse(Low);
            Assert.IsFalse(PlayerPrefs.HasKey(ModeKey)); Assert.IsFalse(PlayerPrefs.HasKey(DetailKey));
        }
        [Test] public void LoadReadsBothPersistedChoicesWithoutOverwritingThem()
        {
            PlayerPrefs.SetInt(ModeKey, 0); PlayerPrefs.SetInt(DetailKey, 1);
            Village3DSettings.Enabled = true; Low = false; SettingsCall("Load");
            Assert.IsFalse(Village3DSettings.Enabled); Assert.IsTrue(Low);
            Assert.AreEqual(0, PlayerPrefs.GetInt(ModeKey)); Assert.AreEqual(1, PlayerPrefs.GetInt(DetailKey));
        }
        [Test] public void RuntimeOverridesDirtyOnlyOnChangeAndNeverPersist()
        {
            PlayerPrefs.SetInt(ModeKey, 1); PlayerPrefs.SetInt(DetailKey, 0);
            Village3DSettings.Enabled = true; Low = false;
            var sources = new List<string>(); ZoneRenderHooks.FullDirtyCallback = sources.Add;
            Village3DSettings.Enabled = false; Village3DSettings.Enabled = false;
            Low = true; Low = true;
            Assert.AreEqual(2, sources.Count); Assert.IsTrue(sources.All(s => !string.IsNullOrEmpty(s)));
            Assert.AreEqual(1, PlayerPrefs.GetInt(ModeKey), "Audit/runtime mode must not alter player preference.");
            Assert.AreEqual(0, PlayerPrefs.GetInt(DetailKey));
        }
        [Test] public void ExplicitSaveRoundTripsBothDisplayChoices()
        {
            Village3DSettings.Enabled = false; Low = true; SettingsCall("Save");
            Assert.AreEqual(0, PlayerPrefs.GetInt(ModeKey, -1)); Assert.AreEqual(1, PlayerPrefs.GetInt(DetailKey, -1));
            Village3DSettings.Enabled = true; Low = false; SettingsCall("Load");
            Assert.IsFalse(Village3DSettings.Enabled); Assert.IsTrue(Low);
        }
        [Test] public void ControlsAwakeLoadsPreferenceWithoutAnnouncementOrSave()
        {
            PlayerPrefs.SetInt(ModeKey, 0); PlayerPrefs.SetInt(DetailKey, 1);
            Village3DSettings.Enabled = true; Low = false;
            int count = MessageLog.Count, stamp = MessageLog.FlashStamp; Controls();
            Assert.IsFalse(Village3DSettings.Enabled); Assert.IsTrue(Low);
            Assert.AreEqual(count, MessageLog.Count); Assert.AreEqual(stamp, MessageLog.FlashStamp);
            Assert.AreEqual(0, PlayerPrefs.GetInt(ModeKey)); Assert.AreEqual(1, PlayerPrefs.GetInt(DetailKey));
        }
        [Test] public void ActualF11TogglesAndPersistsModeWithoutTakingATurn()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                PlayerPrefs.SetInt(ModeKey, 1); PlayerPrefs.SetInt(DetailKey, 0); var controls = Controls();
                var turns = new TurnManager(); turns.RestoreSavedState(37, true, f.Player,
                    new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = f.Player, Energy = 731 } });
                var at = f.Zone.GetEntityPosition(f.Player); int stamp = MessageLog.FlashStamp, count = MessageLog.Count;
                Press(controls, Key.F11); f.Refresh();
                Assert.IsFalse(Village3DSettings.Enabled); Assert.IsFalse(f.Presenter.PresentationVisible);
                Assert.AreEqual(0, PlayerPrefs.GetInt(ModeKey)); Assert.IsFalse(Low);
                Assert.AreEqual(37, turns.TickCount); Assert.AreEqual(731, turns.GetEnergy(f.Player));
                Assert.IsTrue(turns.WaitingForInput); Assert.AreSame(f.Player, turns.CurrentActor);
                Assert.AreEqual(at, f.Zone.GetEntityPosition(f.Player));
                Assert.AreEqual(count + 1, MessageLog.Count); StringAssert.Contains("World view", MessageLog.GetLast());
                Assert.AreEqual(stamp, MessageLog.FlashStamp, "A display change is not a modal announcement.");
                Press(controls, Key.F11); f.Refresh(); Assert.IsTrue(f.Presenter.PresentationVisible);
                Assert.AreEqual(1, PlayerPrefs.GetInt(ModeKey)); Assert.AreEqual(37, turns.TickCount);
            }
        }
        [TestCase(Key.LeftShift)] [TestCase(Key.RightShift)]
        public void ActualShiftF11ChangesOnlyDetailAndPersists(Key shift)
        {
            PlayerPrefs.SetInt(ModeKey, 0); PlayerPrefs.SetInt(DetailKey, 0); var controls = Controls();
            int count = MessageLog.Count; Press(controls, shift, Key.F11);
            Assert.IsFalse(Village3DSettings.Enabled, "Detail can be selected while the 3D view is off.");
            Assert.IsTrue(Low); Assert.AreEqual(0, PlayerPrefs.GetInt(ModeKey)); Assert.AreEqual(1, PlayerPrefs.GetInt(DetailKey));
            Assert.AreEqual(count + 1, MessageLog.Count); StringAssert.Contains("World detail", MessageLog.GetLast());
            Press(controls, shift, Key.F11); Assert.IsFalse(Low); Assert.AreEqual(0, PlayerPrefs.GetInt(DetailKey));
        }
        [Test] public void OtherKeysAndHeldF11DoNotRepeatTheToggle()
        {
            PlayerPrefs.SetInt(ModeKey, 1); PlayerPrefs.SetInt(DetailKey, 0); var controls = Controls();
            int count = MessageLog.Count; Keys(Key.F10); Invoke(controls, "Update");
            Assert.IsTrue(Village3DSettings.Enabled); Assert.AreEqual(count, MessageLog.Count);
            Press(controls, Key.F11); Assert.IsFalse(Village3DSettings.Enabled);
            Keys(Key.F11); Assert.IsFalse(keyboard.f11Key.wasPressedThisFrame); Invoke(controls, "Update");
            Assert.IsFalse(Village3DSettings.Enabled); Assert.IsFalse(Low); Assert.AreEqual(count + 1, MessageLog.Count);
        }
        [Test] public void LowDetailReplacesOnlyOwnedTargetAtThreeQuarterResolutionAndReusesIt()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                Low = false; f.Refresh(); AssertTarget(f, 1); var full = f.Presenter.WorldCamera.targetTexture;
                var camera = f.Presenter.WorldCamera; var rect = f.Source.rect; var position = camera.transform.position;
                float size = camera.orthographicSize, aspect = camera.aspect; int mask = f.Source.cullingMask;
                Low = true; f.Refresh(); AssertTarget(f, .75f); var reduced = camera.targetTexture;
                Assert.AreNotSame(full, reduced); Assert.IsTrue(full == null || !full.IsCreated(), "Previous owned target must be released.");
                f.Refresh(); Assert.AreSame(reduced, camera.targetTexture, "Steady-state detail may not churn targets.");
                Assert.AreSame(f.BorrowedTarget, f.Source.targetTexture); Assert.AreEqual(rect, f.Source.rect);
                Assert.AreEqual(mask, f.Source.cullingMask); Assert.AreEqual(position, camera.transform.position);
                Assert.AreEqual(size, camera.orthographicSize); Assert.AreEqual(aspect, camera.aspect);
                Low = false; f.Refresh(); AssertTarget(f, 1); Assert.AreNotSame(reduced, camera.targetTexture);
                Assert.IsTrue(reduced == null || !reduced.IsCreated());
            }
        }
        [Test] public void LowDetailHidesOnlyDecorationRoleAndRestoresExactNativeViews()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                Low = false; f.Refresh(); var definition = Definition;
                Assert.AreEqual(31, definition.staticPlacements.Count(p => p.role == "decoration"));
                Assert.AreEqual(80, definition.staticPlacements.Count(p => p.role != "decoration"));
                var before = definition.owners.Select(p => f.View(p.ownerId)).ToArray();
                var drawn = before.Select(v => Village3DIntegrationFixture.Drawn(v.root)).ToArray();
                foreach (var p in definition.staticPlacements) Assert.IsTrue(Village3DIntegrationFixture.Drawn(Placement(f, p.id).gameObject), p.id);
                Low = true; f.Refresh();
                foreach (var p in definition.staticPlacements)
                    Assert.AreEqual(p.role != "decoration", Village3DIntegrationFixture.Drawn(Placement(f, p.id).gameObject), p.id + ": " + p.role);
                for (int i = 0; i < before.Length; i++)
                {
                    var actual = f.View(definition.owners[i].ownerId);
                    Assert.AreSame(before[i].owner, actual.owner); Assert.AreSame(before[i].root, actual.root);
                    Assert.AreEqual(drawn[i], Village3DIntegrationFixture.Drawn(actual.root), definition.owners[i].ownerId);
                }
                Low = false; f.Refresh();
                foreach (var p in definition.staticPlacements) Assert.IsTrue(Village3DIntegrationFixture.Drawn(Placement(f, p.id).gameObject), p.id);
            }
        }
        [Test] public void LowDetailDisablesOnlyOwnedSunShadowsAndRestoresThem()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                Low = false; f.Refresh(); var suns = f.Root.GetComponentsInChildren<Light>(true);
                Assert.AreEqual(1, suns.Length); var sun = suns[0]; Assert.AreEqual(LightShadows.Soft, sun.shadows);
                var color = sun.color; float intensity = sun.intensity; var rotation = sun.transform.rotation;
                Low = true; f.Refresh(); Assert.AreEqual(LightShadows.None, sun.shadows);
                Assert.IsTrue(sun.enabled); Assert.AreEqual(intensity, sun.intensity); Assert.AreEqual(color, sun.color);
                Assert.AreEqual(rotation, sun.transform.rotation); Low = false; f.Refresh(); Assert.AreEqual(LightShadows.Soft, sun.shadows);
            }
        }
        [Test] public void LowDetailAppliesOnInitialBindBeforeAnotherFrame()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                int members = f.Zone.GetReadOnlyEntities().Count;
                f.Presenter.Bind(null, f.Source); Low = true;
                f.Presenter.Bind(f.Zone, f.Source);
                Assert.IsTrue(f.Presenter.IsReady, f.Presenter.Failure); Assert.IsTrue(f.Presenter.PresentationVisible);
                AssertTarget(f, .75f);
                Assert.AreEqual(LightShadows.None, f.Root.GetComponentsInChildren<Light>(true).Single().shadows);
                foreach (var p in Definition.staticPlacements)
                    Assert.AreEqual(p.role != "decoration", Village3DIntegrationFixture.Drawn(Placement(f, p.id).gameObject), p.id);
                Assert.AreEqual(members, f.Zone.GetReadOnlyEntities().Count);
                Assert.AreSame(f.BorrowedTarget, f.Source.targetTexture);
            }
        }

        [Test] public void DetailChangesPreserveNativeStateAndPickingWithHiddenModeControl()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                Low = false; f.Refresh(); Assert.IsTrue(f.PickOwner("north-guard-west", out var point));
                var original = f.View("north-guard-west").owner; var members = f.Zone.GetReadOnlyEntities().ToArray();
                var positions = members.Select(f.Zone.GetEntityPosition).ToArray(); int version = f.Zone.EntityVersion;
                var state = MorrowfastSceneRuntime.GetState(f.Zone); string doors = state.OpenDoorIds, roofs = state.LiftedRoofIds, removed = state.RemovedIds;
                var inventory = f.Player.GetPart<InventoryPart>().Objects.ToArray();
                Low = true; f.Refresh();
                Assert.IsTrue(f.Presenter.TryPickWorld(point, out var picked, out int x, out int y)); Assert.AreSame(original, picked);
                Assert.AreEqual(f.Zone.GetEntityPosition(original), (x, y));
                CollectionAssert.AreEquivalent(members, f.Zone.GetReadOnlyEntities()); CollectionAssert.AreEqual(positions, members.Select(f.Zone.GetEntityPosition));
                CollectionAssert.AreEqual(inventory, f.Player.GetPart<InventoryPart>().Objects); Assert.AreEqual(version, f.Zone.EntityVersion);
                Assert.AreEqual(doors, state.OpenDoorIds); Assert.AreEqual(roofs, state.LiftedRoofIds); Assert.AreEqual(removed, state.RemovedIds);
                f.Presenter.SetPresentationVisible(false); Assert.IsFalse(f.Presenter.TryPickWorld(point, out _, out _, out _));
                Low = false; f.Presenter.SetPresentationVisible(true); f.Refresh();
                Assert.IsTrue(f.Presenter.TryPickWorld(point, out picked, out _, out _)); Assert.AreSame(original, picked);
            }
        }
    }
}
