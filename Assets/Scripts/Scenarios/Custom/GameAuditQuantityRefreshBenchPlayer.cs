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
    public sealed class GameAuditQuantityRefreshBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditQuantityRefreshBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldEvents, _oldAlchemy, _oldEnhancements;
        private int _initialCarryRecords;

        public void Initialize(ScenarioContext ctx, GameAuditQuantityRefreshBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            _initialCarryRecords = DiagQuery.Apply(new DiagQuery.Filter { Kind = "CarryPenaltyRefreshed", Actor = ctx.PlayerEntity.ID }).Records.Count;
            _oldEvents = Diag.IsChannelEnabled("event"); Diag.SetChannel("event", true);
            _oldAlchemy = Diag.IsChannelEnabled("alchemy"); Diag.SetChannel("alchemy", true);
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
            var input = FindFirstObjectByType<InputHandler>();
            if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController;
            if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (boot.IsActive) yield return Tap(Key.N);
            yield return new WaitForSecondsRealtime(0.3f);
            _bench.Check("native_boot_keeps_arena", !boot.IsActive && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>();
            _bench.Check("native_configured_start", Quantity(_bench.Seed) == 3 && Quantity(_bench.Salt) == 3
                && Quantity(_bench.Dagger) == 3 && Quantity(_bench.Moss) == 3 && inv.GetAllEquipped().Count == 0);
            CheckPenalty("native_start", 55, 0);
            yield return CarriedAction(input, _bench.Seed, "PlantSeed");
            _bench.Check("native_seed_spent_and_crop_placed", Quantity(_bench.Seed) == 2 && _ctx.Zone.GetCell(20, 12).HasObjectWithPart<CropPart>());
            CheckPenalty("native_after_plant", 51, 1);
            yield return Tap(Key.DownArrow);
            _bench.Check("native_walk_with_updated_speed", _ctx.Zone.GetEntityPosition(actor) == (20, 13) && State(input) == "Normal");
            CheckPenalty("native_movement_does_not_double_refresh", 51, 1);
            yield return Infuse(input, _bench.Weapon); CheckPenalty("native_after_paid_infusion", 47, 2);
            int bitsBefore = actor.GetPart<BitLockerPart>().GetBitCount('B');
            yield return CarriedAction(input, _bench.Dagger, "disassemble");
            _bench.Check("native_disassembly_spends_one_and_pays_bits", Quantity(_bench.Dagger) == 2
                && actor.GetPart<BitLockerPart>().GetBitCount('B') == bitsBefore + 1);
            CheckPenalty("native_after_disassembly", 43, 3);
            yield return Brew(input, _bench.Moss);
            _bench.Check("native_mishap_has_no_output", Quantity(_bench.Moss) == 2
                && !inv.Objects.Any(i => i.BlueprintName == "BrewedTonic" || i.BlueprintName == "InertSludge")
                && Count("BrewResolved") == 1);
            CheckPenalty("native_after_mishap", 39, 4);
            yield return Tap(Key.DownArrow);
            _bench.Check("native_final_walk", _ctx.Zone.GetEntityPosition(actor) == (20, 14) && State(input) == "Normal");
            CheckPenalty("native_final_stable_speed", 39, 4);
        }
        private void CheckPenalty(string name, int expected, int changes)
        {
            var actor = _ctx.PlayerEntity;
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "CarryPenaltyRefreshed", Actor = actor.ID }).Records.Skip(_initialCarryRecords).ToArray();
            _bench.Check(name, actor.GetStat("Speed").Penalty == expected && actor.GetStatValue("Speed") == 100 - expected
                && records.Length == changes && records.All(r => r.PayloadJson.Contains("\"delta\":-4")));
        }
        private IEnumerator Brew(InputHandler input, params Entity[] ingredients)
        {
            int before = Count("BrewResolved"); var quantities = ingredients.Select(Quantity).ToArray();
            yield return OpenPanel(input, 4); yield return Tap(Key.B); yield return Tap(Key.C);
            var ui = input.InventoryUI;
            foreach (var ingredient in ingredients)
            {
                var rows = Read(ui, "_craftRows") as IList; int row = -1;
                for (int i = 0; rows != null && i < rows.Count; i++) if (Read(rows[i], "Item") == ingredient) row = i;
                _bench.Check("native_brew_reagent_row", row >= 0);
                yield return MoveCursor(ui, "_craftCursorIndex", row); yield return Tap(Key.Space);
                _bench.Check("native_brew_reagent_marked", CraftingMarkPart.IsMarked(ingredient));
            }
            yield return Tap(Key.Enter);
            _bench.Check("native_inventory_blocks_unstable_preview", Count("BrewResolved") == before && ui.IsOpen
                && ingredients.Select((item, i) => Quantity(item) == quantities[i]).All(v => v));
            CheckPenalty("native_preview_refusal_preserves_penalty", 43, 3);
            yield return Tap(Key.Escape);
            _bench.Check("native_crafting_exit", State(input) == "Normal");
            yield return Tap(Key.UpArrow);
            _bench.Check("native_return_beside_still", _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            yield return Tap(Key.C); yield return Tap(Key.RightArrow);
            var menu = input.WorldActionMenuUI;
            _bench.Check("native_actual_still_menu", menu.IsOpen && menu.SelectedTarget?.GetPart<AlchemyStillPart>() != null);
            var actions = Read(menu, "_actions") as List<InventoryAction>;
            int rowIndex = actions?.FindIndex(a => a.Command == "BrewMix") ?? -1;
            _bench.Check("native_still_brew_action", rowIndex >= 0);
            int hp = _ctx.PlayerEntity.GetStatValue("Hitpoints");
            yield return MoveCursor(menu, "_cursorIndex", rowIndex); yield return Tap(Key.Enter);
            _bench.Check("native_still_commits_mishap", Count("BrewResolved") == before + 1 && State(input) == "Normal"
                && ingredients.Select((item, i) => Quantity(item) == quantities[i] - 1).All(v => v)
                && _ctx.PlayerEntity.GetStatValue("Hitpoints") == hp - 2);
            yield return Tap(Key.DownArrow);
            _bench.Check("native_leave_still", _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 13));
        }
        private IEnumerator Infuse(InputHandler input, Entity item)
        {
            int before = Upgrades(item); int salt = Quantity(_bench.Salt); int applied = Count("Applied", item.ID);
            yield return OpenPanel(input, 2); yield return Tap(Key.M);
            var ui = input.InventoryUI; var rows = Read(ui, "_tinkerRows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if ((string)Read(Read(rows[i], "Recipe"), "ID") == "mod_palesalt_infuse") row = i;
            _bench.Check("native_paid_recipe_row", row >= 0);
            yield return MoveCursor(ui, "_tinkerCursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_modTargetPopup"); var targets = Read(popup, "Targets") as IList; int target = -1;
            for (int i = 0; targets != null && i < targets.Count; i++) if (ReferenceEquals(targets[i], item)) target = i;
            _bench.Check("native_upgrade_target", target >= 0);
            yield return MoveCursor(popup, "CursorIndex", target); yield return Tap(Key.Enter);
            _bench.Check("native_mineral_paid_and_upgrade_applied", Read(ui, "_modTargetPopup") == null && Upgrades(item) == before + 1
                && item.Parts.OfType<EnhancementPaleSalt>().All(p => p.Tier == 2 && p.BonusDamage == 4)
                && (salt > 1 ? Quantity(_bench.Salt) == salt - 1 : !_ctx.PlayerEntity.GetPart<InventoryPart>().Contains(_bench.Salt))
                && Count("Applied", item.ID) == applied + 1);
            yield return Tap(Key.Escape); _bench.Check("native_tinkering_exit", State(input) == "Normal");
        }
        private IEnumerator OpenPanel(InputHandler input, int panel)
        {
            yield return Tap(Key.I);
            for (int i = 0; i < panel; i++) yield return Tap(Key.Tab);
            _bench.Check("native_inventory_panel_" + panel, input.InventoryUI.IsOpen && (int)Read(input.InventoryUI, "_panel") == panel);
        }
        private int Count(string kind, string target = null, string payload = null) => DiagQuery.Apply(new DiagQuery.Filter
            { Kind = kind, Actor = kind == "Applied" ? null : _ctx.PlayerEntity.ID, Target = target, Limit = 500 }).Records.Count(e =>
                (payload == null || (e.PayloadJson != null && e.PayloadJson.Contains(payload)))
                && (kind != "TonicApplied" || (e.PayloadJson != null && e.PayloadJson.Contains("\"consumed\":true"))));
        private static int Quantity(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 0;
        private static int Upgrades(Entity item) => item.Parts.OfType<EnhancementPaleSalt>().Count();
        private static string ItemName(Entity item) => item.GetPart<RenderPart>().DisplayName;
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
            File.WriteAllText(Path.Combine(directory, "GA02f-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditQuantityRefreshBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            Diag.SetChannel("event", _oldEvents);
            Diag.SetChannel("alchemy", _oldAlchemy);
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
