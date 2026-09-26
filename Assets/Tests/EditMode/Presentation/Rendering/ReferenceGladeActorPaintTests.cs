#if UNITY_EDITOR
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeActorPaintTests
    {
        [TestCase("Player")][TestCase("Warden")][TestCase("Villager")]
        [TestCase("MarlbackScrabbler")][TestCase("MarlbackGleaner")][TestCase("MarlbackTunnelguard")]
        [TestCase("MarlbackWallkeeper")][TestCase("MarlbackBreacher")]
        public void LocalActorPaintIsPersistentScopedAndPreservesTheActualAdoptedRig(string blueprint)
        {
            Mesh original=null;Vector2[] originalUvs=null;string modelId=null;
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var owner=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(owner);f.Refresh();
                modelId=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId;
                var source=f.Library.FindModel(modelId).GetComponentInChildren<SkinnedMeshRenderer>();Assert.NotNull(source);
                original=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath).Bindings.Single(b=>b.Source==source.sharedMesh).Voxel;originalUvs=original.uv;
                var skin=f.View(owner).GetComponentInChildren<SkinnedMeshRenderer>();var painted=skin.sharedMesh;
                Assert.That(AssetDatabase.GetAssetPath(painted),Does.StartWith("Assets/Resources/ReferenceGlade3D/ActorPaint/"));Assert.AreNotSame(original,painted);
                CollectionAssert.AreEqual(original.vertices,painted.vertices);CollectionAssert.AreEqual(original.normals,painted.normals);
                CollectionAssert.AreEqual(original.boneWeights,painted.boneWeights);CollectionAssert.AreEqual(original.bindposes,painted.bindposes);
                Assert.AreEqual(original.bounds,painted.bounds);Assert.AreEqual(original.subMeshCount,painted.subMeshCount);
                for(int i=0;i<original.subMeshCount;i++)CollectionAssert.AreEqual(original.GetTriangles(i),painted.GetTriangles(i));
                var kit=ReferenceGladeVoxelLibrary.Load();Assert.AreSame(kit.Material.GetTexture("_BaseMap"),skin.sharedMaterial.GetTexture("_BaseMap"));
                int body=blueprint=="Player"?16:blueprint=="Warden"||blueprint=="Villager"?11:18;
                int accent=blueprint.StartsWith("Marlback")?21:17;
                CollectionAssert.AreEquivalent(new[]{body,accent},painted.uv.Select(uv=>Mathf.FloorToInt(uv.x*24)).Distinct());
                Assert.IsTrue(f.Pick(owner,out var point));f.Refresh();Assert.AreSame(painted,f.View(owner).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
                if(blueprint=="Player")
                {
                    int head=System.Array.FindIndex(skin.bones,b=>b.name=="Head");Assert.GreaterOrEqual(head,0);
                    var weights=painted.boneWeights;var uv=painted.uv;int heads=0;
                    for(int i=0;i<weights.Length;i++)if(weights[i].boneIndex0==head&&weights[i].weight0>.5f){Assert.AreEqual(17,Mathf.FloorToInt(uv[i].x*24));heads++;}
                    Assert.Greater(heads,0);var item=f.Equip(owner,"Dagger");f.Refresh();Assert.IsTrue(f.Equipment(owner,item,out var gear));
                    Assert.IsFalse(gear.GetComponentsInChildren<MeshFilter>(true).Any(m=>AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/Resources/ReferenceGlade3D/ActorPaint/")));
                }
                owner.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(owner));Assert.IsFalse(f.Rendered(owner));
            }
            CollectionAssert.AreEqual(originalUvs,original.uv,"The global adopted mesh is borrowed, never repainted.");
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var owner=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(owner);f.Refresh();
                if(blueprint=="Warden"||blueprint=="Villager")Assert.IsFalse(f.Authored(owner));
                else Assert.AreSame(original,f.View(owner).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
            }
        }
    }
}
#endif
