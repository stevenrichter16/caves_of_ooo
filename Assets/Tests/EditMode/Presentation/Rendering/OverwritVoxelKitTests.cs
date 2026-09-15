using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class OverwritVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "growth", "waymarker", "bench" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = OverwritVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(16, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(OverwritVoxelLibrary.ModelId(family, variant));
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

        [TestCase("OverwritGround", "ground")]
        [TestCase("OverwritNewGrowth", "growth")]
        [TestCase("OverwritWaymarker", "waymarker")]
        [TestCase("OverwritPilgrimBench", "bench")]
        [TestCase("Floor", null)]
        [TestCase("Grass", null)]
        [TestCase("Sand", null)]
        [TestCase("Rock", null)]
        [TestCase("Rubble", null)]
        [TestCase("PalimpsestEcho", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, OverwritVoxelLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("overwrit-ground-3", OverwritVoxelLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => OverwritVoxelLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => OverwritVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => OverwritVoxelLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => OverwritVoxelLibrary.ModelId("ground", 4));
            var kit = OverwritVoxelLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("overwrit-no-such-model-0"));
        }

        [Test]
        public void BlankGroundHasOneVisibleAppearanceAndNoPerCellTopography()
        {
            var kit = OverwritVoxelLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(OverwritVoxelLibrary.ModelId("ground", variant));
                Assert.AreEqual(24, entry.Mesh.vertexCount, "A blank cell is a single calm plane, not rubble or paving.");
                Assert.AreEqual(1, entry.Mesh.uv.Distinct().Count());
                Assert.AreEqual(Vector3.zero.x, entry.Mesh.bounds.max.y, .0001f);
                Assert.AreEqual(1, entry.Mesh.bounds.size.x, .0001f);
                Assert.AreEqual(1, entry.Mesh.bounds.size.z, .0001f);
                Assert.Less(entry.Mesh.bounds.min.y, 0);
                Assert.LessOrEqual(entry.Mesh.bounds.size.y, .12f);
                if (swatch.HasValue) Assert.AreEqual(swatch.Value, entry.Mesh.uv[0]);
                swatch = entry.Mesh.uv[0];
            }
        }

        [Test]
        public void BlankGroundSamplesTheQuietNeutralPaletteInsteadOfYellowSand()
        {
            var texture = new Texture2D(2, 2);
            try
            {
                string path = Path.Combine(Application.dataPath, "Art3D/SpawnRing/Textures/SpawnRingPalette.png");
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = OverwritVoxelLibrary.Load().Find(OverwritVoxelLibrary.ModelId("ground", variant));
                    Vector2 uv = entry.Mesh.uv[0];
                    Color color = texture.GetPixel(Mathf.FloorToInt(uv.x * texture.width), Mathf.FloorToInt(uv.y * texture.height));
                    float spread = Mathf.Max(color.r, Mathf.Max(color.g, color.b)) - Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                    Assert.LessOrEqual(spread, .09f, "The source swatch must be neutral before lighting, not sand disguised by a region-name assertion.");
                    Assert.AreEqual(new Vector2((35 % 16 + .5f) / 16f, (35 / 16 + .5f) / 8f), uv);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [Test]
        public void NewGrowthChangesShapeButNeverItsUniformHeight()
        {
            var kit = OverwritVoxelLibrary.Load();
            float? height = null;
            Vector2 ground = kit.Find(OverwritVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0];
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(OverwritVoxelLibrary.ModelId("growth", variant));
                Assert.That(entry.Mesh.bounds.max.y, Is.InRange(.20f, .40f));
                if (height.HasValue) Assert.AreEqual(height.Value, entry.Mesh.bounds.max.y, .0001f);
                height = entry.Mesh.bounds.max.y;
                Assert.LessOrEqual(entry.Mesh.vertexCount, 96);
                Assert.Less(entry.Mesh.bounds.size.x, .8f);
                Assert.Less(entry.Mesh.bounds.size.z, .8f);
                Assert.IsTrue(entry.Mesh.uv.All(uv => uv != ground), "Sparse growth must still be distinguishable from bare ground.");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryUpwardGrowthTipReachesTheSameHeightInsteadOfOnlyTheTallestShoot(int variant)
        {
            var entry = OverwritVoxelLibrary.Load().Find(OverwritVoxelLibrary.ModelId("growth", variant));
            var vertices = entry.Mesh.vertices; var indices = entry.Mesh.triangles;
            float expectedHeight = entry.Mesh.bounds.max.y;
            int tips = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = vertices[indices[i]]; var b = vertices[indices[i + 1]]; var c = vertices[indices[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (normal.y < .999f) continue;
                tips++;
                Assert.AreEqual(expectedHeight, a.y, .0001f, entry.Id + ": every shoot tip matches the shared cut height.");
                Assert.AreEqual(expectedHeight, b.y, .0001f, entry.Id);
                Assert.AreEqual(expectedHeight, c.y, .0001f, entry.Id);
            }
            Assert.Greater(tips, 0, "The test must inspect actual upward faces, not only a maximum bounding box.");
        }

        [Test]
        public void SilentMarkersArePlainUprightPostsAndBenchesHaveAReadableOpenSide()
        {
            var kit = OverwritVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var marker = kit.Find(OverwritVoxelLibrary.ModelId("waymarker", variant));
                Assert.That(marker.Mesh.bounds.max.y, Is.InRange(.85f, 1.5f));
                Assert.Greater(marker.Mesh.bounds.size.y, marker.Mesh.bounds.size.x * 1.6f);
                Assert.LessOrEqual(marker.Mesh.vertexCount, 48, "A blank post has no engraved glyphs, banners or ornate finials.");
                var bench = kit.Find(OverwritVoxelLibrary.ModelId("bench", variant));
                Assert.That(bench.Mesh.bounds.max.y, Is.InRange(.45f, .85f));
                Assert.Greater(bench.Mesh.bounds.size.x, .75f);
                Assert.LessOrEqual(bench.Mesh.vertexCount, 144);
                var upper = bench.Mesh.vertices.Where(v => v.y > .45f).ToArray();
                Assert.IsNotEmpty(upper, "The low seat has a distinct backrest.");
                Assert.IsTrue(upper.All(v => v.z < -.10f), "Local +Z remains the facing/open side for rim orientation.");
                var front = bench.Mesh.vertices.Where(v => v.z > .15f).ToArray();
                Assert.IsNotEmpty(front, "A usable seat reaches the open side.");
                Assert.Less(front.Max(v => v.y), .45f);
            }
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
        public void CorruptAssetReferencesFailWithoutMutatingTheSourceLibrary(string corruption)
        {
            var source = OverwritVoxelLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var entry = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = entry; break;
                    case "missing": copy.Entries = copy.Entries.Take(16 - 1).ToArray(); break;
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
