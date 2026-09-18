#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>A whole-mesh AABB is not a substitute for each draw range's
    /// bounds. This caught first-create versus in-place-rebuild serialization.
    /// These tests read assets; they never regenerate or edit them.</summary>
    public sealed class MorrowfastCoarseSubmeshBoundsTests
    {
        static void DescriptorMatchesIndexedGeometry(Mesh mesh)
        {
            var vertices = mesh.vertices;
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                var descriptor = mesh.GetSubMesh(index); var indices = mesh.GetTriangles(index);
                Assert.Greater(indices.Length, 0); Assert.AreEqual(MeshTopology.Triangles, descriptor.topology);
                Assert.AreEqual(indices.Length, descriptor.indexCount);
                int low = indices.Min(), high = indices.Max();
                Assert.AreEqual(low, descriptor.firstVertex);
                Assert.AreEqual(high - low + 1, descriptor.vertexCount);
                var expected = new Bounds(vertices[indices[0]], Vector3.zero);
                foreach (int vertex in indices) expected.Encapsulate(vertices[vertex]);
                Assert.Greater(expected.size.sqrMagnitude, .000001f, "The positive control must contain real visible extent.");
                Assert.Less(Vector3.Distance(expected.center, descriptor.bounds.center), .00001f, mesh.name + " material bounds center");
                Assert.Less(Vector3.Distance(expected.extents, descriptor.bounds.extents), .00001f, mesh.name + " material bounds extent");
            }
        }
        [Test]
        public void FreshGeneratedGeometryAlreadyHasCorrectMaterialBoundsAsTheControl()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.MorrowfastCoarseVoxelBuilder")).FirstOrDefault(t => t != null);
            Assert.NotNull(type);
            var native = MorrowfastSceneDefinition.Parse(Resources.Load<TextAsset>("SceneArt/Morrowfast/definition").text);
            var mesh = (Mesh)type.GetMethod("CreateModelMesh", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { "ground-patch-0", native, false });
            try { DescriptorMatchesIndexedGeometry(mesh); }
            finally { Object.DestroyImmediate(mesh); }
        }
        [Test]
        public void Every117PublishedScenerySubmeshRetainsItsActualIndexedGeometryBounds()
        {
            var catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            Assert.NotNull(catalog); catalog.Validate(); int checkedMeshes = 0;
            foreach (var row in catalog.Bindings)
            {
                if (!MorrowfastCoarseArtContract.IsVerified(row)) continue;
                DescriptorMatchesIndexedGeometry(row.Voxel); checkedMeshes++;
            }
            Assert.AreEqual(117, checkedMeshes);
        }
    }
}
#endif
