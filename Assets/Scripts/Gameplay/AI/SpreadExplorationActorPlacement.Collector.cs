using System;
using System.Collections.Generic;
using System.Linq;
namespace CavesOfOoo.Core
{
 public static partial class SpreadExplorationActorPlacement
 {
  /// <summary>Cold placement of three exact producer-owned sources. Caller proves
  /// and consumes receipts; this helper creates no owner, contents or stock.</summary>
  public static bool TryCollectorReturn(Zone zone,Entity bird,Entity home,Entity item,Func<bool> authority,out Func<bool> finalState)
  {
   finalState=null;var role=bird?.GetPart<SpreadCollectorPart>();
   if(zone==null||authority==null||!CollectorEmpty(role,bird)||!CollectorSources(zone,bird,home,item)||!CollectorCapacity(bird,home,item))return false;
   var owner=ActorSnapshot.Capture(zone,bird,"Tatterjay",role);if(owner==null)return false;
   var homeGraph=new OwnedGraphSnapshot(home);var itemGraph=new OwnedGraphSnapshot(item);
   var homeAt=zone.GetEntityPosition(home);var itemOrigin=zone.GetEntityPosition(item);var itemAt=itemOrigin;var birdAt=owner.Origin;int quantity=item.GetPart<StackerPart>()?.StackCount??1;
   bool Source()=>CollectorSources(zone,bird,home,item)&&homeGraph.Matches(null)&&itemGraph.Matches(null)
    &&zone.GetEntityPosition(home)==homeAt&&zone.GetEntityPosition(item)==itemAt&&CollectorCapacity(bird,home,item);
   bool Current()=>Source()&&owner.Current(birdAt)&&CollectorEmpty(role,bird);
   if(!authority()||!Current())return false;
   var ignored=new HashSet<Entity>{bird,item};var original=new SpreadWildernessSituationBuilder.Geometry(zone,ignored);int trials=0;
   for(int radius=4;radius<=8;radius++)for(int y=homeAt.y-radius;y<=homeAt.y+radius;y++)for(int x=homeAt.x-radius;x<=homeAt.x+radius;x++)
   {
    if(Distance((x,y),homeAt)!=radius)continue;var target=(x,y);
    foreach(var delta in new[]{(-2,0),(2,0),(0,-2),(0,2)})
    {
     if(++trials>MaxTrials)return false;var actor=(x:x+delta.Item1,y:y+delta.Item2);
     bool Layout(SpreadWildernessSituationBuilder.Geometry g)=>CollectorLayout(g,actor,target,homeAt)&&g.PreservesAgainst(original,new[]{actor});
     if(!Layout(original))continue;
     bool committed=false,configured=false;
     bool Configured()=>role.ParentEntity==bird&&bird.GetPart<SpreadCollectorPart>()==role&&role.Configured
      &&role.Home==home&&role.Target==item&&role.ZoneID==zone.ZoneID&&role.HomeBlueprint==home.BlueprintName&&role.TargetBlueprint==item.BlueprintName
      &&role.HomeX==homeAt.x&&role.HomeY==homeAt.y&&role.TargetX==target.x&&role.TargetY==target.y
      &&role.Quantity==quantity&&role.Actions==0&&role.Phase==SpreadCollectorPhase.Seeking;
     try
     {
      if(!authority()||!Current())return false;
      if(birdAt!=actor&&!zone.MoveEntity(bird,actor.x,actor.y))return false;birdAt=actor;
      if(!authority()||!Current())return false;
      if(itemAt!=target&&!zone.MoveEntity(item,target.x,target.y))return false;itemAt=target;
      if(!authority()||!Current()||!Layout(new SpreadWildernessSituationBuilder.Geometry(zone,ignored)))return false;
      if(!role.Configure(zone,home,item))return false;configured=true;
      if(!authority()||!Source()||!owner.Current(birdAt)||!Configured())return false;
      var actorGraph=new OwnedGraphSnapshot(bird);
      bool Final()=>Source()&&actorGraph.Matches(null)&&Configured()&&zone.GetEntityPosition(bird)==actor
       &&Layout(new SpreadWildernessSituationBuilder.Geometry(zone,ignored));
      committed=Final();if(committed)finalState=Final;return committed;
     }
     finally
     {
      if(!committed)
      {
       // Undo only the configuration and movements still owned by this call.
       // A callback's replacement part/stock/quantity is not ours to repair.
       if(configured&&Configured())ResetCollector(role);
       if(CollectorEmpty(role,bird))owner.Rollback(birdAt);
       if(itemAt!=itemOrigin&&itemGraph.Matches(null)&&SpreadActorContext.Ground(item,zone)
         &&zone.GetEntityPosition(item)==itemAt&&zone.CanPlaceFootprint(item,itemOrigin.x,itemOrigin.y))zone.MoveEntity(item,itemOrigin.x,itemOrigin.y);
      }
     }
    }
   }
   return false;
  }
  static bool CollectorEmpty(SpreadCollectorPart role,Entity bird)=>role!=null&&role.ParentEntity==bird&&bird.GetPart<SpreadCollectorPart>()==role
   &&!role.Configured&&role.Home==null&&role.Target==null&&role.ZoneID==null&&role.TargetBlueprint==null&&role.HomeBlueprint==null
   &&role.HomeX==0&&role.HomeY==0&&role.TargetX==0&&role.TargetY==0&&role.Quantity==0&&role.Actions==0&&role.Phase==SpreadCollectorPhase.Seeking;
  static void ResetCollector(SpreadCollectorPart role)
  {role.Configured=false;role.Home=role.Target=null;role.ZoneID=role.HomeBlueprint=role.TargetBlueprint=null;role.HomeX=role.HomeY=role.TargetX=role.TargetY=role.Quantity=role.Actions=0;role.Phase=SpreadCollectorPhase.Seeking;}
  static bool CollectorSources(Zone z,Entity bird,Entity home,Entity item)
  {
   if(bird?.BlueprintName!="Tatterjay"||!SpreadActorContext.Actor(bird,z,out var brain)||(brain.CurrentZone!=null&&brain.CurrentZone!=z)||!brain.Passive||brain.PartyLeader!=null
    ||brain.Target!=null||brain.InConversation||brain.GetGoalsSnapshot().Any(g=>!(g is BoredGoal))||CombatSystem.IsDeathHandled(bird)
    ||!bird.HasTag("NoRandomStock")||bird.GetPart<InventoryPart>()?.ParentEntity!=bird||bird.HasPart<SpatialFootprintPart>())return false;
   if(home==null||home==bird||home==item||(home.BlueprintName!="Crate"&&home.BlueprintName!="Sack")||!CollectorGround(z,home)||CollectorProtected(home)
    ||home.GetPart<ContainerPart>() is not ContainerPart c||c.ParentEntity!=home||c.Contents==null||c.IsLocked||!CollectorHomeAnchor(z,home))return false;
   if(item==null||item==bird||(item.BlueprintName!="Hatchet"&&item.BlueprintName!="Cudgel"&&item.BlueprintName!="LeatherBoots")
    ||!CollectorGround(z,item)||CollectorProtected(item)||!item.HasTag("Item")||!item.GetPart<PhysicsPart>().Takeable||item.GetPart<PhysicsPart>().Solid)return false;
   var stack=item.GetPart<StackerPart>();var handling=item.GetPart<HandlingPart>();
   return (stack==null||(stack.ParentEntity==item&&stack.StackCount>0))&&(handling==null||(handling.ParentEntity==item&&handling.Carryable))
    &&c.Contents.All(e=>e!=null&&e!=item&&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.ParentEntity==e&&p.InInventory==home&&p.Equipped==null&&e.SpatialZone==null);
  }
  static bool CollectorHomeAnchor(Zone z,Entity home)
  {var cell=z.GetEntityCell(home);return cell!=null&&!z.GenReservedCells.Contains((cell.X,cell.Y))&&z.TileState.Get(cell.X,cell.Y)?.IsEmpty!=false
    &&cell.Occupants.All(e=>e==home||DoorPart.IsBareGround(e));}
  static bool CollectorGround(Zone z,Entity e)=>SpreadActorContext.Ground(e,z)&&!e.HasTag("Creature")&&!e.HasPart<SpatialFootprintPart>()
   &&e.GetPart<RenderPart>()?.ParentEntity==e;
  static bool CollectorProtected(Entity e)=>e.HasTag("QuestItem")||e.HasTag("Quest")||e.HasTag("Unique")||e.HasTag("NoTrade")||e.HasTag("Owned")||e.HasTag("Essential")||e.HasTag("Currency")||e.HasTag("NoTake")
   ||e.Properties.ContainsKey("Owner")||e.Properties.ContainsKey("OwnerID")||e.Properties.ContainsKey("QuestID")||e.HasPart<KeyPart>()
   ||e.HasPart<CavesOfOoo.Storylets.QuestStarter>()||e.HasPart<CavesOfOoo.Storylets.CompleteObjectiveOnTaken>();
  static bool CollectorCapacity(Entity bird,Entity home,Entity item)
  {
   var inventory=bird.GetPart<InventoryPart>();if(!HandlingService.CanLift(bird,item,out _))return false;
   if(inventory.MaxWeight>=0&&(long)inventory.GetCarriedWeight()+InventoryPart.GetItemWeight(item)>inventory.MaxWeight)return false;
   var c=home.GetPart<ContainerPart>();if(c.MaxItems<0||c.Contents.Count<c.MaxItems)return true;
   var source=item.GetPart<StackerPart>();if(source==null)return false;long remaining=source.StackCount;
   foreach(var e in c.Contents){var s=e.GetPart<StackerPart>();if(s!=null&&s.ParentEntity==e&&s.CanStackWith(item))remaining-=Math.Max(0,(long)s.MaxStack-s.StackCount);}
   return remaining<=0;
  }
  static bool CollectorLayout(SpreadWildernessSituationBuilder.Geometry g,(int x,int y) bird,(int x,int y) item,(int x,int y) home)
  {
   if(bird==item||!g.Place(bird.x,bird.y)||!g.Place(item.x,item.y)||Distance(bird,item)>12||Distance(bird,home)>12||Distance(item,home)>12)return false;
   var blocked=new HashSet<(int x,int y)>{bird};var player=g.Flood(blocked,null,int.MaxValue,null);
   bool Approach((int x,int y) at)=>new[]{(at.x-1,at.y),(at.x+1,at.y),(at.x,at.y-1),(at.x,at.y+1)}.Any(p=>g.In(p.Item1,p.Item2)&&player[p.Item1,p.Item2]);
   if(!Approach(item)||!Approach(home))return false;
   // Cardinal dry steps are legal for the existing single-cell bird and avoid
   // optimistic diagonal corner shortcuts. No speculative movement is executed.
   var distance=new int[Zone.Width,Zone.Height];for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)distance[x,y]=-1;
   var q=new Queue<(int x,int y)>();q.Enqueue(bird);distance[bird.x,bird.y]=0;
   int ToHome((int x,int y) start,int remaining)
   {var reach=g.Flood(null,start,remaining,null);foreach(var p in new[]{(home.x-1,home.y),(home.x+1,home.y),(home.x,home.y-1),(home.x,home.y+1)})if(g.In(p.Item1,p.Item2)&&reach[p.Item1,p.Item2])return 1;return 0;}
   while(q.Count>0)
   {var p=q.Dequeue();int d=distance[p.x,p.y];if(d>SpreadCollectorPart.MaxApproachActions)continue;
    if(Distance(p,item)==1&&ToHome(p,SpreadCollectorPart.MaxApproachActions-d)>0)return true;
    foreach(var next in new[]{(p.x-1,p.y),(p.x+1,p.y),(p.x,p.y-1),(p.x,p.y+1)})if(g.Walk(next.Item1,next.Item2,null)&&distance[next.Item1,next.Item2]<0){distance[next.Item1,next.Item2]=d+1;q.Enqueue(next);}}
   return false;
  }
 }
}
