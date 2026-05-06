using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// LongBlades-class proficiency passive: +2 to-hit when wielding a
    /// LongBlades-attribute weapon (LongSword / Greatsword / ShortSword
    /// / Claymore / Sporeblade / FlamingSword / IceSword / etc.).
    ///
    /// <para>WSP4.4 cold-eye finding 🔵 #5 fix: the LongBlades tree was
    /// missing the +to-hit Expertise that Cudgel / Axe / ShortBlades all
    /// have, breaking the natural cross-tree symmetry the player perceives.
    /// Mirrors Qud's LongBlades Expertise pattern (same +2 magnitude as
    /// Cudgel/Axe; Qud's actual numeric is documented per-skill in
    /// <c>qud-decompiled-project/XRL.World.Parts.Skill/</c>).</para>
    /// </summary>
    public class LongBlades_Expertise : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Expertise);

        public const int HIT_BONUS = 2;

        public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.Attributes)) return 0;
            if (!weapon.Attributes.Contains("LongBlades")) return 0;
            return HIT_BONUS;
        }
    }
}
