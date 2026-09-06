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
    public sealed class GameAuditPositivePaymentBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditPositivePaymentBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private readonly Dictionary<string, bool> _channels = new Dictionary<string, bool>();

        public void Initialize(ScenarioContext ctx, GameAuditPositivePaymentBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            foreach (string channel in new[] { "crop", "event", "mineral-trade", "furniture" })
            { _channels[channel] = Diag.IsChannelEnabled(channel); Diag.SetChannel(channel, true); }
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(AuditInputs()));
        }
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
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>();
            _bench.Check("native_actual_start", Quantity(_bench.Seed) == 2 && Quantity(_bench.Salt) == 1 && Quantity(_bench.Stone) == 2 && inv.GetAllEquipped().Count == 0 && _ctx.Turns.WaitingForInput && _ctx.Turns.CurrentActor == actor);
            int ticks = _ctx.Turns.TickCount, energy = _ctx.Turns.GetEnergy(actor), planted = Count("CropPlanted"), rejected = Count("PlantRejected"), paid = Count("ItemUnitConsumed");
            yield return CarriedAction(input, _bench.Seed, "PlantSeed");
            _bench.Check("native_hard_floor_refuses_without_payment", Quantity(_bench.Seed) == 2 && Count("PlantRejected") == rejected + 1
                && Count("CropPlanted") == planted && Count("ItemUnitConsumed") == paid && !_ctx.Zone.GetCell(20, 12).HasObjectWithPart<CropPart>());
            CheckClock("native_floor_refusal_clock", ticks, energy);
            yield return Tap(Key.DownArrow);
            _bench.Check("native_walk_to_plantable_control", _ctx.Zone.GetEntityPosition(actor) == (20, 13) && State(input) == "Normal");
            ticks = _ctx.Turns.TickCount; energy = _ctx.Turns.GetEnergy(actor);
            yield return CarriedAction(input, _bench.Seed, "PlantSeed");
            var crop = _ctx.Zone.GetCell(20, 13).Objects.SingleOrDefault(e => e.GetPart<CropPart>() != null);
            _bench.Check("native_one_seed_paid_for_one_crop", Quantity(_bench.Seed) == 1 && crop?.BlueprintName == "CandyCarrotCrop"
                && Count("CropPlanted") == planted + 1 && Count("ItemUnitConsumed") == paid + 1);
            CheckClock("native_successful_plant_clock", ticks, energy);
            yield return CarriedAction(input, _bench.Seed, "PlantSeed");
            _bench.Check("native_occupied_refusal_preserves_crop_and_seed", Quantity(_bench.Seed) == 1
                && _ctx.Zone.GetCell(20, 13).Objects.Single(e => e.GetPart<CropPart>() != null) == crop
                && Count("PlantRejected") == rejected + 2 && Count("CropPlanted") == planted + 1 && Count("ItemUnitConsumed") == paid + 1);
            CheckClock("native_occupied_refusal_clock", ticks, energy);
            int trades = Count("Traded");
            yield return Chat(input, _bench.SaltMaster, Key.RightArrow, "salt");
            yield return Choose(input, "Weighed", "SellMineral", "PaleSalt");
            _bench.Check("native_last_salt_paid_for_actual_standing", !inv.Contains(_bench.Salt) && PlayerReputation.Get("TentRight") == 5 && Count("Traded") == trades + 1);
            yield return Choose(input, "Start");
            _bench.Check("native_spent_salt_row_disappears", !HasAction("SellMineral"));
            yield return Tap(Key.Escape); yield return DrainAnnouncements(input);
            CheckClock("native_salt_dialogue_clock", ticks, energy);
            yield return Chat(input, _bench.Tender, Key.LeftArrow, "founding");
            _bench.Check("native_founding_offer_initially_available", HasAction("OfferFoundingStone"));
            yield return Choose(input, "Start", "OfferFoundingStone");
            _bench.Check("native_one_stone_paid_and_once_fact_published", Quantity(_bench.Stone) == 1 && PlayerReputation.Get("CatacombFolk") == 50
                && NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact) == 1 && Count("Traded") == trades + 2);
            _bench.Check("native_spare_stone_cannot_repeat", !HasAction("OfferFoundingStone") && ConversationManager.VisibleChoices.Any(c => c.Target == "Kept"));
            yield return Choose(input, "Kept");
            yield return Tap(Key.Escape); yield return DrainAnnouncements(input);
            yield return Chat(input, _bench.Tender, Key.LeftArrow, "founding_repeat");
            _bench.Check("native_reopened_offer_stays_closed", !HasAction("OfferFoundingStone") && Quantity(_bench.Stone) == 1 && PlayerReputation.Get("CatacombFolk") == 50);
            yield return Tap(Key.Escape); yield return DrainAnnouncements(input);
            CheckClock("native_founding_dialogue_clock", ticks, energy);
            _bench.Check("native_final_normal_state", State(input) == "Normal" && !ConversationManager.IsActive);
        }
        private static bool HasAction(string action) => ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == action));
        private void CheckClock(string name, int ticks, int energy) => _bench.Check(name, _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(_ctx.PlayerEntity) == energy);
        private int Count(string kind) => DiagQuery.Count(new DiagQuery.Filter { Kind = kind, Actor = _ctx.PlayerEntity.ID }).Count;
        private static int Quantity(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 0;
        private IEnumerator Chat(InputHandler input, Entity npc, Key direction, string label)
        {
            yield return Tap(Key.C); yield return Tap(direction);
            var menu = input.WorldActionMenuUI;
            var actions = Read(menu, "_actions") as List<InventoryAction>;
            int index = actions?.FindIndex(a => a.Command == "Chat") ?? -1;
            _bench.Check(label + "_native_chat_menu", menu.IsOpen && menu.SelectedTarget == npc && index >= 0);
            yield return MoveCursor(menu, "_cursorIndex", index);
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
            yield return MoveCursor(ui, "_cursorIndex", index);
            yield return Tap(Key.Enter);
            _bench.Check("native_choice_" + origin + "_to_" + (expected ?? target), ConversationManager.CurrentNode?.ID == (expected ?? target));
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
        private IEnumerator CarriedAction(InputHandler input, Entity item, string command)
        {
            yield return Tap(Key.I); yield return Tap(Key.Tab);
            var ui = input.InventoryUI; var rows = Read(ui, "_rows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (Read(Read(rows[i], "Item"), "Item") == item) row = i;
            _bench.Check("native_carried_inventory_row", ui.IsOpen && row >= 0 && !(bool)Read(Read(rows[row], "Item"), "IsEquipped"));
            yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_itemActionPopup"); var actions = Read(popup, "Actions") as IList; int action = -1;
            for (int i = 0; actions != null && i < actions.Count; i++) if ((string)Read(actions[i], "Command") == command) action = i;
            _bench.Check("native_inventory_action_" + command, Read(popup, "Item") == item && action >= 0);
            yield return MoveCursor(popup, "CursorIndex", action); yield return Tap(Key.Enter);
            _bench.Check("native_inventory_action_closed", Read(ui, "_itemActionPopup") == null && ui.IsOpen);
            yield return Tap(Key.Escape);
            _bench.Check("native_inventory_exit", State(input) == "Normal");
        }
        private IEnumerator MoveCursor(object ui, string field, int row)
        {
            for (int i = 0; i < 100; i++)
            {
                int cursor = (int)Read(ui, field); if (cursor == row) yield break;
                yield return Tap(cursor < row ? Key.DownArrow : Key.UpArrow);
                if ((int)Read(ui, field) == cursor) throw new InvalidOperationException("Native cursor did not move toward intended row.");
            }
            throw new InvalidOperationException("Native cursor did not reach intended row.");
        }
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
            File.WriteAllText(Path.Combine(directory, "GA02i-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditPositivePaymentBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            foreach (var channel in _channels) Diag.SetChannel(channel.Key, channel.Value);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
