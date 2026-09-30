using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class MarlbackCasterArtTests
 {
  static string Expected(string bp)=>"spread-person-marlback-"+(bp=="MarlbackCindercaller"?"cindercaller":"soursprayer");
  [TestCase("MarlbackCindercaller")][TestCase("MarlbackSoursprayer")]
  public void ActualCasterUsesOwnRigAndRealNativeEquipmentWithoutChangingOwner(string bp)
  {
   using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(bp),"Actual child must precede interpreting missing-model RED.");
    var e=f.Add(bp);Assert.NotNull(e.GetPart<Body>());var at=f.Zone.GetEntityPosition(e);var inv=e.GetPart<InventoryPart>();var carried=inv?.Objects.ToArray();
    var r=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);Assert.AreEqual(Expected(bp),r.ModelId);f.Refresh();
    Assert.True(f.Find(e,out var root,out var id));Assert.AreEqual(r.ModelId,id);Assert.True(f.Rendered(e));
    var entry=SpreadBiomeHumanoidLibrary.Load().Find(id);Assert.NotNull(entry);var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();Assert.AreSame(entry.Mesh,skin.sharedMesh);
    Assert.AreEqual(9,skin.bones.Length);Assert.GreaterOrEqual(skin.sharedMesh.uv.Distinct().Count(),3);
    var anim=root.GetComponentInChildren<Animator>();CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},anim.runtimeAnimatorController.animationClips.Select(c=>c.name));
    Assert.True(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Equipment.Hand.L"));Assert.True(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Equipment.Hand.R"));
    Assert.AreEqual(at,f.Zone.GetEntityPosition(e));if(carried!=null)CollectionAssert.AreEqual(carried,inv.Objects);
    e.GetPart<RenderPart>().Visible=false;f.Refresh();Assert.False(f.Rendered(e));e.GetPart<RenderPart>().Visible=true;f.Refresh();Assert.True(f.Rendered(e));f.Zone.RemoveEntity(e);f.Refresh();Assert.False(f.Find(e,out _,out _));
   }
  }
  [TestCase("MarlbackCindercaller")][TestCase("MarlbackSoursprayer")]
  public void InitialRecipeAndFinalRefinementAgreeOnTheNewExactIdentity(string bp)
  {using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID)){Assert.True(f.Factory.Blueprints.ContainsKey(bp));var e=f.Add(bp);var method=typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.NonPublic|BindingFlags.Static);var r=(SpawnRing3DRecipe)method.Invoke(null,new object[]{f.Zone,e,f.Library.Definition,null});Assert.AreEqual(Expected(bp),r.ModelId);Assert.Null(r.Failure);Assert.AreEqual(r.ModelId,SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}}
  [TestCase("MarlbackCindercaller")][TestCase("MarlbackSoursprayer")]
  public void AlteredCasterAppearanceCannotBorrowTheExactNewBody(string bp)
  {using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID)){Assert.True(f.Factory.Blueprints.ContainsKey(bp));var e=f.Add(bp);e.GetPart<RenderPart>().ColorString="&B";Assert.AreNotEqual(Expected(bp),SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}}
  [TestCase("MarlbackScrabbler")][TestCase("Farmer")]
  public void ExistingActorBodyKeepsItsOwnIdentity(string bp)
  {using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID)){var e=f.Add(bp);var id=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId;Assert.NotNull(id);Assert.False(id.Contains("cindercaller")||id.Contains("soursprayer"));}}
  [Test]
  public void ExistingImporterOffersScopedRoleSelectionWithoutAnotherRigPipeline()
  {var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.SpreadBiomeHumanoidBuilder")).FirstOrDefault(t=>t!=null);Assert.NotNull(type);Assert.NotNull(type.GetMethod("Build",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(string),typeof(string),typeof(string[])},null));}
 }
}
