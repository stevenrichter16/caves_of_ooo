using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _combatOnly, _oldDamage, _combatAttempt, _combatDamage, _combatLethal, _combatResponse;
        private bool _combatPoseObserved, _combatPoseCaptured, _combatOldEvent;
        private const int MaxStartingTonicUses = 2;
        private int _combatTonicsUsed, _combatRimeUses;
        private ActivatedAbility _combatStartingRime;
        private readonly Dictionary<string, Entity> _combatStartingTonics = new Dictionary<string, Entity>(StringComparer.Ordinal);
        private int _combatMoves, _combatAttacks, _combatDeathX, _combatDeathY;
        private Entity _combatTarget;
        private string _combatTargetID, _combatLastState;
        private readonly HashSet<string> _combatOwnedDropIDs = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _combatSteps = new List<string>();
        private static readonly string[] CombatChecks =
        {
            "ordinary_player_and_native_zone", "ordinary_combat_start", "exact_original_hostiles",
            "starting_dagger_equipped_by_keyboard", "keyboard_reaches_original_target",
            "actual_player_attack_dispatch", "actual_player_damage", "actual_target_retaliation",
            "player_attributed_defeat_and_owned_drop", "real_attack_pose_and_frame",
            "dead_native_view_removed", "ordinary_combat_finish"
        };
        private bool CombatComplete => _audit.Count == CombatChecks.Length
            && CombatChecks.All(name => _audit.Contains("PASS " + name)) && _screenshots.Count >= 4;

        private IEnumerator RunCombatAudit()
        {
            var actor = _input.PlayerEntity;
            Check("ordinary_combat_start", actor.GetStatValue("Hitpoints") == 40
                && actor.GetStatValue("Strength") == 18 && actor.GetStatValue("Agility") == 18
                && actor.GetStatValue("Toughness") == 18 && actor.GetStatValue("Ego") == 16
                && actor.GetStatValue("Intelligence") == 10 && actor.GetStatValue("Willpower") == 10
                && actor.GetStatValue("Level") == 1 && !DebugInvincibility.IsEnabled(actor)
                && Cell().X == 40 && Cell().Y == 12);
            foreach (var item in actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "HealingTonic"))
                _combatStartingTonics.Add(item.ID, item);
            Require(CombatStartingTonicUnits() == MaxStartingTonicUses, "exact ordinary two starting tonic units");
            for (int slot = 0; slot < ActivatedAbilitiesPart.SlotCount; slot++)
            {
                var ability = actor.GetPart<ActivatedAbilitiesPart>()?.GetAbilityBySlot(slot);
                if (ability?.Command == "CommandRimeGrip") _combatStartingRime = ability;
            }
            var hostiles = _stagedZone.GetReadOnlyEntities().Where(e => e.HasTag("Creature")
                && e.BlueprintName.StartsWith("Marlback", StringComparison.Ordinal)).ToArray();
            var expected = new[] { ("MarlbackScrabbler", 42, 19), ("MarlbackGleaner", 44, 20), ("MarlbackScrabbler", 22, 5) };
            // Bootstrap eagerly captures authored starts, then runs ordinary
            // NPC turns before the player's first input. Current positions may
            // already differ; source identity must use those captured starts.
            foreach (var owner in _stagedZone.GetReadOnlyEntities().Where(e => e.HasTag("Creature")))
            {
                var brain = owner.GetPart<BrainPart>();
                var position = _stagedZone.GetEntityCell(owner);
                _descriptions.Add(new Description
                {
                    subject = "Preflight native creature",
                    source = owner.ID,
                    text = owner.BlueprintName + " @ " + position?.X + "," + position?.Y
                        + "; authoredStart=" + brain?.StartingCellX + "," + brain?.StartingCellY
                        + "; hasBrain=" + (brain != null)
                        + "; liveZone=" + ReferenceEquals(brain?.CurrentZone, _stagedZone)
                        + "; currentZoneMember=" + (position != null)
                        + "; registered=" + _input.TurnManager.IsRegistered(owner)
                        + "; HP=" + owner.GetStatValue("Hitpoints")
                        + "; deathHandled=" + CombatSystem.IsDeathHandled(owner)
                        + "; actual gear=" + CombatGear(owner)
                });
            }
            var sourceMatches = expected.Select(row => hostiles.Count(e => e.BlueprintName == row.Item1
                && e.GetPart<BrainPart>()?.StartingCellX == row.Item2
                && e.GetPart<BrainPart>()?.StartingCellY == row.Item3)).ToArray();
            int wardens = _stagedZone.GetReadOnlyEntities().Count(e => e.BlueprintName == "Warden");
            int dogs = _stagedZone.GetReadOnlyEntities().Count(e => e.BlueprintName == "PetDog");
            bool sourceValid = hostiles.Length == 3 && sourceMatches.All(count => count == 1)
                && hostiles.All(e => e.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(e)
                    && _stagedZone.GetEntityCell(e) != null
                    && ReferenceEquals(e.GetPart<BrainPart>()?.CurrentZone, _stagedZone)
                    && _input.TurnManager.IsRegistered(e))
                && wardens == 1 && dogs == 1;
            _descriptions.Add(new Description
            {
                subject = "Exact authored source preflight",
                source = _stagedZone.ZoneID,
                text = "Marlback owners=" + hostiles.Length + "; expected blueprint/start matches="
                    + string.Join(",", sourceMatches) + "; wardens=" + wardens + "; dogs=" + dogs
                    + "; actual startup tick=" + _input.TurnManager.TickCount + "; sourceValid=" + sourceValid
            });
            Check("exact_original_hostiles", sourceValid);
            Require(sourceValid, "all three exact authored hostiles and ordinary NPCs before combat keys; see preflight owners");
            _combatTarget = hostiles.Single(e => e.GetPart<BrainPart>().StartingCellX == 22
                && e.GetPart<BrainPart>().StartingCellY == 5);
            _combatTargetID = _combatTarget.ID;
            var targetInventory = _combatTarget.GetPart<InventoryPart>();
            Require(targetInventory != null, "authored target has actual inventory ownership");
            foreach (var item in targetInventory.Objects.Concat(targetInventory.EquippedItems.Values).Distinct())
                _combatOwnedDropIDs.Add(item.ID);
            Require(_combatOwnedDropIDs.Count > 0, "authored armed target has actual owned gear before combat");
            _descriptions.Add(new Description { subject = "Fixed authored encounter selection", source = _combatTargetID,
                text = "Northern Scrabbler authored at22,5 selected before keyboard movement; current cell="
                    + _stagedZone.GetEntityCell(_combatTarget).X + "," + _stagedZone.GetEntityCell(_combatTarget).Y
                    + "; initial physical distance="
                    + SpatialQuery.Distance(_stagedZone, actor, _combatTarget) + ". No source changes, travel shortcuts, alternate targets or retries." });
            var dagger = actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Dagger");
            yield return ItemAction(dagger, "equip_auto");
            Require(dagger.GetPart<PhysicsPart>()?.Equipped == actor, "actual starter dagger equipped through menu");
            if (State() == "InventoryOpen") yield return Tap(Key.I);
            yield return CombatWaitForFx();
            Require(State() == "Normal", "native starter equipment menu closes");
            Check("starting_dagger_equipped_by_keyboard", dagger.GetPart<PhysicsPart>().Equipped == actor);

            for (int step = 0; SpatialQuery.Distance(_stagedZone, actor, _combatTarget) > 1; step++)
            {
                Require(step < 40, "finite40-step initial combat approach");
                CombatRequireLiveTarget();
                var route = CombatRoute();
                Require(route != null && route.Count > 0, "real route to exact original target; " + CombatState());
                var at = Cell(); var next = route[0];
                yield return CombatKey(Direction(next.X - at.X, next.Y - at.Y), false);
                _combatMoves++;
            }
            CombatRequireLiveTarget();
            Check("keyboard_reaches_original_target", _combatMoves > 0 && SpatialQuery.Distance(_stagedZone, actor, _combatTarget) == 1);
            var presenter = FindFirstObjectByType<SpawnRing3DPresenter>();
            Require(presenter.TryGetEntityView(_combatTarget, out var targetView, out _) && targetView.activeInHierarchy,
                "live target owns an actual native view at contact");
            Require(presenter.TryGetEntityView(actor, out var actorView, out _) && actorView.activeInHierarchy,
                "ordinary player owns actual native view");
            yield return Capture("02-keyboard-contact");
            for (int attempt = 0; !CombatSystem.IsDeathHandled(_combatTarget); attempt++)
            {
                Require(attempt < 40, "finite40-key combat bound");
                CombatRequireLiveTarget();
                yield return CombatUseStartingSupport();
                CombatRequireLiveTarget();
                if (SpatialQuery.Distance(_stagedZone, actor, _combatTarget) > 1)
                {
                    Require(_combatMoves < 80, "bounded pursuit of the same original owner");
                    var route = CombatRoute();
                    Require(route != null && route.Count > 0, "actual target pursuit route");
                    var origin = Cell(); var next = route[0];
                    yield return CombatKey(Direction(next.X - origin.X, next.Y - origin.Y), false);
                    _combatMoves++; continue;
                }
                var from = Cell(); var to = SpatialQuery.ClosestCell(_stagedZone, _combatTarget, from.X, from.Y);
                yield return CombatKey(Direction(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y)), true);
                _combatAttacks++;
            }
            yield return CombatDismissEarnedAdvancement();
            Check("actual_player_attack_dispatch", _combatAttempt && _combatAttacks > 0);
            Check("actual_player_damage", _combatDamage);
            Check("actual_target_retaliation", _combatResponse);
            var corpse = _stagedZone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "MarlbackCorpse"
                && e.Properties.TryGetValue("SourceID", out string source) && source == _combatTargetID
                && e.Properties.TryGetValue("KillerID", out string killer) && killer == actor.ID);
            bool corpseAtDeath = corpse != null && _stagedZone.GetEntityCell(corpse).X == _combatDeathX
                && _stagedZone.GetEntityCell(corpse).Y == _combatDeathY;
            var ownedDrops = _stagedZone.GetReadOnlyEntities().Where(e => _combatOwnedDropIDs.Contains(e.ID)
                && _stagedZone.GetEntityCell(e).X == _combatDeathX && _stagedZone.GetEntityCell(e).Y == _combatDeathY
                && e.GetPart<PhysicsPart>()?.InInventory == null && e.GetPart<PhysicsPart>()?.Equipped == null).ToArray();
            _descriptions.Add(new Description { subject = "Actual finite death output", source = _combatTargetID,
                text = "corpse=" + corpse?.ID + "; corpseAtDeath=" + corpseAtDeath
                    + "; original-owned gear now at death cell=" + string.Join(",", ownedDrops.Select(e => e.ID)) });
            Check("player_attributed_defeat_and_owned_drop", _combatLethal && CombatSystem.IsDeathHandled(_combatTarget)
                && _combatTarget.GetStatValue("Hitpoints") <= 0 && _stagedZone.GetEntityCell(_combatTarget) == null
                && !_input.TurnManager.IsRegistered(_combatTarget) && (corpseAtDeath || ownedDrops.Length > 0));
            Check("real_attack_pose_and_frame", _combatPoseObserved && _combatPoseCaptured);
            yield return null;
            Check("dead_native_view_removed", !presenter.TryGetEntityView(_combatTarget, out _, out _)
                && targetView == null && presenter.TryGetEntityView(actor, out var livingView, out _)
                && livingView.activeInHierarchy);
            Check("ordinary_combat_finish", ReferenceEquals(actor, _input.PlayerEntity) && State() == "Normal"
                && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
                && !DebugInvincibility.IsEnabled(actor) && !DevMode.Enabled && !actor.HasPart<BitLockerPart>()
                && ReferenceEquals(_input.CurrentZone, _stagedZone) && !presenter.FullReveal
                && !_input.ZoneRenderer.RevealEntire3DZone && dagger.GetPart<PhysicsPart>().Equipped == actor);
            yield return Capture("04-player-defeat-outcome");
        }

        private int CombatStartingTonicUnits()
        {
            var actor = _input.PlayerEntity;
            var inventory = actor.GetPart<InventoryPart>();
            return _combatStartingTonics.Where(pair => pair.Value.ID == pair.Key && inventory.Objects.Contains(pair.Value)
                && pair.Value.GetPart<PhysicsPart>()?.InInventory == actor
                && pair.Value.GetPart<PhysicsPart>()?.Equipped == null)
                .Sum(pair => pair.Value.GetPart<StackerPart>()?.StackCount ?? 1);
        }

        private IEnumerator CombatUseStartingSupport()
        {
            // Real starting controls are used only after this exact owner has
            // already demonstrated retaliation. No cooldown or world state is set.
            if (_combatResponse && _combatRimeUses == 0 && _combatStartingRime?.CooldownRemaining == 0
                && _combatTarget.GetStatValue("Hitpoints") > CavesOfOoo.Skills.Cryomancy_RimeGrip.GRIP_DAMAGE
                && _combatTarget.GetEffect<FrozenEffect>()?.Cold > 0 != true
                && SpatialQuery.Distance(_stagedZone, _input.PlayerEntity, _combatTarget) == 1
                && FactionManager.IsHostile(_combatTarget, _input.PlayerEntity))
            {
                var actor = _input.PlayerEntity;
                var origin = Cell();
                var targetCell = SpatialQuery.ClosestCell(_stagedZone, _combatTarget, origin.X, origin.Y);
                var first = targetCell.Occupants.FirstOrDefault(e => AbilityTargeting.IsCreatureTarget(e, actor))
                    ?? targetCell.Occupants.FirstOrDefault(e => e != null && !e.HasTag("Creature")
                        && AbilityTargeting.IsElementalTarget(e, actor));
                int slot = -1;
                for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
                    if (ReferenceEquals(actor.GetPart<ActivatedAbilitiesPart>()?.GetAbilityBySlot(i), _combatStartingRime)) slot = i;
                if (slot >= 0 && targetCell.IsVisible && _combatTarget.GetPart<RenderPart>()?.Visible == true
                    && ReferenceEquals(first, _combatTarget))
                {
                    string before = CombatState();
                    Diag.Record("scenario", ReferenceGladeCombatEvidence.SupportMarkerKind, actor, _combatTarget,
                        new { runId = RunId, action = "starting_rime_grip", before, slot });
                    string marker = Diag.Snapshot(1).Single().TraceId;
                    try
                    {
                        yield return Tap((Key)Enum.Parse(typeof(Key), slot == 9 ? "Digit0" : "Digit" + (slot + 1)));
                        Require(State() == "AwaitingDirection", "actual initial ready Rime asks for direction");
                        yield return Tap(Direction(Math.Sign(targetCell.X - origin.X), Math.Sign(targetCell.Y - origin.Y)));
                        yield return CombatWaitForFx();
                        Require(Diag.Snapshot(Diag.BufferCapacity).Any(row => row.TraceId == marker)
                            && _combatStartingRime.CooldownRemaining > 0
                            && (_combatTarget.GetEffect<FrozenEffect>()?.Cold > 0 || CombatSystem.IsDeathHandled(_combatTarget)),
                            "actual Rime cooldown and exact original owner effect; " + CombatState());
                        _combatRimeUses++;
                    }
                    finally { CombatRecordSupport(marker, "starting_rime_grip", before); }
                    // A naturally lethal spell cannot substitute for the stronger
                    // melee kill receipt, even if other ordinary effects intervened.
                    CombatRequireLiveTarget();
                }
            }
            var player = _input.PlayerEntity;
            var hp = player.GetStat("Hitpoints");
            if (_combatTonicsUsed >= MaxStartingTonicUses || hp.Value * 3 > hp.Max * 2) yield break;
            var tonic = _combatStartingTonics.Where(pair => pair.Value.ID == pair.Key
                && player.GetPart<InventoryPart>().Objects.Contains(pair.Value)
                && pair.Value.GetPart<PhysicsPart>()?.InInventory == player
                && pair.Value.GetPart<PhysicsPart>()?.Equipped == null).Select(pair => pair.Value).FirstOrDefault();
            if (tonic == null) yield break;
            int beforeUnits = CombatStartingTonicUnits();
            string beforeState = CombatState();
            Diag.Record("scenario", ReferenceGladeCombatEvidence.SupportMarkerKind, player, player,
                new { runId = RunId, action = "starting_healing_tonic", tonic = tonic.ID, beforeUnits, beforeState });
            string tonicMarker = Diag.Snapshot(1).Single().TraceId;
            try
            {
                yield return ItemAction(tonic, "ApplyTonic");
                yield return CombatWaitForFx();
                if (State() == "InventoryOpen") yield return Tap(Key.I);
                yield return CombatWaitForFx();
                Require(ReferenceGladeCombatEvidence.HasConsumedTonic(Diag.Snapshot(Diag.BufferCapacity),
                    tonicMarker, player.ID, tonic.ID, beforeUnits, CombatStartingTonicUnits()),
                    "one actual original tonic unit and fresh exact native application; " + CombatState());
                _combatTonicsUsed++;
            }
            finally { CombatRecordSupport(tonicMarker, "starting_healing_tonic", beforeState); }
            yield return CombatDismissEarnedAdvancement();
        }

        private void CombatRecordSupport(string marker, string action, string before)
        {
            var rows = Diag.Snapshot(Diag.BufferCapacity);
            _combatSteps.Add(JsonConvert.SerializeObject(new { marker, action, before, after = CombatState(),
                windowValid = rows.Any(row => row.TraceId == marker),
                remainingOriginalTonicUnits = CombatStartingTonicUnits(), startingTonicsUsed = _combatTonicsUsed,
                startingRimeUses = _combatRimeUses, records = rows.SkipWhile(row => row.TraceId != marker).ToArray() }));
        }

        private void CombatRequireLiveTarget()
        {
            Require(_clock.Elapsed.TotalSeconds < 180 && State() == "Normal"
                && _input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity),
                "live ordinary combat actor within deadline; " + CombatState());
            Require(_combatTarget.ID == _combatTargetID && _combatTarget.GetStatValue("Hitpoints") > 0
                && !CombatSystem.IsDeathHandled(_combatTarget) && _stagedZone.GetEntityCell(_combatTarget) != null
                && ReferenceEquals(_combatTarget.GetPart<BrainPart>()?.CurrentZone, _stagedZone)
                && _input.TurnManager.IsRegistered(_combatTarget),
                "same original target remains alive; NPC defeat is not player success");
        }

        private List<Cell> CombatRoute()
        {
            var from = Cell(); var start = (from.X, from.Y);
            var previous = new Dictionary<(int, int), (int, int)> { [start] = start };
            var queue = new Queue<(int, int)>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var next = (at.Item1 + dx, at.Item2 + dy);
                    if (previous.ContainsKey(next)) continue;
                    var footprint = _stagedZone.GetOccupiedCells(_input.PlayerEntity, next.Item1, next.Item2);
                    if (footprint.Count == 0 || footprint.Any(c => c == null || c.BlocksMovement(_input.PlayerEntity)
                        || c.Occupants.Any(e => e != _input.PlayerEntity && (e.HasTag("Creature") || e.HasPart<TriggerOnStepPart>())))) continue;
                    previous[next] = at;
                    if (SpatialQuery.DistanceToCell(_stagedZone, _combatTarget, next.Item1, next.Item2) <= 1)
                    {
                        var path = new List<Cell>(); var point = next;
                        while (point != start) { path.Add(_stagedZone.GetCell(point.Item1, point.Item2)); point = previous[point]; }
                        path.Reverse(); return path;
                    }
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        private IEnumerator CombatKey(Key key, bool attack)
        {
            CombatRequireLiveTarget();
            double gate = Time.realtimeSinceStartupAsDouble;
            while (Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - gate < 3, "combat input rate gate"); yield return null; }
            var targetAt = _stagedZone.GetEntityCell(_combatTarget);
            int targetX = targetAt.X, targetY = targetAt.Y;
            string before = CombatState();
            Transform[] bones = Array.Empty<Transform>(); Vector3[] positions = Array.Empty<Vector3>(); Quaternion[] rotations = Array.Empty<Quaternion>();
            Animator animator = null;
            if (attack)
            {
                var presenter = FindFirstObjectByType<SpawnRing3DPresenter>();
                Require(presenter.TryGetEntityView(_input.PlayerEntity, out var root, out _), "actual attack renderer");
                animator = root.GetComponentInChildren<Animator>(true);
                Require(animator != null && root.activeInHierarchy, "actual visible player animator");
                bones = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r => r.bones).Where(b => b != null).Distinct().ToArray();
                Require(bones.Length > 0, "actual bound player rig bones");
                positions = bones.Select(b => b.localPosition).ToArray(); rotations = bones.Select(b => b.localRotation).ToArray();
            }
            Diag.Record("scenario", ReferenceGladeCombatEvidence.MarkerKind, _input.PlayerEntity, _combatTarget,
                new { runId = RunId, key = key.ToString(), attack, before });
            string marker = Diag.Snapshot(1).Single().TraceId;
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            double began = Time.realtimeSinceStartupAsDouble;
            bool released = false, windowPose = false, windowCapture = false;
            do
            {
                yield return null;
                if (!released) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); released = true; }
                yield return new WaitForEndOfFrame();
                if (attack && animator != null && animator.isActiveAndEnabled
                    && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")
                    && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0
                    && bones.Where((bone, i) => bone != null && (Vector3.Distance(bone.localPosition, positions[i]) > .00001f
                        || Quaternion.Angle(bone.localRotation, rotations[i]) > .05f)).Any())
                {
                    windowPose = true;
                    if (!_combatPoseCaptured && !windowCapture)
                    {
                        Directory.CreateDirectory(DirectoryPath);
                        string path = Path.Combine(DirectoryPath, "03-real-player-attack.png");
                        DensityNativeScreenshot.CaptureToFile(path);
                        Require(File.Exists(path) && new FileInfo(path).Length > 0, "real attack-frame capture");
                        if (!_screenshots.Contains(path)) _screenshots.Add(path); windowCapture = true;
                        _descriptions.Add(new Description { subject = "Observed actual attack pose", source = marker,
                            text = "frame=" + Time.frameCount + "; state=" + animator.GetCurrentAnimatorStateInfo(0).fullPathHash
                                + "; normalizedTime=" + animator.GetCurrentAnimatorStateInfo(0).normalizedTime
                                + "; changedBones=" + string.Join(",", bones.Where((bone, i) => bone != null
                                    && (Vector3.Distance(bone.localPosition, positions[i]) > .00001f
                                        || Quaternion.Angle(bone.localRotation, rotations[i]) > .05f)).Select(bone => bone.name)) });
                    }
                }
            } while (Time.realtimeSinceStartupAsDouble - began < .28);
            yield return CombatWaitForFx();
            var rows = Diag.Snapshot(Diag.BufferCapacity);
            var witness = ReferenceGladeCombatEvidence.Inspect(rows, marker, _input.PlayerEntity.ID, _combatTargetID);
            _combatSteps.Add(JsonConvert.SerializeObject(new { marker, key = key.ToString(), attack, before,
                after = CombatState(), witness, windowPose, windowCapture, records = rows.SkipWhile(r => r.TraceId != marker).ToArray() }));
            Require(witness.WindowValid, "actual keyboard diagnostic marker retained in ring");
            _combatPoseObserved |= witness.PlayerAttempt && windowPose;
            _combatPoseCaptured |= witness.PlayerAttempt && windowCapture;
            _combatAttempt |= witness.PlayerAttempt; _combatDamage |= witness.PlayerDamage; _combatResponse |= witness.HostileAttempt;
            if (witness.PlayerLethal && rows.Any(r => r.Kind == "DeathHandled" && r.Category == "damage"
                && r.ActorId == _input.PlayerEntity.ID && r.TargetId == _combatTargetID && r.CauseTraceId == witness.DamageCause))
            { _combatLethal = true; _combatDeathX = targetX; _combatDeathY = targetY; }
            yield return CombatDismissEarnedAdvancement();
        }

        private IEnumerator CombatWaitForFx()
        {
            double began = Time.realtimeSinceStartupAsDouble;
            while (State() == "WaitingForFxResolution" || _input.ZoneRenderer?.WorldFx?.HasBlockingFx == true)
            {
                Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity)
                    && Time.realtimeSinceStartupAsDouble - began < 8, "live combat FX resolution; " + CombatState());
                yield return null;
            }
        }

        private IEnumerator CombatDismissEarnedAdvancement()
        {
            yield return null;
            for (int dismissed = 0; dismissed < 8; dismissed++)
            {
                double began = Time.realtimeSinceStartupAsDouble;
                while (MessageLog.HasPendingAnnouncement && State() != "AnnouncementOpen")
                {
                    Require(State() == "Normal" && Time.realtimeSinceStartupAsDouble - began < 2, "earned combat announcement opens naturally");
                    yield return null;
                }
                if (State() != "AnnouncementOpen") yield break;
                string message = (string)Field(_input.AnnouncementUI, "_message");
                const string prefix = "You advance to level ";
                Require(message != null && message.StartsWith(prefix, StringComparison.Ordinal) && message.EndsWith("!", StringComparison.Ordinal)
                    && int.TryParse(message.Substring(prefix.Length, message.Length - prefix.Length - 1), out int level)
                    && level >= 2 && level <= _input.PlayerEntity.GetStatValue("Level")
                    && MessageLog.GetRecentEntries(64).Any(e => e.Text == message), "exact naturally earned combat advancement");
                var actor = _input.PlayerEntity; var at = Cell(); int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(actor);
                string stats = CombatStats(actor), gear = CombatGear(actor);
                yield return Tap(Key.Escape);
                Require(ReferenceEquals(actor, _input.PlayerEntity) && ReferenceEquals(at, Cell()) && CombatStats(actor) == stats
                    && CombatGear(actor) == gear && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(actor) == energy,
                    "earned combat advancement dismissal preserves gains and costs no turn");
            }
            Require(State() != "AnnouncementOpen" && !MessageLog.HasPendingAnnouncement, "bounded combat announcement queue");
        }

        private static string CombatStats(Entity actor) => string.Join("|", actor.Statistics.OrderBy(p => p.Key)
            .Select(p => p.Key + ":" + p.Value.BaseValue + ":" + p.Value.Value + ":" + p.Value.Min + ":" + p.Value.Max));
        private static string CombatGear(Entity actor)
        {
            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return "none";
            string slots = string.Join("|", inventory.Objects.Select(e => "carried:" + e.ID)
                .Concat(inventory.EquippedItems.Select(p => "equipped:" + p.Key + ":" + p.Value.ID)).OrderBy(s => s));
            string body = actor.GetPart<Body>() == null ? "none" : string.Join("|", actor.GetPart<Body>().GetParts()
                .Where(part => part.Equipped != null).OrderBy(part => part.ID).Select(part => part.ID + ":" + part.Equipped.ID));
            string links = string.Join("|", inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct().OrderBy(e => e.ID)
                .Select(e => e.ID + ":" + e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)
                    + ":carried=" + e.GetPart<PhysicsPart>()?.InInventory?.ID + ":equipped=" + e.GetPart<PhysicsPart>()?.Equipped?.ID));
            return slots + ";body=" + body + ";links=" + links;
        }
        private string CombatState()
        {
            if (!_combatOnly && !_curationQuarantine) return null;
            if (_input == null) return _combatLastState ?? "input-not-ready";
            var actor = _input.PlayerEntity;
            if (actor == null) return _combatLastState ?? "player-not-ready";
            var at = _input.CurrentZone?.GetEntityCell(actor); var targetAt = _combatTarget == null ? null : _input.CurrentZone?.GetEntityCell(_combatTarget);
            _combatLastState = "state=" + State() + "; player=" + actor.ID + "@" + at?.X + "," + at?.Y
                + "; HP=" + actor.GetStatValue("Hitpoints") + "/" + actor.GetStat("Hitpoints")?.Max
                + "; dead=" + CombatSystem.IsDeathHandled(actor) + "; target=" + _combatTargetID + "@" + targetAt?.X + "," + targetAt?.Y
                + "; targetHP=" + _combatTarget?.GetStatValue("Hitpoints") + "; tick=" + _input.TurnManager.TickCount
                + "; energy=" + _input.TurnManager.GetEnergy(actor) + "; moves=" + _combatMoves + "; attacks=" + _combatAttacks
                + "; startingTonicsUsed=" + _combatTonicsUsed + "; startingRimeUses=" + _combatRimeUses
                + "; actualGear=" + CombatGear(actor) + "; stats=" + CombatStats(actor)
                + "; messages=" + string.Join(" | ", MessageLog.GetRecentEntries(12).Select(e => e.Text));
            return _combatLastState;
        }
    }
}
