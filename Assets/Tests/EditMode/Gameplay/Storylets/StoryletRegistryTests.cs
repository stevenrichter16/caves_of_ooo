using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using CavesOfOoo.Storylets;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Unit tests for StoryletRegistry. Mirrors HouseDramaLoaderTests' shape
    /// (matching the post-fix _loaded semantics from commit 83e9522) and
    /// adds coverage for the M0 finding C3 fix: load-time validation of
    /// trigger predicate and effect action names against the conversation
    /// registries — unknown names cause the storylet to be rejected with
    /// a warning rather than fail-OPEN at evaluate time.
    /// </summary>
    public class StoryletRegistryTests
    {
        [SetUp]
        public void Setup()
        {
            StoryletRegistry.Reset();
        }

        // ── Single storylet ───────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_ValidSingleStorylet_RegistersStorylet()
        {
            string json = @"{""Storylets"":[{""ID"":""Alpha"",""OneShot"":true,
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""x:>=:1""}],
                ""Effects"":[{""Key"":""AddMessage"",""Value"":""hi""}]}]}";

            StoryletRegistry.LoadFromJson(json, "test");

            var data = StoryletRegistry.Get("Alpha");
            Assert.IsNotNull(data);
            Assert.AreEqual("Alpha", data.ID);
            Assert.IsTrue(data.OneShot);
            Assert.IsFalse(data.IsQuest);
        }

        // ── Multi storylet ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_MultiStoryletJson_RegistersAll()
        {
            string json = @"{""Storylets"":[
                {""ID"":""Alpha"",""Triggers"":[{""Key"":""IfFact"",""Value"":""a:>=:1""}],""Effects"":[{""Key"":""AddMessage"",""Value"":""a""}]},
                {""ID"":""Beta"", ""Triggers"":[{""Key"":""IfFact"",""Value"":""b:>=:1""}],""Effects"":[{""Key"":""AddMessage"",""Value"":""b""}]}
            ]}";

            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsNotNull(StoryletRegistry.Get("Alpha"));
            Assert.IsNotNull(StoryletRegistry.Get("Beta"));
        }

        // ── Missing ID skipped ────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_StoryletMissingId_SkipsWithoutThrow()
        {
            string json = @"{""Storylets"":[{""OneShot"":true}]}";

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("has no ID"));
            Assert.DoesNotThrow(() => StoryletRegistry.LoadFromJson(json, "test"));

            Assert.AreEqual(0, StoryletRegistry.GetAll().Count);
        }

        // ── Duplicate ID overwrites ───────────────────────────────────────────

        [Test]
        public void LoadFromJson_DuplicateId_OverwritesEarlierEntry()
        {
            string json1 = @"{""Storylets"":[{""ID"":""Alpha"",""OneShot"":true,
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""a:>=:1""}],""Effects"":[]}]}";
            string json2 = @"{""Storylets"":[{""ID"":""Alpha"",""OneShot"":false,
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""b:>=:1""}],""Effects"":[]}]}";

            StoryletRegistry.LoadFromJson(json1, "file1");
            StoryletRegistry.LoadFromJson(json2, "file2");

            var data = StoryletRegistry.Get("Alpha");
            Assert.IsNotNull(data);
            Assert.IsFalse(data.OneShot);
        }

        // ── Null / empty JSON ─────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_NullJson_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => StoryletRegistry.LoadFromJson(null, "test"));
        }

        [Test]
        public void LoadFromJson_EmptyJson_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => StoryletRegistry.LoadFromJson("", "test"));
        }

        // ── Malformed JSON ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_MalformedJson_DoesNotThrow()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Failed to parse"));
            Assert.DoesNotThrow(() =>
                StoryletRegistry.LoadFromJson("{ not valid json }", "bad_file"));
        }

        // ── Register ──────────────────────────────────────────────────────────

        [Test]
        public void Register_AddsToLookup()
        {
            var s = new StoryletData { ID = "Manual" };
            StoryletRegistry.Register(s);

            Assert.IsNotNull(StoryletRegistry.Get("Manual"));
        }

        [Test]
        public void Register_NullData_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => StoryletRegistry.Register(null));
        }

        // ── _loaded semantics (the 83e9522 fix class) ─────────────────────────

        [Test]
        public void GetAll_AfterReset_AndRegisterThree_Returns3()
        {
            StoryletRegistry.Register(new StoryletData { ID = "A" });
            StoryletRegistry.Register(new StoryletData { ID = "B" });
            StoryletRegistry.Register(new StoryletData { ID = "C" });

            Assert.AreEqual(3, StoryletRegistry.GetAll().Count);
        }

        [Test]
        public void Reset_ClearsAllRegisteredStorylets()
        {
            StoryletRegistry.Register(new StoryletData { ID = "Alpha" });
            StoryletRegistry.Reset();

            Assert.IsNull(StoryletRegistry.Get("Alpha"));
            Assert.AreEqual(0, StoryletRegistry.GetAll().Count);
        }

        // ── Get unknown ───────────────────────────────────────────────────────

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            Assert.IsNull(StoryletRegistry.Get("nonexistent_storylet"));
        }

        // ── Quest sub-object discriminator ────────────────────────────────────

        [Test]
        public void StoryletData_WithoutQuest_IsQuestFalse()
        {
            string json = @"{""Storylets"":[{""ID"":""Alpha"",
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""a:>=:1""}],""Effects"":[]}]}";
            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsFalse(StoryletRegistry.Get("Alpha").IsQuest);
        }

        [Test]
        public void StoryletData_WithQuestAndStages_IsQuestTrue()
        {
            string json = @"{""Storylets"":[{""ID"":""Q1"",
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""q:>=:1""}],""Effects"":[],
                ""Quest"":{""Stages"":[
                    {""ID"":""Start"",""Triggers"":[{""Key"":""IfFact"",""Value"":""q:>=:1""}],""OnEnter"":[]},
                    {""ID"":""End"",  ""Triggers"":[{""Key"":""IfFact"",""Value"":""q:>=:2""}],""OnEnter"":[]}
                ]}}]}";
            StoryletRegistry.LoadFromJson(json, "test");

            var s = StoryletRegistry.Get("Q1");
            Assert.IsNotNull(s);
            Assert.IsTrue(s.IsQuest);
            Assert.AreEqual(2, s.Quest.Stages.Count);
            Assert.AreEqual("Start", s.Quest.Stages[0].ID);
        }

        // ── Load-time validation (M0 finding C3) ──────────────────────────────

        [Test]
        public void LoadFromJson_UnknownTriggerPredicate_RejectsStorylet()
        {
            string json = @"{""Storylets"":[{""ID"":""Bad"",
                ""Triggers"":[{""Key"":""IfFnordlblort"",""Value"":""x""}],
                ""Effects"":[{""Key"":""AddMessage"",""Value"":""hi""}]}]}";

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown predicate"));
            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsNull(StoryletRegistry.Get("Bad"));
        }

        [Test]
        public void LoadFromJson_UnknownEffectAction_RejectsStorylet()
        {
            string json = @"{""Storylets"":[{""ID"":""Bad"",
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""x:>=:1""}],
                ""Effects"":[{""Key"":""DoXyzzy"",""Value"":""x""}]}]}";

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown action"));
            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsNull(StoryletRegistry.Get("Bad"));
        }

        [Test]
        public void LoadFromJson_KnownIfNotPredicate_IsAccepted()
        {
            // IfNotFact is not directly registered; it's auto-inverted from IfFact.
            // The IsRegistered accessor must accept it.
            string json = @"{""Storylets"":[{""ID"":""Inv"",
                ""Triggers"":[{""Key"":""IfNotFact"",""Value"":""x:>=:1""}],
                ""Effects"":[{""Key"":""AddMessage"",""Value"":""hi""}]}]}";

            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsNotNull(StoryletRegistry.Get("Inv"));
        }

        [Test]
        public void LoadFromJson_QuestStageWithUnknownTrigger_RejectsStorylet()
        {
            string json = @"{""Storylets"":[{""ID"":""BadQuest"",
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""q:>=:1""}],""Effects"":[],
                ""Quest"":{""Stages"":[
                    {""ID"":""Start"",""Triggers"":[{""Key"":""IfBogus"",""Value"":""x""}],""OnEnter"":[]}
                ]}}]}";

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown predicate"));
            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsNull(StoryletRegistry.Get("BadQuest"));
        }

        [Test]
        public void LoadFromJson_QuestStageWithUnknownOnEnterAction_RejectsStorylet()
        {
            string json = @"{""Storylets"":[{""ID"":""BadQuest"",
                ""Triggers"":[{""Key"":""IfFact"",""Value"":""q:>=:1""}],""Effects"":[],
                ""Quest"":{""Stages"":[
                    {""ID"":""Start"",""Triggers"":[{""Key"":""IfFact"",""Value"":""q:>=:1""}],""OnEnter"":[{""Key"":""DoBogus"",""Value"":""""}]}
                ]}}]}";

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown action"));
            StoryletRegistry.LoadFromJson(json, "test");

            Assert.IsNull(StoryletRegistry.Get("BadQuest"));
        }
    }
}
