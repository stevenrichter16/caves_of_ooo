using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class QuillholdVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "path", "wall", "shelf", "desk", "scribe", "table" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = QuillholdVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(28, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(QuillholdVoxelKitLibrary.ModelId(family, variant));
                    Assert.NotNull(entry, family);
                    Assert.That(entry.Mesh.uv.Distinct().Count(), Is.InRange(1, 2), entry.Id);
                    Assert.That(entry.Mesh.vertexCount, Is.InRange(24, 240), entry.Id);
                    Assert.GreaterOrEqual(entry.Mesh.bounds.min.x, -.5001f, entry.Id);
                    Assert.LessOrEqual(entry.Mesh.bounds.max.x, .5001f, entry.Id);
                    Assert.GreaterOrEqual(entry.Mesh.bounds.min.z, -.5001f, entry.Id);
                    Assert.LessOrEqual(entry.Mesh.bounds.max.z, .5001f, entry.Id);
                    Assert.AreEqual(1, entry.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length, entry.Id);
                    Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<Collider>(true).Length, entry.Id);
                    Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length, entry.Id);
                    Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<Light>(true).Length, entry.Id);
                    Assert.AreSame(entry.Mesh, entry.Prefab.GetComponent<MeshFilter>().sharedMesh);
                    Assert.AreSame(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial,
                        entry.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
                    Assert.AreEqual(entry.Mesh.bounds.center, entry.Spec.boundsCenter, entry.Id);
                    Assert.AreEqual(entry.Mesh.bounds.size, entry.Spec.boundsSize, entry.Id);
                    Assert.AreEqual(entry.Mesh.triangles.Length / 3, entry.Spec.triangles, entry.Id);
                    Assert.AreEqual(family == "ground" || family == "path" ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("Floor","ground")] [TestCase("RoadStone","path")] [TestCase("QuillholdArchiveWall","wall")]
        [TestCase("QuillholdArchiveShelf","shelf")] [TestCase("QuillholdCopyDesk","desk")] [TestCase("Scribe","scribe")]
        [TestCase("RecensionScribe",null)] [TestCase("Bookshelf",null)] [TestCase("SealedArchiveShelf",null)] [TestCase("LibraryMemoryMarbleWall",null)] [TestCase(null,null)]
        public void ExactOwnersCannotAcquireUnrelatedArchiveOrQuestArt(string bp,string family)
        {Assert.AreEqual(family,QuillholdVoxelKitLibrary.Family(bp));}
        [Test] public void WallShelfDeskHaveDifferentReadableHeightsAndNoProtectedLibrarySilhouette()
        {
            var kit=QuillholdVoxelKitLibrary.Load();kit.Validate();
            for(int v=0;v<4;v++)
            {
                var wall=kit.Find(QuillholdVoxelKitLibrary.ModelId("wall",v)).Mesh;
                var shelf=kit.Find(QuillholdVoxelKitLibrary.ModelId("shelf",v)).Mesh;
                var desk=kit.Find(QuillholdVoxelKitLibrary.ModelId("desk",v)).Mesh;
                Assert.That(wall.bounds.size.x,Is.EqualTo(1).Within(.001));Assert.That(wall.bounds.size.z,Is.EqualTo(1).Within(.001));Assert.That(wall.bounds.max.y,Is.InRange(.9f,1.15f));
                Assert.That(shelf.bounds.max.y,Is.InRange(.7f,.9f));Assert.That(desk.bounds.max.y,Is.InRange(.42f,.65f));
                Assert.Greater(shelf.vertexCount,desk.vertexCount,"Coarse book bundles and shelf levels must read separately from the low copying surface.");
                Assert.That(kit.Find(QuillholdVoxelKitLibrary.ModelId("scribe",v)).Mesh.bounds.max.y,Is.InRange(1.3f,1.7f));
            }
        }
        [Test] public void InvalidArtLookupCannotBorrowAnotherArea()
        {
            Assert.Throws<ArgumentException>(()=>QuillholdVoxelKitLibrary.ModelId("not-a-family",0));Assert.Throws<ArgumentOutOfRangeException>(()=>QuillholdVoxelKitLibrary.ModelId("shelf",4));
            var kit=QuillholdVoxelKitLibrary.Load();Assert.IsNull(kit.Find(null));Assert.IsNull(kit.Find("firsttent-ground-0"));
        }
        [Test] public void QuietGroundVariantsNeverIntroduceVisibleCheckerboarding()
        {
            var kit=QuillholdVoxelKitLibrary.Load();kit.Validate();
            foreach(string family in new[]{"ground","path"})for(int variant=0;variant<4;variant++)
            {
                var mesh=kit.Find(QuillholdVoxelKitLibrary.ModelId(family,variant)).Mesh;Assert.AreEqual(1,mesh.uv.Distinct().Count());Assert.AreEqual(24,mesh.vertexCount);
                Assert.IsTrue(mesh.vertices.Where((v,i)=>mesh.normals[i].y>.9f).All(v=>Mathf.Abs(v.y)<.0001f));
                Assert.AreEqual(kit.Find(QuillholdVoxelKitLibrary.ModelId(family,0)).Mesh.uv[0],mesh.uv[0]);
            }
        }
        [Test] public void CommunalTableHasItsOwnCoarseFamilyInsteadOfFolioDeskArt()
        {
            Assert.AreEqual("table",QuillholdVoxelKitLibrary.Family("QuillholdRefectoryTable"));
            var kit=QuillholdVoxelKitLibrary.Load();kit.Validate();
            for(int v=0;v<4;v++)
            {
                var table=kit.Find(QuillholdVoxelKitLibrary.ModelId("table",v)).Mesh;var desk=kit.Find(QuillholdVoxelKitLibrary.ModelId("desk",v)).Mesh;
                Assert.That(table.bounds.size.z,Is.InRange(.8f,1f));Assert.Greater(table.bounds.size.z,desk.bounds.size.z);Assert.LessOrEqual(table.vertexCount,144);Assert.That(table.uv.Distinct().Count(),Is.InRange(1,2));
                Assert.That(table.bounds.max.y,Is.InRange(.5f,.7f));Assert.LessOrEqual(table.bounds.max.x,.5001f);Assert.GreaterOrEqual(table.bounds.min.x,-.5001f);
            }
        }
    }
}
