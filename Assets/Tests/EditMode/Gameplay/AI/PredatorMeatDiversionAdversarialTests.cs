using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed partial class PredatorMeatDiversionTests
    {
        [TestCase(1, true)] [TestCase(0, false)]
        public void OnlyAnUnspentOriginalHuntCanSpendTheBaitAttempt(int remaining, bool consumes)
        {
            Configure(); Meat(); role.PursuitRemaining = remaining;
            for (int i = 0; i < 3; i++) Turn(hunter);
            Assert.AreEqual(consumes, zone.GetEntityCell(meat) == null);
            Assert.AreEqual(consumes, Field<bool>("MeatDiversionAttempted"));
            Assert.AreEqual(consumes ? "Consumed" : "None", MeatPhase);
            Assert.AreEqual(consumes ? SpreadHuntPhase.Watching : SpreadHuntPhase.Exhausted, role.Phase);
        }

        [TestCase("hostile")] [TestCase("combat")] [TestCase("calm")]
        [TestCase("work")] [TestCase("party")] [TestCase("conversation")]
        public void ActivePriorityInterruptionSpendsAttemptAndLeavesFood(string mode)
        {
            Configure(); Meat(); Turn(hunter);
            Assert.AreEqual(1, Field<int>("MeatFeedProgress"));
            var brain = hunter.GetPart<BrainPart>();
            if (mode == "hostile") Assert.True(zone.MoveEntity(player, 10, 11));
            if (mode == "combat") brain.PushGoal(new KillGoal(player));
            if (mode == "calm") brain.PushGoal(new NoFightGoal(9));
            if (mode == "work") brain.PushGoal(new WaitGoal(9));
            if (mode == "party") { brain.SetPartyLeader(player); brain.PushGoal(new FollowLeaderGoal(player)); }
            if (mode == "conversation") brain.InConversation = true;
            Turn(hunter);
            Assert.AreEqual("Aborted", MeatPhase); Assert.True(Field<bool>("MeatDiversionAttempted"));
            Assert.IsNull(Field<Entity>("MeatDiversionTarget")); Assert.NotNull(zone.GetEntityCell(meat));
            Assert.AreEqual(1, Units(meat)); Assert.AreEqual(24, role.PursuitRemaining);
        }

        [Test] public void BlockedAfterNoticeSpendsAtMostSixApproachActions()
        {
            Configure(); Meat(x: 16); string before = HuntState(); Turn(hunter);
            Assert.AreEqual("Approaching", MeatPhase); Assert.AreEqual(5, Field<int>("MeatApproachRemaining"));
            var at = zone.GetEntityCell(hunter);
            // Same-faction living bodies physically obstruct, but neither block
            // sight nor become an outside-prey hostile. Only hunter is scheduled.
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                if (dx != 0 || dy != 0) Place("ReedbackGrazer", at.X + dx, at.Y + dy);
            for (int i = 0; i < 5; i++) Turn(hunter);
            Assert.AreEqual("Aborted", MeatPhase); Assert.AreEqual(0, Field<int>("MeatApproachRemaining"));
            Assert.AreEqual(before, HuntState()); Assert.NotNull(zone.GetEntityCell(meat));
            Assert.AreEqual(1, Units(meat)); Assert.AreSame(at, zone.GetEntityCell(hunter));
        }

        [Test] public void FeedingCallbackCannotReenterForAnotherBiteOrHuntStep()
        {
            Configure(); Meat("RawMeat", 2); string before = HuntState(); int callbacks = 0;
            EntityVisualHooks.InteractionCallback = (actor, target, current) =>
            { if (actor == hunter && ++callbacks == 1) Turn(hunter); };
            Turn(hunter);
            Assert.AreEqual(1, callbacks); Assert.AreEqual(1, Field<int>("MeatFeedProgress"));
            Assert.AreEqual(2, Units(meat)); Assert.AreEqual(before, HuntState());
            Turn(hunter); Turn(hunter); Assert.AreEqual(1, Units(meat)); Assert.AreEqual("Consumed", MeatPhase);
        }

        [TestCase(false)] [TestCase(true)]
        public void PartialDropRollbackOrCommitPrecedesOrdinaryBaitAction(bool commit)
        {
            Configure(); Meat("DriedMeat", 2);
            Assert.True(zone.RemoveEntity(meat)); Assert.True(zone.MoveEntity(player, 11, 10));
            Assert.True(player.GetPart<InventoryPart>().AddObject(meat));
            var transaction = new InventoryTransaction();
            var command = new DropPartialCommand(meat, 1); var context = new InventoryContext(player, zone);
            Assert.True(command.Validate(context).IsValid); Assert.True(command.Execute(context, transaction).Success);
            if (commit) transaction.Commit(); else transaction.Rollback();
            Assert.True(zone.MoveEntity(player, 60, 20));
            for (int i = 0; i < 3; i++) Turn(hunter);
            Assert.AreEqual(commit ? 1 : 2, Units(meat));
            Assert.AreEqual(commit, Field<bool>("MeatDiversionAttempted"));
            Assert.False(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "DriedMeat"));
            Assert.True(player.GetPart<InventoryPart>().Objects.Contains(meat));
        }

        [TestCase(1, false)] [TestCase(1, true)] [TestCase(3, false)] [TestCase(3, true)]
        public void RealThrownSingletonOrSplitIsNotBaitUntilCompletedCommand(int count, bool commit)
        {
            Configure(); Meat("DriedMeat", count);
            Assert.True(zone.RemoveEntity(meat)); Assert.True(zone.MoveEntity(player, 20, 10));
            Assert.True(player.GetPart<InventoryPart>().AddObject(meat));
            var context = new InventoryContext(player, zone); var transaction = new InventoryTransaction();
            var command = new ThrowItemCommand(meat, 11, 10, new Random(1));
            Assert.True(command.Validate(context).IsValid); Assert.True(command.Execute(context, transaction).Success);
            var landed = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "DriedMeat");
            Assert.AreEqual((11, 10), zone.GetEntityPosition(landed)); Assert.AreEqual(1, Units(landed));
            if (commit) transaction.Commit(); else transaction.Rollback();
            for (int i = 0; i < 3; i++) Turn(hunter);
            Assert.False(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "DriedMeat"));
            Assert.AreEqual(commit, Field<bool>("MeatDiversionAttempted"));
            int carried = player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "DriedMeat").Sum(Units);
            Assert.AreEqual(commit ? count - 1 : count, carried);
            if (carried > 0) Assert.True(player.GetPart<InventoryPart>().Objects.Contains(meat));
        }

        [TestCase(1)] [TestCase(2)]
        public void CompetingHuntersRevalidateTheRemainingPhysicalUnit(int count)
        {
            Configure(); Meat("RawMeat", count);
            var other = Place("Furrowstalker", 12, 10); var quarry = Place("ReedbackGrazer", 18, 10);
            var otherRole = other.GetPart<SpreadPredatorPart>();
            Assert.True((bool)typeof(SpreadPredatorPart).GetMethod("Configure").Invoke(otherRole, new object[] { zone, quarry, true }));
            for (int i = 0; i < 2; i++) { Turn(hunter); Turn(other); }
            Assert.AreEqual(2, Field<int>("MeatFeedProgress"));
            Assert.AreEqual(2, typeof(SpreadPredatorPart).GetField("MeatFeedProgress").GetValue(otherRole));
            Turn(hunter); Turn(other);
            Assert.IsNull(zone.GetEntityCell(meat)); Assert.AreEqual("Consumed", MeatPhase);
            Assert.AreEqual(count == 1 ? "Aborted" : "Consumed",
                typeof(SpreadPredatorPart).GetField("MeatDiversionPhase").GetValue(otherRole).ToString());
            Assert.IsNull(typeof(SpreadPredatorPart).GetField("MeatDiversionTarget").GetValue(otherRole));
            Assert.AreEqual(24, role.PursuitRemaining); Assert.AreEqual(24, otherRole.PursuitRemaining);
        }

        [Test] public void EqualDistanceNoticeUsesStableIdentityAndSkipsUnsafeNearerFood()
        {
            Configure(); Meat(x: 9); var other = Place("DriedMeat", 11, 10);
            var expected = string.CompareOrdinal(meat.ID, other.ID) < 0 ? meat : other;
            Turn(hunter); Assert.AreSame(expected, Field<Entity>("MeatDiversionTarget"));
            Cleanup(); Setup(); Configure(); Meat(); zone.TileState.AddHeat(11, 10, 1);
            var safe = Place("RawMeat", 8, 10); Turn(hunter);
            Assert.AreSame(safe, Field<Entity>("MeatDiversionTarget")); Assert.AreEqual(1, Units(meat));
        }

        [TestCase(false)] [TestCase(true)]
        public void LocalReserveOwnershipMustBeReleasedBeforeFoodIsPublic(bool released)
        {
            Configure(); Meat(); meat.AddPart(new ReserveYieldPart { Released = released });
            for (int i = 0; i < 3; i++) Turn(hunter);
            Assert.AreEqual(released, Field<bool>("MeatDiversionAttempted"));
            Assert.AreEqual(released, zone.GetEntityCell(meat) == null);
        }

        [Test] public void HiddenPreyCoordinatesDoNotChangeBaitMovementOrOriginalMemory()
        {
            string[] Observe(int preyX)
            {
                Configure(); Meat(x: 15);
                role.Phase = SpreadHuntPhase.Searching; role.HasLastSeen = true;
                role.LastSeenX = 10; role.LastSeenY = 12; role.PursuitRemaining = 11; role.SearchRemaining = 3;
                for (int x = 0; x < Zone.Width; x++) Place("Tree", x, 13);
                Assert.True(zone.MoveEntity(prey, preyX, 16));
                Assert.False(AIHelpers.HasLineOfSight(zone, 10, 10, preyX, 16));
                var sequence = new List<string>();
                for (int i = 0; i < 7; i++)
                {
                    Turn(hunter);
                    sequence.Add(string.Join("|", zone.GetEntityPosition(hunter), MeatPhase,
                        Field<int>("MeatFeedProgress"), role.Phase, role.LastSeenX, role.LastSeenY,
                        role.PursuitRemaining, role.SearchRemaining));
                }
                Assert.AreEqual("Consumed", MeatPhase); return sequence.ToArray();
            }
            var first = Observe(10); Cleanup(); Setup(); var second = Observe(30);
            CollectionAssert.AreEqual(first, second);
        }

        [Test] public void LosingSightOfStationaryBaitCancelsWithoutFollowingHiddenFood()
        {
            Configure(); Meat(x: 15); Turn(hunter);
            var position = zone.GetEntityPosition(hunter); Place("Tree", 12, 10);
            Turn(hunter); Assert.AreEqual("Aborted", MeatPhase);
            Assert.AreEqual(position, zone.GetEntityPosition(hunter)); Assert.NotNull(zone.GetEntityCell(meat));
            Assert.AreEqual(1, Units(meat)); Assert.IsNull(Field<Entity>("MeatDiversionTarget"));
        }

        [Test] public void CanceledAttemptStaysSpentAfterReplacementSave()
        {
            Configure(); Meat(); Turn(hunter); Assert.True(zone.MoveEntity(meat, 12, 10)); Turn(hunter);
            Assert.AreEqual("Aborted", MeatPhase); RoundTrip();
            Assert.True(Field<bool>("MeatDiversionAttempted")); Assert.IsNull(Field<Entity>("MeatDiversionTarget"));
            Turn(hunter); Assert.AreEqual(1, Units(meat)); Assert.NotNull(zone.GetEntityCell(meat));
        }

        [TestCase(1)] [TestCase(3)]
        public void MidApproachReplacementSaveKeepsExactAnchorAndRemainingBudget(int actions)
        {
            Configure(); Meat("DriedMeat", 2, x: 15);
            string before = HuntState();
            for (int i = 0; i < actions; i++) Turn(hunter);
            Assert.AreEqual("Approaching", MeatPhase);
            Assert.AreEqual(6 - actions, Field<int>("MeatApproachRemaining"));
            var oldHunter = hunter; var oldMeat = meat; var at = zone.GetEntityPosition(hunter);
            RoundTrip();
            Assert.AreNotSame(oldHunter, hunter); Assert.AreNotSame(oldMeat, meat);
            Assert.AreEqual(at, zone.GetEntityPosition(hunter)); Assert.AreEqual(before, HuntState());
            Assert.AreSame(meat, Field<Entity>("MeatDiversionTarget"));
            Assert.AreEqual(meat.ID, Field<string>("MeatDiversionTargetID"));
            Assert.AreEqual(15, Field<int>("MeatDiversionX")); Assert.AreEqual(10, Field<int>("MeatDiversionY"));
            Assert.AreEqual(6 - actions, Field<int>("MeatApproachRemaining"));
            Assert.AreEqual(0, Field<int>("MeatFeedProgress"));
            for (int i = actions; i < 7; i++) Turn(hunter);
            Assert.AreEqual("Consumed", MeatPhase); Assert.AreEqual(1, Units(meat));
            Assert.AreEqual(before, HuntState()); Assert.IsNull(Field<Entity>("MeatDiversionTarget"));
        }

        [TestCase(true)] [TestCase(false)]
        public void DiagToggleChangesOnlyReceiptsNotFoodConservation(bool enabled)
        {
            bool prior = Diag.IsChannelEnabled("ai");
            try
            {
                var before = new HashSet<string>(Diag.Snapshot(4096).Select(e => e.TraceId));
                Diag.SetChannel("ai", enabled); Configure(); Meat();
                for (int i = 0; i < 3; i++) Turn(hunter);
                Assert.IsNull(zone.GetEntityCell(meat)); Assert.AreEqual("Consumed", MeatPhase);
                var rows = Diag.Snapshot(4096).Where(e => !before.Contains(e.TraceId) && e.ActorId == hunter.ID && e.TargetId == meat.ID
                    && e.Kind.StartsWith("SpreadMeatDiversion", StringComparison.Ordinal)).ToArray();
                Assert.AreEqual(enabled ? 4 : 0, rows.Length);
                if (enabled)
                {
                    Assert.AreEqual(1, rows.Count(e => e.Kind == "SpreadMeatDiversionAdmission"));
                    Assert.AreEqual(2, rows.Count(e => e.Kind == "SpreadMeatDiversionProgress"));
                    Assert.AreEqual(1, rows.Count(e => e.Kind == "SpreadMeatDiversionOutcome"));
                }
            }
            finally { Diag.SetChannel("ai", prior); }
        }
    }
}
