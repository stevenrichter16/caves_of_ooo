using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.6 (Docs/FELLING-W4-PLAN.md §3 + R6) — the pruning contract:
    /// the canonical first-act tradeoff, at Cinderhold's Concord post.
    /// Accept: carry the Concord's pruning WRIT into Choir country and
    /// post it — to the grove's own face (the tendril) — then report
    /// back. Accept costs RotChoir standing at the posting; the Concord
    /// pays standing and drams at the report. Refuse aloud: the Concord
    /// notes it, and it costs THEM standing with you — the spoken-no
    /// register. Both sides cost something; neither is wrong (R6 pins
    /// BOTH deltas so a balance pass cannot silently flatten the
    /// tradeoff). The Concord never says "free" (§9 gate 12).
    /// </summary>
    public class PruningContractTests
    {
        private static EntityFactory _factory;
        private static string _friendly, _rotchoir, _quest;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            _friendly = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json"));
            _rotchoir = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/RotChoir.json"));
            var questPath = Path.Combine(
                Application.dataPath, "Resources/Content/Data/Storylets/PruningContract.json");
            _quest = File.Exists(questPath) ? File.ReadAllText(questPath) : "";
        }

        // ════════════════════════════════════════════════════════════
        //   The post and its factor
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cinderhold_HasItsFactor()
        {
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var zone = mgr.GetZone("Overworld.6.6.0");
            Assert.IsNotNull(zone);
            int factors = 0, walls = 0;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName == "ConcordFactor") factors++;
                if (e.BlueprintName == "SandstoneWall") walls++;
            }
            Assert.AreEqual(1, factors, "one factor keeps the post");
            Assert.Greater(walls, 0, "and the post has walls");
        }

        [Test]
        public void TheFactor_WearsTheConcordsColors()
        {
            // R9c: any new Order NPC gets a faction pin the day it ships.
            var factor = _factory.CreateEntity("ConcordFactor");
            Assert.IsNotNull(factor, "the blueprint ships");
            Assert.AreEqual("SaccharineConcord", factor.Tags.ContainsKey("Faction")
                ? factor.Tags["Faction"] : null);
            var conv = factor.GetPart<ConversationPart>();
            Assert.IsNotNull(conv, "and speaks");
            Assert.AreEqual("ConcordFactor_1", conv.ConversationID);
        }

        [Test]
        public void TheWrit_CannotBeSold()
        {
            var writ = _factory.CreateEntity("PruningWrit");
            Assert.IsNotNull(writ, "the writ ships");
            Assert.IsTrue(writ.HasTag("NoTrade"),
                "contract paper is not merchandise (the courier lesson)");
        }

        // ════════════════════════════════════════════════════════════
        //   The wiring (cross-file fact lint, the W3.6 pattern)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheFact_ThreadsAllThreeFiles()
        {
            StringAssert.Contains("pruning_writ_posted:1", _rotchoir,
                "the tendril posting SETS it");
            StringAssert.Contains("pruning_writ_posted:>=:1", _quest,
                "the quest objective TRIGGERS on it");
            StringAssert.Contains("pruning_writ_posted:>=:1", _friendly,
                "the factor's report choice GATES on it");
        }

        [Test]
        public void BothDeltas_ArePinned_SoNeitherFlattens()
        {
            // R6: accepting angers a god's landscape; refusing angers a
            // shipping company. Asymmetric ON PURPOSE.
            StringAssert.Contains("\"ChangeFactionFeeling\", \"Value\": \"RotChoir:Player:-15\"",
                _rotchoir, "posting costs the grove's regard, at the posting");
            StringAssert.Contains("\"ChangeFactionFeeling\", \"Value\": \"SaccharineConcord:Player:10\"",
                _friendly, "the Concord pays standing at the report");
            StringAssert.Contains("\"GiveDrams\", \"Value\": \"20\"", _friendly,
                "and drams");
            StringAssert.Contains("\"ChangeFactionFeeling\", \"Value\": \"SaccharineConcord:Player:-5\"",
                _friendly, "refusing aloud costs the Concord's regard of you");
        }

        [Test]
        public void TheContract_StartsAndCompletes_ByTheBook()
        {
            StringAssert.Contains("\"StartQuest\", \"Value\": \"PruningContract\"", _friendly);
            StringAssert.Contains("\"GiveItem\", \"Value\": \"PruningWrit\"", _friendly);
            StringAssert.Contains("\"CompleteQuest\", \"Value\": \"PruningContract\"", _friendly);
            StringAssert.Contains("\"TakeItem\", \"Value\": \"PruningWrit\"", _rotchoir,
                "the grove keeps what was posted to it");
        }

        // ════════════════════════════════════════════════════════════
        //   Voice
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheConcord_NeverSaysFree()
        {
            // §9 gate 12. Scan every node of the factor's tree.
            ConversationLoader.Reset();
            try
            {
                ConversationLoader.LoadFromJson(_friendly);
                var conv = ConversationLoader.Get("ConcordFactor_1");
                Assert.IsNotNull(conv, "the factor's tree loads");
                foreach (var node in conv.Nodes)
                    StringAssert.DoesNotContain("free",
                        (node.Text ?? "").ToLowerInvariant(),
                        "ConcordFactor_1/" + node.ID +
                        ": nothing is free in the Concord's mouth");
            }
            finally
            {
                ConversationLoader.Reset();
            }
        }
    }
}
