using System;

namespace CavesOfOoo.Core
{
    [Serializable]
    public class HousePressurePointState
    {
        public string Id;
        public HouseDramaActivationState State;
        // Free-form; conventions: "resolved:complete", "resolved:partial",
        // "failed:ignored", "failed:escalated"
        public string Substate;
        // Set on resolution; enables follow-on drama generation
        public string PathTaken;

        public HousePressurePointState Clone()
        {
            return new HousePressurePointState
            {
                Id = Id,
                State = State,
                Substate = Substate,
                PathTaken = PathTaken
            };
        }
    }
}
