using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersDistrictPresentationTests
    {
        [Test] public void ActualCellarKeepsSpreadThreeDimensionalPresentation()
        {
            using(var scope=new HaulingContentScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                manager.GetZone(ReferenceGladePlan.ZoneID);var zone=manager.GetZone(GleanersCellarBuilder.ZoneID);
                Assert.True(SpreadPresentationScope.IsActive(zone),"The first underground visit must not fall back to the old 2D view.");
                var definition=UnityEngine.Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
                Assert.True(definition.SupportsZone(zone));
                foreach(string blueprint in new[]{"Floor","StoneWall","StairsUp","Crate","FallenBeam","MarlbackScrabbler","Signpost"})
                {
                    var owner=zone.GetReadOnlyEntities().First(e=>e.BlueprintName==blueprint);
                    var recipe=SpawnRing3DRecipes.Resolve(zone,owner,definition);
                    Assert.Null(recipe.Failure,blueprint);Assert.IsNotEmpty(recipe.ModelId,blueprint);
                    Assert.NotNull(UnityEngine.Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).FindModel(recipe.ModelId),recipe.ModelId);
                }
                var previous=zone;manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
                Assert.False(SpreadPresentationScope.IsActive(previous),"A detached older graph cannot borrow the current presentation authority.");
            }
        }
        [Test] public void NewPresentationDoesNotClaimOtherDepthsOrChangedBiome()
        {
            using(var scope=new HaulingContentScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                var cellar=manager.GetZone(GleanersCellarBuilder.ZoneID);Assert.True(SpreadPresentationScope.IsActive(cellar));
                manager.WorldMap.Tiles[11,10]=BiomeType.Beating;Assert.False(SpreadPresentationScope.IsActive(cellar));
                manager.WorldMap.Tiles[11,10]=BiomeType.Spread;
                var other=new Zone("Overworld.11.10.2");manager.SetActiveZone(other);Assert.False(SpreadPresentationScope.IsActive(other));
            }
        }
    }
}
