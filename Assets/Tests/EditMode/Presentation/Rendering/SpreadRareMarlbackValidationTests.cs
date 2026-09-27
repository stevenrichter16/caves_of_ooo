using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadRareMarlbackValidationTests
    {
        static SpreadRareMarlbackLibrary Library()
        { var library = SpreadRareMarlbackLibrary.Load(); Assert.NotNull(library, "Actual imported optional rare pack required."); return library; }

        [TestCase("spread-rare-hurdle-cutter")]
        [TestCase("spread-rare-ditch-mate")]
        public void ValidImportedPairKeepsBorrowedSourceBuffers(string id)
        {
            var library = Library(); var source = library.SourceMesh;
            var positions = source.vertices; var weights = source.boneWeights; var indices = source.triangles;
            library.Validate(); SpreadRareMarlbackLibrary.ValidatePair(source, library.Find(id).Mesh);
            CollectionAssert.AreEqual(positions, source.vertices); CollectionAssert.AreEqual(weights, source.boneWeights);
            CollectionAssert.AreEqual(indices, source.triangles);
        }

        [TestCase("same-source")][TestCase("changed-original-vertex")][TestCase("nonfinite-added-uv")]
        public void InvalidPairIsRefusedWithoutMutatingEitherOwner(string mutation)
        {
            var library = Library(); Mesh changed = null;
            try
            {
                var source = library.SourceMesh; var before = source.vertices;
                changed = Object.Instantiate(library.Find("spread-rare-hurdle-cutter").Mesh);
                if (mutation == "changed-original-vertex") { var vertices = changed.vertices; vertices[0] += Vector3.up; changed.vertices = vertices; }
                if (mutation == "nonfinite-added-uv") { var uv = changed.uv; uv[source.vertexCount].y = float.NaN; changed.uv = uv; }
                var positions = changed.vertices; var uvs = changed.uv;
                Assert.Throws<InvalidOperationException>(() => SpreadRareMarlbackLibrary.ValidatePair(source, mutation == "same-source" ? source : changed));
                CollectionAssert.AreEqual(before, source.vertices); CollectionAssert.AreEqual(positions, changed.vertices);
                for (int i = 0; i < uvs.Length; i++)
                { Assert.AreEqual(uvs[i].x, changed.uv[i].x); Assert.AreEqual(uvs[i].y, changed.uv[i].y); }
            }
            finally { if (changed != null) Object.DestroyImmediate(changed); }
        }

        [Test]
        public void MatchingNameForeignBoneCannotPassCurrentPrefabOwnership()
        {
            var source = Library(); var copy = Object.Instantiate(source); GameObject changed = null, foreign = null;
            try
            {
                copy.Entries = source.Entries.Select(e => new SpreadRareMarlbackLibrary.Entry { Id = e.Id, Prefab = e.Prefab, Mesh = e.Mesh, Spec = e.Spec }).ToArray();
                changed = Object.Instantiate(copy.Entries[0].Prefab); copy.Entries[0].Prefab = changed;
                var skin = changed.GetComponentInChildren<SkinnedMeshRenderer>(); var bones = skin.bones;
                foreign = new GameObject(bones[0].name); bones[0] = foreign.transform; skin.bones = bones;
                Assert.Throws<InvalidOperationException>(() => copy.Validate());
                Assert.AreSame(foreign.transform, skin.bones[0], "Validation must refuse, never repair borrowed or malformed rig refs.");
                Assert.DoesNotThrow(() => source.Validate());
            }
            finally { if (changed != null) Object.DestroyImmediate(changed); if (foreign != null) Object.DestroyImmediate(foreign); Object.DestroyImmediate(copy); }
        }

        [TestCase("spread-rare-hurdle-cutter")]
        [TestCase("spread-rare-ditch-mate")]
        public void EveryPhysicalAdditionTouchesAnActualOriginalSurfaceOnItsBone(string id)
        {
            var library = Library(); var source = library.SourceMesh; var variant = library.Find(id).Mesh;
            var original = source.vertices; var oldWeights = source.boneWeights; var triangles = source.triangles;
            var added = variant.vertices; var weights = variant.boneWeights;
            Assert.AreEqual(0, (added.Length - original.Length) % 24, "The current explicit additions are closed six-face cuboids.");
            for (int start = original.Length; start < added.Length; start += 24)
            {
                int bone = weights[start].boneIndex0;
                var box = new Bounds(added[start], Vector3.zero);
                for (int n = 1; n < 24; n++) { Assert.AreEqual(bone, weights[start + n].boneIndex0); box.Encapsulate(added[start + n]); }
                bool contact = false;
                for (int n = 0; n < triangles.Length && !contact; n += 3)
                {
                    int a = triangles[n], b = triangles[n + 1], c = triangles[n + 2];
                    if (oldWeights[a].boneIndex0 != bone || oldWeights[b].boneIndex0 != bone || oldWeights[c].boneIndex0 != bone) continue;
                    // The adopted source is axis-aligned voxel faces. Its two
                    // paired triangle bounds exactly cover each real surface.
                    var face = new Bounds(original[a], Vector3.zero); face.Encapsulate(original[b]); face.Encapsulate(original[c]);
                    if (box.Intersects(face)) contact = true;
                }
                Assert.True(contact, id + " detached physical addition #" + ((start - original.Length) / 24));
            }
        }

        [Test]
        public void UnknownBlueprintAndModelDoNotAcquireFamilyAuthority()
        {
            var library = Library();
            Assert.IsNull(SpreadRareMarlbackLibrary.Model("SpreadHurdleCutterImpostor"));
            Assert.IsNull(SpreadRareMarlbackLibrary.Blueprint("spread-rare-hurdle-cutter-copy"));
            Assert.IsNull(library.Find("spread-rare-hurdle-cutter-copy"));
        }
    }
}
