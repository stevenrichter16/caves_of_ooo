using System;

namespace CavesOfOoo.Core
{
    [Serializable]
    public class RepairableSiteState
    {
        public string SiteId;
        public RepairableSiteType SiteType;
        public RepairProblemType ProblemType;
        public RepairStage Stage;
        public int Severity;
        public RepairMethodId ResolvedByMethod;
        public int ResolvedAtTurn = -1;
        public int? RelapseAtTurn;
        public RepairOutcomeTier OutcomeTier;

        public RepairableSiteState Clone()
        {
            return new RepairableSiteState
            {
                SiteId = SiteId,
                SiteType = SiteType,
                ProblemType = ProblemType,
                Stage = Stage,
                Severity = Severity,
                ResolvedByMethod = ResolvedByMethod,
                ResolvedAtTurn = ResolvedAtTurn,
                RelapseAtTurn = RelapseAtTurn,
                OutcomeTier = OutcomeTier
            };
        }
    }
}
