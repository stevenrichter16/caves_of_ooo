using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class BiomeCropArtTests
 {
  const string Prefix="biome-crop-";
  public static readonly string[] Groups={"Claspbean Pitchpod Drawgourd Wickrush Marlroot","Sumpsieve Drowsebell Chillcress Slipsedge Peatlantern","Sunbladder Shalebean Shadefan Cinderpea Spurgrass","Choirwick Knitmoss Sourmantle Murmurpod Sealbark","Absentmint Margincress Binderroot Greybladder Hollowchime","Raingourd Prismreed ScarletSundew Cloudwick Gripfrond","Lampvein Knucklecap Sootroot Veilpuff Brinebutton"};
  static string Stem(string name)=>name=="ScarletSundew"?"scarlet-sundew":name.ToLowerInvariant();
  static void Stage(Entity e,int stage,int moisture)
  {var c=e.GetPart<CropPart>();Assert.NotNull(c);c.GrowthStage=stage;c.MoistureTicks=moisture;var r=e.GetPart<RenderPart>();r.RenderString=c.GlyphForStage(stage).ToString();r.ColorString=c.ColorForStage(stage);}
  static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f,Entity e)=>SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
  static Mesh Exact(SpawnRing3DIntegrationFixture f,Entity e,string model)
  {
   var r=Recipe(f,e);Assert.AreSame(e,r.Owner);Assert.Null(r.Failure);Assert.AreEqual(model,r.ModelId);f.Refresh(f.Dirty(e));Assert.True(f.Rendered(e),model);
   var prefab=Resources.Load<GameObject>("BiomeCrops3D/"+model);Assert.NotNull(prefab,model);var mesh=prefab.GetComponent<MeshFilter>().sharedMesh;Assert.Greater(mesh.vertexCount,24);Assert.GreaterOrEqual(mesh.uv.Distinct().Count(),2);
   Assert.AreSame(mesh,f.Library.FindModel(model).GetComponent<MeshFilter>().sharedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,prefab.GetComponent<MeshRenderer>().sharedMaterial);Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));return mesh;
  }
  [TestCaseSource(nameof(Groups))]
  public void EachRegionalSetHasThreeRealStagesAndWetDryStateInAnotherBiome(string names)
  {
   using(var f=new SpawnRing3DIntegrationFixture())foreach(string name in names.Split(' '))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(name+"Crop"),name);var e=f.Add(name+"Crop");var before=f.Zone.GetEntityCell(e).Objects.Where(x=>x!=e).ToArray();Mesh previous=null;
    for(int stage=0;stage<3;stage++){Stage(e,stage,0);var dry=Exact(f,e,Prefix+Stem(name)+"-"+stage+"-dry");Stage(e,stage,1);var wet=Exact(f,e,Prefix+Stem(name)+"-"+stage+"-wet");CollectionAssert.AreEqual(dry.vertices,wet.vertices);Assert.False(dry.uv.SequenceEqual(wet.uv));if(previous!=null)Assert.False(previous.vertices.SequenceEqual(dry.vertices));previous=dry;}
    var cell=f.Zone.GetEntityCell(e);Assert.True(f.Zone.RemoveEntity(e));f.Refresh();Assert.False(f.Rendered(e));Assert.Null(Recipe(f,e).ModelId);foreach(var original in before)Assert.Contains(original,cell.Objects);
   }
  }
  [TestCaseSource(nameof(Groups))]
  public void SeedsAndUsefulHarvestsHavePortableFormsWithRealPickupDrop(string names)
  {
   using(var f=new SpawnRing3DIntegrationFixture())foreach(string name in names.Split(' '))
   {
    var blueprint=f.Factory.CreateEntity(name+"Crop");Assert.NotNull(blueprint);var crop=blueprint.GetPart<CropPart>();Assert.NotNull(crop);
    foreach(var pair in new[]{new[]{crop.SeedYieldBlueprint,"seed"},new[]{crop.YieldBlueprint,"harvest"}})
    {var e=f.Add(pair[0]);var mesh=Exact(f,e,Prefix+Stem(name)+"-"+pair[1]);f.Approach(e);Assert.True(InventorySystem.Pickup(f.Player,e,f.Zone));f.Refresh();Assert.False(f.Rendered(e));Assert.Null(Recipe(f,e).ModelId);Assert.True(InventorySystem.Drop(f.Player,e,f.Zone));Assert.AreSame(mesh,Exact(f,e,Prefix+Stem(name)+"-"+pair[1]));Assert.True(f.Zone.RemoveEntity(e));}
   }
  }
  [TestCaseSource(nameof(Groups))]
  public void EverySpeciesCanBePlantedInVillageWithoutReplacingAuthoredTownModels(string names)
  {
   using(var f=new Village3DIntegrationFixture())
   {
    var town=f.View("central-cistern");foreach(string name in names.Split(' '))
    {
     var e=f.Factory.CreateEntity(name+"Crop");Assert.NotNull(e);Assert.True(f.Zone.AddEntity(e,40,23));
     for(int stage=0;stage<3;stage++){Stage(e,stage,stage%2);f.Refresh();Assert.True(f.Presenter.IsRenderedEntity(e),name+" stage "+stage);Assert.True(f.Presenter.TryGetOwnerView("biome-crop:"+e.ID,out var bound,out var root));Assert.AreSame(e,bound);Assert.True(Village3DIntegrationFixture.Drawn(root));Assert.AreEqual(Prefix+Stem(name)+"-"+stage+(stage%2==0?"-dry":"-wet"),root.GetComponent<MeshFilter>().sharedMesh.name);f.Refresh();Assert.True(f.Presenter.TryGetOwnerView("biome-crop:"+e.ID,out _,out var unchanged));Assert.AreSame(root,unchanged);}
     Assert.True(f.Zone.RemoveEntity(e));f.Refresh();Assert.False(f.Presenter.IsRenderedEntity(e));Assert.False(f.Presenter.TryGetOwnerView("biome-crop:"+e.ID,out _,out _));Assert.AreSame(town.root,f.View("central-cistern").root);
    }
   }
  }
  [TestCase("hidden")][TestCase("foreign-crop")][TestCase("foreign-render")][TestCase("foreign-physics")][TestCase("foreign-cell")][TestCase("foreign-spatial")][TestCase("stage")][TestCase("glyph")][TestCase("custom")][TestCase("legacy-mode")][TestCase("wrong-yield")]
  public void InvalidCurrentOwnerCannotBorrowTheNewPlantModel(string fault)
  {
   using(var f=new SpawnRing3DIntegrationFixture())
   {
    var e=f.Add("ClaspbeanCrop");var c=e.GetPart<CropPart>();var p=e.GetPart<PhysicsPart>();var r=e.GetPart<RenderPart>();var cell=f.Zone.GetEntityCell(e);var oldParent=cell.ParentZone;var spatial=typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic);var oldZone=spatial.GetValue(e);Exact(f,e,Prefix+"claspbean-0-dry");
    try{switch(fault){case "hidden":r.Visible=false;break;case "foreign-crop":c.ParentEntity=f.Player;break;case "foreign-render":r.ParentEntity=f.Player;break;case "foreign-physics":p.ParentEntity=f.Player;break;case "foreign-cell":cell.ParentZone=new Zone(f.Zone.ZoneID);break;case "foreign-spatial":spatial.SetValue(e,new Zone(f.Zone.ZoneID));break;case "stage":c.GrowthStage=3;break;case "glyph":r.RenderString="?";break;case "custom":r.VisualID="foreign";break;case "legacy-mode":c.HarvestAtMaturity=false;break;case "wrong-yield":c.YieldBlueprint="Dagger";break;}Assert.False(Recipe(f,e).ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);}
    finally{c.ParentEntity=e;r.ParentEntity=e;p.ParentEntity=e;cell.ParentZone=oldParent;spatial.SetValue(e,oldZone);}
   }
  }
  [Test] public void SavedCurrentReplacementKeepsRipeModelButOldReferenceCannotBorrowIt()
  {
   using(var f=new SpawnRing3DIntegrationFixture())
   {var e=f.Add("LampveinCrop");Stage(e,2,4);Exact(f,e,Prefix+"lampvein-2-wet");string id=e.ID;var at=f.Zone.GetEntityPosition(e);var loaded=f.RoundTrip();f.BindLoaded(loaded);var next=f.Zone.GetReadOnlyEntities().Single(x=>x.ID==id);Assert.AreNotSame(e,next);Assert.AreEqual(at,f.Zone.GetEntityPosition(next));Exact(f,next,Prefix+"lampvein-2-wet");Assert.Null(Recipe(f,e).ModelId);}
  }
  [Test] public void ExactClosedLibraryContains280InertNativeModelsAndOld37Remain()
  {
   var type=typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.BiomeCrop3DLibrary");Assert.NotNull(type,"New library is absent before this feature.");var lib=Resources.Load("BiomeCrops3D/Library",type);Assert.NotNull(lib);type.GetMethod("Validate").Invoke(lib,null);Assert.AreEqual(280,((Array)type.GetField("Entries").GetValue(lib)).Length);Assert.AreEqual(37,RepairCultivation3DLibrary.Load().Entries.Length);
  }
 }
}
