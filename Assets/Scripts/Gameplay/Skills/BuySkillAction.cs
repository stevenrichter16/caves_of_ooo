using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Skill purchase action. Validates parent tree / Cost / Minimum / Requires /
    /// Exclusion gating against the actor's state, spends SP, and calls
    /// <see cref="SkillsPart.AddSkill(string, string)"/> on success.
    /// Mirrors Qud's purchase flow (SkillFactory + Skills.AddSkill +
    /// PowerEntry.MeetsRequirements), with CoO parent-tree purchase gates. Cost-then-
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
            Exclusion,
            /// <summary>The row's Cost is negative — not-for-sale
            /// (grimoire-taught / quest-granted). M0 S6.</summary>
            NotPurchasable,
            StaleReview,
            TransactionInProgress,
            SetupFailed
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

            /// <summary>The row's cost as RESOLVED, set on every path
            /// that got far enough to know it — unlike
            /// <see cref="CostPaid"/>, which is only set on success.
            /// The InsufficientSP failure message reads this (it used
            /// to print CostPaid and told the player they needed
            /// 0sp).</summary>
            public int Cost;
            public int SpBefore;
            public int SpAfter;
            public int OldRank, NewRank, MaxRank;
        }

        /// <summary>
        /// Attempt to purchase the skill or power identified by
        /// <paramref name="skillClassName"/>. The class lookup checks
        /// both skills and powers (Requires/Exclusion lists name either).
        /// On success, deducts Cost from actor's SP stat and calls
        /// <c>SkillsPart.AddSkill(skillClassName, source:"purchase")</c>.
        /// </summary>
        public static Result Execute(Entity actor, string skillClassName, int? expectedRank = null, int? expectedCost = null)
        {
            var result = SkillPurchaseEligibility.Evaluate(actor, skillClassName);
            if (!result.Succeeded)
                return EmitAndReturn(actor, result, skillClassName, result.Reason, result.Detail);

            var skillsPart = actor.GetPart<SkillsPart>();
            var spStat = actor.GetStat("SP");
            int cost = result.Cost;

            if ((expectedRank.HasValue && expectedRank.Value != result.OldRank)
                || (expectedCost.HasValue && expectedCost.Value != cost))
                return EmitAndReturn(actor,result,skillClassName,FailureReason.StaleReview,"review-changed");
            if(skillsPart.PurchaseInProgress)
                return EmitAndReturn(actor,result,skillClassName,FailureReason.TransactionInProgress,"purchase-in-progress");
            skillsPart.PurchaseInProgress=true;
            try
            {
                spStat.BaseValue-=cost;
                bool changed=result.OldRank==0 ? skillsPart.AddSkill(skillClassName,source:"purchase")
                    : skillsPart.AdvanceRank(skillClassName,result.OldRank);
                if(!changed)
                {
                    spStat.BaseValue+=cost;result.SpAfter=spStat.BaseValue;
                    return EmitAndReturn(actor,result,skillClassName,FailureReason.AlreadyOwned,"lifecycle-hook-rejected");
                }
                result.NewRank=result.OldRank+1;result.CostPaid=cost;result.SpAfter=spStat.BaseValue;
                result.Succeeded=true;EmitDiag(actor,skillClassName,result);return result;
            }
            catch (System.Exception error)
            {
                string detail=error.GetType().Name;
                try { if(result.OldRank==0)skillsPart.RollbackAcquisition(skillClassName); }
                catch (System.Exception cleanupError)
                {
                    // Rollback detaches in its finally block. A failing teardown
                    // hook must still return a failed purchase and refund its cost.
                    detail+="; cleanup: "+cleanupError.GetType().Name;
                }
                finally { spStat.BaseValue+=cost;result.SpAfter=spStat.BaseValue;result.CostPaid=0;result.NewRank=result.OldRank; }
                return EmitAndReturn(actor,result,skillClassName,FailureReason.SetupFailed,detail);
            }
            finally { skillsPart.PurchaseInProgress=false; }
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
                    oldRank = result.OldRank, newRank = result.NewRank,
                    spBefore = result.SpBefore,
                    spAfter = result.SpAfter,
                });
        }
    }
}
