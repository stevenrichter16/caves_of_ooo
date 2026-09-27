using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadBiomeHumanoidArtTests
 {
  [TestCase("Farmer","Overworld.12.10.0")][TestCase("Elder","Overworld.10.10.0")]
  [TestCase("Scribe","Overworld.15.6.0")][TestCase("PeatCutter","Overworld.15.6.0")]
  [TestCase("Armorer","Overworld.10.10.0")][TestCase("GantryRegistrar","Overworld.12.10.0")]
  [TestCase("VillageChild","Overworld.12.10.0")][TestCase("SootGremlin","Overworld.12.10.0")]
  public void CurrentNativeRoleGetsActualApprovedRiggedBody(string bp,string zone)
  {
   using(var f=new SpawnRing3DIntegrationFixture(zone))
   {
    var e=f.Add(bp);var before=f.Zone.GetEntityPosition(e);f.Refresh();
    var recipe=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
    Assert.That(recipe.ModelId,Does.StartWith("spread-person-"));Assert.True(recipe.Transient);Assert.False(recipe.Batched);
    Assert.True(f.Find(e,out var root,out var id));Assert.AreEqual(recipe.ModelId,id);Assert.True(f.Rendered(e));Assert.True(f.Pick(e,out _));
    var source=Resources.Load<GameObject>("SpreadBiome3D/Humanoids/"+id);Assert.NotNull(source);
    var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();Assert.NotNull(skin);
    Assert.AreSame(source.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh,skin.sharedMesh);
    var animator=root.GetComponentInChildren<Animator>();Assert.NotNull(animator);Assert.NotNull(animator.avatar);Assert.True(animator.avatar.isValid);
    CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},animator.runtimeAnimatorController.animationClips.Select(c=>c.name));
    var mesh=skin.sharedMesh;var size=root.transform.worldToLocalMatrix.MultiplyVector(skin.localToWorldMatrix.MultiplyVector(mesh.bounds.size));
    Assert.Greater(Mathf.Abs(size.y)*root.transform.localScale.y,bp=="VillageChild"||bp=="SootGremlin"?.70f:1.05f);
    Assert.AreEqual(before,f.Zone.GetEntityPosition(e));Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
   }
  }
  [TestCase("Farmer","glyph")][TestCase("Farmer","portable")][TestCase("Scribe","glyph")][TestCase("VillageChild","hidden")]
  public void MalformedCurrentOwnerDoesNotGetRoleBody(string bp,string mutation)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {var e=f.Add(bp);if(mutation=="glyph")e.GetPart<RenderPart>().RenderString="?";else if(mutation=="portable")e.GetPart<PhysicsPart>().Takeable=true;else e.GetPart<RenderPart>().Visible=false;
    Assert.False(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId?.StartsWith("spread-person-",StringComparison.Ordinal)==true);}
  }
  [Test]
  public void SuccessfulRegionalQuestBodyIsAnimatedWithoutRescuingItsRefusedCounterparts()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.15.6.0"))
   {
    var e=f.Add("DirtGnome");e.AddPart(new AddFactWhenSlain{Fact="warren_gnomes_routed",Amount=1});
    Assert.That(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId,Does.StartWith("spread-person-"));
    e.GetPart<AddFactWhenSlain>().Amount=2;Assert.IsNull(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);
    e.GetPart<AddFactWhenSlain>().Amount=1;e.GetPart<AddFactWhenSlain>().Fact="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);
   }
  }
  [Test]
  public void ForeignTownKeepsItsActualExistingStaticRoleContract()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.6.6.0"))
   {var e=f.Add("Scribe");var r=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);Assert.NotNull(r.ModelId);Assert.False(r.ModelId.StartsWith("spread-person-",StringComparison.Ordinal));}
  }
  [Test]
  public void RoleMeshUsesRealNativeBoneMotionAndDoesNotEditBorrowedSource()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var basePrefab=f.Library.FindModel("ring-nam");var baseSkin=basePrefab.GetComponentInChildren<SkinnedMeshRenderer>();var borrowed=baseSkin.sharedMesh;
    var poses=borrowed.bindposes;var e=f.Add("Farmer");f.Refresh();Assert.True(f.Find(e,out var root,out var id));Assert.That(id,Does.StartWith("spread-person-"));
    var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var anim=root.GetComponentInChildren<Animator>();var mesh=skin.sharedMesh;var verts=mesh.vertices;var uv=mesh.uv;var weights=mesh.boneWeights;Mesh baked=new Mesh();
    try{foreach(var clip in anim.runtimeAnimatorController.animationClips){clip.SampleAnimation(anim.gameObject,0);skin.BakeMesh(baked);var first=baked.vertices;clip.SampleAnimation(anim.gameObject,clip.length*.31f);skin.BakeMesh(baked);Assert.True(first.Where((v,i)=>(v-baked.vertices[i]).sqrMagnitude>1e-7f).Any(),clip.name);}}
    finally{Object.DestroyImmediate(baked);}
    CollectionAssert.AreEqual(poses,borrowed.bindposes);CollectionAssert.AreEqual(verts,mesh.vertices);CollectionAssert.AreEqual(uv,mesh.uv);CollectionAssert.AreEqual(weights,mesh.boneWeights);
    var cell=f.Zone.GetEntityCell(e);cell.IsVisible=false;f.Refresh();Assert.False(f.Rendered(e));cell.IsVisible=true;f.Refresh();Assert.True(f.Rendered(e));f.Zone.RemoveEntity(e);f.Refresh();Assert.False(f.Find(e,out _,out _));
   }
  }

  [Test]
  public void ActualChildBodyStaysSmallerThanAdultAfterNativeNormalization()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var child=f.Add("VillageChild");var adult=f.Add("Farmer");f.Refresh();
    Assert.True(f.Find(child,out var cr,out var ci));Assert.True(f.Find(adult,out var ar,out var ai));
    Assert.That(ci,Does.StartWith("spread-person-"));Assert.That(ai,Does.StartWith("spread-person-"));
    float ch=VisibleHeight(cr),ah=VisibleHeight(ar);
    Assert.Greater(ah,1.05f);Assert.That(ch/ah,Is.InRange(.60f,.90f),"Actual authored vertex bounds, not the imported culling envelope or prefab scale alone.");
   }
  }
  private static float VisibleHeight(GameObject root)
  {
   var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();Assert.NotNull(skin);var vertices=skin.sharedMesh.vertices;
   float low=float.PositiveInfinity,high=float.NegativeInfinity;
   foreach(var vertex in vertices){float y=skin.transform.TransformPoint(vertex).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}return high-low;
  }
 }
}
