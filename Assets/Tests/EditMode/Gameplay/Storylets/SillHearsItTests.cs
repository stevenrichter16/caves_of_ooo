using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/FELLING-W1-W2-PLAN.md SM7 — the Sill inciting storylet. Canon:
    /// "Start: Sill hears sari... sari... for the first time... An elder
    /// goes pale at a sound from a story" (Lore/History/06_Plot.md:176,
    /// 03_History.md:184). The first non-quest storylet ever to fire —
    /// all 11 previously-shipped storylet files carry Quest.Stages, so
    /// pass 1A (StoryletPart.OnTickEnd) had never actually run on
    /// anything before this.
    /// </summary>
    public class SillHearsItTests
    {
        private NarrativeStatePart _narrativeState;
        private StoryletPart _storyletPart;

        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            ConversationPredicates.Reset();
            MessageLog.Clear();

            _narrativeState = new NarrativeStatePart();
            NarrativeStatePart.Current = _narrativeState;

            _storyletPart = new StoryletPart();
            _narrativeState.RegisterReactor(_storyletPart);

            SettlementRuntime.ActiveZone = null;
        }

        [TearDown]
        public void TearDown()
        {
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletRegistry.Reset();
            ConversationPredicates.Reset();
            MessageLog.Clear();
            SettlementRuntime.Reset();
        }

        private void Tick() => _storyletPart.OnTickEnd(_narrativeState);

        private static void LoadTheShippedStorylet()
        {
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Data/Storylets/SillHearsIt.json");
            StoryletRegistry.LoadFromJson(File.ReadAllText(path));
        }

        // ════════════════════════════════════════════════════════════
        // 1. The predicate itself
        // ════════════════════════════════════════════════════════════

        [Test]
        public void IfPlayerInZone_TrueWhenActiveZoneMatches()
        {
            SettlementRuntime.ActiveZone = new Zone("Overworld.10.10.0");
            Assert.IsTrue(ConversationPredicates.Evaluate(
                "IfPlayerInZone", null, null, "Overworld.10.10.0"));
        }

        [Test]
        public void IfPlayerInZone_FalseInADifferentZone()
        {
            SettlementRuntime.ActiveZone = new Zone("Overworld.7.8.0");
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfPlayerInZone", null, null, "Overworld.10.10.0"));
        }

        [Test]
        public void IfPlayerInZone_FalseWhenActiveZoneIsNull()
        {
            // Adversarial: pre-bootstrap / test context — must not throw.
            SettlementRuntime.ActiveZone = null;
            Assert.DoesNotThrow(() => ConversationPredicates.Evaluate(
                "IfPlayerInZone", null, null, "Overworld.10.10.0"));
            Assert.IsFalse(ConversationPredicates.Evaluate(
                "IfPlayerInZone", null, null, "Overworld.10.10.0"));
        }

        // ════════════════════════════════════════════════════════════
        // 2. The shipped storylet fires, once, only in Sill
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SillHearsIt_FiresOnceThePlayerIsInSill()
        {
            LoadTheShippedStorylet();
            SettlementRuntime.ActiveZone = new Zone(WorldMap.StartingZoneID);

            Tick();

            StringAssert.Contains("elder", MessageLog.GetLast() ?? "");
            StringAssert.Contains("water", MessageLog.GetLast() ?? "",
                "the line names the sound's source without naming the word itself — matching the register's own reluctance to say it plainly");
            Assert.AreEqual(1, _narrativeState.GetFact("sill_sari_heard"));
            Assert.IsTrue(_storyletPart.HasFired("SillHearsIt"));
        }

        [Test]
        public void SillHearsIt_DoesNotFireInAnyOtherZone()
        {
            // Counter-check: identical setup, different zone.
            LoadTheShippedStorylet();
            SettlementRuntime.ActiveZone = new Zone("Overworld.7.8.0"); // Gantry

            Tick();

            Assert.IsNull(MessageLog.GetLast());
            Assert.AreEqual(0, _narrativeState.GetFact("sill_sari_heard"));
            Assert.IsFalse(_storyletPart.HasFired("SillHearsIt"));
        }

        [Test]
        public void SillHearsIt_IsOneShot_NeverFiresTwice()
        {
            LoadTheShippedStorylet();
            SettlementRuntime.ActiveZone = new Zone(WorldMap.StartingZoneID);

            Tick();
            MessageLog.Clear();
            Tick(); // still in Sill, still passes the trigger

            Assert.IsNull(MessageLog.GetLast(), "a one-shot storylet must not fire a second time");
        }

        [Test]
        public void SillHearsIt_PlayerNeverVisitsSill_NeverFiresAndNoOrphanState()
        {
            // Adversarial: this is not a quest — never firing must leave
            // no dangling quest state, just an unset fact.
            LoadTheShippedStorylet();
            SettlementRuntime.ActiveZone = new Zone("Overworld.7.8.0");

            for (int i = 0; i < 5; i++) Tick();

            Assert.AreEqual(0, _narrativeState.GetFact("sill_sari_heard"));
            Assert.IsFalse(_storyletPart.HasFired("SillHearsIt"));
            Assert.AreEqual(0, _storyletPart.GetActiveQuests().Count);
        }

        // ════════════════════════════════════════════════════════════
        // 3. Adversarial — fact/fired-flag are two stores; must not desync
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_FactAlreadySetWithoutFiredFlag_DoesNotReFireTheMessage()
        {
            // The save-reach case: a hand-built world state (or a future
            // migration) could set the fact without the OneShot flag
            // having ever been recorded. The trigger predicate reads the
            // fact and would pass again — OneShot must still hold, because
            // it is checked BEFORE Triggers, independent of fact state.
            LoadTheShippedStorylet();
            _narrativeState.SetFact("sill_sari_heard", 1); // fact present...
            _storyletPart.MarkFired("SillHearsIt");        // ...and so is the flag, as it should be together
            SettlementRuntime.ActiveZone = new Zone(WorldMap.StartingZoneID);

            Tick();

            Assert.IsNull(MessageLog.GetLast(),
                "already-fired must win regardless of what the fact store says");
        }

        // ════════════════════════════════════════════════════════════
        // 4. The Recension scribe's reactive branch — the topic exists
        //    only after the fact is set (Lore/Factions/02_Recension.md:270:
        //    "the first NPC who can explain the sari... sari...")
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ScribeConversation_GainsTheSariTopic_OnlyAfterTheFactIsSet()
        {
            ConversationLoader.Reset();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Conversations/FriendlyNPCs.json");
            ConversationLoader.LoadFromJson(File.ReadAllText(path));

            var conv = ConversationLoader.Get("Scribe_1");
            Assert.IsNotNull(conv, "Scribe_1 should load");
            var start = conv.GetStartNode();

            ChoiceData sariChoice = null;
            foreach (var c in start.Choices)
                if (c.Target == "AboutTheSound") sariChoice = c;

            Assert.IsNotNull(sariChoice, "the new choice should exist in the data");
            Assert.IsNotNull(sariChoice.Predicates, "it must be gated, not always-on");
            Assert.AreEqual("IfFact", sariChoice.Predicates[0].Key);
            Assert.AreEqual("sill_sari_heard:>=:1", sariChoice.Predicates[0].Value);

            // Counter-check: before the fact is set, the gate fails.
            _narrativeState.SetFact("sill_sari_heard", 0);
            Assert.IsFalse(ConversationPredicates.CheckAll(
                new List<ConversationParam> { new ConversationParam
                    { Key = sariChoice.Predicates[0].Key, Value = sariChoice.Predicates[0].Value } },
                null, null));

            _narrativeState.SetFact("sill_sari_heard", 1);
            Assert.IsTrue(ConversationPredicates.CheckAll(
                new List<ConversationParam> { new ConversationParam
                    { Key = sariChoice.Predicates[0].Key, Value = sariChoice.Predicates[0].Value } },
                null, null));

            ConversationLoader.Reset();
        }
    }
}
