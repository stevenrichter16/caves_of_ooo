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
    public sealed class GameAuditSeparateOneBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditSeparateOneBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldCraft, _oldEnhancements;

        public void Initialize(ScenarioContext ctx, GameAuditSeparateOneBench bench)
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
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); var source = _bench.Dagger;
            var bits = actor.GetPart<BitLockerPart>(); int weight = inv.GetCarriedWeight(), pen = source.GetPart<MeleeWeaponPart>().PenBonus;
            int ticks = _ctx.Turns.TickCount, energy = _ctx.Turns.GetEnergy(actor); string id = source.ID;
            _bench.Check("native_actual_preconditions", !boot.IsActive && Quantity(source) == 3 && !inv.GetAllEquipped().Any()
                && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1 && !source.HasTag("ModSharp"));
            yield return OpenPanel(input, 1);
            var ui = input.InventoryUI; var rows = Read(ui, "_rows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (Read(Read(rows[i], "Item"), "Item") == source) row = i;
            _bench.Check("native_actual_source_row", row >= 0); yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var oldPopup = Read(ui, "_itemActionPopup"); var actions = Read(oldPopup, "Actions") as IList; int action = -1;
            for (int i = 0; actions != null && i < actions.Count; i++) if ((string)Read(actions[i], "Command") == "separate_one") action = i;
            _bench.Check("native_separate_action_on_exact_source", Read(oldPopup, "Item") == source && action >= 0
                && (string)Read(actions[action], "Label") == "Separate one");
            yield return MoveCursor(oldPopup, "CursorIndex", action); yield return Tap(Key.Enter);
            var unit = inv.Objects.Single(e => e.BlueprintName == "Dagger" && e != source); string unitId = unit.ID;
            var popup = Read(ui, "_itemActionPopup"); var focused = ((IList)Read(ui, "_rows"))[(int)Read(ui, "_cursorIndex")];
            _bench.Check("native_new_singleton_focused_in_row_and_popup", popup != oldPopup && Read(popup, "Item") == unit && Read(Read(focused, "Item"), "Item") == unit);
            _bench.Check("native_one_step_keeps_remainder", inv.Objects.Count == 2 && Quantity(source) == 2 && Quantity(unit) == 1 && source.ID == id
                && Guid.TryParseExact(unitId, "N", out _) && unitId != id && inv.GetCarriedWeight() == weight);
            _bench.Check("native_separation_keeps_owners_equipment_and_time", unit.GetPart<PhysicsPart>().InInventory == actor && source.GetPart<PhysicsPart>().InInventory == actor
                && !inv.GetAllEquipped().Any() && _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(actor) == energy);
            _bench.Check("native_singleton_does_not_offer_separate", !((IList)Read(popup, "Actions")).Cast<object>().Any(a => (string)Read(a, "Command") == "separate_one"));
            yield return Tap(Key.Escape); _bench.Check("native_close_new_popup_keeps_inventory", ui.IsOpen && Read(ui, "_itemActionPopup") == null);
            yield return Tap(Key.Escape); yield return OpenPanel(input, 2); yield return Tap(Key.M);
            var recipes = Read(ui, "_tinkerRows") as IList; row = -1;
            for (int i = 0; recipes != null && i < recipes.Count; i++) if (((TinkerRecipe)Read(recipes[i], "Recipe")).ID == "mod_sharp_melee") row = i;
            _bench.Check("native_real_sharp_recipe", row >= 0); yield return MoveCursor(ui, "_tinkerCursorIndex", row); yield return Tap(Key.Enter);
            var targetPopup = Read(ui, "_modTargetPopup"); var targets = Read(targetPopup, "Targets") as IList;
            _bench.Check("native_only_separated_unit_is_eligible", targets != null && targets.Count == 1 && ReferenceEquals(targets[0], unit) && !targets.Contains(source));
            yield return Tap(Key.Enter);
            _bench.Check("native_sharp_applies_and_pays_once", Read(ui, "_modTargetPopup") == null && unit.HasTag("ModSharp")
                && unit.GetPart<MeleeWeaponPart>().PenBonus == pen + 1 && unit.GetIntProperty("ModificationCount") == 1
                && bits.GetBitCount('B') == 0 && bits.GetBitCount('C') == 0);
            _bench.Check("native_remaining_stack_untouched", Quantity(source) == 2 && !source.HasTag("ModSharp")
                && source.GetPart<MeleeWeaponPart>().PenBonus == pen && source.GetIntProperty("ModificationCount") == 0 && source.ID == id);
            _bench.Check("native_final_mass_identity_and_time", unit.ID == unitId && Quantity(unit) == 1 && inv.Objects.Count == 2
                && inv.GetCarriedWeight() == weight && _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(actor) == energy);
            yield return Tap(Key.Escape); _bench.Check("native_final_normal", State(input) == "Normal" && !ui.IsOpen);
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
            File.WriteAllText(Path.Combine(directory, "FLOW5-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditSeparateOneBench] " + JsonUtility.ToJson(report)); Finished = true;
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
