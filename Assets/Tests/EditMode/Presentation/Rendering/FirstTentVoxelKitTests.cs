using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class FirstTentVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "path", "tent", "corner", "cloth", "host" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = FirstTentVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(24, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(FirstTentVoxelKitLibrary.ModelId(family, variant));
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

        [TestCase("Sand", "ground")]
        [TestCase("RoadStone", "path")]
        [TestCase("TentWall", "tent")]
        [TestCase("GuestClothPole", "cloth")]
        [TestCase("TentRightHost", "host")]
        [TestCase("SaltMaster", null)]
        [TestCase("Well", null)]
        [TestCase("StoneFloor", null)]
        [TestCase("SandstoneWall", null)]
        [TestCase("FirstTentMonument", null)]
        [TestCase("GuestclothPole", null)]
        [TestCase("SaccharineEnvoy", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, FirstTentVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("firsttent-ground-3", FirstTentVoxelKitLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => FirstTentVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => FirstTentVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => FirstTentVoxelKitLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => FirstTentVoxelKitLibrary.ModelId("ground", 4));
            var kit = FirstTentVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("firsttent-no-such-model-0"));
        }

        [Test]
        public void GroundAndApproachAreContinuousQuietPlanesWithoutVisibleVariantNoise()
        {
            foreach (string family in new[] { "ground", "path" })
                for (int variant = 0; variant < 4; variant++)
                {
                    var mesh = Model(family, variant);
                    Assert.LessOrEqual(mesh.vertexCount, 48);
                    Assert.AreEqual(1, mesh.uv.Distinct().Count());
                    Assert.AreEqual(Swatch(family == "ground" ? 64 : 106), mesh.uv[0]);
                    Assert.That(mesh.bounds.size.x, Is.EqualTo(1).Within(.0001f));
                    Assert.That(mesh.bounds.size.z, Is.EqualTo(1).Within(.0001f));
                    var tops = mesh.vertices.Where((v, i) => mesh.normals[i].y > .9f).ToArray();
                    Assert.IsNotEmpty(tops);
                    Assert.IsTrue(tops.All(v => Mathf.Abs(v.y) < .0001f), "Buried thickness may vary; visible height and palette remain continuous.");
                }
            Assert.AreNotEqual(Model("ground", 0).uv[0], Model("path", 0).uv[0], "The native approach must still be distinguishable.");
        }

        [TestCase("ground", 64)]
        [TestCase("path", 106)]
        public void DesertAndApproachUseSubduedPaletteWithoutErasingTheirQuietContrast(string family, int expectedSwatch)
        {
            var palette = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(palette.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "Art3D/SpawnRing/Textures/SpawnRingPalette.png"))));
                for (int variant = 0; variant < 4; variant++)
                {
                    var mesh = Model(family, variant);
                    Assert.AreEqual(Swatch(expectedSwatch), mesh.uv[0], "Fix the shared generation palette, not one manually repainted demonstration.");
                    Color c = palette.GetPixel(Mathf.FloorToInt(mesh.uv[0].x * palette.width), Mathf.FloorToInt(mesh.uv[0].y * palette.height));
                    Assert.LessOrEqual(Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)), .14f);
                    Assert.LessOrEqual(Luminance(c), .50f, "Ground must not outshine the occupied shelters and guest-cloth court.");
                }
                Vector2 sandUV = Model("ground", 0).uv[0], pathUV = Model("path", 0).uv[0];
                Color sand = palette.GetPixel(Mathf.FloorToInt(sandUV.x * palette.width), Mathf.FloorToInt(sandUV.y * palette.height));
                Color path = palette.GetPixel(Mathf.FloorToInt(pathUV.x * palette.width), Mathf.FloorToInt(pathUV.y * palette.height));
                Assert.That(Luminance(sand) - Luminance(path), Is.InRange(.03f, .08f), "A quiet but nonzero route contrast is the countercontrol: neither a painted road network nor an erased path.");
                Assert.AreEqual(Swatch(12), Model("tent", 0).uv[0], "Shelter fabric remains unchanged while the overly bright ground is corrected.");
            }
            finally { UnityEngine.Object.DestroyImmediate(palette); }
        }

        private static float Luminance(Color c) => .2126f * c.r + .7152f * c.g + .0722f * c.b;

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void GuestClothIsBroadDarkFabricOnOneStraightPoleWithoutAnAltarBase(int variant)
        {
            var mesh = Model("cloth", variant);
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(32) }, mesh.uv.Distinct());
            Assert.That(mesh.bounds.max.y, Is.InRange(1.95f, 2.4f));
            var fabric = mesh.vertices.Where((v, i) => mesh.uv[i] == Swatch(12)).ToArray();
            Assert.Greater(fabric.Max(v => v.x) - fabric.Min(v => v.x), .65f);
            Assert.Greater(fabric.Max(v => v.y) - fabric.Min(v => v.y), .45f);
            Assert.Greater(fabric.Min(v => v.y), 1.1f);
            Assert.IsTrue(Hit(mesh, new Vector3(-.30f, .20f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(mesh, new Vector3(.20f, .20f, 1), Vector3.back, 2), "No altar, fence or additional solid architecture is attached to the nonsolid pole owner.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void GoathairWallAndCornerKeepActualOpenInteriorsAndCoarseFabricPanels(int variant)
        {
            var wall = Model("tent", variant); var corner = Model("corner", variant);
            foreach (var mesh in new[] { wall, corner })
            {
                CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(64) }, mesh.uv.Distinct());
                Assert.That(mesh.bounds.max.y, Is.InRange(.95f, 1.2f));
                Assert.LessOrEqual(mesh.vertexCount, 120);
            }
            Assert.That(wall.bounds.size.x, Is.EqualTo(1).Within(.0001f));
            Assert.Less(wall.bounds.size.z, .3f);
            Assert.Greater(corner.bounds.size.z, .45f);
            Assert.IsTrue(Hit(wall, new Vector3(.2f, .5f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(wall, new Vector3(.2f, .5f, .3f), Vector3.down, .5f));
            Assert.IsFalse(Hit(corner, new Vector3(.25f, .5f, .25f), Vector3.down, .5f), "The L joins two faces, leaving the room side open rather than a solid cube.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void HostHasAWeatheredHumanSilhouetteAndOpenWelcomingHand(int variant)
        {
            var mesh = Model("host", variant);
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(32) }, mesh.uv.Distinct());
            Assert.That(mesh.bounds.max.y, Is.InRange(1.35f, 1.7f));
            Assert.IsTrue(Hit(mesh, new Vector3(.12f, .2f, 1), Vector3.back, 2));
            Assert.IsFalse(Hit(mesh, new Vector3(0, .2f, 1), Vector3.back, 2), "Two legs stay separate; no dais or pedestal is attached.");
            Assert.IsTrue(mesh.vertices.Where((v, i) => mesh.uv[i] == Swatch(32)).Any(v => v.x > .2f && v.z > .3f && v.y < 1));
        }

        private static Mesh Model(string family, int variant) => FirstTentVoxelKitLibrary.Load().Find(FirstTentVoxelKitLibrary.ModelId(family, variant)).Mesh;

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
            var source = FirstTentVoxelKitLibrary.Load(); source.Validate();
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
