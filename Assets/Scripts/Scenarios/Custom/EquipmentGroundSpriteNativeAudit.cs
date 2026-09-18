using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Isolated synthetic ground-item benchmark. Real factory items,
    /// ZoneRenderer/LightMap/environment overlays and native movement/inventory
    /// APIs; deliberately not a claim about ordinary keyboard gameplay.</summary>
    public sealed partial class EquipmentGroundSpriteNativeAudit : MonoBehaviour
    {
        public const string WorkloadVersion = "ground-equipment-75s-v1";
        public const int OperationsPerPhase = 100;
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string[] Blueprints = { "Dagger", "ShortSword", "LongSword", "Spear",
            "LeatherBoots", "IronshodBoots", "LeatherGloves", "LeatherCap", "IronHelmet", "Mace",
            "FireTonic", "LeatherArmor", "Chest", "WatchLantern" };
        private static readonly string[] NewTiles = { "item_dagger", "item_sword", "item_sword", "item_spear",
            "item_boots", "item_boots", "item_gloves", "item_helmet", "item_helmet", "item_mace",
            "item_vial", "item_armor", "Chest", "Lantern" };
        private static readonly string[] OldTiles = { "WeaponGround", "WeaponGround", "WeaponGround", "WeaponGround",
            "item_armor", "item_armor", "item_armor", "item_armor", "item_armor", "item_vial",
            "item_vial", "item_armor", "Chest", "Lantern" };
        private readonly List<Check> checks = new List<Check>();
        private readonly List<string> screenshots = new List<string>();
        private readonly List<Claim> claims = new List<Claim>();
        private readonly Entity[] displayed = new Entity[14];
        private readonly Color[] bright = new Color[14];
        private Zone zone;
        private Entity actor, workItem;
        private ZoneRenderer renderer;
        private EnvironmentSpriteRenderer environment;
        private Tilemap foreground, overlay;
        private Camera cameraView;
        private GameObject fixtureRoot;
        private string runId, mode, output, stem, saveRoot, fatal;
        private bool initialized, cleaned, capturing, oldBackground, oldVoxel, oldLowDetail;
        private int oldFrameRate, oldVsync;
        private string captureLabel;
        private double started;
        private Action<int, int, string> oldCellHook;
        private Action<string> oldFullHook;
        private readonly long[] originalPerf = new long[8];
        private Stack<IEnumerator> steps;
        private UnityEngine.InputSystem.InputSettings originalInputSettings;
        private int[] originalDevices;
        private bool settingsRestored, inputUntouched;
        private readonly HashSet<int> priorTileIds = new HashSet<int>();
        private readonly HashSet<int> priorAssetIds = new HashSet<int>();
        private readonly List<UnityEngine.Object> ownedAssets = new List<UnityEngine.Object>();
        private Report report;

        public void Initialize(string id, string logicalMode, string directory)
        {
            if (!Guid.TryParseExact(id, "N", out var guid) || guid == Guid.Empty)
                throw new ArgumentException("Audit requires a fresh GUID.");
            if (logicalMode != "before" && logicalMode != "after")
                throw new ArgumentException("Mode must be before or after.");
            if (string.IsNullOrEmpty(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Editor must establish private save isolation first.");
            runId = id; mode = logicalMode; output = Path.GetFullPath(directory);
            stem = "EGSN-" + mode + "-" + runId; saveRoot = SaveGameService.SaveRootOverride;
            Directory.CreateDirectory(output); started = Time.realtimeSinceStartupAsDouble;
            oldBackground = Application.runInBackground; oldFrameRate = Application.targetFrameRate;
            oldVsync = QualitySettings.vSyncCount; oldVoxel = Village3DSettings.Enabled; oldLowDetail = Village3DSettings.LowDetail;
            oldCellHook = ZoneRenderHooks.CellDirtyCallback; oldFullHook = ZoneRenderHooks.FullDirtyCallback;
            ReadPerf(originalPerf);
            originalInputSettings = UnityEngine.InputSystem.InputSystem.settings;
            originalDevices = new int[UnityEngine.InputSystem.InputSystem.devices.Count];
            for (int i = 0; i < originalDevices.Length; i++) originalDevices[i] = UnityEngine.InputSystem.InputSystem.devices[i].deviceId;
            foreach (var tile in Resources.FindObjectsOfTypeAll<Tile>()) priorTileIds.Add(tile.GetInstanceID());
            foreach (var asset in Resources.FindObjectsOfTypeAll<Sprite>()) priorAssetIds.Add(asset.GetInstanceID());
            foreach (var asset in Resources.FindObjectsOfTypeAll<Texture2D>()) priorAssetIds.Add(asset.GetInstanceID());
            initialized = true;
            Application.runInBackground = true; Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            // Runtime-only setter: never persist either display preference.
            Village3DSettings.Enabled = false;
            StartCoroutine(RunSafely(Audit()));
        }

        private IEnumerator RunSafely(IEnumerator routine)
        {
            steps = new Stack<IEnumerator>(); steps.Push(routine);
            while (steps.Count > 0)
            {
                object value = null; bool moved = false; Exception error = null;
                try { moved = steps.Peek().MoveNext(); if (moved) value = steps.Peek().Current; }
                catch (Exception ex) { error = ex; }
                if (error != null) { fatal = error.ToString(); Add("fatal", false, fatal); break; }
                if (!moved) { (steps.Pop() as IDisposable)?.Dispose(); continue; }
                if (value is IEnumerator nested) { steps.Push(nested); continue; }
                yield return value;
            }
            while (steps.Count > 0) (steps.Pop() as IDisposable)?.Dispose();
            StopRecorders(); Finished = true; WriteReport();
        }

        private IEnumerator Audit()
        {
            CreateFixture();
            yield return Settle(); CollectOwnedTileResources();
            Require(Screen.width == 1920 && Screen.height == 1080, "Native GameView must be 1920x1080.");
            Add("actual_14_blueprints_four_copies", zone.GetReadOnlyEntities().Count == Zone.Width * Zone.Height + 58,
                "2000 floor owners + 56 factory display items + synthetic carrier + native work dagger; equipped blocker is inventory-owned.");
            ValidateClaims("normal", true);
            yield return Capture("normal");
            zone.AmbientLevel = .28f; renderer.MarkDirty("GroundAudit.Dim");
            yield return Settle(); ValidateClaims("dim", false);
            Add("dim_uses_native_lightmap", overlay.GetColor(Position(0)).maxColorComponent < bright[0].maxColorComponent * .7f,
                "The first dagger is outside the watch lantern's native light radius.");
            yield return Capture("dim");
            zone.AmbientLevel = 1f; renderer.MarkDirty("GroundAudit.Normal"); yield return Settle();

            // A real release/return cycle, outside the timed window.
            environment.ReleaseAllClaims();
            Add("release_restores_item_glyph", overlay.GetTile(Position(0)) == null && foreground.GetTile(Position(0)) != null);
            renderer.MarkDirty("GroundAudit.ReleaseRestore"); yield return Settle();
            Add("release_reclaims_item", TileName(Position(0)) == ExpectedTile(0));

            // Warm native method markers, then allocate bounded buffers before sampling.
            renderer.MarkCellDirty(4, 4, "GroundAudit.WarmIncremental"); yield return Settle();
            StartRecorders(); yield return null; yield return null;
            yield return MeasurePhase(0, "idle");
            yield return MeasurePhase(1, "walk_full_redraw");
            // Native walking makes a reversible 10-cell shuttle and ends at30,21.
            Require(zone.GetEntityPosition(actor) == (30, 21), "Walking returned carrier to original cell.");
            yield return MeasurePhase(2, "pickup_drop_incremental");
            StopRecorders();
            Add("workload_complete", phases.Count == 3 && phases[0].operations == 0
                && phases[1].operations == OperationsPerPhase && phases[2].operations == OperationsPerPhase);
            Add("native_inventory_roundtrip", zone.GetEntityCell(workItem) != null
                && workItem.GetPart<PhysicsPart>().InInventory == null
                && !actor.GetPart<InventoryPart>().Contains(workItem));
            Add("owned_save_only", Directory.GetDirectories(saveRoot).Length == 1
                && Directory.GetFiles(saveRoot, "*.sav.gz", SearchOption.AllDirectories).Length == 1,
                "Only NativeSaveIsolation's original disposable boot marker exists; this fixture has no game save runtime.");
            WriteRawFrames();
        }

        private void CreateFixture()
        {
            Require(UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() == null, "Owned benchmark scene must not contain a gameplay bootstrap.");
            Require(UnityEngine.Object.FindFirstObjectByType<ZoneRenderer>() == null, "Owned benchmark scene must not contain an existing renderer.");
            fixtureRoot = new GameObject("Equipment ground native fixture");
            var cameraObject = new GameObject("Main Camera"); cameraObject.transform.SetParent(fixtureRoot.transform, false);
            cameraObject.tag = "MainCamera"; cameraView = cameraObject.AddComponent<Camera>();
            cameraView.orthographic = true; cameraView.orthographicSize = 33.75f;
            cameraView.transform.position = new Vector3(40f, 12.5f, -10f);
            cameraView.clearFlags = CameraClearFlags.SolidColor; cameraView.backgroundColor = new Color(.025f, .03f, .025f);
            cameraView.cullingMask = GameplayRenderLayers.GameplayCameraMask;
            var lightObject = new GameObject("Native fixture global light"); lightObject.transform.SetParent(fixtureRoot.transform, false);
            var light = lightObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global; light.intensity = 1f;
            var grid = new GameObject("Ground audit grid"); grid.transform.SetParent(fixtureRoot.transform, false); grid.AddComponent<Grid>();
            var map = new GameObject("Ground audit foreground"); map.transform.SetParent(grid.transform, false);
            foreground = map.AddComponent<Tilemap>(); map.AddComponent<TilemapRenderer>();
            renderer = map.AddComponent<ZoneRenderer>();
            // Avoid global hotkeys in the synthetic scene; no keyboard/device/settings override.
            var controls = map.GetComponent<Village3DControls>(); if (controls != null) controls.enabled = false;
            environment = (EnvironmentSpriteRenderer)typeof(ZoneRenderer).GetField("_envSpriteRenderer", Hidden).GetValue(renderer);
            Require(environment != null && environment.IsInitialized, "Production ZoneRenderer must initialize its environment overlay.");
            overlay = grid.transform.Find("EnvironmentSpriteTilemap").GetComponent<Tilemap>();
            var inputObject = new GameObject("Native input idle guard"); inputObject.transform.SetParent(fixtureRoot.transform, false);
            inputObject.AddComponent<InputHandler>(); // null context deliberately measures only the real Update idle guard.
            var data = Resources.Load<TextAsset>("Content/Blueprints/Objects"); Require(data != null, "Shipped blueprint resource exists.");
            var factory = new EntityFactory(); factory.LoadBlueprints(data.text);
            zone = new Zone("EquipmentGroundSpriteNativeAudit") { AmbientLevel = 1f, AmbientTint = Color.white };
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                Require(zone.AddEntity(factory.CreateEntity("Floor"), x, y), "Factory floor placement.");
                zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true;
            }
            for (int row = 0; row < 4; row++) for (int i = 0; i < Blueprints.Length; i++)
            {
                var owner = factory.CreateEntity(Blueprints[i]); Require(owner != null && owner.GetPart<RenderPart>() != null, "Real " + Blueprints[i]);
                Require(zone.AddEntity(owner, 5 + 5 * i, 5 + 4 * row), "Display placement.");
                if (row == 0) displayed[i] = owner;
            }
            actor = new Entity { ID = "ground-audit-carrier", BlueprintName = "GroundAuditCarrier" };
            actor.SetTag("Player"); actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart());
            // Invisible carrier keeps the item at its feet inspectable; this is an explicit bench fixture, not authored Player appearance.
            actor.AddPart(new RenderPart { RenderString = "@", DisplayName = "audit carrier", RenderLayer = 20, Visible = false });
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 100, Max = 100, Owner = actor };
            Require(zone.AddEntity(actor, 30, 21), "Carrier placement.");
            var blocker = factory.CreateEntity("LongSword");
            Require(actor.GetPart<InventoryPart>().AddObject(blocker) && InventorySystem.Equip(actor, blocker), "Occupy the native legacy hand slot once before profiling.");
            workItem = factory.CreateEntity("Dagger"); Require(zone.AddEntity(workItem, 30, 21), "Native work item placement.");
            Require(blocker.GetPart<EquippablePart>().Slot == workItem.GetPart<EquippablePart>().Slot,
                "Occupied slot prevents pickup auto-equip/full equipment refresh during incremental phase.");
            renderer.PlayerEntity = actor; renderer.FovRadius = 999; renderer.SetZone(zone);
            Add("real_production_renderer", environment.IsInitialized && renderer.CurrentZone == zone);
            Add("expected_logical_mode", mode == "after"
                ? EnvironmentSpriteRenderer.ResolveItemBody("Dagger") == "item_dagger"
                : EnvironmentSpriteRenderer.ResolveItemBody("Dagger") == null,
                "Before runs restored original renderer; mode never rewrites mappings or hides candidate resources.");
        }

        private Vector3Int Position(int item) => new Vector3Int(5 + 5 * item, Zone.Height - 1 - 5, 0);
        private string ExpectedTile(int item) => mode == "after" ? NewTiles[item] : OldTiles[item];
        private string TileName(Vector3Int p) => overlay.GetTile(p)?.name;
        private void ValidateClaims(string label, bool rememberBright)
        {
            for (int i = 0; i < displayed.Length; i++)
            {
                var p = Position(i); var tile = overlay.GetTile(p) as Tile; Color tint = overlay.GetColor(p);
                claims.Add(new Claim { light = label, blueprint = Blueprints[i], expected = ExpectedTile(i),
                    tile = tile?.name, sprite = tile?.sprite?.name, tint = tint, x = p.x, zoneY = 5 });
                Add(label + "_claim_" + Blueprints[i], tile != null && tile.sprite != null
                    && tile.name == ExpectedTile(i) && foreground.GetTile(p) == null, tile?.name ?? "no overlay");
                if (rememberBright) bright[i] = tint;
            }
        }

        private IEnumerator Settle() { yield return null; yield return null; yield return new WaitForEndOfFrame(); }
        private IEnumerator Capture(string label)
        {
            capturing = true; captureLabel = label; yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Require(frame.width == 1920 && frame.height == 1080, "Actual native screenshot dimensions.");
                // Match the gameplay camera's default + world layers: both
                // overlay tilemaps and the global2D light use the default layer.
                // Logical tile claims alone cannot detect a black capture.
                Vector3 cellCorner = cameraView.WorldToScreenPoint(new Vector3(5f, Zone.Height - 1 - 5, 0));
                var cellPixels = frame.GetPixels(Mathf.RoundToInt(cellCorner.x), Mathf.RoundToInt(cellCorner.y), 16, 16);
                int litPixels = 0;
                foreach (var pixel in cellPixels) if (Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > (label == "normal" ? .15f : .025f)) litPixels++;
                Require(litPixels >= 10, "Native item screenshot must contain visible lit art; logical claims are insufficient.");
                string file = Path.Combine(output, stem + "-" + label + ".png"); File.WriteAllBytes(file, frame.EncodeToPNG()); screenshots.Add(file);
                // Nearest-neighbour 2x enlargement of the actual world pixels, not a synthetic asset montage.
                Vector3 low = cameraView.WorldToScreenPoint(Vector3.zero), high = cameraView.WorldToScreenPoint(new Vector3(80, 25));
                int x = Mathf.RoundToInt(low.x), y = Mathf.RoundToInt(low.y), w = Mathf.RoundToInt(high.x - low.x), h = Mathf.RoundToInt(high.y - low.y);
                Require(x >= 0 && y >= 0 && x + w <= frame.width && y + h <= frame.height, "World bounds fit screenshot.");
                var pixels = frame.GetPixels(x, y, w, h); var enlarged = new Texture2D(w * 2, h * 2, TextureFormat.RGBA32, false);
                try
                {
                    var scaled = new Color[pixels.Length * 4];
                    for (int cy = 0; cy < h * 2; cy++) for (int cx = 0; cx < w * 2; cx++) scaled[cy * w * 2 + cx] = pixels[(cy / 2) * w + cx / 2];
                    enlarged.SetPixels(scaled); enlarged.Apply(); file = Path.Combine(output, stem + "-" + label + "-world-nearest2x.png");
                    File.WriteAllBytes(file, enlarged.EncodeToPNG()); screenshots.Add(file);
                }
                finally { Destroy(enlarged); }
            }
            finally { Destroy(frame); capturing = false; }
            yield return null;
        }

        private void OnGUI()
        {
            if (!capturing || cameraView == null) return;
            GUI.Label(new Rect(24, 20, 1850, 28), "GROUND EQUIPMENT / " + mode + " / " + captureLabel + " / actual renderer, 16 screen pixels per cell; four rows of real native owners");
            for (int i = 0; i < Blueprints.Length; i++)
                GUI.Label(new Rect(24 + (i % 7) * 265, 60 + (i / 7) * 24, 260, 24), (i + 1) + ": " + Blueprints[i]);
            for (int i = 0; i < Blueprints.Length; i++)
            {
                Vector3 p = cameraView.WorldToScreenPoint(new Vector3(5.5f + 5 * i, 21f, 0));
                GUI.Label(new Rect(p.x - 10, Screen.height - p.y - 20, 40, 24), (i + 1).ToString());
            }
        }
        private void Add(string name, bool pass, string detail = null)
        { checks.Add(new Check { name = name, pass = pass, detail = detail }); if (!pass) Failures++; }
        private void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
        public void Abort(string message)
        {
            if (Finished) return; StopAllCoroutines(); fatal = message; Add("abort", false, message);
            StopRecorders(); Finished = true; WriteReport();
        }
        public void SetUnexpectedErrors(int count) { report.unexpectedErrors = count; WriteReport(); }
        private void WriteReport()
        {
            int errors = report?.unexpectedErrors ?? 0;
            report = new Report { runId = runId, mode = mode, workloadVersion = WorkloadVersion, privateRoot = saveRoot,
                failures = Failures, unexpectedErrors = errors, fatal = fatal, seconds = report?.seconds ?? (Time.realtimeSinceStartupAsDouble - started),
                // Teardown runs after Play stops; retain measured view/time instead
                // of replacing them with the restored Editor window and reset clock.
                width = report?.width ?? Screen.width, height = report?.height ?? Screen.height, targetFrameRate = 60, checks = checks.ToArray(),
                claims = claims.ToArray(), screenshots = screenshots.ToArray(), phases = phases.ToArray(), rawFrameCount = rawCount,
                rawFramesPath = Path.Combine(output, stem + "-frames.csv"), cleanupObserved = cleaned, settingsRestored = settingsRestored, inputUntouched = inputUntouched,
                bounds = "Synthetic owned 80x25 scene: real EntityFactory items, ZoneRenderer, LightMap and EnvironmentSpriteRenderer. Direct native MovementSystem/InventorySystem stimuli, no simulated keyboard play. Invisible minimal carrier and occupied legacy hand slot isolate item claims. Native Input.Update is only its null-context idle guard. Fixed100 actions per active25s phase, target60Hz, screenshots outside timing. Bounded non-wrapping recorders consume each GetSample once; zero-emission observations are counted separately from actual sample counts (sparse idle is valid only with a discovered valid handle). Completed-frame profiler flush has normal one-frame latency; aggregate includes renderer/harness/editor overhead, not isolated sprite cost or FPS. Normal/dim screens are real captures; enlarged images are nearest-neighbour copies. No native save roundtrip or ordinary-world gameplay/subjective readability claim." };
            File.WriteAllText(Path.Combine(output, stem + "-native.json"), JsonUtility.ToJson(report, true));
        }
        private void CollectOwnedTileResources()
        {
#if UNITY_EDITOR
            var seen = new HashSet<int>();
            void Own(UnityEngine.Object asset)
            {
                if (asset == null || priorAssetIds.Contains(asset.GetInstanceID()) || UnityEditor.EditorUtility.IsPersistent(asset) || !seen.Add(asset.GetInstanceID())) return;
                ownedAssets.Add(asset);
            }
            foreach (var tile in Resources.FindObjectsOfTypeAll<Tile>())
            {
                if (priorTileIds.Contains(tile.GetInstanceID()) || UnityEditor.EditorUtility.IsPersistent(tile)) continue;
                Own(tile); Own(tile.sprite); if (tile.sprite != null) Own(tile.sprite.texture);
            }
#endif
        }
        private void OnDestroy()
        {
            if (!initialized || cleaned) return;
            StopRecorders(); SaveGameService.RegisterRuntime(null, null);
            if (environment != null) environment.ReleaseAllClaims();
            ZoneRenderHooks.CellDirtyCallback = oldCellHook; ZoneRenderHooks.FullDirtyCallback = oldFullHook;
            Village3DSettings.Enabled = oldVoxel; Village3DSettings.LowDetail = oldLowDetail;
            Application.runInBackground = oldBackground; Application.targetFrameRate = oldFrameRate; QualitySettings.vSyncCount = oldVsync;
            WritePerf(originalPerf);
            // Only assets made by this fixture's actual tile generation, never
            // imported Resources or pre-existing editor assets.
            foreach (var asset in ownedAssets) if (asset != null) DestroyImmediate(asset);
            ownedAssets.Clear();
            settingsRestored = Application.runInBackground == oldBackground && Application.targetFrameRate == oldFrameRate
                && QualitySettings.vSyncCount == oldVsync && Village3DSettings.Enabled == oldVoxel && Village3DSettings.LowDetail == oldLowDetail;
            inputUntouched = ReferenceEquals(originalInputSettings, UnityEngine.InputSystem.InputSystem.settings)
                && originalDevices.Length == UnityEngine.InputSystem.InputSystem.devices.Count;
            for (int i = 0; inputUntouched && i < originalDevices.Length; i++) inputUntouched &= originalDevices[i] == UnityEngine.InputSystem.InputSystem.devices[i].deviceId;
            cleaned = true;
            if (report != null) WriteReport();
        }
        [Serializable] public sealed class Check { public string name, detail; public bool pass; }
        [Serializable] public sealed class Claim { public string light, blueprint, expected, tile, sprite; public int x, zoneY; public Color tint; }
        [Serializable] public sealed class Report
        {
            public string runId, mode, workloadVersion, privateRoot, fatal, bounds, rawFramesPath;
            public int failures, unexpectedErrors, width, height, targetFrameRate, rawFrameCount;
            public double seconds; public bool cleanupObserved, settingsRestored, inputUntouched; public Check[] checks; public Claim[] claims; public string[] screenshots; public Phase[] phases;
        }
    }
}
