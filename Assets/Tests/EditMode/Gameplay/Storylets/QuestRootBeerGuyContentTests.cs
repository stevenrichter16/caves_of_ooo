using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;        // ConversationActions, ConversationPredicates
using CavesOfOoo.Data;        // ConversationData/FileData, NodeData, ChoiceData, ConversationParam
using CavesOfOoo.Storylets;   // StoryletFileData, StoryletData

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CONTENT-INTEGRITY tests for the WORLD-PLACED "Root Beer Guy's Case"
    /// fetch quest (the first quest the player meets in normal play — placed
    /// by VillagePopulationBuilder in the starting village, NOT a dev scenario).
    /// Pins the cross-references that VillagePopulationBuilder relies on
    /// (quest ID, objective ID, item blueprint name, conversation ID) plus the
    /// usual IfFact-well-formed / registered-actions checks.
    ///
    /// IMPORTANT: the string constants below MUST stay in sync with
    /// VillagePopulationBuilder.PlaceStartingVillageQuestGiver — that's the
    /// builder↔content seam this test guards.
    /// </summary>
    public class QuestRootBeerGuyContentTests
    {
        private const string QuestId = "RootBeerGuyCase";
        private const string ObjFind = "find_notebook";
        private const string ObjSlay = "drive_off_gremlin";
        private const string ItemBlueprint = "DetectiveNotebook";
        private const string SlainFact = "rbg_gremlin_routed";
        private const string ConversationId = "RootBeerGuy_Quest";

        private static StoryletData LoadQuest()
        {
            var ta = Resources.Load<TextAsset>("Content/Data/Storylets/RootBeerGuyCase");
            Assert.IsNotNull(ta, "RootBeerGuyCase.json must live in Resources/Content/Data/Storylets");
            var q = JsonUtility.FromJson<StoryletFileData>(ta.text).Storylets.FirstOrDefault(s => s.ID == QuestId);
            Assert.IsNotNull(q, $"quest '{QuestId}' must be in the file");
            return q;
        }

        private static ConversationData LoadConvo()
        {
            var ta = Resources.Load<TextAsset>("Content/Conversations/RootBeerGuy_Quest");
            Assert.IsNotNull(ta, "RootBeerGuy_Quest.json must live in Resources/Content/Conversations");
            var c = JsonUtility.FromJson<ConversationFileData>(ta.text).Conversations.FirstOrDefault(x => x.ID == ConversationId);
            Assert.IsNotNull(c, $"conversation '{ConversationId}' must be in the file");
            return c;
        }

        [Test]
        public void Quest_Loads_WithRobustFetchObjective()
        {
            var q = LoadQuest();
            Assert.IsNotNull(q.Quest);
            Assert.AreEqual(2, q.Quest.Stages.Count, "find + report");
            Assert.AreEqual("find", q.Quest.Stages[0].ID);
            Assert.AreEqual("report", q.Quest.Stages[1].ID);

            var find = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == ObjFind);
            Assert.IsNotNull(find, "the fetch objective the builder's CompleteObjectiveOnTaken references must exist");
            // Robustness: the objective MUST have an IfHaveItem trigger so it
            // completes order-independently (not Part-only). Pin the item match.
            var have = find.Triggers.FirstOrDefault(t => t.Key == "IfHaveItem");
            Assert.IsNotNull(have, "find_notebook must have an IfHaveItem trigger (order-independent / no soft-lock in the wild)");
            Assert.AreEqual(ItemBlueprint, have.Value, "IfHaveItem must match the notebook blueprint the builder places");

            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment), "has a Q7 accomplishment deed");
        }

        [Test]
        public void Quest_HasRobustKillObjective_PollingTheSlainFact()
        {
            // The kill objective must poll the SetFactWhenSlain fact via IfFact
            // (order-independent: the gremlin can die before OR after accept).
            var q = LoadQuest();
            var slay = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == ObjSlay);
            Assert.IsNotNull(slay, "the kill objective the builder's SetFactWhenSlain gremlin drives must exist");
            var iffact = slay.Triggers.FirstOrDefault(t => t.Key == "IfFact");
            Assert.IsNotNull(iffact, "drive_off_gremlin must poll an IfFact (NOT a Part-only/sentinel objective) for order-independence");
            Assert.AreEqual($"{SlainFact}:>=:1", iffact.Value,
                "IfFact must match the fact SetFactWhenSlain sets on the placed gremlin (builder<->content seam)");
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
                        if (pr.Key is "IfQuestNotStarted" or "IfQuestCompleted" or "IfQuestActive" or "IfQuestFailed")
                            Assert.AreEqual(QuestId, pr.Value, $"predicate {pr.Key} must reference the quest");
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
