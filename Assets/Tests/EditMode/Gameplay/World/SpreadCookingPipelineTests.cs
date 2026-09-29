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
    public sealed class SpreadCookingPipelineTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope scope;object oldSettlement;Action<string> oldMessage;
        List<MessageLog.Entry> oldMessages;List<string> oldAnnouncements;int oldFlash,oldSerial;
        readonly List<(FieldInfo field,object value,List<DictionaryEntry> entries)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
        Array oldDiag;object oldWrite,oldFilled,oldDropped;
        readonly List<Action> restore=new List<Action>();
        void Keep(Type type,string name)
        {
            var field=type.GetField(name,All);Assert.NotNull(field,type.Name+"."+name);var value=field.GetValue(null);
            if(value is IDictionary d){var rows=new List<DictionaryEntry>();foreach(DictionaryEntry row in d)rows.Add(row);restore.Add(()=>{field.SetValue(null,value);d.Clear();foreach(var row in rows)d.Add(row.Key,row.Value);});}
            else if(value is HashSet<string> set){var rows=set.ToArray();restore.Add(()=>{field.SetValue(null,value);set.Clear();foreach(var row in rows)set.Add(row);});}
            else if(value is IList list){var rows=list.Cast<object>().ToArray();restore.Add(()=>{field.SetValue(null,value);list.Clear();foreach(var row in rows)list.Add(row);});}
            else restore.Add(()=>field.SetValue(null,value));
        }
        [SetUp]public void Setup()
        {
            foreach(var f in typeof(LootTableRegistry).GetFields(All))
            {if(!f.IsStatic)continue;var v=f.GetValue(null);var rows=new List<DictionaryEntry>();if(v is IDictionary d)foreach(DictionaryEntry row in d)rows.Add(row);globals.Add((f,v,rows));}
            oldMessages=MessageLog.GetAllEntries();oldAnnouncements=MessageLog.GetPendingAnnouncementsSnapshot();oldFlash=MessageLog.FlashStamp;oldSerial=(int)typeof(MessageLog).GetField("NextSerial",All).GetValue(null);
            Keep(typeof(SettlementRuntime),"<ActiveZone>k__BackingField");Keep(typeof(SettlementRuntime),"<ZoneDirtyCallback>k__BackingField");Keep(typeof(ZoneRenderHooks),"<CellDirtyCallback>k__BackingField");Keep(typeof(ZoneRenderHooks),"<FullDirtyCallback>k__BackingField");
            oldSettlement=SettlementManager.Current;oldMessage=MessageLog.OnMessage;MessageLog.OnMessage=null;
            oldDiag=(Array)((Array)typeof(Diag).GetField("_buffer",All).GetValue(null)).Clone();oldWrite=typeof(Diag).GetField("_writeIndex",All).GetValue(null);oldFilled=typeof(Diag).GetField("_filledCount",All).GetValue(null);oldDropped=typeof(Diag).GetField("_droppedCount",All).GetValue(null);
            Keep(typeof(FactionManager),"_factionFeelings");Keep(typeof(FactionManager),"_registeredFactions");Keep(typeof(FactionManager),"_factionData");Keep(typeof(PlayerReputation),"_reputation");
            Keep(typeof(MaterialReactionResolver),"_reactions");Keep(typeof(MaterialReactionResolver),"_initialized");Keep(typeof(MaterialReactionResolver),"Factory");Keep(typeof(HarvestablePart),"Factory");
            scope=new DensityLootTestScope();SettlementRuntime.ActiveZone=null;SettlementRuntime.ZoneDirtyCallback=null;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Factions.json")));
            MaterialReactionResolver.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialReactions"),"*.json").OrderBy(x=>x,StringComparer.Ordinal).Select(File.ReadAllText));
            MaterialReactionResolver.Factory=HarvestablePart.Factory=scope.Factory;
        }
        [TearDown]public void Cleanup()
        {
            try{scope?.Dispose();}finally
            {
                for(int i=restore.Count-1;i>=0;i--)restore[i]();restore.Clear();
                foreach(var g in globals){if(!g.field.IsInitOnly&&!g.field.IsLiteral)g.field.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var row in g.entries)d.Add(row.Key,row.Value);}}globals.Clear();
                if(oldMessages!=null)MessageLog.Restore(oldMessages,oldAnnouncements,oldFlash,oldSerial);
                typeof(SettlementManager).GetProperty("Current",All).SetValue(null,oldSettlement);MessageLog.OnMessage=oldMessage;
                if(oldDiag!=null){oldDiag.CopyTo((Array)typeof(Diag).GetField("_buffer",All).GetValue(null),0);typeof(Diag).GetField("_writeIndex",All).SetValue(null,oldWrite);typeof(Diag).GetField("_filledCount",All).SetValue(null,oldFilled);typeof(Diag).GetField("_droppedCount",All).SetValue(null,oldDropped);}
            }
        }
        sealed class Probe:IZoneBuilder
        {readonly int priority;readonly Action<Zone,Random> action;public Probe(int p,Action<Zone,Random>a){priority=p;action=a;}public string Name=>"CoolingPipelineProbe";public int Priority=>priority;public bool BuildZone(Zone z,EntityFactory f,Random r){action(z,r);return true;}}
        sealed class ObservedManager:OverworldZoneManager
        {
            readonly bool baseline;readonly Action<ObservedManager,Zone> after;
            public SpreadExplorationBuilder Composer;public SpreadCompositionBuilder Terrain;
            public Entity[] Before;public (int x,int y)[] Positions;public string[] Facts,Shapes;public Part[][] Parts;
            public Entity[] After;public (int x,int y)[] AfterPositions;public string[] AfterFacts;public Part[][] AfterParts;
            public int NextRng,Callbacks;public Zone Generated;public SpreadGenerationReceipt[] Receipts;
            public ObservedManager(EntityFactory f,int seed,bool baseline=false,Action<ObservedManager,Zone> after=null):base(f,seed){this.baseline=baseline;this.after=after;}
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p=base.GetPipelineForZone(id);Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();Terrain=p.Builders.OfType<SpreadCompositionBuilder>().SingleOrDefault();
                // Remove only composition after its constructor enables exact
                // producer capture. The source policy/version stays identical.
                if(baseline)p.RemoveBuilders<SpreadExplorationBuilder>();
                p.AddBuilder(new Probe(4299,(z,r)=>{Generated=z;Before=z.GetReadOnlyEntities().ToArray();Positions=Before.Select(z.GetEntityPosition).ToArray();Facts=Before.Select(Exact).ToArray();Shapes=Before.Select(Shape).ToArray();Parts=Before.Select(e=>e.Parts.ToArray()).ToArray();
                    if(Exploration.Entries.Any(e=>e.ZoneID==id&&e.Family.ToString()=="CoolingWorkPatch"))
                    {var info=typeof(SpreadCompositionBuilder).GetProperty("CookingSources",All);Assert.NotNull(info,"Selected terrain requires exact prebuild row receipts.");Receipts=((IEnumerable)info.GetValue(Terrain)).Cast<SpreadGenerationReceipt>().ToArray();Assert.True(Receipts.All(x=>x.Zone==z&&x.IsCurrent&&x.Owners.Count==1&&Row(x.Owners[0])));}
                }));
                p.AddBuilder(new Probe(int.MaxValue,(z,r)=>{After=z.GetReadOnlyEntities().ToArray();AfterPositions=After.Select(z.GetEntityPosition).ToArray();AfterFacts=After.Select(Exact).ToArray();AfterParts=After.Select(e=>e.Parts.ToArray()).ToArray();NextRng=r.Next();}));return p;
            }
            protected override void OnZoneGenerated(Zone z,string id)
            {base.OnZoneGenerated(z,id);if(Composer?.LastResult=="CoolingWorkPatch"){Callbacks++;after?.Invoke(this,z);}}
        }
        static Entity[] Gear(Entity e)
        {var i=e.GetPart<InventoryPart>();var b=e.GetPart<Body>();return(i?.Objects??new List<Entity>()).Concat(i?.EquippedItems.Values??Enumerable.Empty<Entity>()).Concat(b?.GetParts().Select(p=>p.Equipped)??Enumerable.Empty<Entity>()).Where(x=>x!=null).Distinct().ToArray();}
        static string Item(Entity e)=>e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+";value="+e.GetPart<CommercePart>()?.Value+";weight="+e.GetPart<PhysicsPart>()?.Weight+":"+string.Join(",",e.Statistics.OrderBy(s=>s.Key).Select(s=>s.Key+":"+s.Value.BaseValue+":"+s.Value.Bonus+":"+s.Value.Penalty))+";tags="+string.Join(",",e.Tags.OrderBy(s=>s.Key).Select(s=>s.Key+"="+s.Value))+";props="+string.Join(",",e.Properties.OrderBy(s=>s.Key).Select(s=>s.Key+"="+s.Value));
        static string State(Entity e)=>";render="+e.GetPart<RenderPart>()?.DisplayName+":"+e.GetPart<RenderPart>()?.RenderString+":"+e.GetPart<RenderPart>()?.ColorString+";field="+e.GetPart<FieldHarvestPart>()?.Harvested+":"+e.GetPart<FieldHarvestPart>()?.YieldBlueprint+":"+e.GetPart<FieldHarvestPart>()?.YieldCount+";heat="+e.GetPart<ThermalPart>()?.Temperature+";fuel="+e.GetPart<FuelPart>()?.FuelMass;
        static string Shape(Entity e)=>Item(e)+State(e)+";stock="+string.Join("/",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(Item))+";gear="+string.Join("/",Gear(e).Select(Item));
        static string Exact(Entity e)=>e.ID+":"+Shape(e)+";links="+string.Join("/",Gear(e).Concat(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(i=>i.ID+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID+":"+i.GetPart<PhysicsPart>()?.Equipped?.ID));
        void Seed(int seed,string id)=>scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
        SpreadExplorationEntry[] Entries(int seed)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);Assert.AreEqual(7,m.Exploration.Version,"F10 current pipeline preserves its field source policy; literal old versions remain in manifest fixtures.");
            var entries=m.Exploration.Entries.Where(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.FieldStrips).ToArray();
            Assert.IsNotEmpty(entries);Assert.Zero(m.CachedZoneCount);return entries;
        }
        string[] Selected(int seed)=>Entries(seed).Where(e=>e.Family.ToString()=="CoolingWorkPatch").Select(e=>e.ZoneID).ToArray();
        static bool Bare(Entity e)=>(bool)typeof(DoorPart).GetMethod("IsBareGround",All).Invoke(null,new object[]{e});
        static Entity Coals(Zone z)=>z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SpreadCookingCoals");
        static bool Row(Entity e)=>e.BlueprintName=="RipeCropRow"&&e.GetPart<FieldHarvestPart>()?.Harvested==false&&e.GetPart<FieldHarvestPart>().YieldBlueprint=="Emberwheat"&&e.GetPart<FieldHarvestPart>().YieldCount==1;
        static readonly int[] dx={1,-1,0,0},dy={0,0,1,-1};
        static bool Inside(int x,int y)=>x>=0&&x<Zone.Width&&y>=0&&y<Zone.Height;
        static int Range((int x,int y)a,(int x,int y)b)=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y));
        static int[,] Distance(bool[,] walk,(int x,int y)start)
        {
            var d=new int[Zone.Width,Zone.Height];for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)d[x,y]=-1;
            if(!Inside(start.x,start.y)||!walk[start.x,start.y])return d;
            var q=new Queue<(int x,int y)>();q.Enqueue(start);d[start.x,start.y]=0;
            while(q.Count>0){var p=q.Dequeue();for(int n=0;n<4;n++){int x=p.x+dx[n],y=p.y+dy[n];if(Inside(x,y)&&walk[x,y]&&d[x,y]<0){d[x,y]=d[p.x,p.y]+1;q.Enqueue((x,y));}}}return d;
        }
        static List<(int x,int y)> Adjacent(bool[,] walk,(int x,int y)p)=>Enumerable.Range(0,4).Select(n=>(x:p.x+dx[n],y:p.y+dy[n])).Where(a=>Inside(a.x,a.y)&&walk[a.x,a.y]).ToList();
        sealed class Route{public Entity row;public (int x,int y)rowStand,stationStand;public int[] passes;}
        static Route Useful(ObservedManager m,Zone z,Entity source)
        {
            var at=z.GetEntityPosition(source);var plan=m.Terrain.Plan;
            Assert.That(at.x,Is.InRange(2,Zone.Width-3));Assert.That(at.y,Is.InRange(2,Zone.Height-3));Assert.False(z.GenReservedCells.Contains(at));
            Assert.False(z.GetCell(at.x,at.y).IsInterior);Assert.True(z.TileState.Get(at.x,at.y)?.IsEmpty!=false);
            Assert.True(z.GetCell(at.x,at.y).Objects.All(e=>ReferenceEquals(e,source)||Bare(e)));Assert.False(source.GetPart<PhysicsPart>().Solid);
            var hostiles=z.GetReadOnlyEntities().Where(e=>e.GetPart<BrainPart>()!=null&&e.GetStatValue("Hitpoints")>0&&PlayerReputation.GetFeeling(FactionManager.GetFaction(e))<=FactionManager.HOSTILE_THRESHOLD).Select(z.GetEntityPosition).ToArray();
            Assert.True(hostiles.All(h=>Range(h,at)>6));var walk=new bool[Zone.Width,Zone.Height];
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                var c=z.GetCell(x,y);walk[x,y]=!c.IsInterior&&!c.BlocksMovement()&&z.TileState.Get(x,y)?.IsEmpty!=false&&!hostiles.Any(h=>Range(h,(x,y))<=3)
                    &&c.Occupants.All(e=>!e.HasTag("Creature")&&!e.HasPart<LiquidPoolPart>()&&!e.HasPart<GasPoolPart>()&&!e.HasPart<TriggerOnStepPart>());
            }
            // Avoid the coals anchor itself in the oracle even though it is non-solid.
            walk[at.x,at.y]=false;var stands=Adjacent(walk,at);Assert.GreaterOrEqual(stands.Count,2);
            var ports=new[]{(x:0,y:plan.WestY),(x:Zone.Width-1,y:plan.EastY),(x:plan.NorthX,y:0),(x:plan.SouthX,y:Zone.Height-1)};
            var from=ports.Select(p=>Distance(walk,p)).ToArray();
            foreach(var row in m.Receipts.Where(r=>!r.IsCurrent).Select(r=>r.Owners.Single()).Where(Row))
            {
                var rp=z.GetEntityPosition(row);if(Range(rp,at)<2||Range(rp,at)>5)continue;var rowStands=Adjacent(walk,rp);if(rowStands.Count<2)continue;
                foreach(var stand in rowStands)
                {
                    var ds=Distance(walk,stand);if(from.Any(d=>d[stand.x,stand.y]<0))continue;
                    foreach(var dest in stands){int between=ds[dest.x,dest.y];if(between<0||between>8)continue;var passes=from.Select(d=>d[stand.x,stand.y]+between+3).ToArray();if(passes.All(n=>n<=60))return new Route{row=row,rowStand=stand,stationStand=dest,passes=passes};}
                }
            }
            Assert.Fail("Actual current source must serve one original row within all four <=60-pass routes, not just occupy a selected field.");return null;
        }
        static void AssertConserved(ObservedManager m,Zone z,bool committed)
        {
            CollectionAssert.IsSubsetOf(m.Before,m.After);var added=m.After.Except(m.Before).ToArray();Assert.AreEqual(committed?1:0,added.Length);
            CollectionAssert.AreEquivalent(m.After,z.GetReadOnlyEntities());if(committed)Assert.AreEqual("SpreadCookingCoals",added.Single().BlueprintName);
            for(int i=0;i<m.Before.Length;i++){var owner=m.Before[i];int j=Array.IndexOf(m.After,owner);Assert.AreEqual(m.Positions[i],m.AfterPositions[j],owner.BlueprintName+" synchronous position");Assert.AreEqual(m.Facts[i],m.AfterFacts[j],owner.BlueprintName+" state/stock/gear through composition");CollectionAssert.AreEqual(m.Parts[i],m.AfterParts[j]);}
        }
        [Serializable]public sealed class CensusRow{public string zone,result;public int ordinaryOwners,originalRows,sourceCompatibleRows,nextRng;public double baselineMilliseconds,currentMilliseconds;public bool committed;public int[] portPasses;}
        [Serializable]public sealed class Census{public int seed,eligibleFields,selected,sourceCompatibleEntries,committed,refused;public string boundary;public List<CensusRow> rows=new List<CensusRow>();}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void RealCurrentPipelineAddsAtMostOneUsefulCoalsOwnerAndKeepsAllOrdinarySources(int seed)
        {
            var fields=Entries(seed);var ids=Selected(seed);Assert.IsNotEmpty(ids);
            var report=new Census{seed=seed,eligibleFields=fields.Length,selected=ids.Length,boundary="All fixed-seed selected current cooling fields; same-version composer-disabled pair, per-address global roll reset. Exact original owners/positions/parts plus explicit stock/gear/field/heat/fuel projection through composition and paired final resident names. <=60 static cardinal material-pass route is not survival/keyboard/timing proof. 0/1 utility is deliberately new; no v5 economy equivalence or full every-private-field claim. Timings are one paired sample per address, not significance or frame latency."};
            foreach(var id in ids)
            {
                Seed(seed,id);var baseline=new ObservedManager(scope.Factory,seed,true);var watch=System.Diagnostics.Stopwatch.StartNew();var b=baseline.GetZone(id);watch.Stop();double baselineMs=watch.Elapsed.TotalMilliseconds;Assert.NotNull(b);var br=new[]{LoadoutPart.Rng.Next(),TraderPart.Rng.Next(),LootDropSystem.Rng.Next()};
                Seed(seed,id);var current=new ObservedManager(scope.Factory,seed);watch.Restart();var z=current.GetZone(id);watch.Stop();double currentMs=watch.Elapsed.TotalMilliseconds;Assert.NotNull(z);var cr=new[]{LoadoutPart.Rng.Next(),TraderPart.Rng.Next(),LootDropSystem.Rng.Next()};
                Assert.NotNull(current.Composer);Assert.NotNull(current.Terrain);Assert.AreSame(z,current.Terrain.SourceZone);Assert.AreEqual(Formation.FieldStrips,current.Terrain.Plan.Formation);
                CollectionAssert.AreEqual(baseline.Shapes,current.Shapes);CollectionAssert.AreEqual(baseline.Positions,current.Positions);Assert.AreEqual(baseline.NextRng,current.NextRng);CollectionAssert.AreEqual(br,cr,"No caller or ambient/stock/loot RNG consumption by optional composition.");Assert.AreEqual(b.TileState.ToSaveString(),z.TileState.ToSaveString());
                bool done=current.Exploration.DispositionFor(id)==2;AssertConserved(current,z,done);
                for(int i=0;i<current.Before.Length;i++){var e=current.Before[i];Assert.AreEqual(Shape(baseline.Before[i]),Shape(e),"Paired final ordinary stock/gear/state including legitimate resident naming.");Assert.AreEqual(b.GetEntityPosition(baseline.Before[i]),z.GetEntityPosition(e));CollectionAssert.AreEqual(current.AfterParts[Array.IndexOf(current.After,e)],e.Parts);}
                int compatible=current.Receipts.Length;if(compatible>0)report.sourceCompatibleEntries++;int[] passes=null;
                if(done){report.committed++;Assert.AreEqual(1,current.Receipts.Count(r=>!r.IsCurrent),"Exactly one original row receipt authorizes the source.");Assert.AreEqual("CoolingWorkPatch",current.Composer.LastResult);Assert.AreEqual(1,current.Callbacks);var source=Coals(z);passes=Useful(current,z,source).passes;Assert.True(source.GetPart<CampfirePart>().FiniteCooking);Assert.False((bool)typeof(CampfirePart).GetField("AllowRest").GetValue(source.GetPart<CampfirePart>()));Assert.AreEqual(500,source.GetPart<ThermalPart>().Temperature);Assert.AreEqual(25,source.GetPart<FuelPart>().FuelMass);Assert.False(source.HasEffect<BurningEffect>());Assert.False(source.HasPart<ContainerPart>());Assert.False(source.HasPart<InventoryPart>());}
                else{report.refused++;Assert.True(current.Receipts.All(r=>r.IsCurrent));Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadCookingCoals"));}
                var facts=z.GetReadOnlyEntities().Select(Exact).ToArray();current.UnloadZone(id);Assert.AreSame(z,current.GetZone(id));CollectionAssert.AreEqual(facts,z.GetReadOnlyEntities().Select(Exact));
                report.rows.Add(new CensusRow{zone=id,result=current.Composer.LastResult,ordinaryOwners=current.Before.Length,originalRows=current.Before.Count(e=>e.BlueprintName=="RipeCropRow"),sourceCompatibleRows=compatible,nextRng=current.NextRng,baselineMilliseconds=baselineMs,currentMilliseconds=currentMs,committed=done,portPasses=passes});
            }
            string json=JsonUtility.ToJson(report,true);TestContext.WriteLine(json);string output=Environment.GetEnvironmentVariable("COO_COOKING_PIPELINE_CENSUS_OUTPUT");if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"seed-"+seed+".json"),json);}
            Assert.Greater(report.committed,0,"A useful source in each complete fixed selection cohort is required before registration; do not add grain or search another seed.");
        }
        string FindCommitted(int seed)
        {foreach(var id in Selected(seed)){Seed(seed,id);var m=new ObservedManager(scope.Factory,seed);if(m.GetZone(id)!=null&&m.Exploration.DispositionFor(id)==2)return id;}Assert.Fail("No actual committed source in the fixed cohort.");return null;}
        [TestCase("unchanged",true)][TestCase("offroute",true)][TestCase("replace-source",false)][TestCase("block-ports",false)]
        public void FinalCallbackRequiresExactSourceAndStillUsefulApproach(string mutation,bool accepted)
        {
            const int seed=64;string id=FindCommitted(seed);Seed(seed,id);var foreign=new List<Entity>();
            var m=new ObservedManager(scope.Factory,seed,after:(self,z)=>
            {
                var source=Coals(z);Useful(self,z,source);
                if(mutation=="replace-source"){var pos=z.GetEntityPosition(source);Assert.True(z.RemoveEntity(source));var other=scope.Factory.CreateEntity("SpreadCookingCoals");other.ID=source.ID;Assert.True(z.AddEntity(other,pos.x,pos.y));foreign.Add(other);}
                if(mutation=="block-ports"){var plan=self.Terrain.Plan;foreach(var pos in new[]{(0,plan.WestY),(Zone.Width-1,plan.EastY),(plan.NorthX,0),(plan.SouthX,Zone.Height-1)}){var wall=scope.Factory.CreateEntity("StoneWall");Assert.True(z.AddEntity(wall,pos.Item1,pos.Item2));Assert.True(z.GetCell(pos.Item1,pos.Item2).BlocksMovement());foreign.Add(wall);}}
                if(mutation=="offroute"){var item=scope.Factory.CreateEntity("Cudgel");Assert.True(z.AddEntity(item,0,0));foreign.Add(item);}
            });
            var result=m.GetZone(id);Assert.AreEqual(1,m.Callbacks,"Prove actual placement before the callback stimulus.");
            if(accepted){Assert.NotNull(result);Assert.AreEqual(2,m.Exploration.DispositionFor(id));}
            else{Assert.IsNull(result);Assert.Zero(m.CachedZoneCount);Assert.Zero(m.Exploration.DispositionFor(id));Assert.IsNotEmpty(foreign);foreach(var e in foreign)Assert.NotNull(m.Generated.GetEntityCell(e),"Refusal cannot erase independent callback ownership.");}
        }
        [TestCase(5,true)][TestCase(67,false)]
        public void ActualHarvestAndCoolingSurviveInactiveSaveWithoutRestoringHeatOrGrain(int passes,bool ready)
        {
            const int seed=64;string id=FindCommitted(seed);Seed(seed,id);var m=new ObservedManager(scope.Factory,seed);var z=m.GetZone(id);var source=Coals(z);var route=Useful(m,z,source);var row=route.row;var sourcePos=z.GetEntityPosition(source);var rowPos=z.GetEntityPosition(row);
            var player=scope.Factory.CreateEntity("Player");Assert.False(player.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="Emberwheat"));Assert.True(z.AddEntity(player,route.rowStand.x,route.rowStand.y));m.SetActiveZone(z);SettlementRuntime.ActiveZone=z;
            Assert.True(InventorySystem.PerformAction(player,row,"Harvest",z));Assert.True(row.GetPart<FieldHarvestPart>().Harvested);var raw=player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Emberwheat");Assert.AreEqual(1,raw.GetPart<StackerPart>().StackCount);Assert.AreSame(player,raw.GetPart<PhysicsPart>().InInventory);
            for(int i=0;i<passes;i++)MaterialSimSystem.TickMaterialEntities(z);float heat=source.GetPart<ThermalPart>().Temperature,fuel=source.GetPart<FuelPart>().FuelMass;Assert.AreEqual(ready,heat>=CookingService.MinimumFiniteCookingTemperature);Assert.AreEqual(25,fuel);Assert.False(source.HasEffect<BurningEffect>());
            var expected=z.GetReadOnlyEntities().Where(e=>!ReferenceEquals(e,player)).ToDictionary(e=>e.ID,e=>(pos:z.GetEntityPosition(e),facts:Exact(e)));string rawID=raw.ID;int money=TradeSystem.GetDrams(player);var gear=Gear(player).Select(Exact).ToArray();
            // Disclosed core transfer, not native travel. The earned source graph is inactive at save.
            var away=m.GetZone(ReferenceGladePlan.ZoneID);Assert.NotNull(away);Cell target=null;away.ForEachCell((c,x,y)=>{if(target==null&&away.CanPlaceFootprint(player,x,y))target=c;});Assert.NotNull(target);Assert.True(z.RemoveEntity(player));Assert.True(away.AddEntity(player,target.X,target.Y));m.SetActiveZone(away);SettlementRuntime.ActiveZone=away;m.UnloadZone(id);Assert.AreSame(z,m.GetZone(id));
            var state=GameSessionState.Capture("finite-workspot","direct-core-inactive-save",m,null,player);GameSessionState loaded;using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
            var restored=(OverworldZoneManager)loaded.ZoneManager;Assert.AreNotSame(m,restored);Assert.AreNotSame(player,loaded.Player);Assert.AreEqual(ReferenceGladePlan.ZoneID,restored.ActiveZone.ZoneID);Assert.AreEqual(7,restored.Exploration.Version);Assert.AreEqual(2,restored.Exploration.DispositionFor(id));var returned=restored.GetZone(id);Assert.AreNotSame(z,returned);Assert.AreEqual(expected.Count,returned.EntityCount);Assert.AreEqual(money,TradeSystem.GetDrams(loaded.Player));CollectionAssert.AreEqual(gear,Gear(loaded.Player).Select(Exact));
            foreach(var e in returned.GetReadOnlyEntities()){Assert.True(expected.ContainsKey(e.ID));Assert.AreEqual(expected[e.ID].pos,returned.GetEntityPosition(e));Assert.AreEqual(expected[e.ID].facts,Exact(e));}
            var saved=returned.GetReadOnlyEntities().Single(e=>e.ID==source.ID);var cut=returned.GetReadOnlyEntities().Single(e=>e.ID==row.ID);Assert.AreNotSame(source,saved);Assert.AreNotSame(row,cut);Assert.AreEqual(sourcePos,returned.GetEntityPosition(saved));Assert.AreEqual(rowPos,returned.GetEntityPosition(cut));Assert.True(cut.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(heat,saved.GetPart<ThermalPart>().Temperature);Assert.AreEqual(fuel,saved.GetPart<FuelPart>().FuelMass);Assert.True(saved.GetPart<CampfirePart>().FiniteCooking);Assert.False((bool)typeof(CampfirePart).GetField("AllowRest").GetValue(saved.GetPart<CampfirePart>()));
            restored.UnloadZone(id);Assert.AreSame(returned,restored.GetZone(id));Assert.AreEqual(1,returned.GetReadOnlyEntities().Count(e=>e.BlueprintName=="SpreadCookingCoals"));Assert.AreEqual(heat,saved.GetPart<ThermalPart>().Temperature,"Return does not restart the finite window.");
            var earned=loaded.Player.GetPart<InventoryPart>().Objects.Single(e=>e.ID==rawID);Assert.AreNotSame(raw,earned);Assert.AreSame(loaded.Player,earned.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(1,earned.GetPart<StackerPart>().StackCount);
            Assert.True(restored.ActiveZone.RemoveEntity(loaded.Player));Assert.True(returned.AddEntity(loaded.Player,route.stationStand.x,route.stationStand.y));restored.SetActiveZone(returned);SettlementRuntime.ActiveZone=returned;
            Assert.AreEqual(ready,CookingService.TryCook(loaded.Player,earned,returned,scope.Factory));Assert.AreEqual(ready?1:0,loaded.Player.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="ToastedEmberwheat"));Assert.AreEqual(ready?0:1,loaded.Player.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="Emberwheat"));Assert.AreEqual(heat,saved.GetPart<ThermalPart>().Temperature);Assert.AreEqual(fuel,saved.GetPart<FuelPart>().FuelMass);
        }
    }
}
