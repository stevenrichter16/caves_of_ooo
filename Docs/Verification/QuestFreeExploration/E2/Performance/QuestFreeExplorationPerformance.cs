using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Experiments
{
 public static class QuestFreeExplorationPerformance
 {
  const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
  public sealed class Sample { public string phase,arm,zone,result; public int seed,iteration,operations,owners,retained,bytes,gc0,gc1,gc2; public double milliseconds; public long? allocatedBytes; public long heapBefore,heapAfter; }
  public sealed class Report
  {
   public string runtime,unity,beganUtc,endedUtc,boundary;public bool complete,restored,allocationCounterValidated;
   public long allocationProbeBytes;public List<Sample> samples=new List<Sample>();public Dictionary<string,string> cohorts=new Dictionary<string,string>();public List<string> errors=new List<string>();
  }
  static Func<long> allocation;
  static Type TypeOf(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).FirstOrDefault(t=>t!=null)??throw new InvalidOperationException("Missing loaded fixture "+name);
  static object Get(object target,string name)=>target.GetType().GetField(name,All).GetValue(target);
  static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,All).Invoke(target,args);
  static void Need(bool value,string text){if(!value)throw new InvalidOperationException(text);}
  public static string Run(string frozenCohortDirectory,string outputPath,string runtimeLabel)
  {
   Need(!UnityEngine.Application.isPlaying,"Idle fixture probe only.");
   var report=new Report{runtime=runtimeLabel,unity=(string)typeof(UnityEngine.Application).GetProperty("unityVersion",BindingFlags.Public|BindingFlags.Static)?.GetValue(null)??"unavailable-stub",beganUtc=DateTime.UtcNow.ToString("O"),boundary="Current explicit legacy-disabled versus current exploration-enabled runtime, not an archived old binary. One alternating arm pair per each of three frozen E0 twenty-zone cohorts. Constructor/generation/save/load separately timed, source fixture setup and assertions excluded. Managed thread allocation counter validated with a retained64KiB probe; nullable if unsupported. GC.GetTotalMemory(false) is a noisy process-wide estimate, not exact retained graph bytes. No forced GC, profiler changes, Assets writes, ordinary input, GPU/frame timing or claimed significance. Role samples are32 fresh synthetic dense-cell one-action schedulers per branch, not generated encounters or a campaign; baseline owns the same two actors/cells but lacks the role."};
   var kinds=new[]{typeof(LootTableRegistry),typeof(FactionManager),typeof(PlayerReputation),typeof(LiquidRegistry),typeof(GasRegistry),typeof(Diag),typeof(TurnManager),typeof(WorldClock),typeof(MessageLog),typeof(ZoneRenderHooks),typeof(EntityVisualHooks),typeof(AsciiFxBus),typeof(SpellFxBus)};
   var states=new List<StaticState>();
   try
   {
    foreach(var kind in kinds)states.Add(new StaticState(kind));
    ValidateAllocation(report);
    for(int index=0;index<3;index++)
    {
     int seed=new[]{1,64,1729}[index];string file=Path.Combine(frozenCohortDirectory,"seed-"+seed+"-cohort.json");var json=JObject.Parse(File.ReadAllText(file));
     Need((string)json["protocol"]=="QuestFreeExploration.BFS20.v1"&&(int)json["seed"]==seed&&(bool)json["complete"],"Exact E0 cohort protocol/seed/complete");
     string[] ids=((JArray)json["nodes"]).Select(n=>(string)n["id"]).ToArray();Need(ids.Length==20&&ids.Distinct().Count()==20,"Exact frozen twenty distinct IDs");report.cohorts[file]=Hash(file);
     foreach(bool enabled in index%2==0?new[]{false,true}:new[]{true,false})Cold(report,seed,ids,enabled);
    }
    foreach(string kind in new[]{"territory-warning","grazer-feed","grazer-flight"})for(int i=0;i<32;i++)Role(report,kind,i);
    report.complete=report.errors.Count==0;
   }
   catch(Exception e){report.errors.Add(e.ToString());report.complete=false;}
   finally
   {
    report.restored=true;for(int i=states.Count-1;i>=0;i--)try{states[i].Dispose();}catch(Exception e){report.restored=false;report.errors.Add("Restore: "+e);}
    if(!report.restored)report.complete=false;report.endedUtc=DateTime.UtcNow.ToString("O");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));string encoded=JsonConvert.SerializeObject(report,Formatting.Indented);var parsed=JObject.Parse(encoded);Need(((JArray)parsed["samples"]).Count==report.samples.Count,"Serialized sample count");File.WriteAllText(outputPath,encoded);
   }
   return outputPath;
  }
  static void ValidateAllocation(Report report)
  {
   allocation=null;var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",BindingFlags.Public|BindingFlags.Static);
   if(method==null)return;try{allocation=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),method);allocation();long before=allocation();var probe=new byte[65536];probe[0]=1;long delta=allocation()-before;GC.KeepAlive(probe);report.allocationProbeBytes=delta;report.allocationCounterValidated=delta>=65536;if(!report.allocationCounterValidated)allocation=null;}catch{allocation=null;}
  }
  static Sample Timed(string phase,string arm,int seed,int operations,Action action)
  {
   var s=new Sample{phase=phase,arm=arm,seed=seed,operations=operations,heapBefore=GC.GetTotalMemory(false)};long before=allocation?.Invoke()??0;int g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);long start=Stopwatch.GetTimestamp();action();long end=Stopwatch.GetTimestamp();long after=allocation?.Invoke()??0;
   s.milliseconds=(end-start)*1000d/Stopwatch.Frequency;s.allocatedBytes=allocation==null?(long?)null:after-before;s.gc0=GC.CollectionCount(0)-g0;s.gc1=GC.CollectionCount(1)-g1;s.gc2=GC.CollectionCount(2)-g2;s.heapAfter=GC.GetTotalMemory(false);return s;
  }
  static void Cold(Report report,int seed,string[] ids,bool enabled)
  {
   IDisposable scope=null,drama=null;string arm=enabled?"exploration-current":"legacy-current";
   try
   {
    scope=(IDisposable)Activator.CreateInstance(TypeOf("CavesOfOoo.Tests.DensityLootTestScope"),true);var factory=(EntityFactory)Get(scope,"Factory");Call(scope,"Seed",seed);
    string content=Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data");
    LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(content,"LiquidDefinitions"),"*.json").Select(File.ReadAllText));GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(content,"GasDefinitions"),"*.json").Select(File.ReadAllText));FactionManager.Initialize(File.ReadAllText(Path.Combine(content,"Factions.json")));
    drama=(IDisposable)Activator.CreateInstance(TypeOf("CavesOfOoo.Tests.SpreadBiomeCoverageTests+CensusDramaScope"),true);
    OverworldZoneManager manager=null;report.samples.Add(Timed("manager-constructor",arm,seed,1,()=>manager=OverworldZoneManager.CreateDetached(factory,seed,enabled)));Need(manager.Exploration.Enabled==enabled&&manager.CachedZoneCount==0,"Selected constructor mode without generation");
    foreach(string id in ids)
    {
     Need(!manager.CachedZones.ContainsKey(id),"Cold graph before timed GetZone");Zone zone=null;var row=Timed("cold-GetZone",arm,seed,1,()=>zone=manager.GetZone(id));row.zone=id;row.result=zone==null?"refused":"accepted";row.owners=zone?.EntityCount??0;row.retained=manager.Exploration.RetainedGraphCount;report.samples.Add(row);
     Need(zone!=null&&ReferenceEquals(manager.CachedZones[id],zone),"Actual cold cohort graph accepted "+seed+" "+arm+" "+id);
    }
    Need(manager.CachedZoneCount==20,"Exactly twenty cached graphs");var before=Signature(manager);var state=GameSessionState.Capture("questfree-perf","probe",manager,null,null);byte[] bytes=null;
    var save=Timed("save20-memory",arm,seed,20,()=>{using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));bytes=stream.ToArray();}});save.bytes=bytes.Length;save.owners=manager.CachedZones.Values.Sum(z=>z.EntityCount);save.retained=manager.Exploration.RetainedGraphCount;report.samples.Add(save);
    GameSessionState loaded=null;var load=Timed("load20-memory",arm,seed,20,()=>{using(var stream=new MemoryStream(bytes,false))loaded=GameSessionState.Load(new SaveReader(stream,factory));});load.bytes=bytes.Length;load.owners=loaded.ZoneManager.CachedZones.Values.Sum(z=>z.EntityCount);load.retained=loaded.ZoneManager.Exploration.RetainedGraphCount;report.samples.Add(load);
    Need(loaded.ZoneManager!=manager&&loaded.ZoneManager.Exploration.Enabled==enabled&&loaded.ZoneManager.CachedZoneCount==20,"Exact twenty-graph replacement mode/count");
    string after=Signature(loaded.ZoneManager);if(after!=before){int different=0;while(different<Math.Min(after.Length,before.Length)&&after[different]==before[different])different++;throw new InvalidOperationException("Projection first mismatch at "+different+" old="+before.Substring(Math.Max(0,different-80),Math.Min(400,before.Length-Math.Max(0,different-80)))+" new="+after.Substring(Math.Max(0,different-80),Math.Min(400,after.Length-Math.Max(0,different-80))));}
    foreach(string id in ids)Need(!ReferenceEquals(manager.CachedZones[id],loaded.ZoneManager.CachedZones[id]),"Replacement graph "+id);
    GC.KeepAlive(manager);GC.KeepAlive(loaded);GC.KeepAlive(bytes);
   }
   finally{try{drama?.Dispose();}finally{scope?.Dispose();}}
  }
  static string CanonicalTiles(Zone zone)
  {
   var json=JObject.Parse(zone.TileState.ToSaveString());var entries=(JArray)json["Entries"];
   json["Entries"]=new JArray(entries.OrderBy(e=>(int)e["Key"]));return json.ToString(Formatting.None);
  }
  static string Signature(OverworldZoneManager manager)=>string.Join("|",manager.CachedZones.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+":"+CanonicalTiles(p.Value)+":"+string.Join(";",p.Value.GetReadOnlyEntities().OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+","+e.BlueprintName+","+p.Value.GetEntityPosition(e)+","+e.GetPart<LiquidPoolPart>()?.Volume+","+e.GetPart<FieldHarvestPart>()?.Harvested))));
  static void Role(Report report,string kind,int iteration)
  {
   foreach(bool enabled in iteration%2==0?new[]{false,true}:new[]{true,false})
   {
    var fixture=(IDisposable)Activator.CreateInstance(TypeOf("CavesOfOoo.Tests.SpreadExplorationActorTests+Fixture"),true);
    try
    {
     var actor=(Entity)Get(fixture,"Actor");var player=(Entity)Get(fixture,"Player");var zone=(Zone)Get(fixture,"Zone");
     // Deliberately dense inert ground, outside timer, gives every scanned cell a real owner.
     for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var floor=new Entity{BlueprintName="probe-floor"};floor.SetTag("Terrain");Need(zone.AddEntity(floor,x,y),"Dense inert cell fixture");}
     if(kind=="territory-warning"){if(enabled)Call(fixture,"Territory");Call(fixture,"Move",player,11,10);}
     else{actor.GetPart<BrainPart>().Passive=true;if(enabled)Call(fixture,"Grazer");if(kind=="grazer-flight")Call(fixture,"Move",player,12,10);}
     var turn=new TurnManager();turn.AddEntity(actor);turn.AddEntity(player);Entity returned=null;
     var row=Timed("dense-role:"+kind,enabled?"configured-role":"ordinary-no-role",1,1,()=>returned=turn.ProcessUntilPlayerTurn());row.iteration=iteration;row.owners=zone.EntityCount;report.samples.Add(row);
     Need(ReferenceEquals(returned,player)&&turn.GetEnergy(actor)==0&&(int)Get(actor.Parts.Single(p=>p.Name=="ExplorationAttackProbe"),"Ends")==1,"Exactly one completed actor action before player");
     if(enabled)
     {
      if(kind=="territory-warning")Need(actor.GetPart<SpreadTerritoryPart>().WarningTarget==player&&actor.GetPart<SpreadTerritoryPart>().GraceRemaining==2,"Actual warning role path");
      if(kind=="grazer-feed")Need(actor.GetPart<SpreadGrazerPart>().Fed&&((Entity)Get(fixture,"Food")).GetPart<FieldHarvestPart>().Harvested&&!((Entity)Get(fixture,"Reserve")).GetPart<FieldHarvestPart>().Harvested,"Actual finite feeding role path");
      if(kind=="grazer-flight")Need(zone.GetEntityPosition(actor)==(9,10)&&!actor.GetPart<SpreadGrazerPart>().Fed,"Actual healthy flight role path");
     }
    }
    finally{fixture.Dispose();}
   }
  }
  static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  sealed class StaticState:IDisposable
  {
   sealed class Entry{internal FieldInfo field;internal object value;internal Array array;internal DictionaryEntry[] dict;internal object[] items;internal MethodInfo clear,add;}
   readonly List<Entry> entries=new List<Entry>();
   internal StaticState(Type type)
   {
    foreach(var f in type.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
    {
     if(f.IsLiteral)continue;var e=new Entry{field=f,value=f.GetValue(null)};
     if(e.value is Array a)e.array=(Array)a.Clone();else if(e.value is IDictionary d){var list=new List<DictionaryEntry>();foreach(DictionaryEntry item in d)list.Add(item);e.dict=list.ToArray();}
     else if(e.value is IEnumerable enumerable&&!(e.value is string)){var t=e.value.GetType();e.clear=t.GetMethod("Clear",Type.EmptyTypes);e.add=t.GetMethods().FirstOrDefault(m=>(m.Name=="Add"||m.Name=="Enqueue")&&m.GetParameters().Length==1);if(e.clear!=null&&e.add!=null)e.items=enumerable.Cast<object>().ToArray();}
     if(!f.IsInitOnly||e.array!=null||e.dict!=null||e.items!=null)entries.Add(e);
    }
   }
   public void Dispose()
   {
    foreach(var e in entries){if(!e.field.IsInitOnly)e.field.SetValue(null,e.value);if(e.array!=null)Array.Copy(e.array,(Array)e.value,e.array.Length);else if(e.dict!=null){var d=(IDictionary)e.value;d.Clear();foreach(var pair in e.dict)d.Add(pair.Key,pair.Value);}else if(e.items!=null){e.clear.Invoke(e.value,null);foreach(var item in e.items)e.add.Invoke(e.value,new[]{item});}if(!ReferenceEquals(e.field.GetValue(null),e.value)&&!Equals(e.field.GetValue(null),e.value))throw new InvalidOperationException("Static restore "+e.field.Name);}
   }
  }
 }
}
