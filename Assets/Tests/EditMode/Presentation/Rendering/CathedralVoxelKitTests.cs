using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class CathedralVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "vault", "node", "elder", "tendril" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = CathedralVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(20, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(CathedralVoxelLibrary.ModelId(family, variant));
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
                    Assert.AreSame(entry.Mesh, entry.Prefab.GetComponent<MeshFilter>().sharedMesh);
                    Assert.AreSame(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial,
                        entry.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
                    Assert.AreEqual(entry.Mesh.bounds.center, entry.Spec.boundsCenter, entry.Id);
                    Assert.AreEqual(entry.Mesh.bounds.size, entry.Spec.boundsSize, entry.Id);
                    Assert.AreEqual(entry.Mesh.triangles.Length / 3, entry.Spec.triangles, entry.Id);
                    Assert.AreEqual(family == "ground" ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("SandstoneFloor", "ground")]
        [TestCase("StoneFloor", "ground")]
        [TestCase("SubstrateVault", "vault")]
        [TestCase("ChoirNode", "node")]
        [TestCase("EncasedElder", "elder")]
        [TestCase("ChoirTendril", "tendril")]
        [TestCase("SealedLibraryFloor", null)]
        [TestCase("SandstoneWall", null)]
        [TestCase("Grass", null)]
        [TestCase("Tree", null)]
        [TestCase("FungalWall", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, CathedralVoxelLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("cathedral-ground-3", CathedralVoxelLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => CathedralVoxelLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => CathedralVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CathedralVoxelLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CathedralVoxelLibrary.ModelId("ground", 4));
            var kit = CathedralVoxelLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("cathedral-no-such-model-0"));
        }

        [TestCase("ground")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = CathedralVoxelLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(CathedralVoxelLibrary.ModelId(family, variant)).Mesh;
                Assert.AreEqual(24, mesh.vertexCount);
                Assert.AreEqual(1, mesh.uv.Distinct().Count());
                Assert.AreEqual(1, mesh.bounds.size.x, .0001f);
                Assert.AreEqual(1, mesh.bounds.size.z, .0001f);
                Assert.AreEqual(0, mesh.bounds.max.y, .0001f);
                Assert.LessOrEqual(mesh.bounds.size.y, .12f);
                if (swatch.HasValue) Assert.AreEqual(swatch.Value, mesh.uv[0]);
                swatch = mesh.uv[0];
            }
        }

        [Test]
        public void GrownVaultsJoinAsBroadMassesWithAConstantPaleTopAndVioletCourse()
        {
            var kit = CathedralVoxelLibrary.Load();
            var ground = kit.Find(CathedralVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0];
            Vector2? topColor = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(CathedralVoxelLibrary.ModelId("vault", variant)).Mesh;
                Assert.AreEqual(1.1f, mesh.bounds.max.y, .0001f);
                Assert.LessOrEqual(mesh.vertexCount, 72);
                Assert.AreEqual(2, mesh.uv.Distinct().Count());
                foreach (var level in mesh.vertices.GroupBy(v => Mathf.Round(v.y * 10000)))
                {
                    Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f);
                    Assert.AreEqual(.5f, level.Max(v => v.x), .0001f);
                    Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f);
                    Assert.AreEqual(.5f, level.Max(v => v.z), .0001f);
                }
                var tops = Enumerable.Range(0, mesh.vertexCount).Where(i => mesh.vertices[i].y > 1.099f)
                    .Select(i => mesh.uv[i]).Distinct().ToArray();
                Assert.AreEqual(1, tops.Length);
                Assert.AreNotEqual(ground, tops[0]);
                if (topColor.HasValue) Assert.AreEqual(topColor.Value, tops[0]);
                topColor = tops[0];
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EldersShowPairedEyesAndAFaceAboveTheEncasingSubstrate(int variant)
        {
            var mesh = CathedralVoxelLibrary.Load().Find(CathedralVoxelLibrary.ModelId("elder", variant)).Mesh;
            Assert.That(mesh.bounds.max.y, Is.InRange(1.20f, 1.65f));
            Assert.Greater(mesh.bounds.size.x, .65f);
            Vector2 pale = Swatch(19), violet = Swatch(30);
            var face = Enumerable.Range(0, mesh.vertexCount)
                .Where(i => mesh.uv[i] == pale && mesh.vertices[i].y > 1.0f).Select(i => mesh.vertices[i]).ToArray();
            Assert.IsNotEmpty(face, "A conversational person needs an exposed face above the casing.");
            var eyes = Enumerable.Range(0, mesh.vertexCount)
                .Where(i => mesh.uv[i] == violet && mesh.vertices[i].y > 1.1f && mesh.vertices[i].z > .30f)
                .Select(i => mesh.vertices[i]).ToArray();
            Assert.IsTrue(eyes.Any(v => v.x < -.04f));
            Assert.IsTrue(eyes.Any(v => v.x > .04f));
            var baseMass = mesh.vertices.Where(v => v.y < .3f).ToArray();
            Assert.Greater(baseMass.Max(v => v.x) - baseMass.Min(v => v.x), face.Max(v => v.x) - face.Min(v => v.x));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LivingTendrilsBranchAboveARootAndNodesHaveADifferentCentralCrown(int variant)
        {
            var kit = CathedralVoxelLibrary.Load();
            var tendril = kit.Find(CathedralVoxelLibrary.ModelId("tendril", variant)).Mesh;
            var node = kit.Find(CathedralVoxelLibrary.ModelId("node", variant)).Mesh;
            Assert.That(tendril.bounds.max.y, Is.InRange(1.1f, 1.7f));
            Assert.That(node.bounds.max.y, Is.InRange(2.2f, 2.4f));
            var branches = tendril.vertices.Where(v => v.y > .75f).ToArray();
            Assert.IsTrue(branches.Any(v => v.x < -.25f));
            Assert.IsTrue(branches.Any(v => v.x > .25f));
            Assert.AreEqual(2, tendril.uv.Distinct().Count());
            Assert.AreEqual(2, node.uv.Distinct().Count());
            var crown = node.vertices.Where(v => v.y > 1.05f).ToArray();
            Assert.Greater(crown.Max(v => v.x) - crown.Min(v => v.x), .45f);
            Assert.AreNotEqual(string.Join(";", node.vertices), string.Join(";", tendril.vertices));
            Assert.AreNotEqual(node.uv[0], tendril.uv[0], "The native trader and the light fixture need distinct identities.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void CutawayVaultLeavesElderEyesAboveItAndNodeCrownDominatesTheNave(int variant)
        {
            var kit = CathedralVoxelLibrary.Load();
            var vault = kit.Find(CathedralVoxelLibrary.ModelId("vault", variant)).Mesh;
            var elder = kit.Find(CathedralVoxelLibrary.ModelId("elder", variant)).Mesh;
            var node = kit.Find(CathedralVoxelLibrary.ModelId("node", variant)).Mesh;
            var eyes = Enumerable.Range(0, elder.vertexCount)
                .Where(i => elder.uv[i] == Swatch(30) && elder.vertices[i].y > 1.1f && elder.vertices[i].z > .30f)
                .Select(i => elder.vertices[i]).ToArray();
            Assert.IsNotEmpty(eyes);
            Assert.Greater(eyes.Min(v => v.y), vault.bounds.max.y + .1f,
                "Actual eye geometry, not just the casing's highest point, must clear the cutaway wall.");
            Assert.Greater(node.bounds.max.y, elder.bounds.max.y + .70f);
            Assert.Greater(node.bounds.max.y, vault.bounds.max.y + 1.0f);
            var crown = node.vertices.Where(v => v.y > elder.bounds.max.y).ToArray();
            Assert.IsNotEmpty(crown);
            Assert.GreaterOrEqual(crown.Max(v => v.x) - crown.Min(v => v.x), .85f,
                "The central fixture needs a broad raised crown rather than a thin taller spike.");
            Assert.LessOrEqual(node.vertexCount, 168, "Focal scale should improve without additional small boxes.");
            Assert.AreEqual(2, node.uv.Distinct().Count());
        }

        private static Vector2 Swatch(int index) => new Vector2((index % 16 + .5f) / 16f, (index / 16 + .5f) / 8f);

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
        public void CorruptAssetReferencesFailWithoutMutatingTheSourceLibrary(string corruption)
        {
            var source = CathedralVoxelLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var entry = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = entry; break;
                    case "missing": copy.Entries = copy.Entries.Take(20 - 1).ToArray(); break;
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
