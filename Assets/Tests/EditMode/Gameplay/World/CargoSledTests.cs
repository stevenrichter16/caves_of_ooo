using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CargoSledTests : FiftyWorldFixture
    {
        CavesOfOoo.Data.EntityFactory oldForge;
        [SetUp] public void ReadySled()
        {
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            Actor.GetStat("Strength").BaseValue=18;
            Actor.GetStat("Speed").BaseValue=100;
            foreach(var cell in Zone.Cells){cell.IsVisible=true;cell.Explored=true;}
        }
        [TearDown] public void RestoreSledStatics(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        Entity Sled()=>Carry("CargoSled");
        Entity Load(string name="FallenBeam")=>Place(name);
        void Fit(Entity sled)=>Assert.True(Act(sled,Choice(sled,"FitCargoSled|")));
        static Entity Link(Entity owner,string part,string field)
        {var p=owner.GetPart(part);Assert.NotNull(p,part);return (Entity)p.GetType().GetField(field).GetValue(p);}

        [Test] public void OrdinaryBeamRetainsItsExistingHaulWeightAndPenalty()
        {
            var load=Load();Assert.AreEqual(60,HandlingService.GetWeight(load));
            Assert.AreEqual(DragVerdict.Ok,DragRules.CanDrag(Actor,load));
            Assert.AreEqual(24,DragSystem.PenaltyFor(load));
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));
            Assert.AreEqual(76,Actor.GetStatValue("Speed"));DragSystem.Release(Actor);
            Assert.AreEqual(100,Actor.GetStatValue("Speed"));
        }
        [Test] public void ExistingBlockerStillStopsOrdinaryLoadFollowing()
        {
            var load=Load();Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));
            var wall=Place("StoneWall",10,10);
            Assert.True(MovementSystem.ForceMoveTo(Actor,Zone,9,10));
            Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(load));Assert.False(DragSystem.IsDragging(Actor));
            Assert.AreEqual(100,Actor.GetStatValue("Speed"));
        }
        [Test] public void FittingSledKeepsBothPhysicalOwnersAndCountsItsMass()
        {
            var load=Load();var sled=Sled();string id=sled.ID;Fit(sled);
            Assert.AreEqual(id,sled.ID);Assert.AreSame(Zone.GetEntityCell(load),Zone.GetEntityCell(sled));
            Assert.False(Pack.Objects.Contains(sled));Assert.False(sled.GetPart<PhysicsPart>().Takeable);
            Assert.AreSame(load,Link(sled,"CargoSled","Load"));Assert.AreSame(sled,Link(load,"SledLoad","Sled"));
            Assert.AreEqual(66,HandlingService.GetWeight(load));Assert.AreEqual(13,DragSystem.PenaltyFor(load));
        }
        [Test] public void FittedSledChangesSpeedAndFollowsTheSameLoad()
        {
            var load=Load();var sled=Sled();Fit(sled);
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));Assert.AreEqual(87,Actor.GetStatValue("Speed"));
            Assert.True(MovementSystem.ForceMoveTo(Actor,Zone,9,10));
            Assert.AreSame(Zone.GetCell(10,10),Zone.GetEntityCell(load));Assert.AreSame(Zone.GetEntityCell(load),Zone.GetEntityCell(sled));
            DragSystem.Release(Actor);Assert.AreEqual(100,Actor.GetStatValue("Speed"));
        }
        [Test] public void SledDoesNotMakeTooHeavyMillstoneHaulable()
        {
            var load=Load("MillStone");var sled=Sled();
            Assert.False(Actions(sled).Any(a=>a.Command.StartsWith("FitCargoSled|")));
            Assert.AreEqual(DragVerdict.TooHeavy,DragRules.CanDrag(Actor,load));
        }
        [Test] public void RecoveryReturnsSameSledWithoutDeletingOrMovingLoad()
        {
            var load=Load();var sled=Sled();Fit(sled);var cell=Zone.GetEntityCell(load);
            Assert.True(Act(sled,Choice(sled,"TakeCargoSled|")));Assert.True(Pack.Objects.Contains(sled));
            Assert.AreSame(cell,Zone.GetEntityCell(load));Assert.IsNull(Zone.GetEntityCell(sled));
            Assert.IsNull(load.GetPart("SledLoad"));Assert.IsNull(Link(sled,"CargoSled","Load"));
            Assert.AreEqual(60,HandlingService.GetWeight(load));Assert.AreEqual(24,DragSystem.PenaltyFor(load));
        }
        [Test]
        public void ALoadedPalletRoundsTheBendAndRemainsOneTimberSource()
        {
            Actor.GetStat("Strength").BaseValue = 12;
            var pallet = Load("GleanersTimberPallet");
            var sled = Sled();
            Fit(sled);
            Assert.That(HandlingService.GetWeight(pallet), Is.EqualTo(96));
            Assert.That(DragSystem.TryGrab(Actor, pallet, Zone), Is.EqualTo(DragVerdict.Ok));
            Assert.That(Actor.GetStatValue("Speed"), Is.EqualTo(81));

            var wall = Place("StoneWall", 10, 9);
            Assert.That(MovementSystem.TryMoveTo(Actor, Zone, 10, 9), Is.False);
            Assert.That(Zone.GetEntityPosition(Actor), Is.EqualTo((10, 10)));
            Assert.That(Zone.GetEntityPosition(pallet), Is.EqualTo((11, 10)));
            Assert.That(Zone.GetEntityCell(sled), Is.SameAs(Zone.GetEntityCell(pallet)));
            Assert.That(DragSystem.GetDragged(Actor), Is.SameAs(pallet));
            Assert.That(Zone.RemoveEntity(wall), Is.True);

            foreach (var (x, y) in new[] { (9, 10), (9, 11), (8, 11) })
            {
                var vacated = Zone.GetEntityCell(Actor);
                Assert.That(MovementSystem.TryMoveTo(Actor, Zone, x, y), Is.True);
                Assert.That(Zone.GetEntityCell(pallet), Is.SameAs(vacated));
                Assert.That(Zone.GetEntityCell(sled), Is.SameAs(vacated));
                Assert.That(DragSystem.GetDragged(Actor), Is.SameAs(pallet));
            }
            Assert.That(Zone.GetEntityPosition(pallet), Is.EqualTo((9, 11)));
            DragSystem.Release(Actor);
            Assert.That(Actor.GetStatValue("Speed"), Is.EqualTo(100));
            Assert.That(Act(sled, Choice(sled, "TakeCargoSled|")), Is.True);
            Assert.That(Pack.Objects, Does.Contain(sled));
            Assert.That(Zone.GetEntityPosition(pallet), Is.EqualTo((9, 11)));
            Assert.That(Act(pallet, "Harvest"), Is.True);
            Assert.That(Pack.Objects.Single(item => item.BlueprintName == "SalvagedTimber").GetPart<StackerPart>().StackCount,
                Is.EqualTo(2));
            Assert.That(Act(pallet, "Harvest"), Is.False);
            Assert.That(Zone.GetEntityCell(pallet), Is.Null);
        }
        [Test] public void RemovedLoadLeavesSledRecoverable()
        {
            var load=Load();var sled=Sled();Fit(sled);Assert.True(Zone.RemoveEntity(load));
            Assert.True(Act(sled,Choice(sled,"TakeCargoSled|")));Assert.True(Pack.Objects.Contains(sled));
            Assert.IsNull(Zone.GetEntityCell(load));Assert.IsNull(Link(sled,"CargoSled","Load"));
        }
        [Test] public void ActiveHaulingRefusesSledRemoval()
        {
            var load=Load();var sled=Sled();Fit(sled);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));
            Assert.False(Actions(sled).Any(a=>a.Command.StartsWith("TakeCargoSled|")));
        }
        [Test] public void StaleFittingQuoteDoesNotStealMovedLoad()
        {
            var load=Load();var sled=Sled();string command=Choice(sled,"FitCargoSled|");
            Assert.True(Zone.MoveEntity(load,15,10));Assert.False(Act(sled,command));
            Assert.True(Pack.Objects.Contains(sled));Assert.IsNull(load.GetPart("SledLoad"));
        }
        [Test] public void InterruptedFittingRestoresExactSledAndNoSupport()
        {
            var load=Load();var sled=Sled();string command=Choice(sled,"FitCargoSled|");FailAfter();
            Assert.False(Act(sled,command));Assert.True(Pack.Objects.Contains(sled));Assert.IsNull(Zone.GetEntityCell(sled));
            Assert.True(sled.GetPart<PhysicsPart>().Takeable);Assert.IsNull(load.GetPart("SledLoad"));
        }
        [Test] public void FittedSaveGraphRetainsSameSledAndLoadReferences()
        {
            var load=Load();var sled=Sled();Fit(sled);
            var fresh=PartRoundTripHelper.RoundTripEntityViaTokenGraph(load);
            var savedSled=Link(fresh,"SledLoad","Sled");Assert.AreEqual(sled.ID,savedSled.ID);
            Assert.AreSame(fresh,Link(savedSled,"CargoSled","Load"));
        }
        [Test] public void BrokenSledLosesAidWithoutDeletingMassOrLeavingSpeedResidue()
        {
            var load=Load();var sled=Sled();Fit(sled);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));
            Actor.GetStat("Speed").Penalty+=7;Clock.AddEntity(Actor);
            sled.ApplyEffect(new BrokenEffect());Clock.Tick();
            Assert.AreEqual(66,HandlingService.GetWeight(load));Assert.AreEqual(26,DragSystem.PenaltyFor(load));
            Assert.AreEqual(67,Actor.GetStatValue("Speed"));Assert.AreEqual(67,Clock.GetEnergy(Actor));
            DragSystem.Release(Actor);Assert.AreEqual(93,Actor.GetStatValue("Speed"));
        }
        [Test] public void IndependentLoadMovementLeavesSledBehindAndRefreshesBeforeEnergy()
        {
            var load=Load();var sled=Sled();Fit(sled);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));
            Assert.True(MovementSystem.ForceMoveTo(load,Zone,11,11));
            Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(sled));Assert.AreEqual(60,HandlingService.GetWeight(load));
            Clock.AddEntity(Actor);Clock.Tick();Assert.AreEqual(76,Clock.GetEnergy(Actor));
            DragSystem.Release(Actor);Assert.True(Act(sled,Choice(sled,"TakeCargoSled|")));Assert.True(Pack.Objects.Contains(sled));
        }
        [Test] public void BlockedHaulDoesNotMoveSledThroughTheObstacle()
        {
            var load=Load();var sled=Sled();Fit(sled);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,load,Zone));
            Place("StoneWall",10,10);Assert.True(MovementSystem.ForceMoveTo(Actor,Zone,9,10));
            Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(load));Assert.AreSame(Zone.GetEntityCell(load),Zone.GetEntityCell(sled));
            Assert.False(DragSystem.IsDragging(Actor));Assert.AreEqual(100,Actor.GetStatValue("Speed"));
        }
        [Test] public void InterruptedRetrievalRestoresOriginalGroundSledAndLink()
        {
            var load=Load();var sled=Sled();Fit(sled);string command=Choice(sled,"TakeCargoSled|");FailAfter();
            Assert.False(Act(sled,command));Assert.False(Pack.Objects.Contains(sled));
            Assert.AreSame(Zone.GetEntityCell(load),Zone.GetEntityCell(sled));Assert.AreSame(load,Link(sled,"CargoSled","Load"));
            Assert.AreEqual(13,DragSystem.PenaltyFor(load));
        }
        [Test] public void FullPackCannotLoseAnAttemptedSledRetrieval()
        {
            var load=Load();var sled=Sled();Fit(sled);Pack.MaxWeight=0;
            Assert.False(Act(sled,Choice(sled,"TakeCargoSled|")));Assert.AreSame(Zone.GetEntityCell(load),Zone.GetEntityCell(sled));
            Assert.False(Pack.Objects.Contains(sled));Assert.AreSame(load,Link(sled,"CargoSled","Load"));
        }
        [Test] public void RepeatedCurrentGatherRetainsDisplayedSledChoice()
        {Load();var sled=Sled();string command=Choice(sled,"FitCargoSled|");Actions(sled);Assert.True(Act(sled,command));}
        [Test] public void OrdinaryForgeMakesOneSledFromExactFiniteMaterials()
        {
            Place("TinkersForge",9,9);var spike=Carry("IronSpikeComponent");
            var timber=Carry("SalvagedTimber");timber.GetPart<StackerPart>().StackCount=2;
            var cord=Carry("KnotflaxCord");cord.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(spike,Choice(spike,"MaterialProcess|make_cargo_sled|")));
            Assert.AreEqual(1,Count("CargoSled"));Assert.AreEqual(0,Count("IronSpikeComponent"));
            Assert.AreEqual(0,Count("SalvagedTimber"));Assert.AreEqual(0,Count("KnotflaxCord"));
        }
        [TestCase("owned")][TestCase("hidden")][TestCase("stack")][TestCase("capacity")]
        public void IneligibleLoadNeverGetsAFreeSledLink(string mode)
        {
            var load=Load();var sled=Sled();
            if(mode=="owned")load.SetTag("Owned");
            if(mode=="hidden")Zone.GetCell(11,10).IsVisible=false;
            if(mode=="stack")load.AddPart(new StackerPart{StackCount=2});
            if(mode=="capacity")load.GetPart<HandlingPart>().Weight=140;
            Assert.False(Actions(sled).Any(a=>a.Command.StartsWith("FitCargoSled|")));
            Assert.True(Pack.Objects.Contains(sled));Assert.IsNull(load.GetPart("SledLoad"));
        }
        [Test] public void LateIndependentLoadMoveIsPreservedWhileFittingRollsBack()
        {
            var load=Load();var sled=Sled();
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Assert.True(Zone.MoveEntity(load,15,10))});
            Assert.False(Act(sled,Choice(sled,"FitCargoSled|")));
            Assert.AreSame(Zone.GetCell(15,10),Zone.GetEntityCell(load));Assert.True(Pack.Objects.Contains(sled));
            Assert.IsNull(load.GetPart("SledLoad"));Assert.IsNull(Link(sled,"CargoSled","Load"));
        }
        [Test] public void LateIndependentSledMoveStaysRecoverableAtItsRealDestination()
        {
            var load=Load();var sled=Sled();
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Assert.True(Zone.MoveEntity(sled,12,11))});
            Assert.False(Act(sled,Choice(sled,"FitCargoSled|")));
            Assert.AreSame(Zone.GetCell(12,11),Zone.GetEntityCell(sled));Assert.False(Pack.Objects.Contains(sled));
            Assert.IsNull(load.GetPart("SledLoad"));Assert.IsNull(Link(sled,"CargoSled","Load"));
            Assert.True(sled.GetPart<PhysicsPart>().Takeable,"rollback leaves an independently moved sled ordinarily recoverable");
        }
        [Test] public void RecoveryRefusesPhantomOriginalBesideARealSameIdOwner()
        {
            var load=Load();var sled=Sled();Fit(sled);Entity alias=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>
            {
                Assert.True(Pack.RemoveObject(sled));alias=Carry("CargoSled");alias.ID=sled.ID;
                sled.GetPart<PhysicsPart>().InInventory=Actor;
            }});
            Assert.False(Act(sled,Choice(sled,"TakeCargoSled|")));
            Assert.True(Pack.Objects.Contains(alias));Assert.False(Pack.Objects.Contains(sled));
            Assert.IsNull(Zone.GetEntityCell(sled));
        }
    }
}
