using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy active ability: detonate an adjacent target's
    /// <see cref="BurningEffect"/>. The target's Burning is consumed,
    /// and every creature in the 3×3 area centered on the target
    /// takes <see cref="DAMAGE_PER_BURN_TURN"/> × consumed-Duration
    /// Heat damage. Distinct from Overload (chain through targets),
    /// AlchemicCatalyst (force-fire reactions), and AcidPool (cell
    /// residue) — Pyroclasm is the only ability that CONSUMES A
    /// STATUS EFFECT FOR DAMAGE.
    ///
    /// <para><b>Mechanic:</b> no weapon class required (it's a
    /// spell, not a swing). Finds an adjacent creature, queries
    /// their <see cref="StatusEffectsPart"/> for BurningEffect.
    /// If absent: rejection (no_target_burning). If present: the
    /// effect's Duration is read, the effect is removed, and a
    /// 3×3-cell AOE deals
    /// <c>damageAmount = Duration × DAMAGE_PER_BURN_TURN</c> Heat
    /// damage to every creature in the radius (including the
    /// detonation target).</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Pyromancy_Pyroclasm</c>):
    /// "the only ability that consumes a status effect for damage."</para>
    /// </summary>
    public class Pyromancy_Pyroclasm : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_Pyroclasm);

        public const int COOLDOWN = 40;
        public const int DAMAGE_PER_BURN_TURN = 3;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Pyroclasm",
                Command = "CommandPyroclasm",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            // Find adjacent creature with BurningEffect.
            Entity target = null;
            for (int dir = 0; dir < 8 && target == null; dir++)
            {
                var cell = ctx.Zone.GetCellInDirection(actorPos.x, actorPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    var sep = e.GetPart<StatusEffectsPart>();
                    if (sep != null && sep.HasEffect<BurningEffect>())
                    {
                        target = e;
                        break;
                    }
                }
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " finds no burning target adjacent.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Consume the Burning effect, capture its remaining Duration.
            var targetSep = target.GetPart<StatusEffectsPart>();
            var burning = targetSep.GetEffect<BurningEffect>();
            int consumedDuration = burning?.Duration ?? 1;
            if (consumedDuration < 1) consumedDuration = 1; // floor — DURATION_INDEFINITE could be ≤0
            targetSep.RemoveEffect<BurningEffect>();

            int aoeAmount = consumedDuration * DAMAGE_PER_BURN_TURN;

            // 3×3 AOE centered on target. Iterate the 9 cells (target
            // + 8 neighbors). Damage each creature found.
            var targetPos = ctx.Zone.GetEntityPosition(target);
            if (targetPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "target_not_in_zone");
                return;
            }

            int hits = 0;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = targetPos.x + ox;
                    int cy = targetPos.y + oy;
                    if (!ctx.Zone.InBounds(cx, cy)) continue;
                    var cell = ctx.Zone.GetCell(cx, cy);
                    if (cell == null) continue;
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        var e = cell.Objects[i];
                        if (e == null || e == actor) continue;
                        if (!e.Tags.ContainsKey("Creature")) continue;
                        var fireDmg = new Damage(aoeAmount);
                        fireDmg.AddAttribute("Fire");
                        fireDmg.AddAttribute("Heat");
                        CombatSystem.ApplyDamage(e, fireDmg, actor, ctx.Zone);
                        hits++;
                    }
                }
            }

            MessageLog.Add(actor.GetDisplayName() + "'s pyroclasm detonates! "
                + hits + " caught in the blast (" + aoeAmount + " Fire damage each).");
        }
    }
}
