using System;using System.Collections.Generic;using System.IO;using System.Reflection;using CavesOfOoo.Core;using CavesOfOoo.Data;using NUnit.Framework;using UnityEngine;using Random=System.Random;
namespace CavesOfOoo.Tests {
// Pursuit must respect physical props while intentionally omitting temporary actors.
// Real movement/goal/scheduler checks; no native input or frame-performance claim.
public sealed class MovableObstaclePursuitAuditTests {
 EntityFactory f;Zone z;Entity a,t,oldWorld;TurnManager oldActive;List<string> trace=new List<string>();
 [SetUp]public void Setup(){oldWorld=TurnManager.World;oldActive=TurnManager.Active;TurnManager.World=null;FactionManager.Initialize();MessageLog.Clear();f=new EntityFactory();f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));z=new Zone("private-path-audit");a=Actor(false);t=Actor(true);Assert.True(z.AddEntity(a,6,10));Assert.True(z.AddEntity(t,12,10));}
 [TearDown]public void Cleanup(){TurnManager.World=oldWorld;typeof(TurnManager).GetProperty("Active",BindingFlags.Static|BindingFlags.Public).SetValue(null,oldActive);MessageLog.Clear();}
 Entity Actor(bool player){var e=new Entity{BlueprintName=player?"Player":"AuditPursuer"};e.SetTag("Creature");if(player)e.SetTag("Player");e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart{DisplayName=e.BlueprintName});foreach(var s in new[]{("Hitpoints",20),("Strength",16),("Speed",100)})e.Statistics[s.Item1]=new Stat{Name=s.Item1,BaseValue=s.Item2,Min=0,Max=s.Item2};if(!player)e.AddPart(new BrainPart{CurrentZone=z,Rng=new Random(64),SightRadius=20});return e;}
 Entity Ob(string bp,int x,int y,bool tag=false){var e=f.CreateEntity(bp);Assert.NotNull(e);if(tag)e.SetTag("Solid");Assert.True(z.AddEntity(e,x,y));return e;}
 void Barrier(string bp,bool tag=false,bool full=false){for(int y=full?0:6;y<=(full?Zone.Height-1:14);y++)Ob(bp,7,y,tag);}
 void Foot(string raw){z.RemoveEntity(a);a.AddPart(new SpatialFootprintPart{CellsRaw=raw});Assert.True(z.AddEntity(a,6,10));}
 string Trace()=>string.Join(" ",trace);
 bool Approach(int n=30){trace.Clear();for(int i=0;i<n&&SpatialQuery.Distance(z,a,t)>1;i++){var p=z.GetEntityPosition(a);bool moved=AIHelpers.TryApproachWithPathfinding(a,z,p.x,p.y,12,10);trace.Add(i+":"+z.GetEntityPosition(a)+":"+moved);}TestContext.WriteLine(Trace());return SpatialQuery.Distance(z,a,t)==1;}
 void Bypass(){var p=z.GetEntityPosition(a);var path=a.HasPart<SpatialFootprintPart>()?FindPath.ToContact(z,a,t):FindPath.Search(z,p.x,p.y,12,10,ignoreCreatures:false,actor:a);Assert.True(path.Usable,"fixture needs physical bypass");int x=p.x,y=p.y;foreach(var s in path.Steps){x+=s.dx;y+=s.dy;if(x!=12||y!=10)Assert.True(z.CanPlaceFootprint(a,x,y),"physical bypass "+x+","+y);}TestContext.WriteLine("physical bypass steps="+path.Steps.Count);}
 // H1: actual props physically block; unlike sight walls, they remain transparent.
 [TestCase("FallenBeam")][TestCase("HaulBarrel")][TestCase("Hedge")]
 public void ActualBlueprintBlocksMovementWithoutBeingAVisibleWall(string bp){var e=Ob(bp,7,10);Assert.True(e.GetPart<PhysicsPart>().Solid);Assert.False(e.HasTag("Solid"));Assert.True(z.GetCell(7,10).IsPassable());Assert.True(z.GetCell(7,10).BlocksMovement());Assert.False(MovementSystem.TryMove(a,z,1,0));Assert.AreEqual((6,10),z.GetEntityPosition(a));Assert.True(AIHelpers.HasLineOfSight(z,6,10,12,10));}
 // H2: ignore creatures should not omit stationary physical owners.
 [TestCase("FallenBeam",false)][TestCase("FallenBeam",true)][TestCase("HaulBarrel",false)][TestCase("HaulBarrel",true)][TestCase("Hedge",false)][TestCase("Hedge",true)][TestCase("Wall",false)][TestCase("Wall",true)]
 public void SearchAvoidsStationaryBlockersWithEitherCreaturePolicy(string bp,bool ignore){Barrier(bp);var p=FindPath.Search(z,6,10,12,10,ignoreCreatures:ignore,actor:a);Assert.True(p.Usable);int x=6,y=10;foreach(var s in p.Steps){x+=s.dx;y+=s.dy;TestContext.WriteLine(x+","+y);if(x!=12||y!=10)Assert.False(z.GetCell(x,y).BlocksMovement(a),"planned stationary "+bp+" at "+x+","+y);}}
 // H3: an ordinary creature is intentionally omitted only by the true policy.
 [TestCase(false)][TestCase(true)]public void IgnoreCreaturesChangesOnlyTheOccupyingActorRoute(bool ignore){Assert.True(z.AddEntity(Actor(false),7,10));var p=FindPath.Search(z,6,10,12,10,ignoreCreatures:ignore,actor:a);Assert.True(p.Usable);Assert.AreEqual(ignore,p.Steps[0]==(1,0));Assert.False(MovementSystem.TryMove(a,z,1,0));}
 // H4: thin-owner fallback may mask the A* disagreement.
 [TestCase("FallenBeam")][TestCase("HaulBarrel")][TestCase("Hedge")]
 public void SingleOwnerStillHasAnExecutableFallbackAroundIt(string bp){Ob(bp,7,10);Assert.False(MovementSystem.TryMove(a,z,1,0));Assert.True(Approach(),Trace());}
 // H5: same physical boundary, only the authored tag changes.
 [TestCase("FallenBeam",false)][TestCase("FallenBeam",true)][TestCase("HaulBarrel",false)][TestCase("HaulBarrel",true)][TestCase("Hedge",false)][TestCase("Hedge",true)][TestCase("Wall",false)]
 public void OrdinaryApproachMustUseTheRealOpenEndOfABoundary(string bp,bool tag){Barrier(bp,tag);Bypass();Assert.True(Approach(),Trace());}
 // H6: actual brain + ordinary scheduler, with genuine multi-cell counterpart.
 [TestCase("FallenBeam",false)][TestCase("FallenBeam",true)][TestCase("HaulBarrel",false)][TestCase("HaulBarrel",true)][TestCase("Hedge",false)][TestCase("Hedge",true)][TestCase("Wall",false)][TestCase("Wall",true)]
 public void ScheduledKillGoalMustReachPlayerAcrossOpenBypass(string bp,bool wide){if(wide)Foot("0,0;0,1");Barrier(bp);Bypass();a.GetPart<BrainPart>().PushGoal(new KillGoal(t));var tm=new TurnManager();tm.AddEntity(a);tm.AddEntity(t);trace.Clear();int waits=0;Assert.AreSame(t,tm.ProcessUntilPlayerTurn());trace.Add("0:"+z.GetEntityPosition(a));while(waits<30&&SpatialQuery.Distance(z,a,t)>1){tm.EndTurn(t,z);waits++;Assert.AreSame(t,tm.ProcessUntilPlayerTurn());trace.Add(waits+":"+z.GetEntityPosition(a));}TestContext.WriteLine("waits="+waits+" ticks="+tm.TickCount+" energy="+tm.GetEnergy(a)+" "+Trace());Assert.LessOrEqual(tm.TickCount,310);Assert.AreEqual(0,tm.GetEnergy(a));Assert.AreEqual(20,t.GetStatValue("Hitpoints"));Assert.AreEqual(1,SpatialQuery.Distance(z,a,t),Trace());}
 // H7: even same-sized explicit footprint enters the stricter path branch.
 [TestCase(false)][TestCase(true)]public void OneCellExplicitFootprintPreservesPhysicalRoute(bool footprint){if(footprint)Foot("0,0");Barrier("Hedge");var p=FindPath.Search(z,6,10,12,10,ignoreCreatures:true,actor:a);Assert.True(p.Usable);Assert.True(MovementSystem.TryMove(a,z,p.Steps[0].dx,p.Steps[0].dy),"first planned step must execute; explicit footprint="+footprint);}
 // H8: preserve the existing one-action door contract; no free traversal.
 [TestCase("capable")][TestCase("incapable")][TestCase("locked")]
 public void ClosedDoorKeepsItsExplicitOperationContract(string mode){for(int y=0;y<Zone.Height;y++)if(y!=10)Ob("Wall",7,y);var d=new Entity();d.SetTag("Furniture");d.AddPart(new PhysicsPart());d.AddPart(new RenderPart());var part=new DoorPart{IsOpen=false};d.AddPart(part);Assert.True(z.AddEntity(d,7,10));if(mode!="incapable")a.SetTag("CanOpenDoors");if(mode=="locked")d.AddPart(new LockPart{KeyId="absent",IsLocked=true});Assert.AreEqual(mode=="capable",FindPath.Search(z,6,10,12,10,ignoreCreatures:true,actor:a).Usable);Assert.AreEqual(mode=="capable",AIHelpers.TryApproachWithPathfinding(a,z,6,10,12,10));Assert.AreEqual(mode=="capable",part.IsOpen);Assert.AreEqual((6,10),z.GetEntityPosition(a));if(mode=="capable"){Assert.True(AIHelpers.TryApproachWithPathfinding(a,z,6,10,12,10));Assert.AreEqual((7,10),z.GetEntityPosition(a));}}
 // H9: actual sealed geometry does not become a usable path.
 [TestCase("FallenBeam")][TestCase("Wall")]
 public void SealedPhysicalBoundaryMustNotReturnAUsableRoute(string bp){Barrier(bp,full:true);Assert.False(Approach(4));Assert.False(FindPath.Search(z,6,10,12,10,ignoreCreatures:true,actor:a).Usable);}
 // H10: proposed inverse-hauling shape, removed load is a paired control.
 [TestCase(false)][TestCase(true)]public void LoadInHedgeThroatRequiresRealDetourInsteadOfPermanentImmunity(bool load){for(int y=6;y<=14;y++)if(y!=10)Ob("Hedge",7,y);if(load)Ob("FallenBeam",7,10);Bypass();Assert.True(Approach(),"load should alter route cost, not disable pursuit: "+Trace());}
 // H11: execute the whole alternative route, not just an optimistic planner receipt.
 [TestCase("FallenBeam")][TestCase("HaulBarrel")][TestCase("Hedge")]
 public void RespectingPhysicalOwnersProducesAnEntireExecutableBypass(string bp){Barrier(bp);z.RemoveEntity(t);var p=FindPath.Search(z,6,10,12,10,ignoreCreatures:false,actor:a);Assert.True(p.Usable);Assert.Greater(p.Steps.Count,6);foreach(var s in p.Steps)Assert.True(MovementSystem.TryMove(a,z,s.dx,s.dy));Assert.AreEqual((12,10),z.GetEntityPosition(a));TestContext.WriteLine("executed bypass="+p.Steps.Count);}
}}
