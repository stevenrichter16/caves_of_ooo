using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCollectorArtTests
 {
  const string Live="spread-tatterjay",Remains="spread-tatterjay-remains",Folder="SpreadCollector3D/";
  static GameObject Prefab(string id)=>Resources.Load<GameObject>(Folder+id);
  static Mesh MeshOf(GameObject p)=>p.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMesh??p.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
  [TestCase(Live,true)][TestCase(Remains,false)]
  public void ExactOptionalOriginalAssetsUseApprovedPaletteAndInertGeometry(string id,bool rigged)
  {
   var p=Prefab(id);Assert.NotNull(p);var mesh=MeshOf(p);Assert.NotNull(mesh);Assert.True(mesh.isReadable);Assert.Greater(mesh.vertexCount,100);Assert.AreEqual(1,mesh.subMeshCount);
   Assert.AreEqual(1,p.GetComponentsInChildren<Renderer>(true).Length);var r=p.GetComponentInChildren<Renderer>(true);Assert.AreEqual(1,r.sharedMaterials.Length);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,r.sharedMaterial);
   Assert.AreEqual(rigged,p.GetComponentInChildren<Animator>(true)!=null);Assert.That(p.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
   Assert.AreEqual(Vector3.one,p.transform.localScale);Assert.AreEqual(Vector3.zero,p.transform.localPosition);Assert.AreEqual(Quaternion.identity,p.transform.localRotation);
  }
  [Test]
  public void ActualOriginalAvianOwnerHasExactBodyPalettePickingAndNineMovingClips()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var owner=f.Add("Tatterjay");Assert.NotNull(owner);Assert.AreEqual(8,owner.GetStatValue("Hitpoints"));Assert.True(owner.GetPart<BrainPart>().Passive);Assert.NotNull(owner.GetPart<Body>());Assert.That(owner.GetPart<InventoryPart>().GetAllEquipped(),Is.Empty);f.Refresh();
    Assert.True(f.Find(owner,out var view,out string id));Assert.AreEqual(Live,id);Assert.True(f.Rendered(owner));Assert.True(f.Pick(owner,out _));Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.AreSame(MeshOf(Prefab(Live)),proof.ExpectedMesh);
    var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();var animator=view.GetComponentInChildren<Animator>();Assert.AreEqual(8,skin.bones.Length);Assert.True(skin.bones.All(b=>b!=null&&b.IsChildOf(view.transform)));Assert.AreEqual(8,skin.sharedMesh.bindposeCount);
    CollectionAssert.AreEquivalent(new[]{"Root","Body","Head","Wing.L","Wing.R","Leg.L","Leg.R","Tail"},skin.bones.Select(b=>b.name));
    var clips=animator.runtimeAnimatorController.animationClips;CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit","CarryIdle","CarryWalk","Pickup","Deposit"},clips.Select(c=>c.name));
    var source=skin.sharedMesh.vertices;var baked=new Mesh();try{foreach(var clip in clips){clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked);var before=baked.vertices;clip.SampleAnimation(animator.gameObject,clip.length*.31f);skin.BakeMesh(baked);Assert.True(before.Where((v,i)=>(v-baked.vertices[i]).sqrMagnitude>.0000001f).Any(),clip.name);}CollectionAssert.AreEqual(source,skin.sharedMesh.vertices);}finally{Object.DestroyImmediate(baked);}
   }
  }
  [Test]
  public void HeadOwnedBillSocketFollowsDistinctPickupDepositAndCarryPoses()
  {
   var p=Prefab(Live);Assert.NotNull(p);var root=Object.Instantiate(p);try
   {
    var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var animator=root.GetComponentInChildren<Animator>();var head=skin.bones.Single(b=>b.name=="Head");var socket=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Collector.Bill");Assert.True(socket.IsChildOf(head));
    var clips=animator.runtimeAnimatorController.animationClips;Action<string,float> sample=(name,time)=>{var c=clips.Single(x=>x.name==name);c.SampleAnimation(animator.gameObject,c.length*time);};
    sample("Idle",0);Vector3 before=socket.position;sample("Pickup",.5f);Vector3 pickup=socket.position;Assert.Greater((pickup-before).magnitude,.035f);sample("Deposit",.5f);Assert.Greater((socket.position-before).magnitude,.025f);Assert.Greater((socket.position-pickup).magnitude,.005f);
    sample("CarryIdle",0);Assert.Greater((socket.position-before).magnitude,.005f);sample("Idle",0);Assert.That((socket.position-before).magnitude,Is.LessThan(.00001f));
    var mesh=skin.sharedMesh;var baked=new Mesh();try{skin.BakeMesh(baked);int headIndex=Array.IndexOf(skin.bones,head);var weights=mesh.boneWeights;float nearest=baked.vertices.Select((v,i)=>new{v,i}).Where(x=>weights[x.i].boneIndex0==headIndex).Min(x=>Vector3.Distance(skin.transform.TransformPoint(x.v),socket.position));Assert.Less(nearest,.10f,"Socket remains at actual bill geometry, not a floating generic attachment.");}finally{Object.DestroyImmediate(baked);}
   }finally{Object.DestroyImmediate(root);}
  }
  [Test]
  public void ExactCorpseMetadataSelectsOriginalLowAvianRemains()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var actor=f.Add("Tatterjay");Assert.AreEqual("TatterjayCorpse",actor.GetPart<CorpsePart>().CorpseBlueprint);var corpse=f.Add("TatterjayCorpse");corpse.Properties["SourceBlueprint"]=actor.BlueprintName;corpse.Properties["SourceID"]=actor.ID;f.Refresh();
    Assert.True(f.Find(corpse,out _,out var id));Assert.AreEqual(Remains,id);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(corpse,out var proof),proof.Failure);Assert.AreSame(MeshOf(Prefab(Remains)),proof.ExpectedMesh);Assert.Less(proof.ExpectedMesh.bounds.size.y,.25f);
    corpse.Properties["SourceBlueprint"]="Magpie";f.Refresh();Assert.AreNotEqual(Remains,SpawnRing3DRecipes.Resolve(f.Zone,corpse,f.Library.Definition).ModelId);
   }
  }
  [TestCase("Tatterjay")][TestCase("TatterjayCorpse")]
  public void ForeignBiomeDoesNotAcquireNewCollectorAliases(string blueprint)
  {using(var f=new SpawnRing3DIntegrationFixture()){var e=f.Add(blueprint);Assert.NotNull(e);Assert.False(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId?.StartsWith(Live,StringComparison.Ordinal)==true);}}
  [TestCase("glyph")][TestCase("color")][TestCase("visual")][TestCase("hidden")][TestCase("carried")][TestCase("foreign-brain")]
  public void MalformedOrHiddenCollectorCannotAcquireOriginalBody(string change)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var e=f.Add("Tatterjay");Assert.NotNull(e);var r=e.GetPart<RenderPart>();if(change=="glyph")r.RenderString="b";if(change=="color")r.ColorString="&K";if(change=="visual")r.VisualID="unrelated-source";if(change=="hidden")r.Visible=false;if(change=="carried")e.GetPart<PhysicsPart>().InInventory=f.Player;if(change=="foreign-brain")e.GetPart<BrainPart>().ParentEntity=f.Player;Assert.AreNotEqual(Live,SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}}
  [Test]
  public void ExistingMagpieKeepsItsOriginalExactSource()
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var e=f.Add("Magpie");f.Refresh();Assert.True(f.Find(e,out _,out var id));Assert.AreEqual("spread-biome-magpie",id);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(e,out var proof),proof.Failure);Assert.AreSame(SpreadBiomeActorLibrary.Load().Find("spread-biome-magpie").Mesh,proof.ExpectedMesh);}}
  [Test]
  public void CurrentVisibilityMovementRemovalAndReplacementSaveGraphStayOwned()
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var e=f.Add("Tatterjay");string id=e.ID;f.Refresh();Assert.True(f.Find(e,out var view,out _));var free=f.FreeCell();Assert.True(f.Zone.MoveEntity(e,free.x,free.y));f.Refresh();Assert.AreEqual(Village3DProjection.CellCentre(free.x,free.y),view.transform.position);f.Zone.GetEntityCell(e).IsVisible=false;f.Refresh();Assert.False(f.Rendered(e));f.Zone.GetEntityCell(e).IsVisible=true;f.Refresh();Assert.True(f.Rendered(e));var loaded=f.RoundTrip();f.BindLoaded(loaded);var now=f.Zone.GetReadOnlyEntities().Single(x=>x.ID==id);Assert.AreNotSame(e,now);Assert.True(f.Find(now,out _,out var model));Assert.AreEqual(Live,model);Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(e,out _));Assert.True(f.Zone.RemoveEntity(now));f.Refresh();Assert.False(f.Find(now,out _,out _));}}
 }
}
