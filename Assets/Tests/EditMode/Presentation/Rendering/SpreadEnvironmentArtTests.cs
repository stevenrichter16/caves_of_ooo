using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadEnvironmentArtTests
 {
  static string Id(string family,int variant)=>"spread-environment-"+family+"-"+variant;
  static GameObject Prefab(string family,int variant)=>Resources.Load<GameObject>("SpreadEnvironment3D/"+Id(family,variant));
  static SpawnRing3DRecipe Exact(SpawnRing3DIntegrationFixture f,Entity owner,string family)
  {
   var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(recipe.Failure);Assert.That(recipe.ModelId,Does.StartWith("spread-environment-"+family+"-"));
   Assert.True(recipe.Batched);Assert.False(recipe.Transient);Assert.AreSame(owner,recipe.Owner);
   Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);
   int variant=recipe.ModelId[recipe.ModelId.Length-1]-'0';var prefab=Prefab(family,variant);Assert.NotNull(prefab);
   Assert.AreSame(prefab.GetComponent<MeshFilter>().sharedMesh,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);
   return recipe;
  }
  [TestCase("Floor","paving")][TestCase("RoadStone","road")][TestCase("Tree","tree")][TestCase("Hedge","hedge")]
  [TestCase("VineWall","vine-wall")][TestCase("CropRow","stubble")][TestCase("RipeCropRow","grain")][TestCase("FlowerField","flowers")]
  [TestCase("CharmFlowers","flowers")][TestCase("DryBrush","dry-brush")][TestCase("Rock","rock")]
  public void ExactNativeFamilyGetsItsDistinctApprovedCurrentGeometry(string blueprint,string family)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add(blueprint);string id=owner.ID,tiles=f.Zone.TileState.ToSaveString();var parts=owner.Parts.ToArray();var pos=f.Zone.GetEntityPosition(owner);int version=f.Zone.EntityVersion;
    f.Refresh();var first=Exact(f,owner,family);f.Refresh();Assert.AreEqual(first.ModelId,Exact(f,owner,family).ModelId);
    Assert.AreEqual(id,owner.ID);Assert.AreEqual(pos,f.Zone.GetEntityPosition(owner));Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());CollectionAssert.AreEqual(parts,owner.Parts);
   }
  }
  [TestCase("paving")][TestCase("road")][TestCase("tree")][TestCase("hedge")][TestCase("vine-wall")]
  [TestCase("stubble")][TestCase("grain")][TestCase("flowers")][TestCase("dry-brush")][TestCase("rock")]
  public void AllFourPersistentFormsUseActualBoundedApprovedPaletteGeometry(string family)
  {
   var meshes=new System.Collections.Generic.HashSet<Mesh>();
   for(int variant=0;variant<4;variant++){
    var prefab=Prefab(family,variant);Assert.NotNull(prefab,"Native adopted assets required, not private source JSON.");var filter=prefab.GetComponent<MeshFilter>();var renderer=prefab.GetComponent<MeshRenderer>();
    Assert.NotNull(filter);Assert.NotNull(renderer);var mesh=filter.sharedMesh;Assert.NotNull(mesh);Assert.True(meshes.Add(mesh));Assert.True(mesh.isReadable);Assert.Greater(mesh.vertexCount,24);Assert.AreEqual(1,mesh.subMeshCount);
    Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,renderer.sharedMaterial);Assert.AreEqual(Vector3.one,prefab.transform.localScale);Assert.AreEqual(Quaternion.identity,prefab.transform.localRotation);
    Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(prefab.GetComponentsInChildren<Animator>(true));Assert.That(mesh.bounds.min.x,Is.GreaterThanOrEqualTo(-.501f));Assert.That(mesh.bounds.max.x,Is.LessThanOrEqualTo(.501f));
    if(family=="tree")Assert.That(mesh.bounds.size.y,Is.InRange(1.25f,1.85f));
    if(family=="stubble")Assert.Less(mesh.bounds.max.y,.16f);
    if(family=="grain")Assert.Greater(mesh.bounds.max.y,.4f);
   }
  }
  [TestCase("custom")][TestCase("hidden")][TestCase("portable")][TestCase("foreign")]
  public void UnapprovedMutationDoesNotAcquireThisScopedTree(string change)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("Tree");if(change=="custom")owner.GetPart<RenderPart>().VisualID="unrelated-tree";
    else if(change=="hidden")owner.GetPart<RenderPart>().Visible=false;else if(change=="portable")owner.GetPart<PhysicsPart>().Takeable=true;else f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;
    f.Refresh();Assert.False(SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId?.StartsWith("spread-environment-tree-",StringComparison.Ordinal)==true);
   }
  }
  [Test] public void NativeHarvestStateSwitchesGrainToStubbleWithoutRegrowingIt()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("RipeCropRow");var harvest=owner.GetPart<FieldHarvestPart>();Assert.NotNull(harvest);Assert.False(harvest.Harvested);
    f.Refresh();Exact(f,owner,"grain");harvest.Harvested=true;f.Refresh();Exact(f,owner,"stubble");Assert.True(harvest.Harvested);
    f.BindLoaded(f.RoundTrip());owner=f.Zone.GetReadOnlyEntities().Single(x=>x.BlueprintName=="RipeCropRow"&&x.ID==owner.ID);Assert.True(owner.GetPart<FieldHarvestPart>().Harvested);Exact(f,owner,"stubble");
   }
  }
  [TestCase("unmodeled-native-blueprint")][TestCase("missing-native-quest-markers")]
  public void AFailedNativeContractIsNeverRescuedByEnvironmentStyling(string failure)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("Tree");var prior=new SpawnRing3DRecipe(owner,null,null,Vector3.zero,false,true,failure);
    var type=typeof(SpawnRing3DRecipes).Assembly.GetType("CavesOfOoo.Rendering.SpreadEnvironmentRecipes");Assert.NotNull(type);
    var method=type.GetMethod("Refine",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);Assert.NotNull(method);
    var next=(SpawnRing3DRecipe)method.Invoke(null,new object[]{f.Zone,owner,prior});Assert.AreEqual(prior.Failure,next.Failure);Assert.IsNull(next.ModelId);Assert.AreSame(owner,next.Owner);
   }
  }
  [TestCase("spread-reeds-0")][TestCase("sumphold-ground-2")]
  public void SuccessfulUnrelatedNativeShapeIsNeverOverwrittenByBlueprintAlone(string priorModel)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("Tree");var prior=new SpawnRing3DRecipe(owner,priorModel,null,new Vector3(7,0,8),false,true,quarterTurns:3);
    var type=typeof(SpawnRing3DRecipes).Assembly.GetType("CavesOfOoo.Rendering.SpreadEnvironmentRecipes");Assert.NotNull(type);
    var next=(SpawnRing3DRecipe)type.GetMethod("Refine",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{f.Zone,owner,prior});
    Assert.AreEqual(prior.ModelId,next.ModelId);Assert.AreEqual(prior.Position,next.Position);Assert.AreEqual(prior.QuarterTurns,next.QuarterTurns);
   }
  }
  [Test] public void ActualAuthoredTreeBasesOwnOnlyVisibleBoundedContact()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    foreach(var owner in f.Zone.GetReadOnlyEntities().ToArray())if(owner!=f.Player)f.Zone.RemoveEntity(owner);
    var tree=f.Add("Tree",20,10);f.Reveal();f.Refresh();Exact(f,tree,"tree");
    var material=((SpawnRing3DPresenter)f.Presenter).ActiveSurface.MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);
    var mask=material.GetTexture("_GroundContact")as Texture2D;Assert.NotNull(mask);Assert.Greater(mask.GetPixels32().Max(c=>c.r),70);
    f.Zone.GetEntityCell(tree).IsVisible=false;f.Refresh();Assert.AreSame(mask,material.GetTexture("_GroundContact"));Assert.Zero(mask.GetPixels32().Max(c=>c.r));
    f.Zone.GetEntityCell(tree).IsVisible=true;f.Refresh();Assert.Greater(mask.GetPixels32().Max(c=>c.r),70);
    f.Zone.RemoveEntity(tree);f.Refresh();Assert.Zero(mask.GetPixels32().Max(c=>c.r));Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(tree,out _));
   }
  }
 }
}
