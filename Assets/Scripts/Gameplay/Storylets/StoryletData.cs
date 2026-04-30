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
    }
}
