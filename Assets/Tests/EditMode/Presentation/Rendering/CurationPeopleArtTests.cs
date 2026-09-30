using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 // Real content and imported native rig; no paid-action, animation-feel or pixel claim.
 public sealed class CurationPeopleArtTests
 {
  static string Model(string bp)=>"spread-person-"+(bp=="CurationIntakeFiler"?"curation-intake-filer":bp=="CurationJuniorIndexer"?"curation-junior-indexer":"curation-half-set");
  [TestCase("CurationIntakeFiler")][TestCase("CurationJuniorIndexer")][TestCase("CurationHalfSet")]
  public void OriginalCurrentPersonUsesOwnPaletteBodyAndFiveActualClips(string bp)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(bp),"SOURCE PRECONDITION: adopt actual receiving-yard content before interpreting art RED.");var e=f.Add(bp);Assert.AreEqual(bp=="CurationHalfSet"?"h":"@",e.GetPart<RenderPart>().RenderString);Assert.AreEqual(bp=="CurationIntakeFiler"?"&W":bp=="CurationJuniorIndexer"?"&w":"&g",e.GetPart<RenderPart>().ColorString);var at=f.Zone.GetEntityPosition(e);var hp=e.GetStatValue("Hitpoints");var native=(SpawnRing3DRecipe)typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{f.Zone,e,f.Library.Definition,null});
    Assert.AreEqual(Model(bp),native.ModelId,native.Failure);f.Refresh();Assert.True(f.Find(e,out var view,out var id));Assert.AreEqual(Model(bp),id);Assert.True(f.Rendered(e));
    var entry=SpreadBiomeHumanoidLibrary.Load().Find(id);Assert.NotNull(entry);var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();Assert.AreSame(entry.Mesh,skin.sharedMesh);Assert.AreEqual(9,skin.bones.Length);Assert.AreEqual(9,entry.Mesh.bindposeCount);Assert.AreEqual(entry.Mesh.vertexCount,entry.Mesh.boneWeights.Length);Assert.GreaterOrEqual(entry.Mesh.uv.Distinct().Count(),3);
    Assert.AreNotSame(SpreadBiomeHumanoidLibrary.Load().Find("spread-person-filer-clerk").Mesh,entry.Mesh);CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},view.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Select(c=>c.name));
    Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(e,out var proof),proof.Failure);Assert.AreSame(entry.Mesh,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);Assert.AreEqual(at,f.Zone.GetEntityPosition(e));Assert.AreEqual(hp,e.GetStatValue("Hitpoints"));
    e.GetPart<RenderPart>().Visible=false;f.Refresh();Assert.False(f.Rendered(e));e.GetPart<RenderPart>().Visible=true;f.Refresh();Assert.True(f.Rendered(e));f.Zone.RemoveEntity(e);f.Refresh();Assert.False(f.Rendered(e));Assert.False(f.Find(e,out _,out _));
   }
  }
  [TestCase("CurationIntakeFiler","glyph")][TestCase("CurationJuniorIndexer","custom")][TestCase("CurationHalfSet","foreign-brain")]
  [TestCase("CurationHalfSet","foreign-spatial")]
  public void ChangedOrForeignActorCannotBorrowOriginalRole(string bp,string fault)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(bp));var e=f.Add(bp);var brain=e.GetPart<BrainPart>();var spatial=typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic);var prior=spatial.GetValue(e);
    try{if(fault=="glyph")e.GetPart<RenderPart>().RenderString="?";if(fault=="custom")e.GetPart<RenderPart>().VisualID="other-authored-model";if(fault=="foreign-brain")brain.ParentEntity=f.Player;if(fault=="foreign-spatial")spatial.SetValue(e,new Zone(f.Zone.ZoneID));Assert.AreNotEqual(Model(bp),SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}
    finally{brain.ParentEntity=e;spatial.SetValue(e,prior);}
   }
  }
  [Test]public void ExistingCourierClerkKeepsItsOwnBodyAndConversation()
  {using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID)){var e=f.Zone.GetReadOnlyEntities().Single(x=>x.BlueprintName=="FilerClerk");Assert.AreEqual("FilerClerk_1",e.GetPart<ConversationPart>().ConversationID);Assert.AreEqual("spread-person-filer-clerk",SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}}
 }
}
