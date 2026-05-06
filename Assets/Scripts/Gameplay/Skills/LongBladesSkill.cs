using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Long Blades skill tree (longswords,
    /// greatswords, claymores, shortswords, sword-class weapons).
    ///
    /// <para><b>Crit behavior (WSP3.3 — virtual override):</b> on every
    /// critical hit landed with a LongBlades-attribute weapon, applies
    /// <see cref="BleedingEffect"/> with stronger dice
    /// (<see cref="CRIT_BLEED_DAMAGE_DICE"/>) than Lacerate's "1d3"
    /// or the universal Cutting class hook's "1d2". CoO port reframed
    /// from Qud's "+2 penetration on crit" since CoO's post-damage
    /// hook can't modify pre-damage rolls — flavor preserved (cuts deep
    /// + bleeds long), math diverges by design.</para>
    /// </summary>
    public class LongBladesSkill : BaseSkillPart
    {
        public override string Name => nameof(LongBladesSkill);

        public const int CRIT_BLEED_SAVE_TARGET = 15;
        public const string CRIT_BLEED_DAMAGE_DICE = "1d4";

        public override void OnWeaponMadeCriticalHit(SkillEventContext ctx)
        {
            if (ctx?.Damage == null) return;
            // Defense-in-depth: only fire on actual crits.
            if (!ctx.Damage.HasAttribute("Critical")) return;
            if (!ctx.Damage.HasAttribute("LongBlades")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            ctx.Defender.ApplyEffect(
                new BleedingEffect(CRIT_BLEED_SAVE_TARGET, CRIT_BLEED_DAMAGE_DICE, ctx.Rng),
                ctx.Attacker, ctx.Zone);
        }
    }
}
