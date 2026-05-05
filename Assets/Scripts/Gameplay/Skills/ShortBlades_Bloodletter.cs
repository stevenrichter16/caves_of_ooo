using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class on-hit Bleed power. <b>Identity-only stub</b> —
    /// behavior lives in <see cref="OnHitSkillEffects.Apply"/>.
    ///
    /// <para>Mechanic (WSP.3 — Qud-faithful port):
    /// <see cref="OnHitSkillEffects.SHORTBLADES_BLOODLETTER_CHANCE_PERCENT"/>%
    /// chance per Piercing hit to apply <see cref="BleedingEffect"/>
    /// with light dice
    /// (<see cref="OnHitSkillEffects.SHORTBLADES_BLOODLETTER_DAMAGE_DICE"/>
    /// — currently "1d2"). Stacks on top of the
    /// <see cref="ShortBladesSkill"/> crit-Bleed (WSP.1) and the
    /// <see cref="ShortBlades_Jab"/> Confused hook (WS.5). A
    /// fully-trained ShortBlades character on a critical Dagger hit
    /// can apply Confused + Bleed (Bloodletter) + Bleed (crit) +
    /// Confused (class hook) all on the same swing.</para>
    ///
    /// <para>Divergence from Qud's source: the original gates application
    /// on <c>defender.GetEffectCount(typeof(Bleeding)) &lt; 1 + Attacker.AgilityMod</c>
    /// to cap stacking. CoO drops this cap for v1 — BleedingEffect's own
    /// OnStack semantics determine how multiple Bleeds combine. Re-introduce
    /// if playtest shows runaway bleed-stacking on speedy Daggers.</para>
    /// </summary>
    public class ShortBlades_Bloodletter : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Bloodletter);
    }
}
