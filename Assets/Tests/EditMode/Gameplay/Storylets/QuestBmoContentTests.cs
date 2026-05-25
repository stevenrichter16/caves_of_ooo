using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CONTENT-INTEGRITY for the WORLD-PLACED "BMO's Lost Cartridge" reach-a-
    /// location quest. Pins the cross-references VillagePopulationBuilder.
    /// PlaceBmoQuest relies on (quest/objective/conversation IDs + the
    /// reach-location fact the QuestMarkerTriggerPart sets), and the IfFact /
    /// registered-action checks. Consts MUST stay in sync with the builder.
    /// </summary>
    public class QuestBmoContentTests
    {
        private const string QuestId = "BmoCartridge";
        private const string ObjReach = "reach_stump";
        private const string MarkerFact = "bmo_stump_reached";
        private const string ConversationId = "BMO_Quest";

        private static StoryletData LoadQuest()
        {
            var ta = Resources.Load<TextAsset>("Content/Data/Storylets/BmoCartridge");
            Assert.IsNotNull(ta, "BmoCartridge.json must live in Resources/Content/Data/Storylets");
            var q = JsonUtility.FromJson<StoryletFileData>(ta.text).Storylets.FirstOrDefault(s => s.ID == QuestId);
            Assert.IsNotNull(q, $"quest '{QuestId}' must be in the file");
            return q;
        }

        private static ConversationData LoadConvo()
        {
            var ta = Resources.Load<TextAsset>("Content/Conversations/BMO_Quest");
            Assert.IsNotNull(ta, "BMO_Quest.json must live in Resources/Content/Conversations");
            var c = JsonUtility.FromJson<ConversationFileData>(ta.text).Conversations.FirstOrDefault(x => x.ID == ConversationId);
            Assert.IsNotNull(c, $"conversation '{ConversationId}' must be in the file");
            return c;
        }

        [Test]
        public void Quest_HasReachObjective_PollingTheMarkerFact()
        {
            var q = LoadQuest();
            Assert.IsNotNull(q.Quest);
            Assert.AreEqual(2, q.Quest.Stages.Count, "search + report");
            Assert.AreEqual("search", q.Quest.Stages[0].ID);
            Assert.AreEqual("report", q.Quest.Stages[1].ID);

            var reach = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == ObjReach);
            Assert.IsNotNull(reach, "the reach objective the builder's QuestMarkerTrigger drives must exist");
            var iffact = reach.Triggers.FirstOrDefault(t => t.Key == "IfFact");
            Assert.IsNotNull(iffact, "reach_stump must poll an IfFact (order-independent reach detection)");
            Assert.AreEqual($"{MarkerFact}:>=:1", iffact.Value,
                "IfFact must match the fact QuestMarkerTriggerPart sets on the placed stump (builder<->content seam)");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment), "has a Q7 accomplishment deed");
        }

        [Test]
        public void Quest_AllIfFactArgs_AreWellFormed()
        {
            var q = LoadQuest();
            foreach (var stage in q.Quest.Stages)
            {
                AssertIfFact(stage.Triggers, $"stage '{stage.ID}'");
                foreach (var o in stage.Objectives) AssertIfFact(o.Triggers, $"objective '{o.ID}'");
            }
        }

        private static void AssertIfFact(List<ConversationParam> ps, string where)
        {
            if (ps == null) return;
            foreach (var p in ps.Where(x => x.Key == "IfFact"))
                Assert.AreEqual(3, (p.Value ?? "").Split(':').Length,
                    $"IfFact '{p.Value}' in {where} must be key:OP:threshold (3 parts)");
        }

        [Test]
        public void Conversation_ActionsPredicatesRegistered_AndQuestReferencesResolve()
        {
            var c = LoadConvo();
            var q = LoadQuest();
            var stageIds = new HashSet<string>(q.Quest.Stages.Select(s => s.ID));
            foreach (var node in c.Nodes)
            {
                if (node.Choices == null) continue;
                foreach (var choice in node.Choices)
                {
                    foreach (var a in choice.Actions ?? new List<ConversationParam>())
                    {
                        Assert.IsTrue(ConversationActions.IsRegistered(a.Key),
                            $"action '{a.Key}' (node '{node.ID}') must be registered");
                        if (a.Key is "StartQuest" or "CompleteQuest" or "AdvanceQuestStage" or "FailQuest")
                            Assert.AreEqual(QuestId, a.Value, $"action {a.Key} must target the quest");
                    }
                    foreach (var pr in choice.Predicates ?? new List<ConversationParam>())
                    {
                        Assert.IsTrue(ConversationPredicates.IsRegistered(pr.Key),
                            $"predicate '{pr.Key}' (node '{node.ID}') must be registered");
                        if (pr.Key == "IfQuestStage")
                        {
                            var parts = (pr.Value ?? "").Split(':');
                            Assert.AreEqual(QuestId, parts[0], "IfQuestStage questId");
                            if (!int.TryParse(parts[1], out _))
                                Assert.IsTrue(stageIds.Contains(parts[1]),
                                    $"IfQuestStage stageRef '{parts[1]}' must be a real stage");
                        }
                    }
                }
            }
        }
    }
}
