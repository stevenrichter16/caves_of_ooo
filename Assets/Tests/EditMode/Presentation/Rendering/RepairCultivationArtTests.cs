using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 // Native state and submitted presenter geometry; ordinary paid input and visual
 // legibility are checked separately by the Play route, not claimed here.
 public sealed class RepairCultivationArtTests
 {
  const string Prefix="repair-cultivation-",Folder="RepairCultivation3D/";
  static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f,Entity e)=>SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
  static void Field(Part part,string name,object value){Assert.NotNull(part);var field=part.GetType().GetField(name);Assert.NotNull(field,name);field.SetValue(part,value);}
  static Mesh Exact(SpawnRing3DIntegrationFixture f,Entity e,string suffix)
  {
   var r=Recipe(f,e);Assert.AreSame(e,r.Owner);Assert.Null(r.Failure);Assert.AreEqual(Prefix+suffix,r.ModelId);f.Refresh(f.Dirty(e));Assert.True(f.Rendered(e),r.ModelId);
   var prefab=Resources.Load<GameObject>(Folder+r.ModelId);Assert.NotNull(prefab,r.ModelId);var mesh=prefab.GetComponent<MeshFilter>().sharedMesh;Assert.Greater(mesh.vertexCount,24);Assert.GreaterOrEqual(mesh.uv.Distinct().Count(),2);
   Assert.AreSame(mesh,f.Library.FindModel(r.ModelId).GetComponent<MeshFilter>().sharedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,prefab.GetComponent<MeshRenderer>().sharedMaterial);
   Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));return mesh;
  }
  [TestCase("Knotflax","knotflax")][TestCase("Hearthbulb","hearthbulb")][TestCase("Seamleaf","seamleaf")]
  public void ThreeNativeStagesAndWetSoilHaveDistinctRealGeometryInGrovelands(string bp,string family)
  {
   using(var f=new SpawnRing3DIntegrationFixture())
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(bp+"Crop"),"Actual crop blueprint required.");var e=f.Add(bp+"Crop");var c=e.GetPart<CropPart>();Assert.NotNull(c);Field(c,"HarvestAtMaturity",true);float prior=0;
    for(int stage=0;stage<3;stage++)
    {
     c.GrowthStage=stage;e.GetPart<RenderPart>().RenderString=c.GlyphForStage(stage).ToString();e.GetPart<RenderPart>().ColorString=c.ColorForStage(stage);c.MoistureTicks=0;var dry=Exact(f,e,family+"-"+stage+"-dry");c.MoistureTicks=1;var wet=Exact(f,e,family+"-"+stage+"-wet");CollectionAssert.AreEqual(dry.vertices,wet.vertices);Assert.False(dry.uv.SequenceEqual(wet.uv));Assert.Greater(dry.bounds.size.y,prior);prior=dry.bounds.size.y;
    }
    string id=e.ID;var at=f.Zone.GetEntityPosition(e);var loaded=f.RoundTrip();f.BindLoaded(loaded);var replacement=f.Zone.GetReadOnlyEntities().Single(x=>x.ID==id);Assert.AreEqual(at,f.Zone.GetEntityPosition(replacement));Exact(f,replacement,family+"-2-wet");Assert.Null(Recipe(f,e).ModelId);
   }
  }
  [TestCase("RepairLinedWell","clay-well-lining","lined-well")][TestCase("RepairRopeWell","rope-well-line","rope-well")][TestCase("RepairWoodenGate","timber-gate-frame","wooden-gate")]
  public void ActualRepairStateChangesShapeAndRepairedGateStillFollowsDoor(string bp,string fault,string family)
  {
   using(var f=new SpawnRing3DIntegrationFixture())
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(bp));var e=f.Add(bp);var p=e.GetPart("Repairable");Assert.NotNull(p);Assert.AreEqual(fault,p.GetType().GetField("RecipeId").GetValue(p));var before=Exact(f,e,family+"-broken");Field(p,"Repaired",true);var door=e.GetPart<DoorPart>();if(door!=null){door.IsOpen=false;e.GetPart<RenderPart>().RenderString="+";door.QuarterTurns=1;}
    var after=Exact(f,e,family+(door==null?"-restored":"-closed"));Assert.False(before.vertices.SequenceEqual(after.vertices));if(door!=null){door.IsOpen=true;e.GetPart<RenderPart>().RenderString="/";var open=Exact(f,e,family+"-open");Assert.False(open.vertices.SequenceEqual(after.vertices));Assert.AreEqual(1,Recipe(f,e).QuarterTurns);}
    Field(p,"RecipeId","wrong-fault");Assert.False(Recipe(f,e).ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);
   }
  }
  [TestCase("KnotflaxSeed","knotflax-seed")][TestCase("HearthbulbSeed","hearthbulb-seed")][TestCase("SeamleafSeed","seamleaf-seed")]
  [TestCase("KnotflaxCord","knotflax-cord")][TestCase("Hearthbulb","hearthbulb")][TestCase("SeamleafSprig","seamleaf-sprig")][TestCase("RoastedHearthbulb","roasted-hearthbulb")][TestCase("SalvagedTimber","salvaged-timber")]
  public void ExactPortableFormsDisappearOnPickupAndReturnOnDrop(string bp,string model)
  {using(var f=new SpawnRing3DIntegrationFixture()){Assert.True(f.Factory.Blueprints.ContainsKey(bp));var e=f.Add(bp);var mesh=Exact(f,e,model);f.Approach(e);Assert.True(InventorySystem.Pickup(f.Player,e,f.Zone));f.Refresh();Assert.False(f.Rendered(e));Assert.Null(Recipe(f,e).ModelId);Assert.True(InventorySystem.Drop(f.Player,e,f.Zone));Assert.AreSame(mesh,Exact(f,e,model));}}
  [TestCase("RepairClayBank","clay-bank")][TestCase("RepairTimberPile","timber-pile")][TestCase("RepairCordBundle","cord-bundle")]
  public void FiniteSupplyNodeHasAnExactFormThenRemovesWithItsOwner(string bp,string model)
  {using(var f=new SpawnRing3DIntegrationFixture()){Assert.True(f.Factory.Blueprints.ContainsKey(bp));var e=f.Add(bp);Exact(f,e,model);Assert.NotNull(e.GetPart<HarvestablePart>());Assert.True(f.Zone.RemoveEntity(e));f.Refresh();Assert.False(f.Rendered(e));Assert.Null(Recipe(f,e).ModelId);}}
  [TestCase("hidden")][TestCase("foreign-crop")][TestCase("foreign-cell")][TestCase("foreign-spatial")][TestCase("stage")][TestCase("glyph")][TestCase("custom")][TestCase("legacy-mode")]
  public void InvalidCurrentCropCannotBorrowAuthoredMaturity(string fault)
  {
   using(var f=new SpawnRing3DIntegrationFixture())
   {
    Assert.True(f.Factory.Blueprints.ContainsKey("KnotflaxCrop"));var e=f.Add("KnotflaxCrop");var crop=e.GetPart<CropPart>();var cell=f.Zone.GetEntityCell(e);var oldParent=cell.ParentZone;var spatial=typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic);var before=spatial.GetValue(e);Exact(f,e,"knotflax-0-dry");
    try{if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;if(fault=="foreign-crop")crop.ParentEntity=f.Player;if(fault=="foreign-cell")cell.ParentZone=new Zone(f.Zone.ZoneID);if(fault=="foreign-spatial")spatial.SetValue(e,new Zone(f.Zone.ZoneID));if(fault=="stage")crop.GrowthStage=3;if(fault=="glyph")e.GetPart<RenderPart>().RenderString="?";if(fault=="custom")e.GetPart<RenderPart>().VisualID="foreign";if(fault=="legacy-mode")Field(crop,"HarvestAtMaturity",false);Assert.False(Recipe(f,e).ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);}
    finally{crop.ParentEntity=e;cell.ParentZone=oldParent;spatial.SetValue(e,before);}
   }
  }
  [Test]public void CultivatedOverlayRetainsExactUnderlyingGroundAndClearsOnMarkerRemoval()
  {
   using(var f=new SpawnRing3DIntegrationFixture())
   {
    var type=typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.CultivatedSoilPart");Assert.NotNull(type);
    var at=f.FreeCell();var soil=f.Zone.GetCell(at.x,at.y).Objects.First(x=>x.HasTag("Terrain")&&!x.GetPart<PhysicsPart>().Solid);
    Assert.False(BarrenGroundRules.IsBarren(f.Zone.GetEntityCell(soil)));Assert.False(f.Zone.GetEntityCell(soil).HasObjectWithPart<LiquidPoolPart>());
    Assert.IsNotEmpty(soil.GetPart<RenderPart>().GlyphVariants,"Actual terrain variants must survive the appended overlay.");
    var before=Recipe(f,soil);soil.SetTag("Plantable");var marker=(Part)Activator.CreateInstance(type);soil.AddPart(marker);
    Assert.True(RepairCultivationRecipes.HasCultivatedSoil(f.Zone,soil),"Actual current cultivated terrain, retaining native glyph variants.");
    f.Refresh(f.Dirty(soil));var method=f.Presenter.GetType().GetMethod("HasRepresentedCultivatedSoil");Assert.NotNull(method);
    Assert.True((bool)method.Invoke(f.Presenter,new object[]{soil}),"A valid saved bed must contribute a submitted furrow mesh.");
    Assert.AreEqual(before.ModelId,Recipe(f,soil).ModelId);Assert.AreSame(soil,f.Zone.GetCell(at.x,at.y).Objects.First(x=>x==soil));
    marker.ParentEntity=f.Player;f.Refresh(f.Dirty(soil));Assert.False((bool)method.Invoke(f.Presenter,new object[]{soil}));marker.ParentEntity=soil;
    Assert.False(RepairCultivationRecipes.HasCultivatedSoil(f.Zone,null));
   }
  }
 }
}
