using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class SumpholdVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "wall", "hull", "rolls", "cutter", "water" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = SumpholdVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(24, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(SumpholdVoxelKitLibrary.ModelId(family, variant));
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
                    Assert.AreEqual(family == "ground" ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("Floor", "ground")]
        [TestCase("SandstoneWall", "wall")]
        [TestCase("BoatFrame", "hull")]
        [TestCase("TollRolls", "rolls")]
        [TestCase("PeatCutter", "cutter")]
        [TestCase("Grass", null)]
        [TestCase("StoneFloor", null)]
        [TestCase("WaterPuddle", "water")]
        [TestCase("MirePool", null)]
        [TestCase("Waterpuddle", null)]
        [TestCase("Duckboard", null)]
        [TestCase("PeatBank", null)]
        [TestCase("Reeds", null)]
        [TestCase("Boat", null)]
        [TestCase("Tollrolls", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, SumpholdVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("sumphold-ground-3", SumpholdVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => SumpholdVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => SumpholdVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SumpholdVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SumpholdVoxelKitLibrary.ModelId("ground", 4));
            var kit = SumpholdVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("sumphold-no-such-model-0"));
        }

        [TestCase("ground")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = SumpholdVoxelKitLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(SumpholdVoxelKitLibrary.ModelId(family, variant)).Mesh;
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
        public void MasonryWallsJoinAsQuietFullCellCutawaysWithoutTinyCaps(int variant)
        {
            var wall = Model("wall", variant);
            Assert.That(wall.bounds.max.y, Is.InRange(.95f, 1.15f));
            Assert.LessOrEqual(wall.vertexCount, 48);
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(48) }, wall.uv.Distinct());
            foreach (var level in wall.vertices.GroupBy(v => Mathf.Round(v.y * 10000)))
            {
                Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f);
                Assert.AreEqual(.5f, level.Max(v => v.x), .0001f);
                Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f);
                Assert.AreEqual(.5f, level.Max(v => v.z), .0001f);
            }
            Assert.IsTrue(Hit(wall, new Vector3(.45f, .7f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(wall, new Vector3(.45f, 1.4f, 1), Vector3.back, 2));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void InvertedHullHasRaisedKeelBareRibGapsAndTwoTrestlesWithinOneCell(int variant)
        {
            var hull = Model("hull", variant);
            Assert.That(hull.bounds.max.y, Is.InRange(.65f, 1.1f));
            Assert.Greater(hull.bounds.size.x, .85f);
            Assert.Greater(hull.bounds.size.z, .65f);
            var keel = hull.vertices.Where(v => v.y > hull.bounds.max.y - .02f).ToArray();
            Assert.Greater(keel.Max(v => v.x) - keel.Min(v => v.x), .8f);
            Assert.Less(keel.Max(v => v.z) - keel.Min(v => v.z), .2f);
            Assert.IsTrue(Hit(hull, new Vector3(0, 1.2f, 0), Vector3.down, 1.2f));
            Assert.IsTrue(Hit(hull, new Vector3(0, 1.2f, .28f), Vector3.down, 1.2f));
            Assert.IsFalse(Hit(hull, new Vector3(0, 1.2f, .15f), Vector3.down, 1.2f), "A frame must retain bare gaps; no solid roof or boat vehicle shell.");
            Assert.IsFalse(Hit(hull, new Vector3(0, .2f, 1), Vector3.back, 2));
            Assert.IsTrue(Hit(hull, new Vector3(.28f, .2f, 1), Vector3.back, 2));
            Assert.IsTrue(Hit(hull, new Vector3(-.28f, .2f, 1), Vector3.back, 2));
            CollectionAssert.AreEquivalent(new[] { Swatch(9), Swatch(12) }, hull.uv.Distinct());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TollRollsStayVisibleUnderTheirRainHoodAndCutterHasThighBootsAndASpade(int variant)
        {
            var rolls = Model("rolls", variant); var cutter = Model("cutter", variant);
            Assert.That(rolls.bounds.max.y, Is.InRange(1.1f, 1.5f));
            Assert.IsTrue(Hit(rolls, new Vector3(0, 1.6f, 0), Vector3.down, .5f));
            Assert.IsFalse(Hit(rolls, new Vector3(0, .8f, 1), Vector3.back, .8f), "The record stand retains an open working front.");
            var papers = rolls.vertices.Where((v, i) => rolls.uv[i] == Swatch(19)).ToArray();
            Assert.IsNotEmpty(papers);
            Assert.Less(papers.Max(v => v.y), rolls.bounds.max.y - .2f);
            Assert.IsTrue(papers.Any(v => v.x < -.1f)); Assert.IsTrue(papers.Any(v => v.x > .1f));
            Assert.That(cutter.bounds.max.y, Is.InRange(1.2f, 1.7f));
            var boots = cutter.vertices.Where((v, i) => cutter.uv[i] == Swatch(12) && v.y > .5f && v.y < .8f).ToArray();
            Assert.IsTrue(boots.Any(v => v.x < -.08f)); Assert.IsTrue(boots.Any(v => v.x > .08f));
            var blade = cutter.vertices.Where((v, i) => cutter.uv[i] == Swatch(13) && v.x > .33f && v.y < .35f).ToArray();
            Assert.IsNotEmpty(blade);
            Assert.Greater(blade.Max(v => v.z) - blade.Min(v => v.z), .15f);
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(13) }, cutter.uv.Distinct());
        }

        [Test]
        public void RaisedBogGroundKeepsTheExistingQuietSoddenEarthSwatch()
        {
            Assert.AreEqual(Swatch(68), Model("ground", 0).uv[0]);
            Assert.AreEqual(SoddenVoxelLibrary.Load().Find(SoddenVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0], Model("ground", 0).uv[0]);
            Assert.AreNotEqual(Model("wall", 0).uv[0], Model("ground", 0).uv[0]);
        }

        [Test]
        public void RealWaterPoolsHaveOneContinuousDeepTealSurfaceWithNoGrassCheckerPattern()
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var water = Model("water", variant);
                Assert.LessOrEqual(water.vertexCount, 48);
                Assert.AreEqual(1, water.uv.Distinct().Count());
                Assert.AreEqual(Swatch(24), water.uv[0]);
                Assert.AreNotEqual(Model("ground", variant).uv[0], water.uv[0]);
                Assert.AreEqual(1, water.bounds.size.x, .0001f);
                Assert.AreEqual(1, water.bounds.size.z, .0001f);
                Assert.That(water.bounds.max.y, Is.InRange(0, .025f));
                Assert.LessOrEqual(water.bounds.size.y, .12f);
                Assert.AreEqual(Model("water", 0).bounds.max.y, water.bounds.max.y, .0001f);
                Assert.IsTrue(Hit(water, new Vector3(.45f, .4f, .45f), Vector3.down, .5f));
                Assert.IsFalse(Hit(water, new Vector3(.55f, .4f, .45f), Vector3.down, .5f));
                Assert.AreEqual("entity", SumpholdVoxelKitLibrary.Load().Find(SumpholdVoxelKitLibrary.ModelId("water", variant)).Spec.kind,
                    "The visible pool follows its native owner rather than becoming a permanent floor coating.");
            }
        }

        private static Mesh Model(string family, int variant) => SumpholdVoxelKitLibrary.Load().Find(SumpholdVoxelKitLibrary.ModelId(family, variant)).Mesh;

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
            var source = SumpholdVoxelKitLibrary.Load(); source.Validate();
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
