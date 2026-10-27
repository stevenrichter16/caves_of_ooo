using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Presentation.Effects;
using CavesOfOoo.Presentation.Rendering;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Presentation-field lifecycle only: inactive owned components, no bootstrap,
    // zone rendering, save writes, global hooks, or claim of visual parity.
    public sealed class FlatPixelShaleParityPreviewTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public;
        const string Target = "Overworld.16.13.6";
        static readonly Color PreviewUnknown = new Color(.114f, .085f, .070f, .75f);
        GameObject root;
        Component preview;
        Type previewType;
        MethodInfo configure, tick, restore;
        FieldInfo studyEnabled;
        bool oldEnabled, changedRoot;
        string oldRoot, isolatedRoot;
        ZoneRenderer renderer;
        BiomeColorPatcher patcher;
        LightSourceSpriteHook lights;
        Volume volume;
        VolumeProfile shared, runtime;
        ColorAdjustments adjustments, sharedAdjustments;
        OverworldZoneManager manager;
        Zone zone;
        State initial;

        struct State
        {
            public bool Patcher, Torch;
            public Color Filter, Unknown;
            public float Contrast, Saturation, Ambient;
        }

        State Capture() => new State
        {
            Patcher = patcher.enabled, Torch = lights.PlayerTorchEnabled,
            Filter = adjustments.colorFilter.value, Unknown = renderer.UnexploredColor,
            Contrast = adjustments.contrast.value, Saturation = adjustments.saturation.value,
            Ambient = lights.DungeonAmbientIntensity
        };

        static FieldInfo Field(string name)
        {
            var field = typeof(ZoneRenderer).GetField(name, Instance);
            Assert.NotNull(field, name);
            return field;
        }

        void Show(Zone graph)
        {
            // SetZone also binds global tile-state dirty hooks. This fixture only
            // supplies the existing displayed-zone reference to the preview gate.
            var property = typeof(ZoneRenderer).GetProperty("CurrentZone", Instance);
            Assert.NotNull(property);
            var setter = property.GetSetMethod(true);
            Assert.NotNull(setter);
            setter.Invoke(renderer, new object[] { graph });
        }

        Zone Attach(string id)
        {
            var graph = new Zone(id);
            manager.SetActiveZone(graph);
            return graph;
        }

        void Invoke(MethodInfo method, params object[] args)
        {
            try { method.Invoke(preview, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        void Configure(string missing = null) => Invoke(configure,
            missing == "renderer" ? null : renderer,
            missing == "patcher" ? null : patcher,
            missing == "lights" ? null : lights,
            missing == "adjustments" ? null : adjustments);

        void Enabled(bool value) => studyEnabled.SetValue(null, value);
        void Tick() => Invoke(tick);
        void Restore() => Invoke(restore);

        [SetUp]
        public void Setup()
        {
            root = null; preview = null; studyEnabled = null; changedRoot = false;
            shared = null; runtime = null; isolatedRoot = null;
            previewType = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelShaleParityPreview");
            Assert.NotNull(previewType, "Missing reversible isolated presentation preview.");
            Assert.True(typeof(MonoBehaviour).IsAssignableFrom(previewType));
            configure = previewType.GetMethod("Configure", BindingFlags.Instance | BindingFlags.Public, null,
                new[] { typeof(ZoneRenderer), typeof(BiomeColorPatcher), typeof(LightSourceSpriteHook), typeof(ColorAdjustments) }, null);
            tick = previewType.GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            restore = previewType.GetMethod("Restore", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assert.NotNull(configure); Assert.NotNull(tick); Assert.NotNull(restore);
            Assert.AreEqual(typeof(void), configure.ReturnType);
            Assert.AreEqual(typeof(void), tick.ReturnType);
            Assert.AreEqual(typeof(void), restore.ReturnType);
            var study = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelShaleParityStudy");
            Assert.NotNull(study);
            studyEnabled = study.GetField("Enabled", Static);
            Assert.NotNull(studyEnabled); Assert.AreEqual(typeof(bool), studyEnabled.FieldType);
            oldEnabled = (bool)studyEnabled.GetValue(null);
            Assert.False(SaveGameService.HasPendingSave, "Do not flush another session's pending save.");
            oldRoot = SaveGameService.SaveRootOverride;
            isolatedRoot = Path.Combine(Path.GetTempPath(), "coo-shale-preview-test-" + Guid.NewGuid().ToString("N"));
            Assert.False(Directory.Exists(isolatedRoot));
            SaveGameService.SaveRootOverride = isolatedRoot; changedRoot = true;
            Enabled(false);

            manager = OverworldZoneManager.CreateDetached(new EntityFactory(), 65, true);
            zone = Attach(Target);
            Assert.True(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
            root = new GameObject("Owned inactive shale preview fixture");
            root.SetActive(false);
            renderer = root.AddComponent<ZoneRenderer>();
            var environment = root.AddComponent<EnvironmentSpriteRenderer>();
            Field("_envSpriteRenderer").SetValue(renderer, environment);
            Field("_flatPixelModeEnabled").SetValue(renderer, true);
            Show(zone);
            Assert.True(renderer.FlatPixelModeActive, "Real flat-mode predicate must be satisfied.");
            Assert.IsNull(Field("_tilemap").GetValue(renderer), "Do not initialize or repaint the test renderer.");
            patcher = root.AddComponent<BiomeColorPatcher>();
            lights = root.AddComponent<LightSourceSpriteHook>();
            volume = root.AddComponent<Volume>();
            shared = ScriptableObject.CreateInstance<VolumeProfile>();
            sharedAdjustments = shared.Add<ColorAdjustments>(true);
            sharedAdjustments.colorFilter.value = new Color(.26f, .37f, .48f, 1f);
            sharedAdjustments.contrast.value = -9f;
            sharedAdjustments.saturation.value = 18f;
            volume.sharedProfile = shared;
            runtime = volume.profile;
            Assert.AreNotSame(shared, runtime);
            Assert.True(runtime.TryGet(out adjustments));
            Assert.AreNotSame(sharedAdjustments, adjustments);
            patcher.enabled = true;
            adjustments.colorFilter.value = new Color(.62f, .73f, .81f, .92f);
            adjustments.contrast.value = -17f;
            adjustments.saturation.value = 23f;
            adjustments.postExposure.value = .4f;
            lights.DungeonAmbientIntensity = .31f;
            lights.PlayerTorchEnabled = true;
            lights.OutdoorAmbientIntensity = .63f;
            lights.PlayerTorchColor = new Color(.2f, .3f, .4f, 1f);
            lights.PlayerTorchIntensity = 2.3f;
            renderer.UnexploredColor = new Color(.18f, .13f, .27f, .61f);
            renderer.BackgroundColor = new Color(.09f, .12f, .15f, .8f);
            initial = Capture();
            preview = root.AddComponent(previewType);
            Configure();
        }

        [TearDown]
        public void Cleanup()
        {
            try { if (preview != null && restore != null) Restore(); }
            finally
            {
                if (studyEnabled != null) studyEnabled.SetValue(null, oldEnabled);
                if (changedRoot) SaveGameService.SaveRootOverride = oldRoot;
                if (root != null) Object.DestroyImmediate(root);
                DestroyProfile(runtime); DestroyProfile(shared);
                if (isolatedRoot != null) Assert.False(Directory.Exists(isolatedRoot));
            }
        }

        static void DestroyProfile(VolumeProfile profile)
        {
            if (profile == null) return;
            foreach (var component in profile.components) if (component != null) Object.DestroyImmediate(component);
            Object.DestroyImmediate(profile);
        }

        void AssertState(State expected)
        {
            Assert.AreEqual(expected.Patcher, patcher.enabled);
            Assert.AreEqual(expected.Torch, lights.PlayerTorchEnabled);
            Assert.AreEqual(expected.Filter, adjustments.colorFilter.value);
            Assert.AreEqual(expected.Contrast, adjustments.contrast.value);
            Assert.AreEqual(expected.Saturation, adjustments.saturation.value);
            Assert.AreEqual(expected.Ambient, lights.DungeonAmbientIntensity);
            Assert.AreEqual(expected.Unknown, renderer.UnexploredColor);
            AssertUnrelated();
        }

        void AssertUnrelated()
        {
            Assert.AreEqual(new Color(.26f, .37f, .48f, 1f), sharedAdjustments.colorFilter.value);
            Assert.AreEqual(-9f, sharedAdjustments.contrast.value);
            Assert.AreEqual(18f, sharedAdjustments.saturation.value);
            Assert.AreEqual(.4f, adjustments.postExposure.value);
            Assert.AreEqual(.63f, lights.OutdoorAmbientIntensity);
            Assert.AreEqual(new Color(.2f, .3f, .4f, 1f), lights.PlayerTorchColor);
            Assert.AreEqual(2.3f, lights.PlayerTorchIntensity);
            Assert.AreEqual(new Color(.09f, .12f, .15f, .8f), renderer.BackgroundColor);
            Assert.AreSame(runtime, volume.profile);
            Assert.False(lights.IsInitialized);
            Assert.IsNull(Field("_tilemap").GetValue(renderer));
            Assert.AreEqual(0, root.transform.childCount, "No renderer/bootstrap/light objects are created.");
        }

        void AssertActive()
        {
            Assert.False(patcher.enabled);
            Assert.AreEqual(Color.white, adjustments.colorFilter.value);
            Assert.AreEqual(0f, adjustments.contrast.value);
            Assert.AreEqual(0f, adjustments.saturation.value);
            Assert.AreEqual(1f, lights.DungeonAmbientIntensity);
            Assert.False(lights.PlayerTorchEnabled);
            Assert.AreEqual(PreviewUnknown, renderer.UnexploredColor);
            AssertUnrelated();
        }

        [Test]
        public void DisabledStudyLeavesAllPresentationAndSimulationStateUntouched()
        {
            int version = zone.EntityVersion, count = manager.CachedZoneCount;
            string tiles = zone.TileState.ToSaveString();
            var cell = zone.GetCell(10, 10);
            bool visible = cell.IsVisible, explored = cell.Explored;
            Tick(); AssertState(initial);
            Enabled(true); Tick(); AssertActive();
            Assert.AreSame(zone, renderer.CurrentZone);
            Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(count, manager.CachedZoneCount);
            Assert.AreEqual(tiles, zone.TileState.ToSaveString());
            Assert.AreEqual(visible, cell.IsVisible); Assert.AreEqual(explored, cell.Explored);
            Restore(); AssertState(initial);
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void ActivePreviewRestoresExactNondefaultValuesIncludingInitiallyDisabledFlags(bool gradingEnabled, bool torchEnabled)
        {
            patcher.enabled = gradingEnabled; lights.PlayerTorchEnabled = torchEnabled;
            var expected = Capture();
            Enabled(true); Tick(); AssertActive();
            Restore(); AssertState(expected);
        }

        [TestCase("disabled")]
        [TestCase("departure")]
        [TestCase("nonflat")]
        [TestCase("normal-root")]
        public void LosingAnyLiveAdmissionRestoresTheCapturedPresentation(string reason)
        {
            Enabled(true); Tick(); AssertActive();
            if (reason == "disabled") Enabled(false);
            if (reason == "departure") Show(Attach("Overworld.16.13.5"));
            if (reason == "nonflat") Field("_flatPixelModeEnabled").SetValue(renderer, false);
            if (reason == "normal-root") SaveGameService.SaveRootOverride = null;
            Tick(); AssertState(initial);
            manager.SetActiveZone(zone); Show(zone);
            Field("_flatPixelModeEnabled").SetValue(renderer, true);
            SaveGameService.SaveRootOverride = isolatedRoot; Enabled(true);
            Tick(); AssertActive(); Restore(); AssertState(initial);
        }

        [Test]
        public void RepeatedActiveTicksNeverReplaceTheOriginalSnapshotWithPreviewValues()
        {
            Enabled(true); Tick(); AssertActive();
            for (int i = 0; i < 4; i++) { Tick(); AssertActive(); }
            Restore(); AssertState(initial);
        }

        [Test]
        public void FullscreenPauseRestoresPresentationThenUnpauseReactivatesTheStudy()
        {
            Enabled(true); Tick(); AssertActive();
            renderer.Paused = true;
            Assert.True(renderer.FlatPixelModeActive,
                "Fullscreen UI retains flat zone ownership; pause must be a separate preview exclusion.");
            Tick(); AssertState(initial);
            Assert.True(renderer.Paused, "The preview must not close the borrowing fullscreen page.");
            Assert.AreSame(zone, renderer.CurrentZone);
            renderer.Paused = false;
            Tick(); AssertActive();
            Restore(); AssertState(initial);
            // This inactive renderer has no initialized tilemap. Actual fullscreen
            // canvas preservation requires root's separate in-game countercheck.
        }

        [Test]
        public void RepeatedRestoreIsANoopAfterOwnershipHasBeenReleased()
        {
            Enabled(true); Tick(); AssertActive(); Restore(); AssertState(initial);
            adjustments.contrast.value = 8f;
            renderer.UnexploredColor = new Color(.11f, .22f, .33f, .44f);
            var later = Capture();
            Restore(); AssertState(later);
        }

        [Test]
        public void ReentryCapturesTheNewPresentationRatherThanRestoringAStaleFirstSnapshot()
        {
            Enabled(true); Tick(); AssertActive(); Restore(); AssertState(initial);
            patcher.enabled = false; lights.PlayerTorchEnabled = false;
            adjustments.colorFilter.value = new Color(.7f, .4f, .2f, .8f);
            adjustments.contrast.value = 12f; adjustments.saturation.value = -31f;
            lights.DungeonAmbientIntensity = .72f;
            renderer.UnexploredColor = new Color(.23f, .34f, .45f, .56f);
            var next = Capture();
            Tick(); AssertActive(); Restore(); AssertState(next);
        }

        [TestCase("renderer")]
        [TestCase("patcher")]
        [TestCase("lights")]
        [TestCase("adjustments")]
        public void MissingRequiredDependencyFailsClosedWithoutPartialMutation(string missing)
        {
            Configure(missing); Enabled(true);
            Tick(); AssertState(initial);
            Restore(); AssertState(initial);
            Configure(); Tick(); AssertActive(); Restore(); AssertState(initial);
        }

        [Test]
        public void OnDisablePathRestoresAndDoesNotRetainSnapshotOwnership()
        {
            Enabled(true); Tick(); AssertActive();
            var onDisable = previewType.GetMethod("OnDisable", Instance, null, Type.EmptyTypes, null);
            Assert.NotNull(onDisable, "Unity disable lifecycle must restore the preview.");
            // Keep all fixture GameObjects inactive; invoke the exact lifecycle
            // handler instead of starting the renderer's unrelated Awake graph.
            Invoke(onDisable); AssertState(initial);
            adjustments.saturation.value = 11f;
            var later = Capture();
            Restore(); AssertState(later);
        }

        [Test]
        public void DisableWhileFullscreenSuspendedRevokesStudyBeforeWorldResumes()
        {
            Enabled(true); Tick(); AssertActive();
            renderer.Paused = true;
            Tick(); AssertState(initial);
            Assert.True((bool)studyEnabled.GetValue(null), "Temporary menu suspension keeps the explicit study request.");
            var onDisable = previewType.GetMethod("OnDisable", Instance, null, Type.EmptyTypes, null);
            Assert.NotNull(onDisable);
            Invoke(onDisable); AssertState(initial);
            Assert.False((bool)studyEnabled.GetValue(null), "Disabling the suspended controller must revoke candidate art as well as its restored lighting.");
            renderer.Paused = false;
            Tick(); AssertState(initial);
            // A later explicit activation still works; disable is not a permanent lockout.
            Enabled(true); Tick(); AssertActive();
            Restore(); AssertState(initial);
        }
    }
}
