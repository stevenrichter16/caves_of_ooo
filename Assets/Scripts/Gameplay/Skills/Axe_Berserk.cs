using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class self-buff active ability: enters a blood frenzy.
    /// Per Qud's <c>Axe_Berserk</c> — requires an Axe-attribute weapon
    /// equipped, applies a duration-based buff that increases damage
    /// output and reduces defense.
    ///
    /// <para><b>Mechanic (CoO):</b> requires an Axe-class weapon
    /// equipped. Applies <see cref="BerserkEffect"/> to self for
    /// <see cref="DURATION"/> turns. While active: +5 Strength
    /// (feeds damage rolls), -2 DV (easier to be hit). Cooldown
    /// <see cref="COOLDOWN"/> turns — long enough that Berserk is a
    /// commitment, not a spam button.</para>
    /// </summary>
    public class Axe_Berserk : BaseSkillPart
    {
        public override string Name => nameof(Axe_Berserk);

        public const int COOLDOWN = 100;
        public const int DURATION = 5;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Berserk!",
                Command = "CommandAxeBerserk",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;

            // Require an Axe-class weapon equipped.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Axe");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs an axe equipped to go berserk.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }

            actor.ApplyEffect(new BerserkEffect(DURATION), actor, ctx.Zone);
        }
    }
}
