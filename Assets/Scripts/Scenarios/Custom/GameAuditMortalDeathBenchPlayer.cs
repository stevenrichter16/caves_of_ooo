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
    /// <summary>Full combat API stimuli plus queued N/F5/F6/L. No keyboard-combat,
    /// random encounter, visual, allocation, feel, speedup or scheduler-repair claim.</summary>
    public sealed class GameAuditMortalDeathBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => (_bench?.Failures ?? 0) + _fatal + _errors;
        private GameAuditMortalDeathBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldDamage;
        private int _fatal, _errors, _savedTick, _savedEnergy, _savedXp, _savedRep;
        private string _root, _markerID, _freshID, _enemyID, _victimID, _playerAxeID, _enemyAxeID, _bootsID;
        private byte[] _markerBytes, _healthyBytes;
        private System.Diagnostics.Stopwatch _clock;
        private Report _report;
        private readonly List<Stage> _stages = new List<Stage>();
        private double Now => _clock?.Elapsed.TotalSeconds ?? 0;
        private string ReportDirectory => Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit");

        // Performance-only branch; it deliberately contributes zero functional Check groups.
        private string _performanceMode;
        private bool _performanceStarted, _performanceCompleted, _perfMeasuring, _perfOverflow;
        private const int PerfFrameCapacity = 200000, PerfStepCapacity = 4000;
        private const double PerfPhaseSeconds = 25;
        private readonly string[] _perfMarkerNames = { "COO.Input.Update", "COO.Turns.Tick", "COO.Turns.EndTurn", "COO.Turns.ProcessUntilPlayerTurn", "COO.ZoneRenderer.LateUpdate", "GC Allocated In Frame" };
        private static readonly Key[] PerfKeys = { Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow };
        private static readonly int[] PerfDX = { 0, -1, 0, 1 }, PerfDY = { -1, 0, 1, 0 };
        private readonly PerfPhase[] _perfPhases = { new PerfPhase { name = "idle" }, new PerfPhase { name = "discrete" }, new PerfPhase { name = "held" } };
        private ProfilerRecorder[] _perfRecorders;
        private bool[] _perfRecorderValid;
        private long[][] _perfSamples;
        private double[] _perfTimes, _perfFrameMilliseconds;
        private int[] _perfFramePhases;
        private PerfStep[] _perfSteps;
        private int _perfFrames, _perfStepCount, _perfPhase, _perfLoopStep;
        private double _perfStart, _perfPhaseStart, _perfLastStep = -1;
        private InputHandler _perfInput;
        private Entity _perfPlayer, _perfEnemy, _perfVictim;
        private Zone _perfZone;
        private TurnManager _perfTurns;
        private Stat _perfHP, _perfSpeed;
        private BootMenuController _perfBoot;
        private DeathScreenController _perfDeath;
        private string PerfPrefix => "GA03h-" + _performanceMode;
        public void ConfigurePerformance(string mode)
        {
            Need(!_performanceStarted && (mode == "before" || mode == "after"), "Invalid or late performance configuration.");
            _performanceMode = mode;
        }

        public void Initialize(ScenarioContext ctx, GameAuditMortalDeathBench bench)
        {
            _bench = bench; _clock = System.Diagnostics.Stopwatch.StartNew();
            _root = SaveGameService.SaveRootOverride; _markerID = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _enemyID = bench.Enemy.ID; _victimID = bench.Victim.ID; _playerAxeID = bench.PlayerAxe.ID; _enemyAxeID = bench.EnemyAxe.ID; _bootsID = bench.Boots.ID;
            _oldDamage = Diag.IsChannelEnabled("damage"); Diag.SetChannel("damage", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings); _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
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
                if (!moved) { stack.Pop(); continue; } if (current is IEnumerator nested) { stack.Push(nested); continue; } yield return current;
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
            var player = input.PlayerEntity; var victim = Actor(input, _victimID); var enemy = Actor(input, _enemyID);
            _bench.Check("real_anatomy_axes_and_skill_ownership", Ready(input, player, victim) && Ready(input, enemy, player) && victim.GetStatValue("XPValue") == 10
                && victim.GetPart<CorpsePart>().CorpseChance == 100 && victim.GetPart<CorpsePart>().CorpseBlueprint == "CreatureCorpse" && ReferenceEquals(CorpsePart.Factory, input.EntityFactory));
            _bench.Check("positive_equipment_controls_before_death", HealthyGear(input) && player.GetStat("Speed").Penalty == 12 && !CombatSystem.IsDeathHandled(null));
            player.SetIntProperty("GA03hCheckpoint", 1); yield return Save(input); CaptureCheckpoint(input);
            _bench.Check("native_F5_writes_healthy_checkpoint", _healthyBytes.Length > 0 && player.GetIntProperty("GA03hCheckpoint") == 1 && _markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz"))));
            yield return new WaitForSecondsRealtime(.2f);
            _bench.Check("living_positive_hp_does_not_activate_modal", !Dead(input) && Head(player).ParentPart != null && player.GetStatValue("Hitpoints") == 100 && input.TurnManager.IsRegistered(player));

            var observation = Attack(input, player, victim, veto: true);
            _bench.Check("full_melee_veto_keeps_damage_but_refuses_mortality", HitWasTwo(observation, victim) && observation.Target.Before == 1 && observation.Target.Selected.Type == "Head"
                && observation.Target.BeforeHp == 98 && observation.Target.After == 0 && observation.Target.Died == 0 && victim.GetStatValue("Hitpoints") == 98
                && Head(victim).ParentPart != null && !CombatSystem.IsDeathHandled(victim) && NoBleed(victim) && observation.Target.Bleeds == 0
                && input.PlayerEntity.GetStatValue("Experience") == _savedXp && PlayerReputation.Get("Villagers") == _savedRep && Corpses(input, victim).Length == 0);
            Record(input, "veto_control", observation); yield return LoadHealthy(input);
            player = input.PlayerEntity; victim = Actor(input, _victimID);
            Need(player.GetPart<SkillsPart>().RemoveSkill(player.GetPart<SkillsPart>().GetSkill(nameof(Axe_Decapitate)), "GA03h-marker-control"), "Marker removal failed.");
            observation = Attack(input, player, victim);
            var bleed = victim.GetPart<StatusEffectsPart>().GetAllEffects().OfType<BleedingEffect>().SingleOrDefault();
            _bench.Check("without_decapitate_nonmortal_cut_still_bleeds", HitWasTwo(observation, victim) && observation.Target.Selected.Type == "Arm" && !observation.Target.Selected.Mortal
                && observation.Target.After == 1 && observation.Target.Died == 0 && victim.GetStatValue("Hitpoints") == 98 && input.CurrentZone.GetEntityCell(victim) != null
                && input.TurnManager.IsRegistered(victim) && Head(victim).ParentPart != null && bleed != null && bleed.SaveTarget == 35 && bleed.DamageDice == "1d2"
                && observation.Target.Bleeds == 1 && ReferenceEquals(observation.Target.BleedSource, player));
            Record(input, "nonmortal_control", observation); yield return LoadHealthy(input);
            player = input.PlayerEntity; victim = Actor(input, _victimID); observation = Attack(input, player, victim, chanceFails: true);
            _bench.Check("axe_chance_failure_has_only_nonlethal_hit", HitWasTwo(observation, victim) && victim.GetStatValue("Hitpoints") == 98 && victim.GetPart<Body>().DismemberedParts.Count == 0
                && observation.Target.Before == 0 && observation.Target.Died == 0 && NoBleed(victim) && input.TurnManager.IsRegistered(victim));
            Record(input, "chance_control", observation); yield return LoadHealthy(input);

            player = input.PlayerEntity; victim = Actor(input, _victimID); enemy = Actor(input, _enemyID); observation = Attack(input, player, victim);
            _bench.Check("full_axe_decapitation_commits_source_before_Died", HitWasTwo(observation, victim) && MortalDied(observation, player, victim, input.CurrentZone, expectedValue: 0));
            _bench.Check("head_loss_removes_victim_without_postmortem_bleed", GoneWithHead(input, victim, observation.Target.Selected) && NoBleed(victim) && observation.Target.Bleeds == 0
                && observation.DeathAfter == observation.DeathBefore + 1);
            _bench.Check("player_source_receives_exact_XP_and_reputation_once", player.GetStatValue("Experience") == _savedXp + 10 && player.GetStatValue("Level") == 1 && PlayerReputation.Get("Villagers") == _savedRep - 10);
            var corpses = Corpses(input, victim);
            _bench.Check("actual_corpse_records_exact_victim_and_killer", corpses.Length == 1 && corpses[0].BlueprintName == "CreatureCorpse"
                && corpses[0].GetProperty("KillerID") == player.ID && corpses[0].GetProperty("KillerBlueprint") == player.BlueprintName
                && input.CurrentZone.GetEntityPosition(corpses[0]) == (20, 13));
            int deathCount = DeathCount(null, victim); int corpseCount = corpses.Length; CombatSystem.HandleDeath(victim, enemy, input.CurrentZone);
            _bench.Check("later_source_cannot_repeat_or_steal_death", observation.Target.Died == 1 && DeathCount(null, victim) == deathCount && Corpses(input, victim).Length == corpseCount
                && player.GetStatValue("Experience") == _savedXp + 10 && PlayerReputation.Get("Villagers") == _savedRep - 10 && ReferenceEquals(observation.Target.Killer, player));
            Record(input, "attributed_npc_death", observation);
            var oldPlayer = player; var oldHead = Head(player); yield return LoadHealthy(input);
            _bench.Check("F6_restores_fresh_healthy_graph_and_clock", !ReferenceEquals(input.PlayerEntity, oldPlayer) && !ReferenceEquals(Head(input.PlayerEntity), oldHead)
                && Restored(input, 100, 0) && HealthyGear(input) && Actor(input, _victimID).GetPart<Body>().DismemberedParts.Count == 0);

            player = input.PlayerEntity; enemy = Actor(input, _enemyID); int serial = MessageLog.NextSerialValue;
            observation = Attack(input, enemy, player);
            _bench.Check("npc_source_mortal_loss_commits_real_player_death", HitWasTwo(observation, player) && MortalDied(observation, enemy, player, input.CurrentZone, 0)
                && GoneWithHead(input, player, observation.Target.Selected) && NoBleed(player) && DroppedGear(input, player)
                && player.GetStatValue("Experience") == _savedXp && PlayerReputation.Get("Villagers") == _savedRep);
            yield return new WaitForSecondsRealtime(.3f);
            _bench.Check("engine_Update_activates_death_modal_once", Dead(input) && PromptCountSince(serial) == 1 && !input.TurnManager.IsRegistered(player));
            byte[] checkpoint = File.ReadAllBytes(QuickPath()); oldPlayer = player; yield return Tap(Key.F5); yield return Tap(Key.F6);
            _bench.Check("death_modal_blocks_normal_save_and_load_keys", ReferenceEquals(input.PlayerEntity, oldPlayer) && Dead(input) && checkpoint.SequenceEqual(File.ReadAllBytes(QuickPath())) && PromptCountSince(serial) == 1);
            Record(input, "unmodified_player_death", observation); oldHead = observation.Target.Selected;
            yield return Tap(Key.L);
            _bench.Check("native_L_replaces_graph_and_closes_modal", !Dead(input) && !ReferenceEquals(input.PlayerEntity, oldPlayer) && !ReferenceEquals(Head(input.PlayerEntity), oldHead) && Restored(input, 100, 0));
            _bench.Check("L_restores_exact_saved_gear_and_runtime_aliases", HealthyGear(input) && ReferenceEquals(input.CurrentZone, input.ZoneManager.ActiveZone)
                && input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (20, 12) && input.PlayerEntity.GetIntProperty("GA03hCheckpoint") == 1);
            _bench.Check("healthy_load_does_not_resurrect_old_dead_object", CombatSystem.IsDeathHandled(oldPlayer) && oldPlayer.GetStat("Hitpoints").BaseValue == 0
                && oldHead.ParentPart == null && !CombatSystem.IsDeathHandled(input.PlayerEntity) && input.PlayerEntity.GetStat("Hitpoints").Min == 0 && input.PlayerEntity.GetStat("Hitpoints").Max == 100);

            player = input.PlayerEntity; var hp = player.GetStat("Hitpoints"); hp.BaseValue = 90; hp.Bonus = 10; player.SetIntProperty("GA03hCheckpoint", 2);
            yield return Save(input); CaptureCheckpoint(input);
            _bench.Check("positive_bonus_healthy_state_is_really_saved", hp.BaseValue == 90 && hp.Bonus == 10 && hp.Value == 100 && hp.Penalty == 0 && hp.Boost == 0 && !Dead(input)
                && player.GetIntProperty("GA03hCheckpoint") == 2 && _healthyBytes.SequenceEqual(File.ReadAllBytes(QuickPath())));
            enemy = Actor(input, _enemyID); serial = MessageLog.NextSerialValue; observation = Attack(input, enemy, player);
            yield return new WaitForSecondsRealtime(.3f);
            _bench.Check("committed_marker_opens_modal_despite_positive_computed_HP", HitWasTwo(observation, player) && MortalDied(observation, enemy, player, input.CurrentZone, 10)
                && hp.BaseValue == 0 && hp.Bonus == 10 && hp.Penalty == 0 && hp.Boost == 0 && hp.Min == 0 && hp.Max == 100 && hp.Value == 10
                && Dead(input) && PromptCountSince(serial) == 1 && NoBleed(player) && DroppedGear(input, player));
            Record(input, "positive_bonus_committed_death", observation); oldPlayer = player; yield return Tap(Key.L);
            _bench.Check("L_restores_saved_positive_bonus_and_fresh_living_body", !Dead(input) && !ReferenceEquals(input.PlayerEntity, oldPlayer) && Restored(input, 90, 10)
                && HealthyGear(input) && input.PlayerEntity.GetIntProperty("GA03hCheckpoint") == 2 && CombatSystem.IsDeathHandled(oldPlayer));
            _bench.Check("all_saves_remain_in_owned_disposable_scope", SaveGameService.SaveRootOverride == _root && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID
                && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2 && _markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz")))
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID)) && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _markerID)));
            Record(input, "final_healthy_recovery", null);
        }
        private Observation Attack(InputHandler input, Entity source, Entity target, bool veto = false, bool chanceFails = false)
        {
            Need(Ready(input, source, target), "Attack requires checked actual content and healthy resident source/target.");
            var head = Head(target); var parts = target.GetPart<Body>().GetParts(); int weight = parts.Where(p => !p.Abstract && p.TargetWeight > 0).Sum(p => p.TargetWeight);
            bool decap = Axe_Decapitate.ShouldDecapitate(source); var candidates = parts.Where(p => p.IsSeverable() && (!p.SeverRequiresDecapitate() || decap)).ToArray();
            Need(parts[0].Type == "Body" && !parts[0].IsSeverable() && parts[0].TargetWeight > 0 && candidates.Length > 0
                && (decap ? ReferenceEquals(candidates[0], head) : candidates[0].Type == "Arm" && !candidates[0].Mortal), "Unexpected actual target/candidate ordering.");
            var rng = new CombatRolls(weight, candidates.Length, chanceFails); var attackerProbe = new GameAuditMortalProbePart(); var targetProbe = new GameAuditMortalProbePart { Veto = veto };
            source.AddPart(attackerProbe); target.AddPart(targetProbe);
            int deathBefore = DeathCount(source, target); bool attempted;
            try { attempted = CombatSystem.PerformMeleeAttack(source, target, input.CurrentZone, rng); }
            finally { source.RemovePart(attackerProbe); }
            Need(attempted && rng.Remaining == 0, "Full combat did not consume the planned finite RNG path.");
            Need(!(CombatSystem.IsDeathHandled(source) || CombatSystem.IsDeathHandled(target))
                || !rng.Calls.Any(call => call.StartsWith("trailing_offhand")), "Committed death must stop before secondary chance RNG.");
            return new Observation { Attacker = attackerProbe, Target = targetProbe, Calls = rng.Calls.ToArray(), DeathBefore = deathBefore, DeathAfter = DeathCount(source, target) };
        }
        private bool Ready(InputHandler input, Entity source, Entity target)
        {
            if (source == null || target == null || CombatSystem.IsDeathHandled(source) || CombatSystem.IsDeathHandled(target)
                || input.CurrentZone.GetEntityCell(source) == null || input.CurrentZone.GetEntityCell(target) == null || !input.TurnManager.IsRegistered(source) || !input.TurnManager.IsRegistered(target)
                || source.HasPart<GameAuditMortalProbePart>() || target.HasPart<GameAuditMortalProbePart>()) return false;
            var axe = Item(source, source.HasTag("Player") ? _playerAxeID : _enemyAxeID); var weapon = axe.GetPart<MeleeWeaponPart>();
            var equipped = source.GetPart<Body>().GetParts().Where(p => ReferenceEquals(p._Equipped, axe)).ToArray();
            var skills = source.GetPart<SkillsPart>(); var hp = target.GetStat("Hitpoints");
            return axe.BlueprintName == "Battleaxe" && weapon.BaseDamage == "2d6" && weapon.PenBonus == 3 && weapon.Attributes == "Cutting Axe"
                && equipped.Length == 2 && equipped.All(p => p.Type == "Hand") && equipped.Count(p => p.FirstSlotForEquipped) == 1
                && equipped.Single(p => p.FirstSlotForEquipped).GetLaterality() == Laterality.LEFT && equipped.Single(p => p.FirstSlotForEquipped).DefaultPrimary
                && StatUtils.GetModifier(source, "Strength") == 0 && StatUtils.GetModifier(source, "Agility") == 0 && CombatSystem.GetDV(target) == 6
                && CombatSystem.GetPartAV(target, target.GetPart<Body>().GetParts()[0]) == 6 && hp.Value == 100 && hp.Max == 100 && hp.Min == 0
                && skills.HasSkill(nameof(Axe_Dismember)) && skills.SkillList.All(s => s is Axe_Dismember || s is Axe_Decapitate)
                && Head(target).Mortal && Head(target).IsSeverable() && Head(target).ParentPart != null && target.GetPart<Body>().DismemberedParts.Count == 0;
        }
        private static bool HitWasTwo(Observation observation, Entity target) => observation.Attacker.DealtCount == 1 && observation.Attacker.DealtAmount == 2
            && ReferenceEquals(observation.Attacker.DamageTarget, target) && observation.Attacker.HpAfterDamage == 98;
        private static bool MortalDied(Observation o, Entity source, Entity target, Zone zone, int expectedValue)
            => o.Target.Before == 1 && o.Target.After == 1 && o.Target.Selected.Type == "Head" && o.Target.Selected.Mortal && o.Target.BeforeHp == 98 && o.Target.Died == 1
                && o.Target.DiedBase == 0 && o.Target.DiedValue == expectedValue && o.Target.DiedMarked && o.Target.ResidentAtDied
                && o.Target.DeathX == 20 && o.Target.DeathY == (target.HasTag("Player") ? 12 : 13)
                && ReferenceEquals(o.Target.Target, target) && ReferenceEquals(o.Target.Killer, source) && ReferenceEquals(o.Target.ZoneAtDied, zone);
        private static bool GoneWithHead(InputHandler input, Entity actor, BodyPart head)
            => head.ParentPart == null && actor.GetPart<Body>().DismemberedParts.Count(d => ReferenceEquals(d.Part, head)) == 1 && input.CurrentZone.GetEntityCell(actor) == null
                && !input.TurnManager.IsRegistered(actor) && input.CurrentZone.GetReadOnlyEntities().Count(e => e.GetPart<SeveredLimbPart>() is SeveredLimbPart limb
                    && limb.PartType == "Head" && limb.WasMortal && input.CurrentZone.GetEntityPosition(e) == (20, actor.HasTag("Player") ? 12 : 13)) == 1;
        private static bool NoBleed(Entity actor) => !actor.GetPart<StatusEffectsPart>().GetAllEffects().Any(e => e is BleedingEffect);
        private bool HealthyGear(InputHandler input)
        {
            var actor = input.PlayerEntity; var axe = Item(actor, _playerAxeID); var boots = Item(actor, _bootsID); var inventory = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>(); var axeSlots = body.GetParts().Where(p => ReferenceEquals(p._Equipped, axe)).ToArray();
            return inventory.Objects.Count == 0 && inventory.EquippedItems.Count == 3 && axeSlots.Length == 2 && axeSlots.Count(p => p.FirstSlotForEquipped) == 1
                && new[] { axe, boots }.All(item => item.GetPart<PhysicsPart>().Equipped == actor && item.GetPart<PhysicsPart>().InInventory == null
                    && body.GetParts().Where(p => ReferenceEquals(p._Equipped, item)).All(p => inventory.EquippedItems.TryGetValue(p.ID.ToString(), out var cached) && ReferenceEquals(cached, item)))
                && body.GetParts().Count(p => ReferenceEquals(p._Equipped, boots) && p.Type == "Feet") == 1 && actor.GetStat("Speed").Penalty == 12
                && axe.GetPart<EnhancementGlowQuartz>().AppliedBonus && axe.GetPart<EnhancementGlowQuartz>().RadiusBonus == 2 && axe.GetPart<LightSourcePart>().Radius == 2;
        }
        private bool DroppedGear(InputHandler input, Entity oldPlayer)
        {
            var inventory = oldPlayer.GetPart<InventoryPart>(); var body = oldPlayer.GetPart<Body>();
            if (inventory.Objects.Count != 0 || inventory.EquippedItems.Count != 0 || body.GetParts().Any(p => p._Equipped != null) || oldPlayer.GetStat("Speed").Penalty != 7) return false;
            foreach (string id in new[] { _playerAxeID, _bootsID })
            {
                var items = input.CurrentZone.GetReadOnlyEntities().Where(e => e.ID == id).ToArray(); if (items.Length != 1) return false; var item = items[0];
                if (input.CurrentZone.GetEntityPosition(item) != (20, 12) || item.GetPart<PhysicsPart>().Equipped != null || item.GetPart<PhysicsPart>().InInventory != null) return false;
                var glow = item.GetPart<EnhancementGlowQuartz>(); if (glow != null && (glow.AppliedBonus || glow.RadiusBonus != 2 || item.GetPart<LightSourcePart>().Radius != 0)) return false;
            }
            return true;
        }
        private IEnumerator Save(InputHandler input)
        {
            Need(State(input) == "Normal" && !Dead(input) && new[] { input.PlayerEntity, Actor(input, _enemyID), Actor(input, _victimID) }.All(e => !e.HasPart<GameAuditMortalProbePart>()), "Saving probe-free healthy graphs only.");
            int serial = MessageLog.NextSerialValue; byte[] before = File.ReadAllBytes(QuickPath()); yield return Tap(Key.F5);
            Need(MessageLog.GetLast() == "Game saved." && MessageLog.NextSerialValue > serial && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID
                && !before.SequenceEqual(File.ReadAllBytes(QuickPath())), "Native F5 did not write a changed owned checkpoint.");
        }
        private void CaptureCheckpoint(InputHandler input)
        { _healthyBytes = File.ReadAllBytes(QuickPath()); _savedTick = input.TurnManager.TickCount; _savedEnergy = input.TurnManager.GetEnergy(input.PlayerEntity); _savedXp = input.PlayerEntity.GetStatValue("Experience"); _savedRep = PlayerReputation.Get("Villagers"); }
        private IEnumerator LoadHealthy(InputHandler input)
        {
            var old = input.PlayerEntity; Need(!Dead(input), "Use modal L for a dead player."); yield return Tap(Key.F6);
            Need(!ReferenceEquals(old, input.PlayerEntity) && MessageLog.GetLast() == "Game loaded." && Restored(input, 100, 0) && HealthyGear(input), "Native F6 healthy replacement failed.");
        }
        private bool Restored(InputHandler input, int baseHp, int bonusHp)
        {
            var actor = input.PlayerEntity; var hp = actor.GetStat("Hitpoints");
            return State(input) == "Normal" && !Dead(input) && !CombatSystem.IsDeathHandled(actor) && hp.BaseValue == baseHp && hp.Bonus == bonusHp && hp.Penalty == 0 && hp.Boost == 0 && hp.Min == 0 && hp.Max == 100
                && actor.GetPart<Body>().DismemberedParts.Count == 0 && Head(actor).ParentPart != null && !actor.HasPart<GameAuditMortalProbePart>()
                && ReferenceEquals(input.TurnManager, TurnManager.Active) && ReferenceEquals(input.TurnManager.CurrentActor, actor) && input.TurnManager.IsRegistered(actor)
                && input.TurnManager.TickCount == _savedTick && input.TurnManager.GetEnergy(actor) == _savedEnergy && input.CurrentZone.GetEntityCell(actor) != null
                && actor.GetStatValue("Experience") == _savedXp && PlayerReputation.Get("Villagers") == _savedRep;
        }
        private static BodyPart Head(Entity actor) => actor.GetPart<Body>().GetParts().Single(p => p.Type == "Head");
        private static Entity Item(Entity actor, string id) => actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().EquippedItems.Values).Distinct().Single(e => e.ID == id);
        private static Entity Actor(InputHandler input, string id) => input.CurrentZone.GetReadOnlyEntities().Single(e => e.ID == id);
        private static Entity[] Corpses(InputHandler input, Entity actor) => input.CurrentZone.GetReadOnlyEntities().Where(e => e.GetProperty("SourceID") == actor.ID).ToArray();
        private static int DeathCount(Entity source, Entity target) => DiagQuery.Count(new DiagQuery.Filter { Category = "damage", Kind = "DeathHandled", Actor = source?.ID, Target = target.ID }).Count;
        private static int PromptCountSince(int serial) => MessageLog.GetAllEntries().Count(e => e.Serial >= serial && e.Text == "You are dead. Press [L] to load last save, [R] to restart.");
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private static bool Dead(InputHandler input) => (Read(input, "_deathScreenController") as DeathScreenController)?.IsActive ?? false;
        private static void Need(bool value, string reason) => GameAuditMortalDeathBench.Require(value, reason);
        private string QuickPath()
        { Need(!string.IsNullOrWhiteSpace(_root) && !string.IsNullOrWhiteSpace(_freshID) && _freshID.IndexOfAny(new[] { '/', '\\' }) < 0 && SaveGameService.SaveRootOverride == _root, "Owned native save path unavailable."); return Path.Combine(_root, _freshID, "Quick.sav.gz"); }
        private void Queue(params Key[] keys) { if (_keyboard != null) InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
        private IEnumerator Tap(Key key) { Queue(key); yield return new WaitForSecondsRealtime(.06f); Queue(); yield return new WaitForSecondsRealtime(.2f); }
        private void Record(InputHandler input, string label, Observation observation)
        {
            var actor = input.PlayerEntity; var hp = actor.GetStat("Hitpoints");
            _stages.Add(new Stage { label = label, playerID = actor.ID, baseHp = hp.BaseValue, valueHp = hp.Value, bonusHp = hp.Bonus, minHp = hp.Min, maxHp = hp.Max, marked = CombatSystem.IsDeathHandled(actor), modal = Dead(input),
                tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(actor), xp = actor.GetStatValue("Experience"), villagersRep = PlayerReputation.Get("Villagers"),
                rngCalls = observation?.Calls, damage = observation?.Attacker.DealtAmount ?? 0, hpAfterDamage = observation?.Attacker.HpAfterDamage ?? 0, selectedPart = observation?.Target.Selected?.Type,
                died = observation?.Target.Died ?? 0, diedBase = observation?.Target.DiedBase ?? 0, diedValue = observation?.Target.DiedValue ?? 0,
                killerID = observation?.Target.Killer?.ID, targetID = observation?.Target.Target?.ID, corpseCellStillResidentAtDied = observation?.Target.ResidentAtDied ?? false });
        }
        public void SetUnexpectedErrors(int count) { _errors = count; if (_report != null) WriteReport(); }
        private void Finish()
        {
            if (_performanceMode != null) { FinishPerformance(); return; }
            _report = new Report { runId = _bench.RunId, root = _root, freshID = _freshID, seconds = Now,
                canVerify = "Deterministic full CombatSystem melee API → owned Axe passive → mortal death, exact source/XP/reputation/corpse and equipment cleanup; actual N/F5/F6/L, engine death polling, saved fresh aliases, preserved positive HP bonus and committed-marker modal.",
                cannotVerify = "No keyboard attack/limb targeting, random encounter chance, authored NPC Axe loadout, ordinary combat-helper Head-cut path, visual/feel, broad callback exception rollback, A12 scheduler correctness, allocation/performance/speedup claim.",
                fixtureBounds = "Actual Villager/Player/Battleaxe/Boots/Humanoid; stats, natural armor, owned skill grants, GlowQuartz mod, inert AI, legal scripted RNG and veto are explicit fixture setup. Temporary observers are absent from checkpoints. Saved L recovery replaces entities, not resurrection. Review final post-teardown JSON/raw logs; process exit is selected before teardown." };
            WriteReport(); Debug.Log("[GameAuditMortalDeathBench] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private void WriteReport()
        { _report.cases = _bench.Cases; _report.failures = Failures; _report.unexpectedErrors = _errors; _report.audit = _bench.Audit.ToArray(); _report.stages = _stages.ToArray(); Directory.CreateDirectory(ReportDirectory); File.WriteAllText(Path.Combine(ReportDirectory, _performanceMode == null ? "GA03h-native.json" : PerfPrefix + "-perf-native.json"), JsonUtility.ToJson(_report, true)); }
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
            finally { if (_keyboard != null) InputSystem.RemoveDevice(_keyboard); if (_oldSettings != null) InputSystem.settings = _oldSettings; if (_settings != null) Destroy(_settings); Application.runInBackground = _oldBackground; Diag.SetChannel("damage", _oldDamage); _clock?.Stop(); }
        }

        // Same queued-input observer overhead in before/after. Only the owned production
        // predicate differs externally; this method never patches gameplay or its settings.
        private IEnumerator PerformanceAudit()
        {
            _performanceStarted = true;
            _perfInput = FindFirstObjectByType<InputHandler>();
            _perfBoot = Read(_perfInput, "_bootMenuController") as BootMenuController;
            _perfDeath = Read(_perfInput, "_deathScreenController") as DeathScreenController;
            Need(_perfInput != null && _perfBoot != null && _perfBoot.IsActive && _perfDeath != null
                && SaveGameService.SaveRootOverride == _root, "Owned boot/performance input required.");
            _markerBytes = File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz"));
            yield return Tap(Key.N);
            _freshID = SaveGameService.GetSaveInfo("Quick")?.GameID;
            Need(_freshID != null && _freshID != _markerID && File.Exists(QuickPath()) && !_perfBoot.IsActive, "Native N did not bind the fresh private game.");
            _perfPlayer = _perfInput.PlayerEntity; _perfZone = _perfInput.CurrentZone; _perfTurns = _perfInput.TurnManager;
            _perfEnemy = Actor(_perfInput, _enemyID); _perfVictim = Actor(_perfInput, _victimID);
            _perfHP = _perfPlayer.GetStat("Hitpoints"); _perfSpeed = _perfPlayer.GetStat("Speed");
            PerfHealthyBoundary();
            Need(_perfZone.GetEntityPosition(_perfPlayer) == (20, 12), "Unexpected performance origin.");
            int routeX = 20, routeY = 12;
            for (int step = 0; step < 8; step++)
            {
                int direction = step / 2; routeX += PerfDX[direction]; routeY += PerfDY[direction];
                var cell = _perfZone.GetCell(routeX, routeY);
                Need(cell != null && !cell.Objects.Any(e => !ReferenceEquals(e, _perfPlayer)
                    && (e.HasTag("Creature") || e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true)), "Performance route is obstructed.");
            }
            // A real native closed-loop warmup, outside recording; no API repositioning.
            _perfLoopStep = 0;
            for (int step = 0; step < 8; step++) yield return PerfDiscreteStep();
            Queue(); yield return new WaitForSecondsRealtime(.5f); PerfHealthyBoundary();
            Need(_perfZone.GetEntityPosition(_perfPlayer) == (20, 12), "Warmup did not close its route.");
            _perfSamples = new long[_perfMarkerNames.Length][];
            _perfRecorders = new ProfilerRecorder[_perfMarkerNames.Length];
            _perfRecorderValid = new bool[_perfMarkerNames.Length];
            _perfTimes = new double[PerfFrameCapacity]; _perfFrameMilliseconds = new double[PerfFrameCapacity];
            _perfFramePhases = new int[PerfFrameCapacity]; _perfSteps = new PerfStep[PerfStepCapacity];
            var options = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame;
            for (int i = 0; i < _perfMarkerNames.Length; i++)
            {
                _perfSamples[i] = new long[PerfFrameCapacity];
                _perfRecorders[i] = ProfilerRecorder.StartNew(i == _perfMarkerNames.Length - 1 ? ProfilerCategory.Memory : ProfilerCategory.Scripts,
                    _perfMarkerNames[i], 2, options);
                _perfRecorderValid[i] = _perfRecorders[i].Valid;
            }
            Need(_perfRecorderValid.All(v => v), "Required native profiler marker unavailable.");
            // Leave recorder/allocation setup outside the first captured frame.
            yield return null; yield return null;
            _perfStart = Now;
            var idleOrigin = _perfZone.GetEntityPosition(_perfPlayer);
            int idleTick = _perfTurns.TickCount, idleEnergy = _perfTurns.GetEnergy(_perfPlayer);
            PerfBeginPhase(0);
            while (Now - _perfPhaseStart < PerfPhaseSeconds) yield return null;
            PerfEndPhase();
            Need(_perfZone.GetEntityPosition(_perfPlayer) == idleOrigin && _perfTurns.TickCount == idleTick
                && _perfTurns.GetEnergy(_perfPlayer) == idleEnergy, "Idle workload moved or spent a turn.");
            yield return null; PerfBeginPhase(1);
            while (Now - _perfPhaseStart < PerfPhaseSeconds) yield return PerfDiscreteStep();
            PerfEndPhase();
            while (_perfLoopStep % 8 != 0) yield return PerfDiscreteStep();
            yield return null; PerfBeginPhase(2);
            while (Now - _perfPhaseStart < PerfPhaseSeconds) yield return PerfHeldLeg();
            PerfEndPhase();
            while (_perfLoopStep % 8 != 0) yield return PerfHeldLeg();
            Queue(); yield return new WaitForSecondsRealtime(.2f); PerfHealthyBoundary();
            Need(_perfZone.GetEntityPosition(_perfPlayer) == (20, 12), "Measured movement did not return to origin.");
            Need(!_perfOverflow && _perfFrames > 300 && _perfFrames < PerfFrameCapacity
                && _perfPhases.All(p => p.seconds >= PerfPhaseSeconds && p.seconds < PerfPhaseSeconds + 3 && p.frames > 50 && p.failed == 0)
                && _perfPhases[1].accepted >= 50 && _perfPhases[1].accepted == _perfPhases[1].expectedMoves
                && _perfPhases[2].accepted >= 50 && _perfPhases[2].accepted == _perfPhases[2].expectedMoves
                && _perfPhases[2].heldRepeats > 20, "Finite 75-second movement workload was incomplete or invalid.");
            Need(_perfSamples[0].Take(_perfFrames).Any(value => value > 0), "Input.Update recorder has no positive samples.");
            Need(SaveGameService.SaveRootOverride == _root && _markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root, _markerID, "Quick.sav.gz")))
                && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID)), "Performance save isolation changed.");
            _performanceCompleted = true;
        }
        private void PerfHealthyBoundary()
        {
            Need(ReferenceEquals(_perfInput.PlayerEntity, _perfPlayer) && ReferenceEquals(_perfInput.CurrentZone, _perfZone)
                && ReferenceEquals(_perfInput.TurnManager, _perfTurns) && ReferenceEquals(TurnManager.Active, _perfTurns)
                && ReferenceEquals(_perfTurns.CurrentActor, _perfPlayer) && _perfTurns.WaitingForInput && _perfTurns.IsRegistered(_perfPlayer)
                && _perfHP.Value == 100 && _perfHP.BaseValue == 100 && _perfHP.Bonus == 0 && _perfSpeed.Value == 88
                && !_perfBoot.IsActive && !_perfDeath.IsActive && !SpellFxSettingsPanel.IsOpen && State(_perfInput) == "Normal"
                && !CombatSystem.IsDeathHandled(_perfPlayer) && !_perfPlayer.HasPart<GameAuditMortalProbePart>()
                && HealthyGear(_perfInput), "Healthy performance player/input/gear context changed.");
            Need(_perfEnemy.GetPart<BrainPart>() == null && _perfVictim.GetPart<BrainPart>() == null
                && _perfZone.GetEntityPosition(_perfEnemy) == (21, 12) && _perfZone.GetEntityPosition(_perfVictim) == (20, 13)
                && _perfTurns.IsRegistered(_perfEnemy) && _perfTurns.IsRegistered(_perfVictim)
                && _perfEnemy.GetStatValue("Hitpoints") == 100 && _perfVictim.GetStatValue("Hitpoints") == 100
                && !CombatSystem.IsDeathHandled(_perfEnemy) && !CombatSystem.IsDeathHandled(_perfVictim), "Inert NPC control changed; combat is outside this workload.");
        }
        private void PerfBeginPhase(int index)
        {
            PerfHealthyBoundary(); _perfPhase = index; _perfPhaseStart = Now; _perfLastStep = -1;
            var p = _perfPhases[index]; var pos = _perfZone.GetEntityPosition(_perfPlayer);
            p.startSeconds = Now - _perfStart; p.startTick = _perfTurns.TickCount; p.startEnergy = _perfTurns.GetEnergy(_perfPlayer); p.startX = pos.x; p.startY = pos.y;
            _perfMeasuring = true;
        }
        private void PerfEndPhase()
        {
            _perfMeasuring = false; var p = _perfPhases[_perfPhase]; var pos = _perfZone.GetEntityPosition(_perfPlayer);
            p.seconds = Now - _perfPhaseStart; p.endTick = _perfTurns.TickCount; p.endEnergy = _perfTurns.GetEnergy(_perfPlayer); p.endX = pos.x; p.endY = pos.y;
            PerfHealthyBoundary();
        }
        private IEnumerator PerfDiscreteStep()
        {
            int direction = (_perfLoopStep % 8) / 2; var before = _perfZone.GetEntityPosition(_perfPlayer); int tick = _perfTurns.TickCount;
            double start = Now; bool measured = _perfMeasuring;
            if (measured) { _perfPhases[_perfPhase].requests++; _perfPhases[_perfPhase].expectedMoves++; }
            Queue(PerfKeys[direction]);
            while (_perfZone.GetEntityPosition(_perfPlayer) == before && Now - start < 2) yield return null;
            Queue(); PerfObserveStep(before.x, before.y, direction, tick, start, false, measured); _perfLoopStep++;
            yield return new WaitForSecondsRealtime(Mathf.Max(.18f, _perfInput.MoveRepeatDelay + .04f));
        }
        private IEnumerator PerfHeldLeg()
        {
            int direction = (_perfLoopStep % 8) / 2; var previous = _perfZone.GetEntityPosition(_perfPlayer); int tick = _perfTurns.TickCount;
            double started = Now, last = started; int observed = 0; bool measured = _perfMeasuring;
            if (measured) { _perfPhases[_perfPhase].requests++; _perfPhases[_perfPhase].expectedMoves += 2; }
            Queue(PerfKeys[direction]);
            while (observed < 2 && Now - started < 3)
            {
                yield return null; var current = _perfZone.GetEntityPosition(_perfPlayer); if (current == previous) continue;
                bool repeated = observed == 1 && _keyboard[PerfKeys[direction]].isPressed && !_keyboard[PerfKeys[direction]].wasPressedThisFrame;
                PerfObserveStep(previous.x, previous.y, direction, tick, last, repeated, measured);
                observed++; _perfLoopStep++; previous = current; tick = _perfTurns.TickCount; last = Now;
            }
            Queue(); Need(observed == 2, "Held native key did not produce its two requested moves.");
            yield return new WaitForSecondsRealtime(Mathf.Max(.16f, _perfInput.MoveRepeatDelay + .02f));
        }
        private void PerfObserveStep(int oldX, int oldY, int direction, int oldTick, double requested, bool repeated, bool measured)
        {
            var pos = _perfZone.GetEntityPosition(_perfPlayer);
            bool accepted = pos != (oldX, oldY);
            bool valid = pos == (oldX + PerfDX[direction], oldY + PerfDY[direction]) && _perfTurns.TickCount > oldTick
                && ReferenceEquals(_perfTurns.CurrentActor, _perfPlayer) && _perfTurns.WaitingForInput && _perfTurns.IsRegistered(_perfPlayer)
                && _perfHP.Value == 100 && _perfSpeed.Value == 88 && !_perfDeath.IsActive;
            if (measured)
            {
                var p = _perfPhases[_perfPhase]; if (accepted) p.accepted++; if (!valid) p.failed++; if (repeated) p.heldRepeats++;
                Need(_perfStepCount < PerfStepCapacity, "Performance step capacity exhausted.");
                _perfSteps[_perfStepCount++] = new PerfStep { phase = _perfPhase, seconds = Now - _perfStart, latency = Now - requested,
                    interval = _perfLastStep < 0 ? -1 : Now - _perfLastStep, tickDelta = _perfTurns.TickCount - oldTick,
                    energy = _perfTurns.GetEnergy(_perfPlayer), x = pos.x, y = pos.y, repeated = repeated, valid = valid };
                _perfLastStep = Now;
            }
            Need(valid, "Native performance move changed distance, health, Speed or failed to advance turns.");
        }
        private void LateUpdate()
        {
            if (!_perfMeasuring || _perfRecorders == null) return;
            if (_perfFrames >= PerfFrameCapacity) { _perfOverflow = true; _perfMeasuring = false; return; }
            _perfTimes[_perfFrames] = Now - _perfStart; _perfFramePhases[_perfFrames] = _perfPhase;
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
                int p = phase;
                var indices = Enumerable.Range(0, _perfFrames).Where(f => _perfFramePhases[f] == p).ToArray();
                for (int i = 0; i < _perfMarkerNames.Length; i++)
                {
                    int marker = i;
                    var values = indices.Select(f => (double)_perfSamples[marker][f]).OrderBy(v => v).ToArray();
                    metrics.Add(PerfSummarize(_perfPhases[p].name, _perfMarkerNames[i], i == _perfMarkerNames.Length - 1 ? "bytes" : "nanoseconds",
                        values, _perfRecorderValid != null && _perfRecorderValid[i] && values.All(v => v >= 0)));
                }
                metrics.Add(PerfSummarize(_perfPhases[p].name, "Engine frame duration", "milliseconds",
                    indices.Select(f => _perfFrameMilliseconds[f]).OrderBy(v => v).ToArray(), true));
            }
            bool valid = _performanceCompleted && !_perfOverflow && metrics.All(m => m.valid) && Failures == 0;
            if (_performanceCompleted && !valid && _fatal == 0) _fatal++;
            Directory.CreateDirectory(ReportDirectory);
            var csv = new StringBuilder("frame,seconds,phase,engine_frame_ms," + string.Join(",", _perfMarkerNames) + "\n");
            for (int f = 0; f < _perfFrames; f++)
            {
                csv.Append(f).Append(',').Append(PerfF(_perfTimes[f])).Append(',').Append(_perfPhases[_perfFramePhases[f]].name).Append(',').Append(PerfF(_perfFrameMilliseconds[f]));
                for (int i = 0; i < _perfMarkerNames.Length; i++) csv.Append(',').Append(_perfSamples[i][f]); csv.Append('\n');
            }
            File.WriteAllText(Path.Combine(ReportDirectory, PerfPrefix + "-frames.csv"), csv.ToString());
            csv = new StringBuilder("step,seconds,phase,request_to_step_seconds,interval_seconds,tick_delta,energy,x,y,held_repeat,valid\n");
            for (int i = 0; i < _perfStepCount; i++)
            {
                var s = _perfSteps[i]; csv.Append(i).Append(',').Append(PerfF(s.seconds)).Append(',').Append(_perfPhases[s.phase].name).Append(',').Append(PerfF(s.latency))
                    .Append(',').Append(PerfF(s.interval)).Append(',').Append(s.tickDelta).Append(',').Append(s.energy).Append(',').Append(s.x).Append(',').Append(s.y).Append(',').Append(s.repeated).Append(',').Append(s.valid).Append('\n');
            }
            File.WriteAllText(Path.Combine(ReportDirectory, PerfPrefix + "-steps.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(ReportDirectory, PerfPrefix + "-perf.json"), JsonUtility.ToJson(new PerfReport {
                runId = _bench.RunId, mode = _performanceMode, performanceOnly = true, functionalCases = 0, workloadValid = valid, frames = _perfFrames, overflow = _perfOverflow,
                measuredSeconds = _perfPhases.Sum(p => p.seconds), wallSeconds = _perfStart > 0 ? Now - _perfStart : 0,
                moveRepeatDelay = _perfInput != null ? _perfInput.MoveRepeatDelay : 0, targetFrameRate = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount,
                phases = _perfPhases, metrics = metrics.ToArray(),
                bounds = "Single uncapped native editor workload, including queued input/observer/renderer/diagnostics overhead. Latest completed recorder observations can be one frame shifted at phase edges. Engine frame duration is wall duration, not CPU time. Warmup and route alignment excluded. No isolated query timing, speedup, equivalence, zero-GC, combat/death performance or feel claim. Source variant must be verified externally; before/after label does not patch gameplay." }, true));
            _report = new Report { runId = _bench.RunId, root = _root, freshID = _freshID, seconds = Now,
                canVerify = "Performance-only " + _performanceMode + ": queued native N/arrows, accepted closed-loop movement/held repeats, healthy clock/coordinates, per-frame Input.Update and context/GC markers with max/p95/p99, owned-save teardown. Functional cases are zero in this branch.",
                cannotVerify = "No functional mortality matrix in this branch; no combat/death timing, isolated predicate timing, speedup, statistical equivalence, zero allocation, build FPS, physical keyboard, visual or feel claim.",
                fixtureBounds = "Actual healthy GA03h arena and unchanged gear/Speed88; two inert NPCs remain off-route. Before/after labels require external exact-predicate provenance. No save/load or combat during sampling. Review final perf-native JSON after teardown, not only the process code." };
            WriteReport(); Debug.Log("[GameAuditMortalDeathBench Performance] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private static PerfMetric PerfSummarize(string phase, string name, string unit, double[] values, bool valid)
        {
            int n = values.Length;
            return new PerfMetric { phase = phase, name = name, unit = unit, valid = valid && n > 0, samples = n,
                max = n == 0 ? -1 : values[n - 1], p95 = n == 0 ? -1 : values[Math.Max(0, (int)Math.Ceiling(.95 * n) - 1)],
                p99 = n == 0 ? -1 : values[Math.Max(0, (int)Math.Ceiling(.99 * n) - 1)], mean = n == 0 ? -1 : values.Average() };
        }
        private static string PerfF(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
        [Serializable] private sealed class PerfReport
        { public string runId, mode, bounds; public bool performanceOnly, workloadValid, overflow; public int functionalCases, frames, targetFrameRate, vSyncCount; public double measuredSeconds, wallSeconds; public float moveRepeatDelay; public PerfPhase[] phases; public PerfMetric[] metrics; }
        [Serializable] private sealed class PerfPhase
        { public string name; public int frames, requests, expectedMoves, accepted, heldRepeats, failed, startTick, endTick, startEnergy, endEnergy, startX, startY, endX, endY; public double startSeconds, seconds; }
        [Serializable] private sealed class PerfMetric
        { public string phase, name, unit; public bool valid; public int samples; public double max, p95, p99, mean; }
        private struct PerfStep
        { public int phase, tickDelta, energy, x, y; public double seconds, latency, interval; public bool repeated, valid; }

        private sealed class Observation { public GameAuditMortalProbePart Attacker, Target; public string[] Calls; public int DeathBefore, DeathAfter; }
        /// <summary>Checks exact public Random overload/range calls. Never forces exploding10.
        /// Secondary default-fist chance may follow the main weapon on living controls.</summary>
        private sealed class CombatRolls : System.Random
        {
            private readonly Queue<(bool single, int min, int max, int value)> _steps = new Queue<(bool, int, int, int)>();
            public readonly List<string> Calls = new List<string>(); private int _tail;
            public int Remaining => _steps.Count;
            public CombatRolls(int weight, int candidates, bool chanceFails)
            { Add(false, 1, 21, 10); Add(true, 0, weight, 0); Add(false, 1, 11, 9); Add(false, 1, 11, 1); Add(false, 1, 11, 1); Add(false, 1, 7, 1); Add(false, 1, 7, 1); Add(true, 0, 100, 99); Add(true, 0, 100, chanceFails ? 99 : 0); if (!chanceFails) Add(true, 0, candidates, 0); }
            private void Add(bool single, int min, int max, int value) => _steps.Enqueue((single, min, max, value));
            public override int Next(int maxValue) => Take(true, 0, maxValue);
            public override int Next(int minValue, int maxValue) => Take(false, minValue, maxValue);
            private int Take(bool single, int min, int max)
            {
                if (_steps.Count == 0 && single && max == 100 && _tail++ == 0) { Calls.Add("trailing_offhand Next(100)=99"); return 99; }
                Need(_steps.Count > 0, "Unexpected extra combat RNG call."); var step = _steps.Dequeue();
                Need(step.single == single && step.min == min && step.max == max && step.value >= min && step.value < max, "Combat RNG overload/range drift: " + min + "," + max);
                Calls.Add((single ? "Next(" + max : "Next(" + min + "," + max) + ")=" + step.value); return step.value;
            }
        }
        [Serializable] private sealed class Report { public string runId, root, freshID, canVerify, cannotVerify, fixtureBounds; public int cases, failures, unexpectedErrors; public double seconds, shutdownSeconds; public bool shutdownObserved, shutdownRootHeld, shutdownSavingUnregistered; public string[] audit; public Stage[] stages; }
        [Serializable] private sealed class Stage { public string label, playerID, selectedPart, killerID, targetID; public int baseHp, valueHp, bonusHp, minHp, maxHp, tick, energy, xp, villagersRep, damage, hpAfterDamage, died, diedBase, diedValue; public bool marked, modal, corpseCellStillResidentAtDied; public string[] rngCalls; }
    }
}
