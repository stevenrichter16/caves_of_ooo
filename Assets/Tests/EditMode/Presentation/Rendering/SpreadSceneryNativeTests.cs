using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadSceneryNativeTests
    {
        [TestCase("spread-scenery-berrybush-0")]
        [TestCase("spread-scenery-berrybush-1")]
        [TestCase("spread-scenery-signpost-0")]
        [TestCase("spread-scenery-signpost-1")]
        [TestCase("spread-scenery-hollowstump-0")]
        [TestCase("spread-scenery-hollowstump-1")]
        [TestCase("spread-scenery-beehive-0")]
        [TestCase("spread-scenery-beehive-1")]
        [TestCase("spread-scenery-rivershrine-0")]
        [TestCase("spread-scenery-rivershrine-1")]
        [TestCase("spread-scenery-flowerfield-0")]
        [TestCase("spread-scenery-flowerfield-1")]
        [TestCase("spread-scenery-stonefloor-0")]
        [TestCase("spread-scenery-stonefloor-1")]
        [TestCase("spread-scenery-stonewall-0")]
        [TestCase("spread-scenery-stonewall-1")]
        [TestCase("spread-scenery-chair-0")]
        [TestCase("spread-scenery-chair-1")]
        [TestCase("spread-scenery-bed-0")]
        [TestCase("spread-scenery-bed-1")]
        [TestCase("spread-scenery-well-0")]
        [TestCase("spread-scenery-well-1")]
        [TestCase("spread-scenery-oven-0")]
        [TestCase("spread-scenery-oven-1")]
        [TestCase("spread-scenery-watchlantern-0")]
        [TestCase("spread-scenery-watchlantern-1")]
        [TestCase("spread-scenery-campfiregroundmarker-0")]
        [TestCase("spread-scenery-campfiregroundmarker-1")]
        [TestCase("spread-scenery-wellgroundmarker-0")]
        [TestCase("spread-scenery-wellgroundmarker-1")]
        [TestCase("spread-scenery-ovengroundmarker-0")]
        [TestCase("spread-scenery-ovengroundmarker-1")]
        [TestCase("spread-scenery-lanterngroundmarker-0")]
        [TestCase("spread-scenery-lanterngroundmarker-1")]
        [TestCase("spread-scenery-shrine-0")]
        [TestCase("spread-scenery-shrine-1")]
        [TestCase("spread-scenery-alchemyshelf-0")]
        [TestCase("spread-scenery-alchemyshelf-1")]
        [TestCase("spread-scenery-alchemystill-0")]
        [TestCase("spread-scenery-alchemystill-1")]
        [TestCase("spread-scenery-tinkersforge-0")]
        [TestCase("spread-scenery-tinkersforge-1")]
        [TestCase("spread-scenery-oldstump-0")]
        [TestCase("spread-scenery-oldstump-1")]
        [TestCase("spread-scenery-pressureplate-0")]
        [TestCase("spread-scenery-pressureplate-1")]
        [TestCase("spread-scenery-beartrap-0")]
        [TestCase("spread-scenery-beartrap-1")]
        [TestCase("spread-scenery-firetrap-0")]
        [TestCase("spread-scenery-firetrap-1")]
        [TestCase("spread-scenery-spiketrap-0")]
        [TestCase("spread-scenery-spiketrap-1")]
        [TestCase("spread-scenery-weaponrack-0")]
        [TestCase("spread-scenery-weaponrack-1")]
        [TestCase("spread-scenery-watchlantern-unlit-0")]
        [TestCase("spread-scenery-watchlantern-unlit-1")]
        [TestCase("spread-scenery-knotflaxsnare-0")]
        [TestCase("spread-scenery-knotflaxsnare-1")]
        public void EveryExactModelIsPersistentDrawableAndUsesBorrowedApprovedPalette(string id)
        {
            var library=SpreadScenery3DLibrary.Load();Assert.NotNull(library,"Explicit native scenery import is required.");
            Assert.DoesNotThrow(()=>library.Validate());var entry=library.Find(id);Assert.NotNull(entry,id);
            Assert.AreEqual(id.StartsWith("spread-scenery-stonefloor-",StringComparison.Ordinal)?"ground":"entity",entry.Spec.kind);
            Assert.AreSame(entry,library.Find(id));Assert.True(library.ContainsMesh(entry.Mesh));
            Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,library.Material);
            Assert.AreSame(entry.Mesh,entry.Prefab.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreSame(library.Material,entry.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
            var source=JsonUtility.FromJson<SpreadScenerySource>(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/SpreadScenery3D/kit.json"))));
            var authored=source.models.Single(m=>m.id==id);
            Assert.AreEqual(authored.boxes.Length*24,entry.Mesh.vertexCount);
            Assert.AreEqual(authored.boxes.Length*12,entry.Spec.triangles);
            var vertices=entry.Mesh.vertices;var paint=entry.Mesh.uv;
            for(int boxIndex=0;boxIndex<authored.boxes.Length;boxIndex++)
            {
                var box=authored.boxes[boxIndex];var corners=new System.Collections.Generic.HashSet<Vector3>();
                for(int vertex=boxIndex*24;vertex<(boxIndex+1)*24;vertex++)
                {
                    var point=vertices[vertex];corners.Add(point);
                    for(int axis=0;axis<3;axis++)
                    {
                        float low=box.center[axis]-box.size[axis]*.5f,high=box.center[axis]+box.size[axis]*.5f;
                        Assert.True(Mathf.Abs(point[axis]-low)<.000001f||Mathf.Abs(point[axis]-high)<.000001f,id+" box="+boxIndex);
                    }
                    Assert.AreEqual((box.color+.5f)/24f,paint[vertex].x,.000001f,id+" exact source swatch");
                    Assert.AreEqual(.5f,paint[vertex].y);
                }
                Assert.AreEqual(8,corners.Count,id+" complete cuboid corners");
            }
            Assert.Zero(entry.Prefab.GetComponentsInChildren<Collider>(true).Length);
            Assert.Zero(entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length);
        }
        [Test] public void UnknownModelAndForeignMeshAreNeverBorrowed()
        {
            var library=SpreadScenery3DLibrary.Load();Assert.NotNull(library);library.Validate();
            Assert.Null(library.Find(null));Assert.Null(library.Find("spread-scenery-berrybush-2"));Assert.False(library.ContainsMesh(null));
            var foreign=new Mesh();try{Assert.False(library.ContainsMesh(foreign));}finally{UnityEngine.Object.DestroyImmediate(foreign);}
        }
        [TestCase("hash")][TestCase("missing")][TestCase("duplicate")][TestCase("wrong-mesh")]
        [TestCase("wrong-material")][TestCase("wrong-spec")]
        public void AlteredLibraryCannotPublishAValidCache(string change)
        {
            var original=SpreadScenery3DLibrary.Load();Assert.NotNull(original);original.Validate();
            var library=UnityEngine.Object.Instantiate(original);
            library.Entries=original.Entries.Select(e=>new SpreadScenery3DLibrary.Entry{Id=e.Id,Mesh=e.Mesh,Prefab=e.Prefab,Spec=e.Spec}).ToArray();
            try
            {
                switch(change)
                {
                    case "hash":library.SourceSha256="changed";break;
                    case "missing":library.Entries=library.Entries.Skip(1).ToArray();break;
                    case "duplicate":library.Entries[1].Id=library.Entries[0].Id;break;
                    case "wrong-mesh":library.Entries[0].Mesh=library.Entries[1].Mesh;break;
                    case "wrong-material":library.Material=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial;break;
                    case "wrong-spec":library.Entries[0].Spec=library.Entries[1].Spec;break;
                }
                Assert.Throws<InvalidOperationException>(()=>library.Validate());
                Assert.Throws<InvalidOperationException>(()=>library.Find(original.Entries[0].Id));
                Assert.AreSame(original.Entries[0].Mesh,original.Find(original.Entries[0].Id).Mesh);
            }
            finally {UnityEngine.Object.DestroyImmediate(library);}
        }
    }
}
