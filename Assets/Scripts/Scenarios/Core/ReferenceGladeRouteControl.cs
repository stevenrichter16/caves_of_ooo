using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios
{
    /// <summary>Read-only preflight for the biome keyboard audit's original Calm.
    /// It does not cast, reserve a target, change a goal or suppress gameplay AI.</summary>
    public static class ReferenceGladeRouteControl
    {
        /// <summary>Only an actual current, living owner with an unfinished,
        /// stationary top NoFight goal is protected. Expired or buried goals do
        /// not excuse the audit's ordinary hostile clearance.</summary>
        public static bool HasLiveCalm(Zone zone, Entity owner)
        {
            if (!CurrentLiving(zone, owner)) return false;
            var brain = owner.GetPart<BrainPart>();
            var goal = brain?.PeekGoal() as NoFightGoal;
            return brain != null && ReferenceEquals(brain.ParentEntity, owner)
                && ReferenceEquals(brain.CurrentZone, zone) && goal != null
                && ReferenceEquals(goal.ParentBrain, brain) && !goal.Wander && !goal.Finished();
        }

        /// <summary>Find a real bound original ability and exact visible first
        /// impact along a native eight-direction ray. Outputs are only advice;
        /// the keyboard audit must verify real payment and target effect after
        /// input, and freshly recheck all world state before every next action.</summary>
        public static bool TryCalm(Zone zone, Entity actor, Entity target, Guid originalAbility,
            out int slot, out int dx, out int dy)
        {
            slot = -1; dx = dy = 0;
            if (originalAbility == Guid.Empty || !CurrentLiving(zone, actor)
                || !CurrentLiving(zone, target) || ReferenceEquals(actor, target)
                || !target.HasTag("Creature") || HasLiveCalm(zone, target)) return false;
            var brain = target.GetPart<BrainPart>(); var render = target.GetPart<RenderPart>();
            if (brain == null || !ReferenceEquals(brain.ParentEntity, target)
                || !ReferenceEquals(brain.CurrentZone, zone) || brain.HasGoal<NoFightGoal>() || render == null
                || !ReferenceEquals(render.ParentEntity, target) || !render.Visible
                || !(FactionManager.IsHostile(target, actor) || brain.IsPersonallyHostileTo(actor) || ReferenceEquals(brain.Target, actor))) return false;
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null || !ReferenceEquals(abilities.ParentEntity, actor)) return false;
            for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
            {
                var ability = abilities.GetAbilityBySlot(i);
                if (ability?.ID == originalAbility && ability.Command == "CommandCalm"
                    && ability.CooldownRemaining == 0 && ability.TargetingMode == AbilityTargetingMode.DirectionLine
                    && ability.Range == Spellcraft_Calm.RANGE)
                { slot = i; break; }
            }
            if (slot < 0) return false;
            var from = zone.GetEntityCell(actor);
            foreach (var at in zone.GetOccupiedCells(target))
            {
                if (!at.IsVisible) continue;
                int x = at.X - from.X, y = at.Y - from.Y;
                if (x != 0 && y != 0 && Math.Abs(x) != Math.Abs(y)) continue;
                if (Math.Max(Math.Abs(x), Math.Abs(y)) > Spellcraft_Calm.RANGE || (x == 0 && y == 0)) continue;
                int tx = Math.Sign(x), ty = Math.Sign(y);
                for (int step = 1; step <= Spellcraft_Calm.RANGE; step++)
                {
                    var cell = zone.GetCell(from.X + tx * step, from.Y + ty * step);
                    if (cell == null) break;
                    Entity first = null;
                    foreach (var e in cell.Occupants)
                        if (AbilityTargeting.IsCreatureTarget(e, actor)) { first = e; break; }
                    if (first == null)
                        foreach (var e in cell.Occupants)
                            if (e != null && !e.HasTag("Creature") && AbilityTargeting.IsElementalTarget(e, actor)) { first = e; break; }
                    if (ReferenceEquals(first, target)) { dx = tx; dy = ty; return true; }
                    if (first != null || cell.IsSolid()) break;
                }
            }
            slot = -1;
            return false;
        }
        private static bool CurrentLiving(Zone zone, Entity owner)
        {
            if (zone == null || owner == null) return false;
            var cell = zone.GetEntityCell(owner);
            return cell != null && cell.Objects.Contains(owner)
                && !CombatSystem.IsDeathHandled(owner) && owner.GetStatValue("Hitpoints") > 0;
        }
    }
}
