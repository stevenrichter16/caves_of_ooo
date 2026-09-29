using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHuntSourceTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  static void Enable(SpreadCompositionBuilder b,bool enabled){var field=typeof(SpreadCompositionBuilder).GetField("CaptureCoverSources",All);Assert.NotNull(field,"Capture only successful original opaque Trees before the cold pair transaction.");field.SetValue(b,enabled);}
  static SpreadGenerationReceipt[] Sources(SpreadCompositionBuilder b){var prop=typeof(SpreadCompositionBuilder).GetProperty("CoverSources",All);Assert.NotNull(prop);return((IEnumerable)prop.GetValue(b)).Cast<SpreadGenerationReceipt>().ToArray();}
  static string[] Shape(Zone z)=>z.GetReadOnlyEntities().Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e)+":"+e.GetPart<DestructiblePart>()?.HP).OrderBy(s=>s).ToArray();
  [Test]public void CoverCaptureRecordsExactOriginalTreesAndPreservesOrdinaryShapeAndRng()
  {using(var scope=new HaulingContentScope()){var on=new SpreadCompositionBuilder(1){FormationOverride=Formation.Fallow};Enable(on,true);var off=new SpreadCompositionBuilder(1){FormationOverride=Formation.Fallow};Enable(off,false);var a=new Zone("Overworld.10.4.0");var b=new Zone(a.ZoneID);var r1=new Random(64);var r2=new Random(64);Assert.True(on.BuildZone(a,scope.Factory,r1));Assert.True(off.BuildZone(b,scope.Factory,r2));CollectionAssert.AreEqual(Shape(a),Shape(b));Assert.AreEqual(r1.Next(),r2.Next());var sources=Sources(on);Assert.IsNotEmpty(sources);Assert.AreEqual(a.GetReadOnlyEntities().Count(e=>e.BlueprintName=="Tree"),sources.Length);Assert.IsEmpty(Sources(off));Assert.True(sources.All(s=>s.IsCurrent&&s.Zone==a&&s.Factory==scope.Factory&&s.Owners.Count==1&&s.Owners[0].BlueprintName=="Tree"));}}
  [Test]public void MatchingLateTreeCannotBecomeAnOriginalCoverSource()
  {using(var scope=new HaulingContentScope()){var b=new SpreadCompositionBuilder(1){FormationOverride=Formation.Fallow};Enable(b,true);var z=new Zone("Overworld.10.4.0");Assert.True(b.BuildZone(z,scope.Factory,new Random(1)));var before=Sources(b);var decoy=scope.Factory.CreateEntity("Tree");Assert.True(z.AddEntity(decoy,40,12));CollectionAssert.AreEqual(before,Sources(b));Assert.False(Sources(b).Any(r=>r.Owners.Contains(decoy)));}}
  [Test]public void RebuildAndInvalidRetryCannotReuseAnOlderTreeReceipt()
  {using(var scope=new HaulingContentScope()){var b=new SpreadCompositionBuilder(1){FormationOverride=Formation.Fallow};Enable(b,true);var z=new Zone("Overworld.10.4.0");Assert.True(b.BuildZone(z,scope.Factory,new Random(1)));var old=Sources(b).First();Assert.True(old.IsCurrent);Assert.False(b.BuildZone(z,scope.Factory,new Random(1)));Assert.False(old.IsCurrent);Assert.IsEmpty(Sources(b));}}
  [Test]public void ChangedOriginalTreeLosesItsSourceAuthority()
  {using(var scope=new HaulingContentScope()){var b=new SpreadCompositionBuilder(1){FormationOverride=Formation.Fallow};Enable(b,true);var z=new Zone("Overworld.10.4.0");Assert.True(b.BuildZone(z,scope.Factory,new Random(1)));var source=Sources(b).First();Assert.True(source.IsCurrent);source.Owners[0].GetPart<DestructiblePart>().HP--;Assert.False(source.IsCurrent);}}
 }
}
