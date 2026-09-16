using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class SpreadVoxelKitTests
    {
        [Test] public void KitHasFourDistinctCoarseVariantsPerFamilyWithOnlyTwoSwatches()
        {
            var kit=SpreadVoxelLibrary.Load();Assert.NotNull(kit);kit.Validate();
            Assert.AreEqual(16,kit.Entries.Length);
            foreach(var e in kit.Entries)
            {
                Assert.That(e.Mesh.uv.Distinct().Count(),Is.InRange(1,2),e.Id);
                Assert.LessOrEqual(e.Mesh.vertexCount,216,e.Id);
                Assert.LessOrEqual(e.Mesh.bounds.size.x,1);Assert.LessOrEqual(e.Mesh.bounds.size.z,1);
                Assert.Greater(e.Mesh.bounds.size.y,.2f);
                Assert.AreEqual(1,e.Prefab.GetComponentsInChildren<MeshRenderer>().Length);
            }
            foreach(string family in new[]{"hedge","barley","flowers","reeds"})
            {
                var shapes=Enumerable.Range(0,4).Select(i=>string.Join(";",kit.Find(SpreadVoxelLibrary.ModelId(family,i)).Mesh.vertices.Select(v=>v.ToString("F3")))).ToArray();
                Assert.AreEqual(4,shapes.Distinct().Count(),family);
            }
        }
        [Test] public void AlreadyVoxelKitIsBorrowedWithoutMissingMappingOrWorldMutation()
        {
            bool old=Village3DSettings.Enabled;Village3DSettings.Enabled=true;
            try
            {
                var z=new Zone(SpreadCompositionTests.Id);var p=VoxelWorldPresentation.ForZone(z);
                var kit=SpreadVoxelLibrary.Load();
                foreach(var e in kit.Entries)Assert.AreSame(e.Mesh,p.Resolve(e.Mesh));
                Assert.AreEqual(0,p.MissingMeshCount);Assert.AreEqual(0,z.EntityCount);
                Assert.IsNull(VoxelWorldPresentation.ForZone(new Zone("Overworld.18.18.1")));
            }
            finally{Village3DSettings.Enabled=old;}
        }
    }
}
