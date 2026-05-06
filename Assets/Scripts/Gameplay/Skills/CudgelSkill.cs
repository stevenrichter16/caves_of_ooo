using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Cudgel skill tree (mauls, maces,
    /// warhammers, club-class weapons). Owning this part means the
    /// actor "knows" the Cudgel weapon family.
    ///
    /// <para><b>Crit behavior (WSP3.3 — virtual override):</b> on every
    /// critical hit landed with a Cudgel-attribute weapon, applies
    /// <see cref="StunnedEffect"/> for a random
    /// <see cref="CRIT_STUN_DURATION_MIN"/>-<see cref="CRIT_STUN_DURATION_MAX"/>
    /// turn duration. Verbatim Qud port — see Qud's
    /// <c>XRL.World.Parts.Skill.Cudgel.WeaponMadeCriticalHit</c>
    /// (<c>Stat.Random(1, 4)</c>).</para>
    ///
    /// <para>Modifying the skill: edit this class. The override receives
    /// the <see cref="SkillEventContext"/> with all combat fields. Default
    /// virtual is no-op so removing the override turns the tree-root back
    /// into a flavor-only marker.</para>
    /// </summary>
    public class CudgelSkill : BaseSkillPart
    {
        public override string Name => nameof(CudgelSkill);

        /// <summary>Minimum turns of Stunned applied on a Critical Cudgel hit.</summary>
        public const int CRIT_STUN_DURATION_MIN = 1;

        /// <summary>Maximum (inclusive) turns of Stunned applied on a Critical Cudgel hit.</summary>
        public const int CRIT_STUN_DURATION_MAX = 4;

        public override void OnWeaponMadeCriticalHit(SkillEventContext ctx)
        {
            if (ctx?.Damage == null) return;
            // Defense-in-depth: tree-root crit hooks should only fire
            // on actual critical hits. CombatSystem.PerformSingleAttack
            // already gates the dispatcher on damage.HasAttribute("Critical");
            // checking again here makes the invariant explicit in the
            // skill code and survives any dispatcher-level regression.
            if (!ctx.Damage.HasAttribute("Critical")) return;
            if (!ctx.Damage.HasAttribute("Cudgel")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            // Random.Next is exclusive on upper bound; +1 makes MAX inclusive.
            int duration = ctx.Rng.Next(CRIT_STUN_DURATION_MIN, CRIT_STUN_DURATION_MAX + 1);
            ctx.Defender.ApplyEffect(new StunnedEffect(duration), ctx.Attacker, ctx.Zone);
        }
    }
}
