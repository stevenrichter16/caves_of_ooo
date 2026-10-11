using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Opt-in authored NPC tactics. Grants supported skills on ObjectCreated;
    /// save/load preserves the real Skills/ActivatedAbilities owners and cooldowns.
    /// Only powers with an audited target preview are eligible for autonomous use.
    /// </summary>
    public sealed class CombatTacticsPart : Part
    {
        public override string Name => "CombatTactics";
        /// <summary>Semicolon-separated supported runtime skill class names.</summary>
        public string SkillClasses = "";
        /// <summary>Percent chance to replace the ordinary action with one eligible ability.</summary>
        public int AbilityChance = 45;
        /// <summary>Both caller and receiver must opt in to local same-faction assistance.</summary>
        public bool AssistAllies;
        /// <summary>Maximum Chebyshev radius; also bounded by each receiver's sight.</summary>
        public int AssistRadius = 6;
        /// <summary>Zero preserves ordinary closing. Positive values opt into bounded ranged spacing.</summary>
        public int PreferredRange;
        public bool RepositionForShot;
        /// <summary>Only Quench then Arc Bolt: both are real owned abilities with ordinary cooldowns.</summary>
        public bool PreferElementalSetup;

        public override bool HandleEvent(GameEvent e)
        {
            if (e?.ID == "ObjectCreated") GrantSkills();
            return true;
        }

        private void GrantSkills()
        {
            if (ParentEntity == null || ParentEntity.HasTag("Player") || !ParentEntity.HasTag("Creature"))
            { Record("TacticGrantRejected", null, "", "not-npc-creature"); return; }
            foreach (var raw in (SkillClasses ?? "").Split(';'))
            {
                string name = raw.Trim();
                if (name.Length == 0) continue;
                var skill = CreateSupportedSkill(name);
                if (skill == null)
                {
                    Record("TacticGrantRejected", null, "", "unsupported-skill", skillClass: name);
                    continue;
                }
                var skills = ParentEntity.GetPart<SkillsPart>();
                if (skills != null && skills.HasSkill(name)) continue;
                var abilities = ParentEntity.GetPart<ActivatedAbilitiesPart>();
                var spec = skill.DeclareActivatedAbility(ParentEntity);
                bool collision = false;
                if (abilities != null)
                    foreach (var entry in abilities.AbilityList)
                        if (entry != null && entry.Command == spec.Command) { collision = true; break; }
                // Adversarial grant collision: SkillsPart.AddSkill cannot roll back
                // a duplicate command. Refuse before attaching any orphan skill.
                if (collision)
                { Record("TacticGrantRejected", null, spec.Command, "command-collision", skillClass: name); continue; }
                if (abilities == null) ParentEntity.AddPart(new ActivatedAbilitiesPart());
                if (skills == null) { skills = new SkillsPart(); ParentEntity.AddPart(skills); }
                bool added = skills.AddSkill(skill, "authored-combat-kit");
                Record(added ? "TacticGranted" : "TacticGrantRejected", null, spec.Command,
                    added ? "" : "registration-refused", skillClass: name);
            }
        }

        private static BaseSkillPart CreateSupportedSkill(string name)
        {
            switch (name)
            {
                case nameof(Cudgel_Slam): return new Cudgel_Slam();
                case nameof(ShortBlades_Shank): return new ShortBlades_Shank();
                case nameof(LongBlades_Lunge): return new LongBlades_Lunge();
                case nameof(Axe_Berserk): return new Axe_Berserk();
                case nameof(Corrosion_AcidSpray): return new Corrosion_AcidSpray();
                case nameof(Cryomancy_IceLance): return new Cryomancy_IceLance();
                case nameof(Pyromancy_EmberSpit): return new Pyromancy_EmberSpit();
                case nameof(Hydromancy_Quench): return new Hydromancy_Quench();
                case nameof(Galvanism_ArcBolt): return new Galvanism_ArcBolt();
                default: return null;
            }
        }

        private sealed class Candidate
        {
            public ActivatedAbility Ability;
            public int Dx, Dy;
            public Cell TargetCell;
        }

        /// <summary>
        /// Attempts one supported ability against this exact live hostile target.
        /// False means the caller should perform its ordinary attack/movement.
        /// All randomness comes from rng; a refused command never consumes a second action.
        /// </summary>
        public bool TryUseAbility(Entity target, Zone zone, Random rng)
        {
            var actor = ParentEntity;
            if (actor == null || zone == null || rng == null)
                return Reject(target, null, "missing-context");
            if (actor.HasTag("Player") || actor.GetStatValue("Hitpoints") <= 0 ||
                target == null || target == actor || target.GetStatValue("Hitpoints") <= 0 ||
                !target.HasTag("Creature") || actor.SpatialZone != zone || target.SpatialZone != zone)
                return Reject(target, null, "invalid-actor-or-target");
            if (!FactionManager.IsHostile(actor, target)) return Reject(target, null, "not-hostile");
            if (!AIHelpers.TryGetVisibleTargetCell(actor, target, zone,
                actor.GetPart<BrainPart>()?.SightRadius ?? 10, out _))
                return Reject(target, null, "target-not-visible");
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var skills = actor.GetPart<SkillsPart>();
            if (abilities == null || skills == null) return Reject(target, null, "no-owned-skills");

            var eligible = new List<Candidate>();
            for (int i = 0; i < skills.SkillList.Count; i++)
            {
                var skill = skills.SkillList[i];
                if (skill == null || !SetupEligible(skill, target)) continue;
                var ability = abilities.GetAbility(skill.ActivatedAbilityID);
                if (ability == null) { Reject(target, null, "unregistered-skill"); continue; }
                if (!ability.IsUsable) { Reject(target, ability, "cooldown"); continue; }
                var spec = skill.DeclareActivatedAbility(actor);
                if (spec == null || spec.Command != ability.Command || skill.ParentEntity != actor)
                { Reject(target, ability, "invalid-registration"); continue; }
                var candidate = Preview(skill, ability, target, zone, Math.Min(spec.Range, ability.Range));
                if (candidate != null) eligible.Add(candidate);
            }
            if (eligible.Count == 0) return Reject(target, null, "no-eligible-ability");
            int chance = Math.Max(0, Math.Min(100, AbilityChance));
            if (chance == 0 || (chance < 100 && rng.Next(100) >= chance))
                return Reject(target, null, "chance");
            var picked = eligible.Count == 1 ? eligible[0] : eligible[rng.Next(eligible.Count)];
            var command = GameEvent.New(picked.Ability.Command);
            try
            {
                command.SetParameter("Zone", (object)zone);
                command.SetParameter("RNG", (object)rng);
                command.SetParameter("SourceCell", (object)zone.GetEntityCell(actor));
                command.SetParameter("TargetCell", (object)(picked.TargetCell ?? zone.GetEntityCell(target)));
                command.SetParameter("DirectionX", picked.Dx);
                command.SetParameter("DirectionY", picked.Dy);
                command.SetParameter("Range", picked.Ability.Range);
                actor.FireEvent(command);
                if (!command.Handled) return Reject(target, picked.Ability, "command-refused");
                Record("TacticUsed", target, picked.Ability.Command, "", picked.Ability.CooldownRemaining);
                return true;
            }
            finally { command.Release(); }
        }

        private Candidate Preview(BaseSkillPart skill, ActivatedAbility ability, Entity target, Zone zone, int range, Cell from = null)
        {
            var actor = ParentEntity;
            string weaponClass = skill is ShortBlades_Shank ? "Piercing"
                : skill is LongBlades_Lunge ? "LongBlades" : skill is Axe_Berserk ? "Axe"
                : skill is Cudgel_Slam ? "Cudgel" : null;
            bool projectile = IsProjectile(skill);
            if (weaponClass == null && !projectile) { Reject(target, ability, "unsupported-skill"); return null; }
            if (weaponClass != null && SkillCombatHelpers.FindEquippedWeaponOfClass(actor, weaponClass) == null)
            { Reject(target, ability, "weapon-class"); return null; }
            if (range < 1 || (from == null && SpatialQuery.Distance(zone, actor, target) > range))
            { Reject(target, ability, "range"); return null; }
            if (skill is Axe_Berserk)
            {
                if (actor.HasEffect<BerserkEffect>()) { Reject(target, ability, "already-berserk"); return null; }
                return new Candidate { Ability = ability };
            }
            if (skill is ShortBlades_Shank || skill is Cudgel_Slam)
            {
                // Preview the same chosen-cell policy the skill executes. An
                // ally in another direction no longer steals the selection;
                // an earlier occupant of this exact cell still blocks it.
                foreach (var cell in MultiCellAbilityQueries.AdjacentCells(zone, actor))
                    if (SkillCombatHelpers.FindAdjacentSkillTarget(actor, zone, cell, out var contact) == target
                        && (!(skill is Cudgel_Slam) || SafeSlamContact(target, zone, contact)))
                        return new Candidate { Ability = ability, TargetCell = contact };
                Reject(target, ability, "adjacent-target-mismatch"); return null;
            }
            // Preview the actual power's first-impact policy, including targetable
            // scenery. Eight rays support footprint contacts without aiming at an
            // off-ray anchor or firing through a different creature.
            var source = from ?? zone.GetEntityCell(actor);
            for (int dir = 0; dir < 8; dir++)
            {
                var next = zone.GetCellInDirection(source.X, source.Y, dir);
                if (next == null) continue;
                int dx = next.X - source.X, dy = next.Y - source.Y;
                Entity first;
                if (skill is LongBlades_Lunge)
                    first = LineTargeting.TraceFirstImpact(zone, actor, source.X, source.Y, dx, dy, range).HitEntity;
                else
                {
                    var hits = SkillLine.Collect(zone, actor, source.X, source.Y, dx, dy, range);
                    first = hits.Count == 0 ? null : hits[0];
                }
                // Seeing one part of a large body does not reveal every other
                // part. The selected first-impact contact must itself be seen.
                if (first == target && VisibleContactOnRay(zone, source, dx, dy, range, target))
                    return new Candidate { Ability = ability, Dx = dx, Dy = dy };
            }
            Reject(target, ability, "no-clear-target-ray"); return null;
        }

        // Match Slam's bounded footprint placement without moving anything.
        // Stop at the first obstruction; do not inspect a friend beyond a wall.
        // A brace may stop sooner, but never makes an otherwise unsafe aim eligible.
        private bool SafeSlamContact(Entity target, Zone zone, Cell contact)
        {
            var actor = ParentEntity;
            if (target.GetPart<RenderPart>()?.Visible == false || BrainPart.ArePartyAligned(actor, target)) return false;
            var eye = SpatialQuery.ClosestCell(zone, actor, contact.X, contact.Y);
            if (eye == null || !AIHelpers.HasLineOfSight(zone, eye.X, eye.Y, contact.X, contact.Y)
                || AIHelpers.ChebyshevDistance(eye.X, eye.Y, contact.X, contact.Y) > (actor.GetPart<BrainPart>()?.SightRadius ?? 10)) return false;
            int direction = MultiCellAbilityQueries.ContactDirection(zone, actor, contact);
            if (direction < 0) return false;
            var position = zone.GetEntityPosition(target);
            for (int step = 0; step < Cudgel_Slam.SLAM_DISTANCE; step++)
            {
                var next = zone.GetCellInDirection(position.x, position.y, direction);
                if (next == null) return true;
                if (!zone.CanPlaceFootprint(target, next.X, next.Y)
                    || MultiCellAbilityQueries.CreatureAtPlacement(zone, target, next.X, next.Y) != null)
                {
                    foreach (var cell in zone.GetOccupiedCells(target, next.X, next.Y))
                        if (cell != null)
                            foreach (var occupant in cell.Occupants)
                                if (occupant != null && occupant != target && occupant.HasTag("Creature")
                                    && (BrainPart.ArePartyAligned(actor, occupant) || !FactionManager.IsHostile(actor, occupant))) return false;
                    return true;
                }
                position = (next.X, next.Y);
            }
            return true;
        }

        private bool VisibleContactOnRay(Zone zone, Cell source, int dx, int dy, int range, Entity target)
        {
            int sight = ParentEntity.GetPart<BrainPart>()?.SightRadius ?? 10;
            for (int step = 1; step <= range; step++)
            {
                var cell = zone.GetCell(source.X + dx * step, source.Y + dy * step);
                if (cell == null) return false;
                foreach (var occupant in cell.Occupants)
                    if (occupant == target)
                        return step <= sight && AIHelpers.HasLineOfSight(zone, source.X, source.Y, cell.X, cell.Y);
                if (cell.IsSolid()) return false;
            }
            return false;
        }

        private static bool IsProjectile(BaseSkillPart skill) => skill is Corrosion_AcidSpray
            || skill is Cryomancy_IceLance || skill is Pyromancy_EmberSpit || skill is Hydromancy_Quench || skill is Galvanism_ArcBolt;
        private bool SetupEligible(BaseSkillPart skill, Entity target)
        {
            if (!PreferElementalSetup) return true;
            bool soaked = target?.GetEffect<WetEffect>()?.Moisture > .2f;
            return skill is Hydromancy_Quench ? !soaked : !(skill is Galvanism_ArcBolt) || soaked;
        }

        /// <summary>One ordinary step or a deliberate cooldown wait. Never moves and casts together.</summary>
        public bool TryPositionForShot(Entity target, Zone zone)
        {
            var actor = ParentEntity; var brain = actor?.GetPart<BrainPart>();
            if ((!RepositionForShot && PreferredRange <= 0) || actor == null || zone == null || target == null
                || actor.HasTag("Player") || actor.SpatialZone != zone || target.SpatialZone != zone
                || actor.GetStatValue("Hitpoints") <= 0 || target.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(actor) || CombatSystem.IsDeathHandled(target)
                || brain?.CurrentZone != zone || brain.HasGoal<NoFightGoal>()
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !FactionManager.IsHostile(actor,target)) return false;
            var source = zone.GetEntityCell(actor); var aim = zone.GetEntityCell(target);
            int distance = SpatialQuery.Distance(zone, actor, target);
            if (source == null || aim == null || distance > brain.SightRadius
                || !AIHelpers.HasLineOfSight(zone, source.X, source.Y, aim.X, aim.Y)) return false;
            var skills = actor.GetPart<SkillsPart>(); var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            if (skills == null || abilities == null) return false;
            var shots = new List<BaseSkillPart>();
            foreach (var skill in skills.SkillList)
                if (IsProjectile(skill) && SetupEligible(skill,target) && skill.ParentEntity == actor
                    && abilities.GetAbility(skill.ActivatedAbilityID) != null) shots.Add(skill);
            if (shots.Count == 0) return false;
            bool Clear(Cell at)
            {
                foreach (var skill in shots)
                {
                    var ability=abilities.GetAbility(skill.ActivatedAbilityID); var spec=skill.DeclareActivatedAbility(actor);
                    if (ability.Command == spec.Command && Preview(skill,ability,target,zone,Math.Min(spec.Range,ability.Range),at) != null) return true;
                }
                return false;
            }
            int preferred = Math.Min(4, Math.Max(0, PreferredRange));
            if (preferred > 0 && distance < preferred
                && AIHelpers.TryStepAway(actor,zone,source.X,source.Y,aim.X,aim.Y))
            { Record("TacticPositioned",target,"","backstep"); return true; }
            bool clear = Clear(source);
            if (preferred > 0 && distance == preferred && clear)
            { Record("TacticPositioned",target,"","hold-range"); return true; }
            if (!RepositionForShot || clear) return false;
            int attempted = 0;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int bestDirection = -1, bestCost = int.MaxValue;
                Cell best = null;
                for (int dir = 0; dir < 8; dir++)
                {
                    if ((attempted & (1 << dir)) != 0) continue;
                    var next = zone.GetCellInDirection(source.X, source.Y, dir);
                    if (next == null || !zone.CanPlaceFootprint(actor, next.X, next.Y)
                        || MultiCellAbilityQueries.CreatureAtPlacement(zone, actor, next.X, next.Y) != null || !Clear(next)) continue;
                    int cost = TerrainNavigationWeight.ForStep(zone, next.X, next.Y, actor);
                    if (cost >= bestCost) continue;
                    bestDirection = dir; best = next; bestCost = cost;
                    // Costs cannot be negative. The first clean square already
                    // wins, preserving the original direction order on ties.
                    if (cost == 0) break;
                }
                if (best == null) break;
                attempted |= 1 << bestDirection;
                if (MovementSystem.TryMoveTo(actor, zone, best.X, best.Y))
                { Record("TacticPositioned",target,"","clear-shot-step"); return true; }
            }
            return false;
        }

        /// <summary>
        /// Alerts idle, living, opted-in faction mates in sight of both the caller
        /// and threat. Receiver alerts do not relay; existing fights and parties
        /// remain authoritative. This performs no zone generation or global scan.
        /// </summary>
        public void AlertAllies(Entity target, Zone zone)
        {
            var actor = ParentEntity;
            if (!AssistAllies || actor == null || target == null || zone == null ||
                actor.SpatialZone != zone || target.SpatialZone != zone ||
                actor.GetStatValue("Hitpoints") <= 0 || target.GetStatValue("Hitpoints") <= 0 ||
                !FactionManager.IsHostile(actor, target))
            { Record("AssistRejected", target, "", "caller-ineligible"); return; }
            string faction = FactionManager.GetFaction(actor);
            if (string.IsNullOrEmpty(faction)) { Record("AssistRejected", target, "", "no-faction"); return; }
            var source = zone.GetEntityCell(actor); var threat = zone.GetEntityCell(target);
            int count = 0;
            foreach (var ally in zone.GetEntitiesWithTag("Creature"))
            {
                if (ally == actor || ally == target) continue;
                var tactics = ally.GetPart<CombatTacticsPart>();
                if (tactics == null) continue;
                var brain = ally.GetPart<BrainPart>();
                string reason = null;
                if (!tactics.AssistAllies || brain == null || brain.Passive || ally.HasTag("Player") ||
                    ally.GetStatValue("Hitpoints") <= 0) reason = "receiver-unwilling";
                else if (FactionManager.GetFaction(ally) != faction) reason = "different-faction";
                else if (BrainPart.ArePartyAligned(ally, target) || brain.PartyLeader != null) reason = "party";
                else if (brain.Target != null) reason = "already-engaged";
                else if (brain.PeekGoal() is NoFightGoal calm && (calm.Duration <= 0 || calm.Age < calm.Duration))
                    reason = "controlled";
                var cell = zone.GetEntityCell(ally);
                if (reason == null && (cell == null || AssistRadius < 1 || tactics.AssistRadius < 1 ||
                    SpatialQuery.Distance(zone, actor, ally) > Math.Min(AssistRadius, tactics.AssistRadius) ||
                    SpatialQuery.Distance(zone, ally, target) > brain.SightRadius)) reason = "range";
                if (reason == null && (!AIHelpers.HasLineOfSight(zone, cell.X, cell.Y, source.X, source.Y) ||
                    !AIHelpers.HasLineOfSight(zone, cell.X, cell.Y, threat.X, threat.Y))) reason = "occluded";
                if (reason != null) { tactics.Record("AssistRejected", target, "", reason); continue; }
                brain.SetPersonallyHostile(target, alertAllies: false);
                brain.InvalidateHostileCache();
                count++;
            }
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", "AssistAlert", actor: actor, target: target, payload: new { allies = count, radius = AssistRadius });
        }

        private bool Reject(Entity target, ActivatedAbility ability, string reason)
        {
            Record("TacticRejected", target, ability?.Command ?? "", reason, ability?.CooldownRemaining ?? 0);
            return false;
        }
        private void Record(string kind, Entity target, string command, string reason, int cooldown = 0, string skillClass = "")
        {
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", kind, actor: ParentEntity, target: target,
                    payload: new { command, reason, cooldown, skillClass });
        }
    }
}
