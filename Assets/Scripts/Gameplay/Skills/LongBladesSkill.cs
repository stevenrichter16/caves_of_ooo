using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Long Blades skill tree (longswords,
    /// greatswords, claymores, shortswords, sword-class weapons).
    /// Behavior in v1 lives on the powers; the tree-root is flavor-only.
    ///
    /// <para>Per the user's "1 SP / no other requirement" directive,
    /// this tree-root costs 1 SP and has no Minimum / Requires /
    /// Exclusion.</para>
    /// </summary>
    public class LongBladesSkill : BaseSkillPart
    {
        public override string Name => nameof(LongBladesSkill);
    }
}
