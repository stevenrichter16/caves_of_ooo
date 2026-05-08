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
    public class Galvanism_Overload : BaseSkillPart
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

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            int x = actorPos.x, y = actorPos.y;
            int hits = 0;
            for (int step = 0; step < OVERLOAD_RANGE; step++)
            {
                int nx = x + dx, ny = y + dy;
                if (!ctx.Zone.InBounds(nx, ny)) break;
                var cell = ctx.Zone.GetCell(nx, ny);
                if (cell == null) break;
                if (cell.IsSolid()) break;

                // Find a creature in this cell.
                Entity creature = null;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (e.Tags.ContainsKey("Creature")) { creature = e; break; }
                }

                if (creature == null)
                {
                    // No creature here — chain continues through empty cells.
                    x = nx; y = ny;
                    continue;
                }

                // Conductivity check.
                var sep = creature.GetPart<StatusEffectsPart>();
                bool conductive = sep != null
                    && (sep.HasEffect<WetEffect>() || sep.HasEffect<ElectrifiedEffect>());
                if (!conductive)
                {
                    // First non-conductor breaks the chain. Diag emit
                    // surfaces the dropped-chain reason.
                    EmitSkillRejectedDiag(ctx, "chain_broken_non_conductor");
                    break;
                }

                // Damage + chain continues.
                var elecDmg = new Damage(OVERLOAD_DAMAGE);
                elecDmg.AddAttribute("Electric");
                elecDmg.AddAttribute("Lightning");
                CombatSystem.ApplyDamage(creature, elecDmg, actor, ctx.Zone);
                hits++;
                x = nx; y = ny;
            }

            if (hits == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s overload finds no conductors.");
                return;
            }
            MessageLog.Add(actor.GetDisplayName() + "'s overload chains through "
                + hits + " conductor" + (hits == 1 ? "" : "s") + "!");
        }
    }
}
