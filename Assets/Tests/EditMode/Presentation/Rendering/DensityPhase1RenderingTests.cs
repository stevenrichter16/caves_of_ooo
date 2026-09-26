using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class DensityPhase1RenderingTests
    {
        [TestCase("OilSlick", "ring-tar-seep-")]
        [TestCase("OilSeep", "ring-tar-seep-")]
        [TestCase("ConvalescencePool", "stillleaf-spring-")]
        [TestCase("AcidPool", "density-pool-acid")]
        [TestCase("MemoryBathPool", "density-pool-memory")]
        [TestCase("MirrorMucilagePool", "density-pool-mirror")]
        [TestCase("MarketStall", "cinderhold-stall-")]
        public void RestoredSurfacesAndMarketRetainTheirExactNativeOwnerAcrossPresenters(string blueprint, string prefix)
        {
            var factory = GrovelandsCompositionTests.Factory();
            var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            foreach (string id in new[] { "Overworld.4.6.0", "Overworld.4.6.2", "Overworld.8.16.0", "Overworld.2.7.1", "Overworld.5.4.1" })
            {
                var zone = new Zone(id); var owner = factory.CreateEntity(blueprint);
                Assert.IsTrue(zone.AddEntity(owner, 20, 10));
                int version = zone.EntityVersion; var liquid = owner.GetPart<LiquidPoolPart>();
                string liquidId = liquid?.LiquidId; int? volume = liquid?.Volume;
                var render = owner.GetPart<RenderPart>(); string glyph = render.RenderString, color = render.ColorString;
                var recipe = SpawnRing3DRecipes.Resolve(zone, owner, library.Definition);
                Assert.NotNull(recipe.ModelId, id + " " + blueprint + ": " + recipe.Failure);
                StringAssert.StartsWith(prefix, recipe.ModelId); Assert.AreSame(owner, recipe.Owner);
                Assert.NotNull(library.FindModel(recipe.ModelId)); Assert.NotNull(library.Definition.FindModel(recipe.ModelId));
                Assert.AreSame(library.FindModel(recipe.ModelId), library.FindModel(recipe.ModelId), "Cached asset identity");
                Assert.AreEqual(version, zone.EntityVersion); Assert.AreSame(liquid, owner.GetPart<LiquidPoolPart>());
                Assert.AreEqual(liquidId, liquid?.LiquidId); Assert.AreEqual(volume, liquid?.Volume);
                Assert.AreEqual(glyph, render.RenderString); Assert.AreEqual(color, render.ColorString);
                render.Visible = false; Assert.IsNull(SpawnRing3DRecipes.Resolve(zone, owner, library.Definition).ModelId);
                render.Visible = true; zone.RemoveEntity(owner);
                Assert.IsNull(SpawnRing3DRecipes.Resolve(zone, owner, library.Definition).ModelId);
            }
        }

        [TestCase("AcidPool", 57)]
        [TestCase("MemoryBathPool", 122)]
        [TestCase("MirrorMucilagePool", 26)]
        public void NewPoolSwatchesReuseFlatGeometryWithoutRecoloringTheSource(string blueprint, int palette)
        {
            var zone = new Zone("Overworld.4.6.0"); var owner = GrovelandsCompositionTests.Factory().CreateEntity(blueprint);
            zone.AddEntity(owner, 20, 10); var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var recipe = SpawnRing3DRecipes.Resolve(zone, owner, library.Definition); Assert.NotNull(recipe.ModelId, recipe.Failure);
            var prefab = library.FindModel(recipe.ModelId); var mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            var source = StillleafVoxelLibrary.Load().Find(StillleafVoxelLibrary.ModelId("spring", 0)).Mesh;
            Assert.AreNotSame(source, mesh); CollectionAssert.AreEqual(source.vertices, mesh.vertices);
            CollectionAssert.AreEqual(source.triangles, mesh.triangles); Assert.AreEqual(source.bounds, mesh.bounds);
            Assert.AreEqual(24, mesh.vertexCount); Assert.AreEqual(1, mesh.uv.Distinct().Count());
            Assert.AreEqual(new Vector2((palette % 16 + .5f) / 16f, (palette / 16 + .5f) / 8f), mesh.uv[0]);
            Assert.AreEqual(new Vector2((123 % 16 + .5f) / 16f, (123 / 16 + .5f) / 8f), source.uv[0]);
            Assert.AreEqual("entity", library.Definition.FindModel(recipe.ModelId).kind);
            Assert.AreSame(library.WorldMaterial, prefab.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
        }

        [TestCase("Overworld.4.6.0")]
        [TestCase("Overworld.8.16.0")]
        [TestCase("Overworld.2.7.1")]
        public void MixedRestoredOwnersBatchWithoutMissingVoxelMappings(string id)
        {
            using (var f = new SpawnRing3DIntegrationFixture(id))
            {
                f.Zone = new Zone(id); int x = 10;
                foreach (string blueprint in new[] { "OilSlick", "OilSeep", "ConvalescencePool", "AcidPool", "MemoryBathPool", "MirrorMucilagePool", "MarketStall" })
                    f.Zone.AddEntity(f.Factory.CreateEntity(blueprint), x++, 10);
                int version = f.Zone.EntityVersion; f.Bind(f.Zone); f.Set("FullReveal", true); f.Refresh();
                Assert.IsTrue(f.Get<bool>("IsReady"), f.Get<string>("Failure"));
                Assert.IsTrue(f.Get<bool>("VoxelPresentationActive"));
                foreach (var owner in f.Zone.GetAllEntities())
                {
                    Assert.IsTrue(f.Authored(owner), owner.BlueprintName);
                    Assert.IsTrue(f.Rendered(owner), owner.BlueprintName);
                }
                Assert.AreEqual(0, f.Get<int>("VoxelMissingMeshCount")); Assert.AreEqual(version, f.Zone.EntityVersion);
            }
        }

        [TestCase("SpikeTrap")][TestCase("BearTrap")][TestCase("FireTrap")][TestCase("PressurePlate")]
        [TestCase("Pillar")][TestCase("GlassblownDrifter")][TestCase("PalimpsestEcho")]
        [TestCase("AmbushBandit")][TestCase("BrassHusk")][TestCase("RuneCultist")]
        public void UnauthoredStampAndTrapArtKeepsTheVisibleNativeFallback(string blueprint)
        {
            var zone = new Zone("Overworld.4.6.0"); var owner = GrovelandsCompositionTests.Factory().CreateEntity(blueprint);
            zone.AddEntity(owner, 20, 10); var render = owner.GetPart<RenderPart>();
            string glyph = render.RenderString, color = render.ColorString;
            Assert.IsTrue(render.Visible); Assert.IsNotEmpty(glyph);
            var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var recipe = SpawnRing3DRecipes.Resolve(zone, owner, library.Definition);
            Assert.IsNull(recipe.ModelId); Assert.AreEqual("unmodeled-native-blueprint", recipe.Failure);
            Assert.AreEqual(glyph, render.RenderString); Assert.AreEqual(color, render.ColorString); Assert.IsTrue(render.Visible);
        }
    }
}
