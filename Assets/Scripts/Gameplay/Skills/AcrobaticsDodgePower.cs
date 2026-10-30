using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// First concrete passive skill: <b>+2 DV</b> while owned. Mirrors
    /// Qud's <c>Acrobatics_Dodge</c>
    /// (XRL.World.Parts.Skill/Acrobatics_Dodge.cs:6-21) line-for-line —
    /// <c>AddSkill</c> applies a saved shared stat modifier; removal reverses
    /// that receipt, including after save/load. Legacy saves keep their base
    /// shift until removal.
    ///
    /// <para>CombatSystem.GetDV consumes the DV stat as an additive
    /// adjustment to base, agility and armor defense. The +2 therefore
    /// affects hit rolls as well as the inventory's computed DV display.</para>
    /// </summary>
    public class AcrobaticsDodgePower : BaseSkillPart
    {
        public override string Name => nameof(AcrobaticsDodgePower);

        /// <summary>+2 DV bonus, mirroring Qud's Dodge constant
        /// (Acrobatics_Dodge.cs:13). Per-skill constant so future
        /// balance changes are localized to this file.</summary>
        public const int DV_BONUS = 2;

        // Null identifies an old save whose +2 was stored in DV.BaseValue.
        // New grants persist the exact shared-modifier receipt in DV.Bonus.
        public string AppliedDodgeBonus;

        public override bool AddSkill(Entity entity)
        {
            AppliedDodgeBonus = StatModifiers.Format(StatModifiers.Replace(entity,
                StatModifiers.Parse(AppliedDodgeBonus), new[] { new StatModifier("DV", DV_BONUS) }));
            return true;
        }

        public override bool RemoveSkill(Entity entity)
        {
            if (AppliedDodgeBonus != null)
                StatModifiers.Remove(entity, StatModifiers.Parse(AppliedDodgeBonus));
            else if (ParentEntity == entity && entity.Parts.Contains(this))
            {
                var dv = entity.GetStat("DV");
                if (dv != null) dv.BaseValue -= DV_BONUS;
            }
            AppliedDodgeBonus = "";
            return true;
        }
    }
}
