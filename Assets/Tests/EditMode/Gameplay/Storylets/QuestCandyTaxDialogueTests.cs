using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// End-to-end dialogue tests for "The Candy Tax" (SM2,
    /// Docs/QUEST-POOL-EXPANSION.md) — the collect-N-via-dialogue pool quest.
    /// Three candy citizens SHARE one `CandyCitizen` conversation; each must
    /// contribute to the `candy_taxes_collected` counter EXACTLY ONCE, gated by
    /// a per-entity speaker property (`candy_taxed`) so re-talking can't double-
    /// count. This exercises the AddFact dialogue counter + the
    /// IfNotSpeakerHaveProperty / SetSpeakerProperty once-gate through the real
    /// ConversationManager.SelectChoice path (not a bypass).
    /// </summary>
    [TestFixture]
    public class QuestCandyTaxDialogueTests
    {
        private const string QuestId = "TheCandyTax";

        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            ConversationLoader.Reset();
            StoryletRegistry.Reset();
            ConversationManager.EndConversation();
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = new NarrativeStatePart();

            // Load the SHIPPED citizen conversation from disk.
            string convPath = Path.Combine(
                "Assets", "Resources", "Content", "Conversations", "CandyCitizen.json");
            ConversationLoader.LoadFromJson(File.ReadAllText(convPath), "CandyCitizen.json");

            // Minimal quest def so IfQuestActive/IfQuestStage resolve.
            StoryletRegistry.Register(new StoryletData
            {
                ID = QuestId,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "collect" },
                        new QuestStageData { ID = "report" },
                    },
                },
            });
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            // Clear the partial loader cache so it can't leak (with _loaded=true)
            // into a later in-editor playtest (the Elder_1 stale-cache lesson).
            ConversationLoader.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
        }

        private static Entity MakeCitizen()
        {
            var citizen = new Entity { BlueprintName = "Villager" };
            citizen.AddPart(new ConversationPart { ConversationID = "CandyCitizen" });
            return citizen;
        }

        private static Entity MakePlayer()
        {
            var p = new Entity { BlueprintName = "Player" };
            p.Tags["Player"] = "";
            p.AddPart(new InventoryPart());
            return p;
        }

        private void StartQuestActive()
        {
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
        }

        private static List<string> VisibleChoiceTexts(Entity speaker, Entity listener)
        {
            ConversationManager.StartConversation(speaker, listener);
            return ConversationManager.VisibleChoices.Select(c => c.Text).ToList();
        }

        private static bool SelectChoiceStartingWith(string prefix)
        {
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text.StartsWith(prefix))
                {
                    ConversationManager.SelectChoice(i);
                    return true;
                }
            }
            return false;
        }

        // ════════════════ the happy path ════════════════

        [Test]
        public void CollectTax_WhenQuestActive_IncrementsCounter_AndMarksCitizen()
        {
            StartQuestActive();
            var citizen = MakeCitizen();
            var player = MakePlayer();

            var choices = VisibleChoiceTexts(citizen, player);
            Assert.IsTrue(choices.Any(c => c.StartsWith("[Collect")),
                "with the quest active and citizen unpaid, the collect choice must show");

            Assert.IsTrue(SelectChoiceStartingWith("[Collect"));

            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("candy_taxes_collected"),
                "collecting increments the shared dialogue counter once");
            Assert.IsTrue(citizen.Properties.ContainsKey("candy_taxed"),
                "the citizen is marked taxed (its own entity property) so it can't be re-collected");
        }

        // ════════════════ counter-checks ════════════════

        [Test]
        public void CollectTax_ReTalkSameCitizen_DoesNotDoubleCount()
        {
            // Counter-check: a buggy impl without the per-citizen gate would let
            // the player re-talk ONE citizen to farm the counter.
            StartQuestActive();
            var citizen = MakeCitizen();
            var player = MakePlayer();

            VisibleChoiceTexts(citizen, player);
            SelectChoiceStartingWith("[Collect");
            ConversationManager.EndConversation();

            var choices2 = VisibleChoiceTexts(citizen, player);
            Assert.IsFalse(choices2.Any(c => c.StartsWith("[Collect")),
                "a paid citizen must NOT re-offer the collect choice (IfNotSpeakerHaveProperty gate)");
            Assert.IsTrue(choices2.Any(c => c.StartsWith("[Already paid")),
                "a paid citizen shows the already-paid line instead");
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("candy_taxes_collected"),
                "re-talking a paid citizen does not inflate the counter");
        }

        [Test]
        public void CollectTax_BeforeQuestAccepted_ChoiceHidden()
        {
            // Quest NOT active → no collect choice (no soft-lock: the player
            // accepts at the giver first, then collects). Order-independent-safe.
            var citizen = MakeCitizen();
            var player = MakePlayer();

            var choices = VisibleChoiceTexts(citizen, player);
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Collect")),
                "before the quest is active the collect choice must be hidden (IfQuestActive gate)");
        }

        [Test]
        public void ThreeCitizens_EachContributeOnce_CounterReachesThree()
        {
            // The collect-N proof, end-to-end: three DISTINCT citizen entities,
            // each contributes once → counter == 3 → the objective's
            // IfFact:candy_taxes_collected:>=:3 is satisfiable in the world.
            StartQuestActive();
            var player = MakePlayer();

            for (int i = 0; i < 3; i++)
            {
                var citizen = MakeCitizen();
                VisibleChoiceTexts(citizen, player);
                Assert.IsTrue(SelectChoiceStartingWith("[Collect"),
                    $"citizen {i} must offer the collect choice (each is a fresh entity)");
                ConversationManager.EndConversation();
            }

            Assert.AreEqual(3, NarrativeStatePart.Current.GetFact("candy_taxes_collected"),
                "three distinct citizens each contributing once reaches the threshold of 3");
        }
    }
}
