using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class SpreadExplorationFinalGeometryTests
 {
  static object Geometry(Zone z){var t=typeof(SpreadWildernessSituationBuilder).GetNestedType("Geometry",BindingFlags.NonPublic);Assert.NotNull(t);return Activator.CreateInstance(t,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{z,new HashSet<Entity>()},null);}
  static bool Preserves(object current,object original){var m=current.GetType().GetMethod("PreservesAgainst",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(m,"A later builder must preserve the previously accepted approaches, not redefine critical cells after blocking them.");return (bool)m.Invoke(current,new object[]{original,new (int x,int y)[0]});}
  static void Wall(Zone z,int x,int y){var e=new Entity{ID=x+":"+y,BlueprintName="fixture-wall"};e.AddPart(new PhysicsPart{Solid=true});z.AddEntity(e,x,y);}
  [Test]public void LaterCrossingBarrierCannotBecomeItsOwnPermissiveBaseline(){var z=new Zone("late-geometry");var original=Geometry(z);Assert.True(Preserves(Geometry(z),original));for(int x=1;x<Zone.Width-1;x++)Wall(z,x,12);Wall(z,0,12);Wall(z,Zone.Width-1,12);Assert.False(Preserves(Geometry(z),original));}
  [Test]public void HarmlessInteriorObstaclePreservesEarlierConnections(){var z=new Zone("late-geometry-control");var original=Geometry(z);Wall(z,40,12);Assert.True(Preserves(Geometry(z),original));}
 }
}
