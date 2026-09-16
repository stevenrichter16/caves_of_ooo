using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class CinderholdVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "wall", "factor", "notice", "forge", "anvil", "smith", "stall", "token", "path" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = CinderholdVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(40, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(CinderholdVoxelKitLibrary.ModelId(family, variant));
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

        [TestCase("Grass", "ground")]
        [TestCase("Floor", "ground")]
        [TestCase("SandstoneWall", "wall")]
        [TestCase("ConcordFactor", "factor")]
        [TestCase("CinderholdNoticeBoard", "notice")]
        [TestCase("TinkersForge", "forge")]
        [TestCase("SmithAnvil", "anvil")]
        [TestCase("Weaponsmith", "smith")]
        [TestCase("MarketStall", "stall")]
        [TestCase("RoadStone", "path")]
        [TestCase("CrunchyLocket", null)]
        [TestCase("Marketstall", null)]
        [TestCase("StoneFloor", null)]
        [TestCase("StoneWall", null)]
        [TestCase("Oven", null)]
        [TestCase("Tinker", null)]
        [TestCase("Concordfactor", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, CinderholdVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("cinderhold-ground-3", CinderholdVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => CinderholdVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => CinderholdVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CinderholdVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CinderholdVoxelKitLibrary.ModelId("ground", 4));
            var kit = CinderholdVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("cinderhold-no-such-model-0"));
        }

        [TestCase("ground")]
        [TestCase("path")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = CinderholdVoxelKitLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(CinderholdVoxelKitLibrary.ModelId(family, variant)).Mesh;
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
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(56) }, wall.uv.Distinct());
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
        public void ForgeHasAnOpenCoalBowlAndRearChimneyWhileAnvilHasAHornAndNarrowWaist(int variant)
        {
            var forge = Model("forge", variant); var anvil = Model("anvil", variant);
            Assert.That(forge.bounds.max.y, Is.InRange(1.3f, 1.8f));
            Assert.That(anvil.bounds.max.y, Is.InRange(.55f, .85f));
            Assert.Greater(forge.bounds.max.y, anvil.bounds.max.y + .5f);
            Assert.IsFalse(Hit(forge, new Vector3(0, 1, .10f), Vector3.down, .50f), "The native walkable forge's bowl is open above its lower coal bed.");
            Assert.IsTrue(Hit(forge, new Vector3(.30f, 1, .10f), Vector3.down, 1));
            Assert.IsTrue(forge.vertices.Any(v => v.z < -.20f && v.y > 1.1f));
            Assert.IsTrue(forge.vertices.Where((v, i) => forge.uv[i] == Swatch(49)).Any(v => v.y < .5f));
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(49) }, forge.uv.Distinct());
            Assert.IsTrue(Hit(anvil, new Vector3(0, .35f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(anvil, new Vector3(.25f, .35f, 1), Vector3.back, 2));
            Assert.IsTrue(Hit(anvil, new Vector3(.44f, 1, 0), Vector3.down, .6f));
            Assert.IsFalse(Hit(anvil, new Vector3(-.44f, 1, 0), Vector3.down, .6f));
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(13) }, anvil.uv.Distinct());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FactorAndNoticeReadAsAdministrationWhileSmithShowsBroadWorkingForearms(int variant)
        {
            var factor = Model("factor", variant); var notice = Model("notice", variant); var smith = Model("smith", variant);
            Assert.That(factor.bounds.max.y, Is.InRange(1.2f, 1.7f));
            Assert.That(smith.bounds.max.y, Is.InRange(1.2f, 1.7f));
            CollectionAssert.AreEquivalent(new[] { Swatch(19), Swatch(12) }, factor.uv.Distinct());
            var coat = factor.vertices.Where((v, i) => factor.uv[i] == Swatch(19) && v.y < .8f).ToArray();
            Assert.IsNotEmpty(coat);
            Assert.Greater(coat.Max(v => v.x) - coat.Min(v => v.x), .35f);
            var forearms = smith.vertices.Where((v, i) => smith.uv[i] == Swatch(32) && v.y > .6f && v.y < 1.15f).ToArray();
            Assert.IsTrue(forearms.Any(v => v.x < -.35f));
            Assert.IsTrue(forearms.Any(v => v.x > .35f));
            Assert.That(notice.bounds.max.y, Is.InRange(1.2f, 1.7f));
            var paper = notice.vertices.Where((v, i) => notice.uv[i] == Swatch(19) && v.y > .8f).ToArray();
            Assert.IsTrue(paper.Any(v => v.x < -.12f));
            Assert.IsTrue(paper.Any(v => v.x > .12f));
            Assert.Greater(paper.Max(v => v.x) - paper.Min(v => v.x), .5f);
            Assert.IsFalse(Hit(notice, new Vector3(0, .3f, 1), Vector3.back, 2), "Two supports must not become a solid pedestal slab.");
            Assert.IsTrue(Hit(notice, new Vector3(.30f, .3f, 1), Vector3.back, 2));
        }

        [Test]
        public void ClearedForestGroundUsesOneMutedEarthSwatch()
        {
            Assert.AreEqual(Swatch(75), Model("ground", 0).uv[0]);
            Assert.AreNotEqual(Model("wall", 0).uv[0], Model("ground", 0).uv[0]);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MarketStallHasALowCounterAnOpenServingGapAndABroadAwning(int variant)
        {
            var stall = Model("stall", variant);
            Assert.That(stall.bounds.max.y, Is.InRange(1.3f, 1.7f));
            Assert.Greater(stall.bounds.size.x, .85f);
            Assert.Greater(stall.bounds.size.z, .65f);
            Assert.IsTrue(Hit(stall, new Vector3(0, .65f, 1), Vector3.back, 2), "A counter must occupy the working front.");
            Assert.IsFalse(Hit(stall, new Vector3(0, 1.1f, 1), Vector3.back, 2), "The serving gap must remain open under the awning.");
            Assert.IsTrue(Hit(stall, new Vector3(.4f, 2, 0), Vector3.down, .7f), "A broad canopy is recognizable at gameplay distance.");
            Assert.AreEqual(2, stall.uv.Distinct().Count());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void CarvedQuestTokenHasAPendantAndAnOpenCordLoopWithoutClaimingDynamicOwners(int variant)
        {
            var token = Model("token", variant);
            Assert.That(token.bounds.max.y, Is.InRange(.06f, .22f));
            Assert.That(token.bounds.size.x, Is.InRange(.3f, .7f));
            Assert.That(token.bounds.size.z, Is.InRange(.5f, .95f));
            Assert.IsTrue(Hit(token, new Vector3(0, .5f, .2f), Vector3.down, .5f), "The carved pendant is a visible solid shape.");
            Assert.IsFalse(Hit(token, new Vector3(0, .5f, -.2f), Vector3.down, .5f), "Its cord loop has a genuine opening.");
            Assert.IsTrue(Hit(token, new Vector3(-.18f, .5f, -.2f), Vector3.down, .5f), "The opening control must retain an actual cord beside it.");
            Assert.AreEqual(2, token.uv.Distinct().Count());
            Assert.IsNull(CinderholdVoxelKitLibrary.Family("CrunchyLocket"), "Only the runtime quest Part contract authorizes this dynamic item.");
        }

        [Test]
        public void ForestRoadUsesMutedPackedStoneInsteadOfTheBrightDesertRoadSwatch()
        {
            var path = Model("path", 0);
            Assert.AreEqual(Swatch(64), path.uv[0]);
            Assert.AreNotEqual(Model("ground", 0).uv[0], path.uv[0]);
            Assert.AreNotEqual(WellmeetVoxelLibrary.Load().Find(WellmeetVoxelLibrary.ModelId("path", 0)).Mesh.uv[0], path.uv[0]);
        }

        private static Mesh Model(string family, int variant) => CinderholdVoxelKitLibrary.Load().Find(CinderholdVoxelKitLibrary.ModelId(family, variant)).Mesh;

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
            var source = CinderholdVoxelKitLibrary.Load(); source.Validate();
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
