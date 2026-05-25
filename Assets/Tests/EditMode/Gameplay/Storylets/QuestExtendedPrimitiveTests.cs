using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The three Tier-3 quest primitives from QUEST-DESIGN-CATALOG.md:
    /// <list type="bullet">
    /// <item><b>AddFactWhenSlain</b> — kill-N / collect-N counters (increment-on-death).</item>
    /// <item><b>QuestMarkerTriggerPart</b> — reach-a-location (SetFact when the player steps on it).</item>
    /// <item><b>IfStageAgeAtMost</b> — timed gating (stage no older than N turns).</item>
    /// </list>
    /// </summary>
    public class QuestExtendedPrimitiveTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            Diag.ResetAll();
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        private static void FireDied(Entity e)
        {
            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)e);
            e.FireEventAndRelease(died);
        }

        // ════════════════ AddFactWhenSlain (counters) ════════════════

        [Test]
        public void AddFactWhenSlain_Slain_IncrementsFact()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var mob = new Entity { ID = "rat" };
            mob.AddPart(new AddFactWhenSlain { Fact = "rats_killed" });
            FireDied(mob);
            Assert.AreEqual(1, ns.GetFact("rats_killed"));
        }

        [Test]
        public void AddFactWhenSlain_ThreeKills_FactReachesThree()
        {
            // The kill-N proof: three mobs sharing one fact → counter == 3.
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            for (int i = 0; i < 3; i++)
            {
                var mob = new Entity { ID = "rat" + i };
                mob.AddPart(new AddFactWhenSlain { Fact = "rats_killed" });
                FireDied(mob);
            }
            Assert.AreEqual(3, ns.GetFact("rats_killed"),
                "each kill increments → a kill-N objective gates on IfFact:rats_killed:>=:3");
        }

        [Test]
        public void AddFactWhenSlain_CustomAmount()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var mob = new Entity { ID = "boss" };
            mob.AddPart(new AddFactWhenSlain { Fact = "threat", Amount = 5 });
            FireDied(mob);
            Assert.AreEqual(5, ns.GetFact("threat"));
        }

        [Test]
        public void AddFactWhenSlain_NonDied_Ignored_AndEmptyFact_NoThrow()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var mob = new Entity { ID = "m" };
            mob.AddPart(new AddFactWhenSlain { Fact = "k" });
            mob.FireEventAndRelease(GameEvent.New("BeforeTakeDamage"));
            Assert.AreEqual(0, ns.GetFact("k"), "non-Died increments nothing");

            var e2 = new Entity { ID = "m2" };
            e2.AddPart(new AddFactWhenSlain { Fact = "" });
            Assert.DoesNotThrow(() => FireDied(e2));
        }

        [Test]
        public void AddFactWhenSlain_SaveLoad_RoundTrips()
        {
            var e = new Entity { ID = "m", BlueprintName = "Rat" };
            e.AddPart(new AddFactWhenSlain { Fact = "rats_killed", Amount = 2 });
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            var p = loaded.GetPart<AddFactWhenSlain>();
            Assert.IsNotNull(p);
            Assert.AreEqual("rats_killed", p.Fact);
            Assert.AreEqual(2, p.Amount);
        }

        // ════════════════ QuestMarkerTriggerPart (reach-location) ════════════════

        private static Entity MakeMarker(string fact)
        {
            var m = new Entity { ID = "marker", BlueprintName = "QuestMarker" };
            m.AddPart(new QuestMarkerTriggerPart { Fact = fact });
            return m;
        }

        private static void FireStep(Entity marker, Entity actor, Zone zone)
        {
            var e = GameEvent.New("EntityEnteredCell");
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Cell", (object)zone.GetEntityCell(marker));
            marker.FireEventAndRelease(e);
        }

        [Test]
        public void QuestMarker_PlayerSteps_SetsFact()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var player = new Entity { ID = "player" };
            StoryletPart.LocalPlayer = player;
            var zone = new Zone();
            var marker = MakeMarker("reached_clearing");
            zone.AddEntity(marker, 5, 5);

            FireStep(marker, player, zone);
            Assert.AreEqual(1, ns.GetFact("reached_clearing"),
                "the player stepping on the marker sets the fact (→ IfFact objective)");
        }

        [Test]
        public void QuestMarker_NonPlayerSteps_DoesNotSetFact_AndPersists()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var player = new Entity { ID = "player" };
            StoryletPart.LocalPlayer = player;
            var zone = new Zone();
            var marker = MakeMarker("reached_clearing");
            zone.AddEntity(marker, 5, 5);

            var npc = new Entity { ID = "wanderer" };
            FireStep(marker, npc, zone);
            Assert.AreEqual(0, ns.GetFact("reached_clearing"),
                "an NPC wandering over the marker must NOT satisfy the player's objective");
            Assert.IsNotNull(zone.GetEntityCell(marker),
                "and must NOT consume the marker (ConsumeOnTrigger=false) — the player can still reach it");
        }

        [Test]
        public void QuestMarker_SaveLoad_RoundTrips()
        {
            var m = new Entity { ID = "marker", BlueprintName = "QuestMarker" };
            m.AddPart(new QuestMarkerTriggerPart { Fact = "reached_clearing", Value = 1 });
            var loaded = PartRoundTripHelper.RoundTripEntity(m);
            var p = loaded.GetPart<QuestMarkerTriggerPart>();
            Assert.IsNotNull(p);
            Assert.AreEqual("reached_clearing", p.Fact);
        }

        // ════════════════ IfStageAgeAtMost (timed) ════════════════

        private static StoryletPart QuestOnStageEnteredAt(int enteredTurn)
        {
            var sd = new StoryletData { ID = "Q", Quest = new QuestData() };
            sd.Quest.Stages.Add(new QuestStageData { ID = "s0" });
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            StoryletPart.Current = part;
            part.StartQuest(new QuestState { QuestId = "Q", CurrentStageIndex = 0 });
            part.GetQuestState("Q").EnteredStageAtTurn = enteredTurn; // simulate elapsed time vs now=0
            return part;
        }

        [Test]
        public void IfStageAgeAtMost_WithinWindow_True()
        {
            _ = new TurnManager(); // now = TickCount = 0
            QuestOnStageEnteredAt(0); // age 0
            Assert.IsTrue(ConversationPredicates.Evaluate("IfStageAgeAtMost", null, null, "Q:5"));
        }

        [Test]
        public void IfStageAgeAtMost_AtBoundary_True()
        {
            _ = new TurnManager();
            QuestOnStageEnteredAt(-5); // age 5
            Assert.IsTrue(ConversationPredicates.Evaluate("IfStageAgeAtMost", null, null, "Q:5"),
                "the boundary is inclusive (age == max → still within)");
        }

        [Test]
        public void IfStageAgeAtMost_BeyondWindow_False()
        {
            _ = new TurnManager();
            QuestOnStageEnteredAt(-10); // age 10 > 5
            Assert.IsFalse(ConversationPredicates.Evaluate("IfStageAgeAtMost", null, null, "Q:5"),
                "past the deadline → false, so the timed objective can no longer auto-complete");
        }

        [Test]
        public void IfStageAgeAtMost_NoQuest_False()
        {
            _ = new TurnManager();
            StoryletPart.Current = new StoryletPart(); // nothing started
            Assert.IsFalse(ConversationPredicates.Evaluate("IfStageAgeAtMost", null, null, "Ghost:5"));
        }
    }
}
