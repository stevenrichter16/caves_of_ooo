using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class LastCounterVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "wall", "sign", "envoy" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = LastCounterVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(16, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(LastCounterVoxelKitLibrary.ModelId(family, variant));
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

        [TestCase("Floor", "ground")]
        [TestCase("Sand", null)]
        [TestCase("SandstoneWall", "wall")]
        [TestCase("LastCounterSign", "sign")]
        [TestCase("SaccharineEnvoy", "envoy")]
        [TestCase("RoadStone", null)]
        [TestCase("StoneFloor", null)]
        [TestCase("Signpost", null)]
        [TestCase("CinderholdNoticeBoard", null)]
        [TestCase("ConcordFactor", null)]
        [TestCase("ChestCampGoodsT1", null)]
        [TestCase("Chest", null)]
        [TestCase("lastCounterSign", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, LastCounterVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("lastcounter-ground-3", LastCounterVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => LastCounterVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => LastCounterVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LastCounterVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LastCounterVoxelKitLibrary.ModelId("ground", 4));
            var kit = LastCounterVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("lastcounter-no-such-model-0"));
        }

        [Test]
        public void OutdoorFloorRemainsContinuousWhileLowStoneWallsRevealTheWorkingCourt()
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var ground = Model("ground", variant); var wall = Model("wall", variant);
                Assert.AreEqual(1, ground.uv.Distinct().Count());
                Assert.AreEqual(Swatch(64), ground.uv[0]);
                Assert.That(ground.bounds.max.y, Is.EqualTo(0).Within(.0001f));
                Assert.That(ground.bounds.size.x, Is.EqualTo(1).Within(.0001f));
                Assert.That(ground.bounds.size.z, Is.EqualTo(1).Within(.0001f));
                Assert.LessOrEqual(wall.vertexCount, 48);
                Assert.That(wall.bounds.max.y, Is.InRange(1f, 1.12f));
                CollectionAssert.AreEquivalent(new[] { Swatch(56), Swatch(79) }, wall.uv.Distinct());
                foreach (var slice in wall.vertices.GroupBy(v => Mathf.RoundToInt(v.y * 10000)))
                {
                    Assert.That(slice.Max(v => v.x) - slice.Min(v => v.x), Is.EqualTo(1).Within(.0001f));
                    Assert.That(slice.Max(v => v.z) - slice.Min(v => v.z), Is.EqualTo(1).Within(.0001f));
                }
                Assert.Greater(wall.bounds.max.y, ground.bounds.max.y + .9f);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void DisclaimerHasThreeSeparatedVisibleLinesOnASinglePlanedBoard(int variant)
        {
            var sign = Model("sign", variant);
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(35) }, sign.uv.Distinct());
            Assert.That(sign.bounds.max.y, Is.InRange(1.5f, 2.1f));
            Assert.Greater(sign.bounds.size.x, .85f); Assert.Less(sign.bounds.size.z, .4f);
            foreach (float height in new[] { .9f, 1.15f, 1.4f })
                Assert.IsTrue(Hit(sign, new Vector3(0, height, .5f), Vector3.back, .39f), "The current disclaimer and its two earlier inscriptions remain distinct broad bands.");
            foreach (float height in new[] { 1.025f, 1.275f })
                Assert.IsFalse(Hit(sign, new Vector3(0, height, .5f), Vector3.back, .39f), "Actual face gaps prevent one dark slab from satisfying all three lines.");
            Assert.IsTrue(Hit(sign, new Vector3(0, 1.275f, .5f), Vector3.back, .6f), "The three lines are mounted on one actual supporting board.");
            Assert.IsFalse(Hit(sign, new Vector3(0, .3f, 1), Vector3.back, 2), "Separate posts keep the sign from becoming an altar or wall.");
            Assert.IsTrue(Hit(sign, new Vector3(.32f, .3f, 1), Vector3.back, 2));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void EnvoyReadsAsAnUprightGoldCladPersonCarryingASeparateLedger(int variant)
        {
            var mesh = Model("envoy", variant);
            CollectionAssert.AreEquivalent(new[] { Swatch(49), Swatch(12) }, mesh.uv.Distinct());
            Assert.That(mesh.bounds.max.y, Is.InRange(1.35f, 1.7f));
            Assert.IsTrue(Hit(mesh, new Vector3(.12f, .2f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(mesh, new Vector3(0, .2f, 1), Vector3.back, 2), "An actor cannot carry an attached counter or terrain platform.");
            Assert.IsTrue(mesh.vertices.Where((v, i) => mesh.uv[i] == Swatch(12)).Any(v => v.z > .24f && v.y > .65f && v.y < 1.1f));
            Assert.Less(mesh.bounds.size.x, .85f); Assert.Less(mesh.bounds.size.z, .7f);
        }

        private static Mesh Model(string family, int variant) => LastCounterVoxelKitLibrary.Load().Find(LastCounterVoxelKitLibrary.ModelId(family, variant)).Mesh;

        private static Vector2 Swatch(int index) => new Vector2((index % 16 + .5f) / 16f, (index / 16 + .5f) / 8f);

        // Two-sided bounded ray/triangle intersection: probes actual openings rather than a bounding-box proxy.
        private static bool Hit(Mesh mesh, Vector3 origin, Vector3 direction, float maxDistance)
        {
            var vertices = mesh.vertices; var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], e1 = vertices[triangles[i + 1]] - a, e2 = vertices[triangles[i + 2]] - a;
                Vector3 p = Vector3.Cross(direction, e2); float determinant = Vector3.Dot(e1, p);
                if (Mathf.Abs(determinant) < .000001f) continue;
                float inverse = 1 / determinant; Vector3 t = origin - a;
                float u = Vector3.Dot(t, p) * inverse; if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(t, e1); float v = Vector3.Dot(direction, q) * inverse;
                if (v < 0 || u + v > 1) continue;
                float distance = Vector3.Dot(e2, q) * inverse;
                if (distance >= 0 && distance <= maxDistance) return true;
            }
            return false;
        }

        [TestCase("duplicate")]
        [TestCase("missing")]
        [TestCase("null-entry")]
        [TestCase("null-id")]
        [TestCase("metadata-kind")]
        [TestCase("metadata-bounds")]
        [TestCase("metadata-triangles")]
        [TestCase("metadata-path")]
        [TestCase("metadata-rig")]
        [TestCase("metadata-material")]
        [TestCase("wrong-mesh")]
        [TestCase("wrong-material")]
        [TestCase("extra-light")]
        public void CorruptAssetReferencesFailWithoutMutatingTheSourceLibrary(string corruption)
        {
            var source = LastCounterVoxelKitLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var entry = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = entry; break;
                    case "missing": copy.Entries = copy.Entries.Take(copy.Entries.Length - 1).ToArray(); break;
                    case "null-entry": copy.Entries[0] = null; break;
                    case "null-id": entry.Id = null; break;
                    case "metadata-kind": entry.Spec.kind = "actor"; break;
                    case "metadata-bounds": entry.Spec.boundsSize += Vector3.one; break;
                    case "metadata-triangles": entry.Spec.triangles++; break;
                    case "metadata-path": entry.Spec.path = "wrong/path.prefab"; break;
                    case "metadata-rig": entry.Spec.rigged = true; break;
                    case "metadata-material": entry.Spec.materialFamily = "wrong-palette"; break;
                    case "wrong-mesh":
                        badPrefab = UnityEngine.Object.Instantiate(entry.Prefab);
                        badPrefab.GetComponent<MeshFilter>().sharedMesh = copy.Entries[4].Mesh;
                        entry.Prefab = badPrefab;
                        break;
                    case "extra-light":
                        badPrefab = UnityEngine.Object.Instantiate(entry.Prefab);
                        badPrefab.AddComponent<Light>();
                        entry.Prefab = badPrefab;
                        break;
                    case "wrong-material":
                        badPrefab = UnityEngine.Object.Instantiate(entry.Prefab);
                        badPrefab.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WaterMaterial;
                        entry.Prefab = badPrefab;
                        break;
                }
                Assert.Throws<InvalidOperationException>(() => copy.Validate(), corruption);
                Assert.DoesNotThrow(() => source.Validate(), "Corruption controls must leave real source assets intact.");
            }
            finally
            {
                if (badPrefab != null) UnityEngine.Object.DestroyImmediate(badPrefab);
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
    }
}
