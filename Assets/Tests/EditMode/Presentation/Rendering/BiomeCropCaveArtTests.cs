using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class BiomeCropCaveArtTests
 {
  static string OrdinaryColumn(OverworldZoneManager manager)
  {
   var columns=BiomeCropPlan.CaveColumns(manager);Assert.IsNotEmpty(columns);var p=WorldMap.FromZoneID(columns[0]);return WorldMap.ToZoneID(p.x,p.y,1);
  }
  [Test]public void CurrentOrdinaryCaveIsAdmittedButDetachedSameIdAndUnmanagedAddressAreNot()
  {
   using(var f=new EntityEquipmentContentFixture())
   {
    var manager=new OverworldZoneManager(f.Factory,729490642);string id=OrdinaryColumn(manager);var zone=manager.GetZone(id);Assert.NotNull(zone);Assert.True(BiomeCropPlan.IsOrdinaryCave(manager,id));
    var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);Assert.NotNull(library);
    Assert.True(library.Definition.SupportsZone(zone),"Actual receiving-manager ordinary cave must admit botanical geometry.");
    Assert.False(library.Definition.SupportsZone(new Zone(id)));Assert.False(library.Definition.SupportsZone(id),"Do not widen address-only authority.");
   }
  }
  [Test]public void CurrentDepthTerrainResolvesRealFloorAndWallOwnersWithoutGraphReplacement()
  {
   using(var f=new EntityEquipmentContentFixture())
   {
    var manager=new OverworldZoneManager(f.Factory,729490642);var p=WorldMap.FromZoneID(OrdinaryColumn(manager));var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
    for(int depth=1;depth<=5;depth++)
    {
     var zone=manager.GetZone(WorldMap.ToZoneID(p.x,p.y,depth));Assert.NotNull(zone);var materials=SolidEarthBuilder.GetMaterialsForDepth(depth);
     foreach(string bp in new[]{materials.wallBP,materials.floorBP})
     {var owner=zone.GetReadOnlyEntities().First(e=>e.BlueprintName==bp);var cell=zone.GetEntityCell(owner);var recipe=SpawnRing3DRecipes.Resolve(zone,owner,library.Definition);Assert.NotNull(recipe.ModelId,bp);Assert.AreSame(owner,recipe.Owner);Assert.AreSame(cell,zone.GetEntityCell(owner));Assert.NotNull(library.FindModel(recipe.ModelId));}
    }
   }
  }
  [Test]public void ActualOrdinaryCavePresenterShowsAllFiveNativePlantSpecies()
  {
   string id;using(var content=new EntityEquipmentContentFixture())id=OrdinaryColumn(new OverworldZoneManager(content.Factory,729490642));
   using(var f=new SpawnRing3DIntegrationFixture(id))
   {
    Assert.True(f.Get<bool>("VoxelPresentationActive"));foreach(string name in BiomeCropArtTests.Groups[6].Split(' '))
    {var e=f.Add(name+"Crop");f.Refresh(f.Dirty(e));Assert.True(f.Rendered(e),name);Assert.AreEqual("biome-crop-"+name.ToLowerInvariant()+"-0-dry",SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}
   }
  }
 }
}
