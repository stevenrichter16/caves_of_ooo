using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Queued-key mechanical audit. Reflection only reads UI state. Stopwatch
    /// remains usable during teardown; no timing, allocation, speedup or visual-feel claim.</summary>
    public sealed class GameAuditEquipmentLifecycleBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => (_bench == null ? 0 : _bench.Failures) + _fatalFailures + _unexpectedErrors;
        private ScenarioContext _ctx;
        private GameAuditEquipmentLifecycleBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldDev, _oldEnhancementChannel;
        private int _fatalFailures, _unexpectedErrors, _healthyTick, _healthyEnergy;
        private string _root, _markerID, _freshID, _bucklerID, _daggerID, _bootsID, _trapID;
        private System.Diagnostics.Stopwatch _clock;
        private Report _report;
        private readonly List<Snapshot> _snapshots = new List<Snapshot>();
        private string DirectoryPath => Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit");
        private double Now => _clock?.Elapsed.TotalSeconds ?? 0;
        public void Initialize(ScenarioContext ctx, GameAuditEquipmentLifecycleBench bench)
        {
            _ctx = ctx; _bench = bench; _clock = System.Diagnostics.Stopwatch.StartNew();
            _root = SaveGameService.SaveRootOverride; _markerID = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _bucklerID = bench.Buckler.ID; _daggerID = bench.Dagger.ID; _bootsID = bench.Boots.ID; _trapID = bench.Trap.ID;
            _oldDev = DevMode.Enabled; // Enable only after boot N; no development bootstrap grants are requested.
            _oldEnhancementChannel = Diag.IsChannelEnabled("enhancement"); Diag.SetChannel("enhancement", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings; _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>(); StartCoroutine(RunSafely(Audit()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; } catch (Exception ex) { error = ex; }
                if (error != null) { if (_bench.Failures == 0) _fatalFailures++; Debug.LogError(error); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Queue(); Finish();
        }
        private IEnumerator Audit()
        {
            yield return new WaitForSecondsRealtime(.7f);
            var input = FindFirstObjectByType<InputHandler>(); if (input == null) throw new InvalidOperationException("Missing native InputHandler.");
            var boot = Read(input, "_bootMenuController") as BootMenuController;
            _bench.Check("real_boot_marker_menu", boot != null && boot.IsActive);
            yield return Tap(Key.N); yield return new WaitForSecondsRealtime(.2f);
            _freshID = SaveGameService.GetSaveInfo("Quick")?.GameID;
            _bench.Check("fresh_owned_checkpoint_and_arena", !boot.IsActive && State(input) == "Normal" && _freshID != null && _freshID != _markerID
                && ReferenceEquals(input.PlayerEntity, _ctx.PlayerEntity) && input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (20, 12) && SaveGameService.HasQuickSave());
            DevMode.Enabled = true;
            var actor = input.PlayerEntity; var inventory = actor.GetPart<InventoryPart>();
            var buckler = FindItem(input, _bucklerID); var dagger = FindItem(input, _daggerID); var boots = FindItem(input, _bootsID);
            _bench.Check("actual_three_carried_unique_singletons", actor.HasTag("Player") && !actor.HasTag("NoDropOnDeath") && !actor.HasTag("Temporary")
                && inventory.Objects.Count == 3 && inventory.EquippedItems.Count == 0
                && new[] { buckler, dagger, boots }.All(e => Quantity(e) == 1 && !string.IsNullOrWhiteSpace(e.ID) && e.GetPart<PhysicsPart>().InInventory == actor && e.GetPart<PhysicsPart>().Equipped == null)
                && new[] { _bucklerID, _daggerID, _bootsID, _trapID }.Distinct().Count() == 4);
            _bench.Check("actual_content_and_unapplied_positive_controls", buckler.BlueprintName == "Buckler" && buckler.HasTag("ModDuelistCut")
                && buckler.GetPart<EquippablePart>().Slot == "Hand" && buckler.GetPart<ArmorPart>().AV == 0
                && dagger.BlueprintName == "Dagger" && dagger.GetPart<EnhancementGlowQuartz>().RadiusBonus == 2 && !dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus
                && (dagger.GetPart<LightSourcePart>()?.Radius ?? 0) == 0 && boots.BlueprintName == "IronshodBoots"
                && boots.GetPart<ArmorPart>().SpeedPenalty == 5 && actor.GetStat("Agility").Bonus == 0 && actor.GetStat("Speed").Penalty == 7);
            AssertTrap(input, "actual_trap_preflight");
            Record(input, "carried_before_equip");
            yield return Equip(input, buckler, "Hand", Laterality.LEFT, "duelist_left_hand");
            _bench.Check("duelist_positive_agility", actor.GetStat("Agility").Bonus == 2 && actor.GetStatValue("Agility") == 18);
            yield return Equip(input, dagger, "Hand", Laterality.RIGHT, "glow_right_hand");
            _bench.Check("glow_positive_applied_radius", dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 2);
            yield return Equip(input, boots, "Feet", 0, "actual_boots_feet");
            AssertHealthy(input, "three_equipped_positive_controls"); Record(input, "healthy_equipped");
            yield return SaveWithReceipt(input, "healthy_checkpoint");
            byte[] healthyBytes = File.ReadAllBytes(OwnedQuickPath());
            _healthyTick = input.TurnManager.TickCount; _healthyEnergy = input.TurnManager.GetEnergy(actor);
            var oldActor = actor; yield return Tap(Key.F6); AssertLoaded(input, oldActor, _healthyTick, _healthyEnergy, "healthy_load");
            AssertHealthy(input, "loaded_applied_bonuses"); Record(input, "healthy_loaded");

            actor = input.PlayerEntity; buckler = FindItem(input, _bucklerID); dagger = FindItem(input, _daggerID); boots = FindItem(input, _bootsID);
            var probe = new GameAuditEquipmentVetoPart { VetoDismember = true }; actor.AddPart(probe);
            var left = NextAppendage(actor); _bench.Check("F8_actual_first_candidate_left_arm", left.Type == "Arm" && left.GetLaterality() == Laterality.LEFT);
            string vetoBefore = JsonUtility.ToJson(Capture(input, ""));
            yield return Tap(Key.F8);
            _bench.Check("F8_veto_reaches_exact_before_event", probe.BeforeCount == 1 && probe.LastPartID == left.ID && probe.AfterCount == 0);
            _bench.Check("F8_veto_preserves_all_observed_state", JsonUtility.ToJson(Capture(input, "")) == vetoBefore);
            probe.VetoDismember = false;
            int version = EquipmentChangeBus.GlobalVersion; int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(actor);
            yield return Tap(Key.F8);
            AssertDetached(input, buckler, 20, 12, "left_duelist_drop");
            _bench.Check("left_arm_removed_once_and_bonus_refunded", left.ParentPart == null && actor.GetPart<Body>().DismemberedParts.Count(d => d.Part == left) == 1
                && actor.GetStat("Agility").Bonus == 0 && actor.GetStatValue("Agility") == 16 && buckler.HasTag("ModDuelistCut") && buckler.GetPart<ArmorPart>().AV == 0);
            _bench.Check("left_forced_observer_and_notification", probe.BeforeCount == 2 && probe.AfterCount == 1 && probe.CountFor(buckler) == 1 && probe.ObservedCleared
                && EquipmentChangeBus.GlobalVersion > version && input.TurnManager.TickCount == tick && input.TurnManager.GetEnergy(actor) == energy);
            AssertEquipped(input, dagger, "Hand", Laterality.RIGHT, "untargeted_glow_survives_left"); AssertEquipped(input, boots, "Feet", 0, "untargeted_boots_survive_left");
            _bench.Check("untargeted_contributions_survive_left", dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 2 && actor.GetStat("Speed").Penalty == 12);
            actor.RemovePart(probe); Record(input, "left_arm_dropped");
            yield return SaveWithReceipt(input, "injured_checkpoint");
            oldActor = actor; tick = input.TurnManager.TickCount; energy = input.TurnManager.GetEnergy(actor); yield return Tap(Key.F6); AssertLoaded(input, oldActor, tick, energy, "injured_load");
            actor = input.PlayerEntity; buckler = FindItem(input, _bucklerID); dagger = FindItem(input, _daggerID); boots = FindItem(input, _bootsID);
            AssertDetached(input, buckler, 20, 12, "loaded_ground_item_has_no_ghost_equipment");
            AssertEquipped(input, dagger, "Hand", Laterality.RIGHT, "loaded_untargeted_dagger"); AssertEquipped(input, boots, "Feet", 0, "loaded_untargeted_boots");
            _bench.Check("injured_load_exact_persisted_contributions", actor.GetStat("Agility").Bonus == 0 && actor.GetStat("Speed").Penalty == 12
                && dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 2);
            Record(input, "injured_loaded");

            probe = new GameAuditEquipmentVetoPart(); actor.AddPart(probe);
            var right = NextAppendage(actor); _bench.Check("F8_next_candidate_right_arm", right.Type == "Arm" && right.GetLaterality() == Laterality.RIGHT);
            version = EquipmentChangeBus.GlobalVersion; int removed = EnhancementCount("BonusRemoved", actor, dagger);
            yield return Tap(Key.F8); AssertDetached(input, dagger, 20, 12, "right_glow_drop");
            _bench.Check("glow_applied_delta_removed_once", !dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 0
                && dagger.GetPart<EnhancementGlowQuartz>().RadiusBonus == 2 && EnhancementCount("BonusRemoved", actor, dagger) == removed + 1
                && probe.CountFor(dagger) == 1 && probe.ObservedCleared && EquipmentChangeBus.GlobalVersion > version);
            AssertEquipped(input, boots, "Feet", 0, "boots_survive_both_arms"); Record(input, "right_arm_dropped");
            var feet = NextAppendage(actor); _bench.Check("F8_next_candidate_feet", feet.Type == "Feet" && feet.Mobility == 2);
            version = EquipmentChangeBus.GlobalVersion; yield return Tap(Key.F8); AssertDetached(input, boots, 20, 12, "feet_boots_drop");
            _bench.Check("boots_refund_preserves_unrelated_and_new_mobility", actor.GetPart<Body>().CalculateMobilityPenalty() == 60
                && actor.GetIntProperty("MobilityPenalty") == 60 && actor.GetStat("Speed").Penalty == 67 && actor.GetStatValue("Speed") == 33);
            _bench.Check("all_forced_cache_and_observer_cleanup", actor.GetPart<InventoryPart>().EquippedItems.Count == 0 && actor.GetPart<InventoryPart>().Objects.Count == 0
                && probe.AfterCount == 2 && probe.CountFor(boots) == 1 && probe.ObservedCleared && EquipmentChangeBus.GlobalVersion > version);
            actor.RemovePart(probe); Record(input, "feet_dropped");

            File.WriteAllBytes(OwnedQuickPath(), healthyBytes); oldActor = actor; yield return Tap(Key.F6); AssertLoaded(input, oldActor, _healthyTick, _healthyEnergy, "healthy_restore_for_trap");
            AssertHealthy(input, "trap_survival_equipment_precondition"); AssertTrap(input, "trap_survival_content_precondition");
            actor = input.PlayerEntity; actor.GetStat("Hitpoints").BaseValue = 13; // Explicit HP fixture, actual authored trap damage stays 12.
            int survivalVersion = EquipmentChangeBus.GlobalVersion; var trap = FindTrap(input);
            yield return Tap(Key.RightArrow);
            _bench.Check("ordinary_trap_step_survives_thirteen_hp", input.CurrentZone.GetEntityPosition(actor) == (21, 12) && actor.GetStatValue("Hitpoints") == 1
                && input.CurrentZone.GetEntityCell(trap) == null && !DeathActive(input) && State(input) == "Normal");
            AssertHealthy(input, "surviving_trap_retains_equipment");
            _bench.Check("surviving_trap_no_equipment_change", EquipmentChangeBus.GlobalVersion == survivalVersion); Record(input, "trap_survival");
            oldActor = actor; yield return Tap(Key.F6); AssertLoaded(input, oldActor, _healthyTick, _healthyEnergy, "healthy_restore_for_death");
            AssertHealthy(input, "trap_death_equipment_precondition"); AssertTrap(input, "trap_death_content_precondition");
            actor = input.PlayerEntity; buckler = FindItem(input, _bucklerID); dagger = FindItem(input, _daggerID); boots = FindItem(input, _bootsID); trap = FindTrap(input);
            probe = new GameAuditEquipmentVetoPart(); actor.AddPart(probe); actor.GetStat("Hitpoints").BaseValue = 1;
            version = EquipmentChangeBus.GlobalVersion; removed = EnhancementCount("BonusRemoved", actor, dagger);
            yield return Tap(Key.RightArrow);
            _bench.Check("ordinary_trap_step_causes_real_player_death", actor.GetStatValue("Hitpoints") <= 0 && DeathActive(input)
                && input.CurrentZone.GetEntityCell(actor) == null && input.CurrentZone.GetEntityCell(trap) == null);
            AssertDetached(input, buckler, 21, 12, "death_duelist_drop"); AssertDetached(input, dagger, 21, 12, "death_glow_drop"); AssertDetached(input, boots, 21, 12, "death_boots_drop");
            _bench.Check("death_refunds_bonuses_without_limb_penalty", actor.GetStat("Agility").Bonus == 0 && actor.GetStat("Speed").Penalty == 7
                && actor.GetIntProperty("MobilityPenalty") == 0 && !dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 0);
            _bench.Check("death_all_three_observers_and_glow_receipt_once", probe.AfterCount == 3 && probe.CountFor(buckler) == 1 && probe.CountFor(dagger) == 1 && probe.CountFor(boots) == 1
                && probe.ObservedCleared && EquipmentChangeBus.GlobalVersion > version && EnhancementCount("BonusRemoved", actor, dagger) == removed + 1);
            Record(input, "actual_trap_death");
            oldActor = actor; yield return Tap(Key.F6); _bench.Check("death_modal_suppresses_normal_F6", ReferenceEquals(input.PlayerEntity, oldActor) && DeathActive(input));
            yield return Tap(Key.L); AssertLoaded(input, oldActor, _healthyTick, _healthyEnergy, "death_screen_L_recovery", expectsLoadMessage: false);
            _bench.Check("death_screen_closes_after_real_load", !DeathActive(input) && input.PlayerEntity.GetStatValue("Hitpoints") == 100);
            AssertHealthy(input, "L_recovery_healthy_equipment_and_hooks"); AssertTrap(input, "L_recovery_original_trap"); Record(input, "healthy_after_death_load");
            _bench.Check("all_saves_stay_disposable", SaveGameService.SaveRootOverride == _root && _freshID != _markerID
                && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2
                && File.Exists(Path.Combine(_root, _markerID, "Quick.sav.gz")) && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID))
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _markerID)));
        }
        private IEnumerator Equip(InputHandler input, Entity item, string type, int laterality, string label)
        {
            var actor = input.PlayerEntity; var part = actor.GetPart<Body>().GetParts().Single(p => p.Type == type && p.GetLaterality() == laterality);
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(actor); _bench.Check(label + "_slot_starts_empty", part._Equipped == null);
            yield return Tap(Key.I);
            for (int i = 0; i < 5 && (int)Read(input.InventoryUI, "_panel") != 1; i++) yield return Tap(Key.Tab);
            var ui = input.InventoryUI; _bench.Check(label + "_inventory_panel", ui.IsOpen && (int)Read(ui, "_panel") == 1);
            var rows = Read(ui, "_rows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (ReferenceEquals(Read(Read(rows[i], "Item"), "Item"), item)) row = i;
            _bench.Check(label + "_exact_item_row", row >= 0); yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_itemActionPopup"); var actions = Read(popup, "Actions") as IList; int action = -1;
            for (int i = 0; actions != null && i < actions.Count; i++) if ((string)Read(actions[i], "Command") == "equip_manual") action = i;
            _bench.Check(label + "_real_manual_equip_action", action >= 0 && ReferenceEquals(Read(popup, "Item"), item));
            yield return MoveCursor(popup, "CursorIndex", action); yield return Tap(Key.Enter);
            var parts = Read(popup, "BodyParts") as IList; int slot = parts == null ? -1 : parts.IndexOf(part);
            _bench.Check(label + "_real_body_part_picker", ReferenceEquals(Read(ui, "_itemActionPopup"), popup) && (bool)Read(popup, "InBodyPartPicker") && slot >= 0);
            yield return MoveCursor(popup, "BodyPartCursor", slot); yield return Tap(Key.Enter);
            _bench.Check(label + "_success_closes_popup", Read(ui, "_itemActionPopup") == null && Read(ui, "_displaceConfirm") == null && ui.IsOpen);
            AssertEquipped(input, item, type, laterality, label + "_exact_equipment_links");
            yield return Tap(Key.Escape); _bench.Check(label + "_returns_normal_without_time", State(input) == "Normal" && !ui.IsOpen && input.TurnManager.TickCount == tick && input.TurnManager.GetEnergy(actor) == energy);
        }
        private IEnumerator MoveCursor(object owner, string field, int target)
        {
            for (int i = 0; i < 100; i++)
            { int cursor = (int)Read(owner, field); if (cursor == target) yield break; yield return Tap(cursor < target ? Key.DownArrow : Key.UpArrow); if ((int)Read(owner, field) == cursor) throw new InvalidOperationException("Native cursor did not move: " + field); }
            throw new InvalidOperationException("Native cursor did not reach intended row: " + field);
        }
        private IEnumerator SaveWithReceipt(InputHandler input, string label)
        {
            _bench.Check(label + "_normal_without_fixture_probe", State(input) == "Normal" && !input.PlayerEntity.HasPart<GameAuditEquipmentVetoPart>());
            string path = OwnedQuickPath(); byte[] before = File.Exists(path) ? File.ReadAllBytes(path) : null; int serial = MessageLog.NextSerialValue;
            yield return Tap(Key.F5);
            _bench.Check(label + "_new_success_message", MessageLog.GetLast() == "Game saved." && MessageLog.NextSerialValue > serial
                && SaveGameService.HasQuickSave() && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID);
            _bench.Check(label + "_changed_owned_payload", File.Exists(path) && (before == null || !before.SequenceEqual(File.ReadAllBytes(path))));
        }
        private void AssertLoaded(InputHandler input, Entity oldActor, int tick, int energy, string label, bool expectsLoadMessage = true)
        {
            _bench.Check(label + "_fresh_graph_and_saved_clock", !ReferenceEquals(input.PlayerEntity, oldActor) && ReferenceEquals(input.TurnManager, TurnManager.Active)
                && ReferenceEquals(input.TurnManager.CurrentActor, input.PlayerEntity) && input.TurnManager.TickCount == tick && input.TurnManager.GetEnergy(input.PlayerEntity) == energy
                && State(input) == "Normal" && input.CurrentZone.GetEntityCell(input.PlayerEntity) != null);
            // F6 adds a success message; the death controller restores history
            // without adding that receipt. Both must restore the exact graph.
            if (expectsLoadMessage) _bench.Check(label + "_normal_load_receipt", MessageLog.GetLast() == "Game loaded.");
        }
        private void AssertHealthy(InputHandler input, string label)
        {
            var actor = input.PlayerEntity; var dagger = FindItem(input, _daggerID);
            AssertEquipped(input, FindItem(input, _bucklerID), "Hand", Laterality.LEFT, label + "_buckler");
            AssertEquipped(input, dagger, "Hand", Laterality.RIGHT, label + "_dagger"); AssertEquipped(input, FindItem(input, _bootsID), "Feet", 0, label + "_boots");
            _bench.Check(label + "_stats_hooks_and_exact_counts", actor.GetPart<InventoryPart>().Objects.Count == 0 && actor.GetPart<InventoryPart>().EquippedItems.Count == 3
                && actor.GetPart<Body>().DismemberedParts.Count == 0 && actor.GetStat("Agility").Bonus == 2 && actor.GetStatValue("Agility") == 18
                && actor.GetStat("Speed").Penalty == 12 && actor.GetStatValue("Speed") == 88 && actor.GetIntProperty("MobilityPenalty") == 0
                && dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<EnhancementGlowQuartz>().RadiusBonus == 2 && dagger.GetPart<LightSourcePart>().Radius == 2);
        }
        private void AssertEquipped(InputHandler input, Entity item, string type, int laterality, string label)
        {
            var actor = input.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); var parts = actor.GetPart<Body>().GetParts().Where(p => ReferenceEquals(p._Equipped, item)).ToArray();
            _bench.Check(label, parts.Length == 1 && parts[0].Type == type && parts[0].GetLaterality() == laterality && parts[0].FirstSlotForEquipped
                && inv.EquippedItems.TryGetValue(parts[0].ID.ToString(), out var cached) && ReferenceEquals(cached, item) && inv.EquippedItems.Values.Count(e => ReferenceEquals(e, item)) == 1
                && !inv.Objects.Contains(item) && item.GetPart<PhysicsPart>().Equipped == actor && item.GetPart<PhysicsPart>().InInventory == null && input.CurrentZone.GetEntityCell(item) == null && Quantity(item) == 1);
        }
        private void AssertDetached(InputHandler input, Entity item, int x, int y, string label)
        {
            var actor = input.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); var body = actor.GetPart<Body>();
            _bench.Check(label, !body.GetParts().Any(p => ReferenceEquals(p._Equipped, item))
                && !body.DismemberedParts.Any(d => d.Part.GetParts().Any(p => ReferenceEquals(p._Equipped, item)))
                && !inv.EquippedItems.Values.Any(e => ReferenceEquals(e, item)) && !inv.Objects.Contains(item)
                && item.GetPart<PhysicsPart>().Equipped == null && item.GetPart<PhysicsPart>().InInventory == null && Quantity(item) == 1
                && input.CurrentZone.GetEntityPosition(item) == (x, y) && input.CurrentZone.GetReadOnlyEntities().Count(e => ReferenceEquals(e, item)) == 1
                && input.CurrentZone.GetEntityCell(item).Objects.Count(e => ReferenceEquals(e, item)) == 1);
        }
        private void AssertTrap(InputHandler input, string label)
        {
            var trap = FindTrap(input); var trigger = trap?.GetPart<SpikeTrapTriggerPart>();
            _bench.Check(label, trap != null && trap.BlueprintName == "SpikeTrap" && !trap.GetPart<PhysicsPart>().Solid && trigger != null && trigger.Damage == 12
                && trigger.ConsumeOnTrigger && string.IsNullOrEmpty(trigger.TriggerFaction) && input.CurrentZone.GetEntityPosition(trap) == (21, 12));
        }
        private static BodyPart NextAppendage(Entity actor) => actor.GetPart<Body>().GetParts().First(p => !p.Mortal && !p.Abstract && p.Appendage);
        private Entity FindItem(InputHandler input, string id)
        {
            var inv = input.PlayerEntity.GetPart<InventoryPart>();
            var matches = inv.Objects.Concat(inv.EquippedItems.Values).Concat(input.CurrentZone.GetReadOnlyEntities()).Where(e => e != null && e.ID == id).Distinct().ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Tracked equipment ID must resolve to one resident entity: " + id + ", matches=" + matches.Length);
            return matches[0];
        }
        private Entity FindTrap(InputHandler input) => input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.ID == _trapID);
        private static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        private static int EnhancementCount(string kind, Entity actor, Entity item) => DiagQuery.Count(new DiagQuery.Filter { Category = "enhancement", Kind = kind, Actor = actor.ID, Target = item.ID }).Count;
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private static bool DeathActive(InputHandler input) => (Read(input, "_deathScreenController") as DeathScreenController)?.IsActive ?? false;
        private string OwnedQuickPath()
        {
            if (string.IsNullOrEmpty(_root) || string.IsNullOrEmpty(_freshID) || _freshID.IndexOfAny(new[] { '/', '\\' }) >= 0 || SaveGameService.SaveRootOverride != _root)
                throw new InvalidOperationException("Owned native save destination missing or changed.");
            return Path.Combine(_root, _freshID, "Quick.sav.gz");
        }
        private void Queue(params Key[] keys) { if (_keyboard != null) InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
        private IEnumerator Tap(Key key) { Queue(key); yield return new WaitForSecondsRealtime(.06f); Queue(); yield return new WaitForSecondsRealtime(.2f); }
        private void Record(InputHandler input, string label) => _snapshots.Add(Capture(input, label));
        private Snapshot Capture(InputHandler input, string label)
        {
            var actor = input.PlayerEntity; var body = actor.GetPart<Body>(); var inv = actor.GetPart<InventoryPart>(); var p = input.CurrentZone.GetEntityPosition(actor);
            return new Snapshot { label = label, actorID = actor.ID, state = State(input), x = p.Item1, y = p.Item2, hp = actor.GetStatValue("Hitpoints"), agility = actor.GetStatValue("Agility"), agilityBonus = actor.GetStat("Agility").Bonus,
                speed = actor.GetStatValue("Speed"), speedPenalty = actor.GetStat("Speed").Penalty, mobilityPenalty = actor.GetIntProperty("MobilityPenalty"), tick = input.TurnManager.TickCount,
                energy = input.TurnManager.GetEnergy(actor), equipmentVersion = EquipmentChangeBus.GlobalVersion, bodyParts = body.GetParts().Count, dismembered = body.DismemberedParts.Count,
                carried = inv.Objects.Count, equippedEntries = inv.EquippedItems.Count, groundEntities = input.CurrentZone.GetReadOnlyEntities().Count(),
                items = new[] { _bucklerID, _daggerID, _bootsID }.Select(id => CaptureItem(input, FindItem(input, id))).ToArray() };
        }
        private static ItemState CaptureItem(InputHandler input, Entity item)
        {
            var actor = input.PlayerEntity; var inv = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>(); var position = input.CurrentZone.GetEntityPosition(item);
            var glow = item.GetPart<EnhancementGlowQuartz>();
            return new ItemState { id = item.ID, blueprint = item.BlueprintName, quantity = Quantity(item), carried = inv.Objects.Count(e => ReferenceEquals(e, item)),
                cacheEntries = inv.EquippedItems.Values.Count(e => ReferenceEquals(e, item)), bodySlots = actor.GetPart<Body>().GetParts().Where(p => ReferenceEquals(p._Equipped, item)).Select(p => p.ID).OrderBy(id => id).ToArray(),
                inInventoryID = physics?.InInventory?.ID, equippedID = physics?.Equipped?.ID, x = position.Item1, y = position.Item2, ground = input.CurrentZone.GetReadOnlyEntities().Count(e => ReferenceEquals(e, item)),
                glowApplied = glow?.AppliedBonus ?? false, glowBonus = glow?.RadiusBonus ?? 0, lightRadius = item.GetPart<LightSourcePart>()?.Radius ?? 0 };
        }
        public void SetUnexpectedErrors(int count) { _unexpectedErrors = count; if (_report != null) WriteReport(); }
        private void Finish()
        {
            _report = new Report { runId = _bench.RunId, root = _root, freshID = _freshID, markerID = _markerID, seconds = Now,
                canVerify = "Queued I/Tab/arrows/Enter equip, developer F8 veto and left/right/Feet injury, F5/F6 alias restoration, actual SpikeTrap walking and death-screen L recovery; exact stats, ownership and enhancement receipts.",
                cannotVerify = "No visual lighting/sprite/text readability, injury feel, ordinary random-combat dismember probability, broad callback rollback, allocation, performance or speedup claim.",
                fixtureBounds = "Mods, controlled HP, veto flag and owned checkpoint-byte restoration are explicit fixture setup. F8 is the existing developer input route, enabled only after bootstrap. Traps and gear are shipped blueprints. Probe Parts are removed before saves. Read final post-teardown JSON and raw logs; process code is selected before teardown." };
            WriteReport(); Debug.Log("[GameAuditEquipmentLifecycleBench] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private void WriteReport()
        {
            _report.cases = _bench.Cases; _report.failures = Failures; _report.unexpectedErrors = _unexpectedErrors; _report.audit = _bench.Audit.ToArray(); _report.snapshots = _snapshots.ToArray();
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, "GA03f-native.json"), JsonUtility.ToJson(_report, true));
        }
        private void OnDestroy()
        {
            try
            {
                if (_report != null)
                {
                    const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
                    bool rootHeld = SaveGameService.SaveRootOverride == _root;
                    bool unregistered = typeof(SaveGameService).GetField("_captureCurrent", flags).GetValue(null) == null && typeof(SaveGameService).GetField("_applyLoaded", flags).GetValue(null) == null;
                    _report.shutdownObserved = true; _report.shutdownSeconds = Now; _report.shutdownRootHeld = rootHeld; _report.shutdownSavingUnregistered = unregistered;
                    if (!rootHeld || !unregistered) _fatalFailures++; WriteReport();
                }
            }
            finally
            {
                if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
                if (_oldSettings != null) InputSystem.settings = _oldSettings;
                if (_settings != null) Destroy(_settings);
                Application.runInBackground = _oldBackground; DevMode.Enabled = _oldDev; Diag.SetChannel("enhancement", _oldEnhancementChannel); _clock?.Stop();
            }
        }
        [Serializable] private sealed class Report
        { public string runId, root, freshID, markerID, canVerify, cannotVerify, fixtureBounds; public int cases, failures, unexpectedErrors; public double seconds, shutdownSeconds; public bool shutdownObserved, shutdownRootHeld, shutdownSavingUnregistered; public string[] audit; public Snapshot[] snapshots; }
        [Serializable] private sealed class Snapshot
        { public string label, actorID, state; public int x, y, hp, agility, agilityBonus, speed, speedPenalty, mobilityPenalty, tick, energy, equipmentVersion, bodyParts, dismembered, carried, equippedEntries, groundEntities; public ItemState[] items; }
        [Serializable] private sealed class ItemState
        { public string id, blueprint, inInventoryID, equippedID; public int quantity, carried, cacheEntries, x, y, ground, glowBonus, lightRadius; public bool glowApplied; public int[] bodySlots; }
    }
}
