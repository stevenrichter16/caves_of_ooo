namespace CavesOfOoo.Core
{
    /// <summary>One saved preparation benefit. A later prepared meal replaces it;
    /// raw food does not. Public payload includes the exact applied shift so saved
    /// stat Boost values can be reverted without reapplying on load.</summary>
    public sealed class PreparedMealEffect : Effect
    {
        public string StatName;
        public int Bonus;
        public int AppliedBonus;
        public override string DisplayName => "prepared meal";
        public override int GetEffectType() => TYPE_METABOLIC;

        public PreparedMealEffect(string statName, int bonus, int duration = 100)
        { StatName = statName; Bonus = bonus; Duration = duration; }

        public static bool Valid(string statName, int bonus, int duration)
        {
            return duration > 0 && bonus > 0 && bonus <= 20 &&
                (statName == "HeatResistance" || statName == "ColdResistance"
                 || statName == "AcidResistance" || statName == "Toughness" || statName == "DV");
        }
        public override bool CanApply(Entity target) => target != null && Valid(StatName, Bonus, Duration);

        public override void OnApply(Entity target)
        {
            var stat = target.GetStat(StatName);
            if (stat == null)
            {
                // Ordinary actors need not author elemental resistance stats.
                // Retain a zero-base stat on removal so unrelated later sources survive.
                stat = new Stat { Name = StatName, Owner = target, Min = -100, Max = 200 };
                target.Statistics[StatName] = stat;
            }
            AppliedBonus = Bonus;
            stat.Boost += AppliedBonus;
        }
        public override void OnRemove(Entity target)
        {
            var stat = target?.GetStat(StatName);
            if (stat != null) stat.Boost -= AppliedBonus;
            AppliedBonus = 0;
        }
        public override bool OnStack(Effect incoming)
        {
            if (!(incoming is PreparedMealEffect meal) || !Valid(meal.StatName, meal.Bonus, meal.Duration)) return false;
            OnRemove(Owner);
            StatName = meal.StatName; Bonus = meal.Bonus; Duration = meal.Duration;
            OnApply(Owner);
            JustApplied = TurnManager.Active?.CurrentActor == Owner;
            return true;
        }
        public string Describe() => "+" + Bonus + " " + Label(StatName) + "; " + Duration
            + " of your turns left. Another prepared meal replaces this benefit.";
        public static string Label(string statName)
        {
            switch (statName)
            {
                case "HeatResistance": return "heat resistance";
                case "ColdResistance": return "cold resistance";
                case "AcidResistance": return "acid resistance";
                case "Toughness": return "Toughness";
                case "DV": return "DV";
                default: return statName ?? "";
            }
        }
    }
}
