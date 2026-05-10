using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.1.6 — adversarial sweep across the bug-class
    /// taxonomy from <c>ADVERSARIAL_TESTING.md</c>. F.1 touches:
    /// <list type="bullet">
    ///   <item>State atomicity (bidirectional PartyLeader ↔ PartyMembers)</item>
    ///   <item>Cross-actor flows (leader + follower)</item>
    ///   <item>Save/load reach (PartyLeader + PartyMembers persisted)</item>
    ///   <item>Stacking semantics (idempotent SetPartyLeader)</item>
    /// </list>
    /// — 4+ surfaces → dedicated sweep mandatory per CLAUDE.md.
    ///
    /// <para>The inline tests in <see cref="FollowerSystemTests"/> already
    /// cover happy paths + per-invariant counter-checks. This file
    /// targets bug classes those don't reach: scale, boundary, rapid
    /// state mutation, mid-state save, deep chain probing, stale-ref
    /// behavior.</para>
    /// </summary>
    [TestFixture]
    public class FollowerSystemAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        private static Entity ActorWithBrain(string id)
        {
            var actor = new Entity { ID = id, BlueprintName = "Test" };
            actor.AddPart(new BrainPart());
            return actor;
        }

        private static BrainPart Brain(Entity actor) => actor.GetPart<BrainPart>();

        // ════════════════════════════════════════════════════════
        // Scale
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwentyFollowers_AllBidirectionallyLinked()
        {
            // Pin bidirectional integrity at scale. A buggy SetPartyLeader
            // that drops the back-roster entry on every Nth follower
            // would surface here — partial-failure mode in the mirror step.
            var leader = ActorWithBrain("L");
            var followers = new List<Entity>();
            for (int i = 0; i < 20; i++)
            {
                var f = ActorWithBrain($"f{i}");
                Brain(f).SetPartyLeader(leader);
                followers.Add(f);
            }

            Assert.AreEqual(20, Brain(leader).PartyMembers.Count,
                "All 20 followers tracked on the leader's roster.");
            foreach (var f in followers)
            {
                Assert.AreSame(leader, Brain(f).PartyLeader,
                    $"Follower {f.ID} still points at the leader.");
                Assert.IsTrue(Brain(leader).PartyMembers.Contains(f),
                    $"Leader's roster contains follower {f.ID}.");
            }
        }

        [Test]
        public void Adversarial_FollowersUnderFollowers_DeepChain_Aligned()
        {
            // Build a 30-deep follower chain. Then verify every pair is
            // party-aligned, GetFinalLeader returns the root, IsLedBy
            // walks the full chain without false negatives.
            var entities = new List<Entity>();
            for (int i = 0; i < 30; i++)
                entities.Add(ActorWithBrain($"e{i}"));
            // e[1] follows e[0], e[2] follows e[1], etc.
            for (int i = 1; i < entities.Count; i++)
                Brain(entities[i]).SetPartyLeader(entities[i - 1]);

            var root = entities[0];
            for (int i = 1; i < entities.Count; i++)
            {
                Assert.AreSame(root, Brain(entities[i]).GetFinalLeader(),
                    $"entities[{i}].GetFinalLeader walks to the root.");
                Assert.IsTrue(Brain(entities[i]).IsLedBy(root),
                    $"entities[{i}].IsLedBy(root) walks the chain.");
            }
            Assert.IsTrue(BrainPart.ArePartyAligned(entities[0], entities[29]),
                "Root and deepest descendant are party-aligned.");
        }

        // ════════════════════════════════════════════════════════
        // Boundary: distance check inclusivity
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_FollowLeaderGoal_ExactlyAtBoundary_IsFinished()
        {
            // Boundary inclusive: distance == CloseEnoughDistance →
            // satisfied. Off-by-one would either spin forever or stop
            // a step too early.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 7, 7, "l"); // Chebyshev=2

            var goal = new FollowLeaderGoal(leader) { CloseEnoughDistance = 2 };
            Brain(follower).PushGoal(goal);

            Assert.IsTrue(goal.Finished(),
                "Distance == CloseEnoughDistance is inclusive — goal "
                + "satisfied. Catches an off-by-one in the comparison.");
        }

        [Test]
        public void Adversarial_FollowLeaderGoal_OneOverBoundary_NotFinished()
        {
            // Pair to the inclusive test — distance > CloseEnoughDistance
            // is NOT satisfied (no rounding/inclusive-bound flip).
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 8, 8, "l"); // Chebyshev=3

            var goal = new FollowLeaderGoal(leader) { CloseEnoughDistance = 2 };
            Brain(follower).PushGoal(goal);

            Assert.IsFalse(goal.Finished(),
                "Distance one over CloseEnoughDistance → goal not satisfied. "
                + "Catches an inclusive-bound flip (would treat distance>=N "
                + "as satisfied when it should be ≤N).");
        }

        // ════════════════════════════════════════════════════════
        // Rapid state mutation
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RapidLeaderSwap_DoesNotLeaveOrphan()
        {
            // Adversarial: a follower swapped through 10 leaders rapidly.
            // Final state: only the LAST leader's roster has the follower;
            // all others are clean. Catches a partial-cleanup bug where
            // intermediate leaders retain stale entries.
            var follower = ActorWithBrain("f");
            var leaders = new List<Entity>();
            for (int i = 0; i < 10; i++)
            {
                var l = ActorWithBrain($"L{i}");
                leaders.Add(l);
                Brain(follower).SetPartyLeader(l);
            }

            Assert.AreSame(leaders[9], Brain(follower).PartyLeader,
                "Follower's leader is the LAST assignment.");
            Assert.IsTrue(Brain(leaders[9]).PartyMembers.Contains(follower),
                "Last leader's roster has the follower.");

            for (int i = 0; i < 9; i++)
            {
                Assert.IsFalse(Brain(leaders[i]).PartyMembers.Contains(follower),
                    $"Intermediate leader L{i}'s roster is clean — "
                    + "no orphan entry left behind.");
            }
        }

        [Test]
        public void Adversarial_ToggleNullAndBack_FinalStateConsistent()
        {
            // Set leader → clear → set leader → clear → set leader.
            // Final: leader assigned, roster has follower.
            var follower = ActorWithBrain("f");
            var leader = ActorWithBrain("L");

            Brain(follower).SetPartyLeader(leader);
            Brain(follower).SetPartyLeader(null);
            Brain(follower).SetPartyLeader(leader);
            Brain(follower).SetPartyLeader(null);
            Brain(follower).SetPartyLeader(leader);

            Assert.AreSame(leader, Brain(follower).PartyLeader);
            Assert.AreEqual(1, Brain(leader).PartyMembers.Count,
                "Leader's roster has exactly one entry (not five — idempotent).");
            Assert.IsTrue(Brain(leader).PartyMembers.Contains(follower));
        }

        // ════════════════════════════════════════════════════════
        // Mid-state save
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_MidFollowGoal_SurvivesRoundTrip()
        {
            // A follower mid-pursuit (FollowLeaderGoal on stack, partway
            // through, leader still alive). Round-trip the whole graph.
            // After load: goal still on stack, leader ref preserved,
            // bidirectional integrity intact.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 15, 15, "L");
            Brain(follower).SetPartyLeader(leader);
            var goal = new FollowLeaderGoal(leader) { Age = 7, CloseEnoughDistance = 3 };
            Brain(follower).PushGoal(goal);

            var loadedFollower = PartRoundTripHelper.RoundTripEntityViaTokenGraph(follower);
            var lb = loadedFollower.GetPart<BrainPart>();

            Assert.IsNotNull(lb.PartyLeader, "PartyLeader survives.");
            Assert.AreEqual("L", lb.PartyLeader.ID);
            Assert.AreEqual(1, lb.GoalCount, "FollowLeaderGoal still on stack.");
            var lg = lb.PeekGoal() as FollowLeaderGoal;
            Assert.IsNotNull(lg);
            Assert.AreEqual(7, lg.Age,
                "Mid-flux Age=7 survives — not reset to 0.");
            Assert.AreEqual(3, lg.CloseEnoughDistance);
            Assert.AreSame(lb.PartyLeader, lg.Leader,
                "Goal's Leader ref AND BrainPart.PartyLeader resolve to the "
                + "same loaded instance (SL.8 token identity).");
        }

        // ════════════════════════════════════════════════════════
        // Hostility guard interactions
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_LeaderlessHostiles_StillHostile()
        {
            // Pin that the F.1.4 guard does NOT make every NPC pacifist
            // by accident. Two leaderless NPCs with personal hostility →
            // still hostile.
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            Brain(a).SetPersonallyHostile(b);

            Assert.IsTrue(FactionManager.IsHostile(a, b),
                "PersonalEnemies-driven hostility unaffected by F.1.4 guard.");
        }

        [Test]
        public void Adversarial_ChainOfFollowersThroughHostileLink()
        {
            // Construct: A leads B leads C. All three party-aligned.
            // Now have A become PERSONALLY hostile to C (e.g. C betrayed).
            // PersonalEnemies takes precedence: A↔C hostile despite tie.
            // But B↔C should remain non-hostile (B has no grudge).
            var a = ActorWithBrain("a");
            var b = ActorWithBrain("b");
            var c = ActorWithBrain("c");
            Brain(b).SetPartyLeader(a);
            Brain(c).SetPartyLeader(b);

            Assert.IsFalse(FactionManager.IsHostile(b, c),
                "Setup: B and C are party-aligned, non-hostile by default.");
            Assert.IsFalse(FactionManager.IsHostile(a, c),
                "Setup: A and C are transitively party-aligned, non-hostile.");

            Brain(a).SetPersonallyHostile(c);

            Assert.IsTrue(FactionManager.IsHostile(a, c),
                "PersonalEnemies overrides the chain alignment: A is hostile to C.");
            Assert.IsFalse(FactionManager.IsHostile(b, c),
                "B retains the alignment — A's grudge isn't transitive to B.");
        }

        // ════════════════════════════════════════════════════════
        // Stale references
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_StaleLeaderRef_AfterRoundTrip_ResolvedConsistently()
        {
            // If a follower's PartyLeader is set BEFORE the leader is
            // added to a zone, then the whole graph is round-tripped,
            // the loaded follower's PartyLeader resolves to the loaded
            // leader by token identity — both are reachable, even
            // though only the follower was the round-trip "primary."
            var leader = new Entity { ID = "L", BlueprintName = "Leader" };
            leader.AddPart(new BrainPart());
            var follower = new Entity { ID = "F", BlueprintName = "Follower" };
            follower.AddPart(new BrainPart());
            Brain(follower).SetPartyLeader(leader);

            // Round-trip the FOLLOWER. Leader is reachable via the
            // PartyLeader entity ref and queued by the token graph.
            var loadedFollower = PartRoundTripHelper.RoundTripEntityViaTokenGraph(follower);
            var loadedLeader = Brain(loadedFollower).PartyLeader;

            Assert.IsNotNull(loadedLeader, "Loaded leader is non-null.");
            // The follower is in the loaded leader's roster, AND that
            // roster Entity instance is the SAME as our loaded follower
            // (one entity, one token).
            Assert.IsTrue(loadedLeader.GetPart<BrainPart>().PartyMembers.Contains(loadedFollower),
                "Loaded leader's roster contains the loaded follower — "
                + "bidirectional integrity preserved across save graph "
                + "even when the round-trip primary was the follower side.");
        }

        // ════════════════════════════════════════════════════════
        // Helpers (TakeAction probe needs a real Zone)
        // ════════════════════════════════════════════════════════

        private Entity CreateCreature(Zone zone, int x, int y, string id)
        {
            var entity = new Entity { ID = id, BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = id });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new BrainPart { CurrentZone = zone, Rng = new System.Random(42) });
            zone.AddEntity(entity, x, y);
            return entity;
        }
    }
}
