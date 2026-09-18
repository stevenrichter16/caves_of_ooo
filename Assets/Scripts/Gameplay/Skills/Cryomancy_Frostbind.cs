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
    /// not a swing). Finds an adjacent creature, applies
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

            var target = MultiCellAbilityQueries.FirstAdjacentCreature(ctx.Zone, actor, out _);
            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has no target to frostbind.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            target.ApplyEffect(new RootedEffect(FROSTBIND_DURATION), actor, ctx.Zone);
        
            return true;
        }
    }
}
