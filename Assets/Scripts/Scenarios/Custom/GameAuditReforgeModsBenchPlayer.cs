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
    public sealed class GameAuditReforgeModsBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditReforgeModsBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldCraft, _oldEnhancements;

        public void Initialize(ScenarioContext ctx, GameAuditReforgeModsBench bench)
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
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); var bits = actor.GetPart<BitLockerPart>();
            _bench.Check("native_actual_unspent_preconditions", Quantity(_bench.Haft) == 2 && Quantity(_bench.Blade) == 1 && Quantity(_bench.Binding) == 1
                && bits.GetBitCount('B') == 2 && bits.GetBitCount('C') == 2 && !inv.Objects.Any(i => i.HasPart<WeaponAssemblyPart>()));
            yield return Forge(input);
            var weapon = inv.Objects.Single(i => i.HasPart<WeaponAssemblyPart>()); int basePen = weapon.GetPart<MeleeWeaponPart>().PenBonus;
            _bench.Check("native_forged_component_base", !weapon.HasTag("ModSharp") && basePen == 0 && Quantity(weapon) == 1);
            yield return Modify(input, weapon, "mod_sharp_melee");
            _bench.Check("native_paid_sharp", weapon.HasTag("ModSharp") && weapon.GetPart<MeleeWeaponPart>().PenBonus == basePen + 1
                && weapon.GetIntProperty("ModificationCount") == 1 && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1);
            yield return Modify(input, weapon, "mod_palesalt_infuse"); var mineral = weapon.GetPart<EnhancementPaleSalt>();
            _bench.Check("native_paid_mineral", mineral != null && mineral.Tier == 2 && mineral.BonusDamage == 4 && !inv.Contains(_bench.Salt)
                && weapon.GetIntProperty("ModificationCount") == 2 && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1);
            yield return ReforgeAtStation(input, weapon, _bench.Haft, true);
            CheckPermanent(weapon, mineral, basePen, "native_first_reforge_keeps_paid_state");
            string firstName = weapon.GetPart<RenderPart>().DisplayName;
            var spare = inv.Objects.Single(i => i.BlueprintName == "OakHaftComponent");
            yield return ReforgeAtStation(input, weapon, spare, false);
            CheckPermanent(weapon, mineral, basePen, "native_second_reforge_does_not_accumulate");
            _bench.Check("native_canonical_name_stable", weapon.GetPart<RenderPart>().DisplayName == firstName);
            yield return OpenPanel(input, 2); yield return Tap(Key.M); yield return SelectRecipe(input, "mod_sharp_melee");
            var popup = Read(input.InventoryUI, "_modTargetPopup"); var targets = Read(popup, "Targets") as IList;
            _bench.Check("native_sharp_filter_keeps_clean_control", targets != null && targets.Cast<Entity>().Contains(_bench.Control) && !targets.Cast<Entity>().Contains(weapon));
            yield return Tap(Key.Escape); yield return Tap(Key.Escape);
            _bench.Check("native_filter_exits_without_payment", State(input) == "Normal" && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1);
            CheckPermanent(weapon, mineral, basePen, "native_final_permanent_state");
        }
        private void CheckPermanent(Entity weapon, EnhancementPaleSalt mineral, int basePen, string label)
        {
            var bits = _ctx.PlayerEntity.GetPart<BitLockerPart>(); string name = weapon.GetPart<RenderPart>().DisplayName;
            _bench.Check(label, weapon.GetPart<MeleeWeaponPart>().PenBonus == basePen + 1 && weapon.HasTag("ModSharp")
                && weapon.GetIntProperty("ModificationCount") == 2 && weapon.GetPart<EnhancementPaleSalt>() == mineral
                && mineral.Tier == 2 && mineral.BonusDamage == 4 && name.Split(' ').Count(s => s == "sharp") == 1
                && name.Split(' ').Count(s => s == "pale-salt-edged") == 1 && Quantity(weapon) == 1
                && bits.GetBitCount('B') == 1 && bits.GetBitCount('C') == 1);
        }
        private IEnumerator Forge(InputHandler input)
        {
            int before = Count("WeaponForged"); yield return OpenPanel(input, 4); yield return Tap(Key.F); yield return Tap(Key.C);
            var ui = input.InventoryUI;
            foreach (var item in new[] { _bench.Blade, _bench.Haft, _bench.Binding })
            {
                var rows = Read(ui, "_craftRows") as IList; int row = -1;
                for (int i = 0; rows != null && i < rows.Count; i++) if (Read(rows[i], "Item") == item) row = i;
                _bench.Check("native_component_row", row >= 0); yield return MoveCursor(ui, "_craftCursorIndex", row); yield return Tap(Key.Space);
                _bench.Check("native_component_marked", CraftingMarkPart.IsMarked(item));
            }
            yield return Tap(Key.Enter);
            var inv = _ctx.PlayerEntity.GetPart<InventoryPart>();
            _bench.Check("native_forge_spends_actual_components", Count("WeaponForged") == before + 1 && inv.Objects.Count(i => i.HasPart<WeaponAssemblyPart>()) == 1
                && !inv.Contains(_bench.Blade) && !inv.Contains(_bench.Binding) && Quantity(_bench.Haft) == 1);
            yield return Tap(Key.C);
            _bench.Check("native_clear_leftover_component_marks", !inv.Objects.Any(CraftingMarkPart.IsMarked));
            yield return Tap(Key.Escape); _bench.Check("native_forge_panel_exit", State(input) == "Normal");
        }
        private IEnumerator Modify(InputHandler input, Entity item, string recipe)
        {
            yield return OpenPanel(input, 2); yield return Tap(Key.M); yield return SelectRecipe(input, recipe);
            var ui = input.InventoryUI; var popup = Read(ui, "_modTargetPopup"); var targets = Read(popup, "Targets") as IList; int row = -1;
            for (int i = 0; targets != null && i < targets.Count; i++) if (ReferenceEquals(targets[i], item)) row = i;
            _bench.Check("native_mod_target_" + recipe, row >= 0);
            yield return MoveCursor(popup, "CursorIndex", row); yield return Tap(Key.Enter);
            _bench.Check("native_mod_commits_" + recipe, Read(ui, "_modTargetPopup") == null);
            yield return Tap(Key.Escape); _bench.Check("native_mod_exit", State(input) == "Normal");
        }
        private IEnumerator SelectRecipe(InputHandler input, string recipe)
        {
            var ui = input.InventoryUI; var rows = Read(ui, "_tinkerRows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if ((string)Read(Read(rows[i], "Recipe"), "ID") == recipe) row = i;
            _bench.Check("native_recipe_" + recipe, row >= 0); yield return MoveCursor(ui, "_tinkerCursorIndex", row); yield return Tap(Key.Enter);
        }
        private IEnumerator ReforgeAtStation(InputHandler input, Entity weapon, Entity component, bool markWeapon)
        {
            int before = Count("WeaponReforged"); yield return Tap(Key.C); yield return Tap(Key.RightArrow);
            _bench.Check("native_actual_forge_menu", input.WorldActionMenuUI.SelectedTarget == _bench.Station && input.WorldActionMenuUI.IsOpen);
            if (markWeapon) yield return MenuAction(input, "CraftToggle:" + weapon.ID);
            yield return MenuAction(input, "CraftToggle:" + component.ID);
            _bench.Check("native_station_selection", CraftingMarkPart.IsMarked(weapon) && CraftingMarkPart.IsMarked(component));
            yield return MenuAction(input, "CraftKit"); var inv = _ctx.PlayerEntity.GetPart<InventoryPart>();
            _bench.Check("native_station_reforge_commits", Count("WeaponReforged") == before + 1 && State(input) == "Normal" && inv.Objects.Contains(weapon)
                && !inv.Objects.Contains(component) && inv.Objects.Where(i => i.BlueprintName == "OakHaftComponent").Sum(Quantity) == 1);
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
            File.WriteAllText(Path.Combine(directory, "GA02g-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditReforgeModsBench] " + JsonUtility.ToJson(report)); Finished = true;
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
