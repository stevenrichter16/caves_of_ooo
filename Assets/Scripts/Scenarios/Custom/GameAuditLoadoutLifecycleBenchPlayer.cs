using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Native engine lifetime, deterministic scenario API stimuli. No queued
    /// input: the real boot modal stays active while APIs exercise the saved game graph.</summary>
    public sealed class GameAuditLoadoutLifecycleBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => (_bench == null ? 0 : _bench.Failures) + _fatalFailures + _unexpectedErrors;
        private ScenarioContext _ctx;
        private GameAuditLoadoutLifecycleBench _bench;
        private System.Diagnostics.Stopwatch _clock;
        private bool _oldBackground;
        private int _fatalFailures, _unexpectedErrors;
        private string _root, _markerID, _freshID;
        private string[] _actorIDs, _equippedItemIDs;
        private Report _report;
        private readonly List<Stage> _stages = new List<Stage>();
        private string DirectoryPath => Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit");
        private double Now => _clock?.Elapsed.TotalSeconds ?? 0;
        public void Initialize(ScenarioContext ctx, GameAuditLoadoutLifecycleBench bench)
        {
            _ctx = ctx; _bench = bench; _clock = System.Diagnostics.Stopwatch.StartNew(); _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _root = SaveGameService.SaveRootOverride; _markerID = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _actorIDs = bench.Actors().Select(a => a.ID).ToArray(); _equippedItemIDs = GameAuditLoadoutLifecycleBench.OwnedItems(bench.EquippedActor).Select(e => e.ID).OrderBy(s => s).ToArray();
            StartCoroutine(RunSafely());
        }
        private IEnumerator RunSafely()
        {
            // OnAfterBootstrap precedes save callback registration. Wait for Start to return.
            yield return new WaitForSecondsRealtime(.6f);
            try { Audit(); } catch (Exception ex) { if (_bench.Failures == 0) _fatalFailures++; Debug.LogError(ex); }
            Finish();
        }
        private void Audit()
        {
            var input = FindFirstObjectByType<InputHandler>(); var boot = Read(input, "_bootMenuController") as BootMenuController;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            _bench.Check("api_runtime_ready_under_owned_boot_guard", input != null && boot != null && boot.IsActive && !string.IsNullOrWhiteSpace(_root)
                && SaveGameService.SaveRootOverride == _root && typeof(SaveGameService).GetField("_captureCurrent", flags).GetValue(null) != null
                && typeof(SaveGameService).GetField("_applyLoaded", flags).GetValue(null) != null && File.Exists(Path.Combine(_root, _markerID, "Quick.sav.gz")));
            byte[] markerBytes = File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz"));
            var equipped = FindActor(input, 0); var carried = FindActor(input, 1); var veto = FindActor(input, 2); var noBody = FindActor(input, 3); var occupied = FindActor(input, 4);
            var boots = Item(equipped, "IronshodBoots"); var buckler = Item(equipped, "Buckler"); var dagger = Item(equipped, "Dagger");
            _bench.Check("fixture_factory_scope_and_actual_item_configuration", ReferenceEquals(LoadoutPart.Factory, _bench.OriginalLoadoutFactory) && ReferenceEquals(LoadoutPart.Rng, _bench.OriginalLoadoutRng)
                && boots.GetPart<ArmorPart>().SpeedPenalty == 5 && buckler.GetPart<EquippablePart>().Slot == "Hand" && dagger.GetPart<EnhancementGlowQuartz>().RadiusBonus == 2
                && _actorIDs.Distinct().Count() == 5 && GameAuditLoadoutLifecycleBench.OwnedItems(equipped).All(e => Quantity(e) == 1));
            _bench.Check("equip_loadout_exact_slots_owners_and_contributions", Healthy(equipped));
            _bench.Check("actual_mod_producers_and_enhancement_applied_once", buckler.HasTag("ModDuelistCut") && buckler.GetPart<ArmorPart>().AV == 0
                && buckler.GetPart<GameAuditLoadoutModSeedPart>().CreatedCount == 1 && dagger.GetPart<GameAuditLoadoutModSeedPart>().CreatedCount == 1
                && dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 2);
            _bench.Check("factory_creation_and_actor_equip_hooks_once", Probe(equipped).CreatedCount == 1 && Probe(equipped).BeforeEquipCount == 3 && Probe(equipped).AfterEquipCount == 3);
            _bench.Check("carry_control_has_items_without_equipment_effects", AllCarried(carried, 3) && Probe(carried).BeforeEquipCount == 0 && Probe(carried).AfterEquipCount == 0);
            _bench.Check("veto_control_retains_grants_without_effects", AllCarried(veto, 3) && Probe(veto).VetoEquip && Probe(veto).BeforeEquipCount == 3 && Probe(veto).AfterEquipCount == 0);
            _bench.Check("no_body_control_remains_carried_without_legacy_slots", noBody.GetPart<Body>() == null && AllCarried(noBody, 3) && Probe(noBody).BeforeEquipCount == 0);
            _bench.Check("occupied_control_preserves_exact_initial_equipment", OccupiedHealthy(occupied));
            Record(input, "factory_created_controls");

            bool removed = InventorySystem.UnequipItem(equipped, boots);
            _bench.Check("normal_unequip_refunds_boot_penalty_only", removed && CarriedBy(equipped, boots) && equipped.GetStat("Speed").Penalty == 0 && equipped.GetStat("Agility").Bonus == 2
                && dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && Probe(equipped).AfterUnequipCount == 1);
            bool restored = InventorySystem.AutoEquip(equipped, boots);
            _bench.Check("normal_reequip_restores_boot_penalty_once", restored && Healthy(equipped) && Probe(equipped).AfterEquipCount == 4);
            removed = InventorySystem.UnequipItem(equipped, dagger);
            _bench.Check("normal_unequip_reverses_glow_delta_only", removed && CarriedBy(equipped, dagger) && !dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus
                && dagger.GetPart<LightSourcePart>().Radius == 0 && dagger.GetPart<EnhancementGlowQuartz>().RadiusBonus == 2 && equipped.GetStat("Speed").Penalty == 5 && equipped.GetStat("Agility").Bonus == 2);
            restored = InventorySystem.AutoEquip(equipped, dagger);
            _bench.Check("normal_reequip_reapplies_glow_once", restored && Healthy(equipped) && Probe(equipped).AfterEquipCount == 5 && Probe(equipped).AfterUnequipCount == 2);

            // Direct APIs intentionally emit no input-adapter save/load messages and do not close the boot modal.
            bool began = SaveGameService.BeginNewGame(); _freshID = SaveGameService.GetSaveInfo("Quick")?.GameID;
            _bench.Check("api_begin_writes_complete_fresh_checkpoint", began && _freshID != null && _freshID != _markerID && boot.IsActive
                && File.Exists(OwnedQuickPath()) && File.ReadAllBytes(OwnedQuickPath()).Length > 0 && SaveGameService.HasQuickSave());
            _bench.Check("checkpoint_keeps_marker_and_private_destination", markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz")))
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID)) && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _markerID)));
            string savedSignature = Signature(equipped); var savedControlSignatures = Enumerable.Range(1, 4).Select(i => ControlSignature(FindActor(input, i))).ToArray();
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(input.PlayerEntity);
            int savedBefore = Probe(equipped).BeforeEquipCount, savedAfter = Probe(equipped).AfterEquipCount, savedUnequip = Probe(equipped).AfterUnequipCount;
            Record(input, "saved_complete_graph");
            var oldPlayer = input.PlayerEntity; var oldEquipped = equipped;
            equipped.SetIntProperty("GA03gSavedMarker", -7); bool changed = InventorySystem.UnequipItem(equipped, boots);
            _bench.Check("load_precondition_is_a_real_serialized_change", changed && equipped.GetIntProperty("GA03gSavedMarker") == -7 && CarriedBy(equipped, boots) && equipped.GetStat("Speed").Penalty == 0);
            bool loaded = SaveGameService.QuickLoad(); equipped = FindActor(input, 0);
            _bench.Check("api_full_load_replaces_graph_and_restores_clock", loaded && !ReferenceEquals(input.PlayerEntity, oldPlayer) && !ReferenceEquals(equipped, oldEquipped)
                && ReferenceEquals(input.TurnManager, TurnManager.Active) && ReferenceEquals(input.TurnManager.CurrentActor, input.PlayerEntity)
                && input.TurnManager.TickCount == tick && input.TurnManager.GetEnergy(input.PlayerEntity) == energy && input.CurrentZone.GetEntityCell(input.PlayerEntity) != null
                && ReferenceEquals(input.CurrentZone, input.ZoneManager?.ActiveZone));
            _bench.Check("full_load_restores_aliases_and_does_not_regrant", Healthy(equipped) && Signature(equipped) == savedSignature && equipped.GetIntProperty("GA03gSavedMarker") == 37
                && Probe(equipped).CreatedCount == 1 && Probe(equipped).BeforeEquipCount == savedBefore && Probe(equipped).AfterEquipCount == savedAfter
                && Probe(equipped).AfterUnequipCount == savedUnequip && ModCreationCountsIntact(equipped) && equipped.GetPart<LoadoutPart>().Equip == "IronshodBoots;Buckler;Dagger");
            // BeginNewGame is not repeated after load: bootstrap deliberately re-registers without a pending fresh identity.
            bool savedAgain = SaveGameService.QuickSave(); oldEquipped = equipped; bool loadedAgain = SaveGameService.QuickLoad(); equipped = FindActor(input, 0);
            _bench.Check("second_complete_roundtrip_is_stable", savedAgain && loadedAgain && !ReferenceEquals(equipped, oldEquipped) && Healthy(equipped)
                && Signature(equipped) == savedSignature && Probe(equipped).CreatedCount == 1 && Probe(equipped).BeforeEquipCount == savedBefore
                && Probe(equipped).AfterEquipCount == savedAfter && Probe(equipped).AfterUnequipCount == savedUnequip && ModCreationCountsIntact(equipped));
            Record(input, "twice_loaded_without_regrant");

            int unequipCount = Probe(equipped).AfterUnequipCount; var position = input.CurrentZone.GetEntityPosition(equipped);
            equipped.GetPart<Body>().DropAllEquipment(input.CurrentZone);
            _bench.Check("loaded_force_cleanup_composes_with_loadout_effects", AllDropped(input, equipped, position.Item1, position.Item2) && Probe(equipped).AfterUnequipCount == unequipCount + 3);
            int version = EquipmentChangeBus.GlobalVersion; unequipCount = Probe(equipped).AfterUnequipCount; equipped.GetPart<Body>().DropAllEquipment(input.CurrentZone);
            _bench.Check("repeated_force_cleanup_is_quiet", AllDropped(input, equipped, position.Item1, position.Item2) && Probe(equipped).AfterUnequipCount == unequipCount && EquipmentChangeBus.GlobalVersion == version);
            savedAgain = SaveGameService.QuickSave(); oldEquipped = equipped; loadedAgain = SaveGameService.QuickLoad(); equipped = FindActor(input, 0);
            _bench.Check("saved_empty_equipment_does_not_regenerate_loadout", savedAgain && loadedAgain && !ReferenceEquals(equipped, oldEquipped)
                && AllDropped(input, equipped, position.Item1, position.Item2) && Probe(equipped).CreatedCount == 1 && Probe(equipped).AfterEquipCount == savedAfter
                && Probe(equipped).AfterUnequipCount == unequipCount && equipped.GetPart<LoadoutPart>().Equip == "IronshodBoots;Buckler;Dagger");
            _bench.Check("loaded_refusal_controls_preserve_their_graphs", AllCarried(FindActor(input, 1), 3) && AllCarried(FindActor(input, 2), 3)
                && Probe(FindActor(input, 2)).BeforeEquipCount == 3 && FindActor(input, 3).GetPart<Body>() == null && AllCarried(FindActor(input, 3), 3) && OccupiedHealthy(FindActor(input, 4))
                && Enumerable.Range(1, 4).All(i => ControlSignature(FindActor(input, i)) == savedControlSignatures[i - 1]));
            _bench.Check("final_scope_is_private_and_runtime_factory_restored", SaveGameService.SaveRootOverride == _root && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID
                && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2 && markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz")))
                && ReferenceEquals(LoadoutPart.Factory, _ctx.Factory) && ReferenceEquals(LoadoutPart.Rng, _bench.OriginalLoadoutRng));
            Record(input, "final_loaded_cleanup_and_controls");
        }
        private static GameAuditLoadoutProbePart Probe(Entity actor) => actor.GetPart<GameAuditLoadoutProbePart>();
        private static Entity Item(Entity actor, string blueprint) => GameAuditLoadoutLifecycleBench.OwnedItems(actor).Single(e => e.BlueprintName == blueprint);
        private Entity FindActor(InputHandler input, int index) => input.CurrentZone.GetReadOnlyEntities().Single(e => e.ID == _actorIDs[index]);
        private static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        private static bool CarriedBy(Entity actor, Entity item)
        { var inventory = actor.GetPart<InventoryPart>(); return inventory.Objects.Count(e => ReferenceEquals(e, item)) == 1 && !inventory.EquippedItems.Values.Contains(item)
            && item.GetPart<PhysicsPart>().InInventory == actor && item.GetPart<PhysicsPart>().Equipped == null && Quantity(item) == 1; }
        private static bool EquippedBy(Entity actor, Entity item)
        {
            var inventory = actor.GetPart<InventoryPart>(); var parts = actor.GetPart<Body>()?.GetParts().Where(p => ReferenceEquals(p._Equipped, item)).ToArray();
            return parts != null && parts.Length == 1 && parts[0].FirstSlotForEquipped && inventory.EquippedItems.TryGetValue(parts[0].ID.ToString(), out var cached) && ReferenceEquals(cached, item)
                && inventory.EquippedItems.Values.Count(e => ReferenceEquals(e, item)) == 1 && !inventory.Objects.Contains(item)
                && item.GetPart<PhysicsPart>().Equipped == actor && item.GetPart<PhysicsPart>().InInventory == null && Quantity(item) == 1;
        }
        private static bool Healthy(Entity actor)
        {
            var inventory = actor.GetPart<InventoryPart>(); var items = GameAuditLoadoutLifecycleBench.OwnedItems(actor);
            var dagger = items.SingleOrDefault(e => e.BlueprintName == "Dagger");
            return items.Length == 3 && inventory.Objects.Count == 0 && inventory.EquippedItems.Count == 3 && items.All(e => EquippedBy(actor, e))
                && actor.GetStat("Agility").Bonus == 2 && actor.GetStatValue("Agility") == 18 && actor.GetStat("Speed").Penalty == 5 && actor.GetStatValue("Speed") == 95
                && dagger != null && dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && dagger.GetPart<LightSourcePart>().Radius == 2;
        }
        private static bool AllCarried(Entity actor, int count)
        {
            var inventory = actor.GetPart<InventoryPart>(); var items = GameAuditLoadoutLifecycleBench.OwnedItems(actor); var dagger = items.SingleOrDefault(e => e.BlueprintName == "Dagger");
            return items.Length == count && inventory.Objects.Count == count && inventory.EquippedItems.Count == 0 && items.All(e => CarriedBy(actor, e))
                && (actor.GetPart<Body>() == null || !actor.GetPart<Body>().GetParts().Any(p => p._Equipped != null)) && actor.GetStat("Agility").Bonus == 0 && actor.GetStat("Speed").Penalty == 0
                && dagger != null && !dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus && (dagger.GetPart<LightSourcePart>()?.Radius ?? 0) == 0;
        }
        private static bool OccupiedHealthy(Entity actor)
        {
            var probe = Probe(actor); var inventory = actor.GetPart<InventoryPart>(); var initial = probe.InitialEquipment;
            return initial != null && initial.BlueprintName == "IronshodBoots" && EquippedBy(actor, initial) && inventory.EquippedItems.Count == 1 && inventory.Objects.Count == 1
                && inventory.Objects[0] != initial && inventory.Objects[0].BlueprintName == "IronshodBoots" && CarriedBy(actor, inventory.Objects[0])
                && actor.GetStat("Speed").Penalty == 5 && probe.CreatedCount == 1 && probe.BeforeEquipCount == 1 && probe.AfterEquipCount == 1;
        }
        private bool AllDropped(InputHandler input, Entity actor, int x, int y)
        {
            var inventory = actor.GetPart<InventoryPart>(); if (inventory.Objects.Count != 0 || inventory.EquippedItems.Count != 0 || actor.GetPart<Body>().GetParts().Any(p => p._Equipped != null)
                || actor.GetStat("Agility").Bonus != 0 || actor.GetStat("Speed").Penalty != 0) return false;
            foreach (string id in _equippedItemIDs)
            {
                var matches = input.CurrentZone.GetReadOnlyEntities().Where(e => e.ID == id).ToArray(); if (matches.Length != 1) return false; var item = matches[0];
                if (input.CurrentZone.GetEntityPosition(item) != (x, y) || input.CurrentZone.GetEntityCell(item).Objects.Count(e => ReferenceEquals(e, item)) != 1
                    || item.GetPart<PhysicsPart>().Equipped != null || item.GetPart<PhysicsPart>().InInventory != null || Quantity(item) != 1) return false;
                var glow = item.GetPart<EnhancementGlowQuartz>(); if (glow != null && (glow.AppliedBonus || item.GetPart<LightSourcePart>().Radius != 0 || glow.RadiusBonus != 2)) return false;
                var seed = item.GetPart<GameAuditLoadoutModSeedPart>(); if (seed != null && seed.CreatedCount != 1) return false;
            }
            return true;
        }
        private static string Signature(Entity actor) => string.Join("|", GameAuditLoadoutLifecycleBench.OwnedItems(actor).Select(e => e.ID + ":" + e.BlueprintName + ":" + Quantity(e)).OrderBy(s => s));
        private static string ControlSignature(Entity actor)
        { var p = Probe(actor); var loadout = actor.GetPart<LoadoutPart>(); return Signature(actor) + "/" + p.CreatedCount + "/" + p.BeforeEquipCount + "/" + p.AfterEquipCount
            + "/" + p.AfterUnequipCount + "/" + p.InitialEquipment?.ID + "/" + loadout.Equip + "/" + loadout.Carry + "/" + loadout.Pick; }
        private static bool ModCreationCountsIntact(Entity actor) => GameAuditLoadoutLifecycleBench.OwnedItems(actor).All(e => e.GetPart<GameAuditLoadoutModSeedPart>() == null || e.GetPart<GameAuditLoadoutModSeedPart>().CreatedCount == 1);
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private string OwnedQuickPath()
        {
            if (string.IsNullOrEmpty(_root) || string.IsNullOrEmpty(_freshID) || _freshID.IndexOfAny(new[] { '/', '\\' }) >= 0 || SaveGameService.SaveRootOverride != _root)
                throw new InvalidOperationException("Owned native save destination missing or changed.");
            return Path.Combine(_root, _freshID, "Quick.sav.gz");
        }
        private void Record(InputHandler input, string label)
        {
            _stages.Add(new Stage { label = label, tick = input.TurnManager.TickCount, equipmentVersion = EquipmentChangeBus.GlobalVersion,
                actors = Enumerable.Range(0, _actorIDs.Length).Select(i => {
                    var a = FindActor(input, i); var p = Probe(a); var inventory = a.GetPart<InventoryPart>();
                    return new ActorState { id = a.ID, blueprint = a.BlueprintName, itemSignature = Signature(a), carried = inventory.Objects.Count, equipmentEntries = inventory.EquippedItems.Count,
                        agility = a.GetStatValue("Agility"), agilityBonus = a.GetStat("Agility").Bonus, speed = a.GetStatValue("Speed"), speedPenalty = a.GetStat("Speed").Penalty,
                        created = p.CreatedCount, beforeEquip = p.BeforeEquipCount, afterEquip = p.AfterEquipCount, afterUnequip = p.AfterUnequipCount, savedMarker = a.GetIntProperty("GA03gSavedMarker") };
                }).ToArray() });
        }
        public void SetUnexpectedErrors(int count) { _unexpectedErrors = count; if (_report != null) WriteReport(); }
        private void Finish()
        {
            _report = new Report { runId = _bench.RunId, root = _root, freshID = _freshID, markerID = _markerID, seconds = Now,
                canVerify = "Native engine lifetime with API stimuli: actual EntityFactory creation, explicit fixture Loadout Equip/Carry/veto/no-Body/occupied controls, real modification payloads, normal and forced cleanup, complete compressed saves through live bootstrap callbacks, exact loaded aliases and no regrant.",
                cannotVerify = "No keyboard routing, ordinary-world Loadout reach, visual appearance, loot balance, feel, frame allocation, performance or speedup claim. No current authored Loadout content is added.",
                fixtureBounds = "Inert private copies of the actor blueprint, real item blueprints with scenario-only mod-creation Parts, a veto/occupied-slot probe, and unique IDs are explicit fixture configuration. Public probe counters/config are serialized to expose replay. APIs leave the boot modal active and do not emit input success messages. Read final post-teardown JSON/raw logs; process code is selected before teardown." };
            WriteReport(); Debug.Log("[GameAuditLoadoutLifecycleBench] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private void WriteReport()
        {
            _report.cases = _bench.Cases; _report.failures = Failures; _report.unexpectedErrors = _unexpectedErrors; _report.audit = _bench.Audit.ToArray(); _report.stages = _stages.ToArray();
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, "GA03g-native.json"), JsonUtility.ToJson(_report, true));
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
            finally { Application.runInBackground = _oldBackground; _clock?.Stop(); }
        }
        [Serializable] private sealed class Report
        { public string runId, root, freshID, markerID, canVerify, cannotVerify, fixtureBounds; public int cases, failures, unexpectedErrors; public double seconds, shutdownSeconds; public bool shutdownObserved, shutdownRootHeld, shutdownSavingUnregistered; public string[] audit; public Stage[] stages; }
        [Serializable] private sealed class Stage
        { public string label; public int tick, equipmentVersion; public ActorState[] actors; }
        [Serializable] private sealed class ActorState
        { public string id, blueprint, itemSignature; public int carried, equipmentEntries, agility, agilityBonus, speed, speedPenalty, created, beforeEquip, afterEquip, afterUnequip, savedMarker; }
    }
}
