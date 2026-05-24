using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Storylets
{
    [Serializable]
    public class StoryletFileData
    {
        public List<StoryletData> Storylets = new List<StoryletData>();
    }

    [Serializable]
    public class StoryletData
    {
        public string ID;
        public bool OneShot;
        public bool Tracked;
        public List<ConversationParam> Triggers = new List<ConversationParam>();
        public List<ConversationParam> Effects = new List<ConversationParam>();
        public QuestData Quest;

        public bool IsQuest =>
            Quest != null && Quest.Stages != null && Quest.Stages.Count > 0;
    }

    [Serializable]
    public class QuestData
    {
        public List<QuestStageData> Stages = new List<QuestStageData>();
    }

    [Serializable]
    public class QuestStageData
    {
        public string ID;
        public List<ConversationParam> Triggers = new List<ConversationParam>();
        public List<ConversationParam> OnEnter = new List<ConversationParam>();
        /// <summary>Q3 — optional parallel objectives within this stage.
        /// EMPTY = the stage behaves exactly as before (advances when its
        /// own <see cref="Triggers"/> pass). NON-EMPTY = the stage advances
        /// when all non-<see cref="QuestObjectiveData.Optional"/> objectives
        /// are finished (in any order). Backward-compatible: existing
        /// content has no Objectives node, so JsonUtility leaves this an
        /// empty list.</summary>
        public List<QuestObjectiveData> Objectives = new List<QuestObjectiveData>();
    }

    /// <summary>
    /// Q3 (Docs/QUEST-PARALLEL-OBJECTIVES.md) — one parallel objective
    /// inside a <see cref="QuestStageData"/>. Objectives complete in any
    /// order; the stage advances once all non-<see cref="Optional"/> ones
    /// are finished. Mirrors a Qud <c>QuestStep</c> (per-objective triggers
    /// + effects + Optional/Hidden flags), scoped under a CoO stage so the
    /// linear-stage model is preserved.
    /// </summary>
    [Serializable]
    public class QuestObjectiveData
    {
        public string ID;
        /// <summary>Quest-log display text for this objective.</summary>
        public string Text;
        /// <summary>Predicates (conversation vocabulary) that, when ALL
        /// pass, finish this objective.</summary>
        public List<ConversationParam> Triggers = new List<ConversationParam>();
        /// <summary>Effects (conversation vocabulary) run when this
        /// objective finishes (e.g. AwardXP) — Qud per-step reward parity.</summary>
        public List<ConversationParam> OnEnter = new List<ConversationParam>();
        /// <summary>Does NOT block stage advancement when unfinished.</summary>
        public bool Optional;
        /// <summary>Hidden in the quest log until finished/revealed.</summary>
        public bool Hidden;
    }
}
