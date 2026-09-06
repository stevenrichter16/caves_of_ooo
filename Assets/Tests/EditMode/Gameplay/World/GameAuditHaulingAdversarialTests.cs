using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    // Dedicated A09 taxonomy: lifecycle, identity, callbacks, load graph and caller integration.
    public class GameAuditHaulingAdversarialTests
    {
        static Entity Wall() {var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.Tags["Solid"]="";e.AddPart(new PhysicsPart{Solid=true});return e;}
        [TestCase(false)] [TestCase(true)]
        public void BlockedActorMovementPreservesGripButBlockedVacatedCellSlips(bool vacated)
        {using(var f=new HaulLifecycleFixture()){Assert.IsTrue(f.Zone.AddEntity(Wall(),vacated?5:4,5));Assert.AreEqual(vacated,f.Move(false));if(vacated)f.Released();else f.Healthy();Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));}}
        [TestCase(false)] [TestCase(true)]
        public void FirstPlacementOrSameCellDoesNotStackLoad(bool first)
        {using(var f=new HaulLifecycleFixture()){if(first){HaulLifecycleFixture.RawRemove(f.Zone,f.Actor);Assert.IsTrue(MovementSystem.ForceMoveTo(f.Actor,f.Zone,5,5));}else Assert.IsTrue(f.Move(true,5,5));f.Healthy();Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));}}
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void RealHorizontalAndVerticalTravelReleasesOnlyAfterSuccessfulTransfer(bool vertical,bool blocked)
        {using(var f=new HaulLifecycleFixture()){
            var target=vertical?new Zone("Overworld.10.10.1"):f.Other;var manager=new ZoneManager(null);manager.CachedZones[f.Zone.ZoneID]=f.Zone;manager.CachedZones[target.ZoneID]=target;
            if(blocked)for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)Assert.IsTrue(target.AddEntity(Wall(),x,y));
            var result=vertical?ZoneTransitionSystem.TransitionPlayerVertical(f.Actor,f.Zone,true,5,5,manager):ZoneTransitionSystem.TransitionPlayer(f.Actor,f.Zone,TransitionDirection.East,5,5,manager,null);
            Assert.AreEqual(!blocked,result.Success);Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));if(blocked){f.Healthy();Assert.AreEqual((5,5),f.Zone.GetEntityPosition(f.Actor));}else{f.Released();Assert.IsNull(f.Zone.GetEntityCell(f.Actor));Assert.IsNotNull(target.GetEntityCell(f.Actor));}
        }}
        [Test]
        public void RealCombatDeathReleasesOnceAndLeavesAvailableBarrel()
        {using(var f=new HaulLifecycleFixture()){CombatSystem.ApplyDamage(f.Actor,20,null,f.Zone);Assert.LessOrEqual(f.Actor.GetStat("Hitpoints").BaseValue,0);Assert.IsNull(f.Zone.GetEntityCell(f.Actor));f.Released();Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));Assert.IsFalse(DragSystem.Release(f.Actor));}}
        [TestCase(false)] [TestCase(true)]
        public void CommittedGoneActorOrLoadCannotFollowEvenBeforeRemoval(bool actor)
        {using(var f=new HaulLifecycleFixture()){var e=actor?f.Actor:f.Load;if(actor)e.AddPart(new DestructiblePart());e.GetPart<DestructiblePart>().Gone=true;Assert.IsTrue(f.Move(true));f.Released();Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));}}
        [Test]
        public void MissingHitpointsRemainsValidForLightweightHauler()
        {using(var f=new HaulLifecycleFixture()){f.Actor.Statistics.Remove("Hitpoints");Assert.IsTrue(f.Move(false));f.Healthy();}}
        [TestCase(false)] [TestCase(true)]
        public void CreatureDeathUsesBaseHpRatherThanModifiedDisplay(bool deadBase)
        {using(var f=new HaulLifecycleFixture()){var hp=f.Actor.GetStat("Hitpoints");hp.BaseValue=deadBase?0:20;hp.Bonus=deadBase?20:0;hp.Penalty=deadBase?0:20;Assert.AreEqual(deadBase?20:0,hp.Value);Assert.IsTrue(f.Move(true));if(deadBase)f.Released();else f.Healthy();}}
        [TestCase(false)] [TestCase(true)]
        public void RebuildRepairsOrphanInverseWithoutStealingDifferentHealthyLoad(bool namedActor)
        {using(var f=new HaulLifecycleFixture()){var stale=HaulLifecycleFixture.MakeLoad();Assert.IsTrue(f.Zone.AddEntity(stale,8,5));stale.AddPart(new DraggedPart{Dragger=namedActor?f.Actor:null});var state=new GameSessionState{Player=f.Actor,ZoneManager=new OverworldZoneManager(null,333)};state.ZoneManager.CachedZones[f.Zone.ZoneID]=f.Zone;SaveGraphSerializer.RebuildLoadedWorld(state);Assert.IsNull(stale.GetPart<DraggedPart>());f.Healthy();}}
        [TestCase(false)] [TestCase(true)]
        public void SessionLoadRepairsNullPointerOnEitherSide(bool nullInverse)
        {using(var f=new HaulLifecycleFixture()){if(nullInverse)f.Load.GetPart<DraggedPart>().Dragger=null;else f.Actor.GetPart<DragPart>().Dragged=null;var loaded=f.RoundTrip();Assert.IsNull(loaded.Player.GetPart<DragPart>());Assert.AreEqual(100,loaded.Player.GetStatValue("Speed"));foreach(var e in loaded.ZoneManager.ActiveZone.GetReadOnlyEntities())if(e.ID==f.Load.ID)Assert.IsNull(e.GetPart<DraggedPart>());}}
        [Test]
        public void ManualRebuildWithoutManagerCleansUnplacedPlayer()
        {using(var f=new HaulLifecycleFixture()){SaveGraphSerializer.RebuildLoadedWorld(new GameSessionState{Player=f.Actor});f.Released();}}
        [Test]
        public void FullSessionRepairsWhollyUnplacedNonPlayerTokenPair()
        {using(var f=new HaulLifecycleFixture()){DragSystem.Release(f.Actor);var npc=HaulLifecycleFixture.MakeActor("unplacedNPC");Assert.IsTrue(f.Zone.AddEntity(npc,5,6));Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(npc,f.Load,f.Zone));HaulLifecycleFixture.RawRemove(f.Zone,npc);HaulLifecycleFixture.RawRemove(f.Zone,f.Load);var inv=new InventoryPart();f.Actor.AddPart(inv);inv.Objects.Add(npc);var loaded=f.RoundTrip();var savedNpc=loaded.Player.GetPart<InventoryPart>().Objects[0];Assert.IsNull(savedNpc.GetPart<DragPart>());Assert.AreEqual(100,savedNpc.GetStatValue("Speed"));}}
        [TestCase(false)] [TestCase(true)]
        public void RemovalMessageCallbackCanReinsertEndpointAndEstablishNewGrip(bool actor)
        {using(var f=new HaulLifecycleFixture()){var oldGrip=f.Actor.GetPart<DragPart>();bool called=false;MessageLog.OnMessage=_=>{if(called)return;called=true;Assert.IsTrue(f.Zone.AddEntity(actor?f.Actor:f.Load,actor?5:6,5));Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(f.Actor,f.Load,f.Zone));};Assert.IsTrue(f.Zone.RemoveEntity(actor?f.Actor:f.Load));Assert.IsTrue(called);Assert.AreNotSame(oldGrip,f.Actor.GetPart<DragPart>());Assert.AreEqual(0,oldGrip.AppliedPenalty);f.Healthy();}}
        [TestCase(false)] [TestCase(true)]
        public void CleanupDiagnosticDistinguishesRemovalFromInvalidMovement(bool removal)
        {using(var f=new HaulLifecycleFixture()){bool prior=Diag.IsChannelEnabled("drag");Diag.SetChannel("drag",true);try{int Count(string kind)=>DiagQuery.Count(new DiagQuery.Filter{Category="drag",Kind=kind,Actor=f.Actor.ID,Target=f.Load.ID}).Count;int released=Count("Released"),slipped=Count("Slipped");if(removal)f.Zone.RemoveEntity(f.Load);else{HaulLifecycleFixture.RawRemove(f.Zone,f.Load);f.Move(false);}Assert.AreEqual(released+(removal?1:0),Count("Released"));Assert.AreEqual(slipped+(removal?0:1),Count("Slipped"));f.Released();}finally{Diag.SetChannel("drag",prior);}}}
        [Test]
        public void NewUnrelatedPenaltyDuringHaulSurvivesRelease()
        {using(var f=new HaulLifecycleFixture()){f.Actor.GetStat("Speed").Penalty+=9;f.Zone.RemoveEntity(f.Load);Assert.AreEqual(91,f.Actor.GetStatValue("Speed"));Assert.AreEqual(9,f.Actor.GetStat("Speed").Penalty);}}
        // Hypothesis: cleanup callbacks can grab a new load; the completed move
        // must not pull that new load, but the next movement must still follow.
        [TestCase(false)] [TestCase(true)]
        public void CleanupCallbackNewGripDoesNotConsumeTheOldMove(bool regrab)
        {using(var f=new HaulLifecycleFixture()){
            var next=HaulLifecycleFixture.MakeLoad();Assert.IsTrue(f.Zone.AddEntity(next,4,6));HaulLifecycleFixture.RawRemove(f.Zone,f.Load);
            bool called=false;MessageLog.OnMessage=_=>{if(called)return;called=true;if(regrab)Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(f.Actor,next,f.Zone));};
            Assert.IsTrue(f.Move(false));Assert.IsTrue(called);Assert.IsNull(f.Zone.GetEntityCell(f.Load));Assert.AreEqual((4,6),f.Zone.GetEntityPosition(next));
            if(regrab){Assert.AreSame(next,DragSystem.GetDragged(f.Actor));Assert.AreEqual(70,f.Actor.GetStatValue("Speed"));Assert.IsTrue(f.Move(false,3,5));Assert.AreEqual((4,5),f.Zone.GetEntityPosition(next));}
            else{Assert.IsNull(f.Actor.GetPart<DragPart>());Assert.AreEqual(100,f.Actor.GetStatValue("Speed"));}
        }}
        [Test]
        public void CleanupCallbackNestedRealMovementFollowsNewGripOnlyForNestedEvent()
        {using(var f=new HaulLifecycleFixture()){
            var next=HaulLifecycleFixture.MakeLoad();Assert.IsTrue(f.Zone.AddEntity(next,4,6));HaulLifecycleFixture.RawRemove(f.Zone,f.Load);
            bool called=false;MessageLog.OnMessage=_=>{if(called)return;called=true;Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(f.Actor,next,f.Zone));Assert.IsTrue(f.Move(true,3,5));};
            Assert.IsTrue(f.Move(false));Assert.IsTrue(called);Assert.AreEqual((3,5),f.Zone.GetEntityPosition(f.Actor));Assert.AreEqual((4,5),f.Zone.GetEntityPosition(next));Assert.AreSame(next,DragSystem.GetDragged(f.Actor));Assert.AreEqual(70,f.Actor.GetStatValue("Speed"));Assert.IsNull(f.Zone.GetEntityCell(f.Load));
        }}
        [TestCase(false)] [TestCase(true)]
        public void LoadedRepairMessagesUseLoadedClockAndPreserveHistoricalTimestamps(bool broken)
        {using(var f=new HaulLifecycleFixture()){
            Func<int> oldClock=()=>222;MessageLog.TickProvider=oldClock;MessageLog.Clear();MessageLog.Add("historical marker");if(broken)HaulLifecycleFixture.RawRemove(f.Zone,f.Load);
            var loaded=f.RoundTrip();var entries=MessageLog.GetAllEntries();Assert.AreEqual(broken?2:1,entries.Count);Assert.AreEqual("historical marker",entries[0].Text);Assert.AreEqual(222,entries[0].Tick);
            if(broken){Assert.IsTrue(entries[1].Text.Contains("slips from your grip"));Assert.AreEqual(loaded.TurnManager.TickCount,entries[1].Tick);Assert.AreEqual(17,entries[1].Tick);}
            Assert.AreSame(oldClock,MessageLog.TickProvider);
        }}
        [Test]
        public void RebuildWithoutSavedClockUsesZeroOnlyWithinRepair()
        {using(var f=new HaulLifecycleFixture()){
            Func<int> oldClock=()=>222;MessageLog.TickProvider=oldClock;MessageLog.Clear();SaveGraphSerializer.RebuildLoadedWorld(new GameSessionState{Player=f.Actor});Assert.AreEqual(0,MessageLog.GetRecentTicks(1)[0]);Assert.AreSame(oldClock,MessageLog.TickProvider);f.Released();
        }}
        [Test]
        public void ThrowingRepairObserverCannotLeakTemporaryClockProvider()
        {using(var f=new HaulLifecycleFixture()){
            Func<int> oldClock=()=>222;MessageLog.TickProvider=oldClock;HaulLifecycleFixture.RawRemove(f.Zone,f.Load);MessageLog.OnMessage=text=>{if(text.Contains("slips from your grip"))throw new InvalidOperationException("audit observer");};
            Assert.Throws<InvalidOperationException>(()=>f.RoundTrip());Assert.AreSame(oldClock,MessageLog.TickProvider);
        }}
        [Test]
        public void StaleFollowRequestCannotMoveOrReleaseDifferentCurrentLoad()
        {using(var f=new HaulLifecycleFixture()){var stale=HaulLifecycleFixture.MakeLoad();Assert.IsTrue(f.Zone.AddEntity(stale,7,5));typeof(DragSystem).GetMethod("FollowInto",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{f.Actor,stale,f.Zone,4,5});f.Healthy();Assert.AreEqual((7,5),f.Zone.GetEntityPosition(stale));Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));}}
    }
}
