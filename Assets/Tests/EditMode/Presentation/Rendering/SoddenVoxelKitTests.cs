using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class SoddenVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "mire", "peat", "snag", "boards", "body" };

        [Test]
        public void KitHasFourCoarseSingleCellVariantsForEverySoddenFamily()
        {
            var kit = SoddenVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(24, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var e = kit.Find(SoddenVoxelLibrary.ModelId(family, variant));
                    Assert.NotNull(e, family);
                    Assert.That(e.Mesh.uv.Distinct().Count(), Is.InRange(1, 2), e.Id);
                    Assert.LessOrEqual(e.Mesh.vertexCount, 240, e.Id);
                    Assert.Greater(e.Mesh.vertexCount, 0, e.Id);
                    Assert.GreaterOrEqual(e.Mesh.bounds.min.x, -.5001f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.max.x, .5001f, e.Id);
                    Assert.GreaterOrEqual(e.Mesh.bounds.min.z, -.5001f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.max.z, .5001f, e.Id);
                    Assert.AreEqual(1, e.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length, e.Id);
                    Assert.AreEqual(0, e.Prefab.GetComponentsInChildren<Collider>(true).Length, e.Id);
                    Assert.AreEqual(0, e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length, e.Id);
                    Assert.AreSame(e.Mesh, e.Prefab.GetComponent<MeshFilter>().sharedMesh);
                    Assert.AreEqual(e.Mesh.bounds.center, e.Spec.boundsCenter, e.Id);
                    Assert.AreEqual(e.Mesh.bounds.size, e.Spec.boundsSize, e.Id);
                    Assert.AreEqual(e.Mesh.triangles.Length / 3, e.Spec.triangles, e.Id);
                    Assert.AreEqual(family == "ground" ? "ground" : "entity", e.Spec.kind, e.Id);
                    shapes[variant] = string.Join(";", e.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [Test]
        public void GroundAndPoolTilesJoinWithoutRaisedRimsOrColorCheckerboards()
        {
            var kit = SoddenVoxelLibrary.Load();
            foreach (string family in new[] { "ground", "mire" })
            {
                Vector2? swatch = null;
                float? surface = null;
                for (int variant = 0; variant < 4; variant++)
                {
                    var e = kit.Find(SoddenVoxelLibrary.ModelId(family, variant));
                    Assert.AreEqual(24, e.Mesh.vertexCount, e.Id);
                    Assert.AreEqual(1, e.Mesh.uv.Distinct().Count(), e.Id);
                    Assert.AreEqual(1f, e.Mesh.bounds.size.x, .0001f, e.Id);
                    Assert.AreEqual(1f, e.Mesh.bounds.size.z, .0001f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.size.y, .12f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.max.y, .06f, e.Id);
                    if (swatch.HasValue) Assert.AreEqual(swatch.Value, e.Mesh.uv[0]);
                    if (surface.HasValue) Assert.AreEqual(surface.Value, e.Mesh.bounds.max.y, .0001f);
                    swatch = e.Mesh.uv[0]; surface = e.Mesh.bounds.max.y;
                }
            }
            Assert.AreNotEqual(kit.Find(SoddenVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0],
                kit.Find(SoddenVoxelLibrary.ModelId("mire", 0)).Mesh.uv[0]);
            Assert.Greater(kit.Find(SoddenVoxelLibrary.ModelId("mire", 0)).Mesh.bounds.min.y,
                kit.Find(SoddenVoxelLibrary.ModelId("ground", 0)).Mesh.bounds.max.y);
        }

        [TestCase("snag", 2.7f, 3.25f)]
        [TestCase("peat", 1.1f, 1.5f)]
        public void MajorLandmarkSilhouettesRemainTallEnoughForTheUnchangedGameplayCamera(string family, float minimum, float maximum)
        {
            var kit = SoddenVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var e = kit.Find(SoddenVoxelLibrary.ModelId(family, variant));
                Assert.That(e.Mesh.bounds.size.y, Is.InRange(minimum, maximum), e.Id);
                Assert.GreaterOrEqual(e.Mesh.bounds.size.x, .80f, e.Id);
                if (family == "snag")
                {
                    // Forks must remain wide well above the base: a tall thin post is not a drowned tree.
                    var upper = e.Mesh.vertices.Where(v => v.y >= minimum * .60f).ToArray();
                    Assert.GreaterOrEqual(upper.Max(v => v.x) - upper.Min(v => v.x), .75f, e.Id);
                }
            }
        }

        [Test]
        public void CutBanksUseTwoEarthTonesIndependentOfFlatBogGround()
        {
            var kit = SoddenVoxelLibrary.Load();
            Vector2 ground = kit.Find(SoddenVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0];
            for (int variant = 0; variant < 4; variant++)
            {
                var e = kit.Find(SoddenVoxelLibrary.ModelId("peat", variant));
                Assert.AreEqual(2, e.Mesh.uv.Distinct().Count(), e.Id);
                Assert.IsFalse(e.Mesh.uv.Contains(ground), "Banks need a readable exposed peat face, not ground-colored caps: " + e.Id);
            }
        }

        [Test]
        public void DuckboardTopStripsSpanAcrossTravelAndUseWeatheredWoodSwatches()
        {
            var kit = SoddenVoxelLibrary.Load();
            var woodTop = new Vector2((11 + .5f) / 16f, .5f / 8f);
            var woodSupport = new Vector2((15 + .5f) / 16f, .5f / 8f);
            foreach (int variant in Enumerable.Range(0, 4))
            {
                var e = kit.Find(SoddenVoxelLibrary.ModelId("boards", variant));
                CollectionAssert.AreEquivalent(new[] { woodTop, woodSupport }, e.Mesh.uv.Distinct().ToArray(), e.Id);
                var vertices = e.Mesh.vertices;
                var indices = e.Mesh.triangles;
                int topTriangles = 0;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var a = vertices[indices[i]]; var b = vertices[indices[i + 1]]; var c = vertices[indices[i + 2]];
                    if (Mathf.Abs(a.y - e.Mesh.bounds.max.y) > .0001f
                        || Mathf.Abs(b.y - a.y) > .0001f || Mathf.Abs(c.y - a.y) > .0001f) continue;
                    float width = Mathf.Max(a.x, b.x, c.x) - Mathf.Min(a.x, b.x, c.x);
                    float length = Mathf.Max(a.z, b.z, c.z) - Mathf.Min(a.z, b.z, c.z);
                    Assert.Greater(length, width * 2.5f, "Top planks must span north/south, across west/east travel: " + e.Id);
                    topTriangles++;
                }
                Assert.AreEqual(6, topTriangles, "Exactly three coarse top planks: " + e.Id);
            }
        }

        [TestCase("Grass", "ground")]
        [TestCase("Floor", "ground")]
        [TestCase("MirePool", "mire")]
        [TestCase("PeatBog", "mire")]
        [TestCase("PeatBank", "peat")]
        [TestCase("DeadTree", "snag")]
        [TestCase("Duckboard", "boards")]
        [TestCase("BogTakenBody", "body")]
        [TestCase("Reeds", null)]
        [TestCase("PreFellingBody", null)]
        [TestCase("Corpse", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyMappingIsExplicitAndDoesNotClaimUnrelatedEntities(string blueprint, string family)
        { Assert.AreEqual(family, SoddenVoxelLibrary.Family(blueprint)); }

        [Test]
        public void ModelIdRejectsUnknownFamilyAndOutOfRangeVariant()
        {
            Assert.AreEqual("sodden-peat-3", SoddenVoxelLibrary.ModelId("peat", 3));
            Assert.Throws<ArgumentException>(() => SoddenVoxelLibrary.ModelId("peet", 0));
            Assert.Throws<ArgumentException>(() => SoddenVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SoddenVoxelLibrary.ModelId("peat", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SoddenVoxelLibrary.ModelId("peat", 4));
            Assert.IsNull(SoddenVoxelLibrary.Load().Find(null));
            Assert.IsNull(SoddenVoxelLibrary.Load().Find("spread-reeds-0"));
            Assert.IsNull(SoddenVoxelLibrary.Load().Find("sodden-nothing-0"));
        }

        [TestCase("duplicate")]
        [TestCase("missing")]
        [TestCase("null-entry")]
        [TestCase("null-id")]
        [TestCase("metadata-kind")]
        [TestCase("metadata-bounds")]
        [TestCase("metadata-triangles")]
        [TestCase("wrong-mesh")]
        [TestCase("wrong-material")]
        public void ValidationRejectsBrokenReferencesAndMetadataWithoutMutatingSource(string corruption)
        {
            var source = SoddenVoxelLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var e = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = e; break;
                    case "missing": copy.Entries = copy.Entries.Take(23).ToArray(); break;
                    case "null-entry": copy.Entries[0] = null; break;
                    case "null-id": e.Id = null; break;
                    case "metadata-kind": e.Spec.kind = "actor"; break;
                    case "metadata-bounds": e.Spec.boundsSize += Vector3.one; break;
                    case "metadata-triangles": e.Spec.triangles++; break;
                    case "wrong-mesh":
                        badPrefab = UnityEngine.Object.Instantiate(e.Prefab);
                        badPrefab.GetComponent<MeshFilter>().sharedMesh = copy.Entries[4].Mesh;
                        e.Prefab = badPrefab;
                        break;
                    case "wrong-material":
                        badPrefab = UnityEngine.Object.Instantiate(e.Prefab);
                        badPrefab.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WaterMaterial;
                        e.Prefab = badPrefab;
                        break;
                }
                Assert.Throws<InvalidOperationException>(() => copy.Validate(), corruption);
                Assert.DoesNotThrow(() => source.Validate(), "Mutation fixture must not change shipped source assets.");
            }
            finally
            {
                if (badPrefab != null) UnityEngine.Object.DestroyImmediate(badPrefab);
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
    }
}
