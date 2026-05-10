using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.7.5 — BrainPart Save/Load contract pin (🟡). See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.7</c>.
    ///
    /// <para>BrainPart's explicit handler (SaveSystem.cs:1413-1487) is
    /// the most field-heavy in the codebase: 14 fields + a goal-stack
    /// list + the PersonalEnemies HashSet. The verification sweep
    /// flagged 🟡 because the goal-stack save filters out
    /// <c>DelegateGoal</c> instances, then load reconstructs each
    /// loaded goal's <c>ParentHandler</c> as the previous one in the
    /// list — fragile if a DelegateGoal was ever mid-stack.</para>
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>All 14 scalar fields round-trip with non-default values
    ///         set on the source brain.</item>
    ///   <item><c>Rng</c> is RESET to a fresh <c>System.Random</c> on
    ///         load — line 1474 explicitly creates new. Pin so the
    ///         load-side reset is non-null.</item>
    ///   <item>Goal stack with 2+ goals — Type round-trips, fields
    ///         round-trip, ParentHandler chain reconstructed serially.</item>
    ///   <item>Empty PersonalEnemies HashSet round-trips with count 0.</item>
    ///   <item>Populated PersonalEnemies HashSet round-trips entity refs.</item>
    /// </list>
    /// </summary>
    public class Tier1BrainPartTests
    {
        [Test]
        public void BrainPart_DefaultBrain_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new BrainPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var brain = loaded.GetPart<BrainPart>();
            Assert.IsNotNull(brain);
            Assert.AreEqual(10, brain.SightRadius, "Default SightRadius=10.");
            Assert.IsTrue(brain.Wanders);
            Assert.IsTrue(brain.WandersRandomly);
            Assert.AreEqual(0.25f, brain.FleeThreshold, 0.0001f);
            Assert.IsFalse(brain.Passive);
            Assert.AreEqual(AIState.Idle, brain.CurrentState);
            Assert.IsFalse(brain.InConversation);
            Assert.AreEqual(-1, brain.StartingCellX);
            Assert.AreEqual(-1, brain.StartingCellY);
            Assert.IsFalse(brain.Staying);
            Assert.IsFalse(brain.ThinkOutLoud);
        }

        [Test]
        public void BrainPart_AllScalarFields_RoundTrip()
        {
            // Pin every saved scalar field with a non-default value.
            // A serializer-order regression would silently scramble them.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var brain = new BrainPart
            {
                SightRadius = 14,
                Wanders = false,
                WandersRandomly = false,
                FleeThreshold = 0.42f,
                Passive = true,
                CurrentState = AIState.Chase,
                InConversation = true,
                StartingCellX = 7,
                StartingCellY = 12,
                Staying = true,
                LastThought = "I sense a wraith.",
                ThinkOutLoud = true,
            };
            actor.AddPart(brain);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.AreEqual(14, lb.SightRadius);
            Assert.IsFalse(lb.Wanders);
            Assert.IsFalse(lb.WandersRandomly);
            Assert.AreEqual(0.42f, lb.FleeThreshold, 0.0001f);
            Assert.IsTrue(lb.Passive);
            Assert.AreEqual(AIState.Chase, lb.CurrentState);
            Assert.IsTrue(lb.InConversation);
            Assert.AreEqual(7, lb.StartingCellX);
            Assert.AreEqual(12, lb.StartingCellY);
            Assert.IsTrue(lb.Staying);
            Assert.AreEqual("I sense a wraith.", lb.LastThought);
            Assert.IsTrue(lb.ThinkOutLoud);
        }

        [Test]
        public void BrainPart_Rng_IsResetTo_FreshNonNullInstance_OnLoad()
        {
            // SaveSystem.cs:1474 explicitly does `brain.Rng = new
            // System.Random()` on load. Pin that the post-load Rng
            // is non-null so callers don't NRE. Internal sequence
            // is necessarily different from the saved one (Random
            // isn't reflectively serializable in a useful way).
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var brain = new BrainPart();
            actor.AddPart(brain);
            brain.Rng = new System.Random(42); // arbitrary seed

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.IsNotNull(lb.Rng,
                "Rng is reset to a fresh `new System.Random()` on load (line 1474). "
                + "If this regresses to null, every per-tick AI roll NREs.");
        }

        [Test]
        public void BrainPart_TargetEntityRef_RoundTrips()
        {
            // Target is an Entity ref via WriteEntityReference.
            // The token-graph helper queues the target's body for
            // full restoration.
            var actor = new Entity { ID = "hunter", BlueprintName = "Hunter" };
            var prey = new Entity { ID = "prey", BlueprintName = "Goblin" };
            var brain = new BrainPart { Target = prey };
            actor.AddPart(brain);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.IsNotNull(lb.Target);
            Assert.AreEqual("prey", lb.Target.ID);
            Assert.AreEqual("Goblin", lb.Target.BlueprintName);
        }

        [Test]
        public void BrainPart_NullTarget_RoundTripsAsNull()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new BrainPart()); // Target is null by default

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsNull(loaded.GetPart<BrainPart>().Target,
                "Counter-check: null Target round-trips as null.");
        }

        [Test]
        public void BrainPart_PersonalEnemies_HashSet_RoundTrips()
        {
            // PersonalEnemies is a HashSet<Entity>; saved as count +
            // each entity ref. Pin that all 3 enemies survive (without
            // duplication, without dropouts).
            var actor = new Entity { ID = "victim", BlueprintName = "Victim" };
            var brain = new BrainPart();
            actor.AddPart(brain);

            var foe1 = new Entity { ID = "foe1", BlueprintName = "Foe" };
            var foe2 = new Entity { ID = "foe2", BlueprintName = "Foe" };
            var foe3 = new Entity { ID = "foe3", BlueprintName = "Foe" };
            brain.PersonalEnemies.Add(foe1);
            brain.PersonalEnemies.Add(foe2);
            brain.PersonalEnemies.Add(foe3);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.AreEqual(3, lb.PersonalEnemies.Count);
            // Convert to ID set for assertion
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in lb.PersonalEnemies) ids.Add(e.ID);
            Assert.IsTrue(ids.Contains("foe1"));
            Assert.IsTrue(ids.Contains("foe2"));
            Assert.IsTrue(ids.Contains("foe3"));
        }

        // ── Goal stack — the 🟡 surface ─────────────────────────

        [Test]
        public void BrainPart_GoalStack_Empty_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new BrainPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.AreEqual(0, lb.GoalCount, "No goals on a fresh brain.");
        }

        [Test]
        public void BrainPart_GoalStack_TwoGoals_TypesAndFieldsRoundTrip()
        {
            // Two goals on the stack: WaitGoal(Duration=5) + StepGoal(DX=1, DY=-1).
            // Pin both Type identity (FormatterServices ctor-bypass works
            // for parameterized goal ctors) AND public field round-trip.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var brain = new BrainPart();
            actor.AddPart(brain);
            brain.PushGoal(new WaitGoal(duration: 5));
            brain.PushGoal(new StepGoal(dx: 1, dy: -1));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.AreEqual(2, lb.GoalCount);

            // Stack indexing: 0 = bottom (oldest); GoalCount-1 = top.
            // We pushed WaitGoal first → bottom; StepGoal second → top.
            var bottom = lb.PeekGoalAt(0);
            Assert.IsInstanceOf<WaitGoal>(bottom, "Bottom-of-stack is WaitGoal.");
            Assert.AreEqual(5, ((WaitGoal)bottom).Duration);

            var top = lb.PeekGoalAt(lb.GoalCount - 1);
            Assert.IsInstanceOf<StepGoal>(top, "Top-of-stack is StepGoal.");
            var step = (StepGoal)top;
            Assert.AreEqual(1, step.DX);
            Assert.AreEqual(-1, step.DY);
        }

        [Test]
        public void BrainPart_GoalStack_ParentChainReconstructed()
        {
            // Pin LoadBrainPart's parent-chain reconstruction
            // (line 1483): each loaded goal's ParentHandler is set
            // to the previously-loaded goal. With 3 goals A, B, C
            // pushed in that order, the reconstructed chain should be:
            //   A.ParentHandler = null
            //   B.ParentHandler = A
            //   C.ParentHandler = B
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var brain = new BrainPart();
            actor.AddPart(brain);
            brain.PushGoal(new WaitGoal(2));   // A (bottom)
            brain.PushGoal(new WaitGoal(3));   // B
            brain.PushGoal(new StepGoal(0, 1));// C (top)

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lb = loaded.GetPart<BrainPart>();
            Assert.AreEqual(3, lb.GoalCount);
            // GetGoalsSnapshot exposes the underlying list order; we
            // compare against snapshot rather than PeekGoalAt indices
            // because the parent-chain semantics rely on save-order.
            var snap = lb.GetGoalsSnapshot();
            Assert.IsNull(snap[0].ParentHandler,
                "First-loaded goal has no parent.");
            Assert.AreSame(snap[0], snap[1].ParentHandler,
                "Second goal's parent points at first (line 1483 chain).");
            Assert.AreSame(snap[1], snap[2].ParentHandler,
                "Third goal's parent points at second.");
        }

        // ── Followers F.1.3 — PartyLeader + PartyMembers round-trip ──

        [Test]
        public void BrainPart_PartyLeader_Null_RoundTripsAsNull()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new BrainPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsNull(loaded.GetPart<BrainPart>().PartyLeader,
                "Fresh brain has null PartyLeader; round-trips as null.");
        }

        [Test]
        public void BrainPart_PartyLeader_EntityRef_RoundTrips()
        {
            // F.1.3 — PartyLeader is a public Entity field, so it routes
            // through WriteEntityReference (SaveSystem.cs:1421-style).
            // Pin that the leader's body survives the token-graph flush.
            var follower = new Entity { ID = "f", BlueprintName = "Follower" };
            var leader = new Entity { ID = "l", BlueprintName = "Leader" };
            var brain = new BrainPart();
            follower.AddPart(brain);
            brain.SetPartyLeader(leader);  // uses the F.1.2 path

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(follower);
            var lb = loaded.GetPart<BrainPart>();
            Assert.IsNotNull(lb.PartyLeader);
            Assert.AreEqual("l", lb.PartyLeader.ID,
                "Leader ID round-trips via WriteEntityReference.");
            Assert.AreEqual("Leader", lb.PartyLeader.BlueprintName,
                "Leader body queued + flushed by token-graph helper.");
        }

        [Test]
        public void BrainPart_PartyMembers_EmptyRoster_RoundTripsAsEmpty()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new BrainPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(0, loaded.GetPart<BrainPart>().PartyMembers.Count,
                "Empty roster survives — NOT a null reference, NOT spurious entries.");
        }

        [Test]
        public void BrainPart_PartyMembers_MultipleEntries_AllRoundTrip()
        {
            // Three followers under one leader. Pin all three survive
            // count + entity refs + IDs.
            var leader = new Entity { ID = "L", BlueprintName = "Leader" };
            var lBrain = new BrainPart();
            leader.AddPart(lBrain);
            for (int i = 0; i < 3; i++)
            {
                var f = new Entity { ID = $"f{i}", BlueprintName = $"F{i}" };
                var fBrain = new BrainPart();
                f.AddPart(fBrain);
                fBrain.SetPartyLeader(leader);
            }
            Assert.AreEqual(3, lBrain.PartyMembers.Count,
                "Setup: leader has 3 party members pre-save.");

            var loadedLeader = PartRoundTripHelper.RoundTripEntityViaTokenGraph(leader);
            var loadedRoster = loadedLeader.GetPart<BrainPart>().PartyMembers;
            Assert.AreEqual(3, loadedRoster.Count,
                "All 3 followers' entity refs survive in the roster.");
            // Convert to ID set for assertion (HashSet order is undefined).
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var m in loadedRoster) ids.Add(m.ID);
            Assert.IsTrue(ids.Contains("f0"));
            Assert.IsTrue(ids.Contains("f1"));
            Assert.IsTrue(ids.Contains("f2"));
        }

        [Test]
        public void BrainPart_LeaderAndFollowerInSameGraph_ShareIdentity_OnLoad()
        {
            // SL.8 contract: when both leader L and follower F are in the
            // SAME save graph, after round-trip the follower's PartyLeader
            // must be the SAME loaded Entity instance as the leader (via
            // the token system's reference dedupe). AreSame, not AreEqual.
            var leader = new Entity { ID = "L", BlueprintName = "Leader" };
            var lBrain = new BrainPart();
            leader.AddPart(lBrain);

            var follower = new Entity { ID = "F", BlueprintName = "Follower" };
            var fBrain = new BrainPart();
            follower.AddPart(fBrain);
            fBrain.SetPartyLeader(leader);

            // Round-trip the LEADER (not the follower) — follower is
            // reachable via PartyMembers and queued by the token graph.
            var loadedLeader = PartRoundTripHelper.RoundTripEntityViaTokenGraph(leader);
            var loadedFollower = System.Linq.Enumerable.First(
                loadedLeader.GetPart<BrainPart>().PartyMembers);

            Assert.AreSame(loadedLeader, loadedFollower.GetPart<BrainPart>().PartyLeader,
                "REFERENCE IDENTITY: follower's PartyLeader is the SAME "
                + "loaded Entity instance as the round-trip primary leader. "
                + "Per SL.8 contract — token system enforces by construction.");
        }

        [Test]
        public void BrainPart_RoundTrip_Preserves_LeaderChainTransitively()
        {
            // A→B→C chain — all three in the save graph. After round-trip,
            // C.IsLedBy(A) should still be true (chain walk works post-load).
            var a = new Entity { ID = "A", BlueprintName = "A" };
            var b = new Entity { ID = "B", BlueprintName = "B" };
            var c = new Entity { ID = "C", BlueprintName = "C" };
            a.AddPart(new BrainPart());
            b.AddPart(new BrainPart());
            c.AddPart(new BrainPart());
            b.GetPart<BrainPart>().SetPartyLeader(a);
            c.GetPart<BrainPart>().SetPartyLeader(b);

            // Round-trip A (root). B and C are reachable via PartyMembers.
            var loadedA = PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);

            // Find loaded B and C via the chain.
            var aRoster = loadedA.GetPart<BrainPart>().PartyMembers;
            var loadedB = System.Linq.Enumerable.First(aRoster);
            var bRoster = loadedB.GetPart<BrainPart>().PartyMembers;
            var loadedC = System.Linq.Enumerable.First(bRoster);

            Assert.IsTrue(loadedC.GetPart<BrainPart>().IsLedBy(loadedA),
                "Transitive leadership survives round-trip — C.IsLedBy(A) "
                + "walks the leader chain post-load.");
            Assert.AreSame(loadedA, loadedC.GetPart<BrainPart>().GetFinalLeader(),
                "GetFinalLeader chain-walk reaches the loaded root.");
        }
    }
}
