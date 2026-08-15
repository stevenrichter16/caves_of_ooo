using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Rites tree-root — initiation into the consuming rites, the
    /// grimoire magic found out in the world rather than derived from an
    /// element. The root is a marker passive (no ability, no hooks): the
    /// rites' power budget lives in their two costs — grimoire ink per
    /// cast, and the status marks they consume off the target — so the
    /// tree root deliberately adds no third knob.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original.</b>
    /// The resonance/ink economy has no Qud counterpart.</para>
    /// </summary>
    public class RitesSkill : BaseSkillPart
    {
        public override string Name => nameof(RitesSkill);
    }
}
