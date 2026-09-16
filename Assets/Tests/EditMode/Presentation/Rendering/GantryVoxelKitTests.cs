using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class GantryVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "path", "wall", "desk", "counter", "registrar", "wayboard" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = GantryVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(28, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(GantryVoxelKitLibrary.ModelId(family, variant));
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

        [TestCase("Grass", "ground")][TestCase("Floor", "ground")][TestCase("RoadStone", "path")]
        [TestCase("GantryTimberWall", "wall")][TestCase("GantryRegistryDesk", "desk")]
        [TestCase("GantryExchangeCounter", "counter")][TestCase("GantryRegistrar", "registrar")]
        [TestCase("GantryWayboard", "wayboard")][TestCase("FilerClerk", null)]
        [TestCase("StoneFloor", null)][TestCase("TentRightHost", null)][TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, GantryVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("gantry-ground-3", GantryVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => GantryVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => GantryVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => GantryVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GantryVoxelKitLibrary.ModelId("ground", 4));
            var kit = GantryVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("gantry-no-such-model-0"));
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
            var source = GantryVoxelKitLibrary.Load(); source.Validate();
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
