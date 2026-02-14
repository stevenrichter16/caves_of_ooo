using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Rules;

namespace CavesOfOoo.Core.Inventory.Planning
{
    /// <summary>
    /// Builds deterministic equip plans from composable rules.
    /// </summary>
    public sealed class EquipPlanner
    {
        private readonly List<IEquipRule> _rules;

        public EquipPlanner()
        {
            _rules = new List<IEquipRule>
            {
                new TargetPartCompatibilityRule(),
                new SlotCountRule(),
                new SlotAvailabilityRule(),
                new DisplacementRule(),
            };
        }

        public EquipPlan Build(Entity actor, Entity item, BodyPart targetBodyPart = null)
        {
            var body = actor?.GetPart<Body>();
            var equippable = item?.GetPart<EquippablePart>();
            var plan = new EquipPlan(actor, item, body, equippable, targetBodyPart);

            if (actor == null)
            {
                plan.Invalidate("No actor provided for equip plan.");
                return plan;
            }

            if (item == null)
            {
                plan.Invalidate("No item provided for equip plan.");
                return plan;
            }

            if (body == null)
            {
                plan.Invalidate("Actor has no body for body-part equip planning.");
                return plan;
            }

            if (equippable == null)
            {
                plan.Invalidate($"{item.GetDisplayName()} is not equippable.");
                return plan;
            }

            if (plan.SlotTypes.Count == 0)
            {
                plan.Invalidate($"{item.GetDisplayName()} has no valid equip slots.");
                return plan;
            }

            var builder = new EquipPlanBuilder(plan);
            for (int i = 0; i < _rules.Count; i++)
            {
                var result = _rules[i].Evaluate(builder);
                if (result == null || !result.Success)
                {
                    string reason = result?.FailureReason;
                    if (string.IsNullOrEmpty(reason))
                        reason = "Equip rule failed.";

                    plan.Invalidate(reason);
                    break;
                }
            }

            return plan;
        }
    }
}
