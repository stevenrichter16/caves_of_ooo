using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// ShortBlades-class proficiency passive: +<see cref="PEN_BONUS"/>
    /// to penetration on every melee swing made with a Piercing-attribute
    /// weapon (Dagger / Spear / ChoirSpine / TemporalShard /
    /// GlassblownStiletto / etc.). Per Qud's
    /// <c>ShortBlades_Puncture</c> (XRL.World.Parts.Skill/ShortBlades_Puncture.cs:18-23) —
    /// Qud's mechanic is "AV - 2" on the hit-dice roll, which in CoO's
    /// penetration model is mathematically equivalent to "+2 pen bonus."
    /// CoO uses the +pen framing because it's clearer to reason about
    /// at the call site (CombatSystem.PerformSingleAttack:218).
    ///
    /// <para><b>Self-contained per WSP3.3 / WSP6.6.</b> Overrides
    /// <see cref="BaseSkillPart.OnGetPenetrationModifier"/> — the new
    /// virtual added in WSP6.6 to support pre-attack penetration
    /// modifiers (the first new combat hook added since the system
    /// shipped, following the §"Adding a new combat event" mechanical
    /// pattern).</para>
    ///
    /// <para>Classification: <b>Match</b> per CLAUDE.md §4.2 —
    /// mechanic, magnitude, and class gate are all verbatim from
    /// Qud. The only divergence is the framing (AV-reduction vs
    /// pen-bonus), which is mathematically equivalent.</para>
    /// </summary>
    public class ShortBlades_Puncture : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Puncture);

        public const int PEN_BONUS = 2;

        public override int OnGetPenetrationModifier(Entity actor, MeleeWeaponPart weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.Attributes)) return 0;
            // Gate on the Piercing attribute — that's CoO's unifying tag
            // for short-blade-class weapons (mirrors the rest of the
            // ShortBlades tree: ShortBlades_Expertise also gates on
            // "Piercing", per ShortBlades_Expertise.cs:22).
            if (!weapon.Attributes.Contains("Piercing")) return 0;
            return PEN_BONUS;
        }
    }
}
