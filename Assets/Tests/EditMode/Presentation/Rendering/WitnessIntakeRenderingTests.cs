using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WitnessIntakeRenderingTests
    {
        // These manager-level checks need the shipped stock registry, just as
        // runtime bootstrap does. Other suites deliberately install partial
        // loot tables; importing their leftover tables is not a native town.
        [SetUp] public void LoadNativeStock()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeStock()=>CavesOfOoo.Data.LootTableRegistry.ResetForTests();
        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.12.12.0")]
        public void TheTwoNativeChunksBindVoxelPresentationWithoutUnmappedSources(string id)
        {
            Assert.IsTrue(VoxelWorldPresentation.IsSupported(id), id);
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Set("FullReveal",true);f.Refresh();
                Assert.IsTrue(f.Get<bool>("VoxelPresentationActive"),f.Get<string>("Failure"));
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
                var camera=f.Get<Camera>("WorldCamera");int builds=f.Get<int>("GroundBuildCount");
                f.Refresh(new HashSet<int>());
                Assert.AreSame(camera,f.Get<Camera>("WorldCamera"));
                Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
            }
        }

        [TestCase("Overworld.17.5.1")] [TestCase("Overworld.12.12.1")]
        [TestCase("Overworld.5.17.0")] [TestCase("Overworld.12.3.2")]
        [TestCase("Overworld.017.5.0")] [TestCase("Overworld.12.012.0")]
        public void NeighboringPlacesAndDeeperCatacombsRetainTheirPresentation(string id)
            =>Assert.IsFalse(VoxelWorldPresentation.IsSupported(id));

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void EveryVisibleNativeOwnerAcrossTwoChunksHasAReadOnlyModel(int seed)
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var missing=new SortedSet<string>();int count=0;
            foreach(var id in Addresses())
            {
                var zone=manager.GetZone(id);int version=zone.EntityVersion;count++;
                foreach(var owner in zone.GetAllEntities())
                {
                    var recipe=SpawnRing3DRecipes.Resolve(zone,owner,library.Definition);
                    if(owner.GetPart<RenderPart>()?.Visible!=true){Assert.IsNull(recipe.ModelId);continue;}
                    if(recipe.ModelId==null){missing.Add(id+" "+owner.BlueprintName+": "+recipe.Failure);continue;}
                    Assert.AreSame(owner,recipe.Owner);
                    Assert.NotNull(library.FindModel(recipe.ModelId),recipe.ModelId);
                    Assert.NotNull(library.Definition.FindModel(recipe.ModelId),recipe.ModelId);
                }
                Assert.AreEqual(version,zone.EntityVersion);
            }
            Assert.AreEqual(2,count);Assert.IsEmpty(missing,string.Join("\n",missing));
        }

        [TestCase("Overworld.17.5.0","PreFellingBody","drownedledger-preserved-")]
        [TestCase("Overworld.17.5.0","SurveyStake","drownedledger-stake-")]
        [TestCase("Overworld.17.5.0","ReadingTable","drownedledger-table-")]
        [TestCase("Overworld.17.5.0","RecensionScribe","drownedledger-scribe-")]
        [TestCase("Overworld.17.5.0","CurationSorter","drownedledger-sorter-")]
        [TestCase("Overworld.17.5.0","SealedBogTakenBody","drownedledger-parcel-")]
        [TestCase("Overworld.12.12.0","SealedBogTakenBody","drownedledger-parcel-")]
        [TestCase("Overworld.12.12.0","Floor","marrowstye-ground-")]
        [TestCase("Overworld.12.12.0","SandstoneWall","marrowstye-wall-")]
        [TestCase("Overworld.12.12.0","StoneCoffer","marrowstye-coffer-")]
        [TestCase("Overworld.12.12.0","SaltCuredBody","marrowstye-cured-")]
        [TestCase("Overworld.12.12.0","FilerClerk","marrowstye-clerk-")]
        [TestCase("Overworld.17.5.0","WaterPuddle","sumphold-water-")]
        [TestCase("Overworld.17.5.0","MarketStall","cinderhold-stall-")]
        [TestCase("Overworld.12.12.0","MarketStall","cinderhold-stall-")]
        [TestCase("Overworld.17.5.0","Duckboard","drownedledger-boards-")]
        [TestCase("Overworld.17.5.0","SandstoneWall","sumphold-wall-")]
        [TestCase("Overworld.12.12.0","RoadStone","marrowstye-path-")]
        public void DefiningOwnersHaveDistinctCurrentNativeModels(string id,string blueprint,string prefix)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone(id);
            var owner=factory.CreateEntity(blueprint);Assert.IsTrue(zone.AddEntity(owner,20,10));
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
            Assert.NotNull(recipe.ModelId,recipe.Failure);StringAssert.StartsWith(prefix,recipe.ModelId);
            Assert.AreSame(owner,recipe.Owner);
            Assert.AreEqual(owner.HasTag("Creature")||owner.GetPart<PhysicsPart>()?.Takeable==true,recipe.Transient);
            owner.GetPart<RenderPart>().Visible=false;
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
            owner.GetPart<RenderPart>().Visible=true;zone.RemoveEntity(owner);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }

        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.12.12.0")]
        public void CurrentMapPlaceAuthorityVetoesTheFiniteAddressAndSurvivesRestore(string id)
        {
            var factory=GrovelandsCompositionTests.Factory();var at=WorldMap.FromZoneID(id);
            var own=new OverworldZoneManager(factory,64);var home=own.GetZone(id);
            var other=new OverworldZoneManager(factory,1729);
            other.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.Village,"different place",tier:3));
            var loaded=new Zone(id);var owner=factory.CreateEntity("StoneFloor");loaded.AddEntity(owner,10,10);
            int version=loaded.EntityVersion;
            other.ReplaceLoadedState(new Dictionary<string,Zone>{{id,loaded}},id,new Dictionary<string,List<ZoneConnection>>());
            Assert.IsTrue(AreaCompositionScope.Allows(home));Assert.IsFalse(AreaCompositionScope.Allows(loaded));
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            Assert.IsNull(SpawnRing3DRecipes.Resolve(loaded,owner,catalog).ModelId);
            Assert.AreEqual(version,loaded.EntityVersion);Assert.AreEqual(1,loaded.EntityCount);
        }

        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.12.12.0")]
        public void RemovingNativeGroundNeverPaintsItBackFromAProceduralPlan(string id)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Zone=new Zone(id);f.Bind(f.Zone);f.Set("FullReveal",true);f.Refresh();
                Assert.IsTrue(f.Get<bool>("IsReady"),f.Get<string>("Failure"));
                Assert.AreEqual(0,PatchVertices(f));
                var floor=f.Add("StoneFloor",40,12);f.Refresh();Assert.Greater(PatchVertices(f),0);
                f.Zone.RemoveEntity(floor);f.Refresh(new HashSet<int>());Assert.AreEqual(0,PatchVertices(f));
            }
        }

        private static int PatchVertices(SpawnRing3DIntegrationFixture f)=>f.Root.GetComponentsInChildren<MeshFilter>(true)
            .Where(x=>x.sharedMesh!=null&&x.sharedMesh.name.StartsWith("Ring patch",StringComparison.Ordinal)).Sum(x=>x.sharedMesh.vertexCount);
        internal static IEnumerable<string> Addresses()
        { yield return "Overworld.17.5.0";yield return "Overworld.12.12.0"; }
    }
}
