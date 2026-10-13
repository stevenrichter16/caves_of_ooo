using System;
namespace CavesOfOoo.Skills
{
    /// <summary>Read-only ranked content contract shared by purchases, saves and presentation.</summary>
    public static class SkillRanks
    {
        public static bool Valid(int max,int[] costs)
        {
            if(max<1 || max>32)return false;
            if(costs==null || costs.Length==0)return true;
            if(costs.Length!=max)return false;
            for(int i=0;i<costs.Length;i++)if(costs[i]<0)return false;
            return true;
        }
        public static int Maximum(string cls)
        {
            if(SkillRegistry.TryGetSkillByClass(cls,out var root)) return Math.Max(1,root.MaxRank);
            if(SkillRegistry.TryGetPowerByClass(cls,out var power)) return Math.Max(1,power.MaxRank);
            return 1;
        }
        public static int Cost(string cls,int nextRank)
        {
            int cost,max;int[] costs;
            if(SkillRegistry.TryGetSkillByClass(cls,out var root)) {cost=root.Cost;max=root.MaxRank;costs=root.RankCosts;}
            else if(SkillRegistry.TryGetPowerByClass(cls,out var power)) {cost=power.Cost;max=power.MaxRank;costs=power.RankCosts;}
            else return -1;
            if(!Valid(max,costs)||nextRank<1||nextRank>max||cost<0)return -1;
            if(costs==null||costs.Length==0)return cost;
            if(costs.Length!=max)return -1;
            for(int i=0;i<costs.Length;i++)if(costs[i]<0)return -1;
            return costs[nextRank-1];
        }
    }
}
