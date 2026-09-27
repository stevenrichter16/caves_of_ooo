#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeHumanoidImportTests
    {
        [TestCase(false)][TestCase(true)]
        public void RepeatedWriteAndReimportReplacesTheEntireOwnedVertexBufferAndPreservesSource(bool existingBuffer)
        {
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var prefab=library.FindModel("ring-player");var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var source=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath).Resolve(skin.sharedMesh);
            var sourceVertices=source.vertices;var sourceWeights=source.boneWeights;var sourceUv=source.uv;
            Assert.AreEqual(2116,source.vertexCount,"Exact current borrowed coarse rig is the regression premise.");
            string path="Assets/Tests/EditMode/Presentation/Rendering/_owned_humanoid_import_"+Guid.NewGuid().ToString("N")+".asset";
            GameObject cube=null;Mesh owned=null;bool published=false;
            try
            {
                cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
                var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.ReferenceGladeHumanoidMeshBuilder")).FirstOrDefault(t=>t!=null);
                Assert.NotNull(type);var prepare=type.GetMethod("Prepare",BindingFlags.NonPublic|BindingFlags.Static);Assert.NotNull(prepare);
                var plan=prepare.Invoke(null,new object[]{"ring-player",prefab,skin,source,cube.GetComponent<MeshFilter>().sharedMesh});
                var fill=plan.GetType().GetMethod("Fill",BindingFlags.NonPublic|BindingFlags.Instance);
                Assert.NotNull(fill,"The mesh writer must replace its actual native buffers, not only serialized submesh metadata.");
                owned=existingBuffer?Object.Instantiate(source):new Mesh();owned.name="Owned humanoid import regression";
                AssetDatabase.CreateAsset(owned,path);published=true;string guid=AssetDatabase.AssetPathToGUID(path);
                Vector3[] written=null;
                for(int import=0;import<2;import++)
                {
                    fill.Invoke(plan,new object[]{owned});EditorUtility.SetDirty(owned);AssetDatabase.SaveAssetIfDirty(owned);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    owned=AssetDatabase.LoadAssetAtPath<Mesh>(path);Assert.NotNull(owned);
                    Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path),"Existing asset identity is retained on reimport.");
                    Assert.AreEqual(384,owned.vertexCount);Assert.AreEqual(384,owned.vertices.Length);Assert.AreEqual(384,owned.boneWeights.Length);
                    Assert.AreEqual(576,owned.GetIndexCount(0));Assert.Less(owned.triangles.Max(),384);
                    CollectionAssert.AreEqual(source.bindposes,owned.bindposes);
                    if(written==null)written=owned.vertices;else CollectionAssert.AreEqual(written,owned.vertices,"Repeated import is stable.");
                }
                CollectionAssert.AreEqual(sourceVertices,source.vertices);CollectionAssert.AreEqual(sourceWeights,source.boneWeights);CollectionAssert.AreEqual(sourceUv,source.uv);
            }
            finally
            {
                if(published)AssetDatabase.DeleteAsset(path);else if(owned!=null)Object.DestroyImmediate(owned);
                if(cube!=null)Object.DestroyImmediate(cube);
            }
        }
    }
}
#endif
