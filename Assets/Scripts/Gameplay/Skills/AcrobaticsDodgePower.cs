using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// First concrete passive skill: <b>+2 DV</b> while owned. Mirrors
    /// Qud's <c>Acrobatics_Dodge</c>
    /// (XRL.World.Parts.Skill/Acrobatics_Dodge.cs:6-21) line-for-line —
    /// <c>AddSkill</c> applies <c>StatShifter.SetStatShift("DV", 2)</c>;
    /// <c>RemoveSkill</c> calls <c>RemoveStatShifts</c>.
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

        public override bool AddSkill(Entity entity)
        {
            StatShifter.SetStatShift("DV", DV_BONUS);
            return true;
        }

        public override bool RemoveSkill(Entity entity)
        {
            StatShifter.RemoveStatShifts();
            return true;
        }
    }
}
