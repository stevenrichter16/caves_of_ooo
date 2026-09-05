using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Bounded native-keyboard audit. Reflection reads menu state;
    /// gameplay commands are selected only by queued real input-system keys.
    /// Teleports between cases are explicit scenario staging, not test actions.</summary>
    public sealed class FoundingVillageBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private FoundingVillageBench _bench;
        private InputHandler _input;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private readonly List<string> _audit = new List<string>();
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public void Initialize(ScenarioContext ctx, FoundingVillageBench bench)
        {
            _ctx = ctx; _bench = bench;
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.5f);
            SaveGameService.RegisterRuntime(null, null);
            yield return Tap(Key.N);
            yield return new WaitForSecondsRealtime(1);
            _input = UnityEngine.Object.FindFirstObjectByType<InputHandler>();
            Require(_input != null && _input.CurrentZone == _ctx.Zone, "live input zone");
            SettlementRuntime.ActiveZone = _ctx.Zone;
            int tick = _ctx.Turns.TickCount, energy = _ctx.Turns.GetEnergy(_ctx.PlayerEntity);
            yield return OpenPlume(); yield return Tap(Key.S);
            Check("untrusted_sleep_refused", _ctx.Turns.TickCount == tick && _ctx.PlayerEntity.GetStatValue("Hitpoints") == 5 && NarrativeStatePart.Current.GetFact("RootedMet") == 0);

            var tender = _ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "FoundingPlaqueTender");
            var p = _ctx.Zone.GetEntityPosition(tender); Stage(p.x - 1, p.y);
            yield return Tap(Key.C); yield return Tap(Key.D); yield return SelectCommand("Chat");
            Require(ConversationManager.IsActive, "tender conversation");
            yield return Tap(Key.Space); // completes the actual streamed text
            int offer = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferFoundingStone"));
            Require(offer >= 0 && offer < 26, "visible stone service");
            yield return Tap((Key)((int)Key.A + offer));
            Check("one_stone_earns_trust", PlayerReputation.Get("CatacombFolk") == 50 && NarrativeStatePart.Current.GetFact("FoundingStoneOffered") == 1 && Units() == 1);
            Check("service_is_once_only", !FoundingTrustService.CanOffer(_ctx.PlayerEntity, tender));
            yield return Tap(Key.Escape);

            Stage(57, 12); tick = _ctx.Turns.TickCount;
            yield return OpenPlume(); yield return Tap(Key.S);
            Check("trusted_rest_exact_sixty", _ctx.Turns.TickCount == tick + 60 && _ctx.PlayerEntity.GetStatValue("Hitpoints") == 40
                && _ctx.Turns.GetEnergy(_ctx.PlayerEntity) == energy && _ctx.Turns.WaitingForInput
                && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (57, 12));
            Check("meeting_and_patch_bloom", NarrativeStatePart.Current.GetFact("RootedMet") == 1 && FoundingPlumePart.HasBloom(_ctx.PlayerEntity));
            tick = _ctx.Turns.TickCount;
            yield return OpenPlume(); yield return Tap(Key.S);
            Check("repeat_rest_no_duplicate_meeting", _ctx.Turns.TickCount == tick + 60 && NarrativeStatePart.Current.EventLog.Count(s => s == "RootedMet") == 1);
            tick = _ctx.Turns.TickCount;
            yield return OpenPlume(); yield return Tap(Key.Escape);
            Check("cancel_spends_no_time", _ctx.Turns.TickCount == tick && State() == "Normal");
            yield return OpenPlume(); Stage(56, 12); yield return Tap(Key.S);
            Check("stale_reach_refused", _ctx.Turns.TickCount == tick && State() == "Normal");
            Stage(57, 12);
            yield return new WaitForSecondsRealtime(.3f);
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/FellingW6");
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "W64-native-chamber.png"));
            yield return new WaitForSecondsRealtime(.5f);
            var report = new Report { runId = _bench.RunId, cases = _audit.Count, failures = Failures, tickCount = _ctx.Turns.TickCount,
                energy = _ctx.Turns.GetEnergy(_ctx.PlayerEntity), audit = _audit.ToArray() };
            File.WriteAllText(Path.Combine(directory, "W64-native-audit.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[FoundingVillageBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private IEnumerator OpenPlume()
        {
            yield return Tap(Key.C); yield return Tap(Key.Period);
            Require(State() == "WorldActionMenuOpen", "underfoot menu opened by c/self");
            var plume = _ctx.Zone.GetCell(57, 12).Objects.Single(e => e.BlueprintName == "FoundingPlume");
            yield return SelectCommand(WorldInteractionSystem.PickTargetCommandPrefix + plume.ID);
            Require(Actions().Any(a => a.Command == FoundingPlumePart.SleepCommand), "actual plume sleep row");
        }
        private IEnumerator SelectCommand(string command)
        {
            var rows = Actions(); int index = rows.FindIndex(a => a.Command == command);
            Require(index >= 0, "menu command " + command);
            int cursor = (int)typeof(WorldActionMenuUI).GetField("_cursorIndex", Private).GetValue(_input.WorldActionMenuUI);
            for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
            for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);
        }
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return new WaitForSecondsRealtime(.09f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(.09f);
        }
        private void Stage(int x, int y) { _ctx.Zone.MoveEntity(_ctx.PlayerEntity, x, y); ZoneRenderHooks.MarkFullDirty("FoundingAuditStage"); }
        private int Units() => _ctx.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "Tepuibone").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        private string State() => typeof(InputHandler).GetField("_inputState", Private).GetValue(_input).ToString();
        private List<InventoryAction> Actions() => (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(_input.WorldActionMenuUI);
        private void Check(string name, bool passed)
        {
            if (!passed) Failures++; _audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            Debug.Log("[FoundingVillageBench] " + _bench.RunId + " " + _audit[_audit.Count - 1]);
            Diag.Record("scenario", "FoundingNativeAudit", payload: new { runId = _bench.RunId, name, passed });
        }
        private void Require(bool value, string context)
        {
            if (value) return;
            Failures++; Finished = true; throw new InvalidOperationException("Founding native audit precondition: " + context);
        }
        private void OnDestroy()
        {
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public int cases, failures, tickCount, energy; public string[] audit; }
    }
}
