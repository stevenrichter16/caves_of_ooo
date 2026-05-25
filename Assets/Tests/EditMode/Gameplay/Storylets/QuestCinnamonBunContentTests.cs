using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;        // ConversationActions, ConversationPredicates
using CavesOfOoo.Data;        // ConversationData/FileData, NodeData, ChoiceData, ConversationParam
using CavesOfOoo.Storylets;   // StoryletFileData, StoryletData, QuestStageData, QuestObjectiveData
using CavesOfOoo.Scenarios.Custom; // QuestCinnamonBunPlayable (const IDs)

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CONTENT-INTEGRITY tests for the playable "Cinnamon Bun's Favor" quest.
    /// Multi-file content (quest JSON + conversation JSON + scenario C#) is
    /// fragile at the SEAMS — an ID typo in one file silently breaks the loop
    /// with no compile error. These tests pin every cross-reference:
    ///   - the quest's stage/objective IDs match the scenario's const IDs
    ///     (which its CompleteObjectiveOnTaken / FinishObjectiveWhenSlain Parts use)
    ///   - every IfFact arg is well-formed (key:OP:threshold — the bug class
    ///     fixed in EnchiridionQuest)
    ///   - every conversation action/predicate name is registered
    ///   - every quest-referencing dialogue arg names CinnamonBunFavor, and
    ///     every IfQuestStage stageRef is a real stage.
    /// Loads the JSON directly (Resources.Load + JsonUtility) — no dependency
    /// on registry load order / other tests' Reset() calls.
    /// </summary>
    public class QuestCinnamonBunContentTests
    {
        private static StoryletData LoadQuest()
        {
            var ta = Resources.Load<TextAsset>("Content/Data/Storylets/CinnamonBunFavor");
            Assert.IsNotNull(ta, "CinnamonBunFavor.json must live in Resources/Content/Data/Storylets");
            var fd = JsonUtility.FromJson<StoryletFileData>(ta.text);
            var q = fd.Storylets.FirstOrDefault(s => s.ID == QuestCinnamonBunPlayable.QuestId);
            Assert.IsNotNull(q, $"quest '{QuestCinnamonBunPlayable.QuestId}' must be in the file");
            return q;
        }

        private static ConversationData LoadConvo()
        {
            var ta = Resources.Load<TextAsset>("Content/Conversations/CinnamonBun_Quest");
            Assert.IsNotNull(ta, "CinnamonBun_Quest.json must live in Resources/Content/Conversations");
            var fd = JsonUtility.FromJson<ConversationFileData>(ta.text);
            var c = fd.Conversations.FirstOrDefault(x => x.ID == QuestCinnamonBunPlayable.ConversationId);
            Assert.IsNotNull(c, $"conversation '{QuestCinnamonBunPlayable.ConversationId}' must be in the file");
            return c;
        }

        [Test]
        public void Quest_Loads_WithStagesAndObjectivesMatchingScenarioConsts()
        {
            var q = LoadQuest();
            Assert.IsNotNull(q.Quest, "has a Quest body");
            Assert.AreEqual(2, q.Quest.Stages.Count, "errands + report");
            Assert.AreEqual("errands", q.Quest.Stages[0].ID);
            Assert.AreEqual("report", q.Quest.Stages[1].ID);

            var objIds = q.Quest.Stages[0].Objectives.Select(o => o.ID).ToList();
            // The scenario's Parts reference these objective IDs — they MUST exist.
            Assert.Contains(QuestCinnamonBunPlayable.ObjFetch, objIds,
                "CompleteObjectiveOnTaken's objective must exist in the quest");
            Assert.Contains(QuestCinnamonBunPlayable.ObjSlay, objIds,
                "FinishObjectiveWhenSlain's objective must exist in the quest");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment),
                "has a Q7 accomplishment deed (logged on completion)");
        }

        [Test]
        public void Quest_AllIfFactArgs_AreWellFormed()
        {
            // IfFact needs key:OP:threshold (3 colon-parts). A 2-part arg
            // silently always-false (the EnchiridionQuest bug class).
            var q = LoadQuest();
            foreach (var stage in q.Quest.Stages)
            {
                AssertIfFact(stage.Triggers, $"stage '{stage.ID}'");
                foreach (var o in stage.Objectives)
                    AssertIfFact(o.Triggers, $"objective '{o.ID}'");
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
        public void Conversation_AllActionsAndPredicates_AreRegistered()
        {
            var c = LoadConvo();
            foreach (var node in c.Nodes)
            {
                if (node.Choices == null) continue;
                foreach (var choice in node.Choices)
                {
                    foreach (var a in choice.Actions ?? new List<ConversationParam>())
                        Assert.IsTrue(ConversationActions.IsRegistered(a.Key),
                            $"action '{a.Key}' (node '{node.ID}') must be a registered ConversationAction");
                    foreach (var pr in choice.Predicates ?? new List<ConversationParam>())
                        Assert.IsTrue(ConversationPredicates.IsRegistered(pr.Key),
                            $"predicate '{pr.Key}' (node '{node.ID}') must be a registered ConversationPredicate");
                }
            }
        }

        [Test]
        public void Conversation_QuestReferences_MatchTheQuestAndStages()
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
                        if (a.Key is "StartQuest" or "CompleteQuest" or "AdvanceQuestStage" or "FailQuest")
                            Assert.AreEqual(QuestCinnamonBunPlayable.QuestId, a.Value,
                                $"action {a.Key} (node '{node.ID}') must target the quest");
                    }
                    foreach (var pr in choice.Predicates ?? new List<ConversationParam>())
                    {
                        if (pr.Key is "IfQuestNotStarted" or "IfQuestCompleted" or "IfQuestActive" or "IfQuestFailed")
                            Assert.AreEqual(QuestCinnamonBunPlayable.QuestId, pr.Value,
                                $"predicate {pr.Key} (node '{node.ID}') must reference the quest");
                        if (pr.Key == "IfQuestStage")
                        {
                            var parts = (pr.Value ?? "").Split(':');
                            Assert.AreEqual(2, parts.Length, $"IfQuestStage arg '{pr.Value}' is questId:stageRef");
                            Assert.AreEqual(QuestCinnamonBunPlayable.QuestId, parts[0], "IfQuestStage questId");
                            if (!int.TryParse(parts[1], out _))
                                Assert.IsTrue(stageIds.Contains(parts[1]),
                                    $"IfQuestStage stageRef '{parts[1]}' (node '{node.ID}') must be a real stage ID");
                        }
                    }
                }
            }
        }
    }
}
