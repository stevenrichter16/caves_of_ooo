using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public class GameAuditDecodeIsolationAdversarialTests
    {
        private static int Marker(byte[] bytes,string name)
        {
            byte[] marker;using(var s=new MemoryStream()){new SaveWriter(s).WriteCheck(name);marker=s.ToArray();}
            var offsets=new List<int>();for(int i=0;i<=bytes.Length-4;i++)if(bytes.Skip(i).Take(4).SequenceEqual(marker))offsets.Add(i);
            Assert.AreEqual(1,offsets.Count,"Unique section marker: "+name);return offsets[0];
        }
        [Test]
        public void TruncatedHeaderLeavesEveryLiveQueueFieldUntouched()
        {using(var f=new DecodeIsolationFixture()){Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(f.Bytes,"header")));f.AssertLiveUnchanged();}}
        [TestCase("EntityBodies.Begin")] [TestCase("EntityBodies.End")]
        public void BadEntitySectionCheckNeverRunsHooksOrPublishes(string section)
        {using(var f=new DecodeIsolationFixture(observers:true)){var bytes=(byte[])f.Bytes.Clone();bytes[Marker(bytes,section)]^=0xff;Assert.Catch(()=>DecodeIsolationFixture.Decode(bytes));f.AssertLiveUnchanged();CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);}}
        [Test]
        public void TruncatedReputationDoesNotPublishPreviouslyParsedMessages()
        {using(var f=new DecodeIsolationFixture()){int at=Marker(f.Bytes,"PlayerReputation");Assert.Greater(BitConverter.ToInt32(f.Bytes,at+4),0);Assert.Catch(()=>DecodeIsolationFixture.Decode(f.Bytes.Take(at+9).ToArray()));f.AssertLiveUnchanged();}}
        [TestCase("version")] [TestCase("late-body")]
        public void CompressedLateOrVersionFailureKeepsFullLiveState(string kind)
        {using(var f=new DecodeIsolationFixture()){f.WriteQuick(DecodeIsolationFixture.Damage(f.Bytes,kind));LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Load.*failed"));Assert.IsFalse(SaveGameService.QuickLoad());f.AssertLiveUnchanged();}}
        [Test]
        public void NongzipFileRefusesBeforeApplyingAnything()
        {using(var f=new DecodeIsolationFixture()){f.WriteQuick(f.Bytes,false);LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Load.*failed"));Assert.IsFalse(SaveGameService.QuickLoad());f.AssertLiveUnchanged();}}
        [Test]
        public void ValidNoAuraStateClearsOldTransientQueues()
        {using(var f=new DecodeIsolationFixture(aura:false)){f.AssertPublished(DecodeIsolationFixture.Decode(f.Bytes),aura:false);}}
        [TestCase(false)] [TestCase(true)]
        public void LoadedAuraUsesBrainZoneOrActiveFallback(bool otherZone)
        {
            using(var f=new DecodeIsolationFixture())
            {
                var active=f.Saved.ZoneManager.ActiveZone;var npc=active.GetCell(7,8).Objects.Single(o=>o.ID=="saved-npc");var brain=new BrainPart();npc.AddPart(brain);
                var other=new Zone("Overworld.12.12.0");
                if(otherZone){active.RemoveEntity(npc);Assert.IsTrue(other.AddEntity(npc,6,6));brain.CurrentZone=other;}
                f.Saved.ZoneManager.ReplaceLoadedState(new Dictionary<string,Zone>{{active.ZoneID,active},{other.ZoneID,other}},active.ZoneID,new Dictionary<string,List<ZoneConnection>>());
                var loaded=DecodeIsolationFixture.Decode(f.EncodeSaved());var requests=AsciiFxBus.Drain();Assert.AreEqual(1,requests.Count);var zone=otherZone?loaded.ZoneManager.CachedZones[other.ZoneID]:loaded.ZoneManager.ActiveZone;
                Assert.AreSame(zone,requests[0].Zone);Assert.AreEqual("saved-npc",requests[0].Anchor.ID);Assert.AreSame(zone.GetCell(otherZone?6:7,otherZone?6:8).Objects.Single(o=>o.ID=="saved-npc"),requests[0].Anchor);
                foreach(var request in requests)AsciiFxBus.Release(request);
            }
        }
        [Test]
        public void StandaloneInventoryGraphRestoresOwnedReferences()
        {
            using(var f=new DecodeIsolationFixture())
            {
                var actor=new Entity{ID="owner"};var inventory=new InventoryPart();actor.AddPart(inventory);var item=new Entity{ID="carried"};item.AddPart(new PhysicsPart{Takeable=true});Assert.IsTrue(inventory.AddObject(item));
                using(var stream=new MemoryStream()){var writer=new SaveWriter(stream);writer.WriteEntityReference(actor);writer.WriteQueuedEntityBodies();stream.Position=0;var reader=new SaveReader(stream,null);var loaded=reader.ReadEntityReference();reader.ReadEntityBodies();var carried=loaded.GetPart<InventoryPart>().Objects.Single();Assert.AreEqual("carried",carried.ID);Assert.AreSame(loaded,carried.GetPart<PhysicsPart>().InInventory);Assert.IsNull(carried.GetPart<PhysicsPart>().Equipped);}
            }
        }
        [Test]
        public void RepeatedFailureThenValidRecoveryDoesNotAccumulatePublication()
        {using(var f=new DecodeIsolationFixture()){foreach(string kind in new[]{"footer","magic"}){Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(f.Bytes,kind)));f.AssertLiveUnchanged();}f.AssertPublished(DecodeIsolationFixture.Decode(f.Bytes));}}
        [Test]
        public void ExistingTrailingByteCompatibilityRemainsAccepted()
        {using(var f=new DecodeIsolationFixture()){f.AssertPublished(DecodeIsolationFixture.Decode(f.Bytes.Concat(new byte[]{7,8,9,10}).ToArray()));}}
        [Test]
        public void EmptySavedLogAndReputationClearNonemptyLiveValues()
        {using(var f=new DecodeIsolationFixture()){MessageLog.Restore(null,null,0,0);PlayerReputation.Restore(null);byte[] bytes=DecodeIsolationFixture.Serialize(f.Saved);DecodeIsolationFixture.InstallGlobals("live",33);DecodeIsolationFixture.Decode(bytes);CollectionAssert.IsEmpty(MessageLog.GetAllEntries());CollectionAssert.IsEmpty(MessageLog.GetPendingAnnouncementsSnapshot());Assert.AreEqual(0,MessageLog.FlashStamp);Assert.AreEqual(0,MessageLog.NextSerialValue);CollectionAssert.IsEmpty(PlayerReputation.GetAll());}}
        [Test]
        public void LoadedSettlementContentsReplaceLiveContentsWithoutMutatingOldManager()
        {
            using(var f=new DecodeIsolationFixture())
            {
                var saved=new SettlementState{SettlementId="site-audit",SettlementName="saved",LastAdvancedTurn=11};saved.SetSite(new RepairableSiteState{SiteId="well-audit",Stage=RepairStage.StableRepair,Severity=2});saved.SetCondition("saved-condition",true);saved.AddPendingMessage("saved-pending");f.Saved.ZoneManager.SettlementManager.RestoreSettlements(new Dictionary<string,SettlementState>{{saved.SettlementId,saved}});
                var live=new SettlementState{SettlementId="site-audit",SettlementName="live",LastAdvancedTurn=33};live.SetSite(new RepairableSiteState{SiteId="well-audit",Stage=RepairStage.Fouled,Severity=4});f.Live.ZoneManager.SettlementManager.RestoreSettlements(new Dictionary<string,SettlementState>{{live.SettlementId,live}});
                byte[] bytes=f.EncodeSaved();Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(bytes,"footer")));f.AssertLiveUnchanged();Assert.AreEqual(RepairStage.Fouled,SettlementManager.Current.GetSite("site-audit","well-audit").Stage);
                var loaded=DecodeIsolationFixture.Decode(bytes);Assert.AreSame(loaded.ZoneManager.SettlementManager,SettlementManager.Current);var result=SettlementManager.Current.GetAllSettlementsSnapshot()["site-audit"];Assert.AreEqual("saved",result.SettlementName);Assert.AreEqual(11,result.LastAdvancedTurn);Assert.AreEqual(RepairStage.StableRepair,result.GetSite("well-audit").Stage);Assert.IsTrue(result.HasCondition("saved-condition"));CollectionAssert.AreEqual(new[]{"saved-pending"},result.GetPendingMessagesSnapshot());Assert.AreEqual(RepairStage.Fouled,live.GetSite("well-audit").Stage);
            }
        }
        [Test]
        public void SavedStatsAndCooldownDoNotTickDuringFinalization()
        {using(var f=new DecodeIsolationFixture()){f.Saved.Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).CooldownRemaining=8;f.Saved.Player.Statistics["Speed"].BaseValue=87;var loaded=DecodeIsolationFixture.Decode(f.EncodeSaved());Assert.AreEqual(87,loaded.Player.GetStatValue("Speed"));Assert.AreEqual(8,loaded.Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).CooldownRemaining);Assert.AreEqual(11,loaded.TurnManager.TickCount);Assert.AreEqual(1100,loaded.TurnManager.GetEnergy(loaded.Player));}}
        [TestCase(true)] [TestCase(false)]
        public void OneMissingManagerLeavesOnlyThatExistingGlobalAlone(bool missingTurns)
        {using(var f=new DecodeIsolationFixture()){var oldTurns=TurnManager.Active;var oldSettlements=SettlementManager.Current;if(missingTurns)f.Saved.TurnManager=null;else f.Saved.ZoneManager=null;var loaded=DecodeIsolationFixture.Decode(f.EncodeSaved());if(missingTurns){Assert.AreSame(oldTurns,TurnManager.Active);Assert.AreSame(loaded.ZoneManager.SettlementManager,SettlementManager.Current);}else{Assert.AreSame(loaded.TurnManager,TurnManager.Active);Assert.AreSame(oldSettlements,SettlementManager.Current);}}}
        [Test]
        public void MessageRestoreDoesNotRefireMessageAddedNotifications()
        {using(var f=new DecodeIsolationFixture()){var previous=MessageLog.OnMessage;int calls=0;try{MessageLog.OnMessage=_=>calls++;MessageLog.Add("positive-probe");Assert.AreEqual(1,calls);calls=0;DecodeIsolationFixture.Decode(f.Bytes);Assert.AreEqual(0,calls);f.AssertGlobals("saved",11);}finally{MessageLog.OnMessage=previous;}}}
        [Test]
        public void StandaloneTurnLoaderRetainsImmediateActivation()
        {using(var f=new DecodeIsolationFixture()){using(var stream=new MemoryStream()){SaveGraphSerializer.SaveTurnManager(f.Saved.TurnManager,new SaveWriter(stream));stream.Position=0;var loaded=SaveGraphSerializer.LoadTurnManager(new SaveReader(stream,null));Assert.AreSame(loaded,TurnManager.Active);Assert.AreEqual(11,loaded.TickCount);}}}
        [Test]
        public void StandaloneMessageLoaderRetainsImmediatePublication()
        {using(var f=new DecodeIsolationFixture()){using(var stream=new MemoryStream()){DecodeIsolationFixture.InstallGlobals("saved",11);SaveGraphSerializer.SaveMessageLog(new SaveWriter(stream));DecodeIsolationFixture.InstallGlobals("live",33);stream.Position=0;SaveGraphSerializer.LoadMessageLog(new SaveReader(stream,null));Assert.AreEqual("saved-two",MessageLog.GetLast());Assert.AreEqual(16,MessageLog.NextSerialValue);Assert.AreEqual(33,PlayerReputation.Get("live-faction"));}}}
        [Test]
        public void StandaloneReputationLoaderRetainsImmediatePublication()
        {using(var f=new DecodeIsolationFixture()){using(var stream=new MemoryStream()){DecodeIsolationFixture.InstallGlobals("saved",11);SaveGraphSerializer.SavePlayerReputation(new SaveWriter(stream));DecodeIsolationFixture.InstallGlobals("live",33);stream.Position=0;SaveGraphSerializer.LoadPlayerReputation(new SaveReader(stream,null));Assert.AreEqual(11,PlayerReputation.Get("saved-faction"));Assert.AreEqual(0,PlayerReputation.Get("live-faction"));Assert.AreEqual("live-two",MessageLog.GetLast());}}}
        // Post-review player-flow and compatibility hypotheses, all bounded controls.
        [TestCase(true)] [TestCase(false)]
        public void SavedSiteRemainsAuthoritativeWithOrWithoutPoi(bool village)
        {using(var f=new DecodeIsolationFixture()){string id=SettlementSiteDefinitions.StartingVillageZoneId;var (x,y,z)=WorldMap.FromZoneID(id);f.Saved.ZoneManager.WorldMap.SetPOI(x,y,village?new PointOfInterest(POIType.Village,"Different map name","Villagers",1):null);var state=new SettlementState{SettlementId=id,SettlementName="Recorded name"};state.SetSite(new RepairableSiteState{SiteId=SettlementSiteDefinitions.MainWellSiteId,Stage=RepairStage.ImprovedWithCaretaker,Severity=8});f.Saved.ZoneManager.SettlementManager.RestoreSettlements(new Dictionary<string,SettlementState>{{id,state}});var loaded=DecodeIsolationFixture.Decode(f.EncodeSaved());var site=SettlementManager.Current.GetSite(id,SettlementSiteDefinitions.MainWellSiteId);Assert.NotNull(site);Assert.AreEqual(RepairStage.ImprovedWithCaretaker,site.Stage);Assert.AreEqual(8,site.Severity);Assert.AreEqual("Recorded name",loaded.ZoneManager.SettlementManager.GetAllSettlementsSnapshot()[id].SettlementName);}}
        [TestCase(true)] [TestCase(false)]
        public void OrdinaryCachedZoneEntryAlreadyInitializesKnownVillage(bool village)
        {using(var f=new DecodeIsolationFixture()){string id=SettlementSiteDefinitions.StartingVillageZoneId;var (x,y,z)=WorldMap.FromZoneID(id);var manager=f.Saved.ZoneManager;manager.WorldMap.SetPOI(x,y,village?new PointOfInterest(POIType.Village,"Entry Sill","Villagers",1):null);var zone=new Zone(id);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{id,zone}},id,new Dictionary<string,List<ZoneConnection>>());Assert.AreEqual(0,manager.SettlementManager.GetAllSettlementsSnapshot().Count);Assert.AreSame(zone,manager.GetZone(id));var snapshot=manager.SettlementManager.GetAllSettlementsSnapshot();if(village){Assert.IsTrue(snapshot.ContainsKey(id));Assert.AreEqual("Entry Sill",snapshot[id].SettlementName);Assert.NotNull(snapshot[id].GetSite(SettlementSiteDefinitions.MainWellSiteId));}else Assert.IsFalse(snapshot.ContainsKey(id));}}
        [Test]
        public void OtherVillageStillHasNoStartingTownWell()
        {using(var f=new DecodeIsolationFixture()){string id="Overworld.11.11.0";Assert.AreNotEqual(SettlementSiteDefinitions.StartingVillageZoneId,id);f.Saved.ZoneManager.WorldMap.SetPOI(11,11,new PointOfInterest(POIType.Village,"Other town","Villagers",1));var loaded=DecodeIsolationFixture.Decode(f.EncodeSaved());Assert.IsNull(SettlementManager.Current.GetSite(id,SettlementSiteDefinitions.MainWellSiteId));Assert.AreEqual("Other town",loaded.ZoneManager.SettlementManager.GetAllSettlementsSnapshot()[id].SettlementName);}}
        [TestCase("messages")] [TestCase("reputation")]
        public void StandalonePartialPayloadDoesNotPublish(string kind)
        {using(var f=new DecodeIsolationFixture()){using(var stream=new MemoryStream()){new SaveWriter(stream).Write(1);stream.Position=0;var reader=new SaveReader(stream,null);if(kind=="messages")Assert.Catch(()=>SaveGraphSerializer.LoadMessageLog(reader));else Assert.Catch(()=>SaveGraphSerializer.LoadPlayerReputation(reader));f.AssertLiveUnchanged();}}}
        [TestCase(true)] [TestCase(false)]
        public void LoadedManagerResolvesUnrecordedStartingVillageFromLoadedMap(bool village)
        {
            using(var f=new DecodeIsolationFixture())
            {
                string id=SettlementSiteDefinitions.StartingVillageZoneId;var (x,y,z)=WorldMap.FromZoneID(id);f.Saved.ZoneManager.WorldMap.SetPOI(x,y,village?new PointOfInterest(POIType.Village,"Loaded Sill","Villagers",1):null);
                Assert.AreEqual(0,f.Saved.ZoneManager.SettlementManager.GetAllSettlementsSnapshot().Count);var loaded=DecodeIsolationFixture.Decode(f.EncodeSaved());var site=SettlementManager.Current.GetSite(id,SettlementSiteDefinitions.MainWellSiteId);
                if(village){Assert.NotNull(site);Assert.AreEqual(RepairStage.Fouled,site.Stage);Assert.AreEqual("Loaded Sill",loaded.ZoneManager.SettlementManager.GetAllSettlementsSnapshot()[id].SettlementName);}else Assert.IsNull(site);
            }
        }
    }
}
