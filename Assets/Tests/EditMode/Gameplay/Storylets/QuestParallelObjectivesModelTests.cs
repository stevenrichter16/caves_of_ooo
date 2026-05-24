using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q3.1 (Docs/QUEST-PARALLEL-OBJECTIVES.md) — the parallel-objective
    /// DATA MODEL: <see cref="QuestObjectiveData"/> +
    /// <see cref="QuestStageData.Objectives"/> deserialize from JSON, and
    /// <see cref="QuestState.FinishedObjectives"/> round-trips through
    /// StoryletPart save/load. Dispatch + API land in Q3.2.
    /// </summary>
    public class QuestParallelObjectivesModelTests
    {
        // ════════════════ JSON deserialization (JsonUtility) ════════════════

        [Test]
        public void Json_StageWithObjectives_ParsesTextFlagsAndTriggers()
        {
            string json = @"{ ""Storylets"": [ { ""ID"": ""Q1"", ""Quest"": { ""Stages"": [
              { ""ID"": ""s0"", ""Objectives"": [
                { ""ID"": ""kill_boss"", ""Text"": ""Defeat the guardian"",
                  ""Optional"": false, ""Hidden"": false,
                  ""Triggers"": [ { ""Key"": ""IfActorDead"", ""Value"": ""Boss"" } ] },
                { ""ID"": ""loot_chest"", ""Text"": ""Loot the chest"",
                  ""Optional"": true, ""Hidden"": true } ] } ] } } ] }";

            var fileData = JsonUtility.FromJson<StoryletFileData>(json);
            var stage = fileData.Storylets[0].Quest.Stages[0];

            Assert.AreEqual(2, stage.Objectives.Count,
                "both objectives must deserialize from the JSON array");
            Assert.AreEqual("kill_boss", stage.Objectives[0].ID);
            Assert.AreEqual("Defeat the guardian", stage.Objectives[0].Text);
            Assert.IsFalse(stage.Objectives[0].Optional);
            Assert.IsFalse(stage.Objectives[0].Hidden);
            Assert.AreEqual(1, stage.Objectives[0].Triggers.Count,
                "objective-level Triggers reuse the ConversationParam vocabulary");
            Assert.AreEqual("IfActorDead", stage.Objectives[0].Triggers[0].Key);
            Assert.AreEqual("Boss", stage.Objectives[0].Triggers[0].Value);
            // Optional/Hidden flags carry through.
            Assert.IsTrue(stage.Objectives[1].Optional);
            Assert.IsTrue(stage.Objectives[1].Hidden);
        }

        [Test]
        public void Json_StageWithoutObjectives_HasEmptyObjectives_NotNull()
        {
            // Backward-compat counter-check: existing content has no
            // Objectives node — the field must default to an empty list so
            // the dispatch (Q3.2) can treat "no objectives" as the legacy
            // linear path without a null check.
            string json = @"{ ""Storylets"": [ { ""ID"": ""Q1"", ""Quest"": { ""Stages"": [
              { ""ID"": ""s0"", ""Triggers"": [] } ] } } ] }";

            var fileData = JsonUtility.FromJson<StoryletFileData>(json);
            var stage = fileData.Storylets[0].Quest.Stages[0];

            Assert.IsNotNull(stage.Objectives,
                "Objectives must default to an empty list (not null) when the " +
                "JSON omits it — JsonUtility runs the field initializer.");
            Assert.AreEqual(0, stage.Objectives.Count);
        }

        // ════════════════ Save round-trip ════════════════

        private static StoryletPart RoundTrip(StoryletPart part)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            part.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = new StoryletPart();
            loaded.Load(reader);
            return loaded;
        }

        [Test]
        public void SaveRoundTrip_FinishedObjectives_Preserved()
        {
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            var st = part.GetQuestState("Q1");
            st.FinishedObjectives.Add("obj_a");
            st.FinishedObjectives.Add("obj_b");

            var loaded = RoundTrip(part);
            var ls = loaded.GetQuestState("Q1");

            Assert.IsNotNull(ls, "the quest itself must round-trip");
            Assert.AreEqual(2, ls.FinishedObjectives.Count);
            Assert.IsTrue(ls.FinishedObjectives.Contains("obj_a"));
            Assert.IsTrue(ls.FinishedObjectives.Contains("obj_b"));
        }

        [Test]
        public void SaveRoundTrip_NoFinishedObjectives_StaysEmptyNotNull()
        {
            // Counter-check: a quest with no finished objectives round-trips
            // with an empty (non-null) set — the dispatch can iterate it
            // without a null guard.
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = "Q1" });

            var loaded = RoundTrip(part);
            var ls = loaded.GetQuestState("Q1");

            Assert.IsNotNull(ls.FinishedObjectives,
                "FinishedObjectives must be an empty set, never null, after load");
            Assert.AreEqual(0, ls.FinishedObjectives.Count);
        }

        [Test]
        public void SaveRoundTrip_MultipleQuests_ObjectivesKeyedCorrectly()
        {
            // Adversarial: two quests, each with distinct finished objectives.
            // Pins that the trailing section keys objectives to the right
            // quest (a buggy section that dropped the key would cross-wire).
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = "QA" });
            part.StartQuest(new QuestState { QuestId = "QB" });
            part.GetQuestState("QA").FinishedObjectives.Add("a1");
            part.GetQuestState("QB").FinishedObjectives.Add("b1");
            part.GetQuestState("QB").FinishedObjectives.Add("b2");

            var loaded = RoundTrip(part);

            Assert.AreEqual(1, loaded.GetQuestState("QA").FinishedObjectives.Count);
            Assert.IsTrue(loaded.GetQuestState("QA").FinishedObjectives.Contains("a1"));
            Assert.AreEqual(2, loaded.GetQuestState("QB").FinishedObjectives.Count);
            Assert.IsFalse(loaded.GetQuestState("QA").FinishedObjectives.Contains("b1"),
                "objectives must not cross-wire between quests");
        }

        [Test]
        public void Load_PreQ3Save_NoObjectivesSection_DefaultsEmptyWithoutThrowing()
        {
            // CLAUDE.md review finding (the load-bearing back-compat claim):
            // a pre-Q3 save has NO trailing objectives section. Hand-write
            // that exact byte layout (fired + quests[3 fields] + completed)
            // and confirm the EOF guard defaults FinishedObjectives to an
            // empty set instead of throwing. This is the whole reason the
            // objectives section is a SEPARATE trailing block.
            using var stream = new MemoryStream();
            var w = new SaveWriter(stream);
            w.Write(0);              // _firedStorylets count
            w.Write(1);              // _quests count
            w.WriteString("Q1");     // dict key
            w.WriteString("Q1");     // QuestId
            w.Write(2);              // CurrentStageIndex
            w.Write(7);              // EnteredStageAtTurn
            w.Write(0);              // _completedQuests count
            // (no objectives section — this is the pre-Q3 format)

            stream.Position = 0;
            var loaded = new StoryletPart();
            Assert.DoesNotThrow(() => loaded.Load(new SaveReader(stream, null)),
                "pre-Q3 save (no objectives section) must load via the EOF guard");

            var ls = loaded.GetQuestState("Q1");
            Assert.IsNotNull(ls, "the pre-Q3 quest still loads");
            Assert.AreEqual(2, ls.CurrentStageIndex, "legacy fields intact");
            Assert.AreEqual(7, ls.EnteredStageAtTurn);
            Assert.IsNotNull(ls.FinishedObjectives,
                "FinishedObjectives defaults to an empty set on a pre-Q3 save");
            Assert.AreEqual(0, ls.FinishedObjectives.Count);
        }
    }
}
