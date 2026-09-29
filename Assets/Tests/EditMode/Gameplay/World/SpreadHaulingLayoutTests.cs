using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 internal sealed class HaulingFixture:IDisposable
 {
  internal const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  internal readonly HaulingContentScope Scope=new HaulingContentScope();internal readonly Zone Zone=new Zone("Overworld.10.5.0");internal readonly SpreadCompositionBuilder Terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.Hedgerow,Topology=SpreadExplorationTopology.BrokenEnclosures,CapturePassageSources=true};internal readonly HaulablePropBuilder Haul=new HaulablePropBuilder(BiomeType.Spread);
  internal HaulingFixture()
  {
   Scope.Seed(64);Assert.True(Terrain.BuildZone(Zone,Scope.Factory,new Random(64)));
   // Controlled original-terrain fixture. Only unrelated decoration is removed;
   // actual unedited layouts are measured independently by the frozen census.
   foreach(var e in Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName!="Hedge"&&!(bool)typeof(DoorPart).GetMethod("IsBareGround",All).Invoke(null,new object[]{e})).ToArray())Zone.RemoveEntity(e);
   var field=typeof(HaulablePropBuilder).GetField("CaptureSourceReceipts",All);Assert.NotNull(field);field.SetValue(Haul,true);
   var spots=from y in Enumerable.Range(2,Zone.Height-4) from x in Enumerable.Range(2,Zone.Width-4) where !Zone.GenReservedCells.Contains((x,y))&&new[]{(x,y),(x-1,y),(x+1,y),(x,y-1),(x,y+1)}.All(p=>!Zone.GetCell(p.Item1,p.Item2).BlocksMovement()) select(x,y);
   Assert.IsNotEmpty(spots);Assert.True(Haul.BuildZone(Zone,Scope.Factory,new Roll(spots.First())));Assert.NotNull(Receipt);
  }
  sealed class Roll:Random{readonly (int x,int y)p;int n;public Roll((int,int)p){this.p=p;}public override int Next(int max)=>max==1000?0:1;public override int Next(int min,int max)=>n++%2==0?p.x:p.y;}
  internal SpreadGenerationReceipt Receipt=>(SpreadGenerationReceipt)typeof(HaulablePropBuilder).GetProperty("SourceReceipt",All).GetValue(Haul);
  internal SpreadGenerationReceipt[] Hedges=>((IEnumerable)typeof(SpreadCompositionBuilder).GetProperty("PassageSources",All).GetValue(Terrain)).Cast<SpreadGenerationReceipt>().ToArray();
  internal bool Place(out Entity load,out Func<bool> final,Func<bool> authority=null)
  {var t=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationHauling");Assert.NotNull(t,"Useful exact-source cold layout helper is required.");var method=t.GetMethod("TryPlace",All);Assert.NotNull(method);object[] a={Zone,Terrain,Haul,authority??(()=>true),null,null};bool ok=(bool)method.Invoke(null,a);load=(Entity)a[4];final=(Func<bool>)a[5];return ok;}
  internal (int x,int y) Axis(Entity load)
  {var p=Zone.GetEntityPosition(load);foreach(var d in new[]{(x:0,y:1),(x:1,y:0),(x:0,y:-1),(x:-1,y:0)}){bool wall=Enumerable.Range(-6,13).Where(i=>i!=0).All(i=>Zone.GetCell(p.x+i*d.y,p.y-i*d.x)?.Occupants.Count(e=>e.BlueprintName=="Hedge")==1);int route=Distance((p.x-d.x,p.y-d.y),(p.x+d.x,p.y+d.y));if(wall&&route>=14&&route<=60)return d;}Assert.Fail("Actual eight-direction dry physical route must have a substantial optional detour.");return default;}
  internal int Distance((int x,int y)a,(int x,int y)b)
  {bool Walk((int x,int y)p)=>Zone.InBounds(p.x,p.y)&&!Zone.GetCell(p.x,p.y).BlocksMovement()&&Zone.TileState.Get(p.x,p.y)?.IsEmpty!=false;var q=new Queue<(int x,int y,int n)>();var seen=new HashSet<(int,int)>();q.Enqueue((a.x,a.y,0));seen.Add(a);while(q.Count>0){var p=q.Dequeue();if((p.x,p.y)==b)return p.n;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){var next=(x:p.x+dx,y:p.y+dy);if((dx!=0||dy!=0)&&Walk(next)&&seen.Add(next))q.Enqueue((next.x,next.y,p.n+1));}}return -1;}
  public void Dispose()=>Scope.Dispose();
 }
 public sealed class SpreadHaulingLayoutTests
 {
  [Test]public void ExactOriginalPacketCreatesASubstantialOptionalShortcutWithoutAddingOwners()
  {using(var f=new HaulingFixture()){var owners=f.Zone.GetReadOnlyEntities().ToArray();var at=owners.Select(f.Zone.GetEntityPosition).ToArray();Assert.True(f.Place(out var load,out var final));Assert.True(final());Assert.AreSame(f.Receipt.Owners.Single(),load);CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());Assert.AreEqual(12,f.Hedges.Count(r=>!r.IsCurrent));Assert.LessOrEqual(owners.Count(e=>f.Zone.GetEntityPosition(e)!=at[Array.IndexOf(owners,e)]),13);f.Axis(load);Assert.False(f.Place(out _,out _));}}
  [Test]public void RealPlayerCanHaulTwoStepsReleaseAndUseTheShorterRoute()
  {using(var f=new HaulingFixture()){Assert.True(f.Place(out var load,out _));var d=f.Axis(load);var p=f.Zone.GetEntityPosition(load);var a=(x:p.x-d.x,y:p.y-d.y);var b=(x:p.x+d.x,y:p.y+d.y);int detour=f.Distance(a,b);var player=f.Scope.Factory.CreateEntity("Player");Assert.AreEqual(18,player.GetStatValue("Strength"));Assert.True(f.Zone.AddEntity(player,a.x,a.y));Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,load,f.Zone));Assert.That(player.GetStatValue("Speed"),Is.InRange(1,99));Assert.True(MovementSystem.TryMove(player,f.Zone,-d.x,-d.y));Assert.True(MovementSystem.TryMove(player,f.Zone,-d.x,-d.y));Assert.AreEqual((p.x-2*d.x,p.y-2*d.y),f.Zone.GetEntityPosition(load));DragSystem.Release(player);Assert.AreEqual(100,player.GetStatValue("Speed"));int clear=f.Distance(f.Zone.GetEntityPosition(player),b);Assert.That(clear,Is.InRange(1,6));Assert.GreaterOrEqual(detour,4+clear+6);}}
  [TestCase("authority")][TestCase("terrain-capture")][TestCase("load-moved")][TestCase("missing-hedges")][TestCase("reserved")][TestCase("wet")]
  public void InvalidSourceOrUnavailableSpaceLeavesAllOwnersWhereTheyWere(string fault)
  {using(var f=new HaulingFixture()){if(fault=="terrain-capture")f.Terrain.CapturePassageSources=false;if(fault=="load-moved")f.Zone.MoveEntity(f.Receipt.Owners.Single(),1,1);if(fault=="missing-hedges")foreach(var r in f.Hedges)r.Owners[0].GetPart<DestructiblePart>().HP=9;if(fault=="reserved"||fault=="wet")for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){if(fault=="reserved")f.Zone.GenReservedCells.Add((x,y));else f.Zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);}var owners=f.Zone.GetReadOnlyEntities().ToArray();var at=owners.Select(f.Zone.GetEntityPosition).ToArray();Assert.False(f.Place(out var load,out var final,()=>fault!="authority"));Assert.Null(load);Assert.Null(final);CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());CollectionAssert.AreEqual(at,owners.Select(f.Zone.GetEntityPosition));}}
 }
}
