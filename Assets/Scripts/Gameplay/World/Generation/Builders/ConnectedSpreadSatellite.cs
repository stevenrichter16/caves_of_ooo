using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
 /// <summary>One cold, finite satellite from an actual rolled cache and (for the
 /// wet variant) exactly two original Scrabblers. It neither rolls nor respawns sources.</summary>
 public static class ConnectedSpreadSatellite
 {
  public const string RoleKey="ConnectedSatellite.Role";
  public const string RefusalKind="ConnectedSatelliteRefused";
  const int HalfWall=6,MaxTrials=128;
  static readonly (int x,int y) Arrival=(40,12);
  public static bool TryPlace(Zone zone,EntityFactory factory,SpreadCompositionBuilder terrain,
   PopulationBuilder population,ContainerBuilder containers,SpreadExplorationFamily family,Func<bool> authority,
   out Entity[] owners,out Func<bool> final)
  {
   owners=null;final=null;bool wet=family==SpreadExplorationFamily.WetCrossing;
   if((!wet&&family!=SpreadExplorationFamily.HeavyFrame)||zone==null||factory==null||authority==null)return false;
   var plan=terrain?.Plan;var actors=population?.SourceReceipt;var stock=containers?.SourceReceipt;
   string search="not-searched";
   bool Fail(string reason)
   {
    if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen",RefusalKind,payload:new{
     zone=zone.ZoneID,seed=plan?.Seed,family=family.ToString(),reason,search,
     actorReceiptCurrent=actors?.IsCurrent==true,stockReceiptCurrent=stock?.IsCurrent==true,
     actors=actors==null?"absent":string.Join(";",actors.Owners.Select(e=>e==null?"null":e.BlueprintName+"@"+zone.GetEntityPosition(e)+":eligible="+OrdinaryActor(zone,e)+":parts="+string.Join(",",e.Parts.Select(p=>p?.Name??"null")))),
     caches=stock==null?"absent":string.Join(";",stock.Owners.Select(e=>e==null?"null":e.BlueprintName+"@"+zone.GetEntityPosition(e)+":eligible="+Cache(zone,e)+":items="+(e.GetPart<ContainerPart>()?.Contents?.Count??-1)+"/"+(e.GetPart<ContainerPart>()?.MaxItems??-1)))
    });return false;
   }
   bool Identity()=>terrain?.SourceZone==zone&&terrain.Plan==plan&&plan?.ZoneID==zone.ZoneID
    &&population?.CaptureSourceReceipts==true&&population.SourceReceipt==actors&&actors?.Zone==zone&&actors.Factory==factory
    &&containers?.CaptureSourceReceipts==true&&containers.SourceReceipt==stock&&stock?.Zone==zone&&stock.Factory==factory;
   bool Sources()=>Identity()&&actors.IsCurrent&&stock.IsCurrent&&(!wet||(actors.Owners.Count==2&&actors.Owners.All(e=>OrdinaryActor(zone,e))));
   if(!Sources()||!authority()||!Sources()||zone.GetReadOnlyEntities().Any(e=>e.Properties.ContainsKey(RoleKey)))return Fail("source-admission");
   var cache=stock.Owners.FirstOrDefault(e=>Cache(zone,e)&&(e.GetPart<ContainerPart>().MaxItems<0||e.GetPart<ContainerPart>().Contents.Count+(wet?4:3)<=e.GetPart<ContainerPart>().MaxItems));if(cache==null)return Fail("no-eligible-cache-capacity");
   var moving=(wet?actors.Owners:Array.Empty<Entity>()).Concat(new[]{cache}).ToArray();var selected=new HashSet<Entity>(moving);
   var all=new HashSet<Entity>(zone.GetReadOnlyEntities());var initial=SpreadGenerationReceipt.CaptureFinalState(zone,all);
   var unchanged=SpreadGenerationReceipt.CaptureFinalState(zone,all.Where(e=>!selected.Contains(e)));
   var before=new SpreadWildernessSituationBuilder.Geometry(zone,selected);
   var design=Find(zone,before,selected,wet,zone.GetEntityPosition(cache),out search);if(design==null)return Fail("no-layout");
   var specs=design.Specs();var goodsNames=wet?new[]{"Emberwheat","Emberwheat","ClaspbeanPulp","DitchkeepersFootwork"}:new[]{"FireClay","FireClay","DitchkeepersFootwork"};
   if(specs.Any(s=>!factory.Blueprints.ContainsKey(s.bp))||goodsNames.Any(bp=>!factory.Blueprints.ContainsKey(bp)))return Fail("missing-blueprint");
   var container=cache.GetPart<ContainerPart>();if(container.MaxItems>=0&&container.Contents.Count+goodsNames.Length>container.MaxItems)return Fail("cache-capacity-changed");
   var staged=new List<Entity>();var goods=new List<Entity>();var freshProof=new List<Func<bool>>();
   bool Initial()=>Sources()&&initial()&&all.SetEquals(zone.GetReadOnlyEntities())&&freshProof.All(p=>p());
   try
   {
    foreach(var spec in specs)
    {
     var e=factory.CreateEntity(spec.bp);if(!Fresh(e,spec.bp)||!authority()||!Initial()||!ValidFeature(e,spec.bp))return Fail("created-feature-changed");
     e.Properties[RoleKey]=spec.role;staged.Add(e);freshProof.Add(SpreadGenerationReceipt.CaptureDetachedState(e));
    }
    foreach(string bp in goodsNames)
    {
     var e=factory.CreateEntity(bp);if(!Fresh(e,bp)||!authority()||!Initial()||e.GetPart<PhysicsPart>().Takeable!=true||(e.GetPart<StackerPart>()?.StackCount??1)!=1)return Fail("created-supply-changed");
     goods.Add(e);freshProof.Add(SpreadGenerationReceipt.CaptureDetachedState(e));
    }
   }
   catch(Exception){return Fail("creation-exception");}
   if(!Unique(zone,staged.Concat(goods))||!authority()||!Initial())return Fail("staged-packet-changed");
   var originalAt=moving.Select(zone.GetEntityPosition).ToArray();var expected=originalAt.ToArray();
   var target=(wet?new[]{design.At(-1,2),design.At(2,1)}:Array.Empty<(int,int)>()).Concat(new[]{design.At(0,4)}).ToArray();
   var added=new HashSet<Entity>();var proof=new Dictionary<Entity,Func<bool>>();
   foreach(var e in moving)proof[e]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{e});
   bool Others()=>unchanged()&&all.Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
   bool Check()=>authority()&&Identity()&&Others()&&proof.Values.All(p=>p());
   if(!actors.TryConsume()||!stock.TryConsume())return Fail("source-claim");
   bool success=false,stockAdded=false;Func<bool> supplied=null;
   try
   {
    for(int i=0;i<moving.Length;i++)
    {
     if(!Check())return Fail("publication-authority-or-owner-changed");
     if(expected[i]!=target[i]&&!zone.MoveEntity(moving[i],target[i].Item1,target[i].Item2))return Fail("source-move-refused");
     expected[i]=target[i];proof[moving[i]]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{moving[i]});if(!Check())return Fail("publication-authority-or-owner-changed");
    }
    for(int i=0;i<staged.Count;i++)
    {
     if(!Check()||!freshProof[i]())return Fail("new-feature-changed");var at=design.At(specs[i].x,specs[i].y);
     if(!zone.AddEntity(staged[i],at.x,at.y))return Fail("new-feature-placement");added.Add(staged[i]);proof[staged[i]]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{staged[i]});if(!Check())return Fail("publication-authority-or-owner-changed");
    }
    if(!Check()||!freshProof.Skip(staged.Count).All(p=>p())||!ReferenceEquals(cache.GetPart<ContainerPart>(),container))return Fail("supply-publication-changed");
    // A cold supplemental packet is kept as distinct owners; ordinary AddItem
    // would merge away an original stock owner or change its quantity.
    foreach(var item in goods){container.Contents.Add(item);item.GetPart<PhysicsPart>().InInventory=cache;}
    stockAdded=true;supplied=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{cache});proof[cache]=supplied;
    if(!Check())return Fail("publication-authority-or-owner-changed");
    var packet=staged.Concat(moving).ToArray();var packetProof=SpreadGenerationReceipt.CaptureFinalState(zone,packet);
    bool Final()=>authority()&&Identity()&&packetProof()&&design.Fits(zone,new HashSet<Entity>(packet),before,true)&&LiveRoutes(zone,design,wet);
    if(!Final()||!Others())return Fail("final-packet-or-routes");owners=packet;final=Final;success=true;return true;
   }
   finally
   {
    if(!success)
    {
     // Roll back only our unchanged owners. A foreign callback's independent
     // changes remain visible, so the enclosing cold builder rejects its graph.
     if(stockAdded&&supplied?.Invoke()==true){foreach(var e in goods){container.Contents.Remove(e);e.GetPart<PhysicsPart>().InInventory=null;}proof[cache]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{cache});}
     foreach(var e in staged.AsEnumerable().Reverse())if(proof.TryGetValue(e,out var p)&&p()){zone.RemoveEntity(e);added.Remove(e);}
     for(int i=moving.Length-1;i>=0;i--)if(expected[i]!=originalAt[i]&&proof[moving[i]]()
       &&zone.CanPlaceFootprint(moving[i],originalAt[i].x,originalAt[i].y))zone.MoveEntity(moving[i],originalAt[i].x,originalAt[i].y);
    }
   }
  }
  sealed class Design
  {
   internal (int x,int y) Center;internal bool Wet;
   internal (int x,int y) At(int x,int y)=>(Center.x+x,Center.y+y);
   internal List<(string bp,string role,int x,int y)> Specs()
   {
    var s=new List<(string,string,int,int)>();
    if(Wet)
    {
     for(int y=-3;y<=2;y++){s.Add(("StoneWall","cover",-3,y));s.Add(("StoneWall","divider",0,y));}
     for(int y=-2;y<=1;y++)s.Add(("WaterPuddle","wet-lane",-1,y));
    }
    else
    {for(int x=-HalfWall;x<=HalfWall;x++)if(x!=0)s.Add(("StoneWall","shortcut-wall",x,0));s.Add(("ConnectedHeavyFrame","frame",0,0));}
    return s;
   }
   internal IEnumerable<(int x,int y)> Required()
   {
    foreach(var s in Specs())yield return At(s.x,s.y);
    yield return At(0,4);yield return At(-1,4);yield return At(1,4);
    if(Wet)
    {
     for(int y=-4;y<=4;y++)foreach(int x in new[]{-4,-2,-1,1,2,3})yield return At(x,y);
     for(int x=-4;x<=3;x++)foreach(int y in new[]{-4,3,4})yield return At(x,y);
    }
    else
    {
     for(int y=-4;y<=-1;y++)for(int x=-2;x<=2;x++)yield return At(x,y);
     for(int y=-1;y<=4;y++){yield return At(-7,y);yield return At(7,y);yield return At(0,y);}
     for(int x=-7;x<=7;x++){yield return At(x,-1);yield return At(x,2);}
    }
   }
   internal int BlockedGround(Zone z,SpreadWildernessSituationBuilder.Geometry g,HashSet<Entity> ignored,bool placed=false)
   {
    // Creating a wall, pool or owner needs empty ground. Walking beside them
    // only needs a safe existing path; retain harvestable foliage and loose
    // objects instead of rejecting or clearing an otherwise usable corridor.
    var placements=Specs().Select(s=>At(s.x,s.y)).Concat(new[]{At(0,4)}).ToHashSet();
    if(Wet){placements.Add(At(-1,2));placements.Add(At(2,1));}
    var water=Specs().Where(s=>s.bp=="WaterPuddle").Select(s=>At(s.x,s.y)).ToHashSet();
    bool Allowed((int x,int y)p)
    {
     if(placements.Contains(p))return g.Place(p.x,p.y)||(placed&&Wet&&water.Contains(p)&&OwnWater(z,p,ignored));
     return p.x>=2&&p.y>=2&&p.x<Zone.Width-2&&p.y<Zone.Height-2&&g.Walk(p.x,p.y,null)
      &&!z.GenReservedCells.Contains(p)&&!z.GetCell(p.x,p.y).IsInterior;
    }
    return Required().Distinct().Count(p=>!Allowed(p));
   }
   internal bool Fits(Zone z,HashSet<Entity> ignored,SpreadWildernessSituationBuilder.Geometry original,bool placed=false)
   {
    var g=new SpreadWildernessSituationBuilder.Geometry(z,ignored);
    if(BlockedGround(z,g,ignored,placed)>0)return false;
    var solid=Specs().Where(s=>s.bp!="WaterPuddle").Select(s=>At(s.x,s.y)).Concat(new[]{At(0,4)}).ToArray();
    if(!g.PreservesAgainst(original,solid))return false;
    var reach=g.Flood(new HashSet<(int,int)>(solid),Arrival,int.MaxValue,null);
    if(!reach[At(-1,4).x,At(-1,4).y]||!reach[At(1,4).x,At(1,4).y])return false;
    if(!Wet)
    {var pulled=solid.Where(p=>p!=Center).Concat(new[]{At(0,-2)}).ToArray();if(!g.PreservesAgainst(original,pulled))return false;}
    return true;
   }
  }
  static Design Find(Zone z,SpreadWildernessSituationBuilder.Geometry original,HashSet<Entity> ignored,bool wet,(int x,int y)anchor,out string report)
  {
   int trials=0,arrivalRejects=0,otherRejects=0,groundRejects=0,bestGround=int.MaxValue;report="";var others=z.GetReadOnlyEntities().Where(e=>e.HasTag("Creature")&&!ignored.Contains(e)).Select(z.GetEntityPosition).ToArray();
   foreach(var p in from y in Enumerable.Range(6,Zone.Height-12) from x in Enumerable.Range(10,Zone.Width-20) orderby Distance(anchor,(x,y)),y,x select(x,y))
   {
    if(Distance(p,Arrival)<=17){arrivalRejects++;continue;}if(others.Any(at=>Distance(at,p)<12)){otherRejects++;continue;}
    var d=new Design{Center=p,Wet=wet};int blocked=d.BlockedGround(z,original,ignored);bestGround=Math.Min(bestGround,blocked);if(blocked>0){groundRejects++;continue;}
    if(++trials>MaxTrials)break;if(d.Fits(z,ignored,original)){report="found";return d;}
   }
   report="arrival="+arrivalRejects+";other-creature="+otherRejects+";ground="+groundRejects+";fewest-blocked="+bestGround+";route-trials="+trials;return null;
  }
  static bool OwnWater(Zone z,(int x,int y)p,HashSet<Entity> owners)
  {
   var s=z.TileState.Get(p.x,p.y);return s!=null&&s.Coatings.Count==1&&s.Coatings[0].Id=="water"&&s.Coatings[0].Turns==ZoneTileState.Permanent
    &&s.Residues.Count==0&&s.Heat==0&&s.Cold==0&&s.Charge==0&&string.IsNullOrEmpty(s.Cloud)
    &&!z.GenReservedCells.Contains(p)&&!z.GetCell(p.x,p.y).IsInterior&&z.GetCell(p.x,p.y).Occupants.All(e=>owners.Contains(e)||DoorPart.IsBareGround(e));
  }
  static bool LiveRoutes(Zone z,Design d,bool wet)
  {
   if(!wet)return true;
   // The outside approach is genuinely hidden by opaque walls, while the dry
   // shoulder and wet lane remain ordinary visible physical routes.
   var hidden=d.At(-4,0);var watcher=d.At(-1,2);var dry=d.At(2,-3);var second=d.At(2,1);
   return !AIHelpers.HasLineOfSight(z,hidden.x,hidden.y,watcher.x,watcher.y)
    &&AIHelpers.HasLineOfSight(z,dry.x,dry.y,second.x,second.y);
  }
  static bool OrdinaryActor(Zone z,Entity e)=>e?.BlueprintName=="MarlbackScrabbler"&&e.SpatialZone==z&&e.GetStatValue("Hitpoints")>0&&e.HasTag("Creature")
   &&e.GetPart<BrainPart>() is BrainPart b&&b.Target==null&&b.PartyLeader==null&&b.PartyMembers.Count==0&&!b.HasGoalOtherThan("BoredGoal")
   &&!e.HasPart<SpreadTerritoryPart>()&&!e.HasPart<CombatTacticsPart>()&&!e.HasPart<DraggedPart>()&&!e.HasPart<DragPart>()&&!e.HasPart<SpatialFootprintPart>()
   &&!e.HasTag("Player")&&!e.HasTag("Pet")&&!e.HasTag("Owned")&&!e.HasTag("QuestItem")&&!e.HasTag("Unique")&&!z.GenReservedCells.Contains(z.GetEntityPosition(e));
  static bool Cache(Zone z,Entity e)=>e!=null&&(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")&&e.SpatialZone==z&&e.GetPart<ContainerPart>() is ContainerPart c&&!c.IsLocked
   &&e.GetPart<PhysicsPart>() is PhysicsPart p&&!p.Takeable&&p.InInventory==null&&p.Equipped==null&&!e.HasPart<SpatialFootprintPart>()&&!e.HasTag("Owned")&&!e.HasTag("Unique")&&!e.HasTag("QuestItem")&&!z.GenReservedCells.Contains(z.GetEntityPosition(e));
  static bool Fresh(Entity e,string bp)=>e!=null&&e.BlueprintName==bp&&!string.IsNullOrEmpty(e.ID)&&e.SpatialZone==null&&e.GetPart<PhysicsPart>() is PhysicsPart p
   &&p.InInventory==null&&p.Equipped==null&&e.GetPart<RenderPart>()!=null&&e.Parts.All(part=>part!=null&&part.ParentEntity==e)&&!e.HasPart<SpatialFootprintPart>();
  static bool ValidFeature(Entity e,string bp)=>bp=="WaterPuddle"?e.GetPart<LiquidPoolPart>()?.LiquidId=="water"&&e.GetPart<LiquidPoolPart>().Volume==120
   :bp=="ConnectedHeavyFrame"?e.GetPart<PhysicsPart>()?.Solid==true&&e.GetPart<PhysicsPart>().Takeable==false&&e.GetPart<HandlingPart>() is HandlingPart h&&h.Weight==136&&!h.Carryable&&!h.Throwable&&h.MinLiftStrength==0
   :bp=="StoneWall"&&e.GetPart<PhysicsPart>()?.Solid==true;
  static bool Unique(Zone z,IEnumerable<Entity> extra)
  {
   var seen=new HashSet<Entity>();var ids=new HashSet<string>();var queue=new Queue<Entity>(z.GetReadOnlyEntities().Concat(extra));
   while(queue.Count>0){var e=queue.Dequeue();if(e==null)return false;if(!seen.Add(e))continue;if(string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID))return false;
    if(e.GetPart<ContainerPart>() is ContainerPart c)foreach(var child in c.Contents)queue.Enqueue(child);
    if(e.GetPart<InventoryPart>() is InventoryPart inv)foreach(var child in inv.Objects.Concat(inv.EquippedItems.Values).Distinct())queue.Enqueue(child);
   }return true;
  }
  static int Distance((int x,int y)a,(int x,int y)b)=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y));
 }
}
