namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class EquipRuleResult
    {
        public bool Success { get; }

        public string FailureReason { get; }

        private EquipRuleResult(bool success, string failureReason)
        {
            Success = success;
            FailureReason = failureReason ?? string.Empty;
        }

        public static EquipRuleResult Pass()
        {
            return new EquipRuleResult(true, string.Empty);
        }

        public static EquipRuleResult Fail(string failureReason)
        {
            return new EquipRuleResult(false, failureReason);
        }
    }
}
