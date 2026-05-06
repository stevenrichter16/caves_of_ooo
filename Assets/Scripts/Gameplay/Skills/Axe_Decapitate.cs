using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class "Decapitate" marker skill — a tag that modifies how
    /// <see cref="Axe_Dismember"/>'s candidate pool is built. When an
    /// actor owns <c>Axe_Decapitate</c>, an Axe_Dismember proc may
    /// target Mortal severable parts (Head, Heart, etc.) in addition
    /// to the default non-Mortal pool. Without this skill, Dismember
    /// strictly skips Mortal parts.
    ///
    /// <para>Per Qud's <c>Axe_Decapitate.cs</c> — Qud's version is a
    /// toggle (CommandToggleDecapitate) that the player can switch
    /// off to spare an enemy's head. CoO simplifies to <b>always-on
    /// while owned</b>: if you've learned this skill, your axe
    /// dismembers can take heads. The toggle infrastructure
    /// (Toggleable / DefaultToggleState in <c>ActivatedAbility</c>)
    /// is not yet plumbed in CoO; future ports can flip this skill
    /// to a true toggle once the infra lands. Documented per
    /// CLAUDE.md §4.2 as Match (mechanic family) + Divergent (no
    /// toggle).</para>
    ///
    /// <para><b>Design:</b> this is a "marker skill" — no virtual
    /// overrides on <see cref="BaseSkillPart"/>; the only behavior is
    /// <see cref="ShouldDecapitate"/>, queried by Axe_Dismember when
    /// it builds its candidate pool. Mirrors Qud's
    /// <c>Axe_Dismember.BodyPartIsDismemberable</c> pattern
    /// (XRL.World.Parts.Skill/Axe_Dismember.cs:123-141), where the
    /// "include Mortal parts" decision is delegated to
    /// <c>Axe_Decapitate.ShouldDecapitate</c>.</para>
    /// </summary>
    public class Axe_Decapitate : BaseSkillPart
    {
        public override string Name => nameof(Axe_Decapitate);

        /// <summary>
        /// Static helper: does this actor have Decapitate owned (and
        /// therefore "want to decapitate")? Returns false on null actor
        /// or actors without a SkillsPart. Mirrors Qud's static method
        /// <c>Axe_Decapitate.ShouldDecapitate(GameObject)</c>.
        ///
        /// <para>In CoO v1 this is just "is the skill owned?" — once
        /// toggle infrastructure lands, this should also gate on the
        /// toggle being ON. Documented inline so future readers know
        /// where to wire the toggle check.</para>
        /// </summary>
        public static bool ShouldDecapitate(Entity actor)
        {
            if (actor == null) return false;
            var skills = actor.GetPart<SkillsPart>();
            if (skills == null) return false;
            return skills.HasSkill(nameof(Axe_Decapitate));
        }
    }
}
