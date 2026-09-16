using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class ThresholdCampsRenderingTests
    {
        // These manager-level checks need the shipped stock registry, just as
        // runtime bootstrap does. Other suites deliberately install partial
        // loot tables; importing their leftover tables is not a native town.
        [SetUp] public void LoadNativeStock()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeStock()=>CavesOfOoo.Data.LootTableRegistry.ResetForTests();
        [TestCase("Overworld.5.17.0")] [TestCase("Overworld.18.18.0")]
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

        [TestCase("Overworld.5.17.1")] [TestCase("Overworld.18.18.1")]
        [TestCase("Overworld.005.17.0")] [TestCase("Overworld.18.018.0")]
        public void OtherDepthsAndNoncanonicalAddressesRemainUnclaimed(string id)
            =>Assert.IsFalse(VoxelWorldPresentation.IsSupported(id));

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)] [TestCase(1)]
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

        [TestCase("Overworld.5.17.0","GuestClothPole")]
        [TestCase("Overworld.5.17.0","TentRightHost")]
        [TestCase("Overworld.5.17.0","SaltMaster")]
        [TestCase("Overworld.5.17.0","TentWall")]
        [TestCase("Overworld.18.18.0","LastCounterSign")]
        [TestCase("Overworld.18.18.0","SaccharineEnvoy")]
        [TestCase("Overworld.18.18.0","Chest")]
        [TestCase("Overworld.18.18.0","SandstoneWall")]
        public void DefiningOwnersKeepCurrentVisibilityAndNativeMechanics(string id,string blueprint)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone(id);
            var owner=factory.CreateEntity(blueprint);Assert.IsTrue(zone.AddEntity(owner,20,10));
            int parts=owner.Parts.Count,version=zone.EntityVersion;
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
            Assert.NotNull(recipe.ModelId,recipe.Failure);Assert.AreSame(owner,recipe.Owner);
            Assert.AreEqual(owner.HasTag("Creature")||owner.GetPart<PhysicsPart>()?.Takeable==true,recipe.Transient);
            Assert.AreEqual(parts,owner.Parts.Count);Assert.AreEqual(version,zone.EntityVersion);
            owner.GetPart<RenderPart>().Visible=false;
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
            owner.GetPart<RenderPart>().Visible=true;zone.RemoveEntity(owner);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }

        [TestCase("Overworld.5.17.0")] [TestCase("Overworld.18.18.0")]
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

        [TestCase("Overworld.5.17.0")] [TestCase("Overworld.18.18.0")]
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

        [TestCase("Overworld.5.17.0","GuestClothPole","firsttent-cloth-")]
        [TestCase("Overworld.5.17.0","TentRightHost","firsttent-host-")]
        [TestCase("Overworld.5.17.0","RoadStone","firsttent-path-")]
        [TestCase("Overworld.5.17.0","Sand","firsttent-ground-")]
        [TestCase("Overworld.18.18.0","Floor","lastcounter-ground-")]
        [TestCase("Overworld.18.18.0","SandstoneWall","lastcounter-wall-")]
        [TestCase("Overworld.18.18.0","LastCounterSign","lastcounter-sign-")]
        [TestCase("Overworld.18.18.0","SaccharineEnvoy","lastcounter-envoy-")]
        public void SignatureAssetsAreUsedByTheirActualNativeOwners(string id,string bp,string prefix)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(e,20,10);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            StringAssert.StartsWith(prefix,SpawnRing3DRecipes.Resolve(z,e,catalog).ModelId);
        }
        [TestCase("TentRightHost")] [TestCase("SaccharineEnvoy")] [TestCase("SaltMaster")]
        public void TravellingPeopleRetainTheirNativeModelBetweenBothPlaces(string bp)
        {
            var factory=GrovelandsCompositionTests.Factory();var first=new Zone("Overworld.5.17.0");var last=new Zone("Overworld.18.18.0");
            var e=factory.CreateEntity(bp);first.AddEntity(e,20,10);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var initial=SpawnRing3DRecipes.Resolve(first,e,catalog);Assert.NotNull(initial.ModelId);
            first.RemoveEntity(e);last.AddEntity(e,30,11);
            Assert.AreEqual(initial.ModelId,SpawnRing3DRecipes.Resolve(last,e,catalog).ModelId);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(first,e,catalog).ModelId);
        }

        [Test] public void TheThreeDisclaimerBandsFaceTheActualGameplayCamera()
        {
            // Hypothesis from native camera review: +Z sign lettering was on
            // the back because the gameplay camera views from negative Z.
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.18.18.0"))
            {
                f.Zone=new Zone("Overworld.18.18.0");f.Bind(f.Zone);f.Set("FullReveal",true);
                var sign=f.Add("LastCounterSign",40,12);f.Refresh();
                var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,sign,catalog);Assert.AreEqual(2,recipe.QuarterTurns);
                var direction=f.Get<Camera>("WorldCamera").transform.position-recipe.Position;
                var front=Quaternion.Euler(0,recipe.QuarterTurns*90,0)*Vector3.forward;
                Assert.Greater(Vector3.Dot(front,direction),0);Assert.Less(Vector3.Dot(-front,direction),0);
                Assert.IsNull(sign.GetPart<DestructiblePart>());Assert.AreEqual(1,f.Zone.EntityCount);
            }
        }

        private static int PatchVertices(SpawnRing3DIntegrationFixture f)=>f.Root.GetComponentsInChildren<MeshFilter>(true)
            .Where(x=>x.sharedMesh!=null&&x.sharedMesh.name.StartsWith("Ring patch",StringComparison.Ordinal)).Sum(x=>x.sharedMesh.vertexCount);
        internal static IEnumerable<string> Addresses()
        { yield return "Overworld.5.17.0";yield return "Overworld.18.18.0"; }
    }
}
