using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SoddenPopulationArtTests
    {
        DensityLootTestScope scope;
        OverworldZoneManager manager;
        Zone zone;
        SpawnRing3DCatalog catalog;
        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            manager.WorldMap.Tiles[12,2] = BiomeType.Sodden;
            manager.WorldMap.SetPOI(12,2,null);
            zone = new Zone("Overworld.12.2.0"); manager.SetActiveZone(zone);
            catalog = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
        }
        [TearDown] public void Cleanup() { scope.Dispose(); }
        Entity Add(string blueprint)
        { var owner=scope.Factory.CreateEntity(blueprint); Assert.NotNull(owner); zone.AddEntity(owner,20,10); return owner; }
        [TestCase("Reedfrog","spread-visitor-reedfrog")]
        [TestCase("Bandfrog","spread-visitor-bandfrog")]
        [TestCase("GinFrog","spread-visitor-gin-frog")]
        [TestCase("Viper","spread-biome-viper")]
        [TestCase("PeatCutter","spread-person-peat-cutter")]
        public void ActualBogResidentsUseTheirOriginalAnimatedBodies(string blueprint,string expected)
        {
            var owner=Add(blueprint); int count=zone.EntityCount;
            var result=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
            Assert.AreEqual(expected,result.ModelId,result.Failure); Assert.AreSame(owner,result.Owner);
            Assert.True(result.Transient); Assert.False(result.Batched); Assert.AreEqual(count,zone.EntityCount);
        }
        [TestCase("Cudgel")][TestCase("Hatchet")][TestCase("HealingTonic")][TestCase("FrogOil")]
        public void DroppedUsefulItemsKeepTheirAuthoredPortableBodies(string blueprint)
        {
            var owner=Add(blueprint);Assert.True(SpreadPortableRecipes.TryRecipe(owner,out string expected));
            Assert.AreEqual(expected,SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }
        [TestCase("CrackedGlowQuartz")][TestCase("KnotflaxSnare")]
        public void PlacedItemResultsHaveActualThreeDimensionalBodies(string blueprint)
        {
            var owner=Add(blueprint);var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
            Assert.That(recipe.ModelId,Does.StartWith("spread-scenery-"),recipe.Failure);
        }
        [Test] public void GreatdewHasItsOwnBentStemBodyAndKeepsItsActualSnare()
        {
            var owner=Add("Greatdew"); var snare=owner.GetPart<GreatdewSnarePart>();
            var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
            Assert.That(recipe.ModelId,Does.StartWith("sodden-native-greatdew-"),recipe.Failure);
            Assert.True(recipe.Batched);Assert.False(recipe.Transient);Assert.AreSame(snare,owner.GetPart<GreatdewSnarePart>());
            Assert.False(owner.GetPart<PhysicsPart>().Solid);
        }
        [TestCase("Reedfrog")][TestCase("Bandfrog")][TestCase("GinFrog")][TestCase("Viper")][TestCase("Greatdew")]
        public void HiddenMovedAndReskinnedOwnersCannotBorrowBogAppearance(string blueprint)
        {
            var owner=Add(blueprint);var render=owner.GetPart<RenderPart>();
            render.Visible=false;Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
            render.Visible=true;render.VisualID="custom-unreviewed";Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
            render.VisualID=null;zone.RemoveEntity(owner);Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }
        [TestCase("Reedfrog")][TestCase("Bandfrog")][TestCase("GinFrog")][TestCase("Viper")][TestCase("Greatdew")]
        public void ChangedMapAuthorityDoesNotGrantNewBogAppearance(string blueprint)
        {
            var owner=Add(blueprint);manager.WorldMap.Tiles[12,2]=BiomeType.Beating;
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }
        [Test] public void DestroyedGreatdewCannotReturnAsHealthyScenery()
        {
            var owner=Add("Greatdew"); owner.GetPart<DestructiblePart>().HP=0;
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }
        [Test] public void MissingSnareDoesNotGainAnImpliedTrap()
        {
            var owner=Add("Greatdew");owner.RemovePart(owner.GetPart<GreatdewSnarePart>());
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }
    }
}
