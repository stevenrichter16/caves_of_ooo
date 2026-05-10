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
    ///   <item>Leader in a different zone → Finished()</item>
    ///   <item>Leader's CurrentZone is null (e.g. mid-transition) → Finished()</item>
    ///   <item>MaxAge exceeded → Finished() (defensive timeout)</item>
    ///   <item>Close enough to leader → Finished() (goal satisfied)</item>
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
        public void Finished_LeaderInDifferentZone_True()
        {
            // Cross-zone is OUT OF SCOPE for F.1 — the goal gives up
            // when zones don't match. F.4+ may revisit with a zone-
            // transition pursuit mechanic.
            var zoneA = new Zone("A");
            var zoneB = new Zone("B");
            var follower = CreateCreature(zoneA, 5, 5, "f");
            var leader = CreateCreature(zoneB, 10, 10, "l");

            var goal = PushGoal(follower, leader);

            Assert.IsTrue(goal.Finished(),
                "Leader in a different zone → goal finishes (F.1 limitation; "
                + "cross-zone pursuit deferred to F.4+).");
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
        public void Finished_CloseEnough_True()
        {
            // Goal is satisfied when the follower is within
            // CloseEnoughDistance of the leader.
            var zone = new Zone("z");
            var follower = CreateCreature(zone, 5, 5, "f");
            var leader = CreateCreature(zone, 6, 6, "l");

            var goal = PushGoal(follower, leader);
            goal.CloseEnoughDistance = 2;

            Assert.IsTrue(goal.Finished(),
                "Follower at Chebyshev distance 1 from leader → already "
                + "within CloseEnoughDistance=2 → goal is satisfied.");
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
    }
}
