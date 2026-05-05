using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Short Blades skill tree (daggers,
    /// spears, choir spines, pointed thrust-class weapons).
    /// Behavior in v1 lives on the powers; the tree-root is flavor-only.
    ///
    /// <para>The skill tree's gameplay TRIGGER is the existing
    /// "Piercing" damage attribute on CoO weapons (the powers gate on
    /// <c>damage.HasAttribute("Piercing")</c>). The tree retains its
    /// genre-conventional name for player legibility.</para>
    ///
    /// <para>Per the user's "1 SP / no other requirement" directive,
    /// this tree-root costs 1 SP and has no Minimum / Requires /
    /// Exclusion.</para>
    /// </summary>
    public class ShortBladesSkill : BaseSkillPart
    {
        public override string Name => nameof(ShortBladesSkill);
    }
}
