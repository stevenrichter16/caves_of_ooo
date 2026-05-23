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

    /// <summary>Status of one objective row in the quest log, mirroring
    /// Qud's per-stage status decoration (XRL.UI.QuestLog).</summary>
    public enum QuestLogStageStatus
    {
        /// <summary>An earlier stage the player has already passed.</summary>
        Done,
        /// <summary>The player's current stage.</summary>
        Current,
        /// <summary>A later stage not yet reached.</summary>
        Pending,
    }

    /// <summary>One objective/stage row within an active quest, with its
    /// completion status. The renderer decorates by <see cref="Status"/>
    /// (✓ Done / ► Current / · Pending). Forward-compatible with the
    /// flat-step model (Q3): there, rows are just Done/Pending.</summary>
    public readonly struct QuestLogStageRow
    {
        public readonly string StageId;
        public readonly QuestLogStageStatus Status;

        public QuestLogStageRow(string stageId, QuestLogStageStatus status)
        {
            StageId = stageId ?? string.Empty;
            Status = status;
        }
    }

    /// <summary>
    /// One row in the Active section of the quest log: the quest's
    /// ID, the current stage's ID (if resolvable from the registry),
    /// the current stage's index (always valid even if registry has
    /// no name), the turn the player entered the current stage, and
    /// the full ordered list of stage rows with per-row status (so the
    /// renderer can show the whole objective list, Qud-style, not just
    /// the current step).
    /// </summary>
    public readonly struct QuestLogActiveEntry
    {
        public readonly string QuestId;
        public readonly string CurrentStageId;   // may be empty if registry has no name
        public readonly int CurrentStageIndex;
        public readonly int EnteredStageAtTurn;
        /// <summary>Ordered stage rows with status. Empty when the quest
        /// blueprint isn't resolvable from the registry (defensive).</summary>
        public readonly IReadOnlyList<QuestLogStageRow> Stages;

        public QuestLogActiveEntry(
            string questId,
            string currentStageId,
            int currentStageIndex,
            int enteredStageAtTurn,
            IReadOnlyList<QuestLogStageRow> stages = null)
        {
            QuestId = questId ?? string.Empty;
            CurrentStageId = currentStageId ?? string.Empty;
            CurrentStageIndex = currentStageIndex;
            EnteredStageAtTurn = enteredStageAtTurn;
            Stages = stages ?? System.Array.Empty<QuestLogStageRow>();
        }
    }
}
