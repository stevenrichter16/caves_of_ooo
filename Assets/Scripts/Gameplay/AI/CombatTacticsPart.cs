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
                case nameof(ShortBlades_Shank): return new ShortBlades_Shank();
                case nameof(LongBlades_Lunge): return new LongBlades_Lunge();
                case nameof(Axe_Berserk): return new Axe_Berserk();
                case nameof(Corrosion_AcidSpray): return new Corrosion_AcidSpray();
                case nameof(Cryomancy_IceLance): return new Cryomancy_IceLance();
                case nameof(Pyromancy_EmberSpit): return new Pyromancy_EmberSpit();
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
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var skills = actor.GetPart<SkillsPart>();
            if (abilities == null || skills == null) return Reject(target, null, "no-owned-skills");

            var eligible = new List<Candidate>();
            for (int i = 0; i < skills.SkillList.Count; i++)
            {
                var skill = skills.SkillList[i];
                if (skill == null) continue;
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

        private Candidate Preview(BaseSkillPart skill, ActivatedAbility ability, Entity target, Zone zone, int range)
        {
            var actor = ParentEntity;
            string weaponClass = skill is ShortBlades_Shank ? "Piercing"
                : skill is LongBlades_Lunge ? "LongBlades" : skill is Axe_Berserk ? "Axe" : null;
            bool projectile = skill is Corrosion_AcidSpray || skill is Cryomancy_IceLance || skill is Pyromancy_EmberSpit;
            if (weaponClass == null && !projectile) { Reject(target, ability, "unsupported-skill"); return null; }
            if (weaponClass != null && SkillCombatHelpers.FindEquippedWeaponOfClass(actor, weaponClass) == null)
            { Reject(target, ability, "weapon-class"); return null; }
            if (range < 1 || SpatialQuery.Distance(zone, actor, target) > range)
            { Reject(target, ability, "range"); return null; }
            if (skill is Axe_Berserk)
            {
                if (actor.HasEffect<BerserkEffect>()) { Reject(target, ability, "already-berserk"); return null; }
                return new Candidate { Ability = ability };
            }
            if (skill is ShortBlades_Shank)
            {
                // Preview the same chosen-cell policy the skill executes. An
                // ally in another direction no longer steals the selection;
                // an earlier occupant of this exact cell still blocks it.
                foreach (var cell in MultiCellAbilityQueries.AdjacentCells(zone, actor))
                    if (SkillCombatHelpers.FindAdjacentSkillTarget(actor, zone, cell, out var contact) == target)
                        return new Candidate { Ability = ability, TargetCell = contact };
                Reject(target, ability, "adjacent-target-mismatch"); return null;
            }
            // Preview the actual power's first-impact policy, including targetable
            // scenery. Eight rays support footprint contacts without aiming at an
            // off-ray anchor or firing through a different creature.
            var source = zone.GetEntityCell(actor);
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
                if (first == target) return new Candidate { Ability = ability, Dx = dx, Dy = dy };
            }
            Reject(target, ability, "no-clear-target-ray"); return null;
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
