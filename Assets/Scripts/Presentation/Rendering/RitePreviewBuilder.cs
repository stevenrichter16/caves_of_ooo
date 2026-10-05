using System;
using System.Globalization;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional, read-only reading of the selected owned rite. Visible candidates
    /// are not a guarantee about hidden interception or the result of a later cast.</summary>
    public static class RitePreviewBuilder
    {
        public static ConsumingRiteSkillBase OwnedRite(Entity caster, Guid abilityId)
        {
            var abilities = caster?.GetPart<ActivatedAbilitiesPart>();
            var ability = abilities?.GetAbility(abilityId);
            var skills = caster?.GetPart<SkillsPart>();
            if (ability == null || skills?.ParentEntity != caster || abilities.ParentEntity != caster) return null;
            var rite = skills.GetSkill(ability.SourcePowerClass) as ConsumingRiteSkillBase;
            return rite?.ParentEntity == caster && rite.ActivatedAbilityID == abilityId
                && rite.CommandName == ability.Command ? rite : null;
        }

        /// <summary>Returns null for stale ownership, foreign zones, non-rites or an invalid
        /// direction. Does not spend ink, invoke damage/payoff hooks, record FX or dispatch events.</summary>
        public static string Build(Entity caster, Zone zone, Guid abilityId, int dx, int dy)
        {
            var rite = OwnedRite(caster, abilityId);
            if (rite == null || !CombatIntentReadout.IsVisibleActor(caster, zone)) return null;
            bool directed = rite.Shape == ConsumingRiteSkillBase.RiteShape.SingleTarget || rite.Shape == ConsumingRiteSkillBase.RiteShape.Cone;
            if (directed && (dx < -1 || dx > 1 || dy < -1 || dy > 1 || (dx == 0 && dy == 0))) return null;
            ResonanceSystem.EnsureInitialized();
            var targets = rite.GetVisiblePreviewTargets(zone, caster, dx, dy);
            var text = new StringBuilder(rite.DisplayName).Append(" - reading only\n\n");
            text.Append("Visible candidates only. Unseen interception or later changes may alter a cast. Reading spends no ink or turn.\n");
            if (targets.Count == 0) text.Append("\nNo visible target in this shape.\n");
            foreach (var target in targets)
            {
                var result = ResonanceSystem.Preview(target, rite.Element, rite.Slots);
                text.Append("\n").Append(target.GetDisplayName()).Append("\nSpend: ")
                    .Append(result.Consumed.Count == 0 ? "none" : string.Join(", ", result.Consumed));
                text.Append("\nKeep: ").Append(result.Declined.Count == 0 ? "none" : string.Join(", ", result.Declined));
                text.Append("\nMarks: ").Append(result.Consumed.Count).Append('/').Append(rite.Slots);
                if (rite is Rites_HangingBolt)
                    text.Append("; pin: ").Append(result.Consumed.Count * Rites_HangingBolt.PARALYSIS_PER_MARK).Append(" turns of paralysis on a susceptible survivor.");
                else text.Append("; resonance x").Append(result.Multiplier.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append(" (before this rite's special rules, spell bonuses and resistance).");
                text.Append('\n');
            }
            text.Append("\n").Append(AbilityDetailsBuilder.BuildForAbility(caster, abilityId));
            return text.ToString();
        }
    }
}
