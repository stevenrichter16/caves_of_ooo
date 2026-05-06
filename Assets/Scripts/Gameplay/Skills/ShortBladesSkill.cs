using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Short Blades skill tree (daggers,
    /// spears, choir spines, pointed thrust-class weapons). Triggers
    /// on the existing "Piercing" damage attribute (CoO's weapons don't
    /// carry a "ShortBlades" sub-class today — the tree retains the
    /// genre-conventional name for player legibility).
    ///
    /// <para><b>Crit behavior (WSP3.3 — virtual override):</b> on every
    /// critical hit landed with a Piercing-attribute weapon, applies
    /// <see cref="BleedingEffect"/> with light dice
    /// (<see cref="CRIT_BLEED_DAMAGE_DICE"/>). Per Qud's
    /// <c>ShortBlades.WeaponMadeCriticalHit</c> — Qud uses "1d2-1"
    /// which DiceRoller can't represent (negative modifier), so CoO
    /// uses "1d2" as the closest equivalent.</para>
    /// </summary>
    public class ShortBladesSkill : BaseSkillPart
    {
        public override string Name => nameof(ShortBladesSkill);

        public const int CRIT_BLEED_SAVE_TARGET = 15;
        public const string CRIT_BLEED_DAMAGE_DICE = "1d2";

        public override void OnWeaponMadeCriticalHit(SkillEventContext ctx)
        {
            if (ctx?.Damage == null) return;
            // Defense-in-depth: only fire on actual crits.
            if (!ctx.Damage.HasAttribute("Critical")) return;
            if (!ctx.Damage.HasAttribute("Piercing")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            ctx.Defender.ApplyEffect(
                new BleedingEffect(CRIT_BLEED_SAVE_TARGET, CRIT_BLEED_DAMAGE_DICE, ctx.Rng),
                ctx.Attacker, ctx.Zone);
        }
    }
}
