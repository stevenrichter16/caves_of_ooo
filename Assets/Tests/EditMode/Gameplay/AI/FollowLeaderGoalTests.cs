using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.1.5 — FollowLeaderGoal: AI goal that moves a party
    /// member toward their leader. See <c>Docs/FOLLOWERS.md §F.1.5</c>.
    ///
    /// <para><b>Qud parity:</b> Qud doesn't have a single named
    /// FollowLeaderGoal — leadership is one signal among many feeding
    /// the standard Wander/Pathfind goals. CoO factors the follow
    /// behavior into a dedicated <see cref="FollowLeaderGoal"/> for
    /// clarity, with the same observable result: when a follower's
    /// leader is more than <c>CloseEnoughDistance</c> cells away, the
    /// follower steps toward them.</para>
    ///
    /// <para><b>Termination cases pinned here:</b></para>
    /// <list type="bullet">
    ///   <item>Leader null → Finished()</item>
    ///   <item>Leader has no BrainPart (no zone to compare) → Finished()</item>
    ///   <item>Leader's CurrentZone is null (unregistered/destroyed) → Finished()</item>
    ///   <item>Leader in a different (registered) zone → NOT Finished —
    ///         persistent across cross-zone (post-F.2.7 audit fix).
    ///         TakeAction idles + resets Age until zones realign.</item>
    ///   <item>MaxAge exceeded → Finished() (defensive timeout —
    ///         only accumulates when leader is reachable but the follower
    ///         is failing to close distance; cross-zone idle resets Age)</item>
    ///   <item>Close enough to leader → NOT Finished — follow is
    ///         persistent (F.2.6 fix). TakeAction idles and resets Age
    ///         while close, but the goal stays on the stack so the
    ///         follower re-pursues when the leader walks away.</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class FollowLeaderGoalTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────

        private Entity CreateCreature(Zone zone, int x, int y, string id = null)
        {
            var entity = new Entity { ID = id ?? $"e_{x}_{y}", BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "test" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(42) });
            zone.AddEntity(entity, x, y);
            return entity;
        }

        private static FollowLeaderGoal PushGoal(Entity follower, Entity leader)
        {
            var brain = follower.GetPart<BrainPart>();
            var goal = new FollowLeaderGoal(leader);
            brain.PushGoal(goal);
            return goal;
        }

        // ── Construction ─────────────────────────────────────────

        [Test]
        public void Construction_StoresLeader()
        {
            var leader = new Entity { ID = "L", BlueprintName = "Leader" };
            var goal = new FollowLeaderGoal(leader);
            Assert.AreSame(leader, goal.Leader);
        }

        [Test]
        public void Construction_HasSaneDefaults()
        {
            var goal = new FollowLeaderGoal(null);
            Assert.Greater(goal.CloseEnoughDistance, 0,
                "Default CloseEnoughDistance must be positive — a 0 would "
                + "mean the follower must occupy the leader's exact cell, "
                + "which is impossible while leader is alive.");
            Assert.Greater(goal.MaxAgeBeforeGiveUp, 0,
                "Default MaxAgeBeforeGiveUp must be positive (defensive timeout).");
        }

        // ── Finished(): termination cases ────────────────────────

        [Test]
        public void Finished_NullLeader_True()
        {
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var goal = PushGoal(follower, null);

            Assert.IsTrue(goal.Finished(),
                "Null leader → no one to follow → goal is finished.");
        }

        [Test]
        public void Finished_LeaderWithoutBrainPart_True()
        {
            // Edge: leader is an entity but has no BrainPart (no zone
            // info reachable). Goal can't meaningfully follow.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var brainlessLeader = new Entity { ID = "lb", BlueprintName = "NoBrain" };

            var goal = PushGoal(follower, brainlessLeader);

            Assert.IsTrue(goal.Finished(),
                "Brainless leader → can't determine zone → goal finishes "
                + "gracefully rather than NRE.");
        }

        [Test]
        public void Finished_LeaderInDifferentZone_False_PersistentAcrossZones()
        {
            // Post-F.2.7 audit fix: cross-zone is persistent. If the
            // leader is in a different zone (e.g. F.2.7 transit failed
            // to place the follower in the new zone), the goal stays
            // on the stack and resumes pursuit when zones realign.
            // Earlier F.1.5 design popped on cross-zone, which left
            // stranded followers unable to re-pursue after the player
            // returned to their zone.
            var zoneA = new Zone("A");
            var zoneB = new Zone("B");
            var follower = CreateCreature(zoneA, 5, 5, "f");
            var leader = CreateCreature(zoneB, 10, 10, "l");

            var goal = PushGoal(follower, leader);

            Assert.IsFalse(goal.Finished(),
                "Persistent-cross-zone contract: leader in a different "
                + "registered zone → goal stays active. TakeAction idles "
                + "+ resets Age until zones realign; truly-unreachable "
                + "leaders still time out via MaxAgeBeforeGiveUp.");
        }

        [Test]
        public void TakeAction_LeaderInDifferentZone_ResetsAge_NoMove()
        {
            // Counter-pair: when cross-zone, TakeAction idles (doesn't
            // move the follower) AND resets Age (preserves MaxAge for
            // genuinely-unreachable cases only).
            var zoneA = new Zone("A");
            var zoneB = new Zone("B");
            var follower = CreateCreature(zoneA, 5, 5, "f");
            var leader = CreateCreature(zoneB, 10, 10, "l");

            var goal = PushGoal(follower, leader);
            goal.Age = 75; // simulate having been separated for a while
            var posBefore = zoneA.GetEntityPosition(follower);

            goal.TakeAction();

            Assert.AreEqual(0, goal.Age,
                "TakeAction while cross-zone resets Age — separated " +
                "followers don't accumulate timeout pressure.");
            var posAfter = zoneA.GetEntityPosition(follower);
            Assert.AreEqual((posBefore.x, posBefore.y), (posAfter.x, posAfter.y),
                "TakeAction while cross-zone is idle — no movement.");
        }

        [Test]
        public void Finished_LeaderZoneNull_True()
        {
            // Edge: leader has a BrainPart but its CurrentZone is null
            // (e.g. mid-transition, or never placed in a zone).
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = new Entity { ID = "l", BlueprintName = "Leader" };
            leader.AddPart(new BrainPart()); // CurrentZone is null
            // NOT added to a zone.

            var goal = PushGoal(follower, leader);

            Assert.IsTrue(goal.Finished(),
                "Leader's CurrentZone is null → goal can't compute a target "
                + "cell → finishes gracefully.");
        }

        [Test]
        public void Finished_MaxAgeExceeded_True()
        {
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 20, 20, "l");

            var goal = PushGoal(follower, leader);
            goal.Age = goal.MaxAgeBeforeGiveUp + 1;

            Assert.IsTrue(goal.Finished(),
                "Defensive timeout: if Age exceeds MaxAgeBeforeGiveUp, the "
                + "goal gives up. Prevents pathological 'unreachable leader' "
                + "states from spinning forever.");
        }

        [Test]
        public void Finished_CloseEnough_False_PersistentFollow()
        {
            // F.2.6 fix: follow is persistent — when close-enough, the
            // goal idles but does NOT finish. This is the contract that
            // makes "recruit while adjacent, then walk away → follower
            // pursues" actually work. The original F.1.5 design
            // finished on close-enough and broke at recruit time
            // because the recruiter is adjacent by definition.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 6, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;

            Assert.IsFalse(goal.Finished(),
                "Persistent-follow contract: even though follower is at "
                + "Chebyshev distance 1 ≤ CloseEnoughDistance=2, the goal "
                + "does NOT finish — it stays on the stack so the follower "
                + "re-pursues when the leader moves away.");
        }

        [Test]
        public void TakeAction_CloseEnough_ResetsAge()
        {
            // F.2.6 contract: when close, TakeAction resets Age to 0
            // so the MaxAgeBeforeGiveUp timeout only fires for
            // genuinely-unreachable cases (continuous failure to reach).
            // Counter-check pairing with Finished_MaxAgeExceeded_True:
            // a happy follower never accumulates Age while close.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 6, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;
            goal.Age = 50; // simulate having been pursuing for a while

            goal.TakeAction();

            Assert.AreEqual(0, goal.Age,
                "TakeAction while close-enough must reset Age — preserves "
                + "the MaxAge defensive timeout for unreachable cases only.");
        }

        [Test]
        public void Finished_LeaderFar_False()
        {
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 20, 20, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;

            Assert.IsFalse(goal.Finished(),
                "Follower at distance 15 from leader → goal is NOT finished, "
                + "TakeAction will move toward leader on next tick.");
        }

        // ── TakeAction(): movement ───────────────────────────────

        [Test]
        public void TakeAction_RecruitAdjacent_LeaderWalksAway_FollowerPursues()
        {
            // F.2.6 regression test. Pins the exact bug found via the
            // RecruitShowcase playtest:
            //   1. Player recruits scribe while adjacent (distance 1).
            //   2. RecruitedEffect.OnApply pushes FollowLeaderGoal.
            //   3. Player walks away.
            //   4. Follower SHOULD pursue. Before the fix, the goal
            //      finished on tick 1 (close-enough) and was gone.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 5, "l"); // adjacent (distance 1)

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;

            // Tick 1: adjacent. Goal idles, does NOT finish.
            goal.TakeAction();
            Assert.IsFalse(goal.Finished(),
                "Persistent follow: goal does not finish even when adjacent at recruit time.");

            // Leader walks away to distance 5.
            zone.MoveEntity(leader, 10, 5);
            Assert.IsFalse(goal.Finished(),
                "Goal still active after leader moves.");

            // Follower takes one step toward leader.
            var posBefore = zone.GetEntityPosition(follower);
            int distBefore = System.Math.Max(
                System.Math.Abs(posBefore.x - 10),
                System.Math.Abs(posBefore.y - 5));
            goal.TakeAction();
            var posAfter = zone.GetEntityPosition(follower);
            int distAfter = System.Math.Max(
                System.Math.Abs(posAfter.x - 10),
                System.Math.Abs(posAfter.y - 5));
            Assert.Less(distAfter, distBefore,
                "Follower closed distance after leader walked away.");
        }

        [Test]
        public void TakeAction_LeaderFar_MovesFollowerCloser()
        {
            // Counter-check: a follower that wasn't moving (no Goal)
            // would stay still. With FollowLeaderGoal, the follower
            // takes a step toward the leader on each TakeAction.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 15, 5, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;

            var posBefore = zone.GetEntityPosition(follower);
            int distBefore = System.Math.Max(
                System.Math.Abs(posBefore.x - 15),
                System.Math.Abs(posBefore.y - 5));

            goal.TakeAction();

            var posAfter = zone.GetEntityPosition(follower);
            int distAfter = System.Math.Max(
                System.Math.Abs(posAfter.x - 15),
                System.Math.Abs(posAfter.y - 5));

            Assert.Less(distAfter, distBefore,
                "After one TakeAction, the follower is closer (Chebyshev) "
                + "to the leader than before. Catches a goal that no-ops "
                + "(e.g. pathfind that returns the same cell).");
        }

        [Test]
        public void TakeAction_CloseEnough_NoMove()
        {
            // Within CloseEnoughDistance — the goal is satisfied per
            // Finished(); production code shouldn't even call TakeAction
            // (the brain pops Finished goals). But if it DOES get called
            // defensively, no NRE, no spurious movement.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 6, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;
            var posBefore = zone.GetEntityPosition(follower);

            Assert.DoesNotThrow(() => goal.TakeAction(),
                "Defensive: TakeAction with already-finished goal must not "
                + "throw.");
            var posAfter = zone.GetEntityPosition(follower);
            Assert.AreEqual(posBefore, posAfter,
                "Within close-enough range → no movement.");
        }

        [Test]
        public void TakeAction_NullLeader_NoCrash()
        {
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var goal = PushGoal(follower, null);

            Assert.DoesNotThrow(() => goal.TakeAction(),
                "TakeAction with null leader must not NRE — defensive "
                + "guard since Finished() is the canonical fail path.");
        }

        // ── Persistence (Goal save/load) ─────────────────────────

        [Test]
        public void Goal_RoundTrips_With_LeaderReference()
        {
            // Goals are serialized via SaveGoal/LoadGoal (SaveSystem
            // line 1489-1517) using WritePublicFields. Pin that
            // FollowLeaderGoal's public fields survive a round-trip
            // — particularly the Leader entity ref.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 15, 5, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 4;
            goal.MaxAgeBeforeGiveUp = 99;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(follower);
            var lb = loaded.GetPart<BrainPart>();
            Assert.AreEqual(1, lb.GoalCount, "Goal stack count survives.");
            var lg = lb.PeekGoal() as FollowLeaderGoal;
            Assert.IsNotNull(lg, "Top-of-stack goal is FollowLeaderGoal.");
            Assert.IsNotNull(lg.Leader, "Leader entity ref survives.");
            Assert.AreEqual("l", lg.Leader.ID,
                "Leader ID round-trips via WriteEntityReference.");
            Assert.AreEqual(4, lg.CloseEnoughDistance,
                "CloseEnoughDistance public field round-trips.");
            Assert.AreEqual(99, lg.MaxAgeBeforeGiveUp);
        }

        // ── F.3.7: leader-target combat assist ───────────────────
        // Docs/FOLLOWERS.md "Phase F.3.7 -- Leader-target combat assist".
        // If the leader is actively fighting something (has a live
        // KillGoal + Target in the same zone), the follower joins in
        // instead of idling/following passively.

        private static Entity PushLeaderKillGoal(Entity leader, Entity hostile)
        {
            var brain = leader.GetPart<BrainPart>();
            brain.Target = hostile;
            brain.PushGoal(new KillGoal(hostile));
            return hostile;
        }

        [Test]
        public void TakeAction_LeaderFighting_FollowerPushesKillGoalOnSameTarget()
        {
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 5, "l");
            var hostile = CreateCreature(zone, 7, 5, "h");
            PushLeaderKillGoal(leader, hostile);

            var goal = PushGoal(follower, leader);
            goal.TakeAction();

            var followerBrain = follower.GetPart<BrainPart>();
            var top = followerBrain.PeekGoal() as KillGoal;
            Assert.IsNotNull(top,
                "Follower must push a KillGoal when the leader is actively fighting.");
            Assert.AreSame(hostile, top.Target,
                "Follower's KillGoal must target the SAME hostile the leader is fighting.");
        }

        [Test]
        public void TakeAction_LeaderNotFighting_FollowerDoesNotPushKillGoal()
        {
            // Counter-check: leader has no KillGoal (e.g. just wandering) —
            // follower must not invent a fight. Confirms the assist branch
            // doesn't fire on Target alone (Target defaults to null here,
            // but this also guards against a future bug where Target is
            // set without a live KillGoal).
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 5, "l");

            var goal = PushGoal(follower, leader);
            goal.TakeAction();

            var followerBrain = follower.GetPart<BrainPart>();
            Assert.IsFalse(followerBrain.HasGoal<KillGoal>(),
                "Follower must not push a KillGoal when the leader isn't fighting anything.");
            Assert.IsFalse(goal.Finished(),
                "FollowLeaderGoal itself must still be on the stack (idling), not popped.");
        }

        [Test]
        public void TakeAction_LeaderFightingInDifferentZone_FollowerDoesNotAssist()
        {
            // Counter-check: leader is mid-fight, but the follower is in a
            // different (not-yet-realigned) zone. The existing cross-zone
            // idle branch must still take priority -- no attempt to push a
            // KillGoal against a target the follower can't even reach.
            var followerZone = new Zone("follower-zone");
            var leaderZone = new Zone("leader-zone");
            var follower = CreateCreature(followerZone, 5, 5, "f");
            var leader = CreateCreature(leaderZone, 6, 5, "l");
            var hostile = CreateCreature(leaderZone, 7, 5, "h");
            PushLeaderKillGoal(leader, hostile);

            var goal = PushGoal(follower, leader);
            goal.TakeAction();

            var followerBrain = follower.GetPart<BrainPart>();
            Assert.IsFalse(followerBrain.HasGoal<KillGoal>(),
                "Follower in a different zone must not push a KillGoal against a target it can't reach.");
            Assert.IsFalse(goal.Finished(), "Goal stays on the stack (cross-zone idle), not popped.");
        }

        [Test]
        public void TakeAction_LeaderFightingWhileFollowerAlreadyCloseEnough_AssistTakesPriority()
        {
            // The assist check must run BEFORE the close-enough idle
            // check -- a follower standing right next to its leader
            // must join the fight, not just idle because it's "close enough."
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 5, "l"); // adjacent -- within default CloseEnoughDistance
            var hostile = CreateCreature(zone, 8, 5, "h");
            PushLeaderKillGoal(leader, hostile);

            var goal = PushGoal(follower, leader);
            goal.TakeAction();

            var followerBrain = follower.GetPart<BrainPart>();
            Assert.IsTrue(followerBrain.HasGoal<KillGoal>(),
                "Assist must take priority over the close-enough idle branch.");
        }

        [Test]
        public void TakeAction_MultipleFollowers_AllAssistSameTarget()
        {
            // Two followers, same leader, same fight -- both should join
            // in on the same hostile (Qud-parity "dogpile," no
            // coordination/threat-splitting logic in this phase).
            var zone = new Zone("z");
            var followerA = CreateCreature(zone, 4, 5, "fa");
            var followerB = CreateCreature(zone, 4, 6, "fb");
            var leader = CreateCreature(zone, 6, 5, "l");
            var hostile = CreateCreature(zone, 7, 5, "h");
            PushLeaderKillGoal(leader, hostile);

            var goalA = PushGoal(followerA, leader);
            var goalB = PushGoal(followerB, leader);
            goalA.TakeAction();
            goalB.TakeAction();

            var killA = followerA.GetPart<BrainPart>().PeekGoal() as KillGoal;
            var killB = followerB.GetPart<BrainPart>().PeekGoal() as KillGoal;
            Assert.IsNotNull(killA);
            Assert.IsNotNull(killB);
            Assert.AreSame(hostile, killA.Target);
            Assert.AreSame(hostile, killB.Target);
        }

        [Test]
        public void TakeAction_AssistFightEnds_FollowerResumesFollowingNextTick()
        {
            // Once the follower's own KillGoal finishes (target dies/leaves),
            // it pops naturally (existing KillGoal.Finished() logic -- no
            // new code), and FollowLeaderGoal is back on top to resume
            // following on the next tick. Simulated here by directly
            // popping the KillGoal (mirrors what HandleTakeTurn's
            // finished-goal cleanup does) rather than driving full combat.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 5, "l");
            var hostile = CreateCreature(zone, 7, 5, "h");
            PushLeaderKillGoal(leader, hostile);

            var goal = PushGoal(follower, leader);
            goal.TakeAction();
            var followerBrain = follower.GetPart<BrainPart>();
            var killGoal = followerBrain.PeekGoal() as KillGoal;
            Assert.IsNotNull(killGoal, "Sanity: follower joined the fight.");

            // Target dies and leaves the zone -- KillGoal.Finished() now true.
            zone.RemoveEntity(hostile);
            Assert.IsTrue(killGoal.Finished());
            followerBrain.RemoveGoal(killGoal);

            Assert.AreSame(goal, followerBrain.PeekGoal(),
                "FollowLeaderGoal is back on top once the assist KillGoal pops.");
            Assert.IsFalse(goal.Finished(), "FollowLeaderGoal itself never finished during the assist.");
        }
    }
}
