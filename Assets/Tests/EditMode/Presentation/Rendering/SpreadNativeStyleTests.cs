using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadNativeStyleTests
 {
  static bool ActiveUnder(Transform child,Transform root){for(var p=child;p!=null;p=p.parent){if(!p.gameObject.activeSelf)return false;if(p==root)return true;}return false;}
  static ScriptableObject Library(){var asset=Resources.Load<ScriptableObject>("SpreadNativeStyle3D/Library");Assert.NotNull(asset,"Explicit adopted native-state style library is required.");return asset;}
  static object Entry(string id){var library=Library();var method=library.GetType().GetMethod("Find");Assert.NotNull(method);var row=method.Invoke(library,new object[]{id});Assert.NotNull(row,id);return row;}
  static T Field<T>(object value,string name){var f=value.GetType().GetField(name);Assert.NotNull(f,name);return(T)f.GetValue(value);}
  static SpawnRing3DRecipe Exact(SpawnRing3DIntegrationFixture f,Entity owner)
  {
   var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(recipe.Failure);Assert.NotNull(recipe.ModelId);
   var entry=Entry(recipe.ModelId);var original=f.Library.FindModel(recipe.ModelId);Assert.AreSame(original,Field<GameObject>(entry,"SourcePrefab"));
   Assert.AreNotSame(original,Field<GameObject>(entry,"Prefab"));Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);
   Assert.AreEqual(recipe.ModelId,proof.ModelId);Assert.AreSame(Field<Mesh>(entry,"Mesh"),proof.ExpectedMesh);Assert.AreSame(Field<Material>(entry,"Material"),proof.ExpectedMaterial);return recipe;
  }
  [TestCase("Overworld.12.10.0","Sack")][TestCase("Overworld.12.10.0","PeatBog")]
  [TestCase("Overworld.12.10.0","Campfire")][TestCase("Overworld.12.10.0","Crate")]
  [TestCase("Overworld.10.10.0","WaterPuddle")][TestCase("Overworld.15.6.0","SandstoneWall")]
  public void SuccessfulNativeOwnerRetainsItsModelAndStateWithExactScopedGeometry(string zone,string blueprint)
  {
   using(var f=new SpawnRing3DIntegrationFixture(zone)){
    var owner=f.Add(blueprint);var before=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(before.Failure,"real native prerequisite");
    var parts=owner.Parts.ToArray();int version=f.Zone.EntityVersion;string tiles=f.Zone.TileState.ToSaveString();var position=f.Zone.GetEntityPosition(owner);
    f.Refresh();var actual=Exact(f,owner);Assert.AreEqual(before.ModelId,actual.ModelId);Assert.AreEqual(before.Position,actual.Position);Assert.AreEqual(before.QuarterTurns,actual.QuarterTurns);
    Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(position,f.Zone.GetEntityPosition(owner));Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());CollectionAssert.AreEqual(parts,owner.Parts);
   }
  }
  [Test] public void AlreadyApprovedStoneFloorRemainsTheExactEnvironmentBodyWithoutResidualOverride()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.15.6.0")){
    var owner=f.Add("StoneFloor");var before=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(before.Failure);
    Assert.That(before.ModelId,Does.StartWith("spread-environment-paving-"));var environment=SpreadEnvironment3DLibrary.Load();Assert.NotNull(environment);
    var expected=environment.Find(before.ModelId);Assert.NotNull(expected);var residual=SpreadNativeStyle3DLibrary.Load();Assert.NotNull(residual);Assert.IsNull(residual.ForOwner(f.Zone,before));
    var parts=owner.Parts.ToArray();int version=f.Zone.EntityVersion;string tiles=f.Zone.TileState.ToSaveString();var position=f.Zone.GetEntityPosition(owner);
    f.Refresh();var actual=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.AreEqual(before.ModelId,actual.ModelId);Assert.AreEqual(before.Position,actual.Position);Assert.AreEqual(before.QuarterTurns,actual.QuarterTurns);
    var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.True(presenter.TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.AreSame(expected.Mesh,proof.ExpectedMesh);Assert.AreSame(environment.Material,proof.ExpectedMaterial);
    Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(position,f.Zone.GetEntityPosition(owner));Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());CollectionAssert.AreEqual(parts,owner.Parts);
   }
  }
  [TestCase(false,0)][TestCase(false,1)][TestCase(true,0)][TestCase(true,3)]
  public void DoorNativeSavedStateAndAxisRemainExact(bool open,int turn)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("VillageDoor");owner.GetPart<DoorPart>().IsOpen=open;owner.GetPart<DoorPart>().QuarterTurns=turn;owner.GetPart<RenderPart>().RenderString=open?"/":"+";
    f.Refresh();var recipe=Exact(f,owner);Assert.That(recipe.ModelId,Does.StartWith(open?"stillleaf-open-door-":"stillleaf-door-"));Assert.AreEqual(turn,recipe.QuarterTurns);
    Assert.AreEqual(open,owner.GetPart<DoorPart>().IsOpen);Assert.AreEqual(turn,owner.GetPart<DoorPart>().QuarterTurns);
   }
  }
  [TestCase("ring-sack")][TestCase("stillleaf-door-0")][TestCase("sumphold-water-0")]
  public void CopiedMeshesPreserveNativeGeometryAndBorrowNoMutableSourceBuffers(string id)
  {
   var entry=Entry(id);var mesh=Field<Mesh>(entry,"Mesh");var prefab=Field<GameObject>(entry,"Prefab");var source=Field<GameObject>(entry,"SourcePrefab");
   Assert.NotNull(mesh);Assert.True(mesh.isReadable);Assert.Greater(mesh.vertexCount,0);Assert.AreEqual(1,mesh.subMeshCount);Assert.AreSame(mesh,prefab.GetComponent<MeshFilter>().sharedMesh);
   Assert.AreEqual(source.transform.localRotation,prefab.transform.localRotation);Assert.AreEqual(source.transform.localScale,prefab.transform.localScale);
   var sources=Field<Mesh[]>(entry,"SourceMeshes");var matrices=Field<Matrix4x4[]>(entry,"SourceTransforms");Assert.Greater(sources.Length,0);Assert.AreEqual(sources.Length,matrices.Length);
   var native=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);Assert.NotNull(native);
   var originals=source.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&ActiveUnder(r.transform,source.transform)).ToArray();Assert.AreEqual(originals.Length,sources.Length);
   for(int i=0;i<originals.Length;i++){
    Assert.AreSame(native.Resolve(originals[i].GetComponent<MeshFilter>().sharedMesh),sources[i],"actual adopted native source mesh");
    var relative=source.transform.worldToLocalMatrix*originals[i].transform.localToWorldMatrix;
    for(int element=0;element<16;element++)Assert.That(matrices[i][element],Is.EqualTo(relative[element]).Within(1e-6f),"actual native source transform");
   }
   var expected=sources.SelectMany((s,i)=>s.vertices.Select(v=>matrices[i].MultiplyPoint3x4(v))).ToArray();Assert.AreEqual(expected.Length,mesh.vertexCount);
   var actual=mesh.vertices;for(int i=0;i<actual.Length;i++)Assert.Less((actual[i]-expected[i]).sqrMagnitude,1e-10f,"source vertex "+i);
   foreach(var s in sources)Assert.AreNotSame(s,mesh);Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(prefab.GetComponentsInChildren<Animator>(true));
  }
  [Test] public void ExactSemanticWaterPaletteCellsRemainAuthoredWhileOrdinaryPropsUseApprovedPalette()
  {
   var water=Entry("sumphold-water-0");Assert.True(Field<bool>(water,"SemanticColors"));var material=Field<Material>(water,"Material");
   var source=Field<GameObject>(water,"SourcePrefab");Assert.AreSame(source.GetComponentInChildren<MeshRenderer>().sharedMaterial,material);
   var original=Field<Mesh[]>(water,"SourceMeshes");CollectionAssert.AreEqual(original.SelectMany(m=>m.uv).ToArray(),Field<Mesh>(water,"Mesh").uv);
   var sack=Entry("ring-sack");Assert.False(Field<bool>(sack,"SemanticColors"));Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,Field<Material>(sack,"Material"));
  }
  [Test] public void HiddenAndRemovedOwnerCannotBorrowItsNeighborsApprovedBatch()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("PeatBog");f.Refresh();Exact(f,owner);owner.GetPart<RenderPart>().Visible=false;f.Refresh();Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
    owner.GetPart<RenderPart>().Visible=true;f.Refresh();Exact(f,owner);f.Zone.RemoveEntity(owner);f.Refresh();Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
   }
  }
  [Test] public void OrdinaryForeignZoneRetainsItsOriginalSourceMeshAndMaterial()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.2.5.0")){
    Assert.False(SpreadPresentationScope.IsActive(f.Zone));var owner=f.Add("VillageDoor");f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(recipe.Failure);
    var source=f.Library.FindModel(recipe.ModelId);var owned=f.View(owner);Assert.NotNull(owned);Assert.AreSame(source.GetComponent<MeshFilter>().sharedMesh,owned.GetComponentInChildren<MeshFilter>().sharedMesh);
    Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
   }
  }
 }
}
