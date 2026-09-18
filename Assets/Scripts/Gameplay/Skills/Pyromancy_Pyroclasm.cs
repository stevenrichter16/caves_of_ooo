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
    public class Pyromancy_Pyroclasm : SpellSkillPart
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

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            // Find burning matter on the physical perimeter. Remember the
            // actual contacted cell so a distant anchor cannot relocate the blast.
            Entity target = null;
            Cell contact = null;
            foreach (var cell in MultiCellAbilityQueries.AdjacentCells(ctx.Zone, actor))
            {
                foreach (var entity in cell.Occupants)
                {
                    if (!AbilityTargeting.IsElementalTarget(entity, actor)) continue;
                    if (!entity.HasEffect<BurningEffect>()) continue;
                    target = entity;
                    contact = cell;
                    break;
                }
                if (target != null) break;
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " finds no burning target adjacent.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            // Consume the Burning effect, capture its remaining Duration.
            var targetSep = target.GetPart<StatusEffectsPart>();
            var burning = targetSep.GetEffect<BurningEffect>();
            int consumedDuration = burning?.Duration ?? 1;
            if (consumedDuration < 1) consumedDuration = 1; // floor — DURATION_INDEFINITE could be ≤0
            // Copy the consumed cost before removing the status or damaging its owner.
            SpellFxCapture.RecordOutcome(ctx.Zone, target, "consumed-status", "Burning", consumedDuration);
            targetSep.RemoveEffect<BurningEffect>();

            int aoeAmount = consumedDuration * DAMAGE_PER_BURN_TURN;

            // Capture physical owners once before damage can alter occupancy.
            var cells = MultiCellAbilityQueries.RadiusCells(ctx.Zone, contact.X, contact.Y, 1);
            var targets = MultiCellAbilityQueries.SnapshotOccupants(cells, actor);
            foreach (var cell in cells) SpellFxCapture.AffectCell(ctx.Zone, cell.X, cell.Y);

            int hits = 0;
            for (int t = 0; t < targets.Count; t++)
            {
                var e = targets[t];
                // A target an earlier hit already removed (chain
                // destruction) gets no phantom damage.
                if (ctx.Zone.GetEntityCell(e) == null || !AbilityTargeting.IsElementalTarget(e, actor)) continue;
                var fireDmg = new Damage(aoeAmount);
                fireDmg.AddAttribute("Fire");
                fireDmg.AddAttribute("Heat");
                // RouteDamage, not ApplyDamage: scenery keeps its
                // hitpoints on a DestructiblePart, and ApplyDamage
                // deliberately early-returns on anything with no
                // Hitpoints stat — so elemental damage aimed at a
                // tree or a barrel was silently discarded.
                DestructionSystem.RouteDamage(e, fireDmg, actor, ctx.Zone);
                hits++;
            }

            MessageLog.Add(actor.GetDisplayName() + "'s pyroclasm detonates! "
                + hits + " caught in the blast (" + aoeAmount + " Fire damage each).");
        
            return true;
        }
    }
}
