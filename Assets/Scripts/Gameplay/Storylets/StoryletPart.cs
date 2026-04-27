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

        // ── INarrativeReactor (M3 fills in dispatch) ──────────────────────────

        public void OnTickEnd(NarrativeStatePart state)
        {
            // M3 — trigger evaluation + dispatch.
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
        }
    }
}
