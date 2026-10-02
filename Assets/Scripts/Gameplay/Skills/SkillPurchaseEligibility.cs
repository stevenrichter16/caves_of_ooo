using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Side-effect-free eligibility for SP purchases and their screen rows.
    /// Parent gates apply only here: books, starter grants and restored skills remain exempt.
    /// Requirements precede affordability, so the screen and command identify the same blocker.</summary>
    public static class SkillPurchaseEligibility
    {
        /// <summary>Returns a fresh result without spending points, adding parts or emitting diagnostics.
        /// Succeeded means eligible, not purchased; CostPaid remains zero.</summary>
        public static BuySkillAction.Result Evaluate(Entity actor, string skillClassName)
        {
            var result = new BuySkillAction.Result();
            string attribute = "", minimum = "", requires = "", exclusion = "";
            PowerData child = null;
            if (SkillRegistry.TryGetSkillByClass(skillClassName, out var skill))
            {
                result.Cost = skill.Cost;
                attribute = skill.Attribute;
            }
            else if (SkillRegistry.TryGetPowerByClass(skillClassName, out child))
            {
                result.Cost = child.Cost;
                attribute = child.Attribute; minimum = child.Minimum;
                requires = child.Requires; exclusion = child.Exclusion;
            }
            else return Refuse(result, BuySkillAction.FailureReason.UnknownSkillClass);

            var skills = actor?.GetPart<SkillsPart>();
            if (skills == null) return Refuse(result, BuySkillAction.FailureReason.ActorMissingSkillsPart);
            var points = actor.GetStat("SP");
            result.SpBefore = result.SpAfter = points?.BaseValue ?? 0;
            // An already learned power stays owned even without its newly required parent.
            if (skills.HasSkill(skillClassName)) return Refuse(result, BuySkillAction.FailureReason.AlreadyOwned);
            if (result.Cost < 0) return Refuse(result, BuySkillAction.FailureReason.NotPurchasable);
            if (points == null) return Refuse(result, BuySkillAction.FailureReason.ActorMissingSPStat);
            if (!MeetsAttributeMinimum(actor, attribute, minimum, out var failedAttribute))
                return Refuse(result, BuySkillAction.FailureReason.StatMinNotMet, failedAttribute);
            if (!MeetsRequires(skills, requires, out var missing))
                return Refuse(result, BuySkillAction.FailureReason.MissingPrereq, missing);
            if (HasAnyExclusion(skills, exclusion, out var excluded))
                return Refuse(result, BuySkillAction.FailureReason.Exclusion, excluded);
            // Use the registry's class identity, never a possibly duplicated display name.
            if (child != null && (string.IsNullOrEmpty(child.ParentSkillClass)
                || !SkillRegistry.TryGetSkillByClass(child.ParentSkillClass, out _)
                || !skills.HasSkill(child.ParentSkillClass)))
                return Refuse(result, BuySkillAction.FailureReason.MissingPrereq, child.ParentSkillClass);
            if (result.SpBefore < result.Cost) return Refuse(result, BuySkillAction.FailureReason.InsufficientSP);
            result.Succeeded = true;
            return result;
        }

        private static BuySkillAction.Result Refuse(BuySkillAction.Result result,
            BuySkillAction.FailureReason reason, string detail = "")
        {
            result.Reason = reason; result.Detail = detail ?? ""; return result;
        }

        /// <summary>
        /// Parse Qud's pipe/comma stat-minimum format and check against
        /// the actor's stats. Returns true if the actor passes ANY OR-group
        /// (each group = comma-separated AND list of attribute,minimum pairs).
        /// Empty Attribute / Minimum = no requirement = true.
        /// </summary>
        private static bool MeetsAttributeMinimum(
            Entity actor, string attribute, string minimum, out string failedAttribute)
        {
            failedAttribute = "";
            if (string.IsNullOrWhiteSpace(attribute) || string.IsNullOrWhiteSpace(minimum))
                return true;

            string[] orGroupsAttr = attribute.Split('|');
            string[] orGroupsMin  = minimum.Split('|');

            // OR across groups: passing any one group passes overall.
            int n = orGroupsAttr.Length < orGroupsMin.Length ? orGroupsAttr.Length : orGroupsMin.Length;
            string lastFailedAttr = "";
            for (int g = 0; g < n; g++)
            {
                string[] attrs = orGroupsAttr[g].Split(',');
                string[] mins  = orGroupsMin[g].Split(',');
                int gn = attrs.Length < mins.Length ? attrs.Length : mins.Length;

                bool groupPasses = true;
                for (int i = 0; i < gn; i++)
                {
                    string attrName = attrs[i].Trim();
                    if (!int.TryParse(mins[i].Trim(), out int minValue)) continue;
                    int actorValue = actor.GetStatValue(attrName, 0);
                    if (actorValue < minValue)
                    {
                        groupPasses = false;
                        lastFailedAttr = attrName;
                        break;
                    }
                }
                if (groupPasses) return true;
            }
            failedAttribute = lastFailedAttr;
            return false;
        }

        /// <summary>
        /// All comma-separated classes in <paramref name="requires"/> must
        /// be owned by the actor. Empty = no requirement = true. Returns
        /// the FIRST missing class (for diag detail).
        /// </summary>
        private static bool MeetsRequires(SkillsPart skills, string requires, out string missing)
        {
            missing = "";
            if (string.IsNullOrWhiteSpace(requires)) return true;
            foreach (var raw in requires.Split(','))
            {
                string cls = raw.Trim();
                if (cls.Length == 0) continue;
                if (!skills.HasSkill(cls)) { missing = cls; return false; }
            }
            return true;
        }

        /// <summary>
        /// True if any class in <paramref name="exclusion"/> is owned by
        /// the actor. Empty = no exclusion = false. Returns the FIRST
        /// blocking class (for diag detail).
        /// </summary>
        private static bool HasAnyExclusion(SkillsPart skills, string exclusion, out string blocking)
        {
            blocking = "";
            if (string.IsNullOrWhiteSpace(exclusion)) return false;
            foreach (var raw in exclusion.Split(','))
            {
                string cls = raw.Trim();
                if (cls.Length == 0) continue;
                if (skills.HasSkill(cls)) { blocking = cls; return true; }
            }
            return false;
        }

    }
}
