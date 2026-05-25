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
        public void PickVillageQuest_EveryPickIsInThePool_AndBothAreReachable()
        {
            var pool = new HashSet<string> { "CrunchyLocket", "HiddenShrine" };
            var seen = new HashSet<string>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                {
                    string q = VillagePopulationBuilder.PickVillageQuest($"Overworld.{x}.{y}.0");
                    Assert.Contains(q, pool.ToList(), "every pick must be a pool quest");
                    seen.Add(q);
                }
            Assert.AreEqual(2, seen.Count, "across many villages BOTH pool quests appear (the pick distributes)");
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
        public void PoolConversations_ActionsAndPredicates_AreRegistered()
        {
            foreach (var convoId in new[] { "Crunchy_Quest", "Pilgrim_Quest" })
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
