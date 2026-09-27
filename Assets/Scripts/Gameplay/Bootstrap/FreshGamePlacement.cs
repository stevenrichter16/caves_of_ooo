using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
 /// <summary>Places only a fresh detached actor. It preserves generated owners,
 /// uses complete physical bodies, and refuses a forced overlap or hazardous
 /// initial cell. This is an initial placement guarantee, not combat immunity.</summary>
 public static class FreshGamePlacement
 {
  public const int ImmediateThreatRadius=3;
  public static bool TryPlace(Zone zone,Entity actor,int preferredX,int preferredY)
  {
   if(zone==null||actor==null||actor.SpatialZone!=null||CombatSystem.IsDeathHandled(actor)
    ||actor.GetStat("Hitpoints") is Stat hp&&hp.Value<=0
    ||actor.GetPart<PhysicsPart>()?.InInventory!=null||actor.GetPart<PhysicsPart>()?.Equipped!=null)
    return Reject(zone,actor,"invalid-fresh-actor");
   if(!zone.InBounds(preferredX,preferredY))return Reject(zone,actor,"invalid-preference");
   var threats=zone.GetReadOnlyEntities().Where(e=>e!=actor&&e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)
    &&(e.GetStat("Hitpoints")==null||e.GetStatValue("Hitpoints")>0)
    &&(FactionManager.IsHostile(e,actor)||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor)==true||e.GetPart<BrainPart>()?.Target==actor)).ToArray();
   // Build a finite cardinal route from actual safe edge anchors. A nearby
   // free step alone can still be an enclosed two-cell room.
   var reachable=ReachableFromEdge(zone,actor,threats);
   for(int r=0;r<Math.Max(Zone.Width,Zone.Height);r++)for(int dx=-r;dx<=r;dx++)for(int dy=-r;dy<=r;dy++)
   {
    if(Math.Abs(dx)!=r&&Math.Abs(dy)!=r)continue;int x=preferredX+dx,y=preferredY+dy;
    if(!zone.InBounds(x,y)||!reachable[x,y])continue;
    if(!zone.AddEntity(actor,x,y))return Reject(zone,actor,"placement-refused");
    Diag.Record("worldgen","FreshGamePlaced",actor,payload:new{zoneId=zone.ZoneID,x,y,preferredX,preferredY});return true;
   }
   return Reject(zone,actor,"no-safe-start");
  }
  private static bool[,] ReachableFromEdge(Zone zone,Entity actor,Entity[] threats)
  {
   var safe=new bool[Zone.Width,Zone.Height];var reached=new bool[Zone.Width,Zone.Height];
   var queue=new Queue<(int x,int y)>();
   for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
   {
    safe[x,y]=Safe(zone,actor,x,y,threats);
    if(safe[x,y]&&(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1))
    {reached[x,y]=true;queue.Enqueue((x,y));}
   }
   while(queue.Count>0)
   {
    var at=queue.Dequeue();for(int d=0;d<4;d++)
    {
     int x=at.x+(d==0?1:d==1?-1:0),y=at.y+(d==2?1:d==3?-1:0);
     if(!zone.InBounds(x,y)||reached[x,y]||!safe[x,y])continue;
     reached[x,y]=true;queue.Enqueue((x,y));
    }
   }
   return reached;
  }
  private static bool Safe(Zone zone,Entity actor,int x,int y,Entity[] threats)
  {
   if(!zone.CanPlaceFootprint(actor,x,y))return false;
   foreach(var c in zone.GetOccupiedCells(actor,x,y))
   {
    if(c==null)return false;var state=zone.TileState.Get(c.X,c.Y);
    if(state!=null&&(state.Heat>0||state.Cold>0||state.Charge>0||!string.IsNullOrEmpty(state.Cloud)
      ||state.Coatings.Count>0))return false;
    foreach(var e in c.Occupants)if(e!=null&&e!=actor&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true))return false;
    foreach(var threat in threats)if(SpatialQuery.DistanceToCell(zone,threat,c.X,c.Y)<=ImmediateThreatRadius)return false;
   }
   return true;
  }
  private static bool Reject(Zone zone,Entity actor,string reason)
  {Diag.Record("worldgen","FreshGamePlacementRejected",actor,payload:new{zoneId=zone?.ZoneID,reason});return false;}
 }
}
