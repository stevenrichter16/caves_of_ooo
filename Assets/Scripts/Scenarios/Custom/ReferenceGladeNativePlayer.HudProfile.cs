using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    // Deliberately uses only APIs present before the HUD polish. Import this
    // observer and its launcher hooks BEFORE the new HUD source/tests.
    public sealed partial class ReferenceGladeNativePlayer
    {
        bool _hudProfileOnly, _hudProfileSampling, _hudProfileOverflow;
        string _hudProfileStage = "before", _hudProfilePhase = "setup";
        Action _hudProfileCleanup;
        Coroutine _hudProfileObserver;
        const int HudProfileCapacity = 12000;
        int _hudProfileCount;
        HudProfileRow[] _hudProfileRows;
        ProfilerRecorder[] _hudProfileRecorders;
        MarkerDescription[] _hudProfileDescriptions;
        double[][] _hudProfileValues;
        bool[][] _hudProfileHasValue;
        static readonly string[] HudProfileMarkers = { "COO.ZoneRenderer.RenderSidebar", "COO.ZoneRenderer.RenderHotbar",
            "COO.UI.Sidebar.Render", "COO.UI.Hotbar.Render", "GC Allocated In Frame" };
        static readonly string[] HudProfileChecks = { "hud_profile_original_source_and_free_start", "hud_profile_native_cycle_returns",
            "hud_profile_real_history_created", "hud_profile_existing_scroll_roundtrip", "hud_profile_existing_look_roundtrip",
            "hud_profile_all_inputs_free", "hud_profile_idle_zero_hotbar_paints", "hud_profile_samples_and_marker_availability",
            "hud_profile_configuration_unchanged", "hud_profile_gamepad_removed_and_restored" };
        bool HudProfileComplete => _audit.Count == HudProfileChecks.Length + 1
            && HudProfileChecks.All(n => _audit.Count(a => a == "PASS " + n) == 1)
            && _audit.Count(a => a == "PASS ordinary_player_and_native_zone") == 1 && _screenshots.Count >= 3;
        const string HudProfileCanVerify = "Identical existing-API input recipe on an ordinary isolated seed64 start:120 idle frames,24 actual D-pad slot changes,40 alternating empty-slot attempts,12 older/newer existing-sidebar scroll keys, and six ordinary Look/cursor/return cycles. No paid action or direct renderer call. Raw completed-frame HUD work counters and native recorder samples are written separately from screenshots with actual viewport/settings. Before/after stage changes only the receipt label.";
        const string HudProfileCannotVerify = "Diagnostic editor observation, not standalone/physical Deck/GPU performance or subjective readability. Native recorder LastValue may describe an earlier completed frame; explicit snapshot and observer frame IDs prevent false correlation. Counter observation materializes a diagnostic snapshot once per observed frame; recorder/input coroutine overhead is shared in both runs. No long-session or combat-performance claim. A before/after claim requires retained successful runs with matching source-approved recipe, viewport, graphics, focus, frame limit and actual action counts; otherwise report unmatched evidence only. This mode does not exercise the new full-history reader and does not resize the view or change preferences.";
        public void ConfigureHudProfile(string stage)
        {
            if (stage != "before" && stage != "after") throw new ArgumentException("HUD profile stage must be before or after.", nameof(stage));
            _hudProfileOnly = true; _hudProfileStage = stage;
        }

        IEnumerator RunHudProfileAudit()
        {
            var previousPad = Gamepad.current; var pad = InputSystem.AddDevice<Gamepad>();
            _hudProfileCleanup = () => {
                StopHudProfileObserver();
                if (pad.added) { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.RemoveDevice(pad); }
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                Check("hud_profile_gamepad_removed_and_restored", !pad.added
                    && (previousPad == null || !previousPad.added || ReferenceEquals(previousPad, Gamepad.current)));
            };
            try
            {
                yield return ControllerHold(pad, new GamepadState(), .05f);
                yield return new WaitForSecondsRealtime(1f); yield return new WaitForEndOfFrame();
                var abilities = _input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();
                Require(State() == "Normal" && Cell().X == 40 && Cell().Y == 12 && abilities != null, "ordinary stable HUD benchmark start");
                var occupied = Enumerable.Range(0, 10).Where(i => abilities.GetAbilityBySlot(i) != null).ToArray();
                Require(occupied.Length == 6 && abilities.GetAbilityBySlot(8) == null && abilities.GetAbilityBySlot(9) == null,
                    "unchanged original six occupied/four empty slots");
                var checkpoint = ControllerBefore(); int selected = _input.CaptureHotbarSelection();
                Check("hud_profile_original_source_and_free_start", _input.ZoneRenderer.SidebarLogScrollOffsetRows == 0 && ControllerCost(checkpoint, 0));
                yield return Capture("hud-profile-01-before-input");
                var settingsBefore = ObserveMarkerSettings();
                string camerasBefore = HudProfileCameras(); string preferencesBefore = HudProfilePreferences();
                string initialSlots = string.Join("|", Enumerable.Range(0, 10).Select(i => { var a = abilities.GetAbilityBySlot(i);
                    return i + ":" + (a == null ? "empty" : a.SourcePowerClass + ":" + a.DisplayName + ":" + a.TargetingMode + ":" + a.Range + ":" + a.CooldownRemaining); }));
                string beganUtc = DateTime.UtcNow.ToString("O");
                BeginHudProfileObserver();
                _hudProfilePhase = "idle";
                for (int i = 0; i < 120; i++) yield return null;
                _hudProfilePhase = "cycle";
                for (int i = 0; i < 24; i++) yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadRight));
                Check("hud_profile_native_cycle_returns", _input.CaptureHotbarSelection() == selected && ControllerCost(checkpoint, 0));
                int oldCount = MessageLog.Count;
                _hudProfilePhase = "messages";
                for (int i = 0; i < 40; i++) yield return Tap(i % 2 == 0 ? Key.Digit9 : Key.Digit0);
                Check("hud_profile_real_history_created", MessageLog.Count == oldCount + 40
                    && MessageLog.GetRecentEntries(40).All(e => e.Text.Contains("No rite bound")));
                _hudProfilePhase = "scroll_older";
                for (int i = 0; i < 12; i++) yield return Tap(Key.Equals);
                int older = _input.ZoneRenderer.SidebarLogScrollOffsetRows;
                _hudProfilePhase = "scroll_newer";
                for (int i = 0; i < 12; i++) yield return Tap(Key.Minus);
                Check("hud_profile_existing_scroll_roundtrip", older > 0 && _input.ZoneRenderer.SidebarLogScrollOffsetRows == 0);
                _hudProfilePhase = "look";
                for (int i = 0; i < 6; i++) { yield return Tap(Key.L); yield return Tap(Key.RightArrow); yield return Tap(Key.Escape); }
                Check("hud_profile_existing_look_roundtrip", State() == "Normal");
                _hudProfilePhase = "settle";
                for (int i = 0; i < 12; i++) yield return null;
                _hudProfilePhase = "idle_after";
                for (int i = 0; i < 120; i++) yield return null;
                _hudProfileSampling = false;
                if (_hudProfileObserver != null) { StopCoroutine(_hudProfileObserver); _hudProfileObserver = null; }
                Check("hud_profile_all_inputs_free", ControllerCost(checkpoint, 0) && ControllerStayed(checkpoint));
                var idleRows = _hudProfileRows.Take(_hudProfileCount).Where(r => r.phase == "idle" || r.phase == "idle_after").ToArray();
                Check("hud_profile_idle_zero_hotbar_paints", idleRows.Length >= 200 && idleRows.All(r => r.hotbarRenders == 0));
                Check("hud_profile_samples_and_marker_availability", !_hudProfileOverflow && _hudProfileCount >= 240
                    && _hudProfileDescriptions.Take(4).All(d => d.valid));
                var settingsAfter = ObserveMarkerSettings();
                Check("hud_profile_configuration_unchanged", settingsBefore.screenWidth == settingsAfter.screenWidth
                    && settingsBefore.screenHeight == settingsAfter.screenHeight && settingsBefore.targetWidth == settingsAfter.targetWidth
                    && settingsBefore.targetHeight == settingsAfter.targetHeight && settingsBefore.msaa == settingsAfter.msaa
                    && settingsBefore.filter == settingsAfter.filter && settingsBefore.vSync == settingsAfter.vSync
                    && settingsBefore.targetFrameRate == settingsAfter.targetFrameRate && settingsBefore.timeScale == settingsAfter.timeScale
                    && settingsBefore.profilerEnabled == settingsAfter.profilerEnabled && settingsBefore.runInBackground == settingsAfter.runInBackground
                    && camerasBefore == HudProfileCameras() && preferencesBefore == HudProfilePreferences());
                WriteHudProfile(beganUtc, settingsBefore, settingsAfter, camerasBefore, preferencesBefore, initialSlots, checkpoint.Tick, oldCount);
                StopHudProfileObserver();
                yield return Capture("hud-profile-02-after-identical-input");
            }
            finally { _hudProfileCleanup?.Invoke(); _hudProfileCleanup = null; }
        }
        string HudProfileCameras()
        {
            var follow = _input.CameraFollow;
            return "hotbar=" + follow.HotbarCamera.pixelRect + ";sidebar=" + follow.SidebarCamera.pixelRect
                + ";hotbarTarget=" + (follow.HotbarCamera.targetTexture == null ? "screen" : "texture")
                + ";sidebarTarget=" + (follow.SidebarCamera.targetTexture == null ? "screen" : "texture");
        }
        string HudProfilePreferences()
        {
            var c = CultureInfo.InvariantCulture;
            return "native=" + Village3DSettings.Enabled + ";lowDetail=" + Village3DSettings.LowDetail
                + ";scale=" + Village3DSettings.WorldResolutionScale.ToString("R", c) + ";shadows=" + Village3DSettings.ShadowsEnabled
                + ";fx=" + SpellFxSettings.Mode + ";fxSpeed=" + SpellFxSettings.AnimationSpeed.ToString("R", c)
                + ";music=" + WorldAudioSettings.MusicVolume.ToString("R", c) + ";ambience=" + WorldAudioSettings.AmbienceVolume.ToString("R", c)
                + ";sfx=" + WorldAudioSettings.SfxVolume.ToString("R", c);
        }
        void BeginHudProfileObserver()
        {
            _hudProfileRows = new HudProfileRow[HudProfileCapacity]; _hudProfileCount = 0; _hudProfileOverflow = false;
            _hudProfileRecorders = new ProfilerRecorder[HudProfileMarkers.Length];
            _hudProfileDescriptions = new MarkerDescription[HudProfileMarkers.Length];
            _hudProfileValues = new double[HudProfileMarkers.Length][]; _hudProfileHasValue = new bool[HudProfileMarkers.Length][];
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            var available = handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            for (int i = 0; i < HudProfileMarkers.Length; i++)
            {
                _hudProfileValues[i] = new double[HudProfileCapacity]; _hudProfileHasValue[i] = new bool[HudProfileCapacity];
                var row = new MarkerDescription { name = HudProfileMarkers[i] }; _hudProfileDescriptions[i] = row;
                var found = available.Where(d => d.Name == row.name).ToArray(); row.matchingHandles = found.Length;
                if (found.Length != 1) { row.failure = "Unavailable or ambiguous exact native marker"; continue; }
                var d = found[0]; row.category = d.Category.Name; row.units = d.UnitType.ToString(); row.dataType = d.DataType.ToString();
                var options = ProfilerRecorderOptions.Default;
                if (row.name.StartsWith("COO.", StringComparison.Ordinal)) options |= ProfilerRecorderOptions.SumAllSamplesInFrame;
                _hudProfileRecorders[i] = ProfilerRecorder.StartNew(d.Category, d.Name, 2, options); row.valid = _hudProfileRecorders[i].Valid;
                if (!row.valid) row.failure = "Invalid native recorder";
            }
            _hudProfileSampling = true; _hudProfileObserver = StartCoroutine(ObserveHudProfile());
        }
        IEnumerator ObserveHudProfile()
        {
            var frameEnd = new WaitForEndOfFrame(); int previousFrame = -1;
            while (_hudProfileSampling)
            {
                yield return frameEnd;
                if (!_hudProfileSampling) yield break;
                if (_hudProfileCount >= HudProfileCapacity) { _hudProfileOverflow = true; yield break; }
                var snap = PerformanceDiagnostics.LastCompletedFrameSnapshot;
                if (snap.UnityFrame == previousFrame) continue;
                previousFrame = snap.UnityFrame; int index = _hudProfileCount++;
                _hudProfileRows[index] = new HudProfileRow { phase = _hudProfilePhase, observerFrame = Time.frameCount,
                    snapshotFrame = snap.UnityFrame, realtime = Time.realtimeSinceStartupAsDouble, wallMs = Time.unscaledDeltaTime * 1000d,
                    tick = snap.TurnTick, hotbarRenders = snap.HotbarRenderCount, sidebarRenders = snap.SidebarRenderCount,
                    tilemapClears = snap.TilemapClearCount, rendererMs = snap.ZoneRendererLateUpdateMs,
                    focused = Application.isFocused, selectedSlot = _input.CaptureHotbarSelection(), logOffset = _input.ZoneRenderer.SidebarLogScrollOffsetRows };
                for (int i = 0; i < _hudProfileRecorders.Length; i++)
                {
                    var d = _hudProfileDescriptions[i]; if (!d.valid) continue;
                    if (!_hudProfileRecorders[i].Valid) { d.valid = false; d.failure = "Recorder became invalid"; continue; }
                    if (_hudProfileRecorders[i].Count == 0) continue;
                    _hudProfileHasValue[i][index] = true;
                    _hudProfileValues[i][index] = d.dataType == "Double" || d.dataType == "Float"
                        ? _hudProfileRecorders[i].LastValueAsDouble : _hudProfileRecorders[i].LastValue;
                }
            }
        }
        void StopHudProfileObserver()
        {
            _hudProfileSampling = false;
            if (_hudProfileObserver != null) { StopCoroutine(_hudProfileObserver); _hudProfileObserver = null; }
            if (_hudProfileRecorders != null) { for (int i = 0; i < _hudProfileRecorders.Length; i++) _hudProfileRecorders[i].Dispose(); _hudProfileRecorders = null; }
        }
        void WriteHudProfile(string began, MarkerSettings before, MarkerSettings after, string cameras, string preferences, string initialSlots, int tick, int startLogCount)
        {
            Directory.CreateDirectory(DirectoryPath); var c = CultureInfo.InvariantCulture;
            using (var writer = new StreamWriter(Path.Combine(DirectoryPath, "hud-profile.csv")))
            {
                writer.Write("phase,observerFrame,snapshotFrame,realtimeSeconds,wallMs,tick,hotbarRenders,sidebarRenders,tilemapClears,rendererMs,focused,selectedSlot,logOffset");
                foreach (var name in HudProfileMarkers) writer.Write("," + name); writer.WriteLine();
                for (int f = 0; f < _hudProfileCount; f++)
                {
                    var r = _hudProfileRows[f]; writer.Write(string.Join(",", r.phase, r.observerFrame, r.snapshotFrame, r.realtime.ToString("R", c),
                        r.wallMs.ToString("R", c), r.tick, r.hotbarRenders, r.sidebarRenders, r.tilemapClears, r.rendererMs.ToString("R", c), r.focused ? 1 : 0, r.selectedSlot, r.logOffset));
                    for (int i = 0; i < HudProfileMarkers.Length; i++) { writer.Write(','); if (_hudProfileHasValue[i][f]) writer.Write(_hudProfileValues[i][f].ToString("R", c)); }
                    writer.WriteLine();
                }
            }
            File.WriteAllText(Path.Combine(DirectoryPath, "hud-profile.json"), JsonUtility.ToJson(new HudProfileReport {
                recipe = "hud-existing-input-v1", stage = _hudProfileStage, runId = RunId, beganUtc = began, endedUtc = DateTime.UtcNow.ToString("O"),
                frames = _hudProfileCount, overflow = _hudProfileOverflow, startTick = tick, startLogCount = startLogCount,
                before = before, after = after, cameras = cameras, preferences = preferences, initialSlots = initialSlots, markers = _hudProfileDescriptions,
                sourceAuthority = "Coordinator records checked-out source revision alongside the raw receipt; this observer does not infer test-to-commit identity.",
                observation = HudProfileCanVerify + " " + HudProfileCannotVerify,
                sampling = "EOF observer, completed snapshot has explicit UnityFrame. Native LastValue is latest completed recorder value, not guaranteed this snapshot frame. Blank means unavailable, not zero. Markers nest; do not sum as exclusive cost. Setup buffers precede sampling; screenshots/file IO follow sampling. Raw phase rows include real input-handler and diagnostic observation overhead; no diagnostic direct Render calls." }, true));
        }
        struct HudProfileRow { public string phase; public int observerFrame, snapshotFrame, tick, hotbarRenders, sidebarRenders, tilemapClears, selectedSlot, logOffset; public double realtime, wallMs, rendererMs; public bool focused; }
        [Serializable] sealed class HudProfileReport { public string recipe, stage, runId, beganUtc, endedUtc, cameras, preferences, initialSlots, observation, sampling, sourceAuthority; public int frames, startTick, startLogCount; public bool overflow; public MarkerSettings before, after; public MarkerDescription[] markers; }
    }
}
