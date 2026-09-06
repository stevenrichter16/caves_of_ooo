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
    public sealed class GameAuditWeaponUnitBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditWeaponUnitBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldCraft, _oldEnhancements;

        public void Initialize(ScenarioContext ctx, GameAuditWeaponUnitBench bench)
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
            yield return new WaitForSecondsRealtime(0.6f);
            var input = FindFirstObjectByType<InputHandler>(); if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController; if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (boot.IsActive) yield return Tap(Key.N); yield return new WaitForSecondsRealtime(0.3f);
            _bench.Check("native_boot_keeps_arena", !boot.IsActive && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>();
            _bench.Check("native_actual_unspent_preconditions", Quantity(_bench.Blade) == 3 && Quantity(_bench.Haft) == 3 && Quantity(_bench.Binding) == 3
                && Quantity(_bench.Glimmer) == 3 && Quantity(_bench.Spark) == 3 && !inv.Objects.Any(i => i.HasPart<WeaponAssemblyPart>()));
            yield return OpenPanel(input, 4); yield return Tap(Key.B); yield return Tap(Key.C);
            yield return Pick(input, _bench.Glimmer); yield return Pick(input, _bench.Spark);
            _bench.Check("native_brew_batch_available", (int)Read(input.InventoryUI, "_craftBatchMax") == 3);
            yield return Chord(Key.LeftShift, Key.Enter);
            var brew = inv.Objects.Single(i => i.HasPart<BrewItemPart>());
            _bench.Check("native_brew_three_actual_media", Quantity(brew) == 3 && brew.GetPart<BrewItemPart>().GetEffects().Count > 0
                && !inv.Objects.Contains(_bench.Glimmer) && !inv.Objects.Contains(_bench.Spark));
            yield return Tap(Key.F); yield return Tap(Key.C);
            foreach (var component in new[] { _bench.Blade, _bench.Haft, _bench.Binding }) yield return Pick(input, component);
            int before = Count("WeaponForged"); yield return Tap(Key.Enter);
            var plain = inv.Objects.Single(i => i.HasPart<WeaponAssemblyPart>());
            _bench.Check("native_first_plain_weapon", Quantity(plain) == 1 && Temper(plain) == 0 && Count("WeaponForged") == before + 1
                && Quantity(_bench.Blade) == 2 && Quantity(_bench.Haft) == 2 && Quantity(_bench.Binding) == 2);
            yield return Pick(input, brew);
            _bench.Check("native_batch_two_one_quench_selected", (int)Read(input.InventoryUI, "_craftBatchMax") == 2 && Read(input.InventoryUI, "_pickedQuench") == brew);
            before = Count("WeaponForged"); int beforeTemper = Count("WeaponTempered");
            yield return Chord(Key.LeftShift, Key.Enter);
            var tempered = inv.Objects.Single(i => i.HasPart<WeaponAssemblyPart>() && Temper(i) == 1);
            _bench.Check("native_batch_changes_one_unit", Quantity(plain) == 2 && Quantity(tempered) == 1 && Quantity(brew) == 2
                && Count("WeaponForged") == before + 2 && Count("WeaponTempered") == beforeTemper + 1);
            _bench.Check("native_batch_pays_all_components", new[] { _bench.Blade, _bench.Haft, _bench.Binding }.All(i => !inv.Objects.Contains(i)));
            _bench.Check("native_new_temper_has_addressable_id", !string.IsNullOrEmpty(tempered.ID) && tempered.ID != plain.ID);
            yield return Tap(Key.C); yield return Tap(Key.Escape);
            _bench.Check("native_pack_selection_clear", State(input) == "Normal" && !inv.Objects.Any(CraftingMarkPart.IsMarked));
            yield return Station(input); yield return MenuAction(input, "CraftToggle:" + plain.ID); yield return MenuAction(input, "CraftToggle:" + brew.ID);
            beforeTemper = Count("WeaponTempered"); yield return MenuAction(input, "CraftKit");
            _bench.Check("native_station_temper_merges_one", Quantity(plain) == 1 && Quantity(tempered) == 2 && Quantity(brew) == 1
                && Count("WeaponTempered") == beforeTemper + 1 && State(input) == "Normal");
            _bench.Check("native_selection_follows_existing_recipient", CraftingMarkPart.IsMarked(tempered) && !CraftingMarkPart.IsMarked(plain));
            yield return Station(input);
            var rows = Read(input.WorldActionMenuUI, "_actions") as List<InventoryAction>;
            _bench.Check("native_recipient_can_be_selected_again", rows != null && rows.Any(a => a.Command == "CraftToggle:" + tempered.ID));
            yield return MenuAction(input, "CraftToggle:" + brew.ID); yield return MenuAction(input, "CraftToggle:" + _bench.Replacement.ID);
            before = Count("WeaponReforged"); yield return MenuAction(input, "CraftKit");
            var changed = inv.Objects.Single(i => i.GetPart<WeaponAssemblyPart>()?.BladeBlueprint == "IronSpikeComponent");
            _bench.Check("native_reforge_changes_one_tempered_unit", Quantity(changed) == 1 && Temper(changed) == 0 && Quantity(tempered) == 1 && Temper(tempered) == 1
                && Quantity(plain) == 1 && Temper(plain) == 0 && Count("WeaponReforged") == before + 1);
            _bench.Check("native_reforge_exact_payment_and_return", !inv.Objects.Contains(_bench.Replacement) && inv.Objects.Where(i => i.BlueprintName == "SteelBladeComponent").Sum(Quantity) == 1 && Quantity(brew) == 1);
            _bench.Check("native_reforge_fresh_selected_identity", !string.IsNullOrEmpty(changed.ID) && changed.ID != tempered.ID && changed.ID != plain.ID
                && CraftingMarkPart.IsMarked(changed) && !CraftingMarkPart.IsMarked(tempered));
            yield return Station(input); rows = Read(input.WorldActionMenuUI, "_actions") as List<InventoryAction>;
            _bench.Check("native_reforged_clone_addressable", rows != null && rows.Any(a => a.Command == "CraftToggle:" + changed.ID));
            yield return Tap(Key.Escape); _bench.Check("native_final_pack_and_exit", State(input) == "Normal" && inv.GetCarriedWeight() <= inv.MaxWeight
                && inv.Objects.Where(i => i.HasPart<WeaponAssemblyPart>()).Sum(Quantity) == 3);
        }
        private static int Temper(Entity weapon) => weapon.GetPart<WeaponTemperPart>()?.TemperCount ?? 0;
        private IEnumerator Pick(InputHandler input, Entity item)
        {
            var ui = input.InventoryUI; var rows = Read(ui, "_craftRows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (Read(rows[i], "Item") == item) row = i;
            _bench.Check("native_craft_row_" + item.BlueprintName, row >= 0);
            yield return MoveCursor(ui, "_craftCursorIndex", row); yield return Tap(Key.Space);
            _bench.Check("native_craft_mark_" + item.BlueprintName, CraftingMarkPart.IsMarked(item));
        }
        private IEnumerator Station(InputHandler input)
        {
            yield return Tap(Key.C); yield return Tap(Key.RightArrow);
            _bench.Check("native_actual_forge_menu", input.WorldActionMenuUI.SelectedTarget == _bench.Station && input.WorldActionMenuUI.IsOpen);
        }
        private IEnumerator Chord(Key modifier, Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(modifier, key)); yield return new WaitForSecondsRealtime(0.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.12f);
        }
        private IEnumerator MenuAction(InputHandler input, string command)
        {
            var menu = input.WorldActionMenuUI; var actions = Read(menu, "_actions") as List<InventoryAction>; int row = actions?.FindIndex(a => a.Command == command) ?? -1;
            _bench.Check("native_menu_" + command.Split(':')[0], menu.IsOpen && row >= 0); yield return MoveCursor(menu, "_cursorIndex", row); yield return Tap(Key.Enter);
        }
        private int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Kind = kind, Actor = _ctx.PlayerEntity.ID }).Records.Count;
        private static int Quantity(Entity item) => item.GetPart<StackerPart>().StackCount;
        private IEnumerator OpenPanel(InputHandler input, int panel)
        {
            yield return Tap(Key.I);
            for (int i = 0; i < panel; i++) yield return Tap(Key.Tab);
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
            File.WriteAllText(Path.Combine(directory, "GA02h-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditWeaponUnitBench] " + JsonUtility.ToJson(report)); Finished = true;
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
