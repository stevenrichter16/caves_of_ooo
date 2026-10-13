using System;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
 /// <summary>Opt-in authored entrance duty. Public scalar fields persist through ordinary saves.
 /// Pursuit uses observed cells, never a hidden target's current position. Return costs normal actions.</summary>
 public sealed class WorksiteGuardPart : Part
 {
  public override string Name=>"WorksiteGuard";
  public string ZoneID;
  public int HomeX,HomeY,PursuitRadius=4;
  public bool Returning;
  /// <summary>Duty is local and yields to party, conversation and peace state.</summary>
  public bool Applies(BrainPart brain,Zone zone)=>zone!=null&&ZoneID==zone.ZoneID&&zone.InBounds(HomeX,HomeY)
   &&PursuitRadius>=1&&PursuitRadius<=6&&SpreadActorContext.Actor(ParentEntity,zone,out var actual)&&actual==brain
   &&ParentEntity.GetPart<WorksiteGuardPart>()==this&&brain.CurrentZone==zone&&brain.PartyLeader==null
   &&!brain.InConversation&&!brain.HasGoal<NoFightGoal>()&&brain.PeekGoal()?.CanFight()!=false
   &&!CombatSystem.IsDeathHandled(ParentEntity);
  bool Inside(int x,int y)=>Math.Max(Math.Abs(x-HomeX),Math.Abs(y-HomeY))<=PursuitRadius;
  internal bool AllowsPursuit(Zone zone,int observedX,int observedY)
  {var at=zone.GetEntityCell(ParentEntity);return at!=null&&Inside(at.X,at.Y)&&Inside(observedX,observedY);}
  internal bool TakeIdleAction(BrainPart brain,Zone zone)
  {
   if(!Applies(brain,zone))return false;
   int hp=ParentEntity.GetStatValue("Hitpoints"),max=ParentEntity.GetStat("Hitpoints")?.Max??0;
   if(max>0&&(float)hp/max<brain.FleeThreshold)return false;
   var at=zone.GetEntityCell(ParentEntity);Entity best=null;int distance=int.MaxValue;
   if(Inside(at.X,at.Y))foreach(var other in zone.GetReadOnlyEntities())
   {
    if(other==ParentEntity||!other.HasTag("Creature")||other.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(other)
     ||(brain.Passive&&!brain.IsPersonallyHostileTo(other))||!FactionManager.IsHostile(ParentEntity,other)
     ||!AIHelpers.TryGetVisibleTargetCell(ParentEntity,other,zone,brain.SightRadius,out var observed)||!Inside(observed.X,observed.Y))continue;
    int d=AIHelpers.ChebyshevDistance(at.X,at.Y,observed.X,observed.Y);if(d>=distance)continue;best=other;distance=d;
   }
   if(best!=null){Returning=false;brain.Target=best;brain.PushGoal(new KillGoal(best));return true;}
   brain.Target=null;brain.CurrentState=AIState.Idle;
   if(at.X==HomeX&&at.Y==HomeY){Returning=false;return true;}
   if(!Returning)
   {
    Returning=true;
    if(Diag.IsChannelEnabled("ai"))Diag.Record("ai","WorksiteGuardReturn",ParentEntity,null,new{HomeX,HomeY});
    if(at.IsVisible&&ParentEntity.GetPart<RenderPart>()?.Visible!=false)MessageLog.Add(ParentEntity.GetDisplayName()+" turns back toward the yard entrance.");
   }
   AIHelpers.TryApproachWithPathfinding(ParentEntity,zone,at.X,at.Y,HomeX,HomeY);return true;
  }
  /// <summary>One paid chase step or door action; a detour cannot leave the duty radius.
  /// Failed paths wait. Return from displacement uses the ordinary unrestricted path helper.</summary>
  internal void Step(Zone zone,int dx,int dy)
  {var at=zone.GetEntityCell(ParentEntity);if(at!=null&&Inside(at.X+dx,at.Y+dy))MovementSystem.TryMoveDetailed(ParentEntity,zone,dx,dy);}
  internal void Approach(Zone zone,int x,int y)
  {
   var at=zone.GetEntityCell(ParentEntity);var path=FindPath.Search(zone,at.X,at.Y,x,y,ignoreCreatures:true,actor:ParentEntity);
   if(!path.Usable||path.Steps.Count==0)return;
   var step=path.Steps[0];Step(zone,step.dx,step.dy);
  }
 }
}
