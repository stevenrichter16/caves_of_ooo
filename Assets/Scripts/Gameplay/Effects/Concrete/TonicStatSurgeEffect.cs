namespace CavesOfOoo.Core
{
    /// <summary>A finite tonic bonus. Saved applied magnitude is undone once;
    /// earlier permanent Boost values are never inferred or migrated.</summary>
    public sealed class TonicStatSurgeEffect : Effect
    {
        public string StatName;
        public int Amount;
        public int AppliedAmount;
        public override string DisplayName => StatName + " surge";
        public TonicStatSurgeEffect(string statName = "", int amount = 0, int duration = 20)
        { StatName = statName; Amount = amount; Duration = duration; }
        public override void OnApply(Entity target)
        {
            var stat = target.GetStat(StatName);
            if (stat == null) return;
            stat.Boost += Amount; AppliedAmount = Amount;
            MessageLog.Add(target.GetDisplayName() + " feels a surge of " + StatName + "!");
        }
        public override void OnRemove(Entity target)
        {
            var stat = target.GetStat(StatName);
            if (stat != null) stat.Boost -= AppliedAmount;
            AppliedAmount = 0;
        }
        public override bool OnStack(Effect incoming)
        {
            if (!(incoming is TonicStatSurgeEffect surge) || surge.StatName != StatName) return false;
            Duration = System.Math.Max(Duration, surge.Duration);
            if (TurnManager.Active?.CurrentActor == Owner) JustApplied = true;
            return true;
        }
    }
}
