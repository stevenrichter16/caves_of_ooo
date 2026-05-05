using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Axe skill tree (battleaxes, hatchets,
    /// chopping weapons). Owning this part means the actor "knows" the
    /// Axe weapon family. Behavior in v1 lives on the powers; the
    /// tree-root is flavor-only.
    ///
    /// <para>Per the user's "1 SP / no other requirement" directive,
    /// this tree-root costs 1 SP and has no Minimum / Requires /
    /// Exclusion.</para>
    /// </summary>
    public class AxeSkill : BaseSkillPart
    {
        public override string Name => nameof(AxeSkill);
    }
}
