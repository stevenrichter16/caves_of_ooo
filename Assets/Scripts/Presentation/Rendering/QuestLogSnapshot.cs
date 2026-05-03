using System.Collections.Generic;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pure-data snapshot consumed by the (forthcoming) QuestLogUI
    /// renderer. Mirrors HotbarSnapshot / SidebarSnapshot pattern —
    /// state-builder is testable in EditMode without Tilemap setup.
    ///
    /// Per Docs/QUEST-SYSTEM.md QS.6. v1 ships this builder + the
    /// 4 plan-specified state tests. The MonoBehaviour rendering
    /// layer that consumes this snapshot (with hotkey 'q' binding +
    /// centered tilemap popup) is a follow-on commit — same way
    /// HotbarStateBuilder existed before HotbarRenderer fully wired.
    /// </summary>
    public readonly struct QuestLogSnapshot
    {
        public readonly IReadOnlyList<QuestLogActiveEntry> Active;
        public readonly IReadOnlyList<string> Completed;
        public readonly int ActiveCount;
        public readonly int CompletedCount;

        public QuestLogSnapshot(
            IReadOnlyList<QuestLogActiveEntry> active,
            IReadOnlyList<string> completed)
        {
            Active = active ?? System.Array.Empty<QuestLogActiveEntry>();
            Completed = completed ?? System.Array.Empty<string>();
            ActiveCount = Active.Count;
            CompletedCount = Completed.Count;
        }
    }

    /// <summary>
    /// One row in the Active section of the quest log: the quest's
    /// ID, the current stage's ID (if resolvable from the registry),
    /// the current stage's index (always valid even if registry has
    /// no name), and the turn the player entered the current stage.
    /// </summary>
    public readonly struct QuestLogActiveEntry
    {
        public readonly string QuestId;
        public readonly string CurrentStageId;   // may be empty if registry has no name
        public readonly int CurrentStageIndex;
        public readonly int EnteredStageAtTurn;

        public QuestLogActiveEntry(
            string questId,
            string currentStageId,
            int currentStageIndex,
            int enteredStageAtTurn)
        {
            QuestId = questId ?? string.Empty;
            CurrentStageId = currentStageId ?? string.Empty;
            CurrentStageIndex = currentStageIndex;
            EnteredStageAtTurn = enteredStageAtTurn;
        }
    }
}
