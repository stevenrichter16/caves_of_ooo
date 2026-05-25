using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CONTENT-AUTHORABILITY pin for the Q5 world-object quest Parts. Proves a
    /// quest designer can attach CompleteObjectiveOnTaken / QuestStarter /
    /// FinishObjectiveWhenSlain to an entity blueprint via JSON (with field
    /// config) and EntityFactory instantiates them with the right field values
    /// — no code changes, no special-casing. This works because
    /// EntityFactory.RegisterPartsFromAssembly auto-registers ALL Part types
    /// and ApplyParameters sets public fields by name via reflection
    /// (EntityFactory.cs:62, 255). The pin guards that contract: a future
    /// EntityFactory change or a field rename would break it here.
    /// </summary>
    public class QuestWorldPartsBlueprintTests
    {
        // Each blueprint needs a Render part (EntityFactory requires it).
        private const string Blueprints = @"{
          ""Objects"": [
            {
              ""Name"": ""QuestRelic"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""relic"" }, { ""Key"": ""RenderString"", ""Value"": ""*"" } ] },
                { ""Name"": ""CompleteObjectiveOnTaken"", ""Params"": [ { ""Key"": ""Quest"", ""Value"": ""EnchiridionQuest"" }, { ""Key"": ""Objective"", ""Value"": ""find_enchiridion"" } ] }
              ],
              ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
            },
            {
              ""Name"": ""QuestScroll"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""scroll"" }, { ""Key"": ""RenderString"", ""Value"": ""?"" } ] },
                { ""Name"": ""QuestStarter"", ""Params"": [ { ""Key"": ""Quest"", ""Value"": ""EnchiridionQuest"" }, { ""Key"": ""IfQuestCompleted"", ""Value"": ""PrologueQuest"" } ] }
              ],
              ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
            },
            {
              ""Name"": ""QuestGuard"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""guard"" }, { ""Key"": ""RenderString"", ""Value"": ""g"" } ] },
                { ""Name"": ""FinishObjectiveWhenSlain"", ""Params"": [ { ""Key"": ""Quest"", ""Value"": ""EnchiridionQuest"" }, { ""Key"": ""Objective"", ""Value"": ""best_the_guardian"" } ] }
              ],
              ""Tags"": [ { ""Key"": ""Creature"", ""Value"": """" } ]
            }
          ]
        }";

        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(Blueprints);
        }

        [Test]
        public void Blueprint_AttachesCompleteObjectiveOnTaken_WithFields()
        {
            var relic = _factory.CreateEntity("QuestRelic");
            Assert.IsNotNull(relic, "QuestRelic blueprint instantiates");
            var part = relic.GetPart<CompleteObjectiveOnTaken>();
            Assert.IsNotNull(part, "CompleteObjectiveOnTaken attaches from the blueprint");
            Assert.AreEqual("EnchiridionQuest", part.Quest);
            Assert.AreEqual("find_enchiridion", part.Objective);
        }

        [Test]
        public void Blueprint_AttachesQuestStarter_WithFields()
        {
            var scroll = _factory.CreateEntity("QuestScroll");
            Assert.IsNotNull(scroll);
            var part = scroll.GetPart<QuestStarter>();
            Assert.IsNotNull(part, "QuestStarter attaches from the blueprint");
            Assert.AreEqual("EnchiridionQuest", part.Quest);
            Assert.AreEqual("PrologueQuest", part.IfQuestCompleted,
                "the optional prerequisite gate is settable from content");
            Assert.IsFalse(part.Activated, "Activated defaults false (fresh starter)");
        }

        [Test]
        public void Blueprint_AttachesFinishObjectiveWhenSlain_WithFields()
        {
            var guard = _factory.CreateEntity("QuestGuard");
            Assert.IsNotNull(guard);
            var part = guard.GetPart<FinishObjectiveWhenSlain>();
            Assert.IsNotNull(part, "FinishObjectiveWhenSlain attaches from the blueprint");
            Assert.AreEqual("EnchiridionQuest", part.Quest);
            Assert.AreEqual("best_the_guardian", part.Objective);
        }

        [Test]
        public void Blueprint_DistinctInstances_DoNotSharePartState()
        {
            // Two creations of the same blueprint get independent Part instances
            // (no shared mutable Part across entities).
            var a = _factory.CreateEntity("QuestScroll");
            var b = _factory.CreateEntity("QuestScroll");
            a.GetPart<QuestStarter>().Activated = true;
            Assert.IsFalse(b.GetPart<QuestStarter>().Activated,
                "spending one scroll's starter must not spend another instance's");
        }
    }
}
