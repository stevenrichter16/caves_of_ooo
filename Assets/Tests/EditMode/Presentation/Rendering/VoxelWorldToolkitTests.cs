using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class VoxelWorldToolkitTests
    {
        const string Fixture = "{\"schemaVersion\":1,\"palette\":[{\"name\":\"sand\",\"hex\":\"BDA475\"}],\"assets\":[{\"id\":\"voxel-test\",\"vertices\":[[0,0,0],[1,0,0],[1,1,0],[0,1,0]],\"quads\":[[0,1,2,3]],\"quadMaterials\":[0]}]}";
        static Mesh Build(string json, string id, Matrix4x4 transform)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.VoxelWorldToolkitImporter")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Toolkit assets must reach native mesh assets through an explicit importer.");
            var method = type.GetMethod("CreateMesh", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            try { return (Mesh)method.Invoke(null, new object[] { json, id, transform, new[] { new Vector2(.25f,.75f) } }); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static Matrix4x4 NativeAxes => new Matrix4x4(new Vector4(1,0,0,0), new Vector4(0,0,1,0), new Vector4(0,1,0,0), new Vector4(0,0,0,1));
        [Test] public void BlenderQuad_TransformsToNativePlaneWithOutwardWindingAndPaletteUv()
        {
            var mesh = Build(Fixture, "voxel-test", NativeAxes);
            try
            {
                Assert.AreEqual(4, mesh.vertexCount); Assert.AreEqual(6, mesh.triangles.Length);
                Assert.IsTrue(mesh.vertices.All(v => v.y == 0));
                Assert.IsTrue(mesh.normals.All(n => n == Vector3.up));
                Assert.IsTrue(mesh.uv.All(uv => uv == new Vector2(.25f,.75f)));
                var p = mesh.vertices; var t = mesh.triangles;
                Assert.Greater(Vector3.Dot(Vector3.Cross(p[t[1]]-p[t[0]],p[t[2]]-p[t[0]]),Vector3.up),0);
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
        [Test] public void IdentityCoordinates_KeepOriginalWindingInsteadOfBlindlyFlipping()
        {
            var mesh = Build(Fixture, "voxel-test", Matrix4x4.identity);
            try { Assert.IsTrue(mesh.normals.All(n => n == Vector3.forward)); }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
        [Test] public void UniformFit_PreservesAspectRatioAndGroundPivot()
        {
            var mesh = Build(Fixture, "voxel-test", NativeAxes * Matrix4x4.Scale(Vector3.one*.5f));
            try { Assert.AreEqual(new Vector3(.5f,0,.5f),mesh.bounds.size); Assert.AreEqual(0,mesh.bounds.min.y); }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
        [Test] public void MissingRecipe_IsRejectedRatherThanPickingAnArbitraryAsset()
        { Assert.Throws<ArgumentException>(() => Build(Fixture, "voxel-missing", NativeAxes)); }
        [Test] public void BadIndex_IsRejectedBeforePublishingGeometry()
        { Assert.Throws<ArgumentException>(() => Build(Fixture.Replace("[0,1,2,3]","[0,1,2,40]"), "voxel-test", NativeAxes)); }
        [Test] public void UnregisteredPalette_IsRejectedInsteadOfPaintingWrongMaterial()
        { Assert.Throws<ArgumentException>(() => Build(Fixture.Replace("quadMaterials\":[0]","quadMaterials\":[2]"), "voxel-test", NativeAxes)); }
    }
}
