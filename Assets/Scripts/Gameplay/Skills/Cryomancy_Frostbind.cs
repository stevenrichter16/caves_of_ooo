using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy active ability: lock an adjacent creature's MOVEMENT
    /// for <see cref="FROSTBIND_DURATION"/> turns by applying
    /// <see cref="RootedEffect"/>. The target can still attack and
    /// cast — only their cell is locked. Distinct from Stunned
    /// (blocks all action) — Frostbind is the only ability that
    /// LOCKS MOVEMENT BUT NOT ACTIONS.
    ///
    /// <para><b>Mechanic:</b> no weapon class required (it's a spell,
    /// not a swing). Uses the selected adjacent creature, applies
    /// <see cref="RootedEffect"/>(<see cref="FROSTBIND_DURATION"/>).
    /// The effect overrides AllowMovement => false so the target's
    /// BeforeMove events are rejected; AllowAction stays true so the
    /// target can still swing at adjacent foes.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Cryomancy_Frostbind</c>):
    /// "the only ability that locks a target's MOVEMENT but not their
    /// actions."</para>
    /// </summary>
    public class Cryomancy_Frostbind : SpellSkillPart
    {
        public override string Name => nameof(Cryomancy_Frostbind);

        public const int COOLDOWN = 35;
        public const int FROSTBIND_DURATION = 4;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Frostbind",
                Command = "CommandFrostbind",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            var target = SkillCombatHelpers.FindAdjacentSkillTarget(actor, ctx.Zone, ctx.TargetCell, out _);
            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has no target to frostbind.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            // Observe the intrinsic install/stack boundary. BeforeApplyEffect
            // observers can independently add a root; EffectApplied observers
            // can remove or replace ours. Neither is proof that this cast left
            // additional restraint on its selected target.
            var incoming = new RootedEffect(FROSTBIND_DURATION);
            RootedEffect prior = null, imposed = null;
            int priorDuration = 0;
            bool applied = target.ApplyEffectWithReceipt(incoming, actor, ctx.Zone,
                beforeChange: () =>
                {
                    prior = target.GetEffect<RootedEffect>();
                    priorDuration = prior?.Duration ?? 0;
                },
                afterChange: () =>
                {
                    var current = target.GetEffect<RootedEffect>();
                    if (current == incoming || (current != null && current == prior))
                        imposed = current;
                });
            bool meaningful = applied && imposed != null && imposed.Owner == target
                && target.GetEffect<RootedEffect>() == imposed && priorDuration >= 0
                && imposed.Duration > priorDuration;
            if (meaningful && CanProvoke(actor, target, ctx.Zone))
                target.GetPart<BrainPart>()?.SetPersonallyHostile(actor);

            // Preserve the established committed-cast payment even if an
            // immunity/lifecycle listener rejects the effect. Only provocation
            // depends on the actual remaining restraint.
            return true;
        }

        private static bool CanProvoke(Entity actor, Entity target, Zone zone)
        {
            return actor != target && !BrainPart.ArePartyAligned(actor, target)
                && actor.SpatialZone == zone && target.SpatialZone == zone
                && zone.GetEntityCell(actor) != null && zone.GetEntityCell(target) != null
                && actor.GetStatValue("Hitpoints") > 0 && target.GetStatValue("Hitpoints") > 0
                && !CombatSystem.IsDeathHandled(actor) && !CombatSystem.IsDeathHandled(target);
        }
    }
}
