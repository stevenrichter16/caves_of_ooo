using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Tree-root marker for the Acrobatics skill tree. Owning this part
    /// means the actor has "the Acrobatics tree" — it unlocks
    /// purchasing of individual Acrobatics powers (Dodge, Tumble,
    /// SwiftReflexes, Jump). The marker itself confers no stat bonus
    /// or active ability — Acrobatics's value is in its powers.
    ///
    /// <para>Mirrors Qud's <c>Acrobatics</c>
    /// (XRL.World.Parts.Skill/Acrobatics.cs:8) — same empty-marker shape.
    /// Future per-tree behavior (e.g. tree-wide aura, tree-completion
    /// bonus) would be added here.</para>
    /// </summary>
    public class AcrobaticsSkill : BaseSkillPart
    {
        public override string Name => nameof(AcrobaticsSkill);
    }
}
