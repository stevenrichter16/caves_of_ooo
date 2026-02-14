using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    /// <summary>
    /// Populates displacement list for already-claimed parts.
    /// </summary>
    public sealed class DisplacementRule : IEquipRule
    {
        public EquipRuleResult Evaluate(EquipPlanBuilder builder)
        {
            if (builder?.Plan == null)
                return EquipRuleResult.Fail("No equip plan.");

            builder.AddDisplacementsFromClaimedParts();
            return EquipRuleResult.Pass();
        }
    }
}
