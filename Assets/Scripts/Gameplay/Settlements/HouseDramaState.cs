using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    [Serializable]
    public class HouseDramaState
    {
        public string DramaId;
        public HouseDramaActivationState State;
        public HouseDramaEndState EndState;
        public int ActivatedTurn = -1;
        public int ResolvedAtTurn = -1;
        // Runtime sum of path corruptionContribution values
        public int CorruptionScore;

        private readonly Dictionary<string, HousePressurePointState> _pressurePoints
            = new Dictionary<string, HousePressurePointState>();

        public IReadOnlyDictionary<string, HousePressurePointState> PressurePoints => _pressurePoints;

        public HousePressurePointState GetPressurePoint(string id)
        {
            HousePressurePointState pp;
            return id != null && _pressurePoints.TryGetValue(id, out pp) ? pp : null;
        }

        public void SetPressurePoint(HousePressurePointState pp)
        {
            if (pp == null || string.IsNullOrEmpty(pp.Id))
                return;
            _pressurePoints[pp.Id] = pp;
        }
    }
}
