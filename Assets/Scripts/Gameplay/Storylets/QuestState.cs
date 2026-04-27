using System;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Per-quest runtime state held in StoryletPart._quests.
    /// </summary>
    [Serializable]
    public class QuestState
    {
        public string QuestId;
        public int CurrentStageIndex;
        public int EnteredStageAtTurn;
    }
}
