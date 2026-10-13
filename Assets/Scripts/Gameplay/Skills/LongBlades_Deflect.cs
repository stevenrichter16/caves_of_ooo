using CavesOfOoo.Core;
namespace CavesOfOoo.Skills
{
    public sealed class LongBlades_Deflect : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Deflect);
        public override string DescribeRank(int rank) => BladeDefense.DeflectChance(rank)
            + "% full parry on an eligible strike.";
    }
}
