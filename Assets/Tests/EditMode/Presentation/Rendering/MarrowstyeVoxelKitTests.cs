using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class MarrowstyeVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "wall", "coffer", "cured", "clerk", "path" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = MarrowstyeVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(24, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(MarrowstyeVoxelKitLibrary.ModelId(family, variant));
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
        [TestCase("RoadStone", "path")]
        [TestCase("Roadstone", null)]
        [TestCase("SandstoneWall", "wall")]
        [TestCase("StoneCoffer", "coffer")]
        [TestCase("SaltCuredBody", "cured")]
        [TestCase("FilerClerk", "clerk")]
        [TestCase("StoneFloor", null)]
        [TestCase("Sand", null)]
        [TestCase("StrongBox", null)]
        [TestCase("Chest", null)]
        [TestCase("CreatureCorpse", null)]
        [TestCase("SealedBogTakenBody", null)]
        [TestCase("PreFellingBody", null)]
        [TestCase("PaleCurator", null)]
        [TestCase("SaltcuredBody", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, MarrowstyeVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("marrowstye-ground-3", MarrowstyeVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => MarrowstyeVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => MarrowstyeVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MarrowstyeVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MarrowstyeVoxelKitLibrary.ModelId("ground", 4));
            var kit = MarrowstyeVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("marrowstye-no-such-model-0"));
        }

        [TestCase("ground")]
        [TestCase("path")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = MarrowstyeVoxelKitLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(MarrowstyeVoxelKitLibrary.ModelId(family, variant)).Mesh;
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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void IntakeMasonryIsAQuietLowCutawayWithFullCellContinuity(int variant)
        {
            var wall = Model("wall", variant);
            Assert.That(wall.bounds.max.y, Is.InRange(.95f, 1.15f));
            Assert.LessOrEqual(wall.vertexCount, 48);
            CollectionAssert.AreEquivalent(new[] { Swatch(13), Swatch(35) }, wall.uv.Distinct());
            Assert.AreEqual(Swatch(64), Model("ground", variant).uv[0]);
            foreach (var level in wall.vertices.GroupBy(v => Mathf.Round(v.y * 10000)))
            {
                Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f); Assert.AreEqual(.5f, level.Max(v => v.x), .0001f);
                Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f); Assert.AreEqual(.5f, level.Max(v => v.z), .0001f);
            }
            Assert.IsTrue(Hit(wall, new Vector3(.45f, .7f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(wall, new Vector3(.45f, 1.3f, 1), Vector3.back, 2));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void HeavyCofferHasASolidBlockAndBroadLidWhileCuredCargoRemainsAHumanBody(int variant)
        {
            var coffer = Model("coffer", variant); var body = Model("cured", variant);
            Assert.That(coffer.bounds.max.y, Is.InRange(.55f, .85f));
            Assert.IsTrue(Hit(coffer, new Vector3(0, .3f, 1), Vector3.back, 2));
            Assert.IsTrue(Hit(coffer, new Vector3(.4f, 1, 0), Vector3.down, .5f), "The pale lid spans the heavy box instead of implying a wooden chest lid.");
            Assert.LessOrEqual(coffer.vertexCount, 96);
            CollectionAssert.AreEquivalent(new[] { Swatch(106), Swatch(35) }, coffer.uv.Distinct());
            Assert.That(body.bounds.max.y, Is.InRange(.2f, .45f));
            Assert.Greater(body.bounds.size.z, .75f);
            Assert.IsTrue(Hit(body, new Vector3(0, .6f, -.34f), Vector3.down, .6f));
            Assert.IsTrue(Hit(body, new Vector3(-.11f, .6f, .39f), Vector3.down, .6f));
            Assert.IsTrue(Hit(body, new Vector3(.11f, .6f, .39f), Vector3.down, .6f));
            Assert.IsFalse(Hit(body, new Vector3(0, .6f, .39f), Vector3.down, .6f));
            CollectionAssert.AreEquivalent(new[] { Swatch(95), Swatch(106) }, body.uv.Distinct());
            Assert.Less(body.bounds.max.y, coffer.bounds.max.y - .15f);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ClerkCarriesAVisibleStampAndLedgerWithoutDraggingADeskAlongWithItsBody(int variant)
        {
            var clerk = Model("clerk", variant);
            Assert.That(clerk.bounds.max.y, Is.InRange(1.2f, 1.7f));
            Assert.Less(clerk.bounds.size.x, .85f); Assert.Less(clerk.bounds.size.z, .7f);
            CollectionAssert.AreEquivalent(new[] { Swatch(13), Swatch(19) }, clerk.uv.Distinct());
            Assert.IsFalse(Hit(clerk, new Vector3(0, .18f, 1), Vector3.back, 2));
            Assert.IsTrue(Hit(clerk, new Vector3(.12f, .18f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(clerk, new Vector3(.44f, .7f, 1), Vector3.back, 2), "No broad fixed counter is attached to this mobile conversational owner.");
            var paper = clerk.vertices.Where((v, i) => clerk.uv[i] == Swatch(19) && v.z > .2f && v.y < 1.2f).ToArray();
            Assert.IsNotEmpty(paper); Assert.Greater(paper.Max(v => v.x) - paper.Min(v => v.x), .2f);
            Assert.IsTrue(clerk.vertices.Where((v, i) => clerk.uv[i] == Swatch(13)).Any(v => v.x > .25f && v.y > 1.1f && v.z > .15f), "The raised hand has a distinct small stamping tool.");
        }

        [Test]
        public void ReceivingRoutesHaveAQuietDistinctStoneSurfaceInsteadOfBlendingIntoTheForecourt()
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var path = Model("path", variant); var ground = Model("ground", variant);
                Assert.AreEqual(Swatch(106), path.uv[0]);
                Assert.AreEqual(Swatch(64), ground.uv[0]);
                Assert.AreNotEqual(path.uv[0], ground.uv[0]);
                Assert.AreEqual(0, path.bounds.max.y, .0001f);
                Assert.IsTrue(Hit(path, new Vector3(.45f, .4f, .45f), Vector3.down, .5f));
                Assert.IsFalse(Hit(path, new Vector3(.55f, .4f, .45f), Vector3.down, .5f));
            }
        }

        private static Mesh Model(string family, int variant) => MarrowstyeVoxelKitLibrary.Load().Find(MarrowstyeVoxelKitLibrary.ModelId(family, variant)).Mesh;

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
            var source = MarrowstyeVoxelKitLibrary.Load(); source.Validate();
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
