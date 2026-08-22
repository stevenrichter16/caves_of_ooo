using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.4 SM-E (Docs/FELLING-W4-PLAN.md §6.2) — the Choir ritual: the
    /// Bloom's ONLY cure, spoken at the shrines. Pure JSON on the five
    /// keeper conversations (the CureEffect action already ships) plus
    /// one tiny predicate — IfHasEffect — so the branch appears only to
    /// a Bloomed listener. Voice gates: the Choir never says "dead";
    /// the Bloom is never funny.
    /// </summary>
    public class ChoirRitualTests
    {
        private static readonly string[] KeeperConversations =
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
        public void TearDown() => ConversationLoader.Reset();

        private static Entity MakeListener(bool bloomed)
        {
            var e = new Entity { ID = "walker", BlueprintName = "Walker" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = "walker" });
            e.AddPart(new StatusEffectsPart());
            if (bloomed) e.ApplyEffect(new BloomedEffect());
            return e;
        }

        private static ChoiceData FindRitualChoice(ConversationData conv)
        {
            foreach (var choice in conv.GetStartNode().Choices)
            {
                if (choice.Actions == null) continue;
                foreach (var a in choice.Actions)
                    if (a.Key == "CureEffect" && a.Value == "Bloomed")
                        return choice;
                // The cure may live one node deep (choice → ritual node
                // whose own choice carries the action).
            }
            foreach (var node in conv.Nodes)
                foreach (var choice in node.Choices)
                {
                    if (choice.Actions == null) continue;
                    foreach (var a in choice.Actions)
                        if (a.Key == "CureEffect" && a.Value == "Bloomed")
                            return choice;
                }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        //   The predicate
        // ════════════════════════════════════════════════════════════

        [Test]
        public void IfHasEffect_IsClosed_ForTheUnbloomed()
        {
            // Pre-impl this is RED for the sharpest possible reason:
            // unknown predicate names PASS-TRUE (ConversationPredicates
            // Evaluate) — a missing IfHasEffect would show the ritual
            // to everyone.
            var listener = MakeListener(bloomed: false);
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfHasEffect", null, listener, "Bloomed"),
                "no Bloom, no ritual branch");
        }

        [Test]
        public void IfHasEffect_Opens_ForTheBloomed()
        {
            var listener = MakeListener(bloomed: true);
            Assert.IsTrue(ConversationPredicates.Evaluate(
                "IfHasEffect", null, listener, "Bloomed"));
        }

        // ════════════════════════════════════════════════════════════
        //   The content
        // ════════════════════════════════════════════════════════════

        [Test]
        public void EveryKeeper_OffersTheRitual_BehindTheGate()
        {
            foreach (var id in KeeperConversations)
            {
                var conv = ConversationLoader.Get(id);
                Assert.IsNotNull(conv, $"{id} loads");

                // The gate on the START choice: only the Bloomed see it.
                ChoiceData gated = null;
                foreach (var c in conv.GetStartNode().Choices)
                {
                    if (c.Predicates == null) continue;
                    foreach (var p in c.Predicates)
                        if (p.Key == "IfHasEffect" && p.Value == "Bloomed")
                            gated = c;
                }
                Assert.IsNotNull(gated,
                    $"{id}: the ritual entrance exists, gated on the Bloom");

                Assert.IsNotNull(FindRitualChoice(conv),
                    $"{id}: and somewhere past it, CureEffect:Bloomed is spoken");
            }
        }

        [Test]
        public void TheRitual_CuresEndToEnd()
        {
            var conv = ConversationLoader.Get("Mogu_1");
            var ritual = FindRitualChoice(conv);
            Assert.IsNotNull(ritual, "Solm speaks the ritual");

            var listener = MakeListener(bloomed: true);
            ConversationActions.ExecuteAll(ritual.Actions, null, listener);

            Assert.IsFalse(listener.HasEffect<BloomedEffect>(),
                "the Choir takes it back");
        }

        [Test]
        public void TheChoir_NeverSaysDead()
        {
            // Voice gate (§9 gate 10) over the WHOLE file — the first
            // cut scanned only the five keepers and walked past
            // ChoirTendril_1 saying it twice (close-out 🟡): the exact
            // asymmetry class the Sopp fix corrected. Everything in
            // RotChoir.json is Choir voice.
            foreach (var id in new[] { "ChoirTendril_1", "Mogu_1", "Grib_1",
                                       "Nam_1", "Sien_1", "Sopp_1" })
            {
                var conv = ConversationLoader.Get(id);
                Assert.IsNotNull(conv, $"{id} loads (widen this list when the file grows)");
                foreach (var node in conv.Nodes)
                {
                    var text = (node.Text ?? "").ToLowerInvariant();
                    StringAssert.DoesNotContain("dead", text,
                        $"{id}/{node.ID}: the Choir does not use that word");
                    StringAssert.DoesNotContain(" died", text,
                        $"{id}/{node.ID}");
                }
            }
        }

        [Test]
        public void TheRitual_LeavesARecord()
        {
            // Close-out 🟡 #6: the plan promised a fact latch and the
            // first cut silently dropped it — the cure was a service
            // with no memory. AddFact ChoirBloomCures counts them:
            // repeat cures become queryable, and future content can
            // react to a walker the Choir has taken the Bloom from
            // more than once.
            var world = new Entity { ID = "world", BlueprintName = "World" };
            var ns = new NarrativeStatePart();
            world.AddPart(ns);
            NarrativeStatePart.Current = ns;
            try
            {
                var conv = ConversationLoader.Get("Mogu_1");
                var ritual = FindRitualChoice(conv);
                var listener = MakeListener(bloomed: true);
                ConversationActions.ExecuteAll(ritual.Actions, null, listener);

                Assert.AreEqual(1, ns.GetFact("ChoirBloomCures"),
                    "the taking is counted");
            }
            finally
            {
                NarrativeStatePart.Current = null;
            }
        }
    }
}
