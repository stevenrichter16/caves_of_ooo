using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class on-hit Stun power. <b>Identity-only stub</b> — the
    /// actual on-hit logic lives in
    /// <see cref="OnHitSkillEffects.Apply"/>, which checks
    /// <c>SkillsPart.HasSkill(nameof(Cudgel_Bludgeon))</c> and rolls a
    /// <see cref="OnHitSkillEffects.CUDGEL_BLUDGEON_CHANCE_PERCENT"/>
    /// chance to apply <see cref="StunnedEffect"/> for
    /// <see cref="OnHitSkillEffects.CUDGEL_BLUDGEON_DURATION"/> turns.
    ///
    /// <para>This pattern (skill class as identity marker, behavior
    /// elsewhere) differs from <see cref="AcrobaticsDodgePower"/>'s
    /// "OnAdd stat-shift, OnRemove undo" pattern because on-hit logic
    /// fires DURING combat, not at AddSkill time. The class still
    /// inherits BaseSkillPart so SkillsPart.HasSkill / Save serialization
    /// work the same way.</para>
    /// </summary>
    public class Cudgel_Bludgeon : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Bludgeon);
    }
}
