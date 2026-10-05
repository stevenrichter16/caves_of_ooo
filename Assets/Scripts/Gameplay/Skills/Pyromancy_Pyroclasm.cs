using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy active ability: detonate an adjacent target's
    /// <see cref="BurningEffect"/>. The target's Burning is consumed,
    /// and every elemental target in the 3×3 area centered on the
    /// chosen contact receives <see cref="DAMAGE_PER_BURN_TURN"/> ×
    /// consumed-Duration base Heat damage, with ordinary spell modifiers.
    /// Distinct from Overload (chain through targets),
    /// AlchemicCatalyst (force-fire reactions), and AcidPool (cell
    /// residue) — Pyroclasm is the only ability that CONSUMES A
    /// STATUS EFFECT FOR DAMAGE.
    ///
    /// <para><b>Mechanic:</b> no weapon class required (it's a
    /// spell, not a swing). Selects adjacent burning matter, queries
    /// their <see cref="StatusEffectsPart"/> for BurningEffect.
    /// If absent: rejection (no_target_burning). If present: the
    /// effect's Duration is read, the effect is removed, and a
    /// 3×3-cell AOE deals
    /// <c>damageAmount = Duration × DAMAGE_PER_BURN_TURN</c> Heat
    /// damage to every elemental target in the radius (including the
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

            // Keep burning objects eligible, while making an explicit physical
            // cell authoritative. A distant anchor cannot relocate the blast.
            var target = FindBurningTarget(actor, ctx.Zone, ctx.TargetCell, out var contact);

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
                // The common spell path preserves structural HP and both
                // elemental aliases while applying the caster's investment.
                SpellDamageHelpers.ApplySpellDamage(e, aoeAmount, "Heat", actor, ctx.Zone, "Fire");
                hits++;
            }

            MessageLog.Add(actor.GetDisplayName() + "'s pyroclasm detonates! "
                + hits + " caught in the blast (" + aoeAmount + " base Fire damage).");
        
            return true;
        }

        private static Entity FindBurningTarget(Entity actor, Zone zone, Cell selected, out Cell contact)
        {
            contact = null;
            if (!IsPlacedOwner(actor, zone)) return null;
            if (selected != null)
            {
                if (selected.ParentZone != zone || zone.GetCell(selected.X, selected.Y) != selected
                    || SpatialQuery.DistanceToCell(zone, actor, selected.X, selected.Y) != 1) return null;
                return FindBurningAt(actor, zone, selected, out contact);
            }

            // Legacy internal calls without a selected cell keep perimeter order.
            foreach (var cell in MultiCellAbilityQueries.AdjacentCells(zone, actor))
            {
                var target = FindBurningAt(actor, zone, cell, out contact);
                if (target != null) return target;
            }
            return null;
        }

        private static Entity FindBurningAt(Entity actor, Zone zone, Cell cell, out Cell contact)
        {
            contact = null;
            foreach (var candidate in cell.Occupants)
            {
                if (!AbilityTargeting.IsElementalTarget(candidate, actor)
                    || !IsPlacedOwner(candidate, zone) || !candidate.HasEffect<BurningEffect>()) continue;
                foreach (var occupied in zone.GetOccupiedCells(candidate))
                    if (occupied == cell) { contact = cell; return candidate; }
            }
            return null;
        }

        private static bool IsPlacedOwner(Entity owner, Zone zone)
        {
            if (owner == null || zone == null || owner.SpatialZone != zone || zone.GetEntityCell(owner) == null
                || owner.GetStatValue("Hitpoints", 1) <= 0 || CombatSystem.IsDeathHandled(owner)) return false;
            var structure = owner.GetPart<DestructiblePart>();
            if (structure != null && (structure.Gone || structure.IsDestroyed)) return false;
            var physics = owner.GetPart<PhysicsPart>();
            return physics == null || (physics.ParentEntity == owner
                && physics.InInventory == null && physics.Equipped == null);
        }
    }
}
