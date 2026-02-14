using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public interface IEquipRule
    {
        EquipRuleResult Evaluate(EquipPlanBuilder builder);
    }
}
