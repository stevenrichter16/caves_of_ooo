using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.1.2 — BrainPart leader/follower scaffolding.
    /// See <c>Docs/FOLLOWERS.md §F.1.2</c>.
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs</c>:
    /// <list type="bullet">
    ///   <item><c>SetPartyLeader</c> (line 895) — bidirectional mirror,
    ///         cycle detection, Forgive step.</item>
    ///   <item><c>IsLedBy</c> (line 1438) — chain-walk for transitive
    ///         leadership.</item>
    ///   <item><c>GetFinalLeader</c> (line 1623) — top of the chain.</item>
    /// </list></para>
    ///
    /// <para><b>What's pinned here:</b></para>
    /// <list type="bullet">
    ///   <item>Bidirectional integrity — every leader-side change is
    ///         mirrored on the follower-side and vice versa.</item>
    ///   <item>Cycle detection blocks A↔B and longer loops.</item>
    ///   <item>Self-reference rejected.</item>
    ///   <item>Forgive: new leader removed from follower's
    ///         <see cref="BrainPart.PersonalEnemies"/>.</item>
    ///   <item>Idempotence: re-setting same leader doesn't duplicate
    ///         entry in their <see cref="BrainPart.PartyMembers"/>.</item>
    /// </list>
    /// </summary>
    public class FollowerSystemTests
    {
        // ── Fixture helpers ──────────────────────────────────────

        private static Entity ActorWithBrain(string id)
        {
            var actor = new Entity { ID = id, BlueprintName = "Test" };
            actor.AddPart(new BrainPart());
            return actor;
        }

        private static BrainPart Brain(Entity actor) => actor.GetPart<BrainPart>();

        // ── SetPartyLeader: assigning a leader ───────────────────

        [Test]
        public void SetPartyLeader_FreshBrain_AssignsLeader()
        {
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");

            bool ok = Brain(follower).SetPartyLeader(leader);

            Assert.IsTrue(ok, "Valid SetPartyLeader returns true.");
            Assert.AreSame(leader, Brain(follower).PartyLeader,
                "Leader field references the assigned entity.");
        }

        [Test]
        public void SetPartyLeader_AlsoAdds_ToLeadersPartyMembers()
        {
            // Bidirectional integrity — the core invariant.
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");

            Brain(follower).SetPartyLeader(leader);

            Assert.IsTrue(Brain(leader).PartyMembers.Contains(follower),
                "Leader's PartyMembers must contain the follower after the "
                + "set. Without this bidirectional mirror, save/load loses "
                + "the roster.");
        }

        // ── Rejection cases ──────────────────────────────────────

        [Test]
        public void SetPartyLeader_Self_Rejected_ReturnsFalse()
        {
            var actor = ActorWithBrain("a");

            bool ok = Brain(actor).SetPartyLeader(actor);

            Assert.IsFalse(ok, "Self-reference is rejected — returns false.");
            Assert.IsNull(Brain(actor).PartyLeader,
                "Leader field unchanged after rejected self-assign.");
            Assert.IsFalse(Brain(actor).PartyMembers.Contains(actor),
                "PartyMembers must not contain self after rejection.");
        }

        [Test]
        public void SetPartyLeader_DirectCycle_Rejected()
        {
            // A→B exists. Try B.SetPartyLeader(A) → would create a 2-cycle.
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            Brain(a).SetPartyLeader(b);  // A follows B

            bool ok = Brain(b).SetPartyLeader(a);  // B tries to follow A

            Assert.IsFalse(ok,
                "Direct A↔B cycle blocked — SetPartyLeader returns false.");
            Assert.IsNull(Brain(b).PartyLeader,
                "B's leader unchanged after cycle rejection.");
            Assert.AreSame(b, Brain(a).PartyLeader,
                "A's leader (B) unchanged — rejection didn't corrupt other state.");
        }

        [Test]
        public void SetPartyLeader_DeepCycle_Rejected()
        {
            // A→B→C exists. Try A.SetPartyLeader(C) → 3-cycle (C is downstream).
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            var c = ActorWithBrain("c");
            Brain(c).SetPartyLeader(b);
            Brain(b).SetPartyLeader(a);
            // Now: A is root, B follows A, C follows B.

            bool ok = Brain(a).SetPartyLeader(c);  // Root tries to follow descendant

            Assert.IsFalse(ok,
                "Deep cycle (A→B→C, then A→C) is blocked.");
            Assert.IsNull(Brain(a).PartyLeader,
                "A's leader unchanged after rejection.");
        }

        // ── Null leader ──────────────────────────────────────────

        [Test]
        public void SetPartyLeader_Null_OnFreshBrain_NoOp_ReturnsTrue()
        {
            // Setting null leader when already null is idempotent.
            var actor = ActorWithBrain("a");

            bool ok = Brain(actor).SetPartyLeader(null);

            Assert.IsTrue(ok, "Null-to-null is a valid no-op.");
            Assert.IsNull(Brain(actor).PartyLeader);
        }

        [Test]
        public void SetPartyLeader_Null_ClearsExistingLeader()
        {
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPartyLeader(leader);

            bool ok = Brain(follower).SetPartyLeader(null);

            Assert.IsTrue(ok);
            Assert.IsNull(Brain(follower).PartyLeader,
                "Leader cleared.");
            Assert.IsFalse(Brain(leader).PartyMembers.Contains(follower),
                "Follower removed from old leader's roster — bidirectional.");
        }

        // ── Changing leader ──────────────────────────────────────

        [Test]
        public void SetPartyLeader_ChangingLeader_RemovesFromOld_AddsToNew()
        {
            var follower = ActorWithBrain("f");
            var leaderA = ActorWithBrain("la");
            var leaderB = ActorWithBrain("lb");
            Brain(follower).SetPartyLeader(leaderA);
            Brain(follower).SetPartyLeader(leaderB);

            Assert.AreSame(leaderB, Brain(follower).PartyLeader);
            Assert.IsFalse(Brain(leaderA).PartyMembers.Contains(follower),
                "Removed from old leader's roster.");
            Assert.IsTrue(Brain(leaderB).PartyMembers.Contains(follower),
                "Added to new leader's roster.");
        }

        // ── Idempotence ──────────────────────────────────────────

        [Test]
        public void SetPartyLeader_SameLeaderTwice_NoDuplicate()
        {
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPartyLeader(leader);
            Brain(follower).SetPartyLeader(leader);

            Assert.AreEqual(1, Brain(leader).PartyMembers.Count,
                "Idempotent: setting the same leader twice produces ONE "
                + "entry, not two. HashSet semantics enforce this; if we "
                + "ever switch to List, the SetPartyLeader logic must "
                + "dedupe manually.");
        }

        // ── Forgive: new leader cleared from follower's PersonalEnemies ──

        [Test]
        public void SetPartyLeader_Forgives_RemovesNewLeader_FromPersonalEnemies()
        {
            // Qud parity: Brain.cs:924-932 — when X becomes leader of Y,
            // Y clears X from LastDamagedBy / InflamedBy. CoO's analog is
            // BrainPart.PersonalEnemies. Without Forgive, a creature
            // recruited mid-combat would IMMEDIATELY re-aggro its leader.
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPersonallyHostile(leader);
            Assert.IsTrue(Brain(follower).IsPersonallyHostileTo(leader),
                "Setup: follower is personally hostile to leader.");

            Brain(follower).SetPartyLeader(leader);

            Assert.IsFalse(Brain(follower).IsPersonallyHostileTo(leader),
                "Forgive: new leader removed from follower's PersonalEnemies. "
                + "Otherwise a freshly-recruited creature would attack its "
                + "leader on the next tick.");
        }

        // ── IsLedBy ──────────────────────────────────────────────

        [Test]
        public void IsLedBy_DirectLeader_True()
        {
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPartyLeader(leader);

            Assert.IsTrue(Brain(follower).IsLedBy(leader));
        }

        [Test]
        public void IsLedBy_TransitiveLeader_True()
        {
            // A→B→C : C is led by both B and A (transitively).
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            var c = ActorWithBrain("c");
            Brain(b).SetPartyLeader(a);
            Brain(c).SetPartyLeader(b);

            Assert.IsTrue(Brain(c).IsLedBy(b),
                "C is led by its direct leader B.");
            Assert.IsTrue(Brain(c).IsLedBy(a),
                "C is transitively led by A — chain walk works.");
        }

        [Test]
        public void IsLedBy_NotLed_False()
        {
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");

            Assert.IsFalse(Brain(a).IsLedBy(b),
                "Counter-check: A has no leader, so isn't led by anyone.");
        }

        [Test]
        public void IsLedBy_Null_False()
        {
            var a = ActorWithBrain("a");
            Assert.IsFalse(Brain(a).IsLedBy(null),
                "Null-safety: IsLedBy(null) returns false, never NREs.");
        }

        [Test]
        public void IsLedBy_LeaderDoesNotLeadFollower()
        {
            // Counter-check the direction: A→B; A.IsLedBy(B) is FALSE
            // (A is the FOLLOWER of B, not the other way around). Catches
            // a bug where IsLedBy walks PartyMembers instead of PartyLeader.
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPartyLeader(leader);

            Assert.IsFalse(Brain(leader).IsLedBy(follower),
                "Direction matters: the leader is NOT led by their follower.");
        }

        // ── GetFinalLeader ───────────────────────────────────────

        [Test]
        public void GetFinalLeader_DirectLeader_ReturnsLeader()
        {
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPartyLeader(leader);

            Assert.AreSame(leader, Brain(follower).GetFinalLeader());
        }

        [Test]
        public void GetFinalLeader_TransitiveChain_ReturnsTopOfChain()
        {
            // A→B→C→D : D's final leader is A.
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            var c = ActorWithBrain("c");
            var d = ActorWithBrain("d");
            Brain(b).SetPartyLeader(a);
            Brain(c).SetPartyLeader(b);
            Brain(d).SetPartyLeader(c);

            Assert.AreSame(a, Brain(d).GetFinalLeader(),
                "GetFinalLeader walks all the way to the root.");
            Assert.AreSame(a, Brain(c).GetFinalLeader());
            Assert.AreSame(a, Brain(b).GetFinalLeader());
            Assert.IsNull(Brain(a).GetFinalLeader(),
                "Root entity (no leader) returns null.");
        }

        [Test]
        public void GetFinalLeader_NoLeader_ReturnsNull()
        {
            var a = ActorWithBrain("a");
            Assert.IsNull(Brain(a).GetFinalLeader());
        }

        // ── Adversarial ──────────────────────────────────────────

        [Test]
        public void Adversarial_OneLeader_TwoFollowers_BothInRoster()
        {
            // Multi-follower: pin that one leader can have multiple
            // followers, all tracked in PartyMembers.
            var leader = ActorWithBrain("l");
            var f1 = ActorWithBrain("f1");
            var f2 = ActorWithBrain("f2");
            Brain(f1).SetPartyLeader(leader);
            Brain(f2).SetPartyLeader(leader);

            Assert.AreEqual(2, Brain(leader).PartyMembers.Count);
            Assert.IsTrue(Brain(leader).PartyMembers.Contains(f1));
            Assert.IsTrue(Brain(leader).PartyMembers.Contains(f2));
        }

        [Test]
        public void Adversarial_BrainOnLeaderMissing_HandledGracefully()
        {
            // Edge: follower tries to follow an Entity without a BrainPart.
            // Qud logs a warning + still sets LeaderReference (Brain.cs:917-920).
            // CoO contract: allow it (the leader pointer is set, the roster
            // mirror is skipped because there's no Brain to update).
            var follower = ActorWithBrain("f");
            var leaderWithoutBrain = new Entity { ID = "lb", BlueprintName = "NoBrain" };

            bool ok = Brain(follower).SetPartyLeader(leaderWithoutBrain);

            Assert.IsTrue(ok,
                "Following a brainless leader is permitted (Qud parity).");
            Assert.AreSame(leaderWithoutBrain, Brain(follower).PartyLeader,
                "Leader pointer set even without a back-roster.");
            // No assertion on roster — there's no BrainPart on leaderWithoutBrain.
        }

        // ── F.1.4 — ArePartyAligned + FactionManager.GetFeeling guard ──

        [Test]
        public void ArePartyAligned_NoLeaders_ReturnsFalse()
        {
            // Counter-check: two unrelated entities are NOT party-aligned.
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");

            Assert.IsFalse(BrainPart.ArePartyAligned(a, b),
                "Two unrelated entities with no leader/follower link are not aligned.");
        }

        [Test]
        public void ArePartyAligned_DirectFollower_True()
        {
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("l");
            Brain(follower).SetPartyLeader(leader);

            Assert.IsTrue(BrainPart.ArePartyAligned(follower, leader),
                "Follower aligned with their direct leader.");
            Assert.IsTrue(BrainPart.ArePartyAligned(leader, follower),
                "Bidirectional: leader aligned with their follower.");
        }

        [Test]
        public void ArePartyAligned_TransitiveLeader_True()
        {
            // A→B→C: A aligned with C (and vice versa) via transitive chain.
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            var c = ActorWithBrain("c");
            Brain(b).SetPartyLeader(a);
            Brain(c).SetPartyLeader(b);

            Assert.IsTrue(BrainPart.ArePartyAligned(a, c),
                "Root A aligned with deep follower C.");
            Assert.IsTrue(BrainPart.ArePartyAligned(c, a),
                "Bidirectional.");
        }

        [Test]
        public void ArePartyAligned_SiblingFollowers_True()
        {
            // Critical case: A and B both follow C. Neither leads the other,
            // but they share a common ancestor → aligned (siblings in party).
            var c = ActorWithBrain("c");  // root leader
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            Brain(a).SetPartyLeader(c);
            Brain(b).SetPartyLeader(c);

            Assert.IsTrue(BrainPart.ArePartyAligned(a, b),
                "Sibling followers under the same leader are party-aligned.");
            Assert.IsTrue(BrainPart.ArePartyAligned(b, a),
                "Bidirectional.");
        }

        [Test]
        public void ArePartyAligned_DeepSiblings_True()
        {
            // Deep tree: C→A and C→B→D. Are A and D aligned?
            // A.GetFinalLeader() = C
            // D.GetFinalLeader() = C
            // Both share C → aligned.
            var c = ActorWithBrain("c");
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            var d = ActorWithBrain("d");
            Brain(a).SetPartyLeader(c);
            Brain(b).SetPartyLeader(c);
            Brain(d).SetPartyLeader(b);

            Assert.IsTrue(BrainPart.ArePartyAligned(a, d),
                "A (root.child) and D (root.child.child) share the root → aligned.");
        }

        [Test]
        public void ArePartyAligned_Null_Safe()
        {
            var a = ActorWithBrain("a");
            Assert.IsFalse(BrainPart.ArePartyAligned(null, a));
            Assert.IsFalse(BrainPart.ArePartyAligned(a, null));
            Assert.IsFalse(BrainPart.ArePartyAligned(null, null));
        }

        [Test]
        public void ArePartyAligned_NoBrain_Safe()
        {
            // Adversarial: one entity has a Brain, the other doesn't.
            // Neither walking IsLedBy nor GetFinalLeader can find a link,
            // so → false. Mustn't NRE.
            var withBrain = ActorWithBrain("a");
            var withoutBrain = new Entity { ID = "b", BlueprintName = "NoBrain" };

            Assert.IsFalse(BrainPart.ArePartyAligned(withBrain, withoutBrain));
            Assert.IsFalse(BrainPart.ArePartyAligned(withoutBrain, withBrain));
        }

        [Test]
        public void ArePartyAligned_SelfReference_Handled()
        {
            // ArePartyAligned(x, x) — x is trivially "aligned with self".
            // FactionManager.GetFeeling already short-circuits to 100 for
            // self at line 169, but the helper should also report true so
            // it composes cleanly elsewhere.
            var a = ActorWithBrain("a");
            Assert.IsTrue(BrainPart.ArePartyAligned(a, a),
                "An entity is trivially aligned with itself.");
        }

        [Test]
        public void ArePartyAligned_UnrelatedEntities_FactionFeelingDeterminesNormally()
        {
            // Adversarial counter-check: ArePartyAligned must NOT spuriously
            // ally unrelated entities. A buggy GetFinalLeader that returns
            // some default (e.g. the same null reference) would falsely
            // align everyone. Pin: two entities with no leader return
            // null final-leader BOTH — but we should NOT treat that as
            // "shared".
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");

            Assert.IsFalse(BrainPart.ArePartyAligned(a, b),
                "Two leader-less entities are NOT aligned (both having null "
                + "GetFinalLeader doesn't count as a 'shared' ancestor). "
                + "Otherwise every NPC would be allied with every other NPC.");
        }

        // ── FactionManager.GetFeeling integration ──

        [Test]
        public void FactionManager_GetFeeling_PartyAligned_OverridesFactionHostility()
        {
            // The integration test: even when faction-feeling would normally
            // be hostile, party alignment makes the feeling non-hostile.
            //
            // Setup: create two NPCs with no faction (faction feeling = 0,
            // not hostile by default). Make them party-aligned. Confirm
            // FactionManager.GetFeeling reports allied-positive.
            var leader = ActorWithBrain("L");
            var follower = ActorWithBrain("F");
            Brain(follower).SetPartyLeader(leader);

            int feeling = FactionManager.GetFeeling(follower, leader);
            Assert.Greater(feeling, 0,
                "Party-aligned entities get a positive feeling — explicitly "
                + "above the HOSTILE_THRESHOLD so AIHelpers.IsValidHostileTarget "
                + "won't target them.");

            // Bidirectional
            int feelingBack = FactionManager.GetFeeling(leader, follower);
            Assert.Greater(feelingBack, 0);
        }

        [Test]
        public void FactionManager_GetFeeling_PersonalHostility_OverridesPartyAlignment()
        {
            // Vendetta case: a follower whose leader directly attacked them
            // is BOTH a follower AND personally hostile. PersonalEnemies takes
            // precedence — they actively want to break the alliance.
            //
            // Note: this is unusual because SetPartyLeader Forgives on
            // accept (clears PersonalEnemies of the leader). The only way
            // to reach this state is to re-add the leader AFTER recruitment.
            var leader = ActorWithBrain("L");
            var follower = ActorWithBrain("F");
            Brain(follower).SetPartyLeader(leader);

            // Manual re-aggro after recruitment
            Brain(follower).SetPersonallyHostile(leader);

            int feeling = FactionManager.GetFeeling(follower, leader);
            Assert.LessOrEqual(feeling, -10,
                "PersonalEnemies overrides PartyLeader alignment. A follower "
                + "who has been personally aggrieved retains hostility.");
        }

        [Test]
        public void FactionManager_GetFeeling_NotPartyAligned_FollowsExistingLogic()
        {
            // Counter-check: two entities with no leader/follower link
            // get the SAME GetFeeling result they would have without the
            // F.1.4 guard. The party-aligned exception MUST NOT affect
            // unrelated entities.
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");

            int feeling = FactionManager.GetFeeling(a, b);
            Assert.AreEqual(0, feeling,
                "No faction, no personal hostility, no party alignment → "
                + "feeling=0 (the existing 'no info' default). Pin that the "
                + "F.1.4 guard didn't accidentally change the default-feeling "
                + "of unrelated entities.");
        }
    }
}
