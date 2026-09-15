using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class OverwritGinmereRenderingTests
    {
        [Test]
        public void OverwritUsesItsCanonicalScrapedPaletteInsteadOfTheLegacyRuinsAlias()
        {
            var palette=CavesOfOoo.Presentation.Rendering.BiomePalette.GetForBiome(BiomeType.Overwrit);
            Assert.AreEqual(BiomeType.Overwrit,palette.Biome);
            Assert.AreEqual(-40,palette.Saturation);Assert.AreEqual(-10,palette.Contrast);
            Assert.AreEqual(new Color(.9f,.9f,.9f,1),palette.ColorFilter);
        }

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void EveryVisibleNativeOwnerAcrossBothAreasHasAnExplicitReadOnlyRecipe(int seed)
        {
            var factory=GrovelandsCompositionTests.Factory();
            var manager=new OverworldZoneManager(factory,seed);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            int count=0;var missing=new SortedSet<string>();
            foreach(string id in Addresses())
            {
                var zone=manager.GetZone(id);int version=zone.EntityVersion;count++;
                Assert.IsTrue(catalog.SupportsZone(id),id);
                Assert.IsTrue(VoxelWorldPresentation.IsSupported(id),id);
                foreach(var owner in zone.GetAllEntities())
                {
                    var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
                    if(owner.GetPart<RenderPart>()?.Visible!=true){Assert.IsNull(recipe.ModelId);continue;}
                    if(recipe.ModelId==null){missing.Add(id+" "+owner.BlueprintName+": "+recipe.Failure);continue;}
                    Assert.NotNull(catalog.FindModel(recipe.ModelId),recipe.ModelId);
                    Assert.AreSame(owner,recipe.Owner);
                }
                Assert.AreEqual(version,zone.EntityVersion,"Recipe lookup is not world generation.");
            }
            Assert.AreEqual(25,count);
            Assert.IsEmpty(missing,string.Join("\n",missing));
        }

        [TestCase(POIType.Village)] [TestCase(POIType.MerchantCamp)]
        public void ARealRuntimePlaceAtTheSameAddressRetainsItsOwnPresentationAuthority(POIType kind)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.4.11.0"))
            {
                var other=new OverworldZoneManager(f.Factory,1729);
                other.WorldMap.SetPOI(4,11,new PointOfInterest(kind,"scope control",tier:4));
                var place=other.GetZone("Overworld.4.11.0");Assert.NotNull(place);
                int version=place.EntityVersion;var owners=place.GetAllEntities().ToArray();
                f.Zone=place;f.Bind(place);f.Refresh();
                Assert.IsFalse(f.Get<bool>("VoxelPresentationActive"),"Address eligibility cannot override a runtime place's native pipeline.");
                Assert.IsFalse(f.Get<bool>("PresentationVisible"));
                Assert.AreEqual(version,place.EntityVersion);CollectionAssert.AreEquivalent(owners,place.GetAllEntities());
            }
        }

        [Test]
        public void RuntimePlaceAuthorityBelongsToItsOwnWorldAndIsReattachedOnRestore()
        {
            var factory=GrovelandsCompositionTests.Factory();const string id="Overworld.4.11.0";
            var ordinary=new OverworldZoneManager(factory,64);var home=ordinary.GetZone(id);
            var custom=new OverworldZoneManager(factory,1729);
            custom.WorldMap.SetPOI(4,11,new PointOfInterest(POIType.Village,"scope control",tier:4));
            var town=custom.GetZone(id);
            Assert.IsTrue(AreaCompositionScope.Allows(home));Assert.IsFalse(AreaCompositionScope.Allows(town));
            var loaded=new Zone(id);int version=loaded.EntityVersion;
            Assert.IsTrue(AreaCompositionScope.Allows(loaded),"A standalone native graph has only its finite-address contract.");
            custom.ReplaceLoadedState(new Dictionary<string,Zone>{{id,loaded}},id,new Dictionary<string,List<ZoneConnection>>());
            Assert.IsFalse(AreaCompositionScope.Allows(loaded));Assert.IsTrue(AreaCompositionScope.Allows(home));
            Assert.AreEqual(version,loaded.EntityVersion);Assert.AreEqual(0,loaded.EntityCount,"Reattaching scope never regenerates a saved graph.");
        }

        [Test]
        public void LivePlaceVetoReleasesTheOldSurfaceBeforeASecondBindAndCanRecover()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.4.11.0"))
            {
                int version=f.Zone.EntityVersion;
                f.Manager.WorldMap.SetPOI(4,11,new PointOfInterest(POIType.MerchantCamp,"scope control",tier:4));
                f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Get<bool>("IsReady"));Assert.IsFalse(f.Get<bool>("PresentationVisible"));
                f.Manager.WorldMap.SetPOI(4,11,null);f.Bind(f.Zone);f.Refresh();
                Assert.IsTrue(f.Get<bool>("VoxelPresentationActive"));Assert.AreEqual(version,f.Zone.EntityVersion);
            }
        }

        [TestCase("Overworld.2.7.0")] [TestCase("Overworld.2.7.1")]
        [TestCase("Overworld.2.7.2")] [TestCase("Overworld.0.11.0")]
        public void NativePresenterBindsBothAreasWithVoxelArtAndNoUnmappedSourceMeshes(string id)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Set("FullReveal",true);f.Refresh();
                Assert.IsTrue(f.Get<bool>("VoxelPresentationActive"));
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
                var camera=f.Get<Camera>("WorldCamera");int before=f.Get<int>("GroundBuildCount");
                f.Refresh(new HashSet<int>());
                Assert.AreSame(camera,f.Get<Camera>("WorldCamera"));
                Assert.AreEqual(before,f.Get<int>("GroundBuildCount"),"An unchanged full-reveal frame does not rebuild scenery.");
            }
        }

        [TestCase("Overworld.2.7.3")] [TestCase("Overworld.2.11.0")]
        [TestCase("Overworld.0.11.1")] [TestCase("Overworld.02.7.1")]
        [TestCase("Overworld.4.6.2")] [TestCase("Overworld.5.4.2")]
        public void NeighboringSpecialPlacesAndDepthsDoNotAcquireTheNewVoxelScope(string id)
        {
            Assert.IsFalse(OverwritCompositionPlan.IsWildernessZone(id));
            Assert.IsFalse(GinmereCompositionPlan.IsSupportedZone(id));
            Assert.IsFalse(VoxelWorldPresentation.IsSupported(id));
        }

        [TestCase("Overworld.2.7.0","Grass")]
        [TestCase("Overworld.2.7.1","Floor")]
        [TestCase("Overworld.2.7.2","Floor")]
        [TestCase("Overworld.0.11.0","OverwritGround")]
        public void MissingNativeGroundStaysMissingInsteadOfReappearingAsDecorativeGrass(string id,string floor)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                var empty=new Zone(id);f.Zone=empty;f.Bind(empty);f.Set("FullReveal",true);f.Refresh();
                Assert.AreEqual(0,PatchVertexCount(f),"A missing floor/cleared hole must not receive synthetic walkable-looking ground.");
                var owner=f.Add(floor,40,12);f.Refresh();
                Assert.Greater(PatchVertexCount(f),0,"Positive control: native floor really contributes geometry.");
                empty.RemoveEntity(owner);f.Refresh(new HashSet<int>());
                Assert.AreEqual(0,PatchVertexCount(f),"Removal must not fall back to painted grass.");
                Assert.IsTrue(f.Get<bool>("IsReady"),f.Get<string>("Failure"));
            }
        }

        [TestCase("MirePool")] [TestCase("SinkholeLip")] [TestCase("DescentLedge")]
        [TestCase("RopeAnchor")] [TestCase("PricklebrowNest")]
        public void GinmereSceneryReconcilesExactOwnerRemovalWithoutReplayingThePlan(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.2.7.2"))
            {
                f.Set("FullReveal",true);
                var owner=f.Add(blueprint,20,10);var control=f.Add(blueprint,60,20);f.Refresh();
                Assert.IsTrue(f.Rendered(owner),blueprint);Assert.IsTrue(f.Rendered(control));
                f.Zone.RemoveEntity(owner);f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Rendered(owner));Assert.IsTrue(f.Rendered(control));
                Assert.IsFalse(f.Zone.GetCell(20,10).Objects.Contains(owner));
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
            }
        }

        [TestCase("GinFrog")] [TestCase("PrickleBrowGecko")]
        public void SimaFaunaStayTransientRespectTheirNativeGlyphAndCurrentMembership(string blueprint)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.2.7.2");
            var owner=factory.CreateEntity(blueprint);zone.AddEntity(owner,10,10);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);
            Assert.NotNull(recipe.ModelId);Assert.IsTrue(recipe.Transient);Assert.IsFalse(recipe.Batched);
            string glyph=owner.GetPart<RenderPart>().RenderString;
            owner.GetPart<RenderPart>().RenderString="?";
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
            owner.GetPart<RenderPart>().RenderString=glyph;
            zone.RemoveEntity(owner);Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId);
        }

        [TestCase("GinFrog")] [TestCase("PrickleBrowGecko")]
        [TestCase("Torch")] [TestCase("DriedMeat")] [TestCase("HealingTonic")]
        public void MovingTheSameActorOrSupplyDoesNotRerollItsAuthoredBody(string blueprint)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.2.7.2");
            var owner=factory.CreateEntity(blueprint);zone.AddEntity(owner,10,10);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            string model=SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId;
            Assert.NotNull(model);
            for(int x=11;x<25;x++)
            {
                Assert.IsTrue(zone.MoveEntity(owner,x,10));
                Assert.AreEqual(model,SpawnRing3DRecipes.Resolve(zone,owner,catalog).ModelId,"Native movement must not replace the body's variant.");
            }
        }

        [Test]
        public void ActualRimBenchGeometryFacesInwardAndNeverMutatesTheBorrowedMesh()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.4.11.0"))
            {
                var zone=new Zone("Overworld.4.11.0");f.Zone=zone;f.Bind(zone);f.Set("FullReveal",true);
                var bench=f.Add("OverwritPilgrimBench",20,10);f.Refresh();
                var recipe=SpawnRing3DRecipes.Resolve(zone,bench,f.Library.Definition);
                Assert.AreEqual(3,recipe.QuarterTurns,"Eastern boundary furniture faces west into the blank.");
                var mesh=f.Library.FindModel(recipe.ModelId).GetComponent<MeshFilter>().sharedMesh;
                var before=mesh.vertices.ToArray();
                var root=f.View(bench);var center=Village3DProjection.CellCentre(20,10);
                var upper=root.GetComponentsInChildren<MeshFilter>(true)
                    .SelectMany(filter=>filter.sharedMesh.vertices.Select(v=>filter.transform.TransformPoint(v)-center))
                    .Where(v=>v.y>.45f).ToArray();
                Assert.IsNotEmpty(upper);Assert.IsTrue(upper.All(v=>v.x>.10f),"The backrest belongs on the inhabited/eastern side.");
                f.Refresh(new HashSet<int>());CollectionAssert.AreEqual(before,mesh.vertices);
            }
        }

        [TestCase("OverwritNewGrowth")] [TestCase("OverwritWaymarker")] [TestCase("OverwritPilgrimBench")]
        public void NativeDestructionRemovesOnlyTheDamagedOverwritOwnerAndDisplaysItsWreckage(string blueprint)
        {
            var previous=DestructionSystem.EntityFactoryRef;
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.4.11.0"))
            {
                try
                {
                    DestructionSystem.EntityFactoryRef=f.Factory;f.Set("FullReveal",true);
                    var owner=f.Add(blueprint,20,10);var control=f.Add(blueprint,60,20);f.Refresh();
                    Assert.IsTrue(f.Rendered(owner));
                    Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,1000,null,f.Zone));
                    f.Refresh(new HashSet<int>());
                    Assert.IsFalse(f.Rendered(owner));Assert.IsTrue(f.Rendered(control));
                    if(blueprint=="OverwritWaymarker")
                        Assert.IsTrue(f.Rendered(f.Zone.GetCell(20,10).Objects.Single(e=>e.BlueprintName=="Rubble")));
                }
                finally{DestructionSystem.EntityFactoryRef=previous;}
            }
        }

        private static IEnumerable<string> Addresses()
        {
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {string id=WorldMap.ToZoneID(x,y);if(OverwritCompositionPlan.IsWildernessZone(id))yield return id;}
            for(int depth=0;depth<3;depth++)yield return "Overworld.2.7."+depth;
        }
        private static int PatchVertexCount(SpawnRing3DIntegrationFixture fixture)
            =>fixture.Root.GetComponentsInChildren<MeshFilter>(true)
                .Where(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith("Ring patch",StringComparison.Ordinal))
                .Sum(f=>f.sharedMesh.vertexCount);
    }
}
