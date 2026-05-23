using System.Collections.Generic;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Builds the pure-data <see cref="QuestLogSnapshot"/> consumed
    /// by the quest log UI renderer. Pure function — same testability
    /// shape as <c>HotbarStateBuilder</c> + <c>SidebarStateBuilder</c>.
    ///
    /// Per Docs/QUEST-SYSTEM.md QS.6. The renderer side (MonoBehaviour
    /// + Tilemap + 'q' hotkey wiring) is a follow-on commit.
    /// </summary>
    public static class QuestLogStateBuilder
    {
        /// <summary>
        /// Build a snapshot from <see cref="StoryletPart.Current"/>
        /// (or the supplied part for testability). Returns an empty
        /// snapshot when no quests are active or completed — callers
        /// can render an empty-state message without null checks.
        ///
        /// For each active quest, resolves the current stage's ID
        /// from <see cref="StoryletRegistry.FindQuest"/> when
        /// available; falls back to empty string when the quest
        /// blueprint isn't in the registry (defensive — a save
        /// could reference content that's been removed).
        /// </summary>
        public static QuestLogSnapshot Build(StoryletPart part)
        {
            if (part == null)
                return new QuestLogSnapshot(null, null);

            var activeStates = part.GetActiveQuests();
            var active = new List<QuestLogActiveEntry>(activeStates.Count);
            for (int i = 0; i < activeStates.Count; i++)
            {
                var s = activeStates[i];
                if (s == null || string.IsNullOrEmpty(s.QuestId)) continue;

                string stageId = string.Empty;
                IReadOnlyList<QuestLogStageRow> stages = System.Array.Empty<QuestLogStageRow>();
                var quest = StoryletRegistry.FindQuest(s.QuestId);
                if (quest != null && quest.Stages != null)
                {
                    // Full ordered stage rows with per-row status (Qud-parity:
                    // mirrors XRL.UI.QuestLog per-stage decoration). Stages
                    // before the current index are Done, the current index is
                    // Current, later stages Pending. Forward-compatible with
                    // the flat-step model (Q3): there, no row is Current.
                    var rows = new List<QuestLogStageRow>(quest.Stages.Count);
                    for (int j = 0; j < quest.Stages.Count; j++)
                    {
                        var status = j < s.CurrentStageIndex ? QuestLogStageStatus.Done
                            : j == s.CurrentStageIndex ? QuestLogStageStatus.Current
                            : QuestLogStageStatus.Pending;
                        rows.Add(new QuestLogStageRow(quest.Stages[j].ID ?? string.Empty, status));
                    }
                    stages = rows;

                    if (s.CurrentStageIndex >= 0 && s.CurrentStageIndex < quest.Stages.Count)
                        stageId = quest.Stages[s.CurrentStageIndex].ID ?? string.Empty;
                }

                active.Add(new QuestLogActiveEntry(
                    s.QuestId, stageId, s.CurrentStageIndex, s.EnteredStageAtTurn, stages));
            }

            var completedSet = part.GetCompletedQuests();
            var completed = new List<string>(completedSet.Count);
            foreach (var id in completedSet)
                completed.Add(id);

            return new QuestLogSnapshot(active, completed);
        }
    }
}
