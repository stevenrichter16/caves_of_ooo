using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Random=System.Random;
namespace CavesOfOoo.Tests
{
    // Fixed-corpus cold integration and direct core command/save evidence only.
    // No native keyboard, discovery, player survival or rendered-art claim.
    public sealed class SpreadFieldPassagePipelineTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope scope;object oldSettlement;Action<string> oldMessage;
        readonly List<(FieldInfo field,object value,List<DictionaryEntry> entries)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
        Array oldDiag;object oldWrite,oldFilled,oldDropped;
        [SetUp]public void Setup()
        {
            foreach(var f in typeof(LootTableRegistry).GetFields(All))
            {if(!f.IsStatic)continue;var v=f.GetValue(null);var rows=new List<DictionaryEntry>();if(v is IDictionary d)foreach(DictionaryEntry row in d)rows.Add(row);globals.Add((f,v,rows));}
            oldSettlement=SettlementManager.Current;oldMessage=MessageLog.OnMessage;MessageLog.OnMessage=null;
            oldDiag=(Array)((Array)typeof(Diag).GetField("_buffer",All).GetValue(null)).Clone();oldWrite=typeof(Diag).GetField("_writeIndex",All).GetValue(null);oldFilled=typeof(Diag).GetField("_filledCount",All).GetValue(null);oldDropped=typeof(Diag).GetField("_droppedCount",All).GetValue(null);
            scope=new DensityLootTestScope();
        }
        [TearDown]public void Cleanup()
        {
            try{scope?.Dispose();}finally
            {
                foreach(var g in globals){if(!g.field.IsInitOnly&&!g.field.IsLiteral)g.field.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var row in g.entries)d.Add(row.Key,row.Value);}}globals.Clear();
                typeof(SettlementManager).GetProperty("Current",All).SetValue(null,oldSettlement);MessageLog.OnMessage=oldMessage;
                if(oldDiag!=null){oldDiag.CopyTo((Array)typeof(Diag).GetField("_buffer",All).GetValue(null),0);typeof(Diag).GetField("_writeIndex",All).SetValue(null,oldWrite);typeof(Diag).GetField("_filledCount",All).SetValue(null,oldFilled);typeof(Diag).GetField("_droppedCount",All).SetValue(null,oldDropped);}
            }
        }
        sealed class Probe:IZoneBuilder
        {readonly int priority;readonly Action<Zone,Random> action;public Probe(int p,Action<Zone,Random>a){priority=p;action=a;}public string Name=>"FieldPassagePipelineProbe";public int Priority=>priority;public bool BuildZone(Zone z,EntityFactory f,Random r){action(z,r);return true;}}
        sealed class ObservedManager:OverworldZoneManager
        {
            readonly bool baseline;readonly Action<ObservedManager,Zone> after;
            public SpreadExplorationBuilder Composer;public SpreadCompositionBuilder Terrain;
            public Entity[] Before;public (int x,int y)[] Positions;public string[] Facts,Shapes;public Part[][] Parts;
            public Entity[] After;public (int x,int y)[] AfterPositions;public string[] AfterFacts;public Part[][] AfterParts;
            public int NextRng,Callbacks;public Zone Generated;
            public ObservedManager(EntityFactory f,int seed,bool baseline=false,Action<ObservedManager,Zone> after=null):base(f,seed){this.baseline=baseline;this.after=after;}
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p=base.GetPipelineForZone(id);Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();Terrain=p.Builders.OfType<SpreadCompositionBuilder>().SingleOrDefault();
                // Remove only composition after its constructor enables exact
                // producer capture. The source policy/version stays identical.
                if(baseline)p.RemoveBuilders<SpreadExplorationBuilder>();
                p.AddBuilder(new Probe(4299,(z,r)=>{Generated=z;Before=z.GetReadOnlyEntities().ToArray();Positions=Before.Select(z.GetEntityPosition).ToArray();Facts=Before.Select(Exact).ToArray();Shapes=Before.Select(Shape).ToArray();Parts=Before.Select(e=>e.Parts.ToArray()).ToArray();}));
                p.AddBuilder(new Probe(int.MaxValue,(z,r)=>{After=z.GetReadOnlyEntities().ToArray();AfterPositions=After.Select(z.GetEntityPosition).ToArray();AfterFacts=After.Select(Exact).ToArray();AfterParts=After.Select(e=>e.Parts.ToArray()).ToArray();NextRng=r.Next();}));return p;
            }
            protected override void OnZoneGenerated(Zone z,string id)
            {base.OnZoneGenerated(z,id);if(Composer?.LastResult=="FieldPassage"){Callbacks++;after?.Invoke(this,z);}}
        }
        static Entity[] Gear(Entity e)
        {var i=e.GetPart<InventoryPart>();var b=e.GetPart<Body>();return(i?.Objects??new List<Entity>()).Concat(i?.EquippedItems.Values??Enumerable.Empty<Entity>()).Concat(b?.GetParts().Select(p=>p.Equipped)??Enumerable.Empty<Entity>()).Where(x=>x!=null).Distinct().ToArray();}
        static string Item(Entity e)=>e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":"+string.Join(",",e.Statistics.OrderBy(s=>s.Key).Select(s=>s.Key+":"+s.Value.BaseValue+":"+s.Value.Bonus+":"+s.Value.Penalty))+";tags="+string.Join(",",e.Tags.OrderBy(s=>s.Key).Select(s=>s.Key+"="+s.Value))+";props="+string.Join(",",e.Properties.OrderBy(s=>s.Key).Select(s=>s.Key+"="+s.Value));
        static string Shape(Entity e)=>Item(e)+";stock="+string.Join("/",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(Item))+";gear="+string.Join("/",Gear(e).Select(Item));
        static string Exact(Entity e)=>e.ID+":"+Shape(e)+";links="+string.Join("/",Gear(e).Concat(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(i=>i.ID+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID+":"+i.GetPart<PhysicsPart>()?.Equipped?.ID));
        void Seed(int seed,string id)=>scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
        string[] Selected(int seed)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);Assert.AreEqual(13,m.Exploration.Version,"F12 pipeline tests exercise the current source policy; literal older assignments remain in manifest tests.");
            var ids=m.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family.ToString()=="FieldPassage").Select(e=>e.ZoneID).ToArray();Assert.IsNotEmpty(ids);Assert.Zero(m.CachedZoneCount);return ids;
        }
        static (Cell a,Cell b,int closed) Approaches(Zone z,Entity gate)
        {
            var p=z.GetEntityPosition(gate);
            foreach(bool horizontal in new[]{true,false})
            {
                var a=z.GetCell(p.x-(horizontal?1:0),p.y-(horizontal?0:1));var b=z.GetCell(p.x+(horizontal?1:0),p.y+(horizontal?0:1));
                var side1=z.GetCell(p.x-(horizontal?0:1),p.y-(horizontal?1:0));var side2=z.GetCell(p.x+(horizontal?0:1),p.y+(horizontal?1:0));
                if(a==null||b==null||a.BlocksMovement()||b.BlocksMovement()||!side1.Objects.Any(e=>e.BlueprintName=="Hedge")||!side2.Objects.Any(e=>e.BlueprintName=="Hedge"))continue;
                var physical=FindPath.Search(z,a.X,a.Y,b.X,b.Y);if(!physical.Usable||physical.Steps.Count<=3||physical.Steps.Count>60)continue;
                // Independently check returned movement cells. Actor-aware pathing
                // may plan through a closed door and is not this bypass oracle.
                int x=a.X,y=a.Y;foreach(var step in physical.Steps){x+=step.dx;y+=step.dy;Assert.False(z.GetCell(x,y).BlocksMovement());Assert.AreNotEqual(p,(x,y));}
                Assert.AreEqual((b.X,b.Y),(x,y));return(a,b,physical.Steps.Count);
            }
            Assert.Fail("Actual gate needs a physical diagonal-aware bypass longer than Open plus two moves.");return(null,null,0);
        }
        static Entity Gate(Zone z)=>z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SpreadFieldGate"||e.BlueprintName=="GleanersBuckledWicket");
        static string ExpectedGate(ObservedManager m,string id)
            =>(uint)typeof(SpreadExplorationPlan).GetMethod("Rank",All).Invoke(null,new object[]{m.WorldSeed,id,"fieldwork-wicket"})%3==0?"GleanersBuckledWicket":"SpreadFieldGate";
        static void AssertConserved(ObservedManager m,Zone z,bool committed)
        {
            var owners=m.After;var removed=m.Before.Except(owners).ToArray();var added=owners.Except(m.Before).ToArray();Assert.AreEqual(m.Before.Length,owners.Length);
            CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities(),"Normal post-generation naming may not replace or add unrelated owners.");
            if(committed){Assert.AreEqual(1,removed.Length);Assert.AreEqual("Hedge",removed[0].BlueprintName);Assert.AreEqual(1,added.Length);Assert.AreEqual(ExpectedGate(m,z.ZoneID),added[0].BlueprintName);Assert.AreEqual(m.Positions[Array.IndexOf(m.Before,removed[0])],z.GetEntityPosition(added[0]));Assert.Null(z.GetEntityCell(removed[0]));}
            else{Assert.IsEmpty(removed);Assert.IsEmpty(added);}
            for(int i=0;i<m.Before.Length;i++){var owner=m.Before[i];if(removed.Contains(owner))continue;int j=Array.IndexOf(m.After,owner);Assert.AreEqual(m.Positions[i],m.AfterPositions[j],owner.BlueprintName+" synchronous position");Assert.AreEqual(m.Facts[i],m.AfterFacts[j],owner.BlueprintName+" exact stock/gear/state through composition");CollectionAssert.AreEqual(m.Parts[i],m.AfterParts[j],owner.BlueprintName+" original parts through composition");}
        }
        [Serializable]public sealed class CensusRow{public string zone,result;public int owners,rawPrecompositionHedges,closedSteps,nextRng;public double baselineMilliseconds,currentMilliseconds;public bool committed;}
        [Serializable]public sealed class Census{public int seed,selected,committed,refused;public string boundary;public List<CensusRow> rows=new List<CensusRow>();}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void RealCurrentPipelineReplacesOneExactHedgeWithUsefulGateAndKeepsOrdinaryBudget(int seed)
        {
            var ids=Selected(seed);var report=new Census{seed=seed,selected=ids.Length,boundary="Fixed current v6 selections, actual cold full pipeline paired with only composer removed. Per-zone global RNG reset; same source policy. Exact current owners/positions/parts and explicit stock/gear/stats/tags/property projection. Raw Hedge count is not eligible-source count; one paired elapsed sample per address is not a frame-time or significance claim. Not full graph serialization, native journey or v4 economy equivalence."};
            foreach(string id in ids)
            {
                Seed(seed,id);var baseline=new ObservedManager(scope.Factory,seed,true);var watch=System.Diagnostics.Stopwatch.StartNew();var before=baseline.GetZone(id);watch.Stop();double baselineMs=watch.Elapsed.TotalMilliseconds;Assert.NotNull(before,id+" baseline");
                Seed(seed,id);var current=new ObservedManager(scope.Factory,seed);watch.Restart();var z=current.GetZone(id);watch.Stop();double currentMs=watch.Elapsed.TotalMilliseconds;Assert.NotNull(z,id+" current");Assert.NotNull(current.Composer);Assert.NotNull(current.Terrain);Assert.AreSame(z,current.Terrain.SourceZone);Assert.AreEqual(Formation.Hedgerow,current.Terrain.Plan.Formation);
                CollectionAssert.AreEqual(baseline.Shapes,current.Shapes,id+" same actual precomposition stock/gear/stats");CollectionAssert.AreEqual(baseline.Positions,current.Positions,id+" precomposition placement");Assert.AreEqual(baseline.NextRng,current.NextRng,id+" caller RNG");Assert.AreEqual(before.TileState.ToSaveString(),z.TileState.ToSaveString(),id+" tile layers");
                bool done=current.Exploration.DispositionFor(id)==2;AssertConserved(current,z,done);int closed=0;
                // LocalPeople.Apply legitimately names residents after the build
                // pipeline. Keep those actual post-generation names in the paired
                // equality rather than declaring all late naming invalid.
                for(int i=0;i<current.Before.Length;i++)
                {
                    var owner=current.Before[i];if(!current.After.Contains(owner))continue;
                    Assert.AreEqual(Shape(baseline.Before[i]),Shape(owner),id+" final paired stock/gear/state and resident name");
                    Assert.AreEqual(before.GetEntityPosition(baseline.Before[i]),z.GetEntityPosition(owner),id+" final paired unrelated position");
                    CollectionAssert.AreEqual(current.AfterParts[Array.IndexOf(current.After,owner)],owner.Parts,id+" no late unrelated part replacement");
                }
                if(done){report.committed++;Assert.AreEqual("FieldPassage",current.Composer.LastResult);Assert.AreEqual(1,current.Callbacks);var gate=Gate(z);Assert.True(gate.GetPart<DoorPart>().IsClosed);Assert.IsEmpty(gate.GetPart<DoorPart>().OwnerId);Assert.False(gate.HasPart<LockPart>());Assert.False(gate.HasPart<InventoryPart>());Assert.False(gate.HasPart<ContainerPart>());Assert.AreEqual(10,gate.GetPart<DestructiblePart>().HP);closed=Approaches(z,gate).closed;}
                else{report.refused++;Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadFieldGate"||e.BlueprintName=="GleanersBuckledWicket"));}
                var facts=z.GetReadOnlyEntities().Select(Exact).ToArray();current.UnloadZone(id);Assert.AreSame(z,current.GetZone(id),"Accepted graph cannot regenerate/reward-refill on explicit unload.");CollectionAssert.AreEqual(facts,z.GetReadOnlyEntities().Select(Exact));
                report.rows.Add(new CensusRow{zone=id,result=current.Composer.LastResult,owners=z.EntityCount,rawPrecompositionHedges=current.Before.Count(e=>e.BlueprintName=="Hedge"),baselineMilliseconds=baselineMs,currentMilliseconds=currentMs,closedSteps=closed,nextRng=current.NextRng,committed=done});
            }
            string json=JsonUtility.ToJson(report,true);TestContext.WriteLine(json);string output=Environment.GetEnvironmentVariable("COO_FIELD_PASSAGE_CENSUS_OUTPUT");if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"seed-"+seed+".json"),json);}
            Assert.Greater(report.committed,0,"Each frozen seed must have at least one actual useful generated shortcut; metadata alone is insufficient.");
        }
        string FindCommitted(int seed)
        {foreach(string id in Selected(seed)){Seed(seed,id);var m=new ObservedManager(scope.Factory,seed);if(m.GetZone(id)!=null&&m.Exploration.DispositionFor(id)==2)return id;}Assert.Fail("No actual committed source in the fixed selection corpus; do not inject or search other seeds.");return null;}
        [TestCase("unchanged",true)][TestCase("offroute",true)][TestCase("replace-gate",false)][TestCase("block-approach",false)]
        public void FinalCallbackKeepsUsefulRouteAndExactGateAuthority(string mutation,bool accepted)
        {
            const int seed=64;string id=FindCommitted(seed);Seed(seed,id);Entity foreign=null;Cell changed=null;
            var m=new ObservedManager(scope.Factory,seed,after:(self,z)=>
            {
                var gate=Gate(z);var a=Approaches(z,gate).a;
                if(mutation=="replace-gate"){changed=z.GetEntityCell(gate);Assert.True(z.RemoveEntity(gate));foreign=scope.Factory.CreateEntity(gate.BlueprintName);foreign.ID=gate.ID;Assert.True(z.AddEntity(foreign,changed.X,changed.Y));}
                if(mutation=="block-approach"){changed=a;foreign=scope.Factory.CreateEntity("StoneWall");Assert.True(z.AddEntity(foreign,a.X,a.Y));}
                if(mutation=="offroute"){foreign=scope.Factory.CreateEntity("Cudgel");Assert.True(z.AddEntity(foreign,0,0));}
            });
            var result=m.GetZone(id);Assert.AreEqual(1,m.Callbacks,"A real committed source precedes the late mutation.");
            if(accepted){Assert.NotNull(result);Assert.AreEqual(2,m.Exploration.DispositionFor(id));}
            else{Assert.IsNull(result);Assert.Zero(m.CachedZoneCount);Assert.Zero(m.Exploration.DispositionFor(id));Assert.NotNull(foreign);Assert.AreSame(changed,m.Generated.GetEntityCell(foreign),"Rejection cannot overwrite independent callback ownership.");}
        }
        [TestCase(false)][TestCase(true)]
        public void ActualOpenedOrBrokenGateSurvivesAwaySaveAndReturnWithoutReplacingHedge(bool broken)
        {
            const int seed=64;string id=FindCommitted(seed);Seed(seed,id);var m=new ObservedManager(scope.Factory,seed);var z=m.GetZone(id);Assert.NotNull(z);var gate=Gate(z);var pos=z.GetEntityPosition(gate);var approach=Approaches(z,gate);string gateID=gate.ID;int axis=gate.GetPart<DoorPart>().QuarterTurns;
            var oldHedge=m.Before.Except(z.GetReadOnlyEntities()).Single();string hedgeID=oldHedge.ID;var player=scope.Factory.CreateEntity("Player");Assert.True(z.AddEntity(player,approach.a.X,approach.a.Y));m.SetActiveZone(z);
            if(gate.GetPart<RepairablePart>() is RepairablePart fault)
            {
                Assert.False(gate.GetPart<DoorPart>().TrySetOpen(player,z,true),"A buckled gate must refuse ordinary opening before investment.");
                for(int i=0;i<2;i++)Assert.True(player.GetPart<InventoryPart>().AddObject(scope.Factory.CreateEntity("SalvagedTimber")));
                Assert.True(fault.TryRepair(player,z));
            }
            var open=GameEvent.New("InventoryAction");try{open.SetParameter("Actor",player);open.SetParameter("Zone",z);open.SetParameter("Command",DoorPart.OpenCommand);gate.FireEvent(open);Assert.True(open.Handled);}finally{open.Release();}
            Assert.True(gate.GetPart<DoorPart>().IsOpen);Assert.AreEqual((approach.a.X,approach.a.Y),z.GetEntityPosition(player),"Opening is a separate stationary core action.");Assert.True(MovementSystem.TryMoveTo(player,z,pos.x,pos.y));Assert.True(MovementSystem.TryMoveTo(player,z,approach.b.X,approach.b.Y));
            if(broken){Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(gate,10,player,z));Assert.Null(z.GetEntityCell(gate));}
            var expected=z.GetReadOnlyEntities().Where(e=>!ReferenceEquals(e,player)).ToDictionary(e=>e.ID,e=>(pos:z.GetEntityPosition(e),facts:Exact(e)));int money=TradeSystem.GetDrams(player);
            // Disclosed core graph transfer exercises an inactive retained source;
            // real keyboard travel/save is a separate native acceptance gate.
            var away=m.GetZone(ReferenceGladePlan.ZoneID);Assert.NotNull(away);Cell target=null;away.ForEachCell((c,x,y)=>{if(target==null&&away.CanPlaceFootprint(player,x,y))target=c;});Assert.NotNull(target);Assert.True(z.RemoveEntity(player));Assert.True(away.AddEntity(player,target.X,target.Y));m.SetActiveZone(away);m.UnloadZone(id);Assert.AreSame(z,m.GetZone(id));
            var state=GameSessionState.Capture("field-passage","direct-core-away-save",m,null,player);GameSessionState loaded;using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
            var restored=(OverworldZoneManager)loaded.ZoneManager;Assert.AreNotSame(m,restored);Assert.AreNotSame(player,loaded.Player);Assert.AreEqual(ReferenceGladePlan.ZoneID,restored.ActiveZone.ZoneID);Assert.AreEqual(13,restored.Exploration.Version);Assert.AreEqual(2,restored.Exploration.DispositionFor(id));var returned=restored.GetZone(id);Assert.NotNull(returned);Assert.AreNotSame(z,returned);Assert.AreEqual(expected.Count,returned.EntityCount);Assert.AreEqual(money,TradeSystem.GetDrams(loaded.Player));
            foreach(var owner in returned.GetReadOnlyEntities()){Assert.True(expected.ContainsKey(owner.ID));Assert.AreEqual(expected[owner.ID].pos,returned.GetEntityPosition(owner));Assert.AreEqual(expected[owner.ID].facts,Exact(owner));}
            Assert.False(returned.GetReadOnlyEntities().Any(e=>e.ID==hedgeID));Assert.False(returned.GetCell(pos.x,pos.y).BlocksMovement());
            if(broken)Assert.False(returned.GetReadOnlyEntities().Any(e=>e.ID==gateID));else{var saved=returned.GetReadOnlyEntities().Single(e=>e.ID==gateID);Assert.AreNotSame(gate,saved);Assert.True(saved.GetPart<DoorPart>().IsOpen);Assert.AreEqual(axis,saved.GetPart<DoorPart>().QuarterTurns);}
            restored.UnloadZone(id);Assert.AreSame(returned,restored.GetZone(id));Assert.AreEqual(expected.Count,returned.EntityCount);
        }
    }
}
