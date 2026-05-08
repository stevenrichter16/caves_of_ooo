using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy active ability: enter self-stasis for
    /// <see cref="HibernatingEffect.HEAL_PERCENT_PER_TURN"/>%-per-turn
    /// healing for <see cref="HIBERNATE_DURATION"/> turns. While
    /// hibernating, the actor cannot act (AllowAction=false) but
    /// gains 100% Heat + Cold resistance. Distinct from every other
    /// ability — Hibernate is the only SELF-STASIS-WITH-HEALING.
    ///
    /// <para><b>Mechanic:</b> SelfCentered, no targeting, no weapon
    /// gate. Applies <see cref="HibernatingEffect"/> to self. The
    /// effect handles the action-blocking + per-turn healing + buff
    /// state via its own OnApply/OnRemove/OnTurnStart hooks. Long
    /// cooldown (200T) makes this a once-per-fight emergency tool.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Cryomancy_Hibernate</c>):
    /// "the only self-stasis with healing trade-off."</para>
    /// </summary>
    public class Cryomancy_Hibernate : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_Hibernate);

        public const int COOLDOWN = 200;
        public const int HIBERNATE_DURATION = 10;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Hibernate",
                Command = "CommandHibernate",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;
            actor.ApplyEffect(new HibernatingEffect(HIBERNATE_DURATION), actor, ctx.Zone);
        }
    }
}
