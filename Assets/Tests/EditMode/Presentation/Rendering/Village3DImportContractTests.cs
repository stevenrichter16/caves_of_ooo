using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Measures imported geometry, not exporter settings. Asymmetric
    /// axis lengths catch sign swaps, mirrored north and centimetre/metre drift.</summary>
    public sealed class Village3DImportContractTests
    {
        const string Path = "Assets/Art3D/Village/Validation/axis_probe.fbx";

        [TestCase("X_end_two_discs", 2f, .05f, 0f)]
        [TestCase("Y_end_square", 0f, .1f, 3f)]
        [TestCase("Z_end_ball", 0f, 4f, 0f)]
        public void ExportedAxesArriveInTheIntendedUnityDirection(string name, float x, float y, float z)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.NotNull(source, "Real imported FBX is required.");
            var meshes = source.GetComponentsInChildren<MeshFilter>(true)
                .Where(m => m.name.StartsWith(name, System.StringComparison.Ordinal)).ToArray();
            Assert.AreEqual(1, meshes.Length, "Axis probe must have one distinct marker.");
            Vector3 centre = meshes[0].transform.TransformPoint(meshes[0].sharedMesh.bounds.center);
            Assert.That(Vector3.Distance(new Vector3(x,y,z), centre), Is.LessThan(.002f));
        }

        [Test]
        public void MetreCubeHasUnitDimensionsAndRestsOnTheGround()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.NotNull(source);
            var renderer = source.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name.StartsWith("one_metre_cube", System.StringComparison.Ordinal));
            Assert.That(Vector3.Distance(Vector3.one, renderer.bounds.size), Is.LessThan(.002f));
            Assert.That(renderer.bounds.min.y, Is.EqualTo(0).Within(.002f));
            Assert.That(renderer.bounds.center.y, Is.EqualTo(.5f).Within(.002f));
            Assert.AreEqual(0, source.GetComponentsInChildren<Camera>(true).Length);
            Assert.AreEqual(0, source.GetComponentsInChildren<Light>(true).Length);
        }
    }
}
