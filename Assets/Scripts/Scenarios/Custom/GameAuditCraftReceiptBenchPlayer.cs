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
    public sealed class GameAuditCraftReceiptBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditCraftReceiptBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldEvent, _oldAlchemy;

        public void Initialize(ScenarioContext ctx, GameAuditCraftReceiptBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            _oldEvent = Diag.IsChannelEnabled("event"); Diag.SetChannel("event", true);
            _oldAlchemy = Diag.IsChannelEnabled("alchemy"); Diag.SetChannel("alchemy", true);
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
            CheckServiceRecipients();
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); var bits = actor.GetPart<BitLockerPart>();
            int ticks = _ctx.Turns.TickCount, energy = _ctx.Turns.GetEnergy(actor);
            _bench.Check("native_actual_recipe_and_capacity_preconditions", !boot.IsActive && Quantity(_bench.Dagger) == 1 && Quantity(_bench.Good) == 2
                && inv.GetCarriedWeight() == 6 && inv.MaxWeight == 16 && bits.GetBitCount('B') == 3 && bits.GetBitCount('C') == 3);
            yield return OpenPanel(input, 4); yield return Tap(Key.B); var ui = input.InventoryUI;
            _bench.Check("native_brew_preview", ((BrewPreview)Read(ui, "_brewPreview")).IsValid && (int)Read(ui, "_craftBatchMax") == 2);
            yield return Tap(Key.Enter);
            var tonic = inv.Objects.Single(e => e.BlueprintName == "BrewedTonic"); string tonicId = tonic.ID;
            _bench.Check("native_first_brew_appended_and_paid", Quantity(tonic) == 1 && Quantity(_bench.Good) == 1 && ReferenceEquals(tonic.GetPart<PhysicsPart>().InInventory, actor));
            yield return Tap(Key.Enter);
            _bench.Check("native_second_brew_keeps_actual_resident", inv.Objects.Count(e => e.BlueprintName == "BrewedTonic") == 1 && inv.Objects.Contains(tonic)
                && Quantity(tonic) == 2 && tonic.ID == tonicId && !inv.Objects.Contains(_bench.Good) && inv.GetCarriedWeight() == 6);
            yield return Tap(Key.Escape); yield return OpenBuild(input);
            string daggerId = _bench.Dagger.ID;
            yield return Tap(Key.Enter);
            _bench.Check("native_first_build_merges_and_pays_once", Quantity(_bench.Dagger) == 2 && inv.Objects.Contains(_bench.Dagger) && bits.GetBitCount('B') == 2 && bits.GetBitCount('C') == 2 && inv.GetCarriedWeight() == 10);
            yield return Tap(Key.Enter);
            _bench.Check("native_second_build_keeps_exact_dagger", Quantity(_bench.Dagger) == 3 && _bench.Dagger.ID == daggerId && inv.Objects.Count(e => e.BlueprintName == "Dagger") == 1
                && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1 && inv.GetCarriedWeight() == 14);
            var entries = inv.Objects.ToArray(); int speed = actor.GetStat("Speed").Penalty;
            yield return Tap(Key.Enter);
            _bench.Check("native_capacity_refusal_restores_items_bits_and_selection", !string.IsNullOrEmpty((string)Read(ui, "_actionStatus"))
                && entries.SequenceEqual(inv.Objects) && Quantity(_bench.Dagger) == 3 && Quantity(tonic) == 2 && tonic.ID == tonicId && _bench.Dagger.ID == daggerId
                && entries.All(e => ReferenceEquals(e.GetPart<PhysicsPart>().InInventory, actor)) && actor.GetStat("Speed").Penalty == speed
                && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1 && inv.GetCarriedWeight() == 14 && Read(ui, "_tinkeringMode").ToString() == "Build");
            _bench.Check("native_crafting_and_refusal_keep_time", _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(actor) == energy);
            yield return Tap(Key.Escape); yield return DropCarried(input, tonic);
            _bench.Check("native_drop_frees_exact_tonic_stack", !inv.Objects.Contains(tonic) && Quantity(tonic) == 2 && inv.GetCarriedWeight() == 12
                && _ctx.Zone.GetEntityPosition(tonic) == _ctx.Zone.GetEntityPosition(actor) && tonic.GetPart<PhysicsPart>().InInventory == null);
            yield return OpenBuild(input); yield return Tap(Key.Enter);
            _bench.Check("native_capacity_retry_succeeds_into_original_dagger", Quantity(_bench.Dagger) == 4 && _bench.Dagger.ID == daggerId && inv.Objects.Count == 1
                && ReferenceEquals(inv.Objects[0], _bench.Dagger) && inv.GetCarriedWeight() == 16 && bits.GetBitCount('B') == 0 && bits.GetBitCount('C') == 0
                && string.IsNullOrEmpty((string)Read(ui, "_actionStatus")));
            yield return Tap(Key.Escape); _bench.Check("native_final_normal", State(input) == "Normal" && !ui.IsOpen);
        }
        // Separate direct-service evidence: the keyboard UI discards service return values.
        private void CheckServiceRecipients()
        {
            var actor = _ctx.Factory.CreateEntity("Player"); var inv = actor.GetPart<InventoryPart>(); inv.MaxWeight = -1;
            var reagent = _ctx.Factory.CreateEntity("GlimmerBrine"); reagent.GetPart<StackerPart>().StackCount = 2;
            _bench.Check("service_reagent_staged", inv.AddObject(reagent));
            _bench.Check("service_brew_append_returns_resident", BrewingService.TryBrew(actor, _ctx.Factory, new[] { reagent }, out var first, out _, out _) && inv.Objects.Contains(first) && Quantity(first) == 1);
            _bench.Check("service_brew_merge_returns_same_resident", BrewingService.TryBrew(actor, _ctx.Factory, new[] { reagent }, out var second, out _, out _) && ReferenceEquals(first, second) && Quantity(second) == 2 && inv.Objects.Contains(second));
            var dagger = _ctx.Factory.CreateEntity("Dagger"); _bench.Check("service_dagger_staged", inv.AddObject(dagger));
            var bits = actor.GetPart<BitLockerPart>(); if (bits == null) { bits = new BitLockerPart(); actor.AddPart(bits); }
            bits.RestoreBitsAndRecipes(new Dictionary<char, int> { ['B'] = 2, ['C'] = 2 }, new[] { "craft_dagger" });
            for (int i = 0; i < 2; i++)
                _bench.Check("service_build_returns_exact_resident_" + i, TinkeringService.TryCraft(actor, _ctx.Factory, "craft_dagger", out var made, out _) && made.Count == 1
                    && ReferenceEquals(made[0], dagger) && inv.Objects.Contains(made[0]) && Quantity(dagger) == i + 2);
        }
        private IEnumerator OpenBuild(InputHandler input)
        {
            yield return OpenPanel(input, 2); yield return Tap(Key.B); var ui = input.InventoryUI; var rows = Read(ui, "_tinkerRows") as IList; int row = -1;
            for (int i = 0; i < rows.Count; i++) if (((TinkerRecipe)Read(rows[i], "Recipe")).ID == "craft_dagger") row = i;
            _bench.Check("native_build_recipe_available", row >= 0); yield return MoveCursor(ui, "_tinkerCursorIndex", row);
        }
        private IEnumerator DropCarried(InputHandler input, Entity item)
        {
            yield return OpenPanel(input, 1); var ui = input.InventoryUI; var rows = Read(ui, "_rows") as IList; int row = -1;
            for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Read(Read(rows[i], "Item"), "Item"), item)) row = i;
            _bench.Check("native_exact_tonic_row", row >= 0); yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_itemActionPopup"); var actions = Read(popup, "Actions") as IList; int action = -1;
            for (int i = 0; i < actions.Count; i++) if ((string)Read(actions[i], "Command") == "drop") action = i;
            _bench.Check("native_drop_action_available", action >= 0 && ReferenceEquals(Read(popup, "Item"), item));
            yield return MoveCursor(popup, "CursorIndex", action); yield return Tap(Key.Enter); yield return Tap(Key.Escape);
        }
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
            File.WriteAllText(Path.Combine(directory, "GA02k-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditCraftReceiptBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            Diag.SetChannel("event", _oldEvent);
            Diag.SetChannel("alchemy", _oldAlchemy);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
