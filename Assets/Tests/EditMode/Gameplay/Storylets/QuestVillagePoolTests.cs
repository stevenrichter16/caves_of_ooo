using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The village-quest POOL (Docs/QUEST-DESIGN-CATALOG.md): non-starting
    /// villages each host ONE quest, picked deterministically by zone ID
    /// (VillagePopulationBuilder.PickVillageQuest). Tests the distribution
    /// (deterministic + both quests reachable) and the content integrity of
    /// the two pool quests (Crunchy's Locket = fetch, Hidden Shrine = reach).
    /// </summary>
    public class QuestVillagePoolTests
    {
        // ════════════════ distribution (the pick) ════════════════

        [Test]
        public void PickVillageQuest_IsDeterministicPerZone()
        {
            Assert.AreEqual(
                VillagePopulationBuilder.PickVillageQuest("Overworld.11.10.0"),
                VillagePopulationBuilder.PickVillageQuest("Overworld.11.10.0"),
                "the same village always hosts the same quest (stable per zone)");
        }

        [Test]
        public void Pool_HasNoDuplicates()
        {
            var pool = VillagePopulationBuilder.VillageQuestPoolIds;
            Assert.AreEqual(pool.Count, pool.Distinct().Count(),
                "the pool must not contain duplicate quest IDs (a dupe wastes a slot + skews the pick)");
        }

        [Test]
        public void PickVillageQuest_EveryPickIsInThePool_AndAllAreReachable()
        {
            // Pool-agnostic: derive expectations from the actual pool so this
            // test survives future pool growth untouched. Across many distinct
            // zone IDs every pool quest must appear at least once (the pick
            // distributes) and no pick may fall outside the pool.
            var pool = VillagePopulationBuilder.VillageQuestPoolIds;
            var poolSet = pool.ToList();
            var seen = new HashSet<string>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                {
                    string q = VillagePopulationBuilder.PickVillageQuest($"Overworld.{x}.{y}.0");
                    Assert.Contains(q, poolSet, "every pick must be a pool quest");
                    seen.Add(q);
                }
            Assert.AreEqual(pool.Count, seen.Count,
                "across many villages EVERY pool quest appears (all reachable; the pick distributes)");
        }

        // ════════════════ content integrity ════════════════

        private static StoryletData LoadQuest(string id)
        {
            var ta = Resources.Load<TextAsset>("Content/Data/Storylets/" + id);
            Assert.IsNotNull(ta, id + ".json must live in Resources/Content/Data/Storylets");
            var q = JsonUtility.FromJson<StoryletFileData>(ta.text).Storylets.FirstOrDefault(s => s.ID == id);
            Assert.IsNotNull(q, $"quest '{id}' must be in the file");
            return q;
        }

        private static ConversationData LoadConvo(string id)
        {
            var ta = Resources.Load<TextAsset>("Content/Conversations/" + id);
            Assert.IsNotNull(ta, id + ".json must live in Resources/Content/Conversations");
            var c = JsonUtility.FromJson<ConversationFileData>(ta.text).Conversations.FirstOrDefault();
            Assert.IsNotNull(c);
            return c;
        }

        [Test]
        public void CrunchyLocket_FetchObjective_PollsIfHaveItem()
        {
            var q = LoadQuest("CrunchyLocket");
            var find = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == "find_locket");
            Assert.IsNotNull(find);
            var have = find.Triggers.FirstOrDefault(t => t.Key == "IfHaveItem");
            Assert.IsNotNull(have, "find_locket must poll IfHaveItem (order-independent)");
            Assert.AreEqual("CrunchyLocket", have.Value, "IfHaveItem must match the locket blueprint the builder places");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment));
        }

        [Test]
        public void HiddenShrine_ReachObjective_PollsTheShrineFact()
        {
            var q = LoadQuest("HiddenShrine");
            var reach = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == "reach_shrine");
            Assert.IsNotNull(reach);
            var iffact = reach.Triggers.FirstOrDefault(t => t.Key == "IfFact");
            Assert.IsNotNull(iffact, "reach_shrine must poll IfFact (order-independent)");
            Assert.AreEqual("shrine_reached:>=:1", iffact.Value,
                "IfFact must match the fact the placed shrine marker's QuestMarkerTrigger sets");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment));
        }

        [Test]
        public void ClearTheWarren_KillObjective_PollsCounterFact()
        {
            // The kill-N counter quest: the objective must poll the SHARED
            // counter fact via IfFact:>=:3 (order-independent), matching the
            // fact the builder's 3 AddFactWhenSlain gnomes increment.
            // String const mirrors VillagePopulationBuilder.PlaceWarrenQuest
            // (the builder↔content seam).
            var q = LoadQuest("ClearTheWarren");
            var rout = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == "rout_gnomes");
            Assert.IsNotNull(rout, "the kill-N objective the builder's counter mobs drive must exist");
            var iffact = rout.Triggers.FirstOrDefault(t => t.Key == "IfFact");
            Assert.IsNotNull(iffact, "rout_gnomes must poll IfFact (order-independent counter, NOT Part-only)");
            Assert.AreEqual("warren_gnomes_routed:>=:3", iffact.Value,
                "IfFact must match the shared fact AddFactWhenSlain sets on the 3 placed gnomes, threshold 3");
            Assert.AreEqual(3, iffact.Value.Split(':').Length, "IfFact arg is key:OP:threshold (3 parts)");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment), "has a Q7 accomplishment deed");
        }

        [Test]
        public void TheCandyTax_CollectObjective_PollsTheDialogueCounter()
        {
            // The collect-N dialogue counter quest: the objective polls the
            // AddFact counter via IfFact:>=:3. The fact MUST match the AddFact
            // the CandyCitizen conversation increments (content↔content seam).
            var q = LoadQuest("TheCandyTax");
            var collect = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == "collect_taxes");
            Assert.IsNotNull(collect, "the collect-N objective must exist");
            var iffact = collect.Triggers.FirstOrDefault(t => t.Key == "IfFact");
            Assert.IsNotNull(iffact, "collect_taxes must poll IfFact (order-independent)");
            Assert.AreEqual("candy_taxes_collected:>=:3", iffact.Value,
                "IfFact must match the counter the CandyCitizen dialogue AddFacts, threshold 3");
            Assert.AreEqual(3, iffact.Value.Split(':').Length, "IfFact arg is key:OP:threshold (3 parts)");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment), "has a Q7 accomplishment deed");
        }

        [Test]
        public void CandyCitizen_CollectChoice_GatedOncePerCitizen()
        {
            // Pin the once-per-citizen gate at the CONTENT level: the collect
            // choice must require BOTH the quest active AND the speaker NOT yet
            // taxed, and must SetSpeakerProperty so re-talk is gated out.
            var c = LoadConvo("CandyCitizen");
            var start = c.Nodes.FirstOrDefault(n => n.ID == "Start");
            Assert.IsNotNull(start, "CandyCitizen needs a Start node");
            var collect = start.Choices.FirstOrDefault(ch => ch.Text.StartsWith("[Collect"));
            Assert.IsNotNull(collect, "Start must offer a [Collect...] choice");

            var preds = collect.Predicates.Select(p => p.Key).ToList();
            Assert.Contains("IfQuestActive", preds, "collect requires the quest active (no collect-before-accept)");
            Assert.Contains("IfNotSpeakerHaveProperty", preds, "collect requires the citizen NOT already taxed");

            var acts = collect.Actions.ToDictionary(a => a.Key, a => a.Value);
            Assert.IsTrue(acts.ContainsKey("AddFact") && acts["AddFact"] == "candy_taxes_collected:1",
                "collect increments the shared counter by 1");
            Assert.IsTrue(acts.ContainsKey("SetSpeakerProperty") && acts["SetSpeakerProperty"].StartsWith("candy_taxed"),
                "collect marks THIS citizen taxed so IfNotSpeakerHaveProperty hides it next time");
        }

        [Test]
        public void MessageForHermit_DeliverObjective_PollsTheDeliveredFact()
        {
            // The deliver quest: the objective polls the fact the RECIPIENT's
            // dialogue SetFacts. The fact MUST match Hermit_Quest's SetFact
            // (giver-quest ↔ recipient-dialogue seam).
            var q = LoadQuest("MessageForHermit");
            var deliver = q.Quest.Stages[0].Objectives.FirstOrDefault(o => o.ID == "deliver_message");
            Assert.IsNotNull(deliver, "the deliver objective must exist");
            var iffact = deliver.Triggers.FirstOrDefault(t => t.Key == "IfFact");
            Assert.IsNotNull(iffact, "deliver_message must poll IfFact (order-independent)");
            Assert.AreEqual("hermit_message_delivered:>=:1", iffact.Value,
                "IfFact must match the fact the hermit's [Deliver] choice SetFacts");
            Assert.AreEqual(3, iffact.Value.Split(':').Length, "IfFact arg is key:OP:threshold (3 parts)");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment), "has a Q7 accomplishment deed");
        }

        [Test]
        public void HermitRecipient_DeliverChoice_GatedAndSetsFact()
        {
            // Pin the recipient seam at content level: the deliver choice must
            // require the quest active AND not-yet-delivered, and SetFact the
            // exact fact the objective polls.
            var c = LoadConvo("Hermit_Quest");
            var start = c.Nodes.FirstOrDefault(n => n.ID == "Start");
            Assert.IsNotNull(start, "Hermit_Quest needs a Start node");
            var deliver = start.Choices.FirstOrDefault(ch => ch.Text.StartsWith("[Deliver"));
            Assert.IsNotNull(deliver, "Start must offer a [Deliver...] choice");

            var preds = deliver.Predicates.Select(p => p.Key).ToList();
            Assert.Contains("IfQuestActive", preds, "deliver requires the quest active (no deliver-before-accept)");
            Assert.Contains("IfFact", preds, "deliver requires not-yet-delivered (IfFact:<:1) so it can't re-fire");

            var setFact = deliver.Actions.FirstOrDefault(a => a.Key == "SetFact");
            Assert.IsNotNull(setFact, "deliver must SetFact");
            Assert.AreEqual("hermit_message_delivered:1", setFact.Value,
                "SetFact must set the exact fact the objective polls");
        }

        [Test]
        public void StrongestInOoo_LiftChoice_StatGated_AndRewardsReputation()
        {
            // The stat-gated quest: the lift choice must require Strength 18+
            // (IfStatAtLeast) and the report must reward faction reputation
            // (ChangeFactionFeeling). Pins the design at content level.
            var q = LoadQuest("StrongestInOoo");
            Assert.IsFalse(string.IsNullOrEmpty(q.Quest.Accomplishment), "has a Q7 accomplishment deed");

            var c = LoadConvo("Strongman_Quest");
            var start = c.Nodes.FirstOrDefault(n => n.ID == "Start");
            Assert.IsNotNull(start);

            var lift = start.Choices.FirstOrDefault(ch => ch.Text.StartsWith("[Lift"));
            Assert.IsNotNull(lift, "Start must offer a [Lift...] choice");
            var stat = lift.Predicates.FirstOrDefault(p => p.Key == "IfStatAtLeast");
            Assert.IsNotNull(stat, "lift must be gated by IfStatAtLeast");
            Assert.AreEqual("Strength:18", stat.Value, "the gate is Strength 18");

            // the come-back branch uses the auto-inverse
            var weak = start.Choices.FirstOrDefault(ch => ch.Text.StartsWith("[I'm not strong"));
            Assert.IsNotNull(weak, "there must be a too-weak (come-back) branch");
            Assert.IsTrue(weak.Predicates.Any(p => p.Key == "IfNotStatAtLeast"),
                "the come-back branch uses the IfNotStatAtLeast auto-inverse (mutually exclusive with lift)");

            var report = start.Choices.FirstOrDefault(ch => ch.Text.StartsWith("[Report"));
            Assert.IsNotNull(report, "there must be a report choice");
            var rep = report.Actions.FirstOrDefault(a => a.Key == "ChangeFactionFeeling");
            Assert.IsNotNull(rep, "report must reward faction reputation");
            Assert.IsTrue(rep.Value.StartsWith("SaccharineConcord:Player:"),
                "rep reward grants the player standing with SaccharineConcord");
            Assert.IsTrue(report.Actions.Any(a => a.Key == "CompleteQuest" && a.Value == "StrongestInOoo"),
                "report completes the quest");
        }

        [Test]
        public void PoolConversations_ActionsAndPredicates_AreRegistered()
        {
            foreach (var convoId in new[] { "Crunchy_Quest", "Pilgrim_Quest", "Warren_Quest", "CandyTax_Quest", "CandyCitizen", "Baker_Quest", "Hermit_Quest", "Strongman_Quest" })
            {
                var c = LoadConvo(convoId);
                foreach (var node in c.Nodes)
                {
                    if (node.Choices == null) continue;
                    foreach (var choice in node.Choices)
                    {
                        foreach (var a in choice.Actions ?? new List<ConversationParam>())
                            Assert.IsTrue(ConversationActions.IsRegistered(a.Key),
                                $"action '{a.Key}' in {convoId} must be registered");
                        foreach (var pr in choice.Predicates ?? new List<ConversationParam>())
                            Assert.IsTrue(ConversationPredicates.IsRegistered(pr.Key),
                                $"predicate '{pr.Key}' in {convoId} must be registered");
                    }
                }
            }
        }
    }
}
