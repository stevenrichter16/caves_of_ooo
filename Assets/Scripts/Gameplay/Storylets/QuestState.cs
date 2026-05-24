using System;
using System.Collections.Generic;

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

        /// <summary>Q3 (Docs/QUEST-PARALLEL-OBJECTIVES.md) — IDs of
        /// objectives finished within the CURRENT stage. Cleared on stage
        /// advance (objectives are scoped to their stage). Empty for
        /// quests whose current stage has no objectives (the legacy
        /// linear path). Serialized by <c>StoryletPart.Save</c> as a
        /// trailing, EOF-defensive section — pre-Q3 saves load with this
        /// defaulting to an empty set (no save-format break).</summary>
        public HashSet<string> FinishedObjectives = new HashSet<string>();
    }
}
