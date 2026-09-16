using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class WellmeetVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "floor", "tent", "cloth", "well", "host", "salt", "bed", "chair", "oven", "shrine", "lantern", "rack", "adult", "child", "corner", "path", "shelf", "marker" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = WellmeetVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(76, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(WellmeetVoxelLibrary.ModelId(family, variant));
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
                    Assert.AreEqual(family == "ground" || family == "floor" || family == "path" ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("Farmer", "adult")]
        [TestCase("WellKeeper", "adult")]
        [TestCase("Wellkeeper", null)]
        [TestCase("FungalFarmer", null)]
        [TestCase("WellGroundMarker", "marker")]
        [TestCase("OvenGroundMarker", "marker")]
        [TestCase("LanternGroundMarker", "marker")]
        [TestCase("CampfireGroundMarker", "marker")]
        [TestCase("HiddenShrineMarker", null)]
        [TestCase("RoadStone", "path")]
        [TestCase("AlchemyShelf", "shelf")]
        [TestCase("TentCorner", null)]
        [TestCase("Sand", "ground")]
        [TestCase("StoneFloor", "floor")]
        [TestCase("TentWall", "tent")]
        [TestCase("GuestClothPole", "cloth")]
        [TestCase("Well", "well")]
        [TestCase("TentRightHost", "host")]
        [TestCase("SaltMaster", "salt")]
        [TestCase("Bed", "bed")]
        [TestCase("Chair", "chair")]
        [TestCase("Oven", "oven")]
        [TestCase("Shrine", "shrine")]
        [TestCase("WatchLantern", "lantern")]
        [TestCase("WeaponRack", "rack")]
        [TestCase("Elder", "adult")]
        [TestCase("Innkeeper", "adult")]
        [TestCase("Merchant", "adult")]
        [TestCase("Quartermaster", "adult")]
        [TestCase("Scribe", "adult")]
        [TestCase("Tinker", "adult")]
        [TestCase("Villager", "adult")]
        [TestCase("Warden", "adult")]
        [TestCase("VillageChild", "child")]
        [TestCase("SandstoneWall", null)]
        [TestCase("Campfire", null)]
        [TestCase("Player", null)]
        [TestCase("GuestclothPole", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, WellmeetVoxelLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("wellmeet-ground-3", WellmeetVoxelLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => WellmeetVoxelLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => WellmeetVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => WellmeetVoxelLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => WellmeetVoxelLibrary.ModelId("ground", 4));
            var kit = WellmeetVoxelLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("wellmeet-no-such-model-0"));
        }

        [TestCase("ground")]
        [TestCase("floor")]
        [TestCase("path")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = WellmeetVoxelLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(WellmeetVoxelLibrary.ModelId(family, variant)).Mesh;
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
        public void TentPanelsJoinAcrossOneCellAndClothSignalsRiseAboveTheCutaway(int variant)
        {
            var tent = Model("tent", variant);
            Assert.That(tent.bounds.max.y, Is.InRange(.85f, 1.2f));
            Assert.AreEqual(1, tent.bounds.size.x, .0001f);
            Assert.LessOrEqual(tent.bounds.size.z, .35f);
            Assert.LessOrEqual(tent.vertexCount, 96);
            Assert.IsTrue(Hit(tent, new Vector3(0, .55f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(tent, new Vector3(0, 1.5f, 1), Vector3.back, 2));
            var cloth = Model("cloth", variant);
            Assert.That(cloth.bounds.max.y, Is.InRange(1.6f, 2.2f));
            var banner = Enumerable.Range(0, cloth.vertexCount).Where(i => cloth.uv[i] == Swatch(10) && cloth.vertices[i].y > 1.0f).Select(i => cloth.vertices[i]).ToArray();
            Assert.IsNotEmpty(banner);
            Assert.Greater(banner.Max(v => v.x) - banner.Min(v => v.x), .5f);
        }

        [Test]
        public void RepairableWellsHaveOpenCentersAndTheFouledRingVisiblyBreaksBeforeRepair()
        {
            for (int v = 0; v < 4; v++)
            {
                var mesh = Model("well", v);
                Assert.That(mesh.bounds.max.y, Is.InRange(.45f, 1.0f));
                Assert.IsFalse(Hit(mesh, new Vector3(0, 1.3f, 0), Vector3.down, .9f));
                Assert.IsTrue(Hit(mesh, new Vector3(.43f, 1.3f, 0), Vector3.down, 1.3f));
                Assert.IsTrue(Hit(mesh, new Vector3(0, 1.3f, 0), Vector3.down, 1.3f));
            }
            Assert.IsFalse(Hit(Model("well", 0), new Vector3(.22f, .46f, 1), Vector3.back, .7f));
            Assert.IsTrue(Hit(Model("well", 2), new Vector3(.22f, .46f, 1), Vector3.back, .7f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void BedChairOvenAndRackKeepDifferentFunctionalSilhouettes(int variant)
        {
            var bed = Model("bed", variant); var chair = Model("chair", variant);
            Assert.Less(bed.bounds.max.y, .65f);
            Assert.Greater(bed.bounds.size.z, .8f);
            Assert.Greater(chair.bounds.max.y, .75f);
            Assert.IsFalse(Hit(chair, new Vector3(0, .65f, 1), Vector3.back, 1));
            Assert.IsTrue(Hit(chair, new Vector3(0, .65f, 1), Vector3.back, 1.5f));
            var oven = Model("oven", variant);
            Assert.IsFalse(Hit(oven, new Vector3(0, .3f, 1), Vector3.back, .8f));
            Assert.IsTrue(Hit(oven, new Vector3(.35f, .3f, 1), Vector3.back, 1.5f));
            var rack = Model("rack", variant);
            Assert.That(rack.bounds.max.y, Is.InRange(1.0f, 1.6f));
            var iron = Enumerable.Range(0, rack.vertexCount).Where(i => rack.uv[i] == Swatch(13)).Select(i => rack.vertices[i]).ToArray();
            Assert.IsTrue(iron.Any(v => v.x < -.1f)); Assert.IsTrue(iron.Any(v => v.x > .1f));
        }

        [Test]
        public void HostsSaltWorkersAndChildrenRemainRecognizableAndServiceAdultVariantsCarryTheirActualTools()
        {
            for (int v = 0; v < 4; v++)
            {
                Assert.That(Model("host", v).bounds.max.y, Is.InRange(1.2f, 1.7f));
                Assert.That(Model("salt", v).bounds.max.y, Is.InRange(1.2f, 1.7f));
                Assert.Less(Model("child", v).bounds.max.y, 1.05f);
                Assert.Greater(Model("adult", v).bounds.max.y, 1.2f);
                var sleeve = Model("salt", v);
                Assert.IsTrue(sleeve.vertices.Where((p, i) => sleeve.uv[i] == Swatch(19)).Any(p => Mathf.Abs(p.x) > .25f && p.y > .5f && p.y < 1.0f));
                CollectionAssert.AreNotEquivalent(Model("host", v).uv.Distinct(), Model("salt", v).uv.Distinct());
            }
            var goods = Model("adult", 0); var ledger = Model("adult", 1); var tool = Model("adult", 2); var traveler = Model("adult", 3);
            Assert.IsTrue(goods.vertices.Any(v => v.z > .33f && v.y > .35f && v.y < .75f));
            Assert.IsTrue(ledger.vertices.Any(v => v.z > .27f && v.y > .8f && v.y < 1.05f));
            Assert.IsTrue(tool.vertices.Any(v => v.x > .32f && v.y > 1.15f));
            Assert.IsFalse(traveler.vertices.Any(v => v.z > .30f && v.y < 1.0f), "The plain traveler is the no-accessory control.");
        }

        [Test]
        public void LanternRepairChangesTheCoreLightSwatchAndShrineRemainsASmallWalkableFixture()
        {
            var dim = Model("lantern", 0); var repaired = Model("lantern", 2);
            Assert.That(repaired.bounds.max.y, Is.InRange(1.5f, 2.1f));
            CollectionAssert.AreNotEquivalent(dim.uv.Distinct(), repaired.uv.Distinct());
            for (int v = 0; v < 4; v++)
            {
                Assert.Less(Model("shrine", v).bounds.max.y, .75f);
                Assert.LessOrEqual(Model("shrine", v).vertexCount, 144);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TentCornerJoinsEastAndNorthHalfPanelsAndLeavesTheInteriorOpen(int variant)
        {
            var corner = Model("corner", variant);
            Assert.That(corner.bounds.max.y, Is.InRange(.85f, 1.2f));
            Assert.LessOrEqual(corner.vertexCount, 144);
            Assert.IsTrue(Hit(corner, new Vector3(.42f, .5f, 1), Vector3.back, 1.2f));
            Assert.IsTrue(Hit(corner, new Vector3(1, .5f, .42f), Vector3.left, 1.2f));
            Assert.IsFalse(Hit(corner, new Vector3(-.30f, 1.5f, -.30f), Vector3.down, 1.5f));
            Assert.AreEqual(.5f, corner.bounds.max.x, .0001f);
            Assert.AreEqual(.5f, corner.bounds.max.z, .0001f);
            CollectionAssert.AreEquivalent(Model("tent", variant).uv.Distinct(), corner.uv.Distinct());
        }

        [Test]
        public void PackedPathsUseOneLighterEarthSwatchThanIndoorFloor()
        {
            var path = Model("path", 0); var floor = Model("floor", 0); var sand = Model("ground", 0);
            Assert.AreEqual(1, path.uv.Distinct().Count());
            Assert.AreEqual(Swatch(7), path.uv[0]);
            Assert.AreNotEqual(floor.uv[0], path.uv[0]);
            Assert.AreNotEqual(sand.uv[0], path.uv[0]);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void AlchemyShelfHasAnOpenWorkingFrontAndVisibleContainers(int variant)
        {
            var shelf = Model("shelf", variant);
            Assert.That(shelf.bounds.max.y, Is.InRange(.9f, 1.5f));
            Assert.IsFalse(Hit(shelf, new Vector3(0, .8f, 1), Vector3.back, .9f));
            Assert.IsTrue(Hit(shelf, new Vector3(.40f, .8f, 1), Vector3.back, 1.5f));
            var vessels = Enumerable.Range(0, shelf.vertexCount).Where(i => shelf.uv[i] == Swatch(22)).Select(i => shelf.vertices[i]).ToArray();
            Assert.IsNotEmpty(vessels);
            Assert.IsTrue(vessels.Any(v => v.x < -.1f));
            Assert.IsTrue(vessels.Any(v => v.x > .1f));
            Assert.IsTrue(vessels.Any(v => v.y > .5f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void GroundMarkersAreQuietFlatScuffsWithBrokenPerimetersInsteadOfAshPiles(int variant)
        {
            var marker = Model("marker", variant);
            Assert.That(marker.bounds.max.y, Is.InRange(.01f, .045f));
            Assert.GreaterOrEqual(marker.bounds.min.y, 0);
            Assert.LessOrEqual(marker.vertexCount, 72);
            Assert.That(marker.bounds.size.x, Is.InRange(.55f, .95f));
            Assert.That(marker.bounds.size.z, Is.InRange(.45f, .95f));
            Assert.IsTrue(Hit(marker, new Vector3(0, 1, 0), Vector3.down, 1));
            Assert.IsFalse(Hit(marker, new Vector3(.48f, 1, .48f), Vector3.down, 1));
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(64) }, marker.uv.Distinct());
            Assert.AreEqual("entity", WellmeetVoxelLibrary.Load().Find(WellmeetVoxelLibrary.ModelId("marker", variant)).Spec.kind,
                "A marker is an overlay owned by its actual native entity, not substitute base terrain.");
            Assert.Greater(Model("well", 2).bounds.max.y, marker.bounds.max.y * 8);
        }

        private static Mesh Model(string family, int variant) => WellmeetVoxelLibrary.Load().Find(WellmeetVoxelLibrary.ModelId(family, variant)).Mesh;

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
            var source = WellmeetVoxelLibrary.Load(); source.Validate();
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
