using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadBiomeStyleEvidenceTests
 {
  static bool Audit(SpawnRing3DIntegrationFixture f,Entity owner,out object evidence)
  {var method=f.Presenter.GetType().GetMethod("TryGetApprovedStyle",BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(method,"Actual adopted-style evidence is required in addition to being drawn.");var args=new object[]{owner,null};bool result=(bool)method.Invoke(f.Presenter,args);evidence=args[1];return result;}
  static T Field<T>(object value,string name){Assert.NotNull(value);var field=value.GetType().GetField(name);Assert.NotNull(field,name);return(T)field.GetValue(value);}
  [TestCase("Player")][TestCase("Magpie")]
  public void ActualApprovedBodyReportsExactPersistentSourceAndOwnedPalette(string bp)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {var e=bp=="Player"?f.Player:f.Add(bp);f.Refresh();Assert.True(Audit(f,e,out var proof));var root=f.View(e);var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
    Assert.AreSame(skin.sharedMesh,Field<Mesh>(proof,"ExpectedMesh"));Assert.AreSame(skin.sharedMesh,Field<Mesh>(proof,"SubmittedMesh"));Assert.AreSame(skin.sharedMaterial,Field<Material>(proof,"SubmittedMaterial"));
    Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,Field<Material>(proof,"ExpectedMaterial"));Assert.IsNull(Field<string>(proof,"Failure"));}
  }
  [Test]
  public void CopiedOrSubstitutedBodyCannotPassExactMeshIdentity()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {Assert.True(Audit(f,f.Player,out _));var skin=f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>();var original=skin.sharedMesh;var copy=Object.Instantiate(original);
    try{skin.sharedMesh=copy;Assert.False(Audit(f,f.Player,out var proof));Assert.AreEqual("submitted-mesh-mismatch",Field<string>(proof,"Failure"));skin.sharedMesh=original;Assert.True(Audit(f,f.Player,out _));}
    finally{skin.sharedMesh=original;Object.DestroyImmediate(copy);}}
  }
  [Test]
  public void OwnedPaletteMutationFailsWithoutMutatingItsBorrowedSource()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {Assert.True(Audit(f,f.Player,out _));var skin=f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>();var material=skin.sharedMaterial;var expected=ReferenceGladeVoxelLibrary.Load().Material;var before=material.GetColor("_BaseColor");var borrowed=expected.GetColor("_BaseColor");
    try{material.SetColor("_BaseColor",Color.magenta);Assert.False(Audit(f,f.Player,out var proof));Assert.AreEqual("submitted-palette-mismatch",Field<string>(proof,"Failure"));Assert.AreEqual(borrowed,expected.GetColor("_BaseColor"));}
    finally{material.SetColor("_BaseColor",before);}Assert.True(Audit(f,f.Player,out _));}
  }
  [TestCase("disabled")][TestCase("forced-off")][TestCase("inactive-child")]
  public void ExactBodyMeshAndPaletteStillRequireAnActuallySubmittedRenderer(string state)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {Assert.True(Audit(f,f.Player,out _));var root=f.View(f.Player);var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();Assert.AreNotSame(root,skin.gameObject,"Test must isolate actual child rendering from logical root visibility.");
    try{if(state=="disabled")skin.enabled=false;else if(state=="forced-off")skin.forceRenderingOff=true;else skin.gameObject.SetActive(false);
     Assert.False(Audit(f,f.Player,out var proof));Assert.AreEqual("submitted-body-not-drawn",Field<string>(proof,"Failure"));}
    finally{skin.enabled=true;skin.forceRenderingOff=false;skin.gameObject.SetActive(true);}Assert.True(Audit(f,f.Player,out _));}
  }
  [TestCase(false)][TestCase(true)]
  public void PropertyBlockPaletteOverrideCannotMasqueradeAsApprovedMaterial(bool indexed)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {Assert.True(Audit(f,f.Player,out _));var skin=f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>();var before=new MaterialPropertyBlock();if(indexed)skin.GetPropertyBlock(before,0);else skin.GetPropertyBlock(before);
    var altered=new MaterialPropertyBlock();if(indexed)skin.GetPropertyBlock(altered,0);else skin.GetPropertyBlock(altered);altered.SetColor("_BaseColor",Color.magenta);
    try{if(indexed)skin.SetPropertyBlock(altered,0);else skin.SetPropertyBlock(altered);Assert.False(Audit(f,f.Player,out var proof));Assert.AreEqual("submitted-palette-mismatch",Field<string>(proof,"Failure"));}
    finally{if(indexed)skin.SetPropertyBlock(before,0);else skin.SetPropertyBlock(before);}Assert.True(Audit(f,f.Player,out _));}
  }
  [Test]
  public void ActualBatchedOwnerNeedsItsOwnCommittedContribution()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
   {var e=f.Add("Reeds");e.GetPart<RenderPart>().VisualID="reference-glade-pale-reeds";f.Refresh();Assert.True(Audit(f,e,out var proof));Assert.True(Field<bool>(proof,"Batched"));Assert.NotNull(Field<Mesh>(proof,"ExpectedMesh"));Assert.NotNull(Field<Mesh>(proof,"SubmittedMesh"));
    f.Zone.RemoveEntity(e);Assert.False(Audit(f,e,out _),"A live neighboring patch cannot stand in for this removed owner, even before refresh.");f.Refresh();Assert.False(Audit(f,e,out _));}
  }
  [Test]
  public void LossOfReceivingAuthorityRefusesBeforeRefreshAndRecoversAfterRebind()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {Assert.True(Audit(f,f.Player,out _));f.Manager.WorldMap.Tiles[12,10]=BiomeType.Beating;Assert.False(Audit(f,f.Player,out _));f.Refresh();Assert.False(Audit(f,f.Player,out _));f.Manager.WorldMap.Tiles[12,10]=BiomeType.Spread;f.Refresh();Assert.True(Audit(f,f.Player,out _));}
  }
  [Test]
  public void HiddenOrRemovedActorCannotClaimStillAllocatedBody()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {var e=f.Add("Magpie");f.Refresh();Assert.True(Audit(f,e,out _));e.GetPart<RenderPart>().Visible=false;Assert.False(Audit(f,e,out _));e.GetPart<RenderPart>().Visible=true;f.Refresh();Assert.True(Audit(f,e,out _));f.Zone.RemoveEntity(e);Assert.False(Audit(f,e,out _));}
  }
 }
}
