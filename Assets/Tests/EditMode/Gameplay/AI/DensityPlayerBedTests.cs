using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityPlayerBedTests
    {
        Zone zone;Entity actor,bed;TurnManager clock,oldClock;Action<string> oldMessage;
        [SetUp]public void Setup()
        {
            oldClock=TurnManager.Active;zone=new Zone("BedTest");clock=new TurnManager();clock.AdvanceClock(105);FactionManager.Initialize();MessageLog.Clear();oldMessage=MessageLog.OnMessage;
            actor=new Entity{ID="player",BlueprintName="Player"};actor.SetTag("Player");actor.AddPart(new PhysicsPart());actor.AddPart(new InventoryPart());actor.AddPart(new StatusEffectsPart());
            actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=7,Min=0,Max=20};zone.AddEntity(actor,10,10);
            bed=new Entity{ID="bed",BlueprintName="Bed"};bed.AddPart(new PhysicsPart{Takeable=false,Solid=false});bed.AddPart(new BedPart());zone.AddEntity(bed,10,10);
        }
        [TearDown]public void Teardown(){MessageLog.OnMessage=oldMessage;FactionManager.Reset();WorldClock.ResetForTests();typeof(TurnManager).GetProperty("Active").SetValue(null,oldClock);}
        bool Act(string command="SleepOnBed",Entity who=null,Zone where=null,InventoryTransaction transaction=null)
        {
            var e=GameEvent.New("InventoryAction");e.SetParameter("Command",command);e.SetParameter("Actor",who??actor);e.SetParameter("Zone",where??zone);
            if(transaction!=null)e.SetParameter("InventoryTransaction",transaction);
            try{return !bed.FireEvent(e)||e.Handled;}finally{e.Release();}
        }
        void Unchanged(int tick=105,int hp=7){Assert.AreEqual(tick,clock.TickCount);Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));}
        Entity Npc(int x=11)
        {var npc=new Entity{ID="npc"};npc.SetTag("Creature");npc.SetTag("AllowIdleBehavior");npc.AddPart(new BrainPart{CurrentZone=zone,Rng=new System.Random(8)});zone.AddEntity(npc,x,10);return npc;}
        [Test]public void ActualBedOffersTwoWorldActionsAndExistingSavePartWorks()
        {
            var factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            var actual=factory.CreateEntity("Bed");Assert.NotNull(actual.GetPart<BedPart>());
            var commands=WorldInteractionSystem.GatherActions(actual,actor).Select(a=>a.Command).ToArray();Assert.Contains("SleepOnBed",commands);Assert.Contains("SleepUntilNextBand",commands);
            Assert.IsFalse(WorldInteractionSystem.GatherActions(actual).Any(a=>a.Command.StartsWith("Sleep")));
        }
        [Test]public void FreeBedHealsCuresBleedingAndCostsSixtyClockTicksWithoutInnBuff()
        {
            actor.ApplyEffect(new BleedingEffect());Assert.True(actor.HasEffect<BleedingEffect>());
            Assert.True(Act());Assert.AreEqual(20,actor.GetStatValue("Hitpoints"));Assert.AreEqual(165,clock.TickCount);Assert.False(actor.HasEffect<BleedingEffect>());
            Assert.False(actor.HasEffect<WellRestedEffect>());Assert.False(bed.GetPart<BedPart>().Occupied);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
        [TestCase(105,300)][TestCase(300,600)][TestCase(1199,1200)]public void NextBandUsesSameBoundedClockRule(int start,int end)
        {clock=new TurnManager();clock.AdvanceClock(start);Assert.True(Act("SleepUntilNextBand"));Assert.AreEqual(end,clock.TickCount);Assert.AreEqual(20,actor.GetStatValue("Hitpoints"));Assert.False(bed.GetPart<BedPart>().Occupied);}
        [Test]public void RepeatedRestStillPaysTimeAndDoesNotGrantPersistentReservation()
        {Assert.True(Act());Assert.True(Act());Assert.AreEqual(225,clock.TickCount);Assert.False(bed.GetPart<BedPart>().Occupied);}
        [TestCase("",true)][TestCase("player",true)][TestCase("Guest",true)][TestCase("other",false)]public void AuthoredOwnershipIsRespected(string owner,bool allowed)
        {bed.GetPart<BedPart>().Owner=owner;actor.SetTag("Guest");Assert.AreEqual(allowed,Act());Assert.AreEqual(allowed?165:105,clock.TickCount);}
        [Test]public void ActualNpcOfferPreventsPlayerRestUntilReservationCleanup()
        {var npc=Npc();var offer=IdleQueryEvent.QueryOffer(bed,npc);Assert.NotNull(offer);Assert.True(bed.GetPart<BedPart>().Occupied);Assert.False(Act());Unchanged();Assert.True(bed.GetPart<BedPart>().Occupied);offer.Cleanup(null);Assert.True(Act());}
        [Test]public void PhysicalCreatureOccupantBlocksEvenWithFalseReservationFlag()
        {var npc=Npc(10);Assert.False(bed.GetPart<BedPart>().Occupied);Assert.False(Act());Unchanged();zone.RemoveEntity(npc);Assert.True(Act());}
        [Test]public void ActualRestReservationPreventsReentrantNpcOfferAndSecondSleep()
        {
            var npc=Npc();bool observed=false;MessageLog.OnMessage=text=>{if(!text.StartsWith("You rest"))return;observed=true;Assert.True(bed.GetPart<BedPart>().Occupied);Assert.IsNull(IdleQueryEvent.QueryOffer(bed,npc));Assert.False(Act());};
            Assert.True(Act());Assert.True(observed);Assert.AreEqual(165,clock.TickCount);Assert.False(bed.GetPart<BedPart>().Occupied);
        }
        [TestCase(false)][TestCase(true)]public void DeadActorsCannotRestOrBeRevived(bool handled)
        {if(handled)actor.SetTag("_DeathHandled");else actor.GetStat("Hitpoints").BaseValue=0;Assert.False(Act());Unchanged(hp:handled?7:0);Assert.False(bed.GetPart<BedPart>().Occupied);}
        [TestCase(11)][TestCase(25)]public void AdjacentOrRemoteBedNeverTeleportsThePlayer(int x)
        {zone.MoveEntity(bed,x,10);Assert.False(Act());Unchanged();Assert.AreEqual((10,10),zone.GetEntityPosition(actor));}
        [TestCase("actor")][TestCase("bed")]public void RemovedOrForeignOwnerRefuses(string which)
        {var other=new Zone("Other");var e=which=="actor"?actor:bed;zone.RemoveEntity(e);Assert.False(Act());other.AddEntity(e,10,10);Assert.False(Act());Unchanged();}
        [Test]public void NonPlayerAndMissingHealthRefuse()
        {actor.Tags.Remove("Player");Assert.False(Act());actor.SetTag("Player");actor.Statistics.Remove("Hitpoints");Assert.False(Act());Assert.AreEqual(105,clock.TickCount);}
        [TestCase("takeable")][TestCase("carried")][TestCase("equipped")]public void PortableOrStaleBedOwnerCannotBecomeRestSite(string shape)
        {var p=bed.GetPart<PhysicsPart>();if(shape=="takeable")p.Takeable=true;else if(shape=="carried")p.InInventory=actor;else p.Equipped=actor;Assert.False(Act());Unchanged();}
        [Test]public void OuterInventoryTransactionRefusesBeforeIrreversibleClockOrHealthMutation()
        {var transaction=new InventoryTransaction();try{Assert.False(Act(transaction:transaction));Unchanged();Assert.AreEqual(0,transaction.StepCount);}finally{transaction.Rollback();}Assert.True(Act());}
        [Test]public void HostileWithinEightBlocksButBeyondEightAllowsRest()
        {var hostile=Npc(18);hostile.SetTag("Faction","OutlandRaiders");Assert.True(FactionManager.IsHostile(hostile,actor));Assert.False(Act());Unchanged();Assert.False(bed.GetPart<BedPart>().Occupied);zone.MoveEntity(hostile,19,10);Assert.True(Act());}
        [Test]public void SittingElsewhereMustBeReleasedBeforeSleep()
        {actor.ApplyEffect(new SittingEffect());Assert.False(Act());Unchanged();actor.RemoveEffect<SittingEffect>();Assert.True(Act());}
        [TestCase("SleepOnBed")][TestCase("SleepUntilNextBand")]public void OverflowingClockRefusesWithoutHealing(string command)
        {clock=new TurnManager();clock.AdvanceClock(int.MaxValue-5);Assert.False(Act(command));Unchanged(int.MaxValue-5);}
        [Test]public void UnknownCommandDoesNotReserveOrHeal()
        {Assert.False(Act("SleepInSomeoneElsesBed"));Unchanged();Assert.False(bed.GetPart<BedPart>().Occupied);}
        [Test]public void MissingClockRefusesAndExactPreviousClockIsRestored()
        {
            var active=typeof(TurnManager).GetProperty("Active");var previous=TurnManager.Active;
            try{active.SetValue(null,null);Assert.False(Act());Assert.False(Act("SleepUntilNextBand"));Unchanged();}
            finally{active.SetValue(null,previous);}
            Assert.True(Act());
        }
        [Test]public void MissingActorOrZoneRefusesWithoutReservation()
        {Assert.False(PlayerBedService.TryAct(null,bed,zone,"SleepOnBed"));Assert.False(PlayerBedService.TryAct(actor,bed,null,"SleepOnBed"));Unchanged();Assert.False(bed.GetPart<BedPart>().Occupied);}
        [Test]public void ExactClockLimitSucceedsAndNextRestRefuses()
        {clock=new TurnManager();clock.AdvanceClock(int.MaxValue-60);Assert.True(Act());Assert.AreEqual(int.MaxValue,clock.TickCount);Assert.False(Act());Assert.AreEqual(int.MaxValue,clock.TickCount);}
        [Test]public void ReentrantSecondBedCannotAdvanceTheSameActorAgain()
        {
            var second=new Entity{ID="second"};second.AddPart(new BedPart());second.AddPart(new PhysicsPart());zone.AddEntity(second,10,10);
            bool observed=false;MessageLog.OnMessage=text=>{if(!text.StartsWith("You rest"))return;observed=true;Assert.False(PlayerBedService.TryAct(actor,second,zone,"SleepOnBed"));};
            Assert.True(Act());Assert.True(observed);Assert.AreEqual(165,clock.TickCount);Assert.False(second.GetPart<BedPart>().Occupied);
        }
        [Test]public void ThrowingLegacyRestObserverReleasesOnlyOwnedReservationAndClaims()
        {
            MessageLog.OnMessage=text=>{if(text.StartsWith("You rest"))throw new InvalidOperationException("observer");};
            Assert.Throws<InvalidOperationException>(()=>Act());Assert.False(bed.GetPart<BedPart>().Occupied);Assert.AreEqual(165,clock.TickCount);
            MessageLog.OnMessage=null;Assert.True(Act());Assert.AreEqual(225,clock.TickCount);
        }
        [Test]public void OuterActorClaimPreventsUnrelatedDirectSleepUntilReleased()
        {
            var transaction=new InventoryTransaction();
            var claim=typeof(InventoryTransaction).GetMethod("TryClaim",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(claim);Assert.True((bool)claim.Invoke(transaction,new object[]{actor,actor,"other"}));
            try{Assert.False(Act());Unchanged();Assert.False(bed.GetPart<BedPart>().Occupied);}finally{transaction.Rollback();}
            Assert.True(Act());
        }
        [TestCase(false)][TestCase(true)]public void ExistingBedSaveGraphKeepsOwnerAndReservation(bool occupied)
        {
            bed.GetPart<BedPart>().Owner="Guest";bed.GetPart<BedPart>().Occupied=occupied;
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(bed);zone.RemoveEntity(bed);bed=saved;zone.AddEntity(bed,10,10);actor.SetTag("Guest");
            Assert.AreEqual("Guest",bed.GetPart<BedPart>().Owner);Assert.AreEqual(occupied,bed.GetPart<BedPart>().Occupied);
            Assert.AreEqual(!occupied,Act());Assert.AreEqual(occupied?105:165,clock.TickCount);
        }
        [TestCase("npc-owner")][TestCase("Guest")]public void ChairOwnerStringAlsoSurvivesTheSharedSaveFieldFilter(string owner)
        {
            var chair=new Entity{ID="owned-chair"};chair.AddPart(new ChairPart{Owner=owner,Occupied=true});
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(chair);
            Assert.AreEqual(owner,loaded.GetPart<ChairPart>().Owner);Assert.True(loaded.GetPart<ChairPart>().Occupied);
        }
        [Test]public void BedWithoutPhysicalPartRefusesRatherThanBecomingSyntheticHealingObject()
        {bed.RemovePart(bed.GetPart<PhysicsPart>());Assert.False(Act());Unchanged();}
        [Test]public void FailedHostileRestDoesNotRemoveBleedingOrCreateInnBuff()
        {var hostile=Npc();hostile.SetTag("Faction","OutlandRaiders");actor.ApplyEffect(new BleedingEffect());Assert.False(Act());Unchanged();Assert.True(actor.HasEffect<BleedingEffect>());Assert.False(actor.HasEffect<WellRestedEffect>());}
        [Test]public void ActualUnderfootBedRemainsReachableThroughTheWorldPicker()
        {
            var rows=WorldInteractionSystem.BuildPileSummaryActions(zone.GetCell(10,10),actor);
            Assert.True(rows.Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+bed.ID));
            var elsewhere=new Entity{ID="other-bed"};elsewhere.AddPart(new BedPart());zone.AddEntity(elsewhere,11,10);
            Assert.False(WorldInteractionSystem.BuildPileSummaryActions(zone.GetCell(11,10),actor).Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+elsewhere.ID));
        }
    }
}
