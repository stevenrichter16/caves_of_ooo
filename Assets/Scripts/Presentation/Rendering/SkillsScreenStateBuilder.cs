using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Rendering
{
    /// <summary>Pure screen data using the same side-effect-free purchase rules as the command.
    /// Merely opening the screen never spends SP, teaches skills or emits purchase attempts.</summary>
    public static class SkillsScreenStateBuilder
    {
        public static SkillsScreenSnapshot Build(Entity actor)
        {
            if (actor == null) return new SkillsScreenSnapshot(null, 0);
            int sp = actor.GetStat("SP")?.BaseValue ?? 0;
            if (actor.GetPart<SkillsPart>() == null) return new SkillsScreenSnapshot(null, sp);
            var rows = new List<SkillsScreenRow>();
            foreach (var skill in SkillRegistry.GetAllSkills())
            {
                if (skill == null) continue;
                rows.Add(BuildRow(actor, skill.Class, skill.Name, skill.Description, true, "", skill.Cost, skill.Flags));
                if (skill.Powers == null) continue;
                foreach (var power in skill.Powers)
                    if (power != null)
                        rows.Add(BuildRow(actor, power.Class, power.Name, power.Description, false, skill.Name, power.Cost, power.Flags));
            }
            return new SkillsScreenSnapshot(rows, sp);
        }

        private static SkillsScreenRow BuildRow(Entity actor, string className, string name, string description,
            bool root, string parentName, int cost, int flags)
        {
            var result = SkillPurchaseEligibility.Evaluate(actor, className);
            var state = result.Succeeded ? SkillsScreenRowState.Buyable
                : result.Reason == BuySkillAction.FailureReason.AlreadyOwned ? SkillsScreenRowState.Owned
                : result.Reason == BuySkillAction.FailureReason.InsufficientSP ? SkillsScreenRowState.InsufficientSP
                : SkillsScreenRowState.RequirementsNotMet;
            bool obfuscated = state == SkillsScreenRowState.RequirementsNotMet && (flags & SkillData.FLAG_OBFUSCATED) != 0;
            return new SkillsScreenRow(className, obfuscated ? "???" : name, description ?? "", root,
                parentName, cost, state, obfuscated);
        }
    }
}
