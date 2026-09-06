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
    public sealed class GameAuditCraftAvailabilityBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditCraftAvailabilityBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldCraft, _oldEnhancements;

        public void Initialize(ScenarioContext ctx, GameAuditCraftAvailabilityBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            _oldCraft = Diag.IsChannelEnabled("craft"); Diag.SetChannel("craft", true);
            _oldEnhancements = Diag.IsChannelEnabled("enhancement"); Diag.SetChannel("enhancement", true);
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
            yield return new WaitForSecondsRealtime(.6f);
            var input = FindFirstObjectByType<InputHandler>(); if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController; if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (boot.IsActive) yield return Tap(Key.N); yield return new WaitForSecondsRealtime(.3f);
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); int ticks = _ctx.Turns.TickCount, energy = _ctx.Turns.GetEnergy(actor);
            _bench.Check("native_actual_stale_mix_preconditions", !boot.IsActive && Quantity(_bench.Good) == 2 && Quantity(_bench.Stale) == 0
                && Quantity(_bench.EmptySalt) == 0 && Quantity(_bench.PositiveSalt) == 2 && inv.Objects.IndexOf(_bench.EmptySalt) < inv.Objects.IndexOf(_bench.PositiveSalt));
            yield return OpenPanel(input, 4); yield return Tap(Key.B); var ui = input.InventoryUI;
            var picked = Read(ui, "_pickedReagents") as IList; var rows = Read(ui, "_craftRows") as IList;
            _bench.Check("native_full_invalid_mix_retained_and_preview_refused", picked.Count == 2 && picked.Contains(_bench.Good) && picked.Contains(_bench.Stale)
                && !((BrewPreview)Read(ui, "_brewPreview")).IsValid && (int)Read(ui, "_craftBatchMax") == 0);
            int row = FindRow(rows, _bench.Stale); _bench.Check("native_stale_pick_visible_for_cleanup", row >= 0 && (bool)Read(rows[row], "IsSelectable") && ((string)Read(rows[row], "Text")).Contains("(empty; remove pick)"));
            yield return Tap(Key.Enter);
            _bench.Check("native_invalid_mix_refuses_without_payment_or_discovery", !string.IsNullOrEmpty((string)Read(ui, "_actionStatus"))
                && Quantity(_bench.Good) == 2 && Quantity(_bench.Stale) == 0 && !inv.Objects.Any(e => e.BlueprintName == "BrewedTonic")
                && (actor.GetPart<BrewKnowledgePart>()?.GetDiscoveredRules().Count ?? 0) == 0);
            yield return MoveCursor(ui, "_craftCursorIndex", row); yield return Tap(Key.Space);
            picked = Read(ui, "_pickedReagents") as IList; rows = Read(ui, "_craftRows") as IList;
            _bench.Check("native_cleanup_removes_only_stale_pick_and_row", !CraftingMarkPart.IsMarked(_bench.Stale) && CraftingMarkPart.IsMarked(_bench.Good)
                && FindRow(rows, _bench.Stale) < 0 && picked.Count == 1 && ReferenceEquals(picked[0], _bench.Good));
            _bench.Check("native_remaining_mix_preview_recovers", ((BrewPreview)Read(ui, "_brewPreview")).IsValid && (int)Read(ui, "_craftBatchMax") == 2);
            yield return Tap(Key.Enter);
            _bench.Check("native_remaining_mix_pays_one_and_creates_brew", Quantity(_bench.Good) == 1 && Quantity(_bench.Stale) == 0
                && inv.Objects.Count(e => e.BlueprintName == "BrewedTonic") == 1 && string.IsNullOrEmpty((string)Read(ui, "_actionStatus")));
            _bench.Check("native_brew_discovery_and_unrelated_picks", (actor.GetPart<BrewKnowledgePart>()?.GetDiscoveredRules().Count ?? 0) > 0
                && Quantity(_bench.EmptySalt) == 0 && Quantity(_bench.PositiveSalt) == 2);
            yield return Tap(Key.Escape); yield return OpenPanel(input, 2); yield return Tap(Key.M);
            var recipes = Read(ui, "_tinkerRows") as IList; row = -1;
            for (int i = 0; i < recipes.Count; i++) if (((TinkerRecipe)Read(recipes[i], "Recipe")).ID == "mod_palesalt_infuse") row = i;
            _bench.Check("native_infusion_available_from_later_positive_mineral", row >= 0 && (bool)Read(recipes[row], "HasIngredient"));
            yield return MoveCursor(ui, "_tinkerCursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_modTargetPopup"); var targets = Read(popup, "Targets") as IList;
            _bench.Check("native_exact_dagger_is_infusion_target", targets != null && targets.Count == 1 && ReferenceEquals(targets[0], _bench.Dagger));
            yield return Tap(Key.Enter);
            _bench.Check("native_infusion_pays_positive_sibling_and_retains_empty", _bench.Dagger.GetPart<EnhancementPaleSalt>()?.Tier == 2
                && Quantity(_bench.PositiveSalt) == 1 && Quantity(_bench.EmptySalt) == 0 && inv.Objects.Contains(_bench.EmptySalt));
            var bits = actor.GetPart<BitLockerPart>();
            _bench.Check("native_infusion_no_extra_bits_and_all_actions_keep_time", bits.GetBitCount('B') == 0 && bits.GetBitCount('C') == 0
                && Read(ui, "_modTargetPopup") == null && _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(actor) == energy);
            yield return Tap(Key.Escape); _bench.Check("native_final_normal", State(input) == "Normal" && !ui.IsOpen);
        }
        private static int FindRow(IList rows, Entity item)
        { for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Read(rows[i], "Item"), item)) return i; return -1; }
        private static int Quantity(Entity item) => item.GetPart<StackerPart>().StackCount;
        private IEnumerator OpenPanel(InputHandler input, int panel)
        {
            yield return Tap(Key.I);
            for (int i = 0; i < 5 && (int)Read(input.InventoryUI, "_panel") != panel; i++) yield return Tap(Key.Tab);
            _bench.Check("native_inventory_panel_" + panel, input.InventoryUI.IsOpen && (int)Read(input.InventoryUI, "_panel") == panel);
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
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.12f);
        }
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private void Finish()
        {
            Failures = Math.Max(Failures, _bench.Failures);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                cases = _bench.Cases, failures = Failures, audit = _bench.Audit.ToArray() };
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit"); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "GA02j-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditCraftAvailabilityBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            Diag.SetChannel("craft", _oldCraft);
            Diag.SetChannel("enhancement", _oldEnhancements);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
