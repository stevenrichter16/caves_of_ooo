using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 6 — narrative-feedback (P0). Two silent
    /// failure classes: (1) HouseDramaZoneBuilder stamped
    /// Drama_&lt;id&gt;_&lt;role&gt; ConversationIDs WITHOUT checking the
    /// content exists — the never-authored Drama_Vex_* set left four
    /// NPCs per affected village completely mute (including a broken
    /// merchant); (2) quest lifecycle transitions (accept / objective /
    /// complete / fail) produced zero on-screen feedback.
    /// </summary>
    [TestFixture]
    public class AlphaNarrativeFeedbackTests
    {
        private const string DramaId = "AlphaGuardTestDrama";
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            HouseDramaRuntime.Reset();
            HouseDramaLoader.Reset();
            ConversationLoader.Reset();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [TearDown]
        public void TearDown()
        {
            ConversationLoader.Reset();
            HouseDramaLoader.Reset();
            HouseDramaRuntime.Reset();
        }

        private Entity BuildDramaNpc()
        {
            var drama = new HouseDramaData
            {
                ID = DramaId,
                NpcRoles = new List<NpcRoleData>
                {
                    new NpcRoleData { Id = "n1", Role = "SilencedHelper", Alive = true }
                }
            };
            HouseDramaLoader.Register(drama);
            var zone = new Zone("T");
            new HouseDramaZoneBuilder(DramaId).BuildZone(zone, _factory, new System.Random(42));
            foreach (var e in zone.GetAllEntities())
                if (e.GetPart<HouseDramaPart>() != null) return e;
            return null;
        }

        // ── SM1: missing-content guard ───────────────────────────

        [Test]
        public void DramaBuilder_MissingConversationContent_KeepsBlueprintDialogue()
        {
            // No Drama_* conversation registered → the NPC must keep its
            // blueprint's WORKING ConversationID instead of pointing at
            // nothing and going mute.
            var npc = BuildDramaNpc();
            Assert.IsNotNull(npc);
            var conv = npc.GetPart<ConversationPart>();
            Assert.IsNotNull(conv);
            StringAssert.DoesNotStartWith("Drama_", conv.ConversationID,
                "unauthored drama content must not hijack the NPC's dialogue");
            Assert.IsFalse(string.IsNullOrEmpty(conv.ConversationID),
                "the blueprint conversation survives");
        }

        [Test]
        public void DramaBuilder_ExistingConversationContent_StillStamps()
        {
            // Counter-check: when the drama conversation EXISTS, the
            // override happens exactly as before.
            ConversationLoader.Register(new ConversationData
            {
                ID = $"Drama_{DramaId}_SilencedHelper",
            });
            var npc = BuildDramaNpc();
            var conv = npc.GetPart<ConversationPart>();
            Assert.AreEqual($"Drama_{DramaId}_SilencedHelper", conv.ConversationID);
        }

        // ── SM3: quest lifecycle feedback ────────────────────────

        [Test]
        public void StartQuest_EmitsAcceptedMessage_OnceForNewQuest()
        {
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = "TestQuest" });
            Assert.IsTrue(MessageLog.GetMessages().Exists(
                    m => m.Contains("Quest accepted") && m.Contains("TestQuest")),
                "accepting a quest must be visible on screen");

            MessageLog.Clear();
            part.StartQuest(new QuestState { QuestId = "TestQuest" });
            Assert.IsFalse(MessageLog.GetMessages().Exists(m => m.Contains("Quest accepted")),
                "re-starting an already-active quest must not re-announce");
        }

        [Test]
        public void CompleteQuest_EmitsCompleteMessage()
        {
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = "DoneQuest" });
            MessageLog.Clear();

            bool ok = part.CompleteQuest("DoneQuest");

            Assert.IsTrue(ok);
            Assert.IsTrue(MessageLog.GetMessages().Exists(
                m => m.Contains("Quest complete") && m.Contains("DoneQuest")));
        }

        [Test]
        public void CompleteQuest_NotActive_EmitsNothing()
        {
            // Counter-check: the rejected path stays silent on screen
            // (it has its own diag record).
            var part = new StoryletPart();
            bool ok = part.CompleteQuest("NeverStarted");
            Assert.IsFalse(ok);
            Assert.IsFalse(MessageLog.GetMessages().Exists(m => m.Contains("Quest complete")));
        }
    }
}
