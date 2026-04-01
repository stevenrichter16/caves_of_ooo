using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public static class SettlementSiteDefinitions
    {
        public const string MainWellSiteId = "MainWell";
        public const string StartingVillageKnowledgeProperty = "KnowsPurifyWater";
        public const string ImprovedWellCondition = "ImprovedWell";
        public const string StartingVillageZoneId = "Overworld.10.10.0";

        public static bool IsTrackedVillage(string settlementId, PointOfInterest poi)
        {
            return !string.IsNullOrEmpty(settlementId)
                && settlementId == StartingVillageZoneId
                && poi != null
                && poi.Type == POIType.Village;
        }

        public static IEnumerable<RepairableSiteState> CreateInitialSites(string settlementId, PointOfInterest poi)
        {
            if (!IsTrackedVillage(settlementId, poi))
                yield break;

            yield return new RepairableSiteState
            {
                SiteId = MainWellSiteId,
                SiteType = RepairableSiteType.Well,
                ProblemType = RepairProblemType.FouledWater,
                Stage = RepairStage.Fouled,
                Severity = 1,
                ResolvedByMethod = RepairMethodId.None,
                OutcomeTier = RepairOutcomeTier.None,
                ResolvedAtTurn = -1,
                RelapseAtTurn = null
            };
        }
    }
}
