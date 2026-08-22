using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.5 (Docs/FELLING-W4-PLAN.md §3 + R3) — the encasement offer,
    /// v1. The offer lives with the named Choir keepers at the shrines:
    /// a branch that appears only when the listener is BADLY HURT (the
    /// new IfStatBelowPercent predicate), gentle, refusable, spoken
    /// ONCE (a fact latch on the entrance). Accepting is DECLINED by
    /// the Choir in v1 — "not yet; you are only a little tired" — the
    /// OFFER is the mechanic, and the decline keeps the text honest
    /// about what ships (encasement itself is W5+). R3 records this as
    /// a deliberate divergence from canon's "lie down anywhere soft"
    /// (no wilderness rest verb exists).
    /// </summary>
    public class EncasementOfferTests
    {
        private static readonly string[] Keepers =
            { "Mogu_1", "Grib_1", "Nam_1", "Sien_1", "Sopp_1" };

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/RotChoir.json")));
        }

        [TearDown]
        public void TearDown()
        {
            ConversationLoader.Reset();
            NarrativeStatePart.Current = null;
        }

        private static Entity MakeWalker(int hp, int maxHp = 30)
        {
            var e = new Entity { ID = "walker", BlueprintName = "Walker" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = maxHp };
            e.AddPart(new RenderPart { DisplayName = "walker" });
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   The predicate
        // ════════════════════════════════════════════════════════════

        [Test]
        public void IfStatBelowPercent_OpensOnlyForTheBadlyHurt()
        {
            // Pre-impl RED for the pass-true reason again: a missing
            // predicate would make the offer to everyone.
            Assert.IsTrue(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, MakeWalker(hp: 9), "Hitpoints:35"),
                "9/30 = 30% — badly hurt");
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, MakeWalker(hp: 30), "Hitpoints:35"),
                "whole bodies are not offered the wall");
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, MakeWalker(hp: 11), "Hitpoints:35"),
                "11/30 = 36.7% — a little tired is not badly hurt");
        }

        [Test]
        public void IfStatBelowPercent_FailsClosed()
        {
            var w = MakeWalker(hp: 5);
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, w, "Hitpoints"), "malformed arg");
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, w, "Hitpoints:abc"), "non-numeric");
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, w, "NoSuchStat:35"), "unknown stat");
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, null, "Hitpoints:35"), "null listener");

            // max<=0 was claimed fail-closed but never constructed.
            var zeroMax = MakeWalker(hp: 0, maxHp: 0);
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, zeroMax, "Hitpoints:35"),
                "a body with no maximum is not 'badly hurt' — it is a bug");
            var negMax = MakeWalker(hp: 1, maxHp: -5);
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfStatBelowPercent", null, negMax, "Hitpoints:35"));
        }

        // ════════════════════════════════════════════════════════════
        //   The content
        // ════════════════════════════════════════════════════════════

        private static ChoiceData FindOfferEntrance(ConversationData conv)
        {
            foreach (var c in conv.GetStartNode().Choices)
            {
                if (c.Predicates == null) continue;
                foreach (var p in c.Predicates)
                    if (p.Key == "IfStatBelowPercent")
                        return c;
            }
            return null;
        }

        [Test]
        public void EveryKeeper_MakesTheOffer_GatedAndLatched()
        {
            foreach (var id in Keepers)
            {
                var conv = ConversationLoader.Get(id);
                var entrance = FindOfferEntrance(conv);
                Assert.IsNotNull(entrance, $"{id}: the offer entrance exists");

                bool latched = false, once = false;
                foreach (var p in entrance.Predicates)
                    if (p.Key == "IfNotFact" && p.Value.StartsWith("EncasementOffered"))
                        once = true;
                if (entrance.Actions != null)
                    foreach (var a in entrance.Actions)
                        if (a.Key == "SetFact" && a.Value == "EncasementOffered:1")
                            latched = true;
                Assert.IsTrue(once, $"{id}: the offer is spoken once ever");
                Assert.IsTrue(latched, $"{id}: and speaking it sets the latch");

                var offerNode = conv.GetNode(entrance.Target);
                Assert.IsNotNull(offerNode, $"{id}: the offer is spoken");
                Assert.IsTrue(offerNode.Text.Length > 40,
                    $"{id}: gently, in the keeper's own words");
            }
        }

        [Test]
        public void Accepting_IsGentlyDeclined_NothingHappens()
        {
            // v1's honesty: the Choir says not yet. No effect, no
            // teleport, no state beyond the latch — the accept choice
            // carries NO actions, only words.
            foreach (var id in Keepers)
            {
                var conv = ConversationLoader.Get(id);
                var entrance = FindOfferEntrance(conv);
                var offerNode = conv.GetNode(entrance.Target);

                ChoiceData accept = null;
                foreach (var c in offerNode.Choices)
                    if (c.Target != "Start" && c.Target != "End") accept = c;
                Assert.IsNotNull(accept, $"{id}: yes is sayable");
                Assert.IsTrue(accept.Actions == null || accept.Actions.Count == 0,
                    $"{id}: and saying yes DOES nothing — the decline is the mechanic");

                var declined = conv.GetNode(accept.Target);
                Assert.IsNotNull(declined, $"{id}: the Choir answers");
                StringAssert.Contains("not", declined.Text.ToLowerInvariant(),
                    $"{id}: and the answer is a gentle no");
            }
        }

        [Test]
        public void TheOffer_CanBeReturnedTo_AsThePromisesSay()
        {
            // Every decline node invites a return — "Come back when the
            // seam shows, and ask again", "The offer keeps", "The offer
            // stands as long as we do". But the only entrance was gated
            // IfNotFact EncasementOffered, the fact is never cleared,
            // and no player-initiated ask existed: five keepers making
            // promises no mechanic backs (R9d). A returning walker who
            // is badly hurt can raise it themselves.
            foreach (var id in Keepers)
            {
                var conv = ConversationLoader.Get(id);
                ChoiceData ret = null;
                foreach (var c in conv.GetStartNode().Choices)
                {
                    if (c.Predicates == null) continue;
                    bool needsLatch = false, needsHurt = false;
                    foreach (var p in c.Predicates)
                    {
                        if (p.Key == "IfFact" && p.Value.StartsWith("EncasementOffered"))
                            needsLatch = true;
                        if (p.Key == "IfStatBelowPercent") needsHurt = true;
                    }
                    if (needsLatch && needsHurt) ret = c;
                }
                Assert.IsNotNull(ret,
                    id + ": a walker who was offered the wall can raise it again");
                Assert.IsNotNull(conv.GetNode(ret.Target),
                    id + ": and the keeper answers");
            }
        }

        [Test]
        public void TheLatch_ClosesTheDoor()
        {
            // Execute the entrance's actions once; the gate predicate
            // must then read closed.
            var world = new Entity { ID = "w", BlueprintName = "World" };
            var ns = new NarrativeStatePart();
            world.AddPart(ns);
            NarrativeStatePart.Current = ns;

            var conv = ConversationLoader.Get("Mogu_1");
            var entrance = FindOfferEntrance(conv);
            var hurt = MakeWalker(hp: 9);

            Assert.IsTrue(ConversationPredicates.CheckAll(
                entrance.Predicates, null, hurt), "precondition: the door is open");
            ConversationActions.ExecuteAll(entrance.Actions, null, hurt);
            Assert.IsFalse(ConversationPredicates.CheckAll(
                entrance.Predicates, null, hurt),
                "spoken once — the offer does not repeat");
        }
    }
}
