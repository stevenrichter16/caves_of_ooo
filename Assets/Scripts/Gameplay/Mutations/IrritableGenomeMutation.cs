using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Special mutation: non-BuyNew MP spending is remembered and later auto-spent randomly.
    /// </summary>
    public class IrritableGenomeMutation : BaseMutation
    {
        private bool _spending;

        public int MPSpentMemory;
        public Random Rng = new Random();

        public override string Name => "IrritableGenome";
        public override string MutationType => "Mental";
        public override string DisplayName => "Irritable Genome";

        public override bool CanLevel()
        {
            return false;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "UsedMP" && !_spending)
            {
                string context = e.GetStringParameter("Context", "default");
                if (!string.Equals(context, "BuyNew", StringComparison.OrdinalIgnoreCase))
                {
                    int amount = e.GetIntParameter("Amount", 0);
                    if (amount > 0)
                    {
                        MPSpentMemory += amount;
                        TrySpending();
                    }
                }
            }
            else if (e.ID == "GainedMP" && !_spending)
            {
                TrySpending();
            }

            return true;
        }

        public void TrySpending()
        {
            if (_spending || MPSpentMemory <= 0 || ParentEntity == null)
                return;

            MutationsPart mutations = ParentEntity.GetPart<MutationsPart>();
            if (mutations == null || ParentEntity.GetStat("MP") == null)
                return;

            int availableMP = ParentEntity.GetStatValue("MP", 0);
            if (availableMP <= 0)
                return;

            _spending = true;
            try
            {
                int budget = Math.Min(availableMP, MPSpentMemory);
                int spent = mutations.RandomlySpendMutationPoints(
                    maxMPToSpend: budget,
                    rng: Rng,
                    spendContext: "IrritableGenomeRandomSpend");

                if (spent > 0)
                {
                    MPSpentMemory -= spent;
                    if (MPSpentMemory < 0)
                        MPSpentMemory = 0;
                }
            }
            finally
            {
                _spending = false;
            }
        }
    }
}
