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
    public sealed class GameAuditSplitIdentityBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditSplitIdentityBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldCraft, _oldEnhancements;

        public void Initialize(ScenarioContext ctx, GameAuditSplitIdentityBench bench)
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
            _bench.Check("native_unspent_preconditions", Quantity(_bench.Dagger) == 2 && Quantity(_bench.Blade) == 2
                && Quantity(_bench.Haft) == 2 && Quantity(_bench.Binding) == 2 && !inv.GetAllEquipped().Any());
            yield return OpenPanel(input, 4); yield return Tap(Key.B); yield return Tap(Key.C);
            yield return Pick(input, _bench.Glimmer); yield return Pick(input, _bench.Spark); yield return Tap(Key.Enter);
            var brew = inv.Objects.Single(i => i.HasPart<BrewItemPart>());
            _bench.Check("native_paid_quench", Quantity(brew) == 1 && !inv.Contains(_bench.Glimmer) && !inv.Contains(_bench.Spark));
            yield return Tap(Key.F); yield return Tap(Key.C);
            foreach (var component in new[] { _bench.Blade, _bench.Haft, _bench.Binding }) yield return Pick(input, component);
            _bench.Check("native_forge_two_available", (int)Read(input.InventoryUI, "_craftBatchMax") == 2);
            yield return Chord(Key.LeftShift, Key.Enter);
            var stock = inv.Objects.Single(i => i.HasPart<WeaponAssemblyPart>()); string stockId = stock.ID;
            _bench.Check("native_paid_forged_pair", Quantity(stock) == 2 && !inv.Contains(_bench.Blade) && !inv.Contains(_bench.Haft) && !inv.Contains(_bench.Binding));
            yield return Tap(Key.C); yield return Tap(Key.Escape);

            yield return OpenPanel(input, 1); yield return ItemAction(input, _bench.Dagger, "equip_auto");
            var dagger = inv.GetAllEquipped().Single(); string daggerId = dagger.ID;
            _bench.Check("native_dagger_equip_creates_addressable_unit", Quantity(_bench.Dagger) == 1 && Quantity(dagger) == 1
                && dagger != _bench.Dagger && Guid.TryParseExact(daggerId, "N", out _) && daggerId != _bench.Dagger.ID);
            yield return ItemAction(input, dagger, "unequip"); yield return Tap(Key.Escape);
            _bench.Check("native_dagger_unequip_retains_exact_unit", inv.Objects.Contains(dagger) && dagger.ID == daggerId && !inv.GetAllEquipped().Any());
            yield return Station(input); yield return MenuAction(input, "CraftToggle:" + daggerId); yield return MenuAction(input, "CraftToggle:" + brew.ID);
            _bench.Check("native_station_selects_split_dagger_only", CraftingMarkPart.IsMarked(dagger) && !CraftingMarkPart.IsMarked(_bench.Dagger));
            int before = Count("WeaponTempered"); yield return MenuAction(input, "CraftKit");
            _bench.Check("native_station_tempers_exact_split", dagger.ID == daggerId && Temper(dagger) == 1 && Temper(_bench.Dagger) == 0
                && Count("WeaponTempered") == before + 1 && !inv.Contains(brew) && Quantity(_bench.Dagger) == 1);
            yield return Station(input); yield return MenuAction(input, "CraftToggle:" + daggerId); yield return Tap(Key.Escape);

            yield return OpenPanel(input, 1); yield return ItemAction(input, stock, "equip_auto");
            var forged = inv.GetAllEquipped().Single(); string forgedId = forged.ID;
            _bench.Check("native_forged_equip_creates_addressable_unit", Quantity(stock) == 1 && Quantity(forged) == 1
                && Guid.TryParseExact(forgedId, "N", out _) && forgedId != stockId && stock.ID == stockId);
            yield return ItemAction(input, forged, "unequip"); yield return Tap(Key.Escape);
            yield return Station(input); yield return MenuAction(input, "CraftToggle:" + forgedId); yield return MenuAction(input, "CraftToggle:" + _bench.Replacement.ID);
            _bench.Check("native_station_selects_split_forged_only", CraftingMarkPart.IsMarked(forged) && !CraftingMarkPart.IsMarked(stock) && !CraftingMarkPart.IsMarked(dagger));
            before = Count("WeaponReforged"); yield return MenuAction(input, "CraftKit");
            _bench.Check("native_station_reforges_exact_split", forged.ID == forgedId && forged.GetPart<WeaponAssemblyPart>().BladeBlueprint == "IronSpikeComponent"
                && stock.GetPart<WeaponAssemblyPart>().BladeBlueprint == "SteelBladeComponent" && stock.ID == stockId && Count("WeaponReforged") == before + 1);
            _bench.Check("native_reforge_pays_and_returns_one", !inv.Contains(_bench.Replacement)
                && inv.Objects.Where(i => i.BlueprintName == "SteelBladeComponent").Sum(Quantity) == 1);
            yield return OpenPanel(input, 1); yield return ItemAction(input, dagger, "drop"); yield return ItemAction(input, forged, "drop"); yield return Tap(Key.Escape);
            _bench.Check("native_both_exact_units_on_ground", _ctx.Zone.GetEntityPosition(dagger) == (20, 12) && _ctx.Zone.GetEntityPosition(forged) == (20, 12)
                && !inv.Contains(dagger) && !inv.Contains(forged) && dagger.ID == daggerId && forged.ID == forgedId);
            yield return Tap(Key.C); yield return Tap(Key.Period); yield return MenuAction(input, WorldInteractionSystem.PickCellCommand);
            yield return MenuAction(input, WorldInteractionSystem.PickTargetCommandPrefix + daggerId);
            _bench.Check("native_individual_picker_resolves_dagger", input.WorldActionMenuUI.SelectedTarget == dagger);
            yield return MenuAction(input, WorldInteractionSystem.PickCellCommand);
            yield return MenuAction(input, WorldInteractionSystem.PickTargetCommandPrefix + forgedId);
            _bench.Check("native_individual_picker_resolves_forged", input.WorldActionMenuUI.SelectedTarget == forged);
            yield return Tap(Key.Escape);
            _bench.Check("native_final_exact_siblings_and_exit", State(input) == "Normal" && Quantity(stock) == 1 && Quantity(_bench.Dagger) == 1
                && stock.ID == stockId && !inv.GetAllEquipped().Any());
        }
        private IEnumerator ItemAction(InputHandler input, Entity item, string command)
        {
            var ui = input.InventoryUI; var rows = Read(ui, "_rows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (Read(Read(rows[i], "Item"), "Item") == item) row = i;
            _bench.Check("native_item_row_" + command, ui.IsOpen && (int)Read(ui, "_panel") == 1 && row >= 0);
            yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_itemActionPopup"); var actions = Read(popup, "Actions") as IList; int action = -1;
            for (int i = 0; actions != null && i < actions.Count; i++) if ((string)Read(actions[i], "Command") == command) action = i;
            _bench.Check("native_item_action_" + command, Read(popup, "Item") == item && action >= 0);
            yield return MoveCursor(popup, "CursorIndex", action); yield return Tap(Key.Enter);
            _bench.Check("native_item_action_completed_" + command, ui.IsOpen && Read(ui, "_itemActionPopup") == null);
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
            File.WriteAllText(Path.Combine(directory, "FLOW4-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditSplitIdentityBench] " + JsonUtility.ToJson(report)); Finished = true;
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
