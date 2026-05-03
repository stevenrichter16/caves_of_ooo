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

        /// <summary>
        /// QS.7 fix: the local player entity, set by GameBootstrap so
        /// tick-driven dispatch can evaluate player-state predicates
        /// (IfHaveItem, IfReputationAtLeast, IfStatAtLeast etc).
        ///
        /// Pre-fix: <see cref="OnTickEnd"/> passed null/null to
        /// <see cref="ConversationPredicates.CheckAll"/>, which made
        /// any predicate that reads `listener` return false. Quests
        /// that used "the player picks up X" as a stage trigger would
        /// silently never advance via tick.
        ///
        /// Set null outside of play. Tick-driven predicates that
        /// dereference this defensively skip when null (matching
        /// the existing M3 null-listener convention).
        /// </summary>
        public static Entity LocalPlayer;

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
        /// QS.3 cold-eye fix #1: single source of truth for quest
        /// completion side effects. Centralizes the
        /// MarkQuestCompleted state mutation + the quest/Completed
        /// diag record emission. Callers pass `actor` so the diag
        /// substrate can record whether completion was player-driven
        /// (CompleteQuest action passes listener) or world-driven
        /// (AdvanceQuestStage's auto-complete passes null).
        ///
        /// Pre-fix: the action AND the AdvanceQuestStage helper each
        /// emitted their own quest/Completed Diag.Record with
        /// hand-rolled identical payloads. A future payload-shape
        /// change would have needed updates in both places. Fix #1+#3
        /// (Docs/QUEST-SYSTEM.md self-review): one place fires the
        /// diag, one parameter shape, no drift.
        ///
        /// No-op (returns false) on unknown / already-completed quests
        /// so callers don't double-fire.
        /// </summary>
        public bool CompleteQuest(string questId, Entity actor = null)
        {
            if (string.IsNullOrEmpty(questId)) return false;
            if (!_quests.ContainsKey(questId)) return false;

            var quest = StoryletRegistry.FindQuest(questId);
            int totalStages = quest?.Stages?.Count ?? 0;

            MarkQuestCompleted(questId);

            if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("quest"))
            {
                CavesOfOoo.Diagnostics.Diag.Record(
                    category: "quest", kind: "Completed",
                    actor: actor,
                    payload: new { questId, totalStages });
            }
            return true;
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
        ///
        /// QS.3 cold-eye fix #3: <paramref name="actor"/> is threaded
        /// through to the auto-complete branch's diag record so the
        /// substrate can record who triggered the advance (player
        /// action passes listener; tick-driven dispatch passes null).
        /// </summary>
        public int AdvanceQuestStage(string questId, int currentTurn, Entity actor = null)
        {
            if (string.IsNullOrEmpty(questId)) return -1;
            if (!_quests.TryGetValue(questId, out var state)) return -1;

            var quest = StoryletRegistry.FindQuest(questId);
            int totalStages = quest?.Stages?.Count ?? 0;

            int newIndex = state.CurrentStageIndex + 1;
            if (newIndex >= totalStages)
            {
                // Past the last stage — auto-complete via the
                // centralized helper. Diag record + state mutation
                // both live there (cold-eye fix #1).
                CompleteQuest(questId, actor);
                return -1;  // sentinel: quest moved to completed
            }

            int oldIndex = state.CurrentStageIndex;
            state.CurrentStageIndex = newIndex;
            state.EnteredStageAtTurn = currentTurn;

            if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("quest"))
            {
                CavesOfOoo.Diagnostics.Diag.Record(
                    category: "quest", kind: "StageAdvanced",
                    actor: actor,
                    payload: new { questId, fromIndex = oldIndex, toIndex = newIndex });
            }
            return newIndex;
        }

        // ── INarrativeReactor — single-pass dispatch ──────────────────────────

        // Reused tick-scoped buffers to avoid per-tick allocations.
        private readonly List<StoryletData> _eligibleScratch = new List<StoryletData>();
        // QS.4: snapshot of (questId, advance-to-stage-index) pairs eligible
        // to advance this tick. Snapshotting before mutating preserves
        // single-pass deterministic semantics (a quest whose stage-N
        // OnEnter flips stage-(N+1)'s trigger does NOT cascade to
        // stage-(N+1) the same tick).
        private readonly List<string> _eligibleQuestAdvances = new List<string>();

        /// <summary>
        /// Polled once per TickEnd via NarrativeStatePart's reactor list.
        ///
        /// Single-pass dispatch (M3 contract preserved through M4):
        /// snapshot eligibility at the top of the tick, then fire effects.
        /// A storylet/quest-stage whose effect mutates the FactBag in a way
        /// that flips ANOTHER storylet/quest-stage's predicate does NOT
        /// cause that other one to fire this tick — the second one fires
        /// next tick. Keeps test order deterministic and avoids the
        /// "infinite cascade in one tick" footgun.
        ///
        /// QS.4 (Docs/QUEST-SYSTEM.md): quest dispatch loop. For each
        /// active quest, evaluate the CURRENT stage's Triggers. If they
        /// all pass, advance via the centralized AdvanceQuestStage helper
        /// (which fires the quest/StageAdvanced or quest/Completed diag)
        /// and execute the new stage's OnEnter effects.
        ///
        /// Tick-driven advances pass null/null for speaker/listener — same
        /// constraint as M3's storylet effect dispatch. Content authors
        /// should know that GiveItem-style player-targeted actions don't
        /// have an actor context in tick-dispatch; those should live on
        /// player-initiated AdvanceQuestStage actions instead.
        /// </summary>
        public void OnTickEnd(NarrativeStatePart state)
        {
            _eligibleScratch.Clear();
            _eligibleQuestAdvances.Clear();

            // QS.7 fix: thread the local player through tick dispatch
            // so player-state predicates (IfHaveItem, IfReputationAtLeast)
            // can evaluate. LocalPlayer null = pre-bootstrap or test
            // context that didn't set it; predicates that dereference
            // listener defensively return false in that case.
            Entity player = LocalPlayer;

            // Pass 1A: storylet eligibility (M3, now with player listener).
            var all = StoryletRegistry.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                var s = all[i];
                if (s == null || string.IsNullOrEmpty(s.ID)) continue;
                if (s.IsQuest) continue;  // quests handled in pass 1B
                if (s.OneShot && _firedStorylets.Contains(s.ID)) continue;

                if (!ConversationPredicates.CheckAll(s.Triggers, null, player))
                    continue;

                _eligibleScratch.Add(s);
            }

            // Pass 1B (QS.4): quest stage-trigger eligibility. Snapshot
            // QuestId only — we re-resolve QuestState in pass 2 because
            // the dict can mutate during dispatch (auto-completions
            // remove entries; intentional but means iterating a stale
            // QuestState reference would be brittle).
            var activeQuests = GetActiveQuests();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                var qs = activeQuests[i];
                if (qs == null || string.IsNullOrEmpty(qs.QuestId)) continue;

                var qd = StoryletRegistry.FindQuest(qs.QuestId);
                if (qd == null) continue;
                if (qs.CurrentStageIndex < 0
                    || qs.CurrentStageIndex >= qd.Stages.Count) continue;

                var stage = qd.Stages[qs.CurrentStageIndex];
                if (!ConversationPredicates.CheckAll(stage.Triggers, null, player))
                    continue;

                _eligibleQuestAdvances.Add(qs.QuestId);
            }

            // Pass 2A: storylet effect dispatch.
            for (int i = 0; i < _eligibleScratch.Count; i++)
            {
                var s = _eligibleScratch[i];
                ConversationActions.ExecuteAll(s.Effects, null, player);
                if (s.OneShot)
                    _firedStorylets.Add(s.ID);
            }

            // Pass 2B (QS.4): quest stage-advance dispatch. Goes through
            // AdvanceQuestStage helper so auto-completion + diag records
            // emit on a single source of truth (same path the
            // AdvanceQuestStage conversation action uses).
            int currentTurn = TurnManager.Active?.TickCount ?? 0;
            for (int i = 0; i < _eligibleQuestAdvances.Count; i++)
            {
                string questId = _eligibleQuestAdvances[i];
                int newIndex = AdvanceQuestStage(questId, currentTurn, actor: player);
                if (newIndex < 0) continue;  // auto-completed

                // Fire OnEnter for the new stage. listener=player so
                // GiveItem/AwardXP/GiveDrams target the right recipient.
                var quest = StoryletRegistry.FindQuest(questId);
                if (quest != null && newIndex < quest.Stages.Count
                    && quest.Stages[newIndex].OnEnter != null)
                {
                    ConversationActions.ExecuteAll(
                        quest.Stages[newIndex].OnEnter, null, player);
                }
            }

            _eligibleScratch.Clear();
            _eligibleQuestAdvances.Clear();
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
