using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Standalone engine-lifetime self-audit; no gameplay/input simulation.
    /// Unity invokes Awake/OnDisable/OnDestroy. Reflection observes fields only.</summary>
    public sealed class GameAuditCameraCleanupBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private int _cases;
        private readonly List<string> _audit = new List<string>();
        private readonly string _runId = Guid.NewGuid().ToString("N");
        private double _started;
        private Fixture _fixture;
        private SpellFxMode _mode; private float _speed, _shake, _flash;
        private Action<int, int, string> _cellHook; private Action<string> _fullHook;
        private EntityMovedVisualHandler _moved; private EntityAttackVisualHandler _attack; private EntityCastVisualHandler _cast;
        private EntityDamageVisualHandler _damage; private EntityDeathVisualHandler _death;
        private void Start()
        {
            _started = Time.realtimeSinceStartupAsDouble;
            _mode = SpellFxSettings.Mode; _speed = SpellFxSettings.AnimationSpeed; _shake = SpellFxSettings.ShakeIntensity; _flash = SpellFxSettings.FlashIntensity;
            _cellHook = ZoneRenderHooks.CellDirtyCallback; _fullHook = ZoneRenderHooks.FullDirtyCallback;
            _moved = EntityVisualHooks.MovedCallback; _attack = EntityVisualHooks.AttackCallback; _cast = EntityVisualHooks.CastCallback;
            _damage = EntityVisualHooks.DamageCallback; _death = EntityVisualHooks.DeathCallback;
            StartCoroutine(RunSafely(AuditLifetimes()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; } catch (Exception e) { failure = e; }
                if (failure != null) { Failures++; Debug.LogException(failure); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            _fixture?.Cleanup(); _fixture = null;
            yield return null; yield return null; // Allow native destruction and its logs to finish.
            var report = new Report { runId = _runId, seconds = Time.realtimeSinceStartupAsDouble - _started, cases = _cases, failures = Failures, audit = _audit.ToArray() };
            string folder = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "GA03a-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[CameraCleanupAudit] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private IEnumerator AuditLifetimes()
        {
            foreach (string cameraState in new[] { "live", "component", "object" })
                foreach (string route in new[] { "cancel", "disable", "destroy" })
                {
                    _fixture = new Fixture(); var f = _fixture; string label = cameraState + "_" + route;
                    Check(label + "_automatic_awake_binding", ReferenceEquals(f.Camera, Read<Camera>(f.Renderer, "_mainCamera")) && f.World.CameraAccent != null);
                    var handle = f.World.Play(f.Sequence()); f.QueueBoth(); f.Follow.Shake(.3f, .8f);
                    Check(label + "_active_and_queued_preconditions", handle.State == WorldFxPlaybackState.Playing && f.World.ActiveSequenceCount > 0 && f.Ascii.ActiveParticleCount > 0
                        && AsciiFxBus.PendingCount > 0 && SpellFxBus.PendingCount > 0 && Read<float>(f.Follow, "_shakeIntensity") > 0);
                    if (cameraState == "component") Destroy(f.Camera); if (cameraState == "object") Destroy(f.CameraObject);
                    if (cameraState != "live") { yield return null; Check(label + "_real_destroyed_wrapper", f.Camera == null && !ReferenceEquals(f.Camera, null)); }
                    if (route == "cancel") f.Renderer.CancelWorldFx();
                    if (route == "disable") { f.Renderer.enabled = true; f.Renderer.enabled = false; }
                    if (route == "destroy") { Destroy(f.Renderer); yield return null; }
                    Check(label + "_lifetime_dispatch_cleared_work", handle.State == WorldFxPlaybackState.Cancelled && f.World.ActiveSequenceCount == 0
                        && f.Ascii.ActiveParticleCount == 0 && AsciiFxBus.PendingCount == 0 && SpellFxBus.PendingCount == 0 && !f.World.HasBlockingFx);
                    if (route == "destroy") Check(label + "_owned_hooks_cleared", f.Renderer == null && ZoneRenderHooks.CellDirtyCallback == null && ZoneRenderHooks.FullDirtyCallback == null);
                    if (cameraState == "live") Check(label + "_live_shake_reset", Read<float>(f.Follow, "_shakeIntensity") == 0 && Read<float>(f.Follow, "_shakeTimeRemaining") == 0);
                    f.Cleanup(); _fixture = null; yield return null; yield return null;
                }
            foreach (bool destroyed in new[] { false, true })
            {
                _fixture = new Fixture(); var f = _fixture;
                if (destroyed) { Destroy(f.Camera); yield return null; }
                var handle = f.World.Play(f.Sequence(2));
                Check("accented_cast_" + destroyed + "_keeps_playing", handle.State == WorldFxPlaybackState.Playing && f.World.ActiveSequenceCount > 0 && f.World.HasBlockingFx);
                if (!destroyed) Check("accented_cast_live_reaches_follow", Read<float>(f.Follow, "_shakeIntensity") > 0 && Read<float>(f.Follow, "_shakeTimeRemaining") > 0);
                f.Renderer.CancelWorldFx(); Check("accented_cast_" + destroyed + "_cancels", handle.State == WorldFxPlaybackState.Cancelled && !f.World.HasBlockingFx);
                f.Cleanup(); _fixture = null; yield return null; yield return null;
            }
        }
        private void Check(string name, bool passed)
        {
            _cases++; _audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "CameraCleanupNativeAudit", payload: new { runId = _runId, name, passed }); } finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Camera cleanup native audit: " + name);
        }
        private static T Read<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private void OnDestroy()
        {
            _fixture?.Cleanup(); _fixture = null;
            SpellFxSettings.Mode = _mode; SpellFxSettings.AnimationSpeed = _speed; SpellFxSettings.ShakeIntensity = _shake; SpellFxSettings.FlashIntensity = _flash;
            ZoneRenderHooks.CellDirtyCallback = _cellHook; ZoneRenderHooks.FullDirtyCallback = _fullHook;
            EntityVisualHooks.MovedCallback = _moved; EntityVisualHooks.AttackCallback = _attack; EntityVisualHooks.CastCallback = _cast;
            EntityVisualHooks.DamageCallback = _damage; EntityVisualHooks.DeathCallback = _death;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
        }
        private sealed class Fixture
        {
            public Camera Camera; public CameraFollow Follow; public GameObject CameraObject;
            public ZoneRenderer Renderer; public WorldFxCoordinator World; public AsciiFxRenderer Ascii; public Zone Zone;
            private readonly List<GameObject> _roots = new List<GameObject>();
            public Fixture()
            {
                CameraObject = new GameObject("Cleanup Camera"); CameraObject.tag = "MainCamera"; Camera = CameraObject.AddComponent<Camera>(); Follow = CameraObject.AddComponent<CameraFollow>(); Follow.enabled = false;
                var grid = new GameObject("Cleanup Grid", typeof(Grid)); _roots.Add(grid);
                var map = new GameObject("Cleanup Map", typeof(Tilemap), typeof(TilemapRenderer)); map.transform.SetParent(grid.transform, false);
                Renderer = map.AddComponent<ZoneRenderer>(); // Real Unity Awake; no reflective lifecycle invocation.
                World = Read<WorldFxCoordinator>(Renderer, "_worldFxCoordinator"); Ascii = Read<AsciiFxRenderer>(Renderer, "_asciiFxRenderer");
                foreach (string field in new[] { "_fineWaterGridTransform", "_sidebarGridTransform", "_hotbarGridTransform", "_popupOverlayGridTransform" })
                { var root = Read<Transform>(Renderer, field); if (root != null) _roots.Add(root.gameObject); }
                Renderer.enabled = false; Camera.enabled = false; // Suppress unrelated frame rendering/layout in this lifetime fixture.
                SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.ShakeIntensity = 1; SpellFxSettings.AnimationSpeed = 1;
                Zone = new Zone("NativeCameraCleanup"); foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; } Renderer.SetZone(Zone);
            }
            public SpellFxSequence Sequence(float intensity = 1) => new SpellFxSequence("MissingArt", Zone, null, new Point(4, 4), new[] { new Point(5, 4), new Point(6, 4) }, new[] { new Point(6, 4) }, intensity: intensity);
            public void QueueBoth() { AsciiFxBus.EmitBurst(Zone, 4, 4, AsciiFxTheme.Fire, true, .2f); SpellFxBus.Emit(Sequence()); }
            public void Cleanup()
            {
                // Cleanup after observations, including failed runs; never replace the tested callback earlier.
                if (World != null) { World.CameraAccent = null; World.Dispose(); }
                foreach (var root in _roots) if (root != null) Destroy(root); _roots.Clear();
                if (CameraObject != null) Destroy(CameraObject);
            }
        }
        [Serializable] private sealed class Report { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
