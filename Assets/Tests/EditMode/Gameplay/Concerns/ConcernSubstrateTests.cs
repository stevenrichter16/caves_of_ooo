using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// C1 (Docs/PLACE-WORK-AND-PROGRESSION.md §2): concerns compile into ordinary quests —
    /// a work stage the first finished approach resolves, a report stage the owner's turn-in
    /// completes — so the journal and the closure ledger see them; content is linted so an
    /// unplayable concern cannot ship; and a quest can observe a place's repair sites by
    /// place name (the conversation-only IfSettlementSiteStage needs a speaker, which tick
    /// dispatch never has).
    /// </summary>
    public sealed class ConcernSubstrateTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); MessageLog.Clear();
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            player = new Entity { BlueprintName = "Player", ID = "player" }; player.SetTag("Player");
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
        }
        [TearDown] public void Cleanup()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; SettlementManager.ResetCurrent();
        }

        static ConversationParam P(string k, string v) => new ConversationParam { Key = k, Value = v };
        static ConcernData Sample(string id = "probe.trouble") => new ConcernData
        {
            Id = id, Place = "Sill", Owner = "WellKeeper", Faction = "Villagers", Tier = 1,
            Title = "A Probe Trouble", Pitch = "Something is wrong.", Thanks = "It is mended.",
            Approaches = new List<ConcernApproachData>
            {
                new ConcernApproachData { Id = "mend", Kind = "Restore", Label = "Mend it.", Objective = "Mend it",
                    Triggers = { P("IfFact", "probe_mended:>=:1") }, Outcome = { P("SetFact", "probe_outcome:1") } },
                new ConcernApproachData { Id = "pay", Kind = "Settle", Label = "Pay someone.", Objective = "Pay someone",
                    Triggers = { P("IfFact", "probe_paid:>=:1") }, Outcome = { P("SetFact", "probe_outcome:2") } },
            },
            Refusal = new ConcernRefusalData { Text = "No.", Reply = "Then no." },
        };

        // ════════════════ compiling ════════════════

        [Test] public void AConcernCompilesToAWorkStageOfOptionalApproachesAndAReportStage()
        {
            var quest = ConcernRegistry.Compile(Sample()).Quest;
            Assert.AreEqual("A Probe Trouble", quest.Name);
            Assert.AreEqual(new[] { "work", "report" }, quest.Stages.Select(s => s.ID).ToArray());
            Assert.True(quest.Stages[0].Objectives.All(o => o.Optional), "any one approach resolves it");
            Assert.AreEqual(new[] { "mend", "pay" }, quest.Stages[0].Objectives.Select(o => o.ID).ToArray());
            Assert.IsEmpty(quest.Stages[1].Objectives);
        }

        [Test] public void TheFirstApproachFinishedResolvesTheWorkAndRecordsWhich()
        {
            ConcernRegistry.LoadFromJson(JsonUtility.ToJson(new ConcernFileData { Concerns = { Sample() } }));
            string q = ConcernRegistry.QuestIdFor("probe.trouble");
            storylets.StartQuest(new QuestState { QuestId = q });
            Assert.True(storylets.FinishObjective(q, "pay", player));
            Assert.AreEqual("report", StoryletRegistry.FindQuest(q).Stages[storylets.GetQuestState(q).CurrentStageIndex].ID);
            Assert.AreEqual(2, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("probe.trouble")), "the second approach");
            Assert.AreEqual(2, NarrativeStatePart.Current.GetFact("probe_outcome"), "its outcome ran");
            Assert.False(storylets.FinishObjective(q, "mend", player), "counter-check: the other approach no longer applies");
        }

        [Test] public void AnUndertakenConcernIsOpenOnTheLedgerUntilReportedOrRefused()
        {
            ConcernRegistry.LoadFromJson(JsonUtility.ToJson(new ConcernFileData { Concerns = { Sample("a.one"), Sample("a.two") } }));
            storylets.StartQuest(new QuestState { QuestId = ConcernRegistry.QuestIdFor("a.one") });
            storylets.StartQuest(new QuestState { QuestId = ConcernRegistry.QuestIdFor("a.two") });
            Assert.AreEqual(2, storylets.ReadLedger().Open);
            Assert.True(storylets.CompleteQuest(ConcernRegistry.QuestIdFor("a.one")));
            Assert.True(storylets.RefuseQuest(ConcernRegistry.QuestIdFor("a.two")));
            var reading = storylets.ReadLedger();
            Assert.AreEqual(1, reading.Closed); Assert.AreEqual(1, reading.Refused); Assert.True(reading.Clean);
        }

        // ════════════════ lint ════════════════

        [Test] public void ASampleConcernLintsClean() => Assert.IsEmpty(ConcernRegistry.Lint(Sample(), factory));

        [Test] public void LintRejectsUnplayableConcerns()
        {
            var one = Sample(); one.Approaches.RemoveAt(1);
            StringAssert.Contains("two approaches", string.Join("|", ConcernRegistry.Lint(one, factory)));
            var mute = Sample(); mute.Refusal = new ConcernRefusalData();
            StringAssert.Contains("spoken refusal", string.Join("|", ConcernRegistry.Lint(mute, factory)));
            var bad = Sample(); bad.Approaches[0].Triggers[0].Key = "IfNoSuchThing";
            StringAssert.Contains("unknown predicate", string.Join("|", ConcernRegistry.Lint(bad, factory)));
            var lost = Sample(); lost.Place = "Atlantis"; lost.Owner = "NoSuchResident"; lost.Next.Add("nowhere");
            var lines = string.Join("|", ConcernRegistry.Lint(lost, factory));
            StringAssert.Contains("unknown place", lines); StringAssert.Contains("unknown owner", lines); StringAssert.Contains("does not exist", lines);
            var never = Sample(); never.Approaches[1].Triggers.Clear();
            StringAssert.Contains("no trigger", string.Join("|", ConcernRegistry.Lint(never, factory)));
        }

        // ════════════════ observing a place's sites ════════════════

        [Test] public void APlaceSiteStageIsReadByPlaceNameAndMeansAtLeast()
        {
            var poi = new PointOfInterest(POIType.Village, "Sill");
            var manager = new SettlementManager(() => 10, id => id == "Overworld.10.10.0" ? poi : null);
            Assert.False(ConversationPredicates.Evaluate("IfPlaceSiteStage", null, player, "Sill:MainWell:TemporarilyPurified"), "fouled");
            var site = manager.GetSite("Overworld.10.10.0", SettlementSiteDefinitions.MainWellSiteId);
            Assume.That(site, Is.Not.Null, "precondition: Sill tracks its well");
            site.Stage = RepairStage.StableRepair;
            Assert.True(ConversationPredicates.Evaluate("IfPlaceSiteStage", null, player, "Sill:MainWell:TemporarilyPurified"), "stable is at least temporary");
            Assert.True(ConversationPredicates.Evaluate("IfPlaceSiteStage", null, player, "Sill:MainWell:StableRepair"));
            Assert.False(ConversationPredicates.Evaluate("IfPlaceSiteStage", null, player, "Sill:MainWell:ImprovedWithCaretaker"), "counter-check");
            Assert.False(ConversationPredicates.Evaluate("IfPlaceSiteStage", null, player, "Atlantis:MainWell:StableRepair"), "unknown place");
            Assert.False(ConversationPredicates.Evaluate("IfPlaceSiteStage", null, player, "Sill:MainWell"), "malformed");
        }

        // ════════════════ shipped content ════════════════

        [Test] public void EveryShippedConcernLintsClean()
        {
            ConcernRegistry.LoadAll();
            Assert.Greater(ConcernRegistry.All.Count, 0, "precondition: concerns ship");
            var problems = ConcernRegistry.All.SelectMany(c => ConcernRegistry.Lint(c, factory)).ToList();
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void SillsWellOvenAndLanternAreConcernsOnTheRepairSites()
        {
            ConcernRegistry.LoadAll();
            foreach (var (id, site) in new[] { ("sill.well", "MainWell"), ("sill.oven", "VillageOven"), ("sill.lantern", "VillageLantern") })
            {
                var c = ConcernRegistry.Get(id);
                Assert.NotNull(c, id);
                Assert.AreEqual("Sill", c.Place);
                Assert.GreaterOrEqual(c.Approaches.Count, 2, id + ": more than one way to a reliable fix");
                Assert.True(c.Approaches.All(a => a.Triggers.Any(t => (t.Key == "IfPlaceSiteStage" || t.Key == "IfPlaceSiteMethod") && t.Value.StartsWith("Sill:" + site + ":"))), id + " observes " + site);
            }
        }
    }
}
