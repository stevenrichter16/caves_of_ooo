using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationEncounterBypassTests
 {
  [TestCase("Viper","visible-alternative",true)][TestCase("Viper","occluded-alternative",true)][TestCase("Viper","no-alternative",false)][TestCase("Viper","near-edge",false)]
  [TestCase("MarlbackScrabbler","visible-alternative",true)][TestCase("MarlbackScrabbler","occluded-alternative",true)][TestCase("MarlbackScrabbler","no-alternative",false)][TestCase("MarlbackScrabbler","near-edge",false)]
  public void ArrivalStandoffAndRealSightBypassAreSeparateProofs(string blueprint,string layout,bool expected)
  {
   using(var scope=new SpreadExplorationActorTests.Scope())
   {
    var f=new EntityFactory();f.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
    var z=new Zone("Overworld.7.9.0");for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var e=new Entity{ID="ground-"+x+","+y,BlueprintName="Grass"};e.Tags["Terrain"]="true";Assert.True(z.AddEntity(e,x,y));}
    var snake=f.CreateEntity(blueprint);int sy=layout=="near-edge"?3:layout=="no-alternative"?11:4;Assert.True(z.AddEntity(snake,30,sy));snake.GetPart<BrainPart>().CurrentZone=z;
    void Wall(int x,int y){var e=new Entity{ID="wall-"+x+","+y};e.Tags["Solid"]="true";e.AddPart(new PhysicsPart{Solid=true});Assert.True(z.AddEntity(e,x,y));}
    if(layout=="occluded-alternative")for(int x=20;x<=40;x++)Wall(x,2);
    if(layout=="no-alternative")for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(y!=12&&!(x==30&&y==sy))Wall(x,y);
    Assert.AreEqual(10,snake.GetPart<BrainPart>().SightRadius);
    if(layout=="visible-alternative")Assert.True(AIHelpers.HasLineOfSight(z,30,sy,30,0),"Visible enemy pressure is real; the safer route is elsewhere.");
    if(layout=="occluded-alternative")Assert.False(AIHelpers.HasLineOfSight(z,30,sy,30,0),"Actual Solid cover, not a visual-only hedge.");
    var type=typeof(SpreadWildernessSituationBuilder).GetNestedType("Geometry",BindingFlags.NonPublic);Assert.NotNull(type);
    object geometry=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{z,new HashSet<Entity>{snake}},null);
    var method=typeof(SpreadExplorationActorPlacement).GetMethod("SafeBypass",BindingFlags.NonPublic|BindingFlags.Static);Assert.NotNull(method);
    Assert.AreEqual(expected,(bool)method.Invoke(null,new object[]{z,geometry,new[]{snake},new[]{(30,sy)}}));
   }
  }
 }
}
