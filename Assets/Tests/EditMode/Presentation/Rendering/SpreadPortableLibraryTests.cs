using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadPortableLibraryTests
    {
        [Test]
        public void AdoptedLibraryContainsEveryExactSourceIdentityWithOwnedNativeBuffers()
        {
            var library = SpreadPortable3DLibrary.Load();
            Assert.NotNull(library, "Portable source candidates have not been adopted natively.");
            library.Validate();
            Assert.AreEqual(SpreadPortableModelIds.All.Count, library.Entries.Length);
            foreach (string id in SpreadPortableModelIds.All)
            {
                var entry = library.Find(id);
                Assert.NotNull(entry, id); Assert.AreSame(entry.Mesh, entry.Prefab.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
                Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(0, entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length);
                Assert.Greater(entry.Mesh.vertexCount, 0);
            }
        }
        [Test]
        public void NativeBuffersMatchReviewedSourceForEveryPortable()
        {
            var library=SpreadPortable3DLibrary.Load(); Assert.NotNull(library);
            var pack=JsonUtility.FromJson<SpreadPortableSource>(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/SpreadPortable3D/portable-source.json"))));
            pack.Validate();
            foreach(var source in pack.models)
            {
                var mesh=library.Find(source.id).Mesh;
                var expected=Enumerable.Range(0,source.paletteIndices.Length).Select(i=>new Vector3(source.positions[i*3],source.positions[i*3+1],source.positions[i*3+2])).ToArray();
                Assert.True(mesh.vertices.SequenceEqual(expected),source.id+" positions");
                Assert.True(mesh.triangles.SequenceEqual(source.triangles),source.id+" triangles");
                Assert.True(mesh.uv.SequenceEqual(source.paletteIndices.Select(i=>new Vector2((i+.5f)/pack.palette.Length,.5f))),source.id+" palette UV");
            }
            var pixels=library.Palette.GetPixels();
            for(int i=0;i<pixels.Length;i++) { ColorUtility.TryParseHtmlString(pack.palette[i],out var color); Assert.Less(((Vector4)(pixels[i]-color)).sqrMagnitude,.00001f); }
        }
        [TestCase("spread-portable-cudgel")][TestCase("spread-portable-hatchet")]
        [TestCase("spread-portable-leatherboots")][TestCase("spread-portable-detectivenotebook")]
        [TestCase("spread-portable-torch")]
        public void ActualNativeCensusGapsHaveSpecificGroundModels(string id)
        {
            var entry = SpreadPortable3DLibrary.Load()?.Find(id);
            Assert.NotNull(entry, id); Assert.Greater(entry.Mesh.bounds.size.sqrMagnitude, .001f);
        }
        [Test]
        public void WrongOrUnknownModelCannotBorrowPortableAsset()
        {
            var library = SpreadPortable3DLibrary.Load(); Assert.NotNull(library);
            Assert.IsNull(library.Find("ring-player")); Assert.IsNull(library.Find("spread-portable-warlordcleaver"));
            Assert.IsNull(library.Find("spread-portable-unknown")); Assert.IsNull(library.Find(null));
        }
        [Test]
        public void ExactMeshMembershipRefusesForeignGeometry()
        {
            var library=SpreadPortable3DLibrary.Load(); Assert.NotNull(library);
            Assert.True(library.ContainsMesh(library.Entries[0].Mesh)); Assert.False(library.ContainsMesh(null));
            var foreign=new Mesh();
            try { Assert.False(library.ContainsMesh(foreign)); }
            finally { UnityEngine.Object.DestroyImmediate(foreign); }
        }
        [TestCase("duplicate")][TestCase("foreignMaterial")][TestCase("missingMesh")][TestCase("wrongSpec")]
        public void LibraryPreflightRejectsMalformedEntryWithoutMutatingBorrowedAssets(string mutation)
        {
            var source = SpreadPortable3DLibrary.Load(); Assert.NotNull(source);
            var copy = ScriptableObject.CreateInstance<SpreadPortable3DLibrary>();
            try
            {
                copy.SourceSha256=source.SourceSha256; copy.Palette=source.Palette; copy.Material=source.Material;
                copy.Entries=source.Entries.Select(e=>new SpreadPortable3DLibrary.Entry { Id=e.Id,Mesh=e.Mesh,Prefab=e.Prefab,Spec=e.Spec }).ToArray();
                if(mutation=="duplicate")copy.Entries[1]=copy.Entries[0];
                if(mutation=="foreignMaterial")copy.Material=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial;
                if(mutation=="missingMesh")copy.Entries[0].Mesh=null;
                if(mutation=="wrongSpec")copy.Entries[0].Spec=new SpawnRing3DCatalog.Model();
                Assert.Throws<InvalidOperationException>(copy.Validate);
                Assert.DoesNotThrow(source.Validate);
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }
    }
}
