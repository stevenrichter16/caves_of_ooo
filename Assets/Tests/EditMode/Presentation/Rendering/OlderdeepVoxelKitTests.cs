using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class OlderdeepVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "rooted", "plume", "niche", "plaque", "oldest", "jar", "listener", "tender", "wall" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = OlderdeepVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(40, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(OlderdeepVoxelLibrary.ModelId(family, variant));
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

        [TestCase("StoneFloor", "ground")]
        [TestCase("SandstoneFloor", "ground")]
        [TestCase("TheRooted", "rooted")]
        [TestCase("FoundingPlume", "plume")]
        [TestCase("NicheHome", "niche")]
        [TestCase("PlaqueWall", "plaque")]
        [TestCase("PlaqueOldest", "oldest")]
        [TestCase("BeetleJar", "jar")]
        [TestCase("FoundingListener", "listener")]
        [TestCase("FoundingPlaqueTender", "tender")]
        [TestCase("SandstoneWall", null)]
        [TestCase("CutawayWall", null)]
        [TestCase("Player", null)]
        [TestCase("CatacombWarden", null)]
        [TestCase("PlaqueTender", null)]
        [TestCase("Tree", null)]
        [TestCase("Grass", null)]
        [TestCase("Foundingplume", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, OlderdeepVoxelLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("olderdeep-ground-3", OlderdeepVoxelLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => OlderdeepVoxelLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => OlderdeepVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => OlderdeepVoxelLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => OlderdeepVoxelLibrary.ModelId("ground", 4));
            var kit = OlderdeepVoxelLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("olderdeep-no-such-model-0"));
        }

        [TestCase("ground")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = OlderdeepVoxelLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(OlderdeepVoxelLibrary.ModelId(family, variant)).Mesh;
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
        public void RootedHasPairedFlatShinsBentKneesAndOpenArmsReachingEast(int variant)
        {
            var mesh = Model("rooted", variant);
            Assert.That(mesh.bounds.max.y, Is.InRange(.65f, 1.0f));
            Assert.LessOrEqual(mesh.vertexCount, 240);
            var shins = mesh.vertices.Where(v => v.x < -.15f && v.y < .1f).ToArray();
            Assert.IsTrue(shins.Any(v => v.z < -.08f));
            Assert.IsTrue(shins.Any(v => v.z > .08f));
            var knees = mesh.vertices.Where(v => v.x < -.1f && v.y > .18f && v.y < .32f).ToArray();
            Assert.IsTrue(knees.Any(v => v.z < -.08f));
            Assert.IsTrue(knees.Any(v => v.z > .08f));
            var arms = mesh.vertices.Where(v => v.x > .40f && v.y > .25f && v.y < .50f).ToArray();
            Assert.IsTrue(arms.Any(v => v.z < -.12f));
            Assert.IsTrue(arms.Any(v => v.z > .12f));
            Assert.IsFalse(Hit(mesh, new Vector3(.43f, .37f, -.12f), Vector3.forward, .24f),
                "The eastward embrace must remain open between the two hands.");
            Assert.IsTrue(Hit(mesh, new Vector3(.43f, .37f, -.5f), Vector3.forward, .5f));
            var fungus = Enumerable.Range(0, mesh.vertexCount).Where(i => mesh.uv[i] == Swatch(19)).Select(i => mesh.vertices[i]).ToArray();
            Assert.IsNotEmpty(fungus);
            Assert.Greater(fungus.Max(v => v.y), .64f);
        }

        [Test]
        public void PlumeIsALowClusteredBedWithBrokenEdgesInsteadOfAFlatFloorBlanket()
        {
            for (int v = 0; v < 4; v++)
            {
                var mesh = Model("plume", v);
                Assert.That(mesh.bounds.max.y, Is.InRange(.18f, .30f));
                Assert.LessOrEqual(mesh.vertexCount, 96, "Four broad overlapping masses suffice; no field of tiny caps.");
                Assert.Less(mesh.bounds.size.x, .98f);
                Assert.Less(mesh.bounds.size.z, .98f);
                var topHeights = Enumerable.Range(0, mesh.vertexCount).Where(i => mesh.normals[i].y > .9f)
                    .Select(i => Mathf.Round(mesh.vertices[i].y * 1000)).Distinct().ToArray();
                Assert.GreaterOrEqual(topHeights.Length, 3, "Living plume needs a low changing silhouette, not one flat top.");
                Assert.IsTrue(Hit(mesh, new Vector3(0, 1, 0), Vector3.down, 1));
                Assert.IsFalse(Hit(mesh, new Vector3(.48f, 1, .48f), Vector3.down, 1), "The visible contour must break before the square cell corner.");
                Assert.IsFalse(Hit(mesh, new Vector3(.55f, 1, .46f), Vector3.down, 1));
                CollectionAssert.AreEquivalent(new[] { Swatch(48), Swatch(50) }, mesh.uv.Distinct(), "Muted pale fungus avoids the previous luminous white floor patch.");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void NicheHasAnOpenFrontAndBeddingWhileOldestPlaqueStaysAtFloorLevel(int variant)
        {
            var niche = Model("niche", variant);
            Assert.That(niche.bounds.max.y, Is.InRange(.8f, 1.2f));
            Assert.IsFalse(Hit(niche, new Vector3(0, .6f, 1), Vector3.back, .8f));
            Assert.IsTrue(Hit(niche, new Vector3(.43f, .6f, 1), Vector3.back, 1.5f));
            Assert.IsTrue(Hit(niche, new Vector3(0, .6f, 1), Vector3.back, 1.5f), "The recessed rear remains a real visible surface.");
            Assert.IsTrue(niche.vertices.Where((v, i) => niche.uv[i] == Swatch(19)).Any(v => v.y < .3f));
            Assert.Less(Model("oldest", variant).bounds.max.y, .5f);
            Assert.Greater(Model("plaque", variant).bounds.max.y, .8f);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void JarHasAHangingBulbAndNativePeopleHaveDifferentStandingAndTendingPoses(int variant)
        {
            var jar = Model("jar", variant);
            Assert.That(jar.bounds.max.y, Is.InRange(1.0f, 1.7f));
            var glow = Enumerable.Range(0, jar.vertexCount).Where(i => jar.uv[i] == Swatch(49)).Select(i => jar.vertices[i]).ToArray();
            Assert.IsNotEmpty(glow);
            Assert.That(glow.Min(v => v.y), Is.GreaterThan(.35f));
            Assert.Greater(glow.Max(v => v.x) - glow.Min(v => v.x), .2f);
            var listener = Model("listener", variant);
            var tender = Model("tender", variant);
            Assert.Greater(listener.bounds.max.y, tender.bounds.max.y + .2f);
            Assert.IsTrue(listener.vertices.Any(v => v.z > .35f && v.y > .8f), "The listener's raised palm reaches the wall.");
            Assert.IsTrue(tender.vertices.Any(v => v.z > .3f && v.y < .7f), "The plaque tender works close to the floor.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FoundingCutawayWallKeepsMutedCaveStoneAndFullCellContinuity(int variant)
        {
            var wall = Model("wall", variant);
            Assert.AreEqual(1.1f, wall.bounds.max.y, .0001f);
            Assert.LessOrEqual(wall.vertexCount, 48);
            var cliff = GinmereVoxelLibrary.Load().Find(GinmereVoxelLibrary.ModelId("cliff", variant)).Mesh;
            CollectionAssert.AreEquivalent(cliff.uv.Distinct(), wall.uv.Distinct());
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(13) }, wall.uv.Distinct());
            Assert.Greater(cliff.bounds.max.y, wall.bounds.max.y + .3f, "Only the sacred chamber uses the low cutaway model.");
            foreach (var level in wall.vertices.GroupBy(v => Mathf.Round(v.y * 10000)))
            {
                Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f);
                Assert.AreEqual(.5f, level.Max(v => v.x), .0001f);
                Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f);
                Assert.AreEqual(.5f, level.Max(v => v.z), .0001f);
            }
            Assert.IsNull(OlderdeepVoxelLibrary.Family("SandstoneWall"), "The generic alias must not lower descent cliffs.");
        }

        private static Mesh Model(string family, int variant) => OlderdeepVoxelLibrary.Load().Find(OlderdeepVoxelLibrary.ModelId(family, variant)).Mesh;

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
        public void CorruptAssetReferencesFailWithoutMutatingTheSourceLibrary(string corruption)
        {
            var source = OlderdeepVoxelLibrary.Load(); source.Validate();
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
