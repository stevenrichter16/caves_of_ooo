using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadExplorationEntryExpansionTests
    {
        DensityLootTestScope scope;
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();}
        [TearDown] public void Cleanup(){scope.Dispose();}
        OverworldZoneManager Fresh(int seed=3)=>OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
        static object Call(object owner,string method,params object[] args)
        {
            var m=typeof(SpreadExplorationPlan).GetMethods(Flags).SingleOrDefault(x=>x.Name==method&&x.GetParameters().Length==args.Length);
            Assert.NotNull(m,"Missing accepted-entry authority: "+method);
            try{return m.Invoke(owner,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
        }
        static SpreadExplorationEntry Exchange(OverworldZoneManager m)
        {var e=m.Exploration.Entries.FirstOrDefault(x=>x.Family.ToString()=="RoadsideExchange");Assert.NotNull(e,"Fresh v4 must assign ordinary roadside exchange opportunities.");return e;}
        static bool Accepted(OverworldZoneManager m,Zone z,out SpreadExplorationEntry entry)
        {object[] args={m,z,null};bool ok=(bool)Call(m.Exploration,"TryGetAcceptedEntry",args);entry=(SpreadExplorationEntry)args[2];return ok;}
        static bool Mark(OverworldZoneManager m,Zone z,SpreadExplorationEntry e,Func<bool> proof)=>(bool)Call(m.Exploration,"TryMarkAcceptedEntryCommitted",m,z,e,proof);
        static string Wire(OverworldZoneManager m)=>SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey);
        static void Restore(OverworldZoneManager m,string wire)
        {var w=new Entity();w.Properties[SpreadExplorationPlan.PropertyKey]=wire;var p=(SpreadExplorationPlan)Call(null,"Restore",m,w);typeof(OverworldZoneManager).GetProperty("Exploration",Flags).SetValue(m,p);}
        Zone Activate(OverworldZoneManager m,SpreadExplorationEntry e){var z=m.GetZone(e.ZoneID);Assert.NotNull(z);m.SetActiveZone(z);return z;}

        [TestCase(1)][TestCase(64)][TestCase(1729)] public void NewFamiliesAreFrozenColdAndHabitatSpecific(int seed)
        {
            var a=Fresh(seed);var b=Fresh(seed);Assert.AreEqual(9,a.Exploration.Version);Assert.Zero(a.CachedZoneCount);
            CollectionAssert.AreEqual(a.Exploration.Entries.Select(e=>e.ZoneID+e.Family),b.Exploration.Entries.Select(e=>e.ZoneID+e.Family));
            foreach(string f in new[]{"CollectorReturn","RoadsideExchange"})
            {
                var rows=a.Exploration.Entries.Where(e=>e.Family.ToString()==f).ToArray();if(f=="CollectorReturn")Assert.IsNotEmpty(rows,f);
                foreach(var e in rows){Assert.True(e.PlacementEligible);Assert.AreEqual(f=="CollectorReturn"?Formation.Hedgerow:Formation.OldRoad,FormationSelector.For(BiomeType.Spread,e.ZoneID));}
            }
            Assert.Zero(a.CachedZoneCount,"Reading new assignments cannot generate actors or stock.");
        }
        // The fixed worlds have no winning OldRoad traveller roll. Family assignment
        // cannot promise an actor that the source does not produce. Seed3 is the first
        // actual source in a prebounded exhaustive1..64 census, used by authority fixtures.
        [TestCase(1)][TestCase(64)][TestCase(1729)] public void MissingTravellerSourceLeavesOrdinaryRoadContent(int seed)
        {
            var m=Fresh(seed);
            var roads=m.Exploration.Entries.Where(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.OldRoad).ToArray();
            Assert.IsNotEmpty(roads);
            foreach(var e in roads)
            {
                Assert.AreNotEqual(0,LegacyTravellerSample(seed,e.ZoneID)%8,"Frozen source census changed.");
                Assert.That(e.Family.ToString(),Is.EqualTo("None").Or.EqualTo("RoadSpill"),e.ZoneID);
            }
            Assert.Zero(m.CachedZoneCount);
        }
        [Test] public void EveryAssignedExchangeHasAnActualStableArrivalWinnerAcrossBoundedCohort()
        {
            int positive=0,negative=0;
            for(int seed=1;seed<=64;seed++)
            {
                var m=Fresh(seed);
                foreach(var e in m.Exploration.Entries.Where(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.OldRoad&&e.Family.ToString()!="None"))
                {
                    bool winner=LegacyTravellerSample(seed,e.ZoneID)%8==0;
                    Assert.AreEqual(winner?"RoadsideExchange":"RoadSpill",e.Family.ToString(),seed+":"+e.ZoneID);
                    if(winner)positive++;else negative++;
                }
                Assert.Zero(m.CachedZoneCount);
            }
            Assert.Greater(positive,0);Assert.Greater(negative,0);
        }
        static uint LegacyTravellerSample(int seed,string id)
        {
            unchecked{uint h=2166136261;foreach(char c in seed.ToString(System.Globalization.CultureInfo.InvariantCulture)+":traveller:"+id)h=(h^c)*16777619;return h;}
        }
        [Test] public void OnlyExactActiveAcceptedGraphCanCommitOnce()
        {
            var m=Fresh();var e=Exchange(m);var z=Activate(m,e);Assert.AreEqual(1,m.Exploration.DispositionFor(e.ZoneID));
            Assert.True(Accepted(m,z,out var current));Assert.AreSame(e,current);Assert.True(Mark(m,z,e,()=>true));
            Assert.AreEqual(2,m.Exploration.DispositionFor(e.ZoneID));Assert.False(Accepted(m,z,out _));Assert.False(Mark(m,z,e,()=>true));
        }
        [Test] public void ColdGenerationCannotCommitTheDynamicExchangeReservation()
        {
            var m=Fresh();var e=Exchange(m);var z=new Zone(e.ZoneID);var guard=(IZoneBuilder)Call(m.Exploration,"Guard",m,e.ZoneID);
            Assert.NotNull(guard);Assert.True(guard.BuildZone(z,scope.Factory,new Random(4)));
            Assert.True(m.Exploration.TryGetGenerationEntry(m,z,out var cold));Assert.AreSame(e,cold);
            Assert.False(m.Exploration.TryMarkPlacementCommitted(m,z,e,()=>true));Assert.Zero(m.Exploration.DispositionFor(e.ZoneID));
        }
        [TestCase("uncached")][TestCase("attached-only")][TestCase("same-id-clone")][TestCase("inactive")][TestCase("foreign-entry")]
        public void AddressOrAttachmentCannotGrantDynamicCommit(string fault)
        {
            var m=Fresh();var e=Exchange(m);Zone z;
            if(fault=="uncached"||fault=="attached-only"){z=new Zone(e.ZoneID);if(fault=="attached-only")m.SetActiveZone(z);}
            else{z=Activate(m,e);if(fault=="same-id-clone")z=new Zone(e.ZoneID);if(fault=="inactive")m.SetActiveZone(new Zone("Overworld.0.0.0"));}
            if(fault=="foreign-entry")e=Exchange(Fresh());
            Assert.False(Mark(m,z,e,()=>true));Assert.Less(m.Exploration.DispositionFor(z.ZoneID),2);
        }
        [TestCase("false")][TestCase("removed")][TestCase("replaced")][TestCase("protected")][TestCase("inactive")][TestCase("new-plan")][TestCase("new-map")][TestCase("new-rare")][TestCase("new-wayhouse")]
        public void FailedOrMutatingProofCannotMarkAnotherGraph(string fault)
        {
            var m=Fresh();var e=Exchange(m);var z=Activate(m,e);var original=m.Exploration;
            Func<bool> proof=()=>{if(fault=="removed")m.CachedZones.Remove(e.ZoneID);if(fault=="replaced")m.CachedZones[e.ZoneID]=new Zone(e.ZoneID);if(fault=="protected"){var at=WorldMap.FromZoneID(e.ZoneID);m.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.MerchantCamp,"new protected site"));}if(fault=="inactive")m.SetActiveZone(new Zone("Overworld.0.0.0"));if(fault=="new-plan")typeof(OverworldZoneManager).GetProperty("Exploration",Flags).SetValue(m,Fresh().Exploration);if(fault=="new-map")typeof(OverworldZoneManager).GetProperty("WorldMap",Flags).SetValue(m,Fresh().WorldMap);if(fault=="new-rare")typeof(OverworldZoneManager).GetProperty("RareEncounters",Flags).SetValue(m,Fresh().RareEncounters);if(fault=="new-wayhouse")typeof(OverworldZoneManager).GetProperty("Wayhouse",Flags).SetValue(m,Fresh().Wayhouse);return fault!="false";};
            Assert.False((bool)Call(original,"TryMarkAcceptedEntryCommitted",m,z,e,proof));Assert.AreEqual(1,original.DispositionFor(e.ZoneID));
        }
        [Test] public void ReentrantOrThrowingProofDoesNotLeakACommitOrLock()
        {
            var m=Fresh();var e=Exchange(m);var z=Activate(m,e);int calls=0;
            Assert.False(Mark(m,z,e,()=>{calls++;Assert.False(Mark(m,z,e,()=>true));return false;}));Assert.AreEqual(1,calls);Assert.AreEqual(1,m.Exploration.DispositionFor(e.ZoneID));
            Assert.Throws<InvalidOperationException>(()=>Mark(m,z,e,()=>throw new InvalidOperationException("proof")));
            Assert.AreEqual(1,m.Exploration.DispositionFor(e.ZoneID));Assert.True(Mark(m,z,e,()=>true));
        }
        [Test] public void ProofCanReadAuthorityButCannotReenterCommit()
        {
            var m=Fresh();var e=Exchange(m);var z=Activate(m,e);
            Assert.True(Mark(m,z,e,()=>Accepted(m,z,out var seen)&&ReferenceEquals(seen,e)));
        }
        [Test] public void MissingProofAndUnrelatedFamilyCannotConsumeEntry()
        {
            var m=Fresh();var e=Exchange(m);var z=Activate(m,e);Assert.False(Mark(m,z,e,null));Assert.AreEqual(1,m.Exploration.DispositionFor(e.ZoneID));
            var other=m.Exploration.Entries.First(x=>x.PlacementEligible&&x.Family.ToString()=="RoadSpill");var oz=Activate(m,other);Assert.False(Accepted(m,oz,out _));Assert.False(Mark(m,oz,other,()=>true));
        }
        [Test] public void DynamicCommitAndExactAcceptedGraphSurviveSaveWithoutReplay()
        {
            var m=Fresh();var e=Exchange(m);var z=Activate(m,e);Assert.True(Mark(m,z,e,()=>true));
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("entry-v4","fixture",m,null,null));
            Assert.AreEqual(9,loaded.ZoneManager.Exploration.Version);var now=loaded.ZoneManager.GetZone(e.ZoneID);Assert.AreNotSame(z,now);
            Assert.AreEqual(2,loaded.ZoneManager.Exploration.DispositionFor(e.ZoneID));Assert.False(Accepted(loaded.ZoneManager,now,out _));
            loaded.ZoneManager.UnloadZone(e.ZoneID);Assert.AreSame(now,loaded.ZoneManager.GetZone(e.ZoneID));
        }
        [TestCase(2)][TestCase(3)] public void EarlierManifestKeepsItsOriginalRoadAssignmentAndVersion(int version)
        {
            var m=Fresh(64);var e=m.Exploration.Entries.First(x=>x.PlacementEligible&&FormationSelector.For(BiomeType.Spread,x.ZoneID)==Formation.OldRoad);
            string wire=version+"|64|1\n"+e.ZoneID+"|3|1|1|0";Restore(m,wire);Assert.AreEqual(version,m.Exploration.Version);Assert.AreEqual("RoadSpill",m.Exploration.Find(e.ZoneID).Family.ToString());Assert.AreEqual(wire,Wire(m));Assert.Zero(m.CachedZoneCount);
        }
        [TestCase(2,7)][TestCase(3,7)][TestCase(3,8)][TestCase(999,1)] public void OldOrFutureHeaderCannotAuthorizeNewFamily(int version,int family)
        {
            var m=Fresh(64);var e=m.Exploration.Entries.First(x=>x.PlacementEligible&&FormationSelector.For(BiomeType.Spread,x.ZoneID)==Formation.OldRoad);
            Assert.Throws<InvalidDataException>(()=>Restore(m,version+"|64|1\n"+e.ZoneID+"|3|"+family+"|1|0"));
        }
    }
}
