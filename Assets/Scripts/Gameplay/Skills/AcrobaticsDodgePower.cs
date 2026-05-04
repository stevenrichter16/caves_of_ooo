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
    /// <para><b>Combat consumption note (out of v1 ST.5 scope):</b>
    /// CoO's combat hit-roll currently reads <c>ArmorPart.DV</c>, not
    /// <c>Entity.Statistics["DV"]</c>. ST.5 ships the substrate (the
    /// stat-shift round-trips correctly on the Entity Stat) but does NOT
    /// modify the combat code that reads DV. A follow-on milestone will
    /// bridge the Entity DV stat into combat's hit-roll calculation;
    /// until then, the +2 from Dodge is visible on
    /// <c>entity.GetStatValue("DV")</c> but doesn't yet affect combat
    /// outcomes. Documented as a 🟡 finding in the ST.5 commit body.</para>
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
