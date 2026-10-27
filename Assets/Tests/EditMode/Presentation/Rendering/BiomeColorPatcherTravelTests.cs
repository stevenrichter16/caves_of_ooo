using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class BiomeColorPatcherTravelTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Color InitialFilter = new Color(.62f, .73f, .81f, 1f);
        private const float InitialContrast = -17f, InitialSaturation = 23f, InitialVignette = .27f;

        private static void Call(BiomeColorPatcher patcher, string name)
        {
            var method = typeof(BiomeColorPatcher).GetMethod(name, Private);
            Assert.NotNull(method, name);
            try { method.Invoke(patcher, null); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        private static void Set(BiomeColorPatcher patcher, string name, object value)
        {
            var field = typeof(BiomeColorPatcher).GetField(name, Private);
            Assert.NotNull(field, name);
            field.SetValue(patcher, value);
        }

        private static void NoBorrowedRenderer()
        {
            Assert.IsNull(Object.FindAnyObjectByType<ZoneRenderer>(),
                "This isolated fixture must not discover or mutate another scene's renderer.");
        }

        private static Zone Attach(OverworldZoneManager manager, string id, BiomeType biome)
        {
            var zone = new Zone(id);
            var (x, y, _) = WorldMap.FromZoneID(id);
            manager.WorldMap.Tiles[x, y] = biome;
            manager.SetActiveZone(zone);
            Assert.AreSame(manager, WorldLocationContext.For(zone), "Use a real attached graph.");
            return zone;
        }

        private static bool Matches(VolumeProfile profile, BiomePalette palette)
        {
            Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var grading));
            Assert.IsTrue(profile.TryGet<Vignette>(out var vignette));
            return Mathf.Abs(grading.contrast.value - palette.Contrast) < .001f
                && Mathf.Abs(grading.saturation.value - palette.Saturation) < .001f
                && Vector4.Distance(grading.colorFilter.value, palette.ColorFilter) < .001f
                && Mathf.Abs(vignette.intensity.value - palette.VignetteIntensity) < .001f;
        }

        private static void FinishFade(Fixture fixture, BiomeType biome)
        {
            // EditMode has no guaranteed advancing deltaTime. Preserve the detected target
            // and exercise the real writer at the end of its fade, not a fake Update loop.
            Set(fixture.Patcher, "_fadeProgress", .999999f);
            Call(fixture.Patcher, "Update");
            Assert.IsTrue(Matches(fixture.Volume.profile, BiomePalette.GetForBiome(biome)),
                "The rendered instance must reach the current biome's complete palette.");
        }

        private static void AssertInitial(VolumeProfile profile)
        {
            Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var grading));
            Assert.IsTrue(profile.TryGet<Vignette>(out var vignette));
            Assert.AreEqual(InitialContrast, grading.contrast.value);
            Assert.AreEqual(InitialSaturation, grading.saturation.value);
            Assert.AreEqual(InitialFilter, grading.colorFilter.value);
            Assert.AreEqual(InitialVignette, vignette.intensity.value);
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject VolumeRoot, PatcherRoot;
            public readonly Volume Volume;
            public readonly VolumeProfile Shared;
            public readonly BiomeColorPatcher Patcher;
            private readonly bool _ownsPatcherRoot;
            private GameObject _rendererRoot;
            private readonly Action<int, int, string> _oldCell = ZoneRenderHooks.CellDirtyCallback;
            private readonly Action<string> _oldFull = ZoneRenderHooks.FullDirtyCallback;
            private readonly FieldInfo _bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _oldBound;
            private readonly Zone _previousZone;
            private readonly Action<int, int> _oldChanged;
            private readonly Action _oldSight;

            public Fixture(bool instantiateBeforeBinding = false, GameObject patcherRoot = null)
            {
                Assert.NotNull(_bound);
                _oldBound = _bound.GetValue(null);
                if (_oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out _previousZone))
                {
                    _oldChanged = _previousZone.TileState.OnCellChanged;
                    _oldSight = _previousZone.TileState.OnSightChanged;
                }
                VolumeRoot = new GameObject("Owned biome volume " + Guid.NewGuid().ToString("N"));
                Volume = VolumeRoot.AddComponent<Volume>();
                Shared = ScriptableObject.CreateInstance<VolumeProfile>();
                var grading = Shared.Add<ColorAdjustments>(true);
                grading.colorFilter.value = InitialFilter;
                grading.contrast.value = InitialContrast;
                grading.saturation.value = InitialSaturation;
                Shared.Add<Vignette>(true).intensity.value = InitialVignette;
                Volume.sharedProfile = Shared;
                if (instantiateBeforeBinding) { var instance = Volume.profile; }
                _ownsPatcherRoot = patcherRoot == null;
                PatcherRoot = patcherRoot ?? new GameObject("Owned biome patcher");
                if (_ownsPatcherRoot) PatcherRoot.SetActive(false);
                Patcher = PatcherRoot.AddComponent<BiomeColorPatcher>();
                Patcher.GlobalVolumeName = VolumeRoot.name;
                Call(Patcher, "FindVolume");
                Assert.IsTrue(Patcher.TestOnly_HasColorAdjustments, "Real volume must be bound.");
            }

            public ZoneRenderer CreateRenderer()
            {
                NoBorrowedRenderer();
                _rendererRoot = new GameObject("Owned biome travel renderer");
                // EditMode does not invoke Awake; only real SetZone and discovery are needed.
                var renderer = _rendererRoot.AddComponent<ZoneRenderer>();
                renderer.enabled = false;
                renderer.FlatPixelModeEnabled = false;
                Assert.AreSame(renderer, Object.FindAnyObjectByType<ZoneRenderer>());
                return renderer;
            }

            public void Dispose()
            {
                if (_rendererRoot != null) Object.DestroyImmediate(_rendererRoot);
                if (_ownsPatcherRoot && PatcherRoot != null) Object.DestroyImmediate(PatcherRoot);
                else if (Patcher != null) Object.DestroyImmediate(Patcher);
                var instance = Volume != null && Volume.HasInstantiatedProfile() ? Volume.profile : null;
                if (VolumeRoot != null) Object.DestroyImmediate(VolumeRoot);
                DestroyProfile(instance);
                DestroyProfile(Shared);
                ZoneRenderHooks.CellDirtyCallback = _oldCell;
                ZoneRenderHooks.FullDirtyCallback = _oldFull;
                _bound.SetValue(null, _oldBound);
                if (_previousZone != null)
                {
                    _previousZone.TileState.OnCellChanged = _oldChanged;
                    _previousZone.TileState.OnSightChanged = _oldSight;
                }
            }
        }

        private static void DestroyProfile(VolumeProfile profile)
        {
            if (profile == null) return;
            foreach (var component in profile.components) if (component != null) Object.DestroyImmediate(component);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void SeededZoneStillDiscoversShownRendererAndTracksLaterLegacyTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer();
                var manager = OverworldZoneManager.CreateDetached(null, 65);
                var boot = Attach(manager, "Overworld.11.10.0", BiomeType.Spread);
                var shown = Attach(manager, "Overworld.4.12.0", BiomeType.Overwrit);
                f.Patcher.ZoneManager = manager;
                f.Patcher.SetCurrentZone(boot);
                renderer.SetZone(shown);
                Call(f.Patcher, "Update");
                Assert.AreSame(shown, f.Patcher.CurrentZone,
                    "A seeded boot zone must not prevent discovery of the actual renderer.");
                Assert.AreEqual(BiomeType.Overwrit, f.Patcher.TestOnly_ActiveBiome);
                FinishFade(f, BiomeType.Overwrit);

                var next = Attach(manager, "Overworld.0.8.0", BiomeType.Grovelands);
                renderer.SetZone(next); // Ordinary travel does not call SetCurrentZone on the patcher.
                Call(f.Patcher, "Update");
                Assert.AreSame(next, f.Patcher.CurrentZone);
                Assert.AreEqual(BiomeType.Grovelands, f.Patcher.TestOnly_ActiveBiome);
                FinishFade(f, BiomeType.Grovelands);
                Assert.IsFalse(renderer.FlatPixelModeEnabled, "Legacy mode must remain reversible.");
                AssertInitial(f.Shared);
            }
        }

        [Test]
        public void ActualContinueAdoptsReplacementManagerEvenWhenZoneIdIsUnchanged()
        {
            using (var game = new HotbarSaveFixture(withInput: false))
            using (var f = new Fixture(patcherRoot: game.Root))
            {
                var boot = HotbarSaveFixture.MakeState(0, 0);
                boot.ZoneManager.WorldMap.Tiles[10, 10] = BiomeType.Spread;
                game.BindOld(boot);
                game.Renderer.SetZone(boot.ZoneManager.ActiveZone);
                f.Patcher.ZoneManager = boot.ZoneManager;
                f.Patcher.SetCurrentZone(boot.ZoneManager.ActiveZone);
                Set(f.Patcher, "_zoneRendererRef", game.Renderer);

                var loaded = HotbarSaveFixture.MakeState(0, 0);
                loaded.ZoneManager.WorldMap.Tiles[10, 10] = BiomeType.Overwrit;
                Assert.AreNotSame(boot.ZoneManager, loaded.ZoneManager);
                Assert.AreEqual(boot.ZoneManager.ActiveZone.ZoneID, loaded.ZoneManager.ActiveZone.ZoneID);
                game.Bootstrap.ApplyLoadedGame(loaded);
                Assert.AreSame(loaded.ZoneManager.ActiveZone, game.Renderer.CurrentZone);
                Assert.AreSame(loaded.ZoneManager, WorldLocationContext.For(game.Renderer.CurrentZone));
                Call(f.Patcher, "Update");
                Assert.AreSame(loaded.ZoneManager, f.Patcher.ZoneManager,
                    "Continue replaces the boot manager; matching zone text does not preserve its authority.");
                Assert.AreSame(game.Renderer.CurrentZone, f.Patcher.CurrentZone);
                Assert.AreEqual(BiomeType.Overwrit, f.Patcher.TestOnly_ActiveBiome);
                FinishFade(f, BiomeType.Overwrit);
                AssertInitial(f.Shared);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GradingWritesTheRenderedProfileWithoutMutatingTheSharedSource(bool instanceExistedAtBinding)
        {
            NoBorrowedRenderer();
            using (var f = new Fixture(instanceExistedAtBinding))
            {
                var live = f.Volume.profile; // The same getter used by GameBootstrap's vignette pass.
                Assert.AreNotSame(f.Shared, live);
                var otherRoot = new GameObject("Owned second volume sharing source");
                VolumeProfile otherProfile = null;
                try
                {
                    var other = otherRoot.AddComponent<Volume>();
                    other.sharedProfile = f.Shared;
                    otherProfile = other.profile;
                    var manager = OverworldZoneManager.CreateDetached(null, 65);
                    f.Patcher.ZoneManager = manager;
                    f.Patcher.SetCurrentZone(Attach(manager, "Overworld.4.12.0", BiomeType.Overwrit));
                    Call(f.Patcher, "Update");
                    FinishFade(f, BiomeType.Overwrit);
                    Assert.AreSame(live, f.Volume.profile);
                    AssertInitial(f.Shared);
                    AssertInitial(otherProfile);
                }
                finally { Object.DestroyImmediate(otherRoot); DestroyProfile(otherProfile); }
            }
        }

        [TestCase("Overworld.4.12.1", BiomeType.Spread)]
        [TestCase("Overworld.4.12.0", BiomeType.Cave)]
        public void FirstCaveZoneAppliesItsPaletteDespiteMatchingTheDefaultEnum(string zoneId, BiomeType surface)
        {
            NoBorrowedRenderer();
            using (var f = new Fixture())
            {
                var manager = OverworldZoneManager.CreateDetached(null, 65);
                f.Patcher.ZoneManager = manager;
                f.Patcher.SetCurrentZone(Attach(manager, zoneId, surface));
                Assert.IsFalse(Matches(f.Volume.profile, BiomePalette.Cave));
                Call(f.Patcher, "Update");
                Assert.IsTrue(f.Patcher.TestOnly_FadeProgress < 1f || Matches(f.Volume.profile, BiomePalette.Cave),
                    "The first Cave target must start a fade or already be applied; enum equality is insufficient.");
                Assert.AreEqual(BiomeType.Cave, f.Patcher.TestOnly_ActiveBiome);
                FinishFade(f, BiomeType.Cave);
                AssertInitial(f.Shared);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitZoneRemainsUsableWithoutAShownRendererZone(bool emptyRendererExists)
        {
            NoBorrowedRenderer();
            using (var f = new Fixture())
            {
                if (emptyRendererExists) Assert.IsNull(f.CreateRenderer().CurrentZone);
                var manager = OverworldZoneManager.CreateDetached(null, 65);
                manager.WorldMap.Tiles[4, 12] = BiomeType.Overwrit;
                var explicitZone = new Zone("Overworld.4.12.0");
                Assert.IsNull(WorldLocationContext.For(explicitZone), "Exercise the explicit reference fallback.");
                f.Patcher.ZoneManager = manager;
                f.Patcher.SetCurrentZone(explicitZone);
                Call(f.Patcher, "Update");
                Assert.AreSame(explicitZone, f.Patcher.CurrentZone);
                Assert.AreSame(manager, f.Patcher.ZoneManager);
                Assert.AreEqual(BiomeType.Overwrit, f.Patcher.TestOnly_ActiveBiome);
                FinishFade(f, BiomeType.Overwrit);
                AssertInitial(f.Shared);
            }
        }

        [Test]
        public void MissingVolumeRemainsANoOpWithExplicitGameplayReferences()
        {
            var root = new GameObject("Owned missing-volume patcher");
            root.SetActive(false);
            try
            {
                var patcher = root.AddComponent<BiomeColorPatcher>();
                patcher.GlobalVolumeName = "Absent volume " + Guid.NewGuid().ToString("N");
                var manager = OverworldZoneManager.CreateDetached(null, 65);
                var zone = Attach(manager, "Overworld.4.12.0", BiomeType.Overwrit);
                patcher.ZoneManager = manager;
                patcher.SetCurrentZone(zone);
                Call(patcher, "FindVolume");
                Assert.IsFalse(patcher.TestOnly_HasVolume);
                Assert.IsFalse(patcher.TestOnly_HasColorAdjustments);
                Assert.DoesNotThrow(() => Call(patcher, "Update"));
                Assert.AreSame(zone, patcher.CurrentZone);
                Assert.AreSame(manager, patcher.ZoneManager);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
