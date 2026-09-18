#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Real imported geometry and native library bindings. These tests
    /// borrow Resources and modify only a disposable instantiated prefab.</summary>
    [Category("VoxelWorldRealArt")]
    public sealed class VoxelWorldImportedArtTests
    {
        const string ToolkitRoot="Assets/Art3D/VoxelWorld/Toolkit/";
        static VoxelWorldMeshCatalog Catalog()
        {
            var c=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            Assert.NotNull(c,"Run the final complete native + toolkit bake before this gate.");c.Validate();return c;
        }
        static GameObject[] Prefabs()
        {
            var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var pilot=Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
            Assert.NotNull(village);Assert.NotNull(ring);Assert.NotNull(pilot);village.Validate();ring.Validate();pilot.Validate();
            return village.Models.Select(m=>m.Prefab).Concat(ring.Models.Select(m=>m.Prefab)).Concat(pilot.Models.Select(m=>m.Prefab))
                .Concat(ring.EquipmentLibrary.Models.Select(m=>m.Prefab)).Distinct().ToArray();
        }
        static Mesh MeshOf(Renderer r)=>r is SkinnedMeshRenderer skin?skin.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;
        static ToolkitBindings Bindings()=>JsonUtility.FromJson<ToolkitBindings>(File.ReadAllText("ArtSource/VoxelTown/native-bindings.json"));
        static Recipes RecipeDocument()=>JsonUtility.FromJson<Recipes>(File.ReadAllText("ArtSource/VoxelTown/Output/native-region/assets-coarse.json"));
        static GameObject Prefab(ToolkitBinding binding)
            =>binding.library=="Village"?Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath).FindModel(binding.model)
            :binding.library=="SpawnRing"?Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).FindModel(binding.model):null;
        static Vector3 P(float[] p)=>new Vector3(p[0],p[1],p[2]);
        static Bounds BoundsOf(IEnumerable<Vector3> points)
        {var array=points.ToArray();Assert.Greater(array.Length,0);var b=new Bounds(array[0],Vector3.zero);foreach(var p in array)b.Encapsulate(p);return b;}
        static IEnumerable<Vector3> Corners(Bounds b)
        {for(int i=0;i<8;i++)yield return new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z);}

        [Test] public void ActualCatalogCoversEveryMeshOfAllNativeLibrariesWithPersistentIdentity()
        {
            var c=Catalog();var prefabs=Prefabs();Assert.Greater(prefabs.Length,300,"Empty/partial libraries are not a complete native conversion.");
            var sources=new HashSet<Mesh>(prefabs.SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).Select(MeshOf).Where(m=>m!=null));
            Assert.Greater(sources.Count,350);CollectionAssert.AreEquivalent(sources,c.Bindings.Select(b=>b.Source));
            foreach(var row in c.Bindings)
            {
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(row.Source,out string guid,out long localId));
                string key=guid+"_"+localId.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace('-','n');
                Assert.AreEqual(key,row.SourceKey);
                Assert.IsTrue(AssetDatabase.Contains(row.Voxel));Assert.IsTrue(row.Voxel.isReadable);
                Assert.AreNotSame(row.Source,row.Voxel);Assert.AreSame(row.Voxel,c.Resolve(row.Source));
                Assert.AreEqual(row.Source.subMeshCount,row.Voxel.subMeshCount);
            }
        }

        [Test] public void EveryToolkitBindingUsesItsActualRecipeTopologyWithUniformNativeFit()
        {
            var c=Catalog();var table=Bindings();var document=RecipeDocument();Assert.AreEqual(1,table.schemaVersion);Assert.AreEqual(1,document.schemaVersion);
            Assert.AreEqual(38,table.bindings.Length,"Current authored native toolkit conversion is 38 model bindings.");
            var keys=new HashSet<string>();int coarse=0;
            foreach(var spec in table.bindings)
            {
                var prefab=Prefab(spec);Assert.NotNull(prefab,spec.model);var filters=prefab.GetComponentsInChildren<MeshFilter>(true);Assert.AreEqual(1,filters.Length,spec.model);
                var filter=filters[0];var row=c.Bindings.Single(b=>b.Source==filter.sharedMesh);Assert.IsTrue(keys.Add(row.SourceKey));
                bool town=MorrowfastCoarseArtContract.IsVerified(row);
                if(town){Assert.AreEqual("Village",spec.library);coarse++;}
                else Assert.AreEqual(ToolkitRoot+row.SourceKey+".asset",AssetDatabase.GetAssetPath(row.Voxel),spec.model+" must use its active recipe override.");
                // Retain full validation of the historical toolkit asset too:
                // town supersession must not destroy or silently rebuild it.
                var recipeMesh=AssetDatabase.LoadAssetAtPath<Mesh>(ToolkitRoot+row.SourceKey+".asset");Assert.NotNull(recipeMesh);
                Assert.AreEqual("CavesOfOoo.VoxelWorld.Toolkit/1",AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(recipeMesh)).userData,
                    "Importer ownership is explicit metadata, not Unity's normalized Mesh.name.");
                var recipe=document.assets.Single(a=>a.id==spec.recipe);Assert.Greater(recipe.triangleMaterials.Length,0);
                Assert.AreEqual(recipe.triangleMaterials.Length*3,recipeMesh.triangles.Length,spec.recipe);
                Assert.AreEqual(recipe.triangleMaterials.Length*2,recipeMesh.vertexCount,spec.recipe+" has four independent vertices per quad.");
                var transform=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var original=BoundsOf(Corners(row.Source.bounds).Select(transform.MultiplyPoint3x4));
                var actual=BoundsOf(recipeMesh.vertices.Select(transform.MultiplyPoint3x4));
                var size=P(recipe.bounds.size);var nativeSize=new Vector3(size.x,size.z,size.y);
                var ratios=new Vector3(actual.size.x/nativeSize.x,actual.size.y/nativeSize.y,actual.size.z/nativeSize.z);
                Assert.Greater(ratios.x,0);Assert.That(ratios.x,Is.EqualTo(ratios.y).Within(.0002f),spec.model);Assert.That(ratios.y,Is.EqualTo(ratios.z).Within(.0002f),spec.model);
                Assert.That(actual.min.y,Is.EqualTo(original.min.y).Within(.0002f),spec.model+" rests on the original bottom plane.");
                Assert.That(actual.center.x,Is.EqualTo(original.center.x).Within(.0002f));Assert.That(actual.center.z,Is.EqualTo(original.center.z).Within(.0002f));
                for(int axis=0;axis<3;axis++)Assert.LessOrEqual(actual.size[axis],original.size[axis]+.0002f,spec.model);
                Assert.That(Mathf.Max(actual.size.x/original.size.x,Mathf.Max(actual.size.y/original.size.y,actual.size.z/original.size.z)),Is.EqualTo(1).Within(.0003f),spec.model+" saturates one extent instead of arbitrary shrinking.");
                if(!town) Assert.That(row.WorldVoxelSize,Is.EqualTo(recipe.voxelSize*ratios.x).Within(.0001f));
            }
            Assert.AreEqual(23,coarse,"Exactly the existing Village recipe subset receives the new native-coordinate overlay.");
        }

        [Test] public void ToolkitFacesKeepIndependentConstantAtlasUvsAndNativeFogMaterials()
        {
            var c=Catalog();
            foreach(var spec in Bindings().bindings)
            {
                var prefab=Prefab(spec);var filter=prefab.GetComponentsInChildren<MeshFilter>(true).Single();var row=c.Bindings.Single(b=>b.Source==filter.sharedMesh);
                var material=filter.GetComponent<MeshRenderer>().sharedMaterial;Assert.NotNull(material);
                foreach(string property in new[]{"_BaseMap","_FogLight","_Transient"})Assert.IsTrue(material.HasProperty(property),spec.model+"/"+property);
                var uv=row.Voxel.uv;Assert.AreEqual(row.Voxel.vertexCount,uv.Length);
                for(int first=0;first<uv.Length;first+=4)
                {
                    Assert.That(uv[first].x,Is.InRange(0f,1f));Assert.That(uv[first].y,Is.InRange(0f,1f));
                    for(int k=1;k<4;k++)Assert.AreEqual(uv[first],uv[first+k],spec.model+" face "+first/4);
                }
            }
        }

        [Test] public void EveryActualSkinnedModelRetainsItsOfflineBoneContractAndLiveBones()
        {
            var c=Catalog();var skins=Prefabs().SelectMany(p=>p.GetComponentsInChildren<SkinnedMeshRenderer>(true)).ToArray();Assert.Greater(skins.Length,20);
            foreach(var skin in skins)
            {
                var row=c.Bindings.Single(b=>b.Source==skin.sharedMesh);Assert.AreEqual(skin.sharedMesh.bindposeCount,skin.bones.Length,skin.name);
                Assert.IsTrue(skin.bones.All(b=>b!=null));Assert.NotNull(row.SourceBindposes);CollectionAssert.AreEqual(row.SourceBindposes,row.Voxel.bindposes);
                var weights=row.Voxel.boneWeights;Assert.AreEqual(row.Voxel.vertexCount,weights.Length);
                foreach(var w in weights)
                {
                    Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.0001f));
                    foreach(var index in new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3})Assert.That(index,Is.InRange(0,skin.bones.Length-1));
                }
                Assert.IsFalse(AssetDatabase.GetAssetPath(row.Voxel).StartsWith(ToolkitRoot,StringComparison.Ordinal),"Static toolkit NPCs must not replace existing native animation rigs.");
            }
        }

        [Test] public void ApplyingToAnActualInstantiatedPrefabLeavesItsBorrowedTemplateAndMaterialsUntouched()
        {
            var c=Catalog();var prefab=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath).FindModel("crate-0");Assert.NotNull(prefab);
            var sourceFilters=prefab.GetComponentsInChildren<MeshFilter>(true);var sources=sourceFilters.Select(f=>f.sharedMesh).ToArray();
            var materials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
            var preview=EditorSceneManager.NewPreviewScene();GameObject instance=null;
            try
            {
                instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
                Assert.Greater(c.Apply(instance),0);Assert.AreEqual(0,c.Apply(instance));
                CollectionAssert.AreEqual(sources,sourceFilters.Select(f=>f.sharedMesh));
                CollectionAssert.AreEqual(materials,instance.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials));
                Assert.AreSame(c.Resolve(sources[0]),instance.GetComponentsInChildren<MeshFilter>(true)[0].sharedMesh);
            }
            finally {if(instance!=null)Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(preview);}
        }
        [Test] public void CorrectedToolkitRegenerationKeepsTheCapturedThirtyEightAssetGuids()
        {
            var c=Catalog();var pin=JsonUtility.FromJson<GuidPin>(File.ReadAllText("Docs/Verification/VoxelWorld/toolkit-guid-pin.json"));
            Assert.AreEqual(1,pin.schemaVersion);Assert.AreEqual(38,pin.rows.Length);
            int coarse=0;
            foreach(var row in pin.rows)
            {
                Assert.AreEqual(row.guid,AssetDatabase.AssetPathToGUID(row.meshAsset),row.meshAsset);
                Assert.AreEqual(row.owner,AssetImporter.GetAtPath(row.meshAsset).userData);
                var binding=c.Bindings.Single(b=>b.SourceKey==row.sourceKey);
                if(MorrowfastCoarseArtContract.IsVerified(binding))coarse++;
                else Assert.AreEqual(row.meshAsset,AssetDatabase.GetAssetPath(binding.Voxel));
            }
            Assert.AreEqual(23,coarse);Assert.AreEqual(15,pin.rows.Length-coarse);
        }
        [Serializable] sealed class ToolkitBindings {public int schemaVersion;public ToolkitBinding[] bindings;}
        [Serializable] sealed class ToolkitBinding {public string library,model,recipe;}
        [Serializable] sealed class Recipes {public int schemaVersion;public Recipe[] assets;}
        [Serializable] sealed class Recipe {public string id;public BoundsSpec bounds;public float voxelSize;public int[] triangleMaterials;}
        [Serializable] sealed class BoundsSpec {public float[] minimum,maximum,size;}
        [Serializable] sealed class GuidPin {public int schemaVersion;public GuidRow[] rows;}
        [Serializable] sealed class GuidRow {public string sourceKey,meshAsset,guid,owner;}
    }
}
#endif
