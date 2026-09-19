using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Breadth pass B.1 (Docs/BREADTH-PASS.md): the journal and the menu titles tell
    /// the truth. Every shipped quest has a name the journal can show; a regional
    /// request closed outside the quest layer is listed under COMPLETED by its title
    /// (the closure line already counted it); and a cell title never names the player.
    /// </summary>
    public sealed class BreadthJournalTruthTests
    {
        private EntityFactory factory; private Zone zone;

        [SetUp] public void SetUp()
        {
            StoryletRegistry.Reset(); StoryletRegistry.LoadAll();
            factory = new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            zone = new Zone("BreadthTestZone");
        }
        [TearDown] public void TearDown() { StoryletRegistry.Reset(); }

        // ════════════════ names ════════════════

        [TestCase("MessageForHermit", "A Message for the Hermit")]
        [TestCase("ClearTheWarren", "The Warren Beneath")]
        [TestCase("HiddenShrine", "The Hidden Shrine")]
        public void EveryShippedQuest_HasAName_TheJournalShows(string id, string name)
        {
            Assert.AreEqual(name, StoryletRegistry.FindQuest(id)?.Name);
            Assert.AreEqual(name, StoryletPart.QuestDisplayName(id), "the journal reads the name, not the id");
        }

        [Test]
        public void NoShippedQuest_FallsBackToItsId()
        {
            // Counter-check over the whole registry: the id fallback is for missing content only.
            // A storylet without a quest still deserialises an empty QuestData (JsonUtility); only staged quests count.
            foreach (var s in StoryletRegistry.GetAll().Where(s => s.Quest?.Stages != null && s.Quest.Stages.Count > 0))
                Assert.IsFalse(string.IsNullOrEmpty(s.Quest.Name), s.ID + " has no name");
        }

        // ════════════════ regional closures in the journal ════════════════

        [Test]
        public void AClosedRegionalRequest_IsListedUnderCompleted_ByItsTitle()
        {
            var sp = new StoryletPart(); sp.UndertakeAct("regional:test:oil", "Oil for Sumphold"); Assert.IsTrue(sp.CloseAct("regional:test:oil"));
            var snap = QuestLogStateBuilder.Build(sp);
            CollectionAssert.Contains(snap.Completed, "Oil for Sumphold");
            Assert.AreEqual(1, snap.ClosureClosed, "the closure line and the list agree");
        }

        [Test]
        public void OpenAndRefusedRegionalRequests_AreNotListedAsCompleted()
        {
            var sp = new StoryletPart();
            sp.UndertakeAct("regional:test:open", "Iron for Orrit");
            sp.UndertakeAct("regional:test:refused", "Sand for Wellmeet"); Assert.IsTrue(sp.RefuseAct("regional:test:refused"));
            var snap = QuestLogStateBuilder.Build(sp);
            CollectionAssert.DoesNotContain(snap.Completed, "Iron for Orrit");
            CollectionAssert.DoesNotContain(snap.Completed, "Sand for Wellmeet");
            CollectionAssert.Contains(snap.Refused, "Sand for Wellmeet", "a refusal keeps its own section");
        }

        [Test]
        public void ACompletedQuest_IsListedOnce_NotTwice()
        {
            // The quest layer and the ledger both record a completion; the list must not double it.
            var sp = new StoryletPart(); sp.StartQuest(new QuestState { QuestId = "MessageForHermit", CurrentStageIndex = 0 }); sp.MarkQuestCompleted("MessageForHermit");
            var snap = QuestLogStateBuilder.Build(sp);
            Assert.AreEqual(1, snap.Completed.Count(t => t == "A Message for the Hermit"));
            Assert.AreEqual(1, snap.Completed.Count);
        }

        // ════════════════ cell titles never name the player ════════════════

        private Entity Player() { var p = factory.CreateEntity("Player"); Assert.IsTrue(p.HasTag("Player")); Assert.AreEqual("you", p.GetDisplayName()); return p; }

        [Test]
        public void YourOwnCell_IsDescribedByWhatIsUnderfoot_NotByYou()
        {
            zone.AddEntity(factory.CreateEntity("Floor"), 5, 5); zone.AddEntity(Player(), 5, 5);
            Assert.AreEqual("You see the floor.", WorldInteractionSystem.DescribeCell(zone.GetCell(5, 5)));
        }

        [Test]
        public void YourOwnCell_WithOneItem_NamesTheItem()
        {
            zone.AddEntity(factory.CreateEntity("Floor"), 5, 5); zone.AddEntity(Player(), 5, 5); zone.AddEntity(factory.CreateEntity("Tepuibone"), 5, 5);
            Assert.AreEqual("You see a tepuibone.", WorldInteractionSystem.DescribeCell(zone.GetCell(5, 5)));
        }

        [Test]
        public void YourOwnCell_WithTwoItems_IsAPileWithoutYouInIt()
        {
            zone.AddEntity(factory.CreateEntity("Floor"), 5, 5); zone.AddEntity(Player(), 5, 5);
            zone.AddEntity(factory.CreateEntity("Tepuibone"), 5, 5); zone.AddEntity(factory.CreateEntity("MemoryMarble"), 5, 5);
            string text = WorldInteractionSystem.DescribeCell(zone.GetCell(5, 5));
            StringAssert.StartsWith("A pile of items", text); StringAssert.DoesNotContain("you", text);
        }

        [Test]
        public void AnotherCreaturesCell_StillNamesTheCreature()
        {
            // Counter-check: only the player is left out; a villager is named as before.
            zone.AddEntity(factory.CreateEntity("Floor"), 5, 5); var v = factory.CreateEntity("Villager"); zone.AddEntity(v, 5, 5);
            StringAssert.Contains(v.GetDisplayName(), WorldInteractionSystem.DescribeCell(zone.GetCell(5, 5)));
        }
    }
}
