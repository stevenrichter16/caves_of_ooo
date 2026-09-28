using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class QuestFreeSpreadArtTests
 {
  const string Prefix="questfree-spread-",Folder="QuestFreeSpread3D/";
  static GameObject Prefab(string suffix)=>Resources.Load<GameObject>(Folder+Prefix+suffix);
  static Mesh Mesh(GameObject prefab)=>prefab.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMesh??prefab.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
  [TestCase("grazer",true,1)][TestCase("grazer-remains",false,1)][TestCase("draw-full",false,2)][TestCase("draw-empty",false,1)]
  public void FourExactPersistentAssetsHaveApprovedMaterialsAndInertOwnedGeometry(string suffix,bool rigged,int slots)
  {
   var prefab=Prefab(suffix);Assert.NotNull(prefab);var mesh=Mesh(prefab);Assert.NotNull(mesh);Assert.True(mesh.isReadable);Assert.Greater(mesh.vertexCount,100);Assert.AreEqual(slots,mesh.subMeshCount);
   Assert.AreEqual(1,prefab.GetComponentsInChildren<Renderer>(true).Length);var renderer=prefab.GetComponentInChildren<Renderer>(true);Assert.AreEqual(slots,renderer.sharedMaterials.Length);
   Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,renderer.sharedMaterials[0]);if(slots==2)Assert.AreSame(PouredLiquid3DLibrary.Load().Find(PouredLiquid3DLibrary.ModelId("&c")).Material,renderer.sharedMaterials[1]);
   Assert.AreEqual(rigged,prefab.GetComponentInChildren<Animator>(true)!=null);Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
   Assert.AreEqual(Vector3.one,prefab.transform.localScale);Assert.AreEqual(Vector3.zero,prefab.transform.localPosition);
  }
  [Test]
  public void OriginalGrazerUsesRealCurrentBodyAndFiveMovingClipsWithHeadDownInteraction()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var actor=f.Add("ReedbackGrazer");Assert.AreEqual(10,actor.GetStatValue("Hitpoints"));Assert.True(actor.GetPart<BrainPart>().Passive);Assert.That(actor.GetPart<InventoryPart>().GetAllEquipped(),Is.Empty);f.Refresh();
    Assert.True(f.Find(actor,out var view,out var model));Assert.AreEqual(Prefix+"grazer",model);Assert.True(f.Rendered(actor));Assert.True(f.Pick(actor,out _));Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor,out var proof),proof.Failure);Assert.AreSame(Mesh(Prefab("grazer")),proof.ExpectedMesh);
    var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();var animator=view.GetComponentInChildren<Animator>();Assert.AreEqual(9,skin.bones.Length);Assert.True(skin.bones.All(b=>b!=null&&b.IsChildOf(view.transform)));Assert.AreEqual(skin.bones.Length,skin.sharedMesh.bindposeCount);
    var clips=animator.runtimeAnimatorController.animationClips;CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},clips.Select(c=>c.name));var baked=new Mesh();
    try{foreach(var clip in clips){clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked);var a=baked.vertices;clip.SampleAnimation(animator.gameObject,clip.length*.31f);skin.BakeMesh(baked);Assert.True(a.Where((v,i)=>(v-baked.vertices[i]).sqrMagnitude>.0000001f).Any(),clip.name);}
     var interact=clips.Single(c=>c.name=="Interact");var head=skin.bones.Single(b=>b.name=="Head");interact.SampleAnimation(animator.gameObject,0);var muzzle=skin.sharedMesh.vertices.Select((v,i)=>new{v,i}).Where(x=>skin.sharedMesh.boneWeights[x.i].boneIndex0==Array.IndexOf(skin.bones,head)).ToArray();Assert.Greater(muzzle.Length,0);skin.BakeMesh(baked);float before=muzzle.Min(x=>skin.transform.TransformPoint(baked.vertices[x.i]).y);interact.SampleAnimation(animator.gameObject,interact.length*.5f);skin.BakeMesh(baked);Assert.Less(muzzle.Min(x=>skin.transform.TransformPoint(baked.vertices[x.i]).y),before-.04f,"Actual imported feeding lowers the head in world-up, not in the rotated skin-local basis.");
     var idle=clips.Single(c=>c.name=="Idle");idle.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked);float idleBefore=muzzle.Min(x=>skin.transform.TransformPoint(baked.vertices[x.i]).y);idle.SampleAnimation(animator.gameObject,idle.length*.5f);skin.BakeMesh(baked);Assert.That(Mathf.Abs(muzzle.Min(x=>skin.transform.TransformPoint(baked.vertices[x.i]).y)-idleBefore),Is.LessThan(.02f),"Matched Idle does not perform the head-down feeding gesture.");}
    finally{Object.DestroyImmediate(baked);}
   }
  }
  [TestCase(3,"draw-full",2)][TestCase(0,"draw-empty",1)]
  public void RealFiniteDrawPointUsesExactCurrentVolumeAndFullPieceEvidence(int volume,string suffix,int pieces)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var owner=f.Add("SpreadDrawPoint");var pool=owner.GetPart<LiquidPoolPart>();Assert.AreEqual("water",pool.LiquidId);Assert.AreEqual(3,pool.Volume);pool.Volume=volume;var cell=f.Zone.GetEntityCell(owner);f.Refresh();
    Assert.True(f.Find(owner,out _,out var model));Assert.AreEqual(Prefix+suffix,model);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.AreEqual(pieces,proof.PieceCount);Assert.AreSame(Mesh(Prefab(suffix)),proof.ExpectedMesh);Assert.AreSame(cell,f.Zone.GetEntityCell(owner));Assert.AreEqual(volume,pool.Volume);
    if(pieces==2)Assert.AreSame(PouredLiquid3DLibrary.Load().Find(PouredLiquid3DLibrary.ModelId("&c")).Material,proof.GetPiece(1).ExpectedMaterial);
   }
  }
  [Test]
  public void EmptyingAndRoundTripKeepsSameRealOwnerAndEmptyArtWithoutNewWater()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var owner=f.Add("SpreadDrawPoint");string id=owner.ID;f.Refresh();Assert.True(f.Find(owner,out _,out var first));Assert.AreEqual(Prefix+"draw-full",first);owner.GetPart<LiquidPoolPart>().Volume=0;f.Refresh();Assert.True(f.Find(owner,out _,out var after));Assert.AreEqual(Prefix+"draw-empty",after);
    var loaded=f.RoundTrip();f.BindLoaded(loaded);var current=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);Assert.AreNotSame(owner,current);Assert.AreEqual(0,current.GetPart<LiquidPoolPart>().Volume);Assert.True(f.Find(current,out _,out var last));Assert.AreEqual(Prefix+"draw-empty",last);Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
   }
  }
  [Test]
  public void RealCorpseSourceUsesOriginalGroundedRemainsAndWrongSourceCannotClaimThem()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var actor=f.Add("ReedbackGrazer");var config=actor.GetPart<CorpsePart>();Assert.AreEqual(70,config.CorpseChance);Assert.AreEqual("ReedbackGrazerCorpse",config.CorpseBlueprint);
    var corpse=f.Add(config.CorpseBlueprint);corpse.Properties["SourceBlueprint"]=actor.BlueprintName;corpse.Properties["SourceID"]=actor.ID;f.Refresh();Assert.True(f.Find(corpse,out _,out var model));Assert.AreEqual(Prefix+"grazer-remains",model);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(corpse,out var proof),proof.Failure);Assert.AreSame(Mesh(Prefab("grazer-remains")),proof.ExpectedMesh);
    corpse.Properties["SourceBlueprint"]="PetDog";f.Refresh();Assert.AreNotEqual(Prefix+"grazer-remains",SpawnRing3DRecipes.Resolve(f.Zone,corpse,f.Library.Definition).ModelId);
   }
  }
  [TestCase("ReedbackGrazer")][TestCase("ReedbackGrazerCorpse")][TestCase("SpreadDrawPoint")]
  public void ForeignBiomeCannotClaimNewScopedForms(string blueprint)
  {using(var f=new SpawnRing3DIntegrationFixture()){var owner=f.Add(blueprint);var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.False(recipe.ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);}}
  [TestCase("glyph")][TestCase("color")][TestCase("visual")][TestCase("carried")][TestCase("foreign-part")]
  public void MutatedDrawOwnerDoesNotAcquireApprovedAlias(string mutation)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=f.Add("SpreadDrawPoint");var render=owner.GetPart<RenderPart>();if(mutation=="glyph")render.RenderString="?";if(mutation=="color")render.ColorString="&R";if(mutation=="visual")render.VisualID="foreign-source";if(mutation=="carried")owner.GetPart<PhysicsPart>().InInventory=f.Player;if(mutation=="foreign-part")owner.GetPart<LiquidPoolPart>().ParentEntity=f.Player;Assert.False(SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);}}
  [Test]
  public void CurrentGrazerVisibilityMovementAndRemovalStayOwned()
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var actor=f.Add("ReedbackGrazer");f.Refresh();Assert.True(f.Find(actor,out var view,out _));var free=f.FreeCell();Assert.True(f.Zone.MoveEntity(actor,free.x,free.y));f.Refresh();Assert.AreEqual(Village3DProjection.CellCentre(free.x,free.y),view.transform.position);f.Zone.GetEntityCell(actor).IsVisible=false;f.Refresh();Assert.False(f.Rendered(actor));f.Zone.GetEntityCell(actor).IsVisible=true;f.Refresh();Assert.True(f.Rendered(actor));Assert.True(f.Zone.RemoveEntity(actor));f.Refresh();Assert.False(f.Find(actor,out _,out _));}}
 }
}
