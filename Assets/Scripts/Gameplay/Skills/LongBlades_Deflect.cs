using CavesOfOoo.Core;
namespace CavesOfOoo.Skills
{
    public sealed class LongBlades_Deflect : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Deflect);
        public override string DescribeRank(int rank) => BladeDefense.DeflectChance(rank)
            + "% full parry against perceived adjacent melee strikes that would hit. Requires a held long blade and ability to act. En Garde adds 30 percentage points.";
    }
}
