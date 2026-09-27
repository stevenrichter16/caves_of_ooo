using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCommonTerrainTests
 {
  static SpawnRing3DPresenter Presenter(SpawnRing3DIntegrationFixture f)=>(SpawnRing3DPresenter)f.Presenter;
  static void Approved(SpawnRing3DIntegrationFixture f,Entity e,string family)
  {
   var recipe=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);Assert.IsNull(recipe.Failure);Assert.That(recipe.ModelId,Does.StartWith("reference-glade-"+family+"-"));
   Assert.True(Presenter(f).TryGetApprovedStyle(e,out var proof),proof.Failure);Assert.True(proof.Batched);var expected=ReferenceGladeVoxelLibrary.Load().Find(recipe.ModelId);Assert.NotNull(expected);
   Assert.AreSame(expected.Mesh,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);Assert.NotNull(proof.SubmittedMesh);
  }
  [TestCase("Overworld.12.10.0")][TestCase("Overworld.10.10.0")][TestCase("Overworld.4.9.0")]
  public void RealGeneratedGrassUsesApprovedGroundAcrossWildernessAndPoi(string address)
  {
   using(var f=new SpawnRing3DIntegrationFixture(address))
   {var grass=f.Zone.GetReadOnlyEntities().FirstOrDefault(e=>e.BlueprintName=="Grass");Assert.NotNull(grass,"Require real generated grass, not a substitute terrain source.");
    int version=f.Zone.EntityVersion;string tiles=f.Zone.TileState.ToSaveString();var before=f.Zone.GetEntityPosition(grass);string visual=grass.GetPart<RenderPart>().VisualID;
    Approved(f,grass,"ground");var material=f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);var mask=material.GetTexture("_GroundContact")as Texture2D;Assert.NotNull(mask);Assert.Greater(material.GetFloat("_GroundContactStrength"),0,"The actual generated chunk must fit existing contact budgets.");Assert.Greater(Maximum(mask),.01f);f.Refresh();Assert.AreSame(mask,material.GetTexture("_GroundContact"));Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());Assert.AreEqual(before,f.Zone.GetEntityPosition(grass));Assert.AreEqual(visual,grass.GetPart<RenderPart>().VisualID);}
  }
  [TestCase("Bush","green-grass")][TestCase("Reeds","pale-reeds")][TestCase("Wall","low-wall")]
  [TestCase("Chest","chest")][TestCase("WoodenBarrel","barrel")][TestCase("MushroomRing","mushroom-ring")]
  public void ExistingNativeFamiliesKeepTheirOwnerWithDistinctApprovedForms(string bp,string family)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var e=f.Add(bp);var physics=e.GetPart<PhysicsPart>();bool solid=physics?.Solid==true;string id=e.ID;f.Refresh();Approved(f,e,family);Assert.AreEqual(id,e.ID);Assert.AreEqual(solid,physics?.Solid==true);}}
  [TestCase("visual")][TestCase("hidden")][TestCase("foreign")]
  public void CustomHiddenAndForeignOwnersCannotAcquireTheScopedAlias(string state)
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var e=f.Add("Bush");var render=e.GetPart<RenderPart>();
    if(state=="visual")render.VisualID="unrelated-custom-owner";
    else if(state=="hidden")render.Visible=false;
    else f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;
    f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
    Assert.False(recipe.ModelId?.StartsWith("reference-glade-",StringComparison.Ordinal)==true,"Custom identity must never acquire the common-form alias.");
    if(state!="visual"){Assert.False(Presenter(f).TryGetApprovedStyle(e,out _));return;}

    // Common-form replacement refuses custom identity. The later palette-only
    // adoption still preserves a successful native source in the receiving biome.
    var method=typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.Static|BindingFlags.NonPublic);Assert.NotNull(method);
    var native=(SpawnRing3DRecipe)method.Invoke(null,new object[]{f.Zone,e,f.Library.Definition,null});
    Assert.IsNull(native.Failure);Assert.NotNull(native.ModelId);Assert.AreEqual(native,recipe);
    Assert.AreEqual("unrelated-custom-owner",render.VisualID);Assert.AreSame(e,recipe.Owner);
    var entry=SpreadNativeStyle3DLibrary.Load().ForOwner(f.Zone,recipe);Assert.NotNull(entry);
    Assert.AreSame(f.Library.FindModel(native.ModelId),entry.SourcePrefab);
    Assert.True(Presenter(f).TryGetApprovedStyle(e,out var proof),proof.Failure);
    Assert.AreEqual(native.ModelId,proof.ModelId);Assert.AreSame(entry.Mesh,proof.ExpectedMesh);
    Assert.AreSame(entry.Material,proof.ExpectedMaterial);Assert.True(proof.Batched);Assert.Greater(proof.SubmittedMesh.vertexCount,0);
    render.VisualID=null;f.Refresh();Approved(f,e,"green-grass");
   }
  }
  [Test]public void ExistingAuthoredGladeWallStateAndNativeDoorIdentityRemainAuthoritative()
  {
   using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
   {var wall=f.Zone.GetReadOnlyEntities().First(e=>e.BlueprintName=="Wall"&&e.GetPart<RenderPart>().VisualID=="reference-glade-lit-wall");var before=SpawnRing3DRecipes.Resolve(f.Zone,wall,f.Library.Definition);f.Refresh();var after=SpawnRing3DRecipes.Resolve(f.Zone,wall,f.Library.Definition);Assert.AreEqual(before.ModelId,after.ModelId);Assert.AreEqual(before.QuarterTurns,after.QuarterTurns);Assert.That(after.ModelId,Does.StartWith("reference-glade-lit-wall-"));}
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {var door=f.Add("VillageDoor");door.GetPart<DoorPart>().IsOpen=false;door.GetPart<RenderPart>().RenderString="+";f.Refresh();Assert.That(SpawnRing3DRecipes.Resolve(f.Zone,door,f.Library.Definition).ModelId,Does.StartWith("stillleaf-door-"));door.GetPart<DoorPart>().IsOpen=true;door.GetPart<RenderPart>().RenderString="/";f.Refresh();Assert.That(SpawnRing3DRecipes.Resolve(f.Zone,door,f.Library.Definition).ModelId,Does.StartWith("stillleaf-open-door-"));}
  }
  [Test]public void NativeQuestRefusalIsNotRescuedByCommonStyle()
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var token=new Entity{BlueprintName="CrunchyLocket"};token.SetTag("Item");token.AddPart(new PhysicsPart{Takeable=true});token.AddPart(new RenderPart{RenderString="*",ColorString="&Y"});token.AddPart(new CompleteObjectiveOnTaken{Quest="CrunchyLocket",Objective="wrong-objective"});Assert.True(f.Zone.AddEntity(token,20,10));f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,token,f.Library.Definition);Assert.IsNull(recipe.ModelId);Assert.IsNotNull(recipe.Failure);Assert.False(Presenter(f).TryGetApprovedStyle(token,out _));}}
  static float Maximum(Texture2D t)=>t.GetPixels32().Max(c=>c.r)/255f;
  [Test]public void ScopedContactFollowsCurrentVisiblePlantAndReleasesOnAuthorityLoss()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {foreach(var e in f.Zone.GetReadOnlyEntities().ToArray())if(e!=f.Player)f.Zone.RemoveEntity(e);var plant=f.Add("Reeds",20,10);f.Reveal();f.Refresh();
    var surface=f.Get<NativeZone3DRenderSurface>("ActiveSurface");var material=surface.MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);var mask=material.GetTexture("_GroundContact")as Texture2D;Assert.NotNull(mask);Assert.AreEqual(640,mask.width);Assert.AreEqual(200,mask.height);Assert.Greater(Maximum(mask),.3f);Assert.Greater(material.GetFloat("_GroundContactStrength"),0);Assert.AreEqual(.24f,material.GetFloat("_GroundMottleStrength"),.0001f);
    f.Zone.GetEntityCell(plant).IsVisible=false;f.Refresh();Assert.AreSame(mask,material.GetTexture("_GroundContact"));Assert.Less(Maximum(mask),.001f);f.Zone.GetEntityCell(plant).IsVisible=true;f.Refresh();Assert.Greater(Maximum(mask),.3f);
    Village3DSettings.LowDetail=true;f.Frame();Assert.AreEqual(0,material.GetFloat("_GroundContactStrength"));Village3DSettings.LowDetail=false;f.Frame();Assert.Greater(material.GetFloat("_GroundContactStrength"),0);
    f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;f.Refresh();Assert.True(mask==null);Assert.True(material==null);Assert.AreEqual(0,ReferenceGladeVoxelLibrary.Load().Material.GetFloat("_GroundContactStrength"));}
  }
 }
}
