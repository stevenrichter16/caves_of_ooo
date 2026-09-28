using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadExplorationManifestAdversarialTests
    {
        DensityLootTestScope scope; IDictionary loot; DictionaryEntry[] oldLoot; object oldInitialized;
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        [SetUp] public void Setup()
        {
            loot=(IDictionary)typeof(LootTableRegistry).GetField("_byName",All).GetValue(null);
            var copy=new List<DictionaryEntry>();foreach(DictionaryEntry e in loot)copy.Add(e);oldLoot=copy.ToArray();
            oldInitialized=typeof(LootTableRegistry).GetField("_initialized",All).GetValue(null);scope=new DensityLootTestScope();
        }
        [TearDown] public void Cleanup()
        {try{scope?.Dispose();}finally{if(loot!=null&&oldLoot!=null){loot.Clear();foreach(var e in oldLoot)loot.Add(e.Key,e.Value);typeof(LootTableRegistry).GetField("_initialized",All).SetValue(null,oldInitialized);}}}
        static void Set(OverworldZoneManager m,string key,object value)=>typeof(OverworldZoneManager).GetProperty(key,All).SetValue(m,value);
        OverworldZoneManager Fresh()=>OverworldZoneManager.CreateDetached(scope.Factory,64,true);
        static string ID(OverworldZoneManager m)=>m.Exploration.Entries.First(e=>e.PlacementEligible).ZoneID;
        static GameSessionState LoadWire(OverworldZoneManager m,string wire)
        {
            // Disabled sender preserves the literal supplied wire so this exercises the actual decoder.
            var sender=OverworldZoneManager.CreateDetached(m.Factory,m.WorldSeed);
            var world=new Entity{BlueprintName="World"};world.Properties[SpreadExplorationPlan.PropertyKey]=wire;
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture("literal-manifest","fixture",sender,null,null,world:world));
        }
        static string Wire(OverworldZoneManager m)=>SpreadExplorationPlan.BindForSave(m,new Entity()).GetProperty(SpreadExplorationPlan.PropertyKey);
        [TestCase("duplicate")][TestCase("foreign")][TestCase("alias")][TestCase("family")][TestCase("topology")]
        [TestCase("placement-without-retention")][TestCase("quiet-mask-family")][TestCase("disposition")][TestCase("missing-installed")]
        [TestCase("wrong-seed")][TestCase("too-many")][TestCase("protected-placement")]
        public void MalformedMetadataRefusesBeforeReplacingTheLiveSession(string fault)
        {
            var m=Fresh();string id=ID(m);string record=id+"|3|0|1|0";string header="2|64|1";
            switch(fault){case "duplicate":header="2|64|2";record+="\n"+record;break;
                case "foreign":record="Overworld.0.0.0|1|0|0|0";break;
                case "alias":record="Overworld.011.10.0|1|0|0|0";break;
                case "family":record=id+"|3|99|1|0";break;
                case "topology":record=id+"|3|0|99|0";break;
                case "placement-without-retention":record=id+"|2|0|1|0";break;
                case "quiet-mask-family":record=id+"|1|1|0|0";break;
                case "disposition":record=id+"|3|0|1|99";break;
                case "missing-installed":record=id+"|3|0|1|1";break;
                case "wrong-seed":header="2|65|1";break;
                case "too-many":header="2|64|401";break;
                case "protected-placement":record=ReferenceGladePlan.ZoneID+"|3|0|1|0";break;}
            var active=TurnManager.Active;var settlements=SettlementManager.Current;
            Assert.Throws<InvalidDataException>(()=>LoadWire(m,header+"\n"+record));
            Assert.AreSame(active,TurnManager.Active);Assert.AreSame(settlements,SettlementManager.Current);
        }
        [Test] public void ValidLiteralEmptyManifestRestoresAsEnabledWithoutSelectingReplacements()
        {var loaded=LoadWire(Fresh(),"2|64|0");Assert.True(loaded.ZoneManager.Exploration.Enabled);Assert.IsEmpty(loaded.ZoneManager.Exploration.Entries);}
        [Test] public void ValidLiteralUninstalledRecordDoesNotGenerateOrRetainAnAttachedClone()
        {var m=Fresh();string id=ID(m);var loaded=LoadWire(m,"2|64|1\n"+id+"|3|0|1|0");Assert.AreEqual(0,loaded.ZoneManager.CachedZoneCount);var fake=new Zone(id);loaded.ZoneManager.SetActiveZone(fake);loaded.ZoneManager.UnloadZone(id);Assert.AreEqual(0,loaded.ZoneManager.CachedZoneCount);}
        [Test] public void RestoreConstructorCannotGenerateBeforeMetadataHydration()
        {
            var ctor=typeof(OverworldZoneManager).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(EntityFactory),typeof(int),typeof(bool),typeof(bool)},null);
            var m=(OverworldZoneManager)ctor.Invoke(new object[]{scope.Factory,64,false,true});
            Assert.Throws<InvalidDataException>(()=>m.GetZone(ReferenceGladePlan.ZoneID));Assert.AreEqual(0,m.CachedZoneCount);
            Assert.Throws<InvalidDataException>(()=>SpreadExplorationPlan.BindForSave(m,new Entity()));
        }
        [Test] public void OldLegacyMissingChunkRemainsLegacyAfterActualLoadAndNewAccess()
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64);GameSessionState loaded;
            using(var stream=new MemoryStream()){GameSessionState.Capture("old","fixture",m,null,null).Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
            string id=ID(Fresh());var z=loaded.ZoneManager.GetZone(id);Assert.NotNull(z);loaded.ZoneManager.UnloadZone(id);
            Assert.False(loaded.ZoneManager.CachedZones.ContainsKey(id));Assert.False(loaded.ZoneManager.Exploration.Enabled);
        }
        [Test] public void ForeignPlanCannotGenerateOrSerializeForAnotherManager()
        {
            var a=Fresh();var b=Fresh();Set(b,"Exploration",a.Exploration);
            Assert.False(b.Exploration.TryGetPlacement(b,ID(a),out _));
            Assert.Throws<InvalidDataException>(()=>b.GetZone(ID(a)));Assert.Throws<InvalidDataException>(()=>SpreadExplorationPlan.BindForSave(b,new Entity()));
        }
        sealed class CallbackBuilder:IZoneBuilder
        {
            internal Action<Zone> Callback;internal bool OnceFailed;internal bool Retry;
            public string Name=>"FixtureManifestCallback";public int Priority=>int.MinValue+1;
            public bool BuildZone(Zone z,EntityFactory f,Random rng){Callback?.Invoke(z);if(Retry&&!OnceFailed){OnceFailed=true;return false;}return true;}
        }
        sealed class ObservedManager:OverworldZoneManager
        {
            internal Action<Zone> Early,Final;internal bool Retry;internal int Starts;
            internal ObservedManager(EntityFactory f):base(f,64){Set(this,"Exploration",SpreadExplorationPlan.Create(this));}
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p=base.GetPipelineForZone(id);p.AddBuilder(new CallbackBuilder{Retry=Retry,Callback=z=>{Starts++;Early?.Invoke(z);}});return p;
            }
            protected override void OnZoneGenerated(Zone zone,string id){base.OnZoneGenerated(zone,id);Final?.Invoke(zone);}
            internal bool ReplaceAttempt(Zone zone)=>base.GetPipelineForZone(zone.ZoneID).Builders.First(b=>b.Name=="SpreadExplorationAttempt").BuildZone(zone,Factory,new Random(1));
        }
        [TestCase("plan")][TestCase("map")][TestCase("rare")][TestCase("wayhouse")][TestCase("cache-clone")]
        public void FinalCallbacksCannotInstallAStaleOrForeignGeneration(string change)
        {
            var m=new ObservedManager(scope.Factory);string id=ID(m);var original=m.Exploration;Zone foreign=null;bool witnessed=false;
            m.Final=z=>{
                Assert.True(original.TryGetGenerationEntry(m,z,out var entry));Assert.AreEqual(id,entry.ZoneID);witnessed=true;
                if(change=="plan")Set(m,"Exploration",SpreadExplorationPlan.Create(m));
                if(change=="map")m.ReplaceLoadedOverworldState(WorldGenerator.Generate(64),null,null);
                if(change=="rare")Set(m,"RareEncounters",SpreadRareEncounterPlan.Create(m));
                if(change=="wayhouse")Set(m,"Wayhouse",SpreadWayhousePlan.Create(m));
                if(change=="cache-clone"){foreign=new Zone(id);m.CachedZones[id]=foreign;}
                Assert.False(original.TryGetGenerationEntry(m,z,out _));
            };
            Assert.IsNull(m.GetZone(id));Assert.True(witnessed);Assert.AreEqual(0,original.RetainedGraphCount);
            if(foreign!=null)Assert.AreSame(foreign,m.CachedZones[id]);else Assert.False(m.CachedZones.ContainsKey(id));
        }
        [Test] public void RetryReplacesTransientAttemptAndFinalAcceptedGraphAloneRetains()
        {
            var m=new ObservedManager(scope.Factory){Retry=true};string id=ID(m);var attempts=new List<Zone>();
            m.Early=z=>{attempts.Add(z);Assert.True(m.Exploration.TryGetGenerationEntry(m,z,out _));};
            var accepted=m.GetZone(id);Assert.NotNull(accepted);Assert.AreEqual(2,m.Starts);Assert.AreSame(attempts[0],attempts[1]);
            Assert.False(m.Exploration.TryGetGenerationEntry(m,accepted,out _));Assert.AreEqual(1,m.Exploration.RetainedGraphCount);
            m.UnloadZone(id);Assert.AreSame(accepted,m.GetZone(id));
        }
        [Test] public void ExactQuietGladeGraphIsRetainedWithoutAssigningAnExplorationFamily()
        {
            var m=Fresh();var entry=m.Exploration.Find(ReferenceGladePlan.ZoneID);Assert.NotNull(entry);Assert.False(entry.PlacementEligible);
            var z=m.GetZone(entry.ZoneID);Assert.NotNull(z);m.UnloadZone(entry.ZoneID);Assert.AreSame(z,m.GetZone(entry.ZoneID));
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("glade","fixture",m,null,null));var restored=loaded.ZoneManager.GetZone(entry.ZoneID);
            loaded.ZoneManager.UnloadZone(entry.ZoneID);Assert.AreSame(restored,loaded.ZoneManager.GetZone(entry.ZoneID));
        }
        [Test] public void POIAndForeignOrdinaryGraphsKeepTheirExistingUnloadLifecycle()
        {
            var m=Fresh();string id="Overworld.0.0.0";Assert.IsNull(m.Exploration.Find(id));var z=new Zone(id);m.SetActiveZone(z);m.UnloadZone(id);Assert.False(m.CachedZones.ContainsKey(id));
        }

        static bool Mark(SpreadExplorationPlan p,OverworldZoneManager m,Zone z,SpreadExplorationEntry entry)
        {var method=typeof(SpreadExplorationPlan).GetMethod("TryMarkPlacementCommitted",new[]{typeof(OverworldZoneManager),typeof(Zone),typeof(SpreadExplorationEntry)});Assert.NotNull(method);return (bool)method.Invoke(p,new object[]{m,z,entry});}
        static int Disposition(SpreadExplorationPlan p,string id)
        {var method=typeof(SpreadExplorationPlan).GetMethod("DispositionFor");Assert.NotNull(method);return (int)method.Invoke(p,new object[]{id});}
        static string ActiveID(OverworldZoneManager m)=>m.Exploration.Entries.First(e=>e.Family!=SpreadExplorationFamily.None).ZoneID;
        [TestCase(false)][TestCase(true)]
        public void AcceptedDispositionMatchesActualAttemptAndSurvivesSave(bool committed)
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);var entry=m.Exploration.Find(id);
            m.Final=z=>{if(committed){Assert.True(Mark(m.Exploration,m,z,entry));Assert.False(Mark(m.Exploration,m,z,entry),"single token consumption");}Assert.AreEqual(0,Disposition(m.Exploration,id));};
            var zone=m.GetZone(id);Assert.NotNull(zone);Assert.AreEqual(committed?2:1,Disposition(m.Exploration,id));
            Assert.False(Mark(m.Exploration,m,zone,entry));
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("disposition","fixture",m,null,null));
            Assert.AreEqual(committed?2:1,Disposition(loaded.ZoneManager.Exploration,id));
            Assert.False(Mark(loaded.ZoneManager.Exploration,loaded.ZoneManager,loaded.ZoneManager.GetZone(id),loaded.ZoneManager.Exploration.Find(id)));
        }
        [TestCase("foreign-entry")][TestCase("foreign-zone")][TestCase("quiet")]
        public void APlacementLabelWithoutExactActiveTokenIsRefused(string mode)
        {
            var m=new ObservedManager(scope.Factory);string id=mode=="quiet"?m.Exploration.Entries.First(e=>e.PlacementEligible&&e.Family==SpreadExplorationFamily.None).ZoneID:ActiveID(m);
            var entry=mode=="foreign-entry"?Fresh().Exploration.Find(id):m.Exploration.Find(id);
            m.Final=z=>Assert.False(Mark(m.Exploration,m,mode=="foreign-zone"?new Zone(id):z,entry));
            Assert.NotNull(m.GetZone(id));Assert.AreEqual(1,Disposition(m.Exploration,id));
        }
        [Test] public void MarkBeforeRejectedFinalCallbackDoesNotPublishACommit()
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);var p=m.Exploration;
            m.Final=z=>{Assert.True(Mark(p,m,z,p.Find(id)));Set(m,"Exploration",SpreadExplorationPlan.Create(m));};
            Assert.IsNull(m.GetZone(id));Assert.AreEqual(0,Disposition(p,id));Assert.AreEqual(0,p.RetainedGraphCount);
        }
        [Test] public void DispositionReadsNeverGenerateOrResolveForeignAddresses()
        {var m=Fresh();Assert.AreEqual(0,Disposition(m.Exploration,ActiveID(m)));Assert.AreEqual(0,Disposition(m.Exploration,null));Assert.AreEqual(0,Disposition(m.Exploration,"Overworld.999.1.0"));Assert.AreEqual(0,m.CachedZoneCount);}

        [TestCase(false)][TestCase(true)]
        public void NegativeWorldSeedWireIsInvariantAcrossCurrentNumericCulture(bool alternateSign)
        {
            var before=System.Globalization.CultureInfo.CurrentCulture;
            var culture=(System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
            if(alternateSign)culture.NumberFormat.NegativeSign="~";
            try
            {
                System.Globalization.CultureInfo.CurrentCulture=culture;
                var m=OverworldZoneManager.CreateDetached(scope.Factory,-64,true);
                string wire=Wire(m);Assert.True(wire.StartsWith("2|-64|",StringComparison.Ordinal));
                var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("culture","fixture",m,null,null));
                Assert.True(loaded.ZoneManager.Exploration.Enabled);Assert.AreEqual(-64,loaded.ZoneManager.WorldSeed);
            }
            finally{System.Globalization.CultureInfo.CurrentCulture=before;}
        }

        static bool MarkWithValidation(SpreadExplorationPlan p,OverworldZoneManager m,Zone z,SpreadExplorationEntry entry,Func<bool> validate)
        {
            var method=typeof(SpreadExplorationPlan).GetMethod("TryMarkPlacementCommitted",new[]{typeof(OverworldZoneManager),typeof(Zone),typeof(SpreadExplorationEntry),typeof(Func<bool>)});
            Assert.NotNull(method,"A token-only mark cannot validate a mutable placement packet at final acceptance.");
            try{return (bool)method.Invoke(p,new object[]{m,z,entry,validate});}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
        }
        [TestCase("unchanged")][TestCase("removed")][TestCase("moved")]
        public void FinalPacketValidatorObservesSourceAfterAllGenerationCallbacks(string mutation)
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);int calls=0;Entity source=null;Zone staged=null;
            m.Final=z=>{
                staged=z;source=z.GetReadOnlyEntities().First(e=>e.HasPart<ContainerPart>());var at=z.GetEntityPosition(source);
                bool Current()=>z.GetEntityCell(source)!=null&&z.GetEntityPosition(source)==at;
                Assert.True(MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),()=>{calls++;Assert.True(m.Exploration.TryGetGenerationEntry(m,z,out _));return Current();}));
                Assert.AreEqual(1,calls);
                if(mutation=="removed")Assert.True(z.RemoveEntity(source));
                if(mutation=="moved"){var next=Enumerable.Range(0,Zone.Width*Zone.Height).Select(n=>z.GetCell(n%Zone.Width,n/Zone.Width)).First(c=>c.X!=at.x&&z.CanPlaceFootprint(source,c.X,c.Y));Assert.True(z.MoveEntity(source,next.X,next.Y));Assert.AreNotEqual(at,z.GetEntityPosition(source));}
            };
            var result=m.GetZone(id);Assert.AreEqual(2,calls);
            if(mutation=="unchanged"){Assert.AreSame(staged,result);Assert.AreEqual(2,Disposition(m.Exploration,id));}
            else{Assert.IsNull(result);Assert.False(m.CachedZones.ContainsKey(id));Assert.AreEqual(0,Disposition(m.Exploration,id));Assert.False(m.Exploration.TryGetGenerationEntry(m,staged,out _));}
        }
        [TestCase(false)][TestCase(true)]
        public void ValidatorPlanSwapRefusesBeforeMarkOrBeforeFinalInstallation(bool swapAtFinal)
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);var original=m.Exploration;int calls=0;
            m.Final=z=>{
                bool marked=MarkWithValidation(original,m,z,original.Find(id),()=>{calls++;if(calls==(swapAtFinal?2:1))Set(m,"Exploration",SpreadExplorationPlan.Create(m));return true;});
                Assert.AreEqual(swapAtFinal,marked);
            };
            Assert.IsNull(m.GetZone(id));Assert.AreEqual(swapAtFinal?2:1,calls);Assert.AreEqual(0,original.RetainedGraphCount);Assert.AreEqual(0,Disposition(original,id));
        }
        [Test] public void NullFinalValidatorCannotPublishAStrictMark()
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);m.Final=z=>Assert.False(MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),null));
            Assert.NotNull(m.GetZone(id));Assert.AreEqual(1,Disposition(m.Exploration,id));
        }
        [Test] public void FinalValidatorIsTransientAndNeverReplaysAfterLoad()
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);int calls=0;
            m.Final=z=>Assert.True(MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),()=>{calls++;return true;}));
            Assert.NotNull(m.GetZone(id));Assert.AreEqual(2,calls);
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("validated-placement","fixture",m,null,null));
            var z=loaded.ZoneManager.GetZone(id);Assert.AreEqual(2,Disposition(loaded.ZoneManager.Exploration,id));
            Assert.False(MarkWithValidation(loaded.ZoneManager.Exploration,loaded.ZoneManager,z,loaded.ZoneManager.Exploration.Find(id),()=>{calls++;return true;}));
            Assert.AreEqual(2,calls);
        }
        [Test] public void ValidatorCannotReenterEitherMarkOverload()
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);int calls=0;
            m.Final=z=>Assert.True(MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),()=>{
                calls++;Assert.False(Mark(m.Exploration,m,z,m.Exploration.Find(id)));
                Assert.False(MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),()=>throw new Exception("nested validator must not run")));return true;}));
            Assert.NotNull(m.GetZone(id));Assert.AreEqual(2,calls);Assert.AreEqual(2,Disposition(m.Exploration,id));
        }

        [TestCase(false)][TestCase(true)]
        public void ReplacedAttemptDuringValidatorCannotKeepOriginalMarkAuthority(bool replaceAtFinal)
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);int calls=0;
            m.Final=z=>{
                bool marked=MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),()=>{calls++;if(calls==(replaceAtFinal?2:1))Assert.True(m.ReplaceAttempt(z));return true;});
                Assert.AreEqual(replaceAtFinal,marked);
            };
            var result=m.GetZone(id);
            if(replaceAtFinal){Assert.IsNull(result);Assert.AreEqual(0,Disposition(m.Exploration,id));}
            else{Assert.NotNull(result);Assert.AreEqual(1,Disposition(m.Exploration,id));}
            Assert.AreEqual(replaceAtFinal?2:1,calls);
        }
        [Test] public void ThrowingFinalValidatorCannotLeaveInstalledOrReusableAuthority()
        {
            var m=new ObservedManager(scope.Factory);string id=ActiveID(m);int calls=0;Zone staged=null;
            m.Final=z=>{staged=z;Assert.True(MarkWithValidation(m.Exploration,m,z,m.Exploration.Find(id),()=>{if(++calls==2)throw new InvalidOperationException("fixture validator failure");return true;}));};
            Assert.Throws<InvalidOperationException>(()=>m.GetZone(id));Assert.AreEqual(0,m.Exploration.RetainedGraphCount);
            Assert.False(m.CachedZones.ContainsKey(id));Assert.False(m.Exploration.TryGetGenerationEntry(m,staged,out _));
        }
    }
}
