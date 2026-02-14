using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Special mutation: on level gain, may randomly manifest another mutation and consume one unstable charge.
    /// </summary>
    public class UnstableGenomeMutation : BaseMutation, IRankedMutation
    {
        public int ProcChancePercent = 33;
        public Random Rng = new Random();

        public override string Name => "UnstableGenome";
        public override string MutationType => "Mental";
        public override string DisplayName => "Unstable Genome";

        public override bool CanLevel()
        {
            return false;
        }

        public int GetRank()
        {
            return BaseLevel;
        }

        public int AdjustRank(int amount)
        {
            BaseLevel += amount;
            if (BaseLevel < 1)
                BaseLevel = 1;
            return BaseLevel;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "StatChanged")
                return true;

            string statName = e.GetStringParameter("Stat");
            if (!string.Equals(statName, "Level", StringComparison.OrdinalIgnoreCase))
                return true;

            int oldValue = e.GetIntParameter("OldValue", 0);
            int newValue = e.GetIntParameter("NewValue", 0);
            if (newValue <= oldValue)
                return true;

            HandleLevelGain(oldValue, newValue);
            return true;
        }

        private void HandleLevelGain(int oldLevel, int newLevel)
        {
            if (ParentEntity == null)
                return;

            MutationsPart mutations = ParentEntity.GetPart<MutationsPart>();
            if (mutations == null)
                return;

            Random rng = Rng ?? (Rng = new Random());
            int chance = ProcChancePercent;
            if (chance < 0) chance = 0;
            if (chance > 100) chance = 100;

            int gains = newLevel - oldLevel;
            for (int i = 0; i < gains; i++)
            {
                if (BaseLevel <= 0)
                    break;

                if (rng.Next(1, 101) > chance)
                    continue;

                var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: rng);
                if (options.Count == 0)
                    continue;

                int pick = rng.Next(options.Count);
                if (!mutations.BuyRandomMutationOption(options[pick], cost: 0, spendContext: "UnstableGenome"))
                    continue;

                if (BaseLevel <= 1)
                {
                    mutations.RemoveMutation(this);
                    break;
                }

                BaseLevel -= 1;
                ChangeLevel(Level);
            }
        }
    }
}
