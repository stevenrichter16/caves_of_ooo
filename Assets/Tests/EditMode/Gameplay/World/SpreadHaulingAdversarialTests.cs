using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHaulingAdversarialTests
 {
  [TestCase("unchanged",true)][TestCase("unrelated",true)][TestCase("moved-load",false)][TestCase("hedge-hp",false)][TestCase("hedge-replaced",false)][TestCase("late-blocker",false)][TestCase("producer-rebuilt",false)]
  public void FinalProofPinsSourcesAndGeometryAndAllowsUnrelatedOffrouteContent(string fault,bool expected)
  {using(var f=new HaulingFixture()){Assert.True(f.Place(out var load,out var final));Assert.True(final());var d=f.Axis(load);var p=f.Zone.GetEntityPosition(load);var hedge=f.Hedges.First(r=>!r.IsCurrent).Owners.Single();if(fault=="unrelated")f.Zone.AddEntity(f.Scope.Factory.CreateEntity("Cudgel"),0,0);if(fault=="moved-load")f.Zone.MoveEntity(load,1,1);if(fault=="hedge-hp")hedge.GetPart<DestructiblePart>().HP--;if(fault=="hedge-replaced"){var at=f.Zone.GetEntityPosition(hedge);f.Zone.RemoveEntity(hedge);var other=f.Scope.Factory.CreateEntity("Hedge");other.ID=hedge.ID;f.Zone.AddEntity(other,at.x,at.y);}if(fault=="late-blocker")f.Zone.AddEntity(f.Scope.Factory.CreateEntity("StoneWall"),p.x-d.x,p.y-d.y);if(fault=="producer-rebuilt")f.Haul.BuildZone(new Zone(f.Zone.ZoneID),f.Scope.Factory,new Random(1));Assert.AreEqual(expected,final());}}

  [TestCase("continue",true)][TestCase("deny",false)][TestCase("weight",false)][TestCase("moved",false)][TestCase("removed",false)][TestCase("replacement",false)][TestCase("duplicate-id",false)][TestCase("producer-rebuilt",false)][TestCase("terrain-rebuilt",false)][TestCase("new-offroute",false)][TestCase("unselected-state",false)][TestCase("reserved",false)][TestCase("foreign-donor",false)]
  public void MutationAtActualAuthoritySeamCannotCommitOrClobberIndependentOwners(string fault,bool expected)
  {
   using(var f=new HaulingFixture())
   {
    var owners=f.Zone.GetReadOnlyEntities().ToArray();var at=owners.ToDictionary(e=>e,f.Zone.GetEntityPosition);Entity changed=null,independent=null;bool fired=false;
    bool Authority()
    {
     if(fired)return fault!="deny";
     changed=owners.FirstOrDefault(e=>f.Zone.GetEntityPosition(e)!=at[e]);if(changed==null)return true;fired=true;
     if(fault=="weight")changed.GetPart<PhysicsPart>().Weight++;
     if(fault=="moved")Assert.True(f.Zone.MoveEntity(changed,1,1));
     if(fault=="removed")Assert.True(f.Zone.RemoveEntity(changed));
     if(fault=="replacement"){var current=f.Zone.GetEntityPosition(changed);Assert.True(f.Zone.RemoveEntity(changed));independent=f.Scope.Factory.CreateEntity(changed.BlueprintName);independent.ID=changed.ID;Assert.True(f.Zone.AddEntity(independent,current.x,current.y));}
     if(fault=="duplicate-id"){independent=f.Scope.Factory.CreateEntity(changed.BlueprintName);independent.ID=changed.ID;Assert.True(f.Zone.AddEntity(independent,1,1));}
     if(fault=="producer-rebuilt")Assert.True(f.Haul.BuildZone(new Zone(f.Zone.ZoneID),f.Scope.Factory,new Random(1)));
     if(fault=="terrain-rebuilt")Assert.True(f.Terrain.BuildZone(new Zone(f.Zone.ZoneID),f.Scope.Factory,new Random(1)));
     if(fault=="new-offroute"){independent=f.Scope.Factory.CreateEntity("Cudgel");Assert.True(f.Zone.AddEntity(independent,0,0));}
     if(fault=="unselected-state"){var other=owners.First(e=>e.BlueprintName=="Hedge"&&!f.Hedges.Any(r=>!r.IsCurrent&&r.Owners[0]==e));other.GetPart<DestructiblePart>().HP--;}
     if(fault=="reserved")f.Zone.GenReservedCells.Add(f.Zone.GetEntityPosition(changed));
     if(fault=="foreign-donor"){independent=f.Scope.Factory.CreateEntity("Cudgel");var donor=at[changed];Assert.True(f.Zone.AddEntity(independent,donor.x,donor.y));}
     return fault!="deny";
    }
    Assert.AreEqual(expected,f.Place(out var load,out var final,Authority));Assert.True(fired,"Stimulus must occur after an actual selected-owner move.");
    if(expected){Assert.NotNull(final);Assert.True(final());}else{Assert.Null(load);Assert.Null(final);}
    if(fault=="deny")foreach(var e in owners)Assert.AreEqual(at[e],f.Zone.GetEntityPosition(e),"Clean denial restores every original anchor.");
    if(independent!=null)Assert.NotNull(f.Zone.GetEntityCell(independent),"Independent owner is never reclaimed by rollback.");
    if(fault=="moved")Assert.AreEqual((1,1),f.Zone.GetEntityPosition(changed));if(fault=="removed"||fault=="replacement")Assert.Null(f.Zone.GetEntityCell(changed));
    if(fault=="weight")Assert.AreEqual(31,changed.GetPart<PhysicsPart>().Weight);
   }
  }

  [TestCase(false)][TestCase(true)]
  public void LastAuthorizationCannotAddAnUnownedObjectBetweenTheMoveLoopAndCommit(bool changed)
  {
   using(var f=new HaulingFixture())
   {
    var load=f.Receipt.Owners.Single();var original=f.Zone.GetEntityPosition(load);int afterLoadMove=0;Entity independent=null;
    bool Authority(){if(f.Zone.GetEntityPosition(load)!=original&&++afterLoadMove==2&&changed){independent=f.Scope.Factory.CreateEntity("Cudgel");Assert.True(f.Zone.AddEntity(independent,0,0));}return true;}
    Assert.AreEqual(!changed,f.Place(out var made,out var final,Authority));Assert.GreaterOrEqual(afterLoadMove,2);
    if(changed){Assert.Null(made);Assert.Null(final);Assert.NotNull(f.Zone.GetEntityCell(independent));Assert.AreEqual(original,f.Zone.GetEntityPosition(load));}else Assert.True(final());
   }
  }

  [TestCase(false)][TestCase(true)]
  public void InitialAuthorizationCannotLaunderANewOwnerIntoTheOriginalPacket(bool changed)
  {using(var f=new HaulingFixture()){bool fired=false;Entity foreign=null;var originals=f.Zone.GetReadOnlyEntities().ToDictionary(e=>e,f.Zone.GetEntityPosition);bool Authority(){if(!fired){fired=true;if(changed){foreign=f.Scope.Factory.CreateEntity("Cudgel");Assert.True(f.Zone.AddEntity(foreign,0,0));}}return true;}Assert.AreEqual(!changed,f.Place(out _,out _,Authority));Assert.True(fired);if(changed){Assert.NotNull(f.Zone.GetEntityCell(foreign));foreach(var p in originals)Assert.AreEqual(p.Value,f.Zone.GetEntityPosition(p.Key));}}}
 }
}
