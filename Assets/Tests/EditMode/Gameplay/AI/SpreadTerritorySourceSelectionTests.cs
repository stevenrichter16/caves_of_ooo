using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests {
 public class SpreadTerritorySourceSelectionTests {
  [TestCase("edge",true)][TestCase("edge",false)]
  [TestCase("distant",true)][TestCase("distant",false)]
  [TestCase("none",true)]
  public void IneligiblePostsDoNotExhaustTheBoundedEligibleSourceList(string clutter,bool valid)
  {
   using(var f=new SpreadExplorationActorTests.Fixture()){
    f.Zone.RemoveEntity(f.Player);f.Zone.RemoveEntity(f.Post);f.Zone.RemoveEntity(f.Food);f.Zone.RemoveEntity(f.Reserve);
    f.Actor.BlueprintName="MarlbackScrabbler";f.Move(f.Actor,60,15);
    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var ground=new Entity{ID="ground:"+x+","+y,BlueprintName="Grass"};ground.SetTag("Terrain");Assert.True(f.Zone.AddEntity(ground,x,y));}
    Entity Post(string id,int x,int y){var e=f.Prop(id,x,y);e.BlueprintName="Hedge";e.GetPart<PhysicsPart>().Solid=true;e.AddPart(new RenderPart{DisplayName="hedge"});return e;}
    var early=new List<Entity>();if(clutter!="none")for(int x=1;x<=32;x++)early.Add(Post("early:"+x,x,clutter=="edge"?0:5));
    Entity selected=valid?Post("actual-local-post",61,15):null;
    var owners=f.Zone.GetReadOnlyEntities().ToArray();var positions=owners.ToDictionary(e=>e,f.Zone.GetEntityPosition);int count=f.Zone.EntityCount;
    bool result=SpreadExplorationActorPlacement.TryTerritory(f.Zone,f.Actor,()=>true);
    Assert.AreEqual(valid,result,"A quota of32 usable post candidates must not be consumed by posts that cannot form the existing territory bounds/distance.");
    Assert.AreEqual(count,f.Zone.EntityCount);CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());
    foreach(var e in owners.Where(e=>e!=f.Actor))Assert.AreEqual(positions[e],f.Zone.GetEntityPosition(e));
    var role=f.Actor.GetPart<SpreadTerritoryPart>();
    if(valid){Assert.NotNull(role);Assert.True(role.Configured);Assert.AreSame(selected,role.Post);Assert.AreEqual(2,role.GraceTurns);Assert.IsNull(role.WarningTarget);Assert.AreEqual(0,role.ReturnAttempts);Assert.NotNull(SpreadExplorationActorPlacement.CaptureFinalGeometry(f.Zone,f.Actor));Assert.True(SpreadExplorationActorPlacement.CaptureFinalGeometry(f.Zone,f.Actor)());}
    else{Assert.IsNull(role);Assert.AreEqual(positions[f.Actor],f.Zone.GetEntityPosition(f.Actor));}
   }
  }
 }
}
