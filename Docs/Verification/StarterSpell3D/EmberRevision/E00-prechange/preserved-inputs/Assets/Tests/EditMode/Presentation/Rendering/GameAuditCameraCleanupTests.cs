using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public abstract class CameraCleanupFixture
    {
        protected ZoneRenderer Renderer; protected WorldFxCoordinator World; protected AsciiFxRenderer Ascii;
        protected Zone Zone; protected Camera Camera; protected CameraFollow Follow; protected GameObject CameraObject, GridObject;
        private readonly List<GameObject> _ownedRoots = new List<GameObject>();
        private Camera[] _previousMain; private SpellFxMode _mode; private float _shake, _speed, _flash;
        private Action<int, int, string> _cellHook; private Action<string> _fullHook;
        private EntityMovedVisualHandler _moved; private EntityAttackVisualHandler _attack; private EntityCastVisualHandler _cast;
        private EntityDamageVisualHandler _damage; private EntityDeathVisualHandler _death;
        [SetUp] public void SetupCameraCleanup()
        {
            _mode = SpellFxSettings.Mode; _shake = SpellFxSettings.ShakeIntensity; _speed = SpellFxSettings.AnimationSpeed; _flash = SpellFxSettings.FlashIntensity;
            _cellHook = ZoneRenderHooks.CellDirtyCallback; _fullHook = ZoneRenderHooks.FullDirtyCallback;
            _moved = EntityVisualHooks.MovedCallback; _attack = EntityVisualHooks.AttackCallback; _cast = EntityVisualHooks.CastCallback;
            _damage = EntityVisualHooks.DamageCallback; _death = EntityVisualHooks.DeathCallback;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            _previousMain = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.CompareTag("MainCamera")).ToArray();
            foreach (var camera in _previousMain) camera.tag = "Untagged";
            CameraObject = new GameObject("A31 Camera"); CameraObject.tag = "MainCamera"; Camera = CameraObject.AddComponent<Camera>(); Follow = CameraObject.AddComponent<CameraFollow>();
            GridObject = new GameObject("A31 Grid", typeof(Grid)); _ownedRoots.Add(GridObject);
            var map = new GameObject("A31 Tilemap", typeof(Tilemap), typeof(TilemapRenderer)); map.transform.SetParent(GridObject.transform, false);
            Renderer = map.AddComponent<ZoneRenderer>();
            if (Read<WorldFxCoordinator>(Renderer, "_worldFxCoordinator") == null) Lifecycle("Awake");
            foreach (string field in new[] { "_fineWaterGridTransform", "_sidebarGridTransform", "_hotbarGridTransform", "_popupOverlayGridTransform" })
            { var root = Read<Transform>(Renderer, field); if (root != null) _ownedRoots.Add(root.gameObject); }
            Assert.AreSame(Camera, Read<Camera>(Renderer, "_mainCamera"), "Exercise the actual Awake-cached camera and bound delegate.");
            World = Read<WorldFxCoordinator>(Renderer, "_worldFxCoordinator"); Ascii = Read<AsciiFxRenderer>(Renderer, "_asciiFxRenderer");
            SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.ShakeIntensity = 1; SpellFxSettings.AnimationSpeed = 1;
            Zone = new Zone("CameraCleanup"); foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; } Renderer.SetZone(Zone);
        }
        [TearDown] public void CleanupCameraFixture()
        {
            // RED must not leak the intentionally broken delegate into later test teardown.
            if (World != null) { World.CameraAccent = null; World.Dispose(); }
            foreach (var root in _ownedRoots) if (root != null) Object.DestroyImmediate(root); _ownedRoots.Clear();
            if (CameraObject != null) Object.DestroyImmediate(CameraObject);
            foreach (var camera in _previousMain ?? Array.Empty<Camera>()) if (camera != null) camera.tag = "MainCamera";
            ZoneRenderHooks.CellDirtyCallback = _cellHook; ZoneRenderHooks.FullDirtyCallback = _fullHook;
            EntityVisualHooks.MovedCallback = _moved; EntityVisualHooks.AttackCallback = _attack; EntityVisualHooks.CastCallback = _cast;
            EntityVisualHooks.DamageCallback = _damage; EntityVisualHooks.DeathCallback = _death;
            SpellFxSettings.Mode = _mode; SpellFxSettings.ShakeIntensity = _shake; SpellFxSettings.AnimationSpeed = _speed; SpellFxSettings.FlashIntensity = _flash;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
        }
        protected static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        protected static void Write(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        protected void Lifecycle(string name) => typeof(ZoneRenderer).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Renderer, null);
        protected SpellFxSequence Sequence(float intensity = 1) => new SpellFxSequence("MissingArt", Zone, null, new Point(4, 4), new[] { new Point(5, 4), new Point(6, 4) }, new[] { new Point(6, 4) }, intensity: intensity);
        protected void QueueBoth()
        { AsciiFxBus.EmitBurst(Zone, 4, 4, AsciiFxTheme.Fire, true, .2f); SpellFxBus.Emit(Sequence()); Assert.Greater(AsciiFxBus.PendingCount, 0); Assert.Greater(SpellFxBus.PendingCount, 0); }
        protected WorldFxPlayback SeedWork()
        { var handle = World.Play(Sequence()); Assert.AreEqual(WorldFxPlaybackState.Playing, handle.State); Assert.Greater(World.ActiveSequenceCount, 0); Assert.Greater(Ascii.ActiveParticleCount, 0); QueueBoth(); Follow.Shake(.3f, .8f); Assert.Greater(Read<float>(Follow, "_shakeIntensity"), 0); return handle; }
        protected void DestroyCamera(string state)
        {
            if (state == "component") Object.DestroyImmediate(Camera);
            else if (state == "object") Object.DestroyImmediate(CameraObject);
            if (state != "live") { Assert.IsTrue(Camera == null); Assert.IsFalse(ReferenceEquals(Camera, null)); }
        }
        protected void Cleared(WorldFxPlayback handle = null)
        {
            if (handle != null) Assert.AreEqual(WorldFxPlaybackState.Cancelled, handle.State);
            Assert.AreEqual(0, World.ActiveSequenceCount); Assert.AreEqual(0, AsciiFxBus.PendingCount); Assert.AreEqual(0, SpellFxBus.PendingCount);
            Assert.AreEqual(0, Ascii.ActiveParticleCount); Assert.AreEqual(0, Ascii.ActiveBurstCount); Assert.IsFalse(World.HasBlockingFx); LogAssert.NoUnexpectedReceived();
        }
        protected void ShakeCleared()
        { Assert.AreEqual(0, Read<float>(Follow, "_shakeIntensity")); Assert.AreEqual(0, Read<float>(Follow, "_shakeTimeRemaining")); }
    }
    public class GameAuditCameraCleanupTests : CameraCleanupFixture
    {
        // EditMode invokes lifecycle bodies directly; the native bench verifies Unity dispatch.
        [TestCase("live", "cancel")] [TestCase("component", "cancel")] [TestCase("object", "cancel")]
        [TestCase("live", "OnDisable")] [TestCase("component", "OnDisable")] [TestCase("object", "OnDisable")]
        [TestCase("live", "OnDestroy")] [TestCase("component", "OnDestroy")] [TestCase("object", "OnDestroy")]
        public void ActualBoundCameraDelegateCannotInterruptCleanup(string cameraState, string route)
        {
            var handle = SeedWork(); DestroyCamera(cameraState);
            Assert.DoesNotThrow(() => { if (route == "cancel") Renderer.CancelWorldFx(); else Lifecycle(route); });
            Cleared(handle); if (cameraState == "live") ShakeCleared();
        }
    }
}
