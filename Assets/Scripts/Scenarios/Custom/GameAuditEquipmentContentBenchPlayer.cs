
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using Unity.Profiling;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>23 finite groups: actual content creation/producers, explicit combat APIs,
    /// queued N/F5/F6/walk/G. No encounter-frequency, art, feel or performance claim.</summary>
    public sealed class GameAuditEquipmentContentBenchPlayer : MonoBehaviour
    {
        private sealed class Kit
        {
            public string Actor; public string[] Equipped, Carried;
            public Kit(string actor, string equipped, string carried = "")
            { Actor = actor; Equipped = equipped.Split(';'); Carried = carried.Length == 0 ? Array.Empty<string>() : carried.Split(';'); }
        }
        private static readonly Kit[] Kits = {
            new Kit("Snapjaw", "Dagger;LeatherCap"),
            new Kit("SnapjawScavenger", "LeatherGloves;Dagger"),
            new Kit("SnapjawHunter", "Spear;LeatherBoots"),
            new Kit("SnapjawChieftain", "ShortSword;LeatherArmor;LeatherCap"),
            new Kit("SnapjawWarlord", "LeatherArmor;IronHelmet;IronshodBoots"),
            new Kit("DesertBandit", "ShortSword;LeatherCap"),
            new Kit("RuinScavenger", "Dagger;LeatherGloves"),
            new Kit("SkeletalSentry", "IronHelmet"),
            new Kit("AmbushBandit", "ShortSword;LeatherArmor"),
            new Kit("RuneCultist", "Dagger;LeatherGloves"),
            new Kit("Warden", "LongSword;LeatherArmor;LeatherBoots"),
            new Kit("Quartermaster", "Spear;LeatherArmor;LeatherBoots"),
            new Kit("Weaponsmith", "LeatherGloves;LeatherBoots"),
            new Kit("Tinker", "Dagger;LeatherGloves"),
            new Kit("Farmer", "LeatherBoots;LeatherGloves"),
            new Kit("WellKeeper", "LeatherBoots;LeatherCap"),
            new Kit("PeatCutter", "LeatherBoots;LeatherGloves", "Dagger"),
            new Kit("Elder", "LeatherCap;LeatherBoots"),
            new Kit("Scribe", "LeatherGloves;LeatherCap"),
            new Kit("Merchant", "ShortSword;LeatherBoots"),
            new Kit("TentRightHost", "LeatherBoots;LeatherCap", "Dagger"),
            new Kit("SaltMaster", "LeatherGloves;LeatherBoots"),
            new Kit("RecensionScribe", "LeatherGloves;LeatherBoots"),
            new Kit("CurationSorter", "LeatherGloves;LeatherCap")
        };
        public bool Finished { get; private set; }
        public int Failures => (_bench?.Failures ?? 0) + _fatal + _errors;
        private GameAuditEquipmentContentBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldEvent, _oldDamage;
        private int _fatal, _errors, _savedTick, _savedEnergy;
        private string _root, _markerID, _freshID, _targetID, _bootsID;
        private byte[] _markerBytes, _healthyBytes;
        private string[] _actorIDs, _healthySignatures, _warlordItemIDs;
        private Entity[] _created;
        private readonly List<ProfileActor> _profiles = new List<ProfileActor>();
        private readonly List<Stage> _stages = new List<Stage>();
        private System.Diagnostics.Stopwatch _clock;
        private Report _report;
        private double Now => _clock?.Elapsed.TotalSeconds ?? 0;
        private string ReportDirectory => Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit");

        // Separate API-combat profiling branch: zero functional content Check groups.
        private string _performanceMode;
        private bool _performanceStarted, _performanceCompleted, _perfMeasuring, _perfOverflow;
        private const int PerfFrameCapacity = 200000, PerfStrikeCapacity = 4000;
        private const double PerfPhaseSeconds = 25, PerfStrikeSpacing = .1;
        private readonly string[] _perfMarkers = { "COO.Combat.PerformMeleeAttack", "COO.Input.Update", "COO.ZoneRenderer.LateUpdate", "COO.Turns.Tick", "COO.Combat.ApplyDamage", "GC Allocated In Frame" };
        private readonly PerfPhase[] _perfPhases = { new PerfPhase { name = "idle" }, new PerfPhase { name = "one_hand" }, new PerfPhase { name = "two_hands" } };
        private ProfilerRecorder[] _perfRecorders;
        private bool[] _perfRecorderValid;
        private long[][] _perfSamples;
        private double[] _perfFrameTimes, _perfFrameMilliseconds;
        private int[] _perfFramePhases;
        private PerfStrike[] _perfStrikes;
        private int _perfFrames, _perfStrikeCount, _perfPhase;
        private double _perfStart, _perfPhaseStart;
        private InputHandler _perfInput;
        private BootMenuController _perfBoot;
        private DeathScreenController _perfDeath;
        private Zone _perfZone;
        private TurnManager _perfTurns;
        private Entity _perfSpectator, _perfOneHand, _perfTwoHands, _perfTarget, _perfSword, _perfGreatsword;
        private Stat _perfSpectatorHP, _perfOneHP, _perfTwoHP, _perfTargetHP;
        private int _perfSpectatorHpValue, _perfOneHpValue, _perfTwoHpValue, _perfTargetHpValue, _perfTick, _perfEnergy;
        private readonly PerfMissRolls _perfRng = new PerfMissRolls();
        private string PerfPrefix => "GA03i-" + _performanceMode;
        public void ConfigurePerformance(string mode)
        {
            Need(!_performanceStarted && (mode == "before" || mode == "after"), "Invalid or late combat performance mode.");
            _performanceMode = mode;
        }

        public void Initialize(ScenarioContext ctx, GameAuditEquipmentContentBench bench)
        {
            _bench = bench; _clock = System.Diagnostics.Stopwatch.StartNew();
            _root = SaveGameService.SaveRootOverride; _markerID = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _oldEvent = Diag.IsChannelEnabled("event"); _oldDamage = Diag.IsChannelEnabled("damage");
            Diag.SetChannel("event", true); Diag.SetChannel("damage", true);
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
                if (error != null) { if (_bench.Failures == 0) _fatal++; Debug.LogError(error); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; } yield return current;
            }
            Queue(); DisposePerformanceRecorders(); Finish();
        }
        private IEnumerator Audit()
        {
            yield return new WaitForSecondsRealtime(.7f);
            if (_performanceMode != null) { yield return PerformanceAudit(); yield break; }
            var input = FindFirstObjectByType<InputHandler>(); var boot = Read(input, "_bootMenuController") as BootMenuController;
            Need(input != null && boot != null && boot.IsActive && SaveGameService.SaveRootOverride == _root, "Owned real boot menu required.");
            _markerBytes = File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz"));
            yield return Tap(Key.N); _freshID = SaveGameService.GetSaveInfo("Quick")?.GameID;
            _bench.Check("native_N_binds_fresh_owned_game", !boot.IsActive && State(input) == "Normal" && _freshID != null && _freshID != _markerID && File.Exists(QuickPath()));
            Need(ReferenceEquals(LoadoutPart.Factory, input.EntityFactory) && ReferenceEquals(TraderPart.Factory, input.EntityFactory), "Both live factory aliases required; do not install a private factory.");

            // Observe real production before replacing the active village with an explicit inert arena.
            var villageRoles = new[] { "Elder", "Merchant", "Quartermaster", "Scribe", "Warden" };
            var village = input.CurrentZone.GetReadOnlyEntities().ToArray();
            _bench.Check("starting_village_producers_keep_personal_gear_and_stock", input.CurrentZone.ZoneID == WorldMap.StartingZoneID
                && villageRoles.All(bp => village.Any(a => a.BlueprintName == bp && GuaranteedKit(a) && StockIntact(a))));
            var active = input.CurrentZone; var turns = input.TurnManager; var player = input.PlayerEntity; int beforeTick = turns.TickCount;
            ObserveProfile(input, "Overworld.8.16.0", "TentRightHost", "SaltMaster");
            ObserveProfile(input, "Overworld.15.6.0", "PeatCutter");
            ObserveProfile(input, "Overworld.17.5.0", "RecensionScribe", "CurationSorter");
            _bench.Check("named_profile_producers_generate_real_gear_without_active_zone_change", _profiles.Count == 6
                && ReferenceEquals(active, input.CurrentZone) && ReferenceEquals(active, input.ZoneManager.ActiveZone)
                && ReferenceEquals(turns, input.TurnManager) && ReferenceEquals(player, input.PlayerEntity) && turns.TickCount == beforeTick);

            int serial = MessageLog.NextSerialValue; var loadoutRng = LoadoutPart.Rng; var traderRng = TraderPart.Rng;
            try
            {
                LoadoutPart.Rng = new ContentRolls(); TraderPart.Rng = new ContentRolls();
                _created = Kits.Select(k => input.EntityFactory.CreateEntity(k.Actor)).ToArray();
            }
            finally { LoadoutPart.Rng = loadoutRng; TraderPart.Rng = traderRng; }
            Need(_created.All(a => a != null), "All actual content factory calls must return an actor.");
            _actorIDs = _created.Select(a => a.ID).ToArray();
            _bench.Check("all_24_authored_kits_have_exact_positive_choices", _created.Length == 24 && Enumerable.Range(0, 24).All(i => ExactKit(_created[i], Kits[i])));
            _bench.Check("factory_creation_is_quiet_but_keeps_per_item_equipment_outcomes", EquipMessagesSince(serial) == 0
                && Enumerable.Range(0, 24).All(i => EventCount("LoadoutEquipResult", _created[i].ID) == Kits[i].Equipped.Length));
            _bench.Check("all_personal_units_have_unique_owners_and_stock_survives", _created.All(a => Ownership(a) && StockIntact(a)
                && a.GetPart<InventoryPart>().MaxWeight == 150 && a.GetPart<InventoryPart>().GetCarriedWeight() <= 150)
                && _created.SelectMany(Owned).Select(e => e.ID).Distinct().Count() == _created.Sum(a => Owned(a).Length));
            var beast = input.EntityFactory.CreateEntity("CaveBear");
            _bench.Check("living_beast_control_keeps_natural_weapons_and_no_loadout", beast.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(beast)
                && beast.GetPart<LoadoutPart>() == null && beast.GetPart<Body>().GetParts().All(p => p._Equipped == null)
                && beast.GetPart<Body>().GetParts().Any(p => p._DefaultBehavior?.GetPart<MeleeWeaponPart>()?.BaseDamage == "2d4"));
            Entity alternate;
            try { LoadoutPart.Rng = new ContentRolls(secondPick: true); alternate = input.EntityFactory.CreateEntity("SnapjawScavenger"); }
            finally { LoadoutPart.Rng = loadoutRng; }
            _bench.Check("actual_scavenger_pick_can_choose_second_weapon_without_double_arming", ExactKit(alternate, new Kit("SnapjawScavenger", "LeatherGloves;ShortSword"))
                && alternate.GetPart<Body>().GetParts().Count(p => p._Equipped?.GetPart<MeleeWeaponPart>() != null) == 1 && Ownership(alternate));

            BuildArena(input);
            var warlord = Actor(input, "SnapjawWarlord"); var sentry = Actor(input, "SkeletalSentry");
            _bootsID = Equipped(warlord).Single(e => e.BlueprintName == "IronshodBoots").ID;
            _warlordItemIDs = Equipped(warlord).Select(e => e.ID).ToArray();
            _bench.Check("armor_is_location_specific_and_boots_apply_exact_slowdown", PartAV(warlord, "Body") == 7 && PartAV(warlord, "Head") == 6 && PartAV(warlord, "Feet") == 6
                && PartAV(warlord, "Hand") == 4 && PartAV(sentry, "Head") == 7 && PartAV(sentry, "Body") == 5
                && warlord.GetStat("Speed").Penalty == 5 && sentry.GetStat("Speed").Penalty == 0
                && CombatSystem.GetDV(warlord) == 6 + StatUtils.GetModifier(warlord, "Agility") - 1);
            _bench.Check("warlord_full_melee_keeps_2d5_pen2_cutting_axe", NaturalAttack(input, warlord, "2d5", 2, "Axe"));
            _bench.Check("sentry_full_melee_keeps_1d6_plus1_pen1_cutting", NaturalAttack(input, sentry, "1d6+1", 1, null));
            Record(input, "actual_kits_and_attacks");

            input.PlayerEntity.SetIntProperty("GA03iCheckpoint", 1); int saveSerial = MessageLog.NextSerialValue; byte[] beforeSave = File.ReadAllBytes(QuickPath());
            yield return Tap(Key.F5); _healthyBytes = File.ReadAllBytes(QuickPath());
            _savedTick = input.TurnManager.TickCount; _savedEnergy = input.TurnManager.GetEnergy(input.PlayerEntity);
            _healthySignatures = _actorIDs.Select(id => Signature(ActorByID(input, id))).ToArray();
            _bench.Check("native_F5_saves_all_real_kits_stock_and_cached_profiles", MessageLog.GetLast() == "Game saved." && MessageLog.NextSerialValue > saveSerial
                && !beforeSave.SequenceEqual(_healthyBytes) && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID && ProfilesIntact(input)
                && _markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz"))));
            var boots = Owned(warlord).Single(e => e.ID == _bootsID);
            _bench.Check("normal_unequip_refunds_only_boot_slowdown", InventorySystem.UnequipItem(warlord, boots) && CarriedBy(warlord, boots)
                && warlord.GetStat("Speed").Penalty == 0 && warlord.GetPart<Body>().CalculateMobilityPenalty() == 0);
            serial = MessageLog.NextSerialValue;
            _bench.Check("normal_reequip_still_announces_and_restores_bonus", InventorySystem.AutoEquip(warlord, boots) && EquippedBy(warlord, boots)
                && warlord.GetStat("Speed").Penalty == 5 && EquipMessagesSince(serial) == 1);
            var feet = warlord.GetPart<Body>().GetParts().Single(p => p.Type == "Feet"); var veto = new GameAuditContentVetoPart(); warlord.AddPart(veto);
            bool cut;
            try { cut = warlord.GetPart<Body>().Dismember(feet, input.CurrentZone); }
            finally { warlord.RemovePart(veto); }
            _bench.Check("living_veto_control_preserves_exact_feet_gear", !cut && veto.Before == 1 && feet.ParentPart != null && EquippedBy(warlord, boots)
                && warlord.GetStat("Speed").Penalty == 5 && Alive(input, warlord));
            int hp = warlord.GetStatValue("Hitpoints");
            cut = warlord.GetPart<Body>().Dismember(feet, input.CurrentZone);
            _bench.Check("nonmortal_feet_loss_drops_exact_boots_and_refunds_equipment_only", cut && feet.ParentPart == null && GroundAt(input, boots, 21, 12)
                && !Owned(warlord).Contains(boots) && warlord.GetStat("Speed").Penalty == warlord.GetPart<Body>().CalculateMobilityPenalty()
                && warlord.GetStat("Speed").Penalty > 0 && warlord.GetStatValue("Hitpoints") == hp && Alive(input, warlord));
            Record(input, "nonmortal_cleanup"); var oldWarlord = warlord; var oldPlayer = input.PlayerEntity;
            yield return Tap(Key.F6); warlord = Actor(input, "SnapjawWarlord"); sentry = Actor(input, "SkeletalSentry");
            _bench.Check("native_F6_restores_fresh_healthy_graph_without_regrant", MessageLog.GetLast() == "Game loaded." && Restored(input)
                && !ReferenceEquals(oldPlayer, input.PlayerEntity) && !ReferenceEquals(oldWarlord, warlord) && warlord.GetPart<Body>().DismemberedParts.Count == 0
                && _warlordItemIDs.All(id => Equipped(warlord).Count(e => e.ID == id) == 1) && warlord.GetStat("Speed").Penalty == 5);

            var livingControls = _actorIDs.Where(id => id != warlord.ID).ToDictionary(id => id, id => ActorByID(input, id).GetStatValue("Hitpoints"));
            var deathItems = Equipped(warlord); boots = deathItems.Single(e => e.ID == _bootsID);
            CombatSystem.ApplyDamage(warlord, new Damage(1000), input.PlayerEntity, input.CurrentZone);
            _bench.Check("attributed_death_drops_each_personal_unit_once_and_keeps_neighbors_alive", CombatSystem.IsDeathHandled(warlord) && warlord.GetStatValue("Hitpoints") <= 0
                && input.CurrentZone.GetEntityCell(warlord) == null && !input.TurnManager.IsRegistered(warlord) && Owned(warlord).Length == 0
                && warlord.GetPart<Body>().GetParts().All(p => p._Equipped == null) && warlord.GetStat("Speed").Penalty == 0
                && deathItems.Length == 3 && deathItems.All(e => GroundAt(input, e, 21, 12))
                && livingControls.All(pair => Alive(input, ActorByID(input, pair.Key)) && ActorByID(input, pair.Key).GetStatValue("Hitpoints") == pair.Value));
            string growth = GrowthSignature(input.PlayerEntity); int deaths = DeathCount(warlord);
            CombatSystem.HandleDeath(warlord, sentry, input.CurrentZone);
            _bench.Check("repeat_death_cannot_duplicate_gear_or_rewards", DeathCount(warlord) == deaths && GrowthSignature(input.PlayerEntity) == growth
                && deathItems.All(e => GroundAt(input, e, 21, 12)) && _warlordItemIDs.All(id => input.CurrentZone.GetReadOnlyEntities().Count(e => e.ID == id) == 1));
            Record(input, "death_drops");

            yield return DrainAnnouncements(input);
            serial = MessageLog.NextSerialValue; yield return Tap(Key.RightArrow);
            Need(input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (21, 12), "Actual east walking must reach the cleared drop cell.");
            yield return Tap(Key.G); var pickup = input.PickupUI;
            Need(pickup.IsOpen, "Actual multiple-item pickup menu required.");
            var choices = Read(pickup, "_items") as List<Entity>; int desired = choices?.IndexOf(boots) ?? -1;
            Need(desired >= 0, "Exact dropped boots must appear in normal pickup choices.");
            int visible = desired - (int)Read(pickup, "_scrollOffset");
            Need(visible >= 0 && visible < 20, "Exact dropped boots must have a visible pickup shortcut.");
            char shown = ShownPickupKey(pickup, visible);
            Need(shown >= 'a' && shown <= 'z' && shown != 'g' && shown != 'j' && shown != 'k', "Actual displayed pickup label required.");
            yield return Tap((Key)((int)Key.A + shown - 'a'));
            if (pickup.IsOpen) yield return Tap(Key.Escape);
            _bench.Check("native_walk_G_picks_exact_dropped_boots_and_normally_autoequips", State(input) == "Normal" && EquippedBy(input.PlayerEntity, boots)
                && input.PlayerEntity.GetStat("Speed").Penalty == 5 && input.CurrentZone.GetEntityCell(boots) == null && EquipMessagesSince(serial) == 1
                && deathItems.Where(e => e != boots).All(e => GroundAt(input, e, 21, 12)));
            string pickedSignature = Signature(input.PlayerEntity);
            _bench.Check("stale_pickup_control_cannot_duplicate_or_steal_equipped_unit", !InventorySystem.Pickup(input.PlayerEntity, boots, input.CurrentZone)
                && Signature(input.PlayerEntity) == pickedSignature && EquippedBy(input.PlayerEntity, boots) && Quantity(boots) == 1);
            oldPlayer = input.PlayerEntity; yield return Tap(Key.F6);
            _bench.Check("final_native_F6_restores_healthy_owner_and_removes_drop_duplicates", MessageLog.GetLast() == "Game loaded." && Restored(input)
                && !ReferenceEquals(oldPlayer, input.PlayerEntity) && Owned(input.PlayerEntity).Length == 0 && Actor(input, "SnapjawWarlord").GetStat("Speed").Penalty == 5
                && _warlordItemIDs.All(id => input.CurrentZone.GetReadOnlyEntities().All(e => e.ID != id)) && _healthyBytes.SequenceEqual(File.ReadAllBytes(QuickPath())));
            _bench.Check("all_checkpoints_stay_in_private_disposable_save_scope", SaveGameService.SaveRootOverride == _root && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID
                && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2 && _markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz")))
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID)) && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _markerID)));
            Record(input, "final_recovery");
        }
        private void ObserveProfile(InputHandler input, string zoneID, params string[] roles)
        {
            var zone = input.ZoneManager.GetZone(zoneID); Need(zone != null, "Actual named profile zone unavailable: " + zoneID);
            foreach (string role in roles)
            {
                var actors = zone.GetReadOnlyEntities().Where(a => a.BlueprintName == role).ToArray();
                Need(actors.Length > 0 && actors.All(a => GuaranteedKit(a)), "Actual profile actor/loadout missing: " + zoneID + "/" + role);
                Need(role != "PeatCutter" || actors.Length == 2, "Actual Boatyard stamp must provide both PeatCutters.");
                foreach (var actor in actors) _profiles.Add(new ProfileActor { zoneID = zoneID, actorID = actor.ID, blueprint = role, signature = Signature(actor) });
            }
        }
        private void BuildArena(InputHandler input)
        {
            var player = input.PlayerEntity; var zone = input.CurrentZone; var turns = input.TurnManager;
            ConversationManager.EndConversation(); DragSystem.Release(player); player.GetPart<Body>().DropAllEquipment(zone);
            var inventory = player.GetPart<InventoryPart>(); foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            Need(inventory.EquippedItems.Count == 0, "Player initial equipment must clean normally before arena setup.");
            inventory.MaxWeight = 150; inventory.RefreshHandlingCarryPenalty(); player.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
            foreach (var entity in zone.GetAllEntities().ToArray()) if (entity != player) { turns.RemoveEntity(entity); zone.RemoveEntity(entity); }
            zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            { zone.TileState.Clear(x, y); if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1) Place(input.EntityFactory.CreateEntity("StoneFloor"), x, y); }
            zone.RemoveEntity(player); Place(player, 20, 12);
            var speed = player.GetStat("Speed"); speed.BaseValue = speed.Max = 100; speed.Min = 0; speed.Bonus = speed.Penalty = speed.Boost = 0;
            player.SetIntProperty("MobilityPenalty", 0, removeIfZero: true);
            for (int i = 0; i < _created.Length; i++)
            {
                var actor = _created[i]; var brain = actor.GetPart<BrainPart>(); if (brain != null) actor.RemovePart(brain);
                int x = actor.BlueprintName == "SnapjawWarlord" ? 21 : actor.BlueprintName == "SkeletalSentry" ? 22 : 30 + (i % 8) * 3;
                int y = actor.BlueprintName == "SnapjawWarlord" ? 12 : actor.BlueprintName == "SkeletalSentry" ? 13 : 6 + (i / 8) * 5;
                Place(actor, x, y); turns.AddEntity(actor);
            }
            var target = input.EntityFactory.CreateEntity("Villager"); Need(target != null, "Actual Villager target unavailable.");
            var targetBrain = target.GetPart<BrainPart>(); if (targetBrain != null) target.RemovePart(targetBrain);
            target.GetStat("Hitpoints").Max = target.GetStat("Hitpoints").BaseValue = 100;
            target.GetStat("Agility").BaseValue = 16; target.GetPart<ArmorPart>().AV = 6;
            Place(target, 22, 12); turns.AddEntity(target); _targetID = target.ID;
            turns.AddEntity(player); turns.ProcessUntilPlayerTurn(); ZoneRenderHooks.MarkFullDirty("GameAuditEquipmentContentBench");
            void Place(Entity entity, int x, int y) { Need(entity != null && zone.AddEntity(entity, x, y), "Content arena placement refused."); }
        }
        private bool NaturalAttack(InputHandler input, Entity actor, string dice, int pen, string extraAttribute)
        {
            var target = ActorByID(input, _targetID); int hp = target.GetStatValue("Hitpoints");
            var hands = actor.GetPart<Body>().GetParts().Where(p => p.Type == "Hand").ToArray();
            Need(hands.Length == 2 && hands.All(p => p._Equipped == null), "Preserved natural weapon actors must have both empty hands.");
            var natural = hands[0]._DefaultBehavior?.GetPart<MeleeWeaponPart>();
            int weight = target.GetPart<Body>().GetParts().Where(p => !p.Abstract && p.TargetWeight > 0).Sum(p => p.TargetWeight);
            Need(target.GetPart<Body>().GetParts()[0].Type == "Body" && !target.GetPart<Body>().GetParts()[0].IsSeverable(), "Finite root hit-location control required.");
            Need((actor.GetPart<SkillsPart>()?.SkillList.Count ?? 0) == 0, "Natural-attack native fixture expects no authored skills.");
            var rng = new AttackRolls(weight, dice == "2d5"); bool attempted = CombatSystem.PerformMeleeAttack(actor, target, input.CurrentZone, rng);
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "DamageRoll", Actor = actor.ID, Target = target.ID, Limit = 20 }).Records;
            var roll = records.Count == 1 ? JsonUtility.FromJson<DamageRoll>(records[0].PayloadJson) : null;
            _stages.Add(new Stage { label = "natural_attack_" + actor.BlueprintName, tick = input.TurnManager.TickCount, rngCalls = rng.Calls.ToArray(), actorSignatures = new[] { Signature(actor) } });
            return attempted && natural != null && natural.BaseDamage == dice && natural.PenBonus == pen && natural.Attributes.Contains("Cutting")
                && (extraAttribute == null || natural.Attributes.Contains(extraAttribute)) && roll != null && roll.damageDice == dice && !roll.naturalTwenty
                && rng.Remaining == 0 && roll.penetrationsRolled == 1 && roll.baseDamageTotal == (dice == "2d5" ? 10 : 7) && roll.attributes.Contains("Cutting") && (extraAttribute == null || roll.attributes.Contains(extraAttribute))
                && target.GetStatValue("Hitpoints") == hp - roll.baseDamageTotal && Alive(input, target) && target.GetPart<Body>().DismemberedParts.Count == 0 && Alive(input, actor);
        }
        private bool Restored(InputHandler input) => State(input) == "Normal" && !Dead(input) && ReferenceEquals(input.CurrentZone, input.ZoneManager.ActiveZone)
            && ReferenceEquals(input.TurnManager, TurnManager.Active) && ReferenceEquals(input.TurnManager.CurrentActor, input.PlayerEntity) && input.TurnManager.IsRegistered(input.PlayerEntity)
            && input.TurnManager.TickCount == _savedTick && input.TurnManager.GetEnergy(input.PlayerEntity) == _savedEnergy && input.PlayerEntity.GetIntProperty("GA03iCheckpoint") == 1
            && _actorIDs.Select((id, i) => Signature(ActorByID(input, id)) == _healthySignatures[i] && Ownership(ActorByID(input, id)) && Alive(input, ActorByID(input, id))).All(x => x)
            && ProfilesIntact(input);
        private bool ProfilesIntact(InputHandler input) => _profiles.All(saved => input.ZoneManager.CachedZones.TryGetValue(saved.zoneID, out var zone)
            && zone.GetReadOnlyEntities().Any(a => a.ID == saved.actorID && a.BlueprintName == saved.blueprint && Signature(a) == saved.signature && GuaranteedKit(a)));
        private static bool ExactKit(Entity actor, Kit kit) => actor != null && actor.BlueprintName == kit.Actor && actor.GetPart<LoadoutPart>() != null
            && Equipped(actor).Select(e => e.BlueprintName).OrderBy(s => s).SequenceEqual(kit.Equipped.OrderBy(s => s))
            && Equipped(actor).All(e => Quantity(e) == 1) && kit.Carried.All(bp => actor.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == bp && Quantity(e) == 1))
            && (actor.GetPart<TraderPart>() != null || actor.GetPart<InventoryPart>().Objects.Count == kit.Carried.Length);
        private static bool GuaranteedKit(Entity actor)
        {
            var kit = Kits.Single(k => k.Actor == actor.BlueprintName); var loadout = actor.GetPart<LoadoutPart>();
            if (loadout == null) return false;
            // These roles have no optional equipped entries. World builders can
            // add/merge legitimate carried stock, including a PeatCutter's Dagger.
            return Equipped(actor).Select(e => e.BlueprintName).OrderBy(s => s).SequenceEqual(kit.Equipped.OrderBy(s => s))
                && Equipped(actor).All(e => Quantity(e) == 1) && kit.Carried.All(bp => actor.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == bp && Quantity(e) > 0))
                && Ownership(actor);
        }
        private static bool StockIntact(Entity actor)
        {
            var trader = actor.GetPart<TraderPart>(); if (trader == null) return true;
            var inventory = actor.GetPart<InventoryPart>();
            return inventory.Objects.Count > 0 && TradeSystem.GetDrams(actor) > 0 && actor.GetProperty(TraderRestockSystem.ShopStockTableProp) == trader.StockTable
                && inventory.Objects.All(e => CarriedBy(actor, e)) && Equipped(actor).All(e => !inventory.Objects.Contains(e));
        }
        private static bool Ownership(Entity actor) => Equipped(actor).All(e => EquippedBy(actor, e)) && actor.GetPart<InventoryPart>().Objects.All(e => CarriedBy(actor, e))
            && Owned(actor).All(e => !string.IsNullOrWhiteSpace(e.ID) && Quantity(e) > 0);
        private static Entity[] Equipped(Entity actor) => actor.GetPart<InventoryPart>().EquippedItems.Values.Distinct().ToArray();
        private static Entity[] Owned(Entity actor) => actor.GetPart<InventoryPart>().Objects.Concat(Equipped(actor)).Distinct().ToArray();
        private static bool EquippedBy(Entity actor, Entity item)
        {
            var inventory = actor.GetPart<InventoryPart>(); var slots = actor.GetPart<Body>().GetParts().Where(p => p._Equipped == item).ToArray();
            return slots.Length == 1 && slots[0].FirstSlotForEquipped && inventory.EquippedItems.TryGetValue(slots[0].ID.ToString(), out var cached) && cached == item
                && inventory.EquippedItems.Values.Count(e => e == item) == 1 && !inventory.Objects.Contains(item)
                && item.GetPart<PhysicsPart>().Equipped == actor && item.GetPart<PhysicsPart>().InInventory == null && Quantity(item) == 1;
        }
        private static bool CarriedBy(Entity actor, Entity item) => actor.GetPart<InventoryPart>().Objects.Count(e => e == item) == 1
            && !actor.GetPart<InventoryPart>().EquippedItems.Values.Contains(item) && item.GetPart<PhysicsPart>().InInventory == actor && item.GetPart<PhysicsPart>().Equipped == null && Quantity(item) > 0;
        private static bool Alive(InputHandler input, Entity actor) => actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
            && input.CurrentZone.GetEntityCell(actor) != null && input.TurnManager.IsRegistered(actor);
        private static bool GroundAt(InputHandler input, Entity item, int x, int y) => input.CurrentZone.GetEntityPosition(item) == (x, y)
            && input.CurrentZone.GetEntityCell(item)?.Objects.Count(e => e == item) == 1 && input.CurrentZone.GetReadOnlyEntities().Count(e => e.ID == item.ID) == 1
            && item.GetPart<PhysicsPart>().InInventory == null && item.GetPart<PhysicsPart>().Equipped == null && Quantity(item) == 1;
        private static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        private static int PartAV(Entity actor, string type) => CombatSystem.GetPartAV(actor, actor.GetPart<Body>().GetParts().First(p => p.Type == type));
        private Entity Actor(InputHandler input, string bp) => ActorByID(input, _actorIDs[Array.FindIndex(Kits, k => k.Actor == bp)]);
        private static Entity ActorByID(InputHandler input, string id) => input.CurrentZone.GetReadOnlyEntities().Single(e => e.ID == id);
        private static string Signature(Entity actor) => string.Join("|", actor.GetPart<InventoryPart>().Objects.Select(e => "C:" + e.ID + ":" + e.BlueprintName + ":" + Quantity(e))
            .Concat(Equipped(actor).Select(e => "E:" + e.ID + ":" + e.BlueprintName + ":" + Quantity(e))).OrderBy(s => s));
        private static int EquipMessagesSince(int serial) => MessageLog.GetAllEntries().Count(e => e.Serial >= serial && e.Text.Contains(" equips "));
        private static int EventCount(string kind, string actor) => DiagQuery.Count(new DiagQuery.Filter { Category = "event", Kind = kind, Actor = actor }).Count;
        private static int DeathCount(Entity actor) => DiagQuery.Count(new DiagQuery.Filter { Category = "damage", Kind = "DeathHandled", Target = actor.ID }).Count;
        private IEnumerator DrainAnnouncements(InputHandler input)
        {
            // Keep the real Warlord XP/level rewards; dismiss their actual modal notices.
            for (int i = 0; i < 8 && (input.AnnouncementUI.IsOpen || MessageLog.HasPendingAnnouncement); i++)
            { yield return new WaitForSecondsRealtime(.12f); if (input.AnnouncementUI.IsOpen) yield return Tap(Key.Enter); }
            Need(!input.AnnouncementUI.IsOpen && !MessageLog.HasPendingAnnouncement && State(input) == "Normal", "Real level-up notices must close before normal walking/pickup.");
        }
        private static string GrowthSignature(Entity actor) => actor.GetStatValue("Experience") + ":" + actor.GetStatValue("Level") + ":"
            + actor.GetStatValue("MP") + ":" + actor.GetStatValue("SP") + ":" + actor.GetStat("Hitpoints").Max;
        private static char ShownPickupKey(PickupUI ui, int visibleRow)
        {
            int x = (int)Read(ui, "_worldOriginX") + 2, y = (int)Read(ui, "_worldTopY") - 3 - visibleRow;
            var tile = ui.Tilemap.GetTile(new Vector3Int(x, y, 0));
            for (char c = 'a'; c <= 'z'; c++) if (tile == CP437TilesetGenerator.GetUiTile(c)) return c;
            return '\0';
        }
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private static bool Dead(InputHandler input) => (Read(input, "_deathScreenController") as DeathScreenController)?.IsActive ?? false;
        private static void Need(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        private string QuickPath()
        { Need(!string.IsNullOrWhiteSpace(_root) && !string.IsNullOrWhiteSpace(_freshID) && _freshID.IndexOfAny(new[] { '/', '\\' }) < 0 && SaveGameService.SaveRootOverride == _root, "Owned native save path unavailable."); return Path.Combine(_root, _freshID, "Quick.sav.gz"); }
        private void Queue(params Key[] keys) { if (_keyboard != null) InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
        private IEnumerator Tap(Key key) { Queue(key); yield return new WaitForSecondsRealtime(.06f); Queue(); yield return new WaitForSecondsRealtime(.2f); }
        private void Record(InputHandler input, string label)
        { _stages.Add(new Stage { label = label, tick = input.TurnManager.TickCount, playerID = input.PlayerEntity.ID, playerSignature = Signature(input.PlayerEntity), actorSignatures = _actorIDs.Select(id => input.CurrentZone.GetReadOnlyEntities().Any(a => a.ID == id) ? Signature(ActorByID(input, id)) : "GONE:" + id).ToArray() }); }
        public void SetUnexpectedErrors(int count) { _errors = count; if (_report != null) WriteReport(); }
        private void Finish()
        {
            if (_performanceMode != null) { FinishPerformance(); return; }
            _report = new Report { runId = _bench.RunId, root = _root, freshID = _freshID, markerID = _markerID, seconds = Now,
                canVerify = "Native N/F5/F6 and exact normal walk/G pickup; actual unmodified 24 actor blueprints through live factory, real starting-village/named-profile producers, actual stock, per-item loadout diagnostics and quiet creation, actual full natural attacks, location armor, normal/veto/forced cleanup, attributed death, exact compressed graph identities and owner links.",
                cannotVerify = "No random encounter-frequency, complete balance, enemy AI, sprite/art appearance, keyboard combat/dismemberment, visual/feel, frame allocation, performance or speedup claim. The sprite phase is separate.",
                fixtureBounds = "Both content RNG overrides are temporary and restored; no actual blueprint mutation or injected Loadout, stock deletion, private factory, mod or new renderer key. After observing real world producers the starting zone becomes a flat arena; only arena NPC Brain Parts are removed, combat target HP/AV/Agility and player speed/storage are explicit fixture settings. Controlled valid-range combat RNG avoids criticals/procs/offhand. Injury/death are explicit APIs. The temporary veto is removed before save. NativeSaveIsolation owns all save destinations through teardown; process code is selected before final teardown report." };
            WriteReport(); Debug.Log("[GameAuditEquipmentContentBench] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private void WriteReport()
        { _report.cases = _bench.Cases; _report.failures = Failures; _report.unexpectedErrors = _errors; _report.audit = _bench.Audit.ToArray(); _report.stages = _stages.ToArray(); _report.profiles = _profiles.ToArray(); Directory.CreateDirectory(ReportDirectory); File.WriteAllText(Path.Combine(ReportDirectory, _performanceMode == null ? "GA03i-native.json" : PerfPrefix + "-perf-native.json"), JsonUtility.ToJson(_report, true)); }
        private void OnDestroy()
        {
            DisposePerformanceRecorders();
            try
            {
                if (_report != null)
                {
                    const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic; bool held = SaveGameService.SaveRootOverride == _root;
                    bool unregistered = typeof(SaveGameService).GetField("_captureCurrent", flags).GetValue(null) == null && typeof(SaveGameService).GetField("_applyLoaded", flags).GetValue(null) == null;
                    _report.shutdownObserved = true; _report.shutdownSeconds = Now; _report.shutdownRootHeld = held; _report.shutdownSavingUnregistered = unregistered;
                    if (!held || !unregistered) _fatal++; WriteReport();
                }
            }
            finally { if (_keyboard != null) InputSystem.RemoveDevice(_keyboard); if (_oldSettings != null) InputSystem.settings = _oldSettings; if (_settings != null) Destroy(_settings); Application.runInBackground = _oldBackground; Diag.SetChannel("event", _oldEvent); Diag.SetChannel("damage", _oldDamage); _clock?.Stop(); }
        }

        // Both modes use current natural initialization and identical content/setup.
        // Only the externally verified occupied-secondary-hand guard differs.
        private IEnumerator PerformanceAudit()
        {
            _performanceStarted = true;
            _perfInput = FindFirstObjectByType<InputHandler>();
            _perfBoot = Read(_perfInput, "_bootMenuController") as BootMenuController;
            _perfDeath = Read(_perfInput, "_deathScreenController") as DeathScreenController;
            Need(_perfInput != null && _perfBoot != null && _perfBoot.IsActive && _perfDeath != null
                && SaveGameService.SaveRootOverride == _root, "Owned boot/combat profile context required.");
            _markerBytes = File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz"));
            yield return Tap(Key.N); _freshID = SaveGameService.GetSaveInfo("Quick")?.GameID;
            Need(_freshID != null && _freshID != _markerID && File.Exists(QuickPath()) && !_perfBoot.IsActive, "Native N did not bind a fresh owned game.");
            BuildPerformanceArena();
            PerfHealthyBoundary();
            // Warm public combat before observing; all rolls are legal guaranteed misses.
            for (int i = 0; i < 4; i++) { PerfAttack(1, false); PerfAttack(2, false); yield return null; }
            yield return new WaitForSecondsRealtime(.5f); PerfHealthyBoundary();
            _perfSamples = new long[_perfMarkers.Length][];
            _perfRecorders = new ProfilerRecorder[_perfMarkers.Length];
            _perfRecorderValid = new bool[_perfMarkers.Length];
            _perfFrameTimes = new double[PerfFrameCapacity]; _perfFrameMilliseconds = new double[PerfFrameCapacity];
            _perfFramePhases = new int[PerfFrameCapacity]; _perfStrikes = new PerfStrike[PerfStrikeCapacity];
            var options = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame;
            for (int i = 0; i < _perfMarkers.Length; i++)
            {
                _perfSamples[i] = new long[PerfFrameCapacity];
                _perfRecorders[i] = ProfilerRecorder.StartNew(i == _perfMarkers.Length - 1 ? ProfilerCategory.Memory : ProfilerCategory.Scripts,
                    _perfMarkers[i], 2, options);
                _perfRecorderValid[i] = _perfRecorders[i].Valid;
            }
            Need(_perfRecorderValid.All(value => value), "A required native combat profile marker is unavailable.");
            yield return null; yield return null;
            _perfStart = Now;
            for (int phase = 0; phase < _perfPhases.Length; phase++)
            {
                PerfBeginPhase(phase);
                while (Now - _perfPhaseStart < PerfPhaseSeconds)
                {
                    if (phase == 0) { yield return null; continue; }
                    PerfAttack(phase, true);
                    // Cadence is controlled outside the timed melee call. No waits/arrays are
                    // allocated per strike by this observer; engine gameplay still may allocate.
                    double resume = Now + PerfStrikeSpacing;
                    while (Now < resume) yield return null;
                }
                PerfEndPhase(); yield return null;
            }
            Queue(); PerfHealthyBoundary();
            Need(!_perfOverflow && _perfFrames > 300 && _perfFrames < PerfFrameCapacity
                && _perfPhases.All(p => p.seconds >= PerfPhaseSeconds && p.seconds < PerfPhaseSeconds + 3 && p.frames > 50 && p.failed == 0)
                && _perfPhases[0].accepted == 0 && _perfPhases[1].accepted >= 200 && _perfPhases[2].accepted >= 200
                && _perfPhases[1].accepted == _perfPhases[1].requests && _perfPhases[2].accepted == _perfPhases[2].requests,
                "Finite 75-second healthy combat workload was incomplete.");
            for (int phase = 0; phase < 3; phase++)
            {
                bool anyCombat = false;
                for (int frame = 0; frame < _perfFrames; frame++) if (_perfFramePhases[frame] == phase)
                {
                    if (_perfSamples[0][frame] > 0) anyCombat = true;
                    Need(_perfSamples[3][frame] == 0 && _perfSamples[4][frame] == 0, "This API/miss profile unexpectedly advanced turns or called ApplyDamage.");
                }
                Need(anyCombat == (phase != 0), "Combat recorder activity does not match its idle/attack phase.");
            }
            Need(_perfSamples[1].Take(_perfFrames).Any(value => value > 0), "Live Input.Update context marker is empty.");
            Need(SaveGameService.SaveRootOverride == _root && _markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz")))
                && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID))
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _markerID)), "Private combat profile save isolation changed.");
            _performanceCompleted = true;
        }
        private void BuildPerformanceArena()
        {
            _perfSpectator = _perfInput.PlayerEntity; _perfZone = _perfInput.CurrentZone; _perfTurns = _perfInput.TurnManager;
            Need(ReferenceEquals(_perfInput.EntityFactory, LoadoutPart.Factory) && ReferenceEquals(_perfInput.EntityFactory, TraderPart.Factory), "Live production factory wiring required.");
            ConversationManager.EndConversation(); DragSystem.Release(_perfSpectator);
            foreach (var entity in _perfZone.GetAllEntities().ToArray()) if (entity != _perfSpectator)
            { _perfTurns.RemoveEntity(entity); Need(_perfZone.RemoveEntity(entity), "Profile arena removal failed."); }
            _perfZone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                _perfZone.TileState.Clear(x, y);
                if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1) Place(_perfInput.EntityFactory.CreateEntity("StoneFloor"), x, y);
            }
            Need(_perfZone.RemoveEntity(_perfSpectator), "Spectator relocation failed."); Place(_perfSpectator, 20, 12);
            _perfOneHand = _perfInput.EntityFactory.CreateEntity("Player"); _perfTwoHands = _perfInput.EntityFactory.CreateEntity("Player");
            _perfTarget = _perfInput.EntityFactory.CreateEntity("Villager");
            Need(_perfOneHand != null && _perfTwoHands != null && _perfTarget != null, "Actual Player/Villager profile actors missing.");
            _perfOneHand.Tags.Remove("Player"); _perfTwoHands.Tags.Remove("Player");
            _perfOneHand.GetPart<RenderPart>().DisplayName = "profile short-sword bearer";
            _perfTwoHands.GetPart<RenderPart>().DisplayName = "profile greatsword bearer";
            _perfTarget.GetPart<RenderPart>().DisplayName = "profile missed target";
            foreach (var actor in new[] { _perfOneHand, _perfTwoHands, _perfTarget })
            {
                var brain = actor.GetPart<BrainPart>(); if (brain != null) actor.RemovePart(brain);
                Need((actor.GetPart<SkillsPart>()?.SkillList.Count ?? 0) == 0 && (actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Count ?? 0) == 0,
                    "Miss-only profile actors must begin without skills/status effects.");
            }
            _perfSword = _perfInput.EntityFactory.CreateEntity("ShortSword"); _perfGreatsword = _perfInput.EntityFactory.CreateEntity("Greatsword");
            Need(_perfSword != null && _perfGreatsword != null && _perfOneHand.GetPart<InventoryPart>().AddObject(_perfSword)
                && _perfTwoHands.GetPart<InventoryPart>().AddObject(_perfGreatsword)
                && InventorySystem.Equip(_perfOneHand, _perfSword) && InventorySystem.Equip(_perfTwoHands, _perfGreatsword), "Normal profile weapon equip failed.");
            Place(_perfOneHand, 21, 11); Place(_perfTwoHands, 21, 13); Place(_perfTarget, 22, 12);
            _perfTurns.AddEntity(_perfOneHand); _perfTurns.AddEntity(_perfTwoHands); _perfTurns.AddEntity(_perfTarget);
            _perfTurns.AddEntity(_perfSpectator); _perfTurns.ProcessUntilPlayerTurn();
            _perfSpectatorHP = _perfSpectator.GetStat("Hitpoints"); _perfOneHP = _perfOneHand.GetStat("Hitpoints");
            _perfTwoHP = _perfTwoHands.GetStat("Hitpoints"); _perfTargetHP = _perfTarget.GetStat("Hitpoints");
            Need(_perfSpectatorHP != null && _perfOneHP != null && _perfTwoHP != null && _perfTargetHP != null, "Actual actor HP stats missing.");
            _perfSpectatorHpValue = _perfSpectatorHP.Value; _perfOneHpValue = _perfOneHP.Value; _perfTwoHpValue = _perfTwoHP.Value; _perfTargetHpValue = _perfTargetHP.Value;
            _perfTick = _perfTurns.TickCount; _perfEnergy = _perfTurns.GetEnergy(_perfSpectator);
            Need(_perfSword.GetPart<MeleeWeaponPart>().BaseDamage == "1d6" && _perfGreatsword.GetPart<MeleeWeaponPart>().BaseDamage == "1d12"
                && _perfGreatsword.GetPart<EquippablePart>().UsesSlots == "Hand,Hand", "Actual one/two-hand weapon content changed.");
            foreach (var actor in new[] { _perfOneHand, _perfTwoHands })
            {
                var weapon = ReferenceEquals(actor, _perfOneHand) ? _perfSword : _perfGreatsword;
                Need(CombatSystem.GetDV(_perfTarget) > 1 + StatUtils.GetModifier(actor, "Agility") + weapon.GetPart<MeleeWeaponPart>().HitBonus,
                    "Legal d20=1 must miss without invoking damage/penetration.");
            }
            ZoneRenderHooks.MarkFullDirty("GameAuditEquipmentContentBench.Performance");
            void Place(Entity entity, int x, int y) { Need(entity != null && _perfZone.AddEntity(entity, x, y), "Combat profile placement failed."); }
        }
        private void PerfHealthyBoundary()
        {
            Need(ReferenceEquals(_perfInput.PlayerEntity, _perfSpectator) && ReferenceEquals(_perfInput.CurrentZone, _perfZone)
                && ReferenceEquals(_perfInput.ZoneManager.ActiveZone, _perfZone) && ReferenceEquals(_perfInput.TurnManager, _perfTurns)
                && ReferenceEquals(TurnManager.Active, _perfTurns) && ReferenceEquals(_perfTurns.CurrentActor, _perfSpectator)
                && _perfTurns.WaitingForInput && _perfTurns.TickCount == _perfTick && _perfTurns.GetEnergy(_perfSpectator) == _perfEnergy
                && !_perfBoot.IsActive && !_perfDeath.IsActive && !SpellFxSettingsPanel.IsOpen && State(_perfInput) == "Normal"
                && !_perfInput.AnnouncementUI.IsOpen && !MessageLog.HasPendingAnnouncement, "Healthy idle spectator/clock context changed.");
            Need(_perfSpectatorHP.Value == _perfSpectatorHpValue && _perfOneHP.Value == _perfOneHpValue
                && _perfTwoHP.Value == _perfTwoHpValue && _perfTargetHP.Value == _perfTargetHpValue
                && _perfSpectatorHpValue > 0 && _perfOneHpValue > 0 && _perfTwoHpValue > 0 && _perfTargetHpValue > 0
                && _perfZone.GetEntityPosition(_perfSpectator) == (20, 12) && _perfZone.GetEntityPosition(_perfOneHand) == (21, 11)
                && _perfZone.GetEntityPosition(_perfTwoHands) == (21, 13) && _perfZone.GetEntityPosition(_perfTarget) == (22, 12), "Combat profile changed health or coordinates.");
            foreach (var actor in new[] { _perfSpectator, _perfOneHand, _perfTwoHands, _perfTarget })
                Need(_perfTurns.IsRegistered(actor) && !CombatSystem.IsDeathHandled(actor) && (actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Count ?? 0) == 0,
                    "Combat profile actor died, deregistered, or accumulated an effect.");
            var one = _perfOneHand.GetPart<Body>().GetPartsByType("Hand"); var two = _perfTwoHands.GetPart<Body>().GetPartsByType("Hand");
            Need(one.Count == 2 && two.Count == 2 && one.All(p => p._DefaultBehavior?.GetPart<MeleeWeaponPart>()?.BaseDamage == "1d2" && p.FirstSlotForDefaultBehavior)
                && two.All(p => p._DefaultBehavior?.GetPart<MeleeWeaponPart>()?.BaseDamage == "1d2" && p.FirstSlotForDefaultBehavior)
                && one.Count(p => p._Equipped == _perfSword && p.FirstSlotForEquipped) == 1 && one.Count(p => p._Equipped == null) == 1
                && two.All(p => p._Equipped == _perfGreatsword) && two.Count(p => p.FirstSlotForEquipped) == 1
                && _perfGreatsword.GetPart<PhysicsPart>().Equipped == _perfTwoHands && _perfSword.GetPart<PhysicsPart>().Equipped == _perfOneHand
                && !_perfOneHand.GetPart<InventoryPart>().Objects.Contains(_perfSword) && !_perfTwoHands.GetPart<InventoryPart>().Objects.Contains(_perfGreatsword),
                "Real natural defaults or normal one/two-hand ownership changed.");
        }
        private void PerfBeginPhase(int phase)
        {
            PerfHealthyBoundary(); _perfPhase = phase; _perfPhaseStart = Now;
            var p = _perfPhases[phase]; p.startSeconds = Now - _perfStart; p.startTick = _perfTurns.TickCount; p.startEnergy = _perfTurns.GetEnergy(_perfSpectator);
            _perfMeasuring = true;
        }
        private void PerfEndPhase()
        {
            _perfMeasuring = false; var p = _perfPhases[_perfPhase]; p.seconds = Now - _perfPhaseStart;
            p.endTick = _perfTurns.TickCount; p.endEnergy = _perfTurns.GetEnergy(_perfSpectator); PerfHealthyBoundary();
        }
        private void PerfAttack(int phase, bool measured)
        {
            var actor = phase == 1 ? _perfOneHand : _perfTwoHands;
            int expectedSwings = phase == 1 || _performanceMode == "before" ? 2 : 1;
            _perfRng.Reset(expectedSwings);
            if (measured) _perfPhases[phase].requests++;
            int tick = _perfTurns.TickCount, serial = MessageLog.NextSerialValue, hp = _perfTargetHP.Value;
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            bool attempted = CombatSystem.PerformMeleeAttack(actor, _perfTarget, _perfZone, _perfRng);
            long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
            bool valid = attempted && _perfRng.Complete && _perfRng.HitRolls == expectedSwings && _perfRng.ChanceRolls == expectedSwings - 1
                && MessageLog.NextSerialValue - serial == expectedSwings && _perfTargetHP.Value == hp && hp == _perfTargetHpValue
                && _perfOneHP.Value == _perfOneHpValue && _perfTwoHP.Value == _perfTwoHpValue && _perfTurns.TickCount == tick
                && _perfTurns.GetEnergy(_perfSpectator) == _perfEnergy && !CombatSystem.IsDeathHandled(actor) && !CombatSystem.IsDeathHandled(_perfTarget)
                && (actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Count ?? 0) == 0 && (_perfTarget.GetPart<StatusEffectsPart>()?.GetAllEffects().Count ?? 0) == 0;
            if (measured)
            {
                var p = _perfPhases[phase]; if (attempted) p.accepted++; if (!valid) p.failed++;
                p.hitRolls += _perfRng.HitRolls; p.offhandRolls += _perfRng.ChanceRolls; p.missMessages += MessageLog.NextSerialValue - serial;
                Need(_perfStrikeCount < PerfStrikeCapacity, "Combat profile strike buffer exhausted.");
                _perfStrikes[_perfStrikeCount++] = new PerfStrike { phase = phase, seconds = Now - _perfStart,
                    methodNanoseconds = elapsed * (1000000000.0 / System.Diagnostics.Stopwatch.Frequency), expectedSwings = expectedSwings,
                    hitRolls = _perfRng.HitRolls, offhandRolls = _perfRng.ChanceRolls, rngCalls = _perfRng.Calls,
                    hpBefore = hp, hpAfter = _perfTargetHP.Value, tickBefore = tick, tickAfter = _perfTurns.TickCount, valid = valid };
            }
            Need(valid, "Healthy combat profile did not follow its exact declared miss/secondary-hand path.");
        }
        private void LateUpdate()
        {
            if (!_perfMeasuring || _perfRecorders == null) return;
            if (_perfFrames >= PerfFrameCapacity) { _perfOverflow = true; _perfMeasuring = false; return; }
            _perfFrameTimes[_perfFrames] = Now - _perfStart; _perfFramePhases[_perfFrames] = _perfPhase;
            _perfFrameMilliseconds[_perfFrames] = Time.unscaledDeltaTime * 1000.0; _perfPhases[_perfPhase].frames++;
            for (int i = 0; i < _perfRecorders.Length; i++)
            {
                if (!_perfRecorders[i].Valid) _perfRecorderValid[i] = false;
                _perfSamples[i][_perfFrames] = _perfRecorders[i].Valid ? _perfRecorders[i].LastValue : -1;
            }
            _perfFrames++;
        }
        private void DisposePerformanceRecorders()
        {
            _perfMeasuring = false; if (_perfRecorders == null) return;
            for (int i = 0; i < _perfRecorders.Length; i++)
            { if (_perfRecorderValid != null) _perfRecorderValid[i] &= _perfRecorders[i].Valid; _perfRecorders[i].Dispose(); }
            _perfRecorders = null;
        }
        private void FinishPerformance()
        {
            var metrics = new List<PerfMetric>();
            for (int phase = 0; phase < 3; phase++)
            {
                int p = phase; var indices = Enumerable.Range(0, _perfFrames).Where(f => _perfFramePhases[f] == p).ToArray();
                for (int marker = 0; marker < _perfMarkers.Length; marker++)
                {
                    int m = marker; var values = indices.Select(f => (double)_perfSamples[m][f]).OrderBy(v => v).ToArray();
                    metrics.Add(PerfSummarize(_perfPhases[p].name, _perfMarkers[m], m == _perfMarkers.Length - 1 ? "bytes" : "nanoseconds", values,
                        _perfRecorderValid != null && _perfRecorderValid[m] && values.All(v => v >= 0)));
                }
                metrics.Add(PerfSummarize(_perfPhases[p].name, "Engine frame duration", "milliseconds", indices.Select(f => _perfFrameMilliseconds[f]).OrderBy(v => v).ToArray(), true));
                if (p != 0) metrics.Add(PerfSummarize(_perfPhases[p].name, "Observed full melee call", "nanoseconds",
                    Enumerable.Range(0, _perfStrikeCount).Where(i => _perfStrikes[i].phase == p).Select(i => _perfStrikes[i].methodNanoseconds).OrderBy(v => v).ToArray(), true));
            }
            bool valid = _performanceCompleted && !_perfOverflow && metrics.All(m => m.valid) && Failures == 0;
            if (_performanceCompleted && !valid && _fatal == 0) _fatal++;
            Directory.CreateDirectory(ReportDirectory);
            var csv = new StringBuilder("frame,seconds,phase,engine_frame_ms," + string.Join(",", _perfMarkers) + "\n");
            for (int frame = 0; frame < _perfFrames; frame++)
            {
                csv.Append(frame).Append(',').Append(PerfF(_perfFrameTimes[frame])).Append(',').Append(_perfPhases[_perfFramePhases[frame]].name).Append(',').Append(PerfF(_perfFrameMilliseconds[frame]));
                for (int m = 0; m < _perfMarkers.Length; m++) csv.Append(',').Append(_perfSamples[m][frame]); csv.Append('\n');
            }
            File.WriteAllText(Path.Combine(ReportDirectory, PerfPrefix + "-frames.csv"), csv.ToString());
            csv = new StringBuilder("strike,seconds,phase,method_nanoseconds,expected_swings,hit_rolls,offhand_rolls,rng_calls,target_hp_before,target_hp_after,tick_before,tick_after,valid\n");
            for (int i = 0; i < _perfStrikeCount; i++)
            {
                var s = _perfStrikes[i]; csv.Append(i).Append(',').Append(PerfF(s.seconds)).Append(',').Append(_perfPhases[s.phase].name).Append(',').Append(PerfF(s.methodNanoseconds))
                    .Append(',').Append(s.expectedSwings).Append(',').Append(s.hitRolls).Append(',').Append(s.offhandRolls).Append(',').Append(s.rngCalls)
                    .Append(',').Append(s.hpBefore).Append(',').Append(s.hpAfter).Append(',').Append(s.tickBefore).Append(',').Append(s.tickAfter).Append(',').Append(s.valid).Append('\n');
            }
            File.WriteAllText(Path.Combine(ReportDirectory, PerfPrefix + "-strikes.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(ReportDirectory, PerfPrefix + "-perf.json"), JsonUtility.ToJson(new PerfReport {
                runId = _bench.RunId, mode = _performanceMode, performanceOnly = true, functionalCases = 0, workloadValid = valid,
                frames = _perfFrames, strikes = _perfStrikeCount, overflow = _perfOverflow, measuredSeconds = _perfPhases.Sum(p => p.seconds), wallSeconds = _perfStart > 0 ? Now - _perfStart : 0,
                targetFrameRate = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount, strikeSpacing = PerfStrikeSpacing,
                spectatorID = _perfSpectator?.ID, oneHandActorID = _perfOneHand?.ID, twoHandActorID = _perfTwoHands?.ID, targetID = _perfTarget?.ID,
                oneHandWeaponID = _perfSword?.ID, twoHandWeaponID = _perfGreatsword?.ID, phases = _perfPhases, metrics = metrics.ToArray(),
                bounds = "Single uncapped native editor API-miss workload. Natural initialization is present in BOTH variants; only an externally verified occupied-secondary-hand guard differs. One-hand control has two miss swings and one offhand roll in both; two-hand before has two/one while after has one/zero. Behavior/work counts intentionally differ. Stopwatch measures the full public melee call including RNG, events, messages, diagnostics and FX publication; frame markers include renderer/editor/observer overhead and can be one completed frame shifted. Engine frame duration is wall time, not CPU. No isolated guard timing, statistical equivalence, full-wave speedup, landed-hit/rider/death timing, scheduler/input combat, build FPS, visual or feel claim." }, true));
            _report = new Report { runId = _bench.RunId, root = _root, freshID = _freshID, markerID = _markerID, seconds = Now,
                canVerify = "Performance-only " + _performanceMode + ": native N/private save bootstrap; real normally equipped ShortSword/Greatsword and factory natural defaults; legal finite full melee misses with explicit swing/RNG counters; unchanged HP/coordinates/turn clock; per-frame context/GC and per-call timing. Functional content cases are zero.",
                cannotVerify = "No functional 23-case content matrix in this branch; no isolated guard cost, full-wave speedup, equivalence, zero allocation, landed-hit/rider/death timing, NPC scheduling, physical keyboard combat, build FPS, sprite quality or feel.",
                fixtureBounds = "Actual Player/Villager/ShortSword/Greatsword via live factory; extra Player tags removed, names explicit, NPC brains removed and a flat arena installed. Both source actors retain factory default fists; weapons use normal Equip. All d20 rolls are1 and offhand rolls0 with exact finite overload/range checks. No damage, status accumulation, movement, save/load or turn advancement during capture. Source variant requires external provenance. Read perf-native teardown plus cleanup logs, not workloadValid/process code alone." };
            WriteReport(); Debug.Log("[GameAuditEquipmentContentBench Performance] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private static PerfMetric PerfSummarize(string phase, string name, string unit, double[] values, bool valid)
        {
            int count = values.Length;
            return new PerfMetric { phase = phase, name = name, unit = unit, valid = valid && count > 0, samples = count,
                max = count == 0 ? -1 : values[count - 1], p95 = count == 0 ? -1 : values[Math.Max(0, (int)Math.Ceiling(.95 * count) - 1)],
                p99 = count == 0 ? -1 : values[Math.Max(0, (int)Math.Ceiling(.99 * count) - 1)], mean = count == 0 ? -1 : values.Average() };
        }
        private static string PerfF(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
        private sealed class PerfMissRolls : System.Random
        {
            private int _expected, _stage;
            public int Calls { get; private set; }
            public int HitRolls { get; private set; }
            public int ChanceRolls { get; private set; }
            public bool Complete => _stage == (_expected == 1 ? 1 : 3);
            public void Reset(int expected) { Need(expected == 1 || expected == 2, "Invalid finite miss count."); _expected = expected; _stage = Calls = HitRolls = ChanceRolls = 0; }
            public override int Next(int maxValue)
            {
                Need(++Calls <= 3 && _expected == 2 && _stage == 1 && maxValue == 100, "Unexpected offhand RNG call/range.");
                _stage++; ChanceRolls++; return 0;
            }
            public override int Next(int minValue, int maxValue)
            {
                Need(++Calls <= 3 && minValue == 1 && maxValue == 21 && (_stage == 0 || (_expected == 2 && _stage == 2)), "Unexpected miss RNG call/range; no penetration/damage is allowed.");
                _stage++; HitRolls++; return 1;
            }
        }
        [Serializable] private sealed class PerfReport
        { public string runId, mode, bounds, spectatorID, oneHandActorID, twoHandActorID, targetID, oneHandWeaponID, twoHandWeaponID; public bool performanceOnly, workloadValid, overflow; public int functionalCases, frames, strikes, targetFrameRate, vSyncCount; public double measuredSeconds, wallSeconds, strikeSpacing; public PerfPhase[] phases; public PerfMetric[] metrics; }
        [Serializable] private sealed class PerfPhase
        { public string name; public int frames, requests, accepted, failed, hitRolls, offhandRolls, missMessages, startTick, endTick, startEnergy, endEnergy; public double startSeconds, seconds; }
        [Serializable] private sealed class PerfMetric
        { public string phase, name, unit; public bool valid; public int samples; public double max, p95, p99, mean; }
        private struct PerfStrike
        { public int phase, expectedSwings, hitRolls, offhandRolls, rngCalls, hpBefore, hpAfter, tickBefore, tickAfter; public double seconds, methodNanoseconds; public bool valid; }

        private sealed class ContentRolls : System.Random
        {
            private readonly bool _second;
            public ContentRolls(bool secondPick = false) { _second = secondPick; }
            public override int Next(int maxValue) { Need(maxValue > 0, "Unexpected content RNG bound."); return _second && maxValue == 2 ? 1 : 0; }
            public override int Next(int minValue, int maxValue) { Need(maxValue > minValue, "Unexpected content RNG range."); return minValue; }
        }
        /// <summary>Exact finite natural attack: noncrit root hit, one penetration,
        /// max damage dice, failed Cutting proc, failed secondary chance. No exploding10.</summary>
        private sealed class AttackRolls : System.Random
        {
            private readonly Queue<(bool single, int min, int max, int value)> _steps = new Queue<(bool, int, int, int)>();
            public readonly List<string> Calls = new List<string>();
            public int Remaining => _steps.Count;
            public AttackRolls(int weight, bool warlord)
            {
                Add(false, 1, 21, 10); Add(true, 0, weight, 0);
                Add(false, 1, 11, 9); Add(false, 1, 11, 1); Add(false, 1, 11, 1);
                if (warlord) { Add(false, 1, 6, 5); Add(false, 1, 6, 5); } else Add(false, 1, 7, 6);
                Add(true, 0, 100, 99); Add(true, 0, 100, 99);
            }
            private void Add(bool single, int min, int max, int value) => _steps.Enqueue((single, min, max, value));
            public override int Next(int maxValue) => Take(true, 0, maxValue);
            public override int Next(int minValue, int maxValue) => Take(false, minValue, maxValue);
            private int Take(bool single, int min, int max)
            {
                Need(_steps.Count > 0, "Unexpected extra natural-combat RNG call."); var step = _steps.Dequeue();
                Need(step.single == single && step.min == min && step.max == max && step.value >= min && step.value < max, "Natural-combat RNG overload/range drift: " + min + "," + max);
                Calls.Add((single ? "Next(" + max : "Next(" + min + "," + max) + ")=" + step.value); return step.value;
            }
        }
        [Serializable] private sealed class DamageRoll { public string damageDice, attributes; public int penetrationsRolled, baseDamageTotal; public bool naturalTwenty; }
        [Serializable] private sealed class ProfileActor { public string zoneID, actorID, blueprint, signature; }
        [Serializable] private sealed class Stage { public string label, playerID, playerSignature; public int tick; public string[] actorSignatures, rngCalls; }
        [Serializable] private sealed class Report { public string runId, root, freshID, markerID, canVerify, cannotVerify, fixtureBounds; public int cases, failures, unexpectedErrors; public double seconds, shutdownSeconds; public bool shutdownObserved, shutdownRootHeld, shutdownSavingUnregistered; public string[] audit; public Stage[] stages; public ProfileActor[] profiles; }
    }
}
