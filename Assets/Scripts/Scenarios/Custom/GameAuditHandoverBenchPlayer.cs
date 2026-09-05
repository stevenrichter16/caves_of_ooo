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
    /// <summary>Temporary native keyboard driver. Reflection observes UI state; it never selects actions.</summary>
    public sealed class GameAuditHandoverBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditHandoverBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private SettlementManager _manager;
        private Action _oldDirty, _auditDirty;
        private int _changes, _dirty;
        private bool _oldEvents;

        public void Initialize(ScenarioContext ctx, GameAuditHandoverBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            _oldEvents = Diag.IsChannelEnabled("event"); Diag.SetChannel("event", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _manager = SettlementManager.Current; _manager.SiteStateChanged += OnSiteChanged;
            _oldDirty = SettlementRuntime.ZoneDirtyCallback;
            _auditDirty = () => { _dirty++; _oldDirty?.Invoke(); }; SettlementRuntime.ZoneDirtyCallback = _auditDirty;
            StartCoroutine(RunSafely(AuditInputs()));
        }
        private void OnSiteChanged(string settlement, RepairableSiteState site) { _changes++; }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            // Flatten nested audit helpers so an assertion in any helper writes a failed report immediately.
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; }
                if (failure != null) { Failures++; Debug.LogError(failure); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }
        private IEnumerator AuditInputs()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            var input = FindFirstObjectByType<InputHandler>();
            if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController;
            if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (boot.IsActive) yield return Tap(Key.N);
            yield return new WaitForSecondsRealtime(0.3f);
            _bench.Check("native_boot_keeps_arena", !boot.IsActive && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            var inv = _ctx.PlayerEntity.GetPart<InventoryPart>();
            _bench.Check("full_pack_copy_precondition", inv.GetCarriedWeight() == inv.MaxWeight
                && inv.Objects.Contains(_bench.Original) && !inv.Objects.Any(e => e.HasTag("GrimoireCopy")));
            yield return Chat(input, _bench.Scribe, Key.RightArrow, "scribe");
            yield return Choose(input, "CopyConfirm");
            yield return Choose(input, "AfterCopy", "CopyGrimoire");
            var copies = _ctx.Zone.GetAllEntities().Where(e => e.BlueprintName == "GrimoireCopy").ToList();
            _bench.Check("native_copy_delivered_at_feet", copies.Count == 1
                && _ctx.Zone.GetEntityPosition(copies[0]) == (20, 12)
                && !inv.Objects.Any(e => e.HasTag("GrimoireCopy")));
            _bench.Check("native_copy_preserves_original_and_knowledge", inv.Objects.Contains(_bench.Original)
                && _bench.Original.GetPart<GrimoirePart>().KnowledgeProperty == "KnowsMendingRite"
                && copies[0].GetPart<GrimoirePart>().KnowledgeProperty == "KnowsMendingRite");
            _bench.Check("native_copy_notice_queued", MessageLog.HasPendingAnnouncement && !input.AnnouncementUI.IsOpen);
            yield return Tap(Key.Escape);
            yield return DrainAnnouncements(input);
            _bench.Check("native_scribe_exit", State(input) == "Normal" && !ConversationManager.IsActive);
            yield return Chat(input, _bench.Farmer, Key.DownArrow, "farmer");
            yield return Choose(input, "OvenRebuild");
            yield return Choose(input, "AfterOvenRebuild", "ResolveSettlementSite", "VillageOven:OvenRebuild");
            var site = _manager.GetSite(_ctx.Zone.ZoneID, "VillageOven");
            _bench.Check("native_repair_spends_one", site.Stage == RepairStage.StableRepair
                && _bench.Clay.GetPart<StackerPart>().StackCount == 2 && inv.Objects.Contains(_bench.Guide)
                && _changes == 1 && _dirty == 1);
            yield return Choose(input, "Start", "ChangeFactionFeeling", "Villagers:Player:10");
            _bench.Check("native_first_reward", PlayerReputation.Get("Villagers") == 10);
            var before = site.Clone(); int changes = _changes, dirty = _dirty;
            yield return Choose(input, "OvenRebuild");
            int rejected = RepairRefusals();
            yield return Choose(input, "AfterOvenRebuild", "ResolveSettlementSite", "VillageOven:OvenRebuild", "OvenRebuild");
            _bench.Check("native_repeat_rejection_dispatched", RepairRefusals() == rejected + 1);
            _bench.Check("native_repeat_refuses_without_payment_or_reward", SameSite(before, site)
                && _bench.Clay.GetPart<StackerPart>().StackCount == 2 && inv.Objects.Contains(_bench.Guide)
                && PlayerReputation.Get("Villagers") == 10 && _changes == changes && _dirty == dirty);
            yield return Choose(input, "Start");
            yield return Tap(Key.Escape);
            yield return DrainAnnouncements(input);
            _bench.Check("native_refusal_remains_cancellable", State(input) == "Normal" && !ConversationManager.IsActive);
        }
        private IEnumerator Chat(InputHandler input, Entity npc, Key direction, string label)
        {
            yield return Tap(Key.C); yield return Tap(direction);
            var menu = input.WorldActionMenuUI;
            var actions = Read(menu, "_actions") as List<InventoryAction>;
            int index = actions?.FindIndex(a => a.Command == "Chat") ?? -1;
            _bench.Check(label + "_native_chat_menu", menu.IsOpen && menu.SelectedTarget == npc && index >= 0);
            yield return MoveCursor(menu, index);
            yield return Tap(Key.Enter);
            _bench.Check(label + "_native_conversation", input.DialogueUI.IsOpen && ConversationManager.Speaker == npc
                && ConversationManager.CurrentNode?.ID == "Start");
        }
        private IEnumerator Choose(InputHandler input, string target, string action = null, string argument = null, string expected = null)
        {
            yield return DrainAnnouncements(input);
            var ui = input.DialogueUI; string origin = ConversationManager.CurrentNode?.ID;
            if ((bool)Read(ui, "_revealing"))
            {
                yield return Tap(Key.Enter);
                _bench.Check("reveal_only_" + origin, !(bool)Read(ui, "_revealing") && ConversationManager.CurrentNode?.ID == origin);
            }
            int index = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == target
                && (action == null || c.Actions != null && c.Actions.Any(a => a.Key == action && (argument == null || a.Value == argument))));
            if (index < 0) throw new InvalidOperationException("Authored native choice missing: " + origin + " -> " + target);
            yield return MoveCursor(ui, index);
            yield return Tap(Key.Enter);
            _bench.Check("native_choice_" + origin + "_to_" + (expected ?? target), ConversationManager.CurrentNode?.ID == (expected ?? target));
        }
        private IEnumerator MoveCursor(object menu, int index)
        {
            int cursor = (int)Read(menu, "_cursorIndex");
            for (int i = cursor; i < index; i++) yield return Tap(Key.DownArrow);
            for (int i = cursor; i > index; i--) yield return Tap(Key.UpArrow);
            if ((int)Read(menu, "_cursorIndex") != index) throw new InvalidOperationException("Native cursor did not reach the intended row.");
        }
        private IEnumerator DrainAnnouncements(InputHandler input)
        {
            // Dialogue deliberately queues notices until CloseDialogue. Keep navigating
            // the actual conversation; drain only after its native Escape/End route.
            if (input.DialogueUI.IsOpen) yield break;
            for (int i = 0; i < 8 && (input.AnnouncementUI.IsOpen || MessageLog.HasPendingAnnouncement); i++)
            {
                yield return new WaitForSecondsRealtime(0.12f);
                if (input.AnnouncementUI.IsOpen) yield return Tap(Key.Enter);
            }
            if (input.AnnouncementUI.IsOpen || MessageLog.HasPendingAnnouncement)
                throw new InvalidOperationException("Native announcement queue did not drain.");
        }
        private static bool SameSite(RepairableSiteState a, RepairableSiteState b) =>
            a.SiteId == b.SiteId && a.SiteType == b.SiteType && a.ProblemType == b.ProblemType && a.Stage == b.Stage
            && a.Severity == b.Severity && a.ResolvedByMethod == b.ResolvedByMethod && a.ResolvedAtTurn == b.ResolvedAtTurn
            && a.RelapseAtTurn == b.RelapseAtTurn && a.OutcomeTier == b.OutcomeTier;
        private int RepairRefusals() => DiagQuery.Apply(new DiagQuery.Filter
            { Kind = "ConversationActionRejected", Actor = _ctx.PlayerEntity.ID, Target = _bench.Farmer.ID, Limit = 500 })
            .Records.Count(e => e.PayloadJson != null && e.PayloadJson.Contains("ResolveSettlementSite")
                && e.PayloadJson.Contains("VillageOven:OvenRebuild") && e.PayloadJson.Contains("repair_refused"));
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(0.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.12f);
        }
        private static object Read(object owner, string field) => owner?.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private void Finish()
        {
            Failures = Math.Max(Failures, _bench.Failures);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                cases = _bench.Cases, failures = Failures, audit = _bench.Audit.ToArray() };
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit"); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "GA02b-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditHandoverBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            Diag.SetChannel("event", _oldEvents);
            if (_manager != null) _manager.SiteStateChanged -= OnSiteChanged;
            if (SettlementRuntime.ZoneDirtyCallback == _auditDirty) SettlementRuntime.ZoneDirtyCallback = _oldDirty;
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
