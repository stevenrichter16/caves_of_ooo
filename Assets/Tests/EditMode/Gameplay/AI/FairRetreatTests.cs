// Native observed-contact, finite-memory and existing-retreat control contracts.
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FairRetreatTests
    {
        FieldMedicineFixture f;
        Entity actor, threat;
        BrainPart brain;
        FleeGoal goal;
        bool previousAi;

        [SetUp] public void Setup()
        {
            f = new FieldMedicineFixture();
            actor = f.Actor(7, medicine: false);
            threat = f.Threat(actor, 16, 10);
            brain = actor.GetPart<BrainPart>(); brain.Wanders = brain.WandersRandomly = false;
            brain.Target = threat; goal = new FleeGoal(threat); brain.PushGoal(goal);
            previousAi = Diag.IsChannelEnabled("ai"); Diag.SetChannel("ai", true);
        }
        [TearDown] public void Cleanup() { Diag.SetChannel("ai", previousAi); f.Dispose(); }
        void Turn() => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        void Screen()
        { for (int y = 0; y < Zone.Height; y++) f.Zone.TileState.WriteCloud(12, y, "smoke", 20); }
        void ClearScreen()
        { for (int y = 0; y < Zone.Height; y++) f.Zone.TileState.Clear(12, y); }
        void Wall(int x, int y)
        {
            var wall = new Entity(); wall.SetTag("Solid"); wall.AddPart(new PhysicsPart { Solid = true });
            Assert.True(f.Zone.AddEntity(wall, x, y));
        }
        static T Read<T>(object instance, string name)
        {
            var field = instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, "Saved retreat field absent: " + name); return (T)field.GetValue(instance);
        }
        static void Write(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, "Saved retreat field absent: " + name); field.SetValue(instance, value);
        }
        int Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter
        { Category = "ai", Kind = kind, Actor = actor.ID }).Records.Count;

        // Same observed threat, two genuinely different hidden detours: both must produce westward retreat.
        [TestCase(6)] [TestCase(14)]
        public void HiddenPlayerDetoursCannotRedirectTheNextRetreatStep(int hiddenY)
        {
            Turn(); Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor));
            Screen(); Assert.True(f.Zone.MoveEntity(threat, 16, hiddenY)); Turn();
            Assert.AreEqual((8, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(16, Read<int>(goal, "LastSeenX")); Assert.AreEqual(10, Read<int>(goal, "LastSeenY"));
            Assert.AreEqual(5, Read<int>(goal, "UnseenRemaining"));
        }
        [Test] public void VisibleDetourDoesRedirectRetreatAndRefreshItsAllowance()
        {
            Turn(); Assert.True(f.Zone.MoveEntity(threat, 16, 14)); Turn();
            Assert.AreEqual((8, 9), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(14, Read<int>(goal, "LastSeenY")); Assert.AreEqual(6, Read<int>(goal, "UnseenRemaining"));
            Assert.False(Read<bool>(goal, "RetreatingUnseen"));
        }
        [TestCase(false)] [TestCase(true)]
        public void NewAndLegacyZeroStateGoalsCannotInventAHiddenThreatLocation(bool legacyShape)
        {
            if (legacyShape)
            {
                brain.ClearGoals();
                goal = (FleeGoal)FormatterServices.GetUninitializedObject(typeof(FleeGoal));
                goal.FleeFrom = threat; goal.MaxTurns = 20; brain.PushGoal(goal); brain.Target = threat;
            }
            Screen(); Turn();
            Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
            Assert.True(goal.Finished()); Assert.Null(brain.Target);
        }
        [Test] public void SixHiddenOpportunitiesExpireWithoutForgivingPersonalHostility()
        {
            Turn(); Screen(); Assert.True(f.Zone.MoveEntity(threat, 16, 14));
            for (int i = 0; i < 5; i++) { Turn(); Assert.False(goal.Finished()); }
            Turn(); Assert.True(goal.Finished()); Assert.AreEqual((3, 10), f.Zone.GetEntityPosition(actor));
            Assert.Null(brain.Target); Assert.True(brain.IsPersonallyHostileTo(threat));
            Turn(); Assert.AreEqual((3, 10), f.Zone.GetEntityPosition(actor));
            Assert.False(brain.HasGoal<FleeGoal>(), "Bored cannot immediately reacquire a still-hidden target.");
        }
        [Test] public void ABlockedRetreatStillSpendsItsHiddenOpportunity()
        {
            Turn(); Screen(); Wall(8, 10); Turn();
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(5, Read<int>(goal, "UnseenRemaining"));
        }
        [Test] public void VisibleReacquisitionRefreshesMemoryAfterTwoHiddenOpportunities()
        {
            Turn(); Screen(); Assert.True(f.Zone.MoveEntity(threat, 16, 14)); Turn(); Turn();
            Assert.AreEqual((7, 10), f.Zone.GetEntityPosition(actor)); ClearScreen(); Turn();
            Assert.AreEqual((6, 9), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(14, Read<int>(goal, "LastSeenY")); Assert.AreEqual(6, Read<int>(goal, "UnseenRemaining"));
            Assert.False(Read<bool>(goal, "RetreatingUnseen")); Assert.False(goal.Finished());
        }
        [TestCase(false)] [TestCase(true)]
        public void ObserverFovChangesOnlyFeedbackNeverTheRetreatDecision(bool actorVisible)
        {
            Turn(); Screen(); f.Zone.GetEntityCell(actor).IsVisible = actorVisible; MessageLog.Clear(); Turn();
            Assert.AreEqual((8, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(5, Read<int>(goal, "UnseenRemaining"));
            Assert.AreEqual(1, Records("RetreatLostSight"));
            Assert.AreEqual(actorVisible, MessageLog.GetAllEntries().Any(m => m.Text.Contains("loses sight")));
        }
        [Test] public void LostSightAndReacquisitionTransitionsAnnounceOnceRatherThanEveryAction()
        {
            Turn(); Screen(); Turn(); Turn(); Assert.AreEqual(1, Records("RetreatLostSight"));
            ClearScreen(); Turn(); Turn(); Assert.AreEqual(1, Records("RetreatReacquired"));
        }
        [Test] public void VisibleBodyContactSuppliesMemoryInsteadOfTheHiddenLargeBodyAnchor()
        {
            Assert.True(f.Zone.RemoveEntity(threat));
            threat.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-5,0" });
            Assert.True(f.Zone.AddEntity(threat, 16, 10)); Screen();
            Assert.False(AIHelpers.HasLineOfSight(f.Zone, 10, 10, 16, 10)); Turn();
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(11, Read<int>(goal, "LastSeenX")); Assert.AreEqual(10, Read<int>(goal, "LastSeenY"));
        }
        [Test] public void CorneredAdjacentContactStillFightsBackInsideMist()
        {
            Assert.True(f.Zone.MoveEntity(threat, 11, 10)); Wall(9, 10);
            f.Zone.TileState.WriteCloud(10, 10, "smoke", 5); f.Zone.TileState.WriteCloud(11, 10, "smoke", 5);
            var attacks = new AttackCounter(); actor.AddPart(attacks); Turn();
            Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(1, attacks.Count);
        }
        [Test] public void RememberedAdjacentPositionDoesNotLicenseAnAttackOnTheNowDistantHiddenBody()
        {
            Assert.True(f.Zone.MoveEntity(threat, 11, 10));
            foreach (var step in new[] { (-1,-1),(-1,0),(-1,1),(0,-1),(0,1),(1,-1),(1,1) })
                Wall(10 + step.Item1, 10 + step.Item2);
            var attacks = new AttackCounter(); actor.AddPart(attacks); Turn(); Assert.AreEqual(1, attacks.Count);
            Screen(); Assert.True(f.Zone.MoveEntity(threat, 16, 14)); int hp = threat.GetStatValue("Hitpoints"); Turn();
            Assert.AreEqual(1, attacks.Count); Assert.AreEqual(hp, threat.GetStatValue("Hitpoints"));
        }
        [TestCase("water")] [TestCase("medicine")]
        public void HiddenSelfTreatmentCostsOneRememberedOpportunityAndDoesNotAlsoMove(string kind)
        {
            Turn(); Screen();
            Entity supply;
            if (kind == "water")
            {
                actor.AddPart(new ThermalPart()); actor.AddPart(new TacticalSupplyPart());
                supply = f.Factory.CreateEntity("SunbladderShell"); Assert.True(actor.GetPart<InventoryPart>().AddObject(supply));
                Assert.True(actor.ApplyEffect(new BurningEffect(1, null, f.Rng)));
            }
            else { FieldMedicineFixture.AttachMedicine(actor); supply = f.Supply(actor); }
            Turn(); Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(5, Read<int>(goal, "UnseenRemaining"));
            if (kind == "water") { Assert.Zero(supply.GetPart<WaterskinPart>().Charges); Assert.False(actor.HasEffect<BurningEffect>()); }
            else { Assert.AreEqual(15, actor.GetStatValue("Hitpoints")); Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(supply)); }
        }
        [Test] public void ScheduledStunDoesNotAdvanceGoalAgeMemoryOrMovement()
        {
            Turn(); Screen(); int age = goal.Age; int allowance = Read<int>(goal, "UnseenRemaining");
            Assert.True(actor.ApplyEffect(new StunnedEffect(5)));
            f.ScheduledAction(actor, threat);
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(age, goal.Age);
            Assert.AreEqual(allowance, Read<int>(goal, "UnseenRemaining"));
        }
        [Test] public void SavedRetreatContinuesFromTheRecordedContactAndRemainingBudget()
        {
            Turn(); Screen(); Assert.True(f.Zone.MoveEntity(threat, 16, 14)); Turn();
            var actorAt = f.Zone.GetEntityPosition(actor); var threatAt = f.Zone.GetEntityPosition(threat);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var restoredBrain = loaded.GetPart<BrainPart>(); var restoredGoal = restoredBrain.FindGoal<FleeGoal>();
            Assert.NotNull(restoredGoal); Assert.True(Read<bool>(restoredGoal, "HasLastSeen"));
            Assert.True(Read<bool>(restoredGoal, "RetreatingUnseen")); Assert.AreEqual(5, Read<int>(restoredGoal, "UnseenRemaining"));
            Assert.AreEqual(16, Read<int>(restoredGoal, "LastSeenX")); Assert.AreEqual(10, Read<int>(restoredGoal, "LastSeenY"));
            Assert.AreSame(restoredBrain, restoredGoal.ParentBrain); Assert.AreSame(restoredBrain.Target, restoredGoal.FleeFrom);
            Assert.AreEqual(goal.Age, restoredGoal.Age); Assert.AreEqual(goal.MaxTurns, restoredGoal.MaxTurns);
            Assert.True(f.Zone.RemoveEntity(actor)); Assert.True(f.Zone.RemoveEntity(threat));
            actor = loaded; threat = restoredGoal.FleeFrom; brain = restoredBrain; goal = restoredGoal;
            Assert.True(f.Zone.AddEntity(actor, actorAt.x, actorAt.y)); Assert.True(f.Zone.AddEntity(threat, threatAt.x, threatAt.y));
            brain.CurrentZone = f.Zone; Turn();
            Assert.AreEqual((7, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(4, Read<int>(goal, "UnseenRemaining"));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void NonpositiveStoredBudgetCannotStartAnotherHiddenAction(int remaining)
        {
            Turn(); Screen(); Write(goal, "UnseenRemaining", remaining); Turn();
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor)); Assert.True(goal.Finished());
        }
        [Test] public void OversizedStoredBudgetIsClampedBeforeUse()
        {
            Turn(); Screen(); Write(goal, "UnseenRemaining", int.MaxValue); Turn();
            Assert.AreEqual(5, Read<int>(goal, "UnseenRemaining"));
        }
        [TestCase(-1,10)] [TestCase(80,10)] [TestCase(10,-1)] [TestCase(10,25)]
        public void InvalidRecordedCellsCannotDriveMovement(int x, int y)
        {
            Turn(); Screen(); Write(goal, "LastSeenX", x); Write(goal, "LastSeenY", y); Turn();
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor)); Assert.True(goal.Finished());
        }
        [Test] public void MemoryExpiryDoesNotClearAnotherGoalsTarget()
        {
            Turn(); Screen(); var other = f.Threat(actor, 10, 15); brain.Target = other;
            Write(goal, "UnseenRemaining", 0); goal.TakeAction();
            Assert.True(goal.Finished()); Assert.AreSame(other, brain.Target);
        }
        [Test] public void ExistingAgeAndHealthCompletionRulesRemainUnchanged()
        {
            goal.Age = goal.MaxTurns; Assert.False(goal.Finished());
            goal.Age++; Assert.True(goal.Finished()); goal.Age = 0;
            // Use an exactly representable fraction for the strict boundary;
            // 8 / 20 versus .4f is not a reliable equality pin in native Mono.
            brain.FleeThreshold = .5f;
            actor.GetStat("Hitpoints").BaseValue = 9; Assert.False(goal.Finished());
            actor.GetStat("Hitpoints").BaseValue = 10; Assert.True(goal.Finished(), "Threshold is strict, not inclusive.");
        }
        [Test] public void LostThreatEndsTheGoalAndNeverCreatesNewKnowledge()
        {
            Assert.True(f.Zone.RemoveEntity(threat)); Assert.True(goal.Finished());
        }
        [Test] public void OrdinaryBoredAcquisitionStillCreatesAnImmediatelyActingRetreat()
        {
            brain.ClearGoals(); brain.Target = null; Turn();
            var acquired = brain.FindGoal<FleeGoal>(); Assert.NotNull(acquired);
            Assert.AreEqual((9, 10), f.Zone.GetEntityPosition(actor)); Assert.True(Read<bool>(acquired, "HasLastSeen"));
            Assert.AreEqual(16, Read<int>(acquired, "LastSeenX")); Assert.AreEqual(6, Read<int>(acquired, "UnseenRemaining"));
        }
        [Test] public void HiddenLowHealthKillStillReturnsToBoredWithoutInventingARetreatHandoff()
        {
            brain.ClearGoals(); actor.GetStat("Hitpoints").BaseValue = 20;
            var kill = new KillGoal(threat); brain.PushGoal(kill); Turn();
            Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor)); actor.GetStat("Hitpoints").BaseValue = 7;
            Screen(); Turn(); Assert.False(brain.HasGoal<KillGoal>()); Assert.False(brain.HasGoal<FleeGoal>());
            Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor)); Assert.Null(brain.Target);
        }
        sealed class AttackCounter : Part
        {
            public int Count;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeMeleeAttack") Count++; return true; }
        }
    }
}
