using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism active ability: a lightning bolt that travels in a
    /// chosen direction up to <see cref="OVERLOAD_RANGE"/> cells.
    /// Each Wet OR Electrified creature in the line takes
    /// <see cref="OVERLOAD_DAMAGE"/> Electric damage AND the chain
    /// continues. Non-conductive (dry, unelectrified) creatures
    /// break the chain. Distinct from Pyroclasm (consume single
    /// target's stack) — Overload chains through CONDUCTORS.
    ///
    /// <para><b>Mechanic:</b> SelfCentered targeting reads direction
    /// from ctx (DirectionLine semantics). Walks the line one cell
    /// at a time. For each creature encountered: if it has
    /// <see cref="WetEffect"/> OR <see cref="ElectrifiedEffect"/>,
    /// it takes Electric damage AND propagation continues; otherwise
    /// the chain breaks immediately. Walls also break the chain.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Galvanism_Overload</c>):
    /// "the only chain-through-targets mechanic. Pyroclasm radiates
    /// from a target; Overload travels through multiple targets in
    /// sequence."</para>
    /// </summary>
    public class Galvanism_Overload : SpellSkillPart
    {
        public override string Name => nameof(Galvanism_Overload);

        public const int COOLDOWN = 50;
        public const int OVERLOAD_RANGE = 8;
        public const int OVERLOAD_DAMAGE = 8;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Overload",
                Command = "CommandOverload",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = OVERLOAD_RANGE,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            int x = actorPos.x, y = actorPos.y;
            var targets = new List<Entity>();
            var seen = new HashSet<Entity>();
            for (int step = 0; step < OVERLOAD_RANGE; step++)
            {
                int nx = x + dx, ny = y + dy;
                if (!ctx.Zone.InBounds(nx, ny)) break;
                var cell = ctx.Zone.GetCell(nx, ny);
                if (cell == null) break;
                if (cell.IsSolid()) break;
                SpellFxCapture.PathCell(ctx.Zone, nx, ny);

                // Find something the charge can pass through in this cell.
                // Creatures first, then scenery — a brine pool is 95%
                // conductive salt water and is exactly what a player expects
                // to shock, but it is not a creature, so the old
                // creature-only scan skipped straight over it and the chain
                // reported "finds no conductors".
                Entity creature = null;
                for (int i = 0; i < cell.Occupants.Count; i++)
                {
                    var e = cell.Occupants[i];
                    if (AbilityTargeting.IsCreatureTarget(e, actor)) { creature = e; break; }
                }
                if (creature == null)
                {
                    for (int i = 0; i < cell.Occupants.Count; i++)
                    {
                        var e = cell.Occupants[i];
                        if (e != null && !e.Tags.ContainsKey("Creature")
                            && AbilityTargeting.IsElementalTarget(e, actor))
                        { creature = e; break; }
                    }
                }

                if (creature == null)
                {
                    // Nothing here — chain continues through empty cells.
                    x = nx; y = ny;
                    continue;
                }

                if (seen.Contains(creature)) { x = nx; y = ny; continue; }

                // Conductivity check. Two ways to qualify: soaked or
                // already charged (the creature route), or MADE of
                // something that conducts (the scenery route — copper
                // pipe, brine pool). The second is what the matrix
                // already encodes, so ask it rather than restating the
                // tag list here and letting the two drift.
                var sep = creature.GetPart<StatusEffectsPart>();
                bool conductive = (sep != null
                        && (sep.HasEffect<WetEffect>() || sep.HasEffect<ElectrifiedEffect>()))
                    || ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), creature)
                        == ObjectStatusVerdict.Applies && !creature.HasTag("Creature");
                if (!conductive)
                {
                    // First non-conductor breaks the chain. Diag emit
                    // surfaces the dropped-chain reason.
                    EmitSkillRejectedDiag(ctx, "chain_broken_non_conductor");
                    break;
                }

                targets.Add(creature);
                seen.Add(creature);
                x = nx; y = ny;
            }

            int hits = 0;
            foreach (var target in targets)
            {
                if (ctx.Zone.GetEntityCell(target) == null) continue;
                var elecDmg = new Damage(OVERLOAD_DAMAGE);
                elecDmg.AddAttribute("Electric");
                elecDmg.AddAttribute("Lightning");
                DestructionSystem.RouteDamage(target, elecDmg, actor, ctx.Zone);
                hits++;
            }

            if (hits == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s overload finds no conductors.");
                return false;
            }
            MessageLog.Add(actor.GetDisplayName() + "'s overload chains through "
                + hits + " conductor" + (hits == 1 ? "" : "s") + "!");
        
            return true;
        }
    }
}
