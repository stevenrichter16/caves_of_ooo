// Lifecycle counterchecks added during the fair-retreat source review.
using System;
using System.Linq;
using System.Runtime.Serialization;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FairRetreatLifecycleCounterTests
    {
        FieldMedicineFixture f;
        Entity actor, threat;
        BrainPart brain;
        FleeGoal goal;
        [SetUp] public void Setup()
        {
            f = new FieldMedicineFixture(); actor = f.Actor(7, medicine: false); threat = f.Threat(actor, 16, 10);
            brain = actor.GetPart<BrainPart>(); brain.Wanders = brain.WandersRandomly = false;
            goal = new FleeGoal(threat); brain.PushGoal(goal); brain.Target = threat;
        }
        [TearDown] public void Cleanup() => f.Dispose();
        void Turn() => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        void Screen()
        { for (int y = 0; y < Zone.Height; y++) f.Zone.TileState.WriteCloud(12, y, "smoke", 20); }

        [Test] public void RemovingAnOrdinaryFleeClearsOnlyItsMatchingTargetAndRetainsHostility()
        {
            brain.RemoveGoal(goal); Assert.Null(brain.Target);
            Assert.True(brain.IsPersonallyHostileTo(threat)); Assert.False(brain.HasGoal<FleeGoal>());
        }
        [Test] public void RemovingOldFleeDoesNotClearADifferentTargetsSelection()
        {
            var other = f.Threat(actor, 10, 15); brain.Target = other;
            brain.RemoveGoal(goal); Assert.AreSame(other, brain.Target);
        }
        [Test] public void RemovingFleeCannotCancelAnAlreadyPaidFixedRayStrike()
        {
            Assert.True(f.Zone.MoveEntity(threat, 12, 10));
            var commitment = new CommittedMeleePart { Reach = 2 }; actor.AddPart(commitment);
            var attacks = new AttackCounter(); actor.AddPart(attacks);
            Assert.True(commitment.TryBegin(threat, f.Zone));
            brain.RemoveGoal(goal);
            Assert.AreSame(threat, brain.Target); Assert.True(commitment.IsWindingUp);
            Turn(); Assert.True(commitment.IsRecovering); Assert.AreEqual(1, attacks.Count);
            Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
        }
        [TestCase("water")] [TestCase("medicine")]
        public void LastHiddenOpportunityCanTreatButStillExhaustsMemoryWithoutMoving(string kind)
        {
            Turn(); Screen();
            for (int i = 0; i < 5; i++) Turn();
            Assert.AreEqual((4, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(1, goal.UnseenRemaining);
            Entity supply;
            if (kind == "water")
            {
                actor.AddPart(new ThermalPart()); actor.AddPart(new TacticalSupplyPart());
                supply = f.Factory.CreateEntity("SunbladderShell"); Assert.True(actor.GetPart<InventoryPart>().AddObject(supply));
                Assert.True(actor.ApplyEffect(new BurningEffect(1, null, f.Rng)));
            }
            else { FieldMedicineFixture.AttachMedicine(actor); supply = f.Supply(actor); }
            Turn(); Assert.AreEqual((4, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(0, goal.UnseenRemaining); Assert.True(goal.Abandoned); Assert.True(goal.Finished());
            Assert.Null(brain.Target);
            if (kind == "water") { Assert.Zero(supply.GetPart<WaterskinPart>().Charges); Assert.False(actor.HasEffect<BurningEffect>()); }
            else { Assert.AreEqual(15, actor.GetStatValue("Hitpoints")); Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(supply)); }
        }
        [Test] public void BrainRetainsTheExistingLastAllowedAgeStepBeforePoppingNextOpportunity()
        {
            Turn(); Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor)); goal.Age = goal.MaxTurns;
            Turn(); Assert.AreEqual(goal.MaxTurns + 1, goal.Age); Assert.AreEqual((8, 10), f.Zone.GetEntityPosition(actor));
            Screen(); Turn();
            Assert.AreEqual((8, 10), f.Zone.GetEntityPosition(actor));
            Assert.False(brain.GetGoalsSnapshot().Contains(goal)); Assert.Null(brain.Target);
        }
        [Test] public void CorneredVisibleLargeBodyContactCanDefendWithoutSeeingTheFarAnchor()
        {
            Assert.True(f.Zone.RemoveEntity(threat)); threat.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-5,0" });
            Assert.True(f.Zone.AddEntity(threat, 16, 10)); Screen();
            var wall = new Entity(); wall.SetTag("Solid"); wall.AddPart(new PhysicsPart { Solid = true });
            Assert.True(f.Zone.AddEntity(wall, 9, 10));
            var attacks = new AttackCounter(); actor.AddPart(attacks);
            Assert.False(AIHelpers.HasLineOfSight(f.Zone, 10, 10, 16, 10)); Turn();
            Assert.AreEqual(11, goal.LastSeenX); Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(1, attacks.Count);
        }
        [Test] public void LegacyZeroStateCanAcquireARealVisibleContactNormally()
        {
            brain.ClearGoals(); goal = (FleeGoal)FormatterServices.GetUninitializedObject(typeof(FleeGoal));
            goal.FleeFrom = threat; goal.MaxTurns = 20; brain.PushGoal(goal); brain.Target = threat;
            Assert.False(goal.HasLastSeen); Assert.Zero(goal.UnseenRemaining); Turn();
            Assert.True(goal.HasLastSeen); Assert.False(goal.RetreatingUnseen); Assert.False(goal.Abandoned);
            Assert.AreEqual(16, goal.LastSeenX); Assert.AreEqual(6, goal.UnseenRemaining);
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor));
        }
        sealed class AttackCounter : Part
        {
            public int Count;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeMeleeAttack") Count++; return true; }
        }
    }
}
