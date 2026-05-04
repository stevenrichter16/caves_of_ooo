using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Skill purchase action. Validates Cost / Minimum / Requires /
    /// Exclusion gating against the actor's state, spends SP, and calls
    /// <see cref="SkillsPart.AddSkill(string, string)"/> on success.
    /// Mirrors Qud's purchase flow (SkillFactory + Skills.AddSkill +
    /// PowerEntry.MeetsRequirements) — same gating model, same Cost-then-
    /// effect ordering, same fail-fast on first invalid check.
    ///
    /// <para><b>Diag emit:</b> every call (success OR failure) emits a
    /// <c>skill/PurchaseAttempted</c> record on the substrate. Payload
    /// includes <c>succeeded</c>, <c>reason</c> (populated only on
    /// failure), <c>detail</c> (the specific blocker — e.g. the missing
    /// prereq class name), <c>costPaid</c>, <c>spBefore</c>, <c>spAfter</c>.
    /// QA / playtest debugging can query
    /// <c>diag_query category=skill kind=PurchaseAttempted</c> to see
    /// every player attempt with full reason context.</para>
    /// </summary>
    public static class BuySkillAction
    {
        // ────────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────────

        public enum FailureReason
        {
            None,
            UnknownSkillClass,
            ActorMissingSkillsPart,
            ActorMissingSPStat,
            AlreadyOwned,
            InsufficientSP,
            StatMinNotMet,
            MissingPrereq,
            Exclusion
        }

        /// <summary>
        /// Outcome of a purchase attempt. Always returned (never null);
        /// inspect <see cref="Succeeded"/> + <see cref="Reason"/> to
        /// branch on. Carries the SP delta + the specific gating
        /// detail for failed attempts (e.g. which prereq is missing).
        /// </summary>
        public class Result
        {
            public bool Succeeded;
            public FailureReason Reason = FailureReason.None;
            /// <summary>
            /// Specific gating detail (e.g. "Agility" for StatMinNotMet,
            /// "AcrobaticsSkill" for MissingPrereq). Empty when
            /// <see cref="Reason"/> is None or the failure is not stat/
            /// prereq/exclusion-specific.
            /// </summary>
            public string Detail = "";
            public int CostPaid;
            public int SpBefore;
            public int SpAfter;
        }

        /// <summary>
        /// Attempt to purchase the skill or power identified by
        /// <paramref name="skillClassName"/>. The class lookup checks
        /// both skills and powers (Requires/Exclusion lists name either).
        /// On success, deducts Cost from actor's SP stat and calls
        /// <c>SkillsPart.AddSkill(skillClassName, source:"purchase")</c>.
        /// </summary>
        public static Result Execute(Entity actor, string skillClassName)
        {
            var result = new Result();

            // 1. Resolve the entry (skill OR power) from the registry.
            //    Cost / Minimum / Requires / Exclusion all live on the entry.
            //    Skills have Cost + Initiatory; powers have Cost + Minimum +
            //    Requires + Exclusion. We look up both and read the relevant
            //    fields from whichever was found.
            int cost = 0;
            string attribute = "";
            string minimum = "";
            string requires = "";
            string exclusion = "";
            if (SkillRegistry.TryGetSkillByClass(skillClassName, out var skill))
            {
                cost = skill.Cost;
                attribute = skill.Attribute;
                minimum  = "";   // Skills don't carry Minimum in Qud
                requires = "";   // ditto
                exclusion = "";
            }
            else if (SkillRegistry.TryGetPowerByClass(skillClassName, out var power))
            {
                cost = power.Cost;
                attribute = power.Attribute;
                minimum  = power.Minimum;
                requires = power.Requires;
                exclusion = power.Exclusion;
            }
            else
            {
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.UnknownSkillClass, "");
            }

            // 2. Actor must have a SkillsPart (the manager).
            var skillsPart = actor?.GetPart<SkillsPart>();
            if (skillsPart == null)
            {
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.ActorMissingSkillsPart, "");
            }

            // 3. Actor must have an SP stat.
            var spStat = actor.GetStat("SP");
            if (spStat == null)
            {
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.ActorMissingSPStat, "");
            }
            result.SpBefore = spStat.BaseValue;

            // 4. Already-owned check. Mirrors Qud's
            //    Skills.AddSkill no-op-on-duplicate at Skills.cs:96-99.
            if (skillsPart.HasSkill(skillClassName))
            {
                result.SpAfter = result.SpBefore;
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.AlreadyOwned, "");
            }

            // 5. Cost check.
            if (result.SpBefore < cost)
            {
                result.SpAfter = result.SpBefore;
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.InsufficientSP, "");
            }

            // 6. Stat minimum check. Pipe/comma format per
            //    PowerEntry.cs:46-61, 124-139:
            //      '|' = OR groups; passing any group passes overall
            //      ',' = AND-conjuncts within group; all must pass
            //    Mirrors Qud's parser without porting the whole
            //    PowerEntryRequirement object — direct string parse.
            if (!MeetsAttributeMinimum(actor, attribute, minimum, out string failedAttr))
            {
                result.SpAfter = result.SpBefore;
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.StatMinNotMet, failedAttr);
            }

            // 7. Requires check. All comma-separated classes must be owned.
            if (!MeetsRequires(skillsPart, requires, out string missingReq))
            {
                result.SpAfter = result.SpBefore;
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.MissingPrereq, missingReq);
            }

            // 8. Exclusion check. Owning ANY blocks purchase.
            if (HasAnyExclusion(skillsPart, exclusion, out string blockingExcl))
            {
                result.SpAfter = result.SpBefore;
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.Exclusion, blockingExcl);
            }

            // ── All checks passed; commit. ──
            spStat.BaseValue -= cost;
            result.SpAfter = spStat.BaseValue;
            result.CostPaid = cost;

            bool added = skillsPart.AddSkill(skillClassName, source: "purchase");
            if (!added)
            {
                // Pathological: AddSkill rolled back due to lifecycle hook.
                // Refund SP since the skill isn't actually owned.
                spStat.BaseValue += cost;
                result.SpAfter = spStat.BaseValue;
                result.CostPaid = 0;
                // Surface this as a special failure so the player /
                // observer knows the buy was attempted but the skill
                // self-rejected. Most realistic via the Diag substrate;
                // re-use AlreadyOwned-ish failure for now (real cause:
                // skill setup failed, SP refunded).
                return EmitAndReturn(actor, result, skillClassName,
                    FailureReason.AlreadyOwned, "lifecycle-hook-rejected");
            }

            result.Succeeded = true;
            EmitDiag(actor, skillClassName, result);
            return result;
        }

        // ────────────────────────────────────────────────────────────────────
        // Gating helpers
        // ────────────────────────────────────────────────────────────────────

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

        // ────────────────────────────────────────────────────────────────────
        // Diag
        // ────────────────────────────────────────────────────────────────────

        private static Result EmitAndReturn(
            Entity actor, Result result, string skillClassName,
            FailureReason reason, string detail)
        {
            result.Succeeded = false;
            result.Reason = reason;
            result.Detail = detail ?? "";
            EmitDiag(actor, skillClassName, result);
            return result;
        }

        private static void EmitDiag(Entity actor, string skillClassName, Result result)
        {
            if (!Diag.IsChannelEnabled("skill")) return;
            Diag.Record(
                category: "skill",
                kind: "PurchaseAttempted",
                target: actor,
                payload: new
                {
                    skillClass = skillClassName,
                    succeeded = result.Succeeded,
                    reason = result.Reason.ToString(),
                    detail = result.Detail,
                    costPaid = result.CostPaid,
                    spBefore = result.SpBefore,
                    spAfter = result.SpAfter,
                });
        }
    }
}
