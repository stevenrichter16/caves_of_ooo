using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Spellcraft active ability: instantly resets every other
    /// activated ability's cooldown to 0 — full spell-suite reload.
    /// Long cooldown on Surge itself (<see cref="COOLDOWN"/> turns)
    /// keeps the move tactically scarce. Distinct from every other
    /// ability in the table — ArcaneSurge is the only ability that
    /// MANIPULATES OTHER ABILITIES' COOLDOWNS.
    ///
    /// <para><b>Mechanic:</b> SelfCentered, no targeting. Iterates
    /// the actor's <see cref="ActivatedAbilitiesPart.AbilityList"/>,
    /// sets <c>CooldownRemaining = 0</c> on every entry EXCEPT this
    /// skill's own (otherwise Surge would reset its own cooldown
    /// alongside the others, defeating the long-cooldown gating).</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Spellcraft_ArcaneSurge</c>):
    /// "the only ability that manipulates other abilities' cooldowns."</para>
    ///
    /// <para>Classification: <b>CoO-original Extension</b> per CLAUDE.md
    /// §4.2 — composes the existing ActivatedAbilitiesPart cooldown
    /// model.</para>
    /// </summary>
    public class Spellcraft_ArcaneSurge : BaseSkillPart
    {
        public override string Name => nameof(Spellcraft_ArcaneSurge);

        public const int COOLDOWN = 250;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Arcane Surge",
                Command = "CommandArcaneSurge",
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
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null || abilities.AbilityList == null)
            {
                EmitSkillRejectedDiag(ctx, "no_abilities");
                return;
            }

            // Reset every cooldown except this skill's own. The Surge's
            // own cooldown is applied by SkillsPart.TryRouteSkillCommand
            // AFTER OnCommand returns — so resetting it here would still
            // be re-set to MaxCooldown by the dispatcher. Even so, the
            // explicit skip-by-Guid keeps the intent clear + matches the
            // brainstorm's "long cooldown on Surge itself prevents abuse."
            int reset = 0;
            for (int i = 0; i < abilities.AbilityList.Count; i++)
            {
                var ability = abilities.AbilityList[i];
                if (ability == null) continue;
                if (ability.ID == ActivatedAbilityID) continue;
                if (ability.CooldownRemaining > 0)
                {
                    ability.CooldownRemaining = 0;
                    reset++;
                }
            }

            MessageLog.Add(actor.GetDisplayName() + " surges with arcane energy! "
                + reset + " ability cooldown" + (reset == 1 ? "" : "s") + " reset.");
        }
    }
}
