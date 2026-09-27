using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeSeventhGrassTests
    {
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ActualGrassKeepsHeightButHasAReadableBroadFootprint(int variant)
        {
            var kit=ReferenceGladeVoxelLibrary.Load();kit.Validate();
            var entry=kit.Find(ReferenceGladeVoxelLibrary.ModelId("green-grass",variant));Assert.NotNull(entry);
            var bounds=entry.Mesh.bounds;
            Assert.That(bounds.size.x,Is.InRange(.89f,1.0f),"Actual sixth image read as narrow needles.");
            Assert.That(bounds.max.y,Is.InRange(.40f,.425f));
            Assert.That(bounds.size.z,Is.InRange(.49f,.58f),"Widen the across-screen silhouette without inflating depth.");
            Assert.AreEqual(21*24,entry.Mesh.vertexCount,"Keep the bounded seven-finger recipe, not a dense hedge mat.");
            Assert.AreSame(entry.Mesh,entry.Prefab.GetComponent<UnityEngine.MeshFilter>().sharedMesh);
            Assert.AreEqual(entry.Spec.boundsSize,bounds.size);Assert.AreEqual(entry.Spec.boundsCenter,bounds.center);
        }
    }
}
