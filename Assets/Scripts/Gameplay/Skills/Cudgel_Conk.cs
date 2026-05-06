using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class active ability: targeted strike on an adjacent
    /// creature that applies Stunned in addition to the swing's
    /// damage. Per Qud's <c>Cudgel_Conk</c> mechanic — Qud's version
    /// requires the defender to have a "Head" body part for the conk
    /// to land. CoO simplifies: any adjacent Creature is a valid target.
    ///
    /// <para><b>Mechanic (CoO):</b> requires a Cudgel-attribute weapon
    /// equipped. Targets the first adjacent Creature (in N→NE→E→...→NW
    /// direction-iteration order for determinism — same lookup as
    /// <see cref="SkillCombatHelpers.FindAdjacentCleaveTarget"/>). If
    /// found, performs a melee attack via
    /// <see cref="CombatSystem.PerformSingleAttack"/> AND applies
    /// <see cref="StunnedEffect"/> for <see cref="STUN_DURATION"/>
    /// turns regardless of swing outcome (the conk is the targeted-
    /// strike effect). Cooldown <see cref="COOLDOWN"/> turns.</para>
    /// </summary>
    public class Cudgel_Conk : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Conk);

        public const int COOLDOWN = 10;
        public const int STUN_DURATION = 4;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Conk",
                Command = "CommandConk",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            // Determinism: bail on null Rng instead of falling back to a
            // wall-clock-seeded one — matches the early-out pattern of
            // every other skill (Bludgeon/Hammer/ShatteringBlows/etc.).
            // Cold-eye finding 🟡 #2: the previous `?? new System.Random()`
            // injected nondeterminism asymmetric to siblings.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            // Require a Cudgel-class weapon equipped.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a cudgel-class weapon to conk.");
                return;
            }

            // Conk needs Zone for the adjacency lookup. (Berserk's
            // self-buff path doesn't need Zone — that asymmetry is by
            // design, cold-eye 🔵 #7.)
            if (ctx.Zone == null) return;
            var target = SkillCombatHelpers.FindAdjacentCleaveTarget(actor, actor, ctx.Zone);
            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " swings at nothing.");
                return;
            }

            // Swing + always apply Stunned (the targeted-strike effect).
            CombatSystem.PerformSingleAttack(
                attacker: actor, defender: target,
                weapon: weapon, isPrimary: true,
                zone: ctx.Zone, rng: ctx.Rng,
                attackSourceDesc: "(Conk)");
            target.ApplyEffect(new StunnedEffect(STUN_DURATION), actor, ctx.Zone);
        }
    }
}
