using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Cudgel skill tree (mauls, maces,
    /// warhammers, club-class weapons). Owning this part means the
    /// actor "knows" the Cudgel weapon family — it unlocks (and is
    /// the conceptual home of) the Cudgel_Bludgeon power and any
    /// future Cudgel powers (Backswing, Slam, ChargingStrike).
    ///
    /// <para>Per WS.0 plan: tree-root is flavor-only in v1 — no
    /// stat-shift, no event hook. Behaviors live on the powers.
    /// Per the user's "1 SP / no other requirement" directive, this
    /// tree-root costs 1 SP and has no Minimum, Requires, or
    /// Exclusion.</para>
    /// </summary>
    public class CudgelSkill : BaseSkillPart
    {
        public override string Name => nameof(CudgelSkill);
    }
}
