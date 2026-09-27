using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadNativeStyleValidationTests
 {
  static SpreadNativeStyleSource Source()=>JsonUtility.FromJson<SpreadNativeStyleSource>(File.ReadAllText(Path.Combine(Application.dataPath,"../ArtSource/SpreadNativeStyle3D/sources.json")));
  [Test]public void ExactReviewedSourceGraphIsCompleteWithoutDependingOnOrder(){var s=Source();s.Validate();Assert.AreEqual(868,s.models.Length);Array.Reverse(s.models);s.Validate();}
  [TestCase("null-models")][TestCase("empty")][TestCase("missing")][TestCase("extra")][TestCase("null-entry")][TestCase("duplicate")]
  [TestCase("version")][TestCase("identity")][TestCase("null-id")][TestCase("empty-id")][TestCase("traversal")][TestCase("slash")][TestCase("uppercase")]
  [TestCase("library")][TestCase("null-library")][TestCase("actor")][TestCase("kind")]
  public void MalformedSourceCannotBecomeOutputPathsOrAdoptedStaticBodies(string change)
  {
   var s=Source();s.Validate();switch(change){
    case "null-models":s.models=null;break;case "empty":s.models=Array.Empty<SpreadNativeStyleSource.Model>();break;
    case "missing":s.models=s.models.Take(s.models.Length-1).ToArray();break;case "extra":s.models=s.models.Concat(new[]{s.models[0]}).ToArray();break;
    case "null-entry":s.models[0]=null;break;case "duplicate":s.models[1].id=s.models[0].id;break;case "version":s.schemaVersion=2;break;case "identity":s.id="other";break;
    case "null-id":s.models[0].id=null;break;case "empty-id":s.models[0].id="";break;case "traversal":s.models[0].id="../ring-sack";break;case "slash":s.models[0].id="ring/sack";break;case "uppercase":s.models[0].id="Ring-sack";break;
    case "library":s.models[0].sourceLibrary="Unknown";break;case "null-library":s.models[0].sourceLibrary=null;break;case "actor":s.models[0].kind="actor";break;case "kind":s.models[0].kind="invisible";break;}
   Assert.Throws<InvalidOperationException>(()=>s.Validate());
  }
  [TestCase("takeable")][TestCase("carried")][TestCase("equipped")][TestCase("creature")][TestCase("part-backlink")][TestCase("removed")][TestCase("foreign-scope")]
  public void StaticSourceSelectionRechecksTheCurrentOwnedGraph(string change)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var library=SpreadNativeStyle3DLibrary.Load();Assert.NotNull(library);var owner=f.Add("Sack");var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(recipe.Failure);Assert.NotNull(library.ForOwner(f.Zone,recipe));
    var physics=owner.GetPart<PhysicsPart>();switch(change){case "takeable":physics.Takeable=true;break;case "carried":physics.InInventory=f.Player;break;case "equipped":physics.Equipped=f.Player;break;case "creature":owner.Tags["Creature"]="";break;case "part-backlink":physics.ParentEntity=f.Player;break;case "removed":f.Zone.RemoveEntity(owner);break;case "foreign-scope":f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;break;}
    Assert.IsNull(library.ForOwner(f.Zone,recipe));
   }
  }
  [TestCase("source-mesh")][TestCase("source-transform")][TestCase("source-prefab")][TestCase("semantic-policy")]
  public void ExactSourceProvenanceIsNotSelfAttestedByCopiedMetadata(string change)
  {
   var original=SpreadNativeStyle3DLibrary.Load();Assert.NotNull(original);original.Validate();var copy=UnityEngine.Object.Instantiate(original);
   try{
    Assert.AreNotSame(original.Entries,copy.Entries);var entry=copy.Entries.Single(e=>e.Id=="ring-sack");Assert.AreNotSame(original.Find("ring-sack"),entry);var other=original.Find("sumphold-water-0");
    if(change=="source-mesh")entry.SourceMeshes[0]=other.SourceMeshes[0];else if(change=="source-transform")entry.SourceTransforms[0]=Matrix4x4.Translate(Vector3.right);
    else if(change=="source-prefab")entry.SourcePrefab=other.SourcePrefab;else entry.SemanticColors=!entry.SemanticColors;
    Assert.Throws<InvalidOperationException>(()=>copy.Validate());original.Validate();
   }finally{UnityEngine.Object.DestroyImmediate(copy);}
  }
  [Test]public void CarriedThenRestoredGraphInvalidatesItsBatchEvenWithEmptyDirtyHint()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    var owner=f.Add("Sack");var at=f.Zone.GetEntityPosition(owner);f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.True(presenter.TryGetApprovedStyle(owner,out _));int revision=f.Revision(at.x,at.y);
    owner.GetPart<PhysicsPart>().InInventory=f.Player;f.Refresh(new System.Collections.Generic.HashSet<int>());Assert.False(presenter.TryGetApprovedStyle(owner,out _));Assert.Greater(f.Revision(at.x,at.y),revision);revision=f.Revision(at.x,at.y);
    owner.GetPart<PhysicsPart>().InInventory=null;f.Refresh(new System.Collections.Generic.HashSet<int>());Assert.True(presenter.TryGetApprovedStyle(owner,out _));Assert.Greater(f.Revision(at.x,at.y),revision);
   }
  }
 }
}
