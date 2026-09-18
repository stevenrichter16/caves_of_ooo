using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Native editor launcher for a disposable synthetic renderer scene.
    /// Explicit --equipment-ground-mode before|after; never changes renderer source.
    /// NativeSaveIsolation owns all saves through Play teardown.</summary>
    [InitializeOnLoad]
    public static class EquipmentGroundSpriteNativeAuditBatch
    {
        private const string Prefix = "EquipmentGround.NativeAudit.";
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static EquipmentGroundSpriteNativeAuditBatch()
        {
            if (SessionState.GetBool(Prefix + "finishing", false))
            {
                Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
                EditorApplication.update += CompleteWhenStopped;
            }
            else if (SessionState.GetBool(Prefix + "active", false)) Subscribe();
        }
        public static void RunFromCommandLine()
        {
            double earliest = EditorApplication.timeSinceStartup + 12;
            void StartWhenSettled()
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) { earliest = EditorApplication.timeSinceStartup + 12; return; }
                if (EditorApplication.timeSinceStartup < earliest) return;
                EditorApplication.update -= StartWhenSettled;
                try { RunCore(Argument("--equipment-ground-mode", null), Argument("--equipment-ground-output", null), true); }
                catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(4); }
            }
            EditorApplication.update += StartWhenSettled;
        }
        [MenuItem("Caves Of Ooo/Scenarios/Equipment/Ground Sprite AFTER Benchmark")]
        public static void Launch() => RunCore("after", null, false);
        private static string Argument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) if (args[i] == key)
            { if (i + 1 == args.Length || args[i + 1].StartsWith("-")) throw new ArgumentException("Missing " + key); return args[i + 1]; }
            return fallback;
        }
        private static void RunCore(string mode, string output, bool exitEditor)
        {
            if (mode != "before" && mode != "after") throw new ArgumentException("Explicit --equipment-ground-mode before|after required.");
            if (Application.isBatchMode) throw new InvalidOperationException("Run a native editor without -batchmode or -nographics.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            if (SessionState.GetBool(Prefix + "active", false)) throw new InvalidOperationException("Ground audit already active.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Refusing to discard scene edits.");
            var saved = new Scenes { rows = new SceneRow[setup.Length] };
            for (int i = 0; i < setup.Length; i++) saved.rows[i] = new SceneRow { path = setup[i].path, isLoaded = setup[i].isLoaded, isActive = setup[i].isActive };
            string id = Guid.NewGuid().ToString("N");
            output = Path.GetFullPath(output ?? Path.Combine(Application.dataPath, "../Docs/Verification/ReleaseStabilization/GroundSprites"));
            Directory.CreateDirectory(output);
            SessionState.SetString(Prefix + "runId", id); SessionState.SetString(Prefix + "mode", mode); SessionState.SetString(Prefix + "output", output);
            SessionState.SetString(Prefix + "scenes", JsonUtility.ToJson(saved));
            SessionState.SetBool(Prefix + "exit", exitEditor); SessionState.SetBool(Prefix + "finishing", false); SessionState.SetBool(Prefix + "started", false);
            SessionState.SetInt(Prefix + "errors", 0); SessionState.SetString(Prefix + "errorText", ""); SessionState.SetBool(Prefix + "viewConfigured", false);
            SessionState.SetBool(Prefix + "oldSaveRootWasNull", SaveGameService.SaveRootOverride == null); SessionState.SetString(Prefix + "oldSaveRoot", SaveGameService.SaveRootOverride ?? "");
            SessionState.SetBool(Prefix + "oldLastGamePresent", PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey)); SessionState.SetString(Prefix + "oldLastGame", PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
            SnapshotPref(Village3DSettings.PreferenceKey, "voxel"); SnapshotPref(Village3DSettings.LowDetailPreferenceKey, "low");
            try
            {
                Configure1080p();
                string token = NativeSaveIsolation.Begin(Prefix, "Equipment-ground-marker-" + id, false);
                SessionState.SetString(Prefix + "token", token); SessionState.SetString(Prefix + "root", SaveGameService.SaveRootOverride);
                SessionState.SetBool(Prefix + "active", true); SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 240);
                WriteProvenance(); EnableOwnedFullReload(); Subscribe();
                // Unsaved owned scene is restored from the exact captured scene setup.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); EditorApplication.isPlaying = true;
            }
            catch
            {
                if (SessionState.GetBool(Prefix + "active", false)) Finish(4);
                else { RestoreEditorSettings(); RestoreView(); RestoreScenes(); }
                throw;
            }
        }
        private const string EditorSettingsPath = "ProjectSettings/EditorSettings.asset";
        private static void EnableOwnedFullReload()
        {
            SessionState.SetBool(Prefix + "oldReloadEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(Prefix + "oldReloadOptions", (int)EditorSettings.enterPlayModeOptions);
            SessionState.SetString(Prefix + "oldEditorSettingsBytes", Convert.ToBase64String(File.ReadAllBytes(EditorSettingsPath)));
            SessionState.SetString(Prefix + "oldPlayModeStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? "");
            SessionState.SetBool(Prefix + "reloadOwned", true);
            // Private clone only; restore both live editor settings and exact
            // serialized bytes after Play, including the original reload-disabled mode.
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorSceneManager.playModeStartScene = null;
        }
        private static void RestoreEditorSettings()
        {
            if (!SessionState.GetBool(Prefix + "reloadOwned", false)) return;
            string startScene = SessionState.GetString(Prefix + "oldPlayModeStartScene", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(startScene) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(startScene);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Prefix + "oldReloadOptions", 0);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Prefix + "oldReloadEnabled", false);
            File.WriteAllBytes(EditorSettingsPath, Convert.FromBase64String(SessionState.GetString(Prefix + "oldEditorSettingsBytes", "")));
            SessionState.SetBool(Prefix + "reloadOwned", false);
        }
        private static bool EditorSettingsRestored() => (AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? "") == SessionState.GetString(Prefix + "oldPlayModeStartScene", "")
            && EditorSettings.enterPlayModeOptionsEnabled == SessionState.GetBool(Prefix + "oldReloadEnabled", false)
            && (int)EditorSettings.enterPlayModeOptions == SessionState.GetInt(Prefix + "oldReloadOptions", 0)
            && Convert.ToBase64String(File.ReadAllBytes(EditorSettingsPath)) == SessionState.GetString(Prefix + "oldEditorSettingsBytes", "");
        private static void SnapshotPref(string key, string suffix)
        { SessionState.SetBool(Prefix + suffix + "Present", PlayerPrefs.HasKey(key)); SessionState.SetInt(Prefix + suffix + "Value", PlayerPrefs.GetInt(key)); }
        private static bool PrefUnchanged(string key, string suffix) => PlayerPrefs.HasKey(key) == SessionState.GetBool(Prefix + suffix + "Present", false)
            && (!PlayerPrefs.HasKey(key) || PlayerPrefs.GetInt(key) == SessionState.GetInt(Prefix + suffix + "Value", 0));
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "token", ""), Finish);
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false) || SessionState.GetBool(Prefix + "finishing", false)) return;
            EquipmentGroundSpriteNativeAudit driver = null;
            try
            {
                driver = UnityEngine.Object.FindFirstObjectByType<EquipmentGroundSpriteNativeAudit>();
                if (EditorApplication.isPlaying && !EditorApplication.isPaused && !SessionState.GetBool(Prefix + "started", false))
                {
                    SessionState.SetBool(Prefix + "started", true);
                    driver = new GameObject("Equipment ground sprite native audit").AddComponent<EquipmentGroundSpriteNativeAudit>();
                    driver.Initialize(SessionState.GetString(Prefix + "runId", ""), SessionState.GetString(Prefix + "mode", ""), SessionState.GetString(Prefix + "output", ""));
                }
                if (driver != null && driver.Finished)
                { driver.SetUnexpectedErrors(SessionState.GetInt(Prefix + "errors", 0)); Finish(driver.Failures == 0 ? 0 : 1); return; }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
                { driver?.Abort("Four-minute native benchmark watchdog."); Finish(2); }
            }
            catch (Exception error) { Debug.LogException(error); driver?.Abort(error.ToString()); Finish(4); }
        }
        private static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
            string prior = SessionState.GetString(Prefix + "errorText", "");
            if (prior.Length < 40000) SessionState.SetString(Prefix + "errorText", prior + type + ": " + message + "\n" + trace + "\n");
        }
        private static void Finish(int code)
        {
            if (SessionState.GetBool(Prefix + "finishing", false)) return;
            SessionState.SetBool(Prefix + "finishing", true); SessionState.SetInt(Prefix + "code", code);
            EditorApplication.update -= Poll; SaveGameService.RegisterRuntime(null, null);
            EditorApplication.update -= CompleteWhenStopped; EditorApplication.update += CompleteWhenStopped;
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "token", ""), code);
        }
        private static void CompleteWhenStopped()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active", false)) return;
            EditorApplication.update -= CompleteWhenStopped; Complete();
        }
        private static string FileStem => "EGSN-" + SessionState.GetString(Prefix + "mode", "") + "-" + SessionState.GetString(Prefix + "runId", "");
        private static string OutputPath(string suffix) => Path.Combine(SessionState.GetString(Prefix + "output", ""), FileStem + suffix);
        private static void Complete()
        {
            if (!SessionState.GetBool(Prefix + "finishing", false)) return;
            int code = SessionState.GetInt(Prefix + "code", 4);
            string id = SessionState.GetString(Prefix + "runId", ""), mode = SessionState.GetString(Prefix + "mode", ""), root = SessionState.GetString(Prefix + "root", "");
            var cleanup = new Cleanup { runId = id, mode = mode, privateRoot = root, privateRootRemoved = !string.IsNullOrEmpty(root) && !Directory.Exists(root),
                nativeCleanupCode = SessionState.GetInt("CavesOfOoo.NativeSaveIsolation.lastCode", 4) };
            try
            {
                try { RestoreScenes(); cleanup.scenesRestored = SceneSetupRestored(); } catch (Exception ex) { cleanup.error += "Scene restore: " + ex; }
                try { RestoreView(); cleanup.viewRestored = ViewConfigurationRestored(); } catch (Exception ex) { cleanup.error += "View restore: " + ex; }
                try { RestoreEditorSettings(); cleanup.editorSettingsRestored = EditorSettingsRestored(); } catch (Exception ex) { cleanup.error += "Editor settings restore: " + ex; }
                string oldRoot = SessionState.GetBool(Prefix + "oldSaveRootWasNull", false) ? null : SessionState.GetString(Prefix + "oldSaveRoot", "");
                cleanup.saveRootRestored = string.Equals(SaveGameService.SaveRootOverride, oldRoot, StringComparison.Ordinal);
                cleanup.lastGameRestored = PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey) == SessionState.GetBool(Prefix + "oldLastGamePresent", false)
                    && (!PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey) || PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey) == SessionState.GetString(Prefix + "oldLastGame", ""));
                cleanup.displayPreferencesUnchanged = PrefUnchanged(Village3DSettings.PreferenceKey, "voxel") && PrefUnchanged(Village3DSettings.LowDetailPreferenceKey, "low");
                string json = File.Exists(OutputPath("-native.json")) ? File.ReadAllText(OutputPath("-native.json")) : "";
                cleanup.reportVerified = ValidateReport(json, id, mode, root);
                cleanup.validatorCounterchecks = Counterchecks(json, id, mode, root);
                if (cleanup.reportVerified)
                {
                    var report = JsonUtility.FromJson<EquipmentGroundSpriteNativeAudit.Report>(json);
                    cleanup.artifactsVerified = report.screenshots.All(p => p.StartsWith(SessionState.GetString(Prefix + "output", "") + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && File.Exists(p) && new FileInfo(p).Length > 1000)
                        && File.Exists(report.rawFramesPath) && File.ReadLines(report.rawFramesPath).Count() == report.rawFrameCount + 1;
                }
                cleanup.unexpectedErrors = SessionState.GetInt(Prefix + "errors", 0); cleanup.errors = SessionState.GetString(Prefix + "errorText", "");
                if (!cleanup.privateRootRemoved || cleanup.nativeCleanupCode != 0 || !cleanup.scenesRestored || !cleanup.viewRestored || !cleanup.saveRootRestored
                    || !cleanup.lastGameRestored || !cleanup.editorSettingsRestored || !cleanup.displayPreferencesUnchanged || !cleanup.reportVerified || !cleanup.validatorCounterchecks || !cleanup.artifactsVerified || cleanup.unexpectedErrors != 0) code = 4;
            }
            catch (Exception error) { code = 4; cleanup.error = error.ToString(); }
            finally
            {
                Application.logMessageReceived -= OnLog; SessionState.SetBool(Prefix + "active", false); SessionState.SetBool(Prefix + "finishing", false);
                cleanup.exitCode = code; File.WriteAllText(OutputPath("-cleanup.json"), JsonUtility.ToJson(cleanup, true));
                Debug.Log("[EquipmentGroundNativeAudit] mode=" + mode + " run=" + id + " cleanup exit=" + code);
                if (SessionState.GetBool(Prefix + "exit", false)) EditorApplication.Exit(code);
            }
        }

        // Pure receipt validation, independent of filesystem existence. Every
        // successful run mutation-tests this validator before final acceptance.
        public static bool ValidateReport(string json, string id, string mode, string privateRoot)
        {
            if (!Guid.TryParseExact(id, "N", out var guid) || guid == Guid.Empty || (mode != "before" && mode != "after") || string.IsNullOrEmpty(privateRoot) || string.IsNullOrEmpty(json)) return false;
            try
            {
                var r = JsonUtility.FromJson<EquipmentGroundSpriteNativeAudit.Report>(json);
                if (r == null || r.runId != id || r.mode != mode || r.privateRoot != privateRoot || r.workloadVersion != EquipmentGroundSpriteNativeAudit.WorkloadVersion
                    || r.failures != 0 || r.unexpectedErrors != 0 || !string.IsNullOrEmpty(r.fatal) || !r.cleanupObserved || !r.settingsRestored || !r.inputUntouched
                    || r.width != 1920 || r.height != 1080 || r.targetFrameRate != 60 || r.phases == null || r.phases.Length != 3 || r.rawFrameCount < 300
                    || r.checks == null || r.checks.Length < 50 || r.checks.Any(c => c == null || !c.pass || string.IsNullOrEmpty(c.name))
                    || r.checks.Select(c => c.name).Distinct().Count() != r.checks.Length
                    || r.claims == null || r.claims.Length != 28 || r.screenshots == null || r.screenshots.Length != 4 || r.screenshots.Distinct().Count() != 4) return false;
                foreach (string check in new[] { "real_production_renderer", "actual_14_blueprints_four_copies", "expected_logical_mode", "dim_uses_native_lightmap",
                    "release_restores_item_glyph", "release_reclaims_item", "workload_complete", "native_inventory_roundtrip", "owned_save_only" })
                    if (!r.checks.Any(c => c.name == check)) return false;
                string[] bps = { "Dagger", "ShortSword", "LongSword", "Spear", "LeatherBoots", "IronshodBoots", "LeatherGloves", "LeatherCap", "IronHelmet", "Mace", "FireTonic", "LeatherArmor", "Chest", "WatchLantern" };
                string[] afterTiles = { "item_dagger", "item_sword", "item_sword", "item_spear", "item_boots", "item_boots", "item_gloves", "item_helmet", "item_helmet", "item_mace", "item_vial", "item_armor", "Chest", "Lantern" };
                string[] beforeTiles = { "WeaponGround", "WeaponGround", "WeaponGround", "WeaponGround", "item_armor", "item_armor", "item_armor", "item_armor", "item_armor", "item_vial", "item_vial", "item_armor", "Chest", "Lantern" };
                foreach (string light in new[] { "normal", "dim" }) for (int i = 0; i < bps.Length; i++)
                {
                    var matches = r.claims.Where(c => c != null && c.light == light && c.blueprint == bps[i]).ToArray();
                    string tile = mode == "after" ? afterTiles[i] : beforeTiles[i];
                    if (matches.Length != 1 || matches[0].tile != tile || matches[0].expected != tile || string.IsNullOrEmpty(matches[0].sprite)
                        || matches[0].x != 5 + 5 * i || matches[0].zoneY != 5 || !r.checks.Any(c => c.name == light + "_claim_" + bps[i])) return false;
                }
                string expectedStem = "EGSN-" + mode + "-" + id;
                if (Path.GetFileName(r.rawFramesPath) != expectedStem + "-frames.csv") return false;
                foreach (string suffix in new[] { "normal.png", "dim.png", "normal-world-nearest2x.png", "dim-world-nearest2x.png" })
                    if (!r.screenshots.Any(s => Path.GetFileName(s) == expectedStem + "-" + suffix)) return false;
                string[] names = { "idle", "walk_full_redraw", "pickup_drop_incremental" };
                string[] metrics = { "COO.EnvSprites.PostRender", "COO.ZoneRenderer.LateUpdate", "COO.Input.Update", "Main Thread", "GC Allocated In Frame" };
                int frames = 0;
                for (int i = 0; i < 3; i++)
                {
                    var p = r.phases[i];
                    if (p == null || p.name != names[i] || double.IsNaN(p.seconds) || double.IsInfinity(p.seconds) || p.seconds < 25 || p.seconds > 28
                        || p.frames < 100 || p.rawStart != frames || p.operations != (i == 0 ? 0 : 100) || p.metrics == null || p.metrics.Length != 5 || p.counters == null || p.counters.Length != 8) return false;
                    frames += p.frames;
                    if (p.moves != (i == 1 ? 100 : 0) || p.pickups != (i == 2 ? 50 : 0) || p.drops != (i == 2 ? 50 : 0) || p.overlayChecks != (i == 2 ? 100 : 0)) return false;
                    for (int j = 0; j < 5; j++)
                    {
                        var m = p.metrics[j];
                        if (m == null || m.name != metrics[j] || !m.available || m.observedFrames != p.frames || m.count < 0 || (!(j == 0 && i == 0) && m.count < 10) || m.units != (j == 4 ? "Bytes" : "TimeNanoseconds")
                            || m.sum < 0 || m.max < 0 || m.p99 < 0 || double.IsNaN(m.average) || Math.Abs(m.average - (double)m.sum / m.observedFrames) > .001
                            || (!(j == 0 && i == 0) && j != 4 && m.max == 0)) return false;
                    }
                    if (i == 0 && (p.counters[0] != 0 || p.counters[3] != 0 || p.metrics[0].sum != 0)) return false;
                    if (i == 1 && (p.counters[1] < 100 || p.counters[3] < 200000)) return false;
                    if (i == 2 && (p.counters[1] != 0 || p.counters[2] < 100 || p.counters[3] > 1800)) return false;
                }
                return frames == r.rawFrameCount;
            }
            catch { return false; }
        }
        private static bool Counterchecks(string json, string id, string mode, string root)
        {
            if (!ValidateReport(json, id, mode, root) || ValidateReport("", id, mode, root) || ValidateReport(json, id, mode == "before" ? "after" : "before", root)
                || ValidateReport(json, Guid.NewGuid().ToString("N"), mode, root)) return false;
            bool Reject(Action<EquipmentGroundSpriteNativeAudit.Report> mutate)
            { var report = JsonUtility.FromJson<EquipmentGroundSpriteNativeAudit.Report>(json); mutate(report); return !ValidateReport(JsonUtility.ToJson(report), id, mode, root); }
            return Reject(r => r.phases[0].seconds = 24.9) && Reject(r => r.phases = Array.Empty<EquipmentGroundSpriteNativeAudit.Phase>())
                && Reject(r => r.phases[1] = null) && Reject(r => r.phases[1].operations = 99)
                && Reject(r => r.phases[2].metrics[0].available = false) && Reject(r => r.phases[0].metrics[1].count = 0)
                && Reject(r => r.phases[0].metrics[0].units = "Bytes") && Reject(r => r.workloadVersion = "other")
                && Reject(r => r.rawFrameCount = 0) && Reject(r => r.cleanupObserved = false);
        }
        private static void WriteProvenance()
        {
            var paths = new List<string> { "Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs", "Assets/Resources/Content/Blueprints/Objects.json",
                "Assets/Scripts/Scenarios/Custom/EquipmentGroundSpriteNativeAudit.cs", "Assets/Scripts/Scenarios/Custom/EquipmentGroundSpriteNativeAudit.Profile.cs",
                "Assets/Editor/Scenarios/EquipmentGroundSpriteNativeAuditBatch.cs" };
            foreach (string body in new[] { "item_dagger", "item_sword", "item_spear", "item_boots", "item_gloves", "item_helmet", "item_mace" })
                paths.Add("Assets/Resources/Sprites/Environment/" + body + ".png");
            var rows = new List<Source>();
            foreach (string p in paths)
            {
                string hash = null;
                if (File.Exists(p)) using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-", "").ToLowerInvariant();
                rows.Add(new Source { path = p, sha256 = hash, exists = File.Exists(p) });
            }
            File.WriteAllText(OutputPath("-provenance.json"), JsonUtility.ToJson(new Provenance { runId = SessionState.GetString(Prefix + "runId", ""), mode = SessionState.GetString(Prefix + "mode", ""),
                workload = EquipmentGroundSpriteNativeAudit.WorkloadVersion, unity = Application.unityVersion, utc = DateTime.UtcNow.ToString("O"), sources = rows.ToArray() }, true));
        }
        private static void RestoreScenes()
        {
            var saved=JsonUtility.FromJson<Scenes>(SessionState.GetString(Prefix+"scenes","{}"));
            if(saved?.rows==null||saved.rows.Length==0)return;
            var setup=new SceneSetup[saved.rows.Length];for(int i=0;i<setup.Length;i++)setup[i]=new SceneSetup{path=saved.rows[i].path,isLoaded=saved.rows[i].isLoaded,isActive=saved.rows[i].isActive};
            if(setup.Length==1&&string.IsNullOrEmpty(setup[0].path))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        private static bool SceneSetupRestored()
        {
            var saved=JsonUtility.FromJson<Scenes>(SessionState.GetString(Prefix+"scenes","{}"));
            var actual=EditorSceneManager.GetSceneManagerSetup();
            if(saved?.rows==null||saved.rows.Length!=actual.Length)return false;
            for(int i=0;i<actual.Length;i++)
                if(saved.rows[i].path!=actual[i].path||saved.rows[i].isLoaded!=actual[i].isLoaded||saved.rows[i].isActive!=actual[i].isActive)return false;
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)return false;
            return true;
        }
        private static Type EditorType(string name)=>typeof(UnityEditor.Editor).Assembly.GetType(name,true);
        private static object Sizes()
        {
            var type=EditorType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);
            return singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        }
        private static object Group()
        {
            var sizes=Sizes();var kind=EditorType("UnityEditor.GameViewSizeGroupType");
            return sizes.GetType().GetMethod("GetGroup",All).Invoke(sizes,new[]{Enum.Parse(kind,"Standalone")});
        }
        private static EditorWindow View()=>EditorWindow.GetWindow(EditorType("UnityEditor.GameView"));
        private static void Configure1080p()
        {
            var viewType=EditorType("UnityEditor.GameView");SessionState.SetBool(Prefix+"hadView",Resources.FindObjectsOfTypeAll(viewType).Length>0);
            var view=View();var selected=viewType.GetProperty("selectedSizeIndex",All);if(selected==null)throw new InvalidOperationException("GameView fixed-size API unavailable.");
            SessionState.SetInt(Prefix+"oldViewIndex",(int)selected.GetValue(view));SessionState.SetInt(Prefix+"addedCustomIndex",-1);
            SessionState.SetBool(Prefix+"viewConfigured",true);var group=Group();var gt=group.GetType();
            int built=(int)gt.GetMethod("GetBuiltinCount",All).Invoke(group,null),custom=(int)gt.GetMethod("GetCustomCount",All).Invoke(group,null),chosen=-1;
            SessionState.SetInt(Prefix+"oldCustomCount",custom);
            for(int i=0;i<built+custom;i++)
            {
                var size=gt.GetMethod("GetGameViewSize",All).Invoke(group,new object[]{i});
                int w=(int)size.GetType().GetProperty("width",All).GetValue(size),h=(int)size.GetType().GetProperty("height",All).GetValue(size);
                if(w==1920&&h==1080){chosen=i;break;}
            }
            if(chosen<0)
            {
                var sizeType=EditorType("UnityEditor.GameViewSize");var enumType=EditorType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Equipment ground audit 1920x1080"},null);
                gt.GetMethod("AddCustomSize",All).Invoke(group,new[]{size});SessionState.SetInt(Prefix+"addedCustomIndex",custom);chosen=built+custom;
            }
            selected.SetValue(view,chosen);view.Show();view.Focus();view.Repaint();EditorApplication.QueuePlayerLoopUpdate();
        }
        private static void RestoreView()
        {
            if(!SessionState.GetBool(Prefix+"viewConfigured",false))return;
            var view=View();var type=view.GetType();int added=SessionState.GetInt(Prefix+"addedCustomIndex",-1);
            if(added>=0){var group=Group();group.GetType().GetMethod("RemoveCustomSize",All).Invoke(group,new object[]{added});}
            type.GetProperty("selectedSizeIndex",All).SetValue(view,SessionState.GetInt(Prefix+"oldViewIndex",0));view.Repaint();
            if(!SessionState.GetBool(Prefix+"hadView",true))view.Close();SessionState.SetBool(Prefix+"viewConfigured",false);
        }
        private static bool ViewConfigurationRestored()
        {
            var viewType=EditorType("UnityEditor.GameView");var views=Resources.FindObjectsOfTypeAll(viewType);
            bool hadView=SessionState.GetBool(Prefix+"hadView",true);
            var group=Group();int count=(int)group.GetType().GetMethod("GetCustomCount",All).Invoke(group,null);
            if(count!=SessionState.GetInt(Prefix+"oldCustomCount",-1))return false;
            if(!hadView)return views.Length==0;
            // RestoreView uses the same GetWindow selection path as Configure1080p.
            return views.Length>0&&(int)viewType.GetProperty("selectedSizeIndex",All).GetValue(View())==SessionState.GetInt(Prefix+"oldViewIndex",-1);
        }
        [Serializable] private sealed class Scenes { public SceneRow[] rows; }
        [Serializable] private sealed class SceneRow { public string path; public bool isLoaded, isActive; }
        [Serializable] private sealed class Source { public string path, sha256; public bool exists; }
        [Serializable] private sealed class Provenance { public string runId, mode, workload, unity, utc; public Source[] sources; }
        [Serializable] private sealed class Cleanup
        {
            public string runId, mode, privateRoot, error, errors;
            public int exitCode, nativeCleanupCode, unexpectedErrors;
            public bool privateRootRemoved, scenesRestored, viewRestored, saveRootRestored, lastGameRestored, displayPreferencesUnchanged,
                reportVerified, artifactsVerified, validatorCounterchecks, editorSettingsRestored;
        }
    }
}
