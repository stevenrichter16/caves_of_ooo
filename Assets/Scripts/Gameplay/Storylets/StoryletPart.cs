using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// World-entity Part that owns the storylet/quest runtime: which one-shot
    /// storylets have already fired, and which quests are active (with their
    /// stage index + turn-of-entry). Implements ISaveSerializable for
    /// round-trip and INarrativeReactor so that StoryletPart can register on
    /// NarrativeStatePart's reactor list and be polled once per TickEnd.
    ///
    /// M2 ships the data + save/load + accessors. M3 fills in OnTickEnd to
    /// drive trigger evaluation and effect dispatch.
    /// </summary>
    public sealed class StoryletPart : Part, ISaveSerializable, INarrativeReactor
    {
        public override string Name => "Storylets";

        /// <summary>
        /// The active StoryletPart for the current game session.
        /// Set by GameBootstrap on fresh boot AND on load. Null outside of play.
        /// </summary>
        public static StoryletPart Current;

        private readonly HashSet<string> _firedStorylets = new HashSet<string>();
        private readonly Dictionary<string, QuestState> _quests = new Dictionary<string, QuestState>();
        // QS.2 (Docs/QUEST-SYSTEM.md): tracks quests the player has
        // already completed so quest-not-started checks can rule out
        // already-finished quests, and so quest-givers can offer
        // post-completion dialogue. Persists via Save/Load.
        private readonly HashSet<string> _completedQuests = new HashSet<string>();

        // ── Fired-storylet API ────────────────────────────────────────────────

        public bool HasFired(string storyletId)
        {
            return !string.IsNullOrEmpty(storyletId) && _firedStorylets.Contains(storyletId);
        }

        public void MarkFired(string storyletId)
        {
            if (string.IsNullOrEmpty(storyletId)) return;
            _firedStorylets.Add(storyletId);
        }

        // ── Quest API ─────────────────────────────────────────────────────────

        public bool IsQuestActive(string questId)
        {
            return !string.IsNullOrEmpty(questId) && _quests.ContainsKey(questId);
        }

        public QuestState GetQuestState(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            return _quests.TryGetValue(questId, out var s) ? s : null;
        }

        public void StartQuest(QuestState state)
        {
            if (state == null || string.IsNullOrEmpty(state.QuestId)) return;
            _quests[state.QuestId] = state;
        }

        public IReadOnlyList<QuestState> GetActiveQuests()
        {
            return new List<QuestState>(_quests.Values);
        }

        // QS.2 (Docs/QUEST-SYSTEM.md): completed-quest API. The
        // `_completedQuests` set + `IsQuestCompleted` / `MarkQuestCompleted`
        // helpers back the IfQuestCompleted + IfQuestNotStarted
        // predicates plus the (forthcoming QS.3) CompleteQuest action.

        public bool IsQuestCompleted(string questId)
        {
            return !string.IsNullOrEmpty(questId) && _completedQuests.Contains(questId);
        }

        /// <summary>
        /// Move a quest from the active dict to the completed set.
        /// Idempotent: calling on an already-completed quest is a no-op,
        /// calling on a never-started quest just adds to completed
        /// (defensive — content shouldn't rely on this branch).
        /// </summary>
        public void MarkQuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return;
            _quests.Remove(questId);
            _completedQuests.Add(questId);
        }

        public IReadOnlyCollection<string> GetCompletedQuests()
        {
            return new List<string>(_completedQuests);
        }

        /// <summary>
        /// QS.3: drop a quest from the active dict WITHOUT moving it
        /// to the completed set. Used by the FailQuest action to
        /// allow the player to re-take the quest later
        /// (IfQuestNotStarted returns true again post-fail). v1:
        /// no separate _failedQuests tracking — see
        /// Docs/QUEST-SYSTEM.md self-review 🟡.
        /// </summary>
        public void RemoveActiveQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return;
            _quests.Remove(questId);
        }

        /// <summary>
        /// QS.3: advance an active quest by one stage. If the new
        /// index is past the terminal stage (Stages.Count - 1), the
        /// quest auto-completes and is moved to the completed set.
        /// Returns the post-advance state's CurrentStageIndex (or
        /// -1 if the quest wasn't active or was completed).
        ///
        /// Centralizes the stage-advance logic so both the
        /// AdvanceQuestStage conversation action AND the QS.4
        /// dispatch loop in OnTickEnd can call into one path.
        /// Keeps the side effects (diag record, auto-completion,
        /// EnteredStageAtTurn update) on a single source of truth.
        /// </summary>
        public int AdvanceQuestStage(string questId, int currentTurn)
        {
            if (string.IsNullOrEmpty(questId)) return -1;
            if (!_quests.TryGetValue(questId, out var state)) return -1;

            var quest = StoryletRegistry.FindQuest(questId);
            int totalStages = quest?.Stages?.Count ?? 0;

            int newIndex = state.CurrentStageIndex + 1;
            if (newIndex >= totalStages)
            {
                // Past the last stage — auto-complete.
                MarkQuestCompleted(questId);
                if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("quest"))
                {
                    CavesOfOoo.Diagnostics.Diag.Record(
                        category: "quest", kind: "Completed",
                        payload: new { questId, totalStages });
                }
                return -1;  // sentinel: quest moved to completed
            }

            int oldIndex = state.CurrentStageIndex;
            state.CurrentStageIndex = newIndex;
            state.EnteredStageAtTurn = currentTurn;

            if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("quest"))
            {
                CavesOfOoo.Diagnostics.Diag.Record(
                    category: "quest", kind: "StageAdvanced",
                    payload: new { questId, fromIndex = oldIndex, toIndex = newIndex });
            }
            return newIndex;
        }

        // ── INarrativeReactor — single-pass dispatch ──────────────────────────

        // Reused tick-scoped buffer to avoid per-tick allocations.
        private readonly List<StoryletData> _eligibleScratch = new List<StoryletData>();

        /// <summary>
        /// Polled once per TickEnd via NarrativeStatePart's reactor list.
        ///
        /// Single-pass dispatch: snapshot eligibility at the top of the tick,
        /// then fire effects. A storylet whose effect mutates the FactBag in
        /// a way that flips ANOTHER storylet's predicate does NOT cause that
        /// other storylet to fire this tick — the second one fires next tick.
        /// This keeps test order deterministic and avoids the "infinite cascade
        /// in one tick" footgun.
        ///
        /// Quest storylets (those with a non-null Quest sub-object) are skipped
        /// here — M4 lands their dispatch.
        /// </summary>
        public void OnTickEnd(NarrativeStatePart state)
        {
            _eligibleScratch.Clear();

            var all = StoryletRegistry.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                var s = all[i];
                if (s == null || string.IsNullOrEmpty(s.ID)) continue;
                if (s.IsQuest) continue;                              // M4 territory
                if (s.OneShot && _firedStorylets.Contains(s.ID)) continue;

                if (!ConversationPredicates.CheckAll(s.Triggers, null, null))
                    continue;

                _eligibleScratch.Add(s);
            }

            for (int i = 0; i < _eligibleScratch.Count; i++)
            {
                var s = _eligibleScratch[i];
                ConversationActions.ExecuteAll(s.Effects, null, null);
                if (s.OneShot)
                    _firedStorylets.Add(s.ID);
            }

            _eligibleScratch.Clear();
        }

        // ── ISaveSerializable ─────────────────────────────────────────────────

        public void Save(SaveWriter writer)
        {
            writer.Write(_firedStorylets.Count);
            foreach (var id in _firedStorylets)
                writer.WriteString(id);

            writer.Write(_quests.Count);
            foreach (var kvp in _quests)
            {
                writer.WriteString(kvp.Key);
                writer.WriteString(kvp.Value.QuestId);
                writer.Write(kvp.Value.CurrentStageIndex);
                writer.Write(kvp.Value.EnteredStageAtTurn);
            }

            // QS.2: completed-quest set. Append-after-quests so old
            // save files (without this section) can still load — Load
            // checks for end-of-section before reading.
            writer.Write(_completedQuests.Count);
            foreach (var id in _completedQuests)
                writer.WriteString(id);
        }

        public void Load(SaveReader reader)
        {
            _firedStorylets.Clear();
            int firedCount = reader.ReadInt();
            for (int i = 0; i < firedCount; i++)
                _firedStorylets.Add(reader.ReadString());

            _quests.Clear();
            int questCount = reader.ReadInt();
            for (int i = 0; i < questCount; i++)
            {
                string key = reader.ReadString();
                var state = new QuestState
                {
                    QuestId = reader.ReadString(),
                    CurrentStageIndex = reader.ReadInt(),
                    EnteredStageAtTurn = reader.ReadInt()
                };
                _quests[key] = state;
            }

            // QS.2: completed-quest set. SaveReader.ReadInt wraps
            // BinaryReader.ReadInt32 which THROWS EndOfStreamException
            // on EOF (verified at SaveSystem.cs:151). For forward-compat
            // with pre-QS.2 saves that didn't write this section, catch
            // the EOF and default to an empty set. Quests-completed-
            // pre-QS.2 didn't exist as a concept anyway, so empty is
            // correct.
            _completedQuests.Clear();
            try
            {
                int completedCount = reader.ReadInt();
                for (int i = 0; i < completedCount; i++)
                    _completedQuests.Add(reader.ReadString());
            }
            catch (System.IO.EndOfStreamException)
            {
                // Pre-QS.2 save file — completed-quests section wasn't
                // written. Leaving _completedQuests empty is correct.
            }
        }
    }
}
