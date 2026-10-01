using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class CultivationVillageArtTests
 {
  static void PrepareFreshGarden(Village3DIntegrationFixture f)
  {
   var type=typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.MorrowfastStartingGarden");Assert.NotNull(type);
   var ensure=type.GetMethod("Ensure",BindingFlags.NonPublic|BindingFlags.Static);Assert.NotNull(ensure);
   Assert.AreEqual(6,ensure.Invoke(null,new object[]{f.Zone}),"Run the actual fresh-game garden setup before interpreting presentation assertions.");
  }
  [TestCase("KnotflaxCrop")][TestCase("HearthbulbCrop")][TestCase("SeamleafCrop")]
  public void StartingVillageShowsActualPlantedStagesAndRemovesOnlyHarvestedOwner(string blueprint)
  {
   using(var f=new Village3DIntegrationFixture())
   {
    PrepareFreshGarden(f);Assert.True(f.Factory.Blueprints.ContainsKey(blueprint));var ground=f.Zone.GetReadOnlyEntities().First(e=>e.HasTag("Terrain")&&e.HasTag("Plantable"));var cell=f.Zone.GetEntityCell(ground);var oldOwners=cell.Objects.ToArray();var e=f.Factory.CreateEntity(blueprint);Assert.True(f.Zone.AddEntity(e,cell.X,cell.Y));var c=e.GetPart<CropPart>();
    for(int stage=0;stage<3;stage++){c.GrowthStage=stage;e.GetPart<RenderPart>().RenderString=c.GlyphForStage(stage).ToString();e.GetPart<RenderPart>().ColorString=c.ColorForStage(stage);f.Refresh();Assert.True(f.Presenter.IsRenderedEntity(e),blueprint+" stage "+stage);Assert.True(f.Presenter.TryGetOwnerView("cultivation:"+e.ID,out var bound,out var root));Assert.AreSame(e,bound);Assert.True(Village3DIntegrationFixture.Drawn(root));StringAssert.StartsWith("repair-cultivation-",root.GetComponent<MeshFilter>().sharedMesh.name);}
    Assert.True(f.Zone.RemoveEntity(e));f.Refresh();Assert.False(f.Presenter.IsRenderedEntity(e));foreach(var owner in oldOwners)Assert.True(cell.Objects.Contains(owner));Assert.False(f.Presenter.TryGetOwnerView("cultivation:"+e.ID,out _,out _));
   }
  }
  [Test]public void VillageFurrowsUseExactTerrainWithoutReplacingAuthoredTownModels()
  {
   using(var f=new Village3DIntegrationFixture())
   {
    PrepareFreshGarden(f);var soil=f.Zone.GetReadOnlyEntities().First(e=>e.HasTag("Terrain")&&e.HasTag("Plantable"));if(soil.GetPart("CultivatedSoil")==null){var t=typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.CultivatedSoilPart");Assert.NotNull(t);soil.AddPart((Part)Activator.CreateInstance(t));}
    var town=f.View("central-cistern");f.Refresh();var method=typeof(Village3DPresenter).GetMethod("HasRepresentedCultivatedSoil");Assert.NotNull(method);Assert.True((bool)method.Invoke(f.Presenter,new object[]{soil}));Assert.AreSame(town.root,f.View("central-cistern").root);var marker=soil.GetPart("CultivatedSoil");marker.ParentEntity=f.Player;f.Refresh();Assert.False((bool)method.Invoke(f.Presenter,new object[]{soil}));marker.ParentEntity=soil;
   }
  }
 }
}
