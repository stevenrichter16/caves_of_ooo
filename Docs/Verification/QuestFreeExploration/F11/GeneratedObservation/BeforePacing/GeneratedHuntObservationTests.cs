using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 // Private observations of actual generated sites. No required winner and no native-input claim.
 public sealed class GeneratedHuntObservationTests
 {
  const string Root="/Users/steven/.codex/scratch/caves-of-ooo/f11/mechanics-generated-observation";
  public sealed class Selection { public List<Site> rows; }
  public sealed class Site { public int seed; public string zone,variant,expectedHunter,expectedPrey; }
  public sealed class Observation
  {
   public Site site; public string status="preparing",exception,actualVariant,hunterID,preyID,playerID,watchCell,initialHunter,initialPrey;
   public int disposition,waits,ticks,creatureCount; public bool coordinateMatchesPriorCensus;
   public string boundary="Actual candidate source/factory/generated terrain, ordinary all-creature TurnManager. One factory Player setup placement with no grants. Unseeded live brain/death/drop RNG; deterministic runner world hash differs from Unity. World TickEnd services are absent; no native keys, rendering, FOV, or balance proof.";
   public List<object> path=new List<object>(),events=new List<object>(),diagnostics=new List<object>(); public object terrain,final;
  }
  static string Pos(Zone z,Entity e)=>e==null||z.GetEntityCell(e)==null?null:z.GetEntityPosition(e).ToString();
  static int Dist(Zone z,Entity a,Entity b){var ac=z.GetEntityCell(a);var bc=z.GetEntityCell(b);return ac==null||bc==null?-1:AIHelpers.ChebyshevDistance(ac.X,ac.Y,bc.X,bc.Y);}
  static bool Los(Zone z,Entity a,Entity b){var ac=z.GetEntityCell(a);var bc=z.GetEntityCell(b);return ac!=null&&bc!=null&&AIHelpers.HasLineOfSight(z,ac.X,ac.Y,bc.X,bc.Y);}
  static bool Terminal(SpreadHuntPhase p)=>p==SpreadHuntPhase.Escaped||p==SpreadHuntPhase.Exhausted||p==SpreadHuntPhase.Aborted||p==SpreadHuntPhase.PreyGone||p==SpreadHuntPhase.Fed;
  static object Actor(Zone z,Entity e,TurnManager t)
  {var b=e?.GetPart<BrainPart>();return new{id=e?.ID,bp=e?.BlueprintName,at=Pos(z,e),hp=e?.GetStatValue("Hitpoints"),dead=e!=null&&CombatSystem.IsDeathHandled(e),state=b?.CurrentState.ToString(),target=b?.Target?.ID,targetBP=b?.Target?.BlueprintName,goals=b?.GetGoalsSnapshot().Select(g=>g.GetType().Name).ToArray(),registered=e!=null&&t.IsRegistered(e),energy=e==null?0:t.GetEnergy(e),speed=e==null?0:t.GetSpeed(e)};}
  static object Pair(Zone z,Entity h,Entity p,Entity player,SpreadPredatorPart role,TurnManager t,int waits,Entity[] originalNPCs)
  {var g=p.GetPart<SpreadGrazerPart>();return new{waits,tick=t.TickCount,phase=role.Phase.ToString(),role.HasLastSeen,role.LastSeenX,role.LastSeenY,role.PursuitRemaining,role.SearchRemaining,role.FeedProgress,role.PreyID,preyRef=role.Prey?.ID,role.CorpseID,corpseRef=role.Corpse?.ID,corpseAt=Pos(z,role.Corpse),corpseCurrent=role.Corpse?.SpatialZone==z,flight=g.FlightRemaining,threatX=g.ThreatX,threatY=g.ThreatY,reciprocalHunter=g.Hunter?.ID,los=Los(z,h,p),distance=Dist(z,h,p),playerDistance=Dist(z,h,player),playerLos=Los(z,h,player),hunter=Actor(z,h,t),prey=Actor(z,p,t),player=Actor(z,player,t),others=originalNPCs.Where(e=>e!=h&&e!=p).Select(e=>Actor(z,e,t)).ToArray()};}
  static void Write(string path,Observation report)=>File.WriteAllText(path,JsonConvert.SerializeObject(report,Formatting.Indented));
  static (int x,int y)? Watch(Zone z,Entity h,Entity p)
  {
   var geometry=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>());var a=z.GetEntityPosition(h);var b=z.GetEntityPosition(p);
   var candidates=new List<(int x,int y)>();
   for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
   {if(!geometry.Place(x,y)||AIHelpers.ChebyshevDistance(x,y,a.x,a.y)<25||AIHelpers.ChebyshevDistance(x,y,b.x,b.y)<15)continue;candidates.Add((x,y));}
   if(candidates.Count==0)return null;
   return candidates.OrderByDescending(c=>AIHelpers.HasLineOfSight(z,c.x,c.y,a.x,a.y)&&AIHelpers.HasLineOfSight(z,c.x,c.y,b.x,b.y)).ThenBy(c=>AIHelpers.ChebyshevDistance(c.x,c.y,a.x,a.y)).ThenBy(c=>c.y).ThenBy(c=>c.x).First();
  }
  void Run(int start,int count)
  {
   var cohort=JsonConvert.DeserializeObject<Selection>(File.ReadAllText(Root+"/cohort-coordinate-update-before-observation.json"));Assert.AreEqual(18,cohort.rows.Count);
   Directory.CreateDirectory(Root+"/results");
   for(int i=start;i<start+count;i++)Observe(i,cohort.rows[i]);
  }
  [Test] public void A_FirstTwoPredeterminedCoveredSites()=>Run(0,2);
  [Test] public void B_RemainingSixteenFrozenSites()=>Run(2,16);
  void Observe(int index,Site site)
  {
   string path=Root+"/results/"+index.ToString("D2")+"-"+site.seed+"-"+site.zone+".json";
   Assert.False(File.Exists(path),"Never silently rerun a completed/partial observation.");
   var report=new Observation{site=site};Write(path,report);
   var oldCorpse=CorpsePart.Factory;var oldWorld=TurnManager.World;
   var moved=EntityVisualHooks.MovedCallback;var attacked=EntityVisualHooks.AttackCallback;var interacted=EntityVisualHooks.InteractionCallback;var damaged=EntityVisualHooks.DamageCallback;var died=EntityVisualHooks.DeathCallback;
   try
   {using(var scope=new HaulingContentScope())
    {
     scope.Factory.LoadBlueprints(File.ReadAllText(Root+"/snapshot/Assets/Resources/Content/Blueprints/Objects.json"));
     LoadoutPart.Rng=null;TraderPart.Rng=null;LootDropSystem.Rng=null;CorpsePart.Factory=scope.Factory;
     FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Factions.json")));
     var manager=OverworldZoneManager.CreateDetached(scope.Factory,site.seed,true);Assert.AreEqual(8,manager.Exploration.Version);
     Assert.AreEqual("HuntThroughCover",manager.Exploration.Find(site.zone)?.Family.ToString());
     report.actualVariant=SpreadExplorationPlan.Rank(site.seed,site.zone,"hunt-variant")%2==0?"covered":"open";Assert.AreEqual(site.variant,report.actualVariant);
     var z=manager.GetZone(site.zone);Assert.NotNull(z);manager.SetActiveZone(z);report.disposition=manager.Exploration.DispositionFor(site.zone);
     var hunters=z.GetReadOnlyEntities().Where(e=>e.HasPart<SpreadPredatorPart>()).ToArray();
     if(hunters.Length!=1||report.disposition!=2){report.status="no-committed-pair";return;}
     var h=hunters.Single();var role=h.GetPart<SpreadPredatorPart>();var p=role.Prey;Assert.NotNull(p);Assert.AreEqual("ReedbackGrazer",p.BlueprintName);
     report.hunterID=h.ID;report.preyID=p.ID;report.initialHunter=Pos(z,h);report.initialPrey=Pos(z,p);
     report.coordinateMatchesPriorCensus=report.initialHunter==site.expectedHunter&&report.initialPrey==site.expectedPrey;
     var watch=Watch(z,h,p);if(watch==null){report.status="no-safe-watch-cell";return;}
     var player=scope.Factory.CreateEntity("Player");Assert.NotNull(player);report.playerID=player.ID;
     Assert.True(z.AddEntity(player,watch.Value.x,watch.Value.y));report.watchCell=Pos(z,player);
     var npcs=z.GetEntitiesWithTag("Creature").Where(e=>e!=player).ToArray();report.creatureCount=npcs.Length+1;
     Assert.Contains(h,npcs);Assert.Contains(p,npcs);
     foreach(var npc in npcs){var brain=npc.GetPart<BrainPart>();if(brain!=null){brain.CurrentZone=z;brain.Rng=new Random();}}
     var turns=new TurnManager();TurnManager.World=null;turns.AddEntity(player);foreach(var npc in npcs)turns.AddEntity(npc);
     // Same zone-entry registration shape: original player already first, all current NPCs added in zone order.
     Assert.True(npcs.All(turns.IsRegistered));
     report.terrain=new{trees=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").Select(e=>new{id=e.ID,at=Pos(z,e)}).ToArray(),initialNPCs=npcs.Select(e=>Actor(z,e,turns)).ToArray()};
     Diag.ResetAll();Diag.SetChannel("turn-verbose",true);Diag.SetChannel("turn",true);var observed=new HashSet<string>();
     void Trace()
     {foreach(var entry in Diag.Snapshot(8192))if(observed.Add(entry.TraceId))report.diagnostics.Add(new{entry.Category,entry.Kind,entry.Turn,entry.ActorId,entry.TargetId,entry.PayloadJson});}
     void Event(string kind,Entity actor,Entity target,object detail)
     {report.events.Add(new{kind,tick=turns.TickCount,waits=report.waits,actor=actor?.ID,actorBP=actor?.BlueprintName,target=target?.ID,targetBP=target?.BlueprintName,detail,pair=Pair(z,h,p,player,role,turns,report.waits,npcs)});}
     EntityVisualHooks.MovedCallback=(e,zone,ox,oy,nx,ny,forced)=>Event("move",e,null,new{ox,oy,nx,ny,forced});
     EntityVisualHooks.AttackCallback=(a,b,zone)=>Event("attack",a,b,null);
     EntityVisualHooks.InteractionCallback=(a,b,zone)=>Event("interact",a,b,null);
     EntityVisualHooks.DamageCallback=(target,source,zone,amount,lethal)=>Event("damage",source,target,new{amount,lethal});
     EntityVisualHooks.DeathCallback=(target,killer,zone,x,y)=>Event("death",killer,target,new{x,y});
     report.path.Add(Pair(z,h,p,player,role,turns,0,npcs));
     var current=turns.ProcessUntilPlayerTurn();Trace();report.path.Add(Pair(z,h,p,player,role,turns,0,npcs));
     while(current==player&&!Terminal(role.Phase)&&report.waits<40&&turns.TickCount<500)
     {turns.EndTurn(player,z);report.waits++;current=turns.ProcessUntilPlayerTurn();Trace();report.path.Add(Pair(z,h,p,player,role,turns,report.waits,npcs));report.ticks=turns.TickCount;Write(path,report);}
     report.ticks=turns.TickCount;
     report.status=current!=player?"player-unavailable":Terminal(role.Phase)?"terminal-"+role.Phase:report.waits>=40?"wait-bound":"tick-bound";
     report.final=new{phase=role.Phase.ToString(),hunterHP=h.GetStatValue("Hitpoints"),preyHP=p.GetStatValue("Hitpoints"),playerHP=player.GetStatValue("Hitpoints"),hunterDead=CombatSystem.IsDeathHandled(h),preyDead=CombatSystem.IsDeathHandled(p),playerDead=CombatSystem.IsDeathHandled(player),corpseClaim=role.CorpseID,role.FeedProgress,corpses=z.GetReadOnlyEntities().Where(e=>e.GetProperty("SourceID")==p.ID||e.GetProperty("SourceID")==h.ID).Select(e=>new{id=e.ID,bp=e.BlueprintName,at=Pos(z,e),source=e.GetProperty("SourceID")}).ToArray()};
     Assert.LessOrEqual(report.waits,40);Assert.LessOrEqual(turns.TickCount,500);
    }}
   catch(Exception ex){report.status="exception";report.exception=ex.ToString();throw;}
   finally{Write(path,report);EntityVisualHooks.MovedCallback=moved;EntityVisualHooks.AttackCallback=attacked;EntityVisualHooks.InteractionCallback=interacted;EntityVisualHooks.DamageCallback=damaged;EntityVisualHooks.DeathCallback=died;CorpsePart.Factory=oldCorpse;TurnManager.World=oldWorld;}
  }
 }
}
