using System;
using System.Linq;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class DrownedLedgerVoxelKitTests
    {
        private static readonly string[] Families = { "preserved", "stake", "table", "scribe", "sorter", "parcel", "boards" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = DrownedLedgerVoxelKitLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(28, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(DrownedLedgerVoxelKitLibrary.ModelId(family, variant));
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

        [TestCase("PreFellingBody", "preserved")]
        [TestCase("SurveyStake", "stake")]
        [TestCase("ReadingTable", "table")]
        [TestCase("RecensionScribe", "scribe")]
        [TestCase("CurationSorter", "sorter")]
        [TestCase("SealedBogTakenBody", "parcel")]
        [TestCase("Duckboard", "boards")]
        [TestCase("DuckBoard", null)]
        [TestCase("BogTakenBody", null)]
        [TestCase("SaltCuredBody", null)]
        [TestCase("FilerClerk", null)]
        [TestCase("Scribe", null)]
        [TestCase("Palimpsest", null)]
        [TestCase("Floor", null)]
        [TestCase("TentWall", null)]
        [TestCase("PreFellingbody", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, DrownedLedgerVoxelKitLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("drownedledger-preserved-3", DrownedLedgerVoxelKitLibrary.ModelId("preserved", 3));
            Assert.Throws<ArgumentException>(() => DrownedLedgerVoxelKitLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => DrownedLedgerVoxelKitLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => DrownedLedgerVoxelKitLibrary.ModelId("preserved", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DrownedLedgerVoxelKitLibrary.ModelId("preserved", 4));
            var kit = DrownedLedgerVoxelKitLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-preserved-0"));
            Assert.IsNull(kit.Find("drownedledger-no-such-model-0"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void PreservedWitnessHasACompleteProneHumanSilhouetteDistinctFromTheWrappedParcel(int variant)
        {
            var body = Model("preserved", variant); var parcel = Model("parcel", variant);
            Assert.That(body.bounds.max.y, Is.InRange(.16f, .4f));
            Assert.That(body.bounds.size.z, Is.InRange(.75f, 1f));
            Assert.Greater(body.bounds.size.z, body.bounds.size.x);
            Assert.IsTrue(Hit(body, new Vector3(0, .6f, -.34f), Vector3.down, .6f), "The witness has a head at its own end of the body.");
            Assert.IsTrue(Hit(body, new Vector3(-.11f, .6f, .39f), Vector3.down, .6f));
            Assert.IsTrue(Hit(body, new Vector3(.11f, .6f, .39f), Vector3.down, .6f));
            Assert.IsFalse(Hit(body, new Vector3(0, .6f, .39f), Vector3.down, .6f), "Separate feet must not become an anonymous rectangular parcel.");
            CollectionAssert.AreEquivalent(new[] { Swatch(13), Swatch(79) }, body.uv.Distinct());
            CollectionAssert.AreEquivalent(new[] { Swatch(12), Swatch(19) }, parcel.uv.Distinct());
            Assert.That(parcel.bounds.max.y, Is.InRange(.2f, .5f));
            Assert.Greater(parcel.bounds.size.z, .7f);
            Assert.IsTrue(Hit(parcel, new Vector3(0, .6f, .32f), Vector3.down, .6f), "The sealed wrapping closes across the parcel's foot end.");
            var binding = parcel.vertices.Where((v, i) => parcel.uv[i] == Swatch(19)).ToArray();
            Assert.IsTrue(binding.Any(v => v.z < -.1f)); Assert.IsTrue(binding.Any(v => v.z > .1f));
            Assert.Greater(binding.Max(v => v.y), .22f);
            Assert.AreNotEqual(string.Join(";", body.vertices.Select(v => v.ToString("F4"))),
                string.Join(";", parcel.vertices.Select(v => v.ToString("F4"))));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ReadingTableLeavesItsCentralWorkingSurfaceBareAndSurveyStakeRemainsNarrow(int variant)
        {
            var table = Model("table", variant); var stake = Model("stake", variant);
            Assert.That(table.bounds.max.y, Is.InRange(.8f, 1.2f));
            Assert.Greater(table.bounds.size.x, .7f); Assert.Greater(table.bounds.size.z, .65f);
            foreach (float x in new[] { -.12f, 0, .12f })
            {
                Assert.IsFalse(Hit(table, new Vector3(x, 1.25f, 0), Vector3.down, .4f), "No fourth preserved body may be built onto the central table.");
                Assert.IsTrue(Hit(table, new Vector3(x, .85f, 0), Vector3.down, .2f), "A bare supporting tabletop must still exist.");
            }
            Assert.IsFalse(Hit(table, new Vector3(0, .3f, 1), Vector3.back, 2), "The furniture retains separate supports rather than a filled stone-coffer body.");
            Assert.IsTrue(Hit(table, new Vector3(.32f, .3f, 1), Vector3.back, 2));
            Assert.That(stake.bounds.max.y, Is.InRange(.85f, 1.5f));
            Assert.Less(stake.bounds.size.x, .36f); Assert.Less(stake.bounds.size.z, .3f);
            CollectionAssert.AreEquivalent(new[] { Swatch(8), Swatch(13) }, stake.uv.Distinct());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ScribeAndSorterHaveDistinctWorkingPalettesAndHeldRecordsWithoutAttachedFurniture(int variant)
        {
            var scribe = Model("scribe", variant); var sorter = Model("sorter", variant);
            CollectionAssert.AreEquivalent(new[] { Swatch(22), Swatch(19) }, scribe.uv.Distinct());
            CollectionAssert.AreEquivalent(new[] { Swatch(35), Swatch(12) }, sorter.uv.Distinct());
            foreach (var actor in new[] { scribe, sorter })
            {
                Assert.That(actor.bounds.max.y, Is.InRange(1.2f, 1.7f));
                Assert.Less(actor.bounds.size.x, .85f); Assert.Less(actor.bounds.size.z, .7f);
                Assert.IsFalse(Hit(actor, new Vector3(0, .18f, 1), Vector3.back, 2), "A walking actor has separate legs, not a desk or platform.");
                Assert.IsTrue(Hit(actor, new Vector3(.12f, .18f, 1), Vector3.back, 2));
            }
            Assert.IsTrue(scribe.vertices.Where((v, i) => scribe.uv[i] == Swatch(19)).Any(v => v.z > .23f && v.y > .7f && v.y < 1.2f));
            Assert.IsTrue(sorter.vertices.Where((v, i) => sorter.uv[i] == Swatch(12)).Any(v => v.z > .2f && v.y > .7f && v.y < 1.2f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void WholeWitnessSilhouetteContrastsWithItsActualDarkInteriorFloor(int variant)
        {
            var body = Model("preserved", variant);
            var floor = WellmeetVoxelLibrary.Load().Find(WellmeetVoxelLibrary.ModelId("floor", 0)).Mesh;
            var palette = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(palette.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "Art3D/SpawnRing/Textures/SpawnRingPalette.png"))));
                Color ground = palette.GetPixel(Mathf.FloorToInt(floor.uv[0].x * palette.width), Mathf.FloorToInt(floor.uv[0].y * palette.height));
                float groundLuma = .2126f * ground.r + .7152f * ground.g + .0722f * ground.b;
                foreach (var uv in body.uv.Distinct())
                {
                    Assert.AreNotEqual(floor.uv[0], uv, "The torso and limbs must not use their floor's exact swatch.");
                    Color color = palette.GetPixel(Mathf.FloorToInt(uv.x * palette.width), Mathf.FloorToInt(uv.y * palette.height));
                    float luma = .2126f * color.r + .7152f * color.g + .0722f * color.b;
                    Assert.Greater(luma, groundLuma + .10f, "Head-only contrast is insufficient: the entire intact silhouette must remain readable.");
                }
                Assert.AreEqual(Swatch(12), floor.uv[0], "Countercontrol preserves the intentionally dark interior, rather than brightening everything.");
            }
            finally { UnityEngine.Object.DestroyImmediate(palette); }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ExcavationDuckboardsUseQuietWeatheredPlanksWithActualGapsAndLowSupports(int variant)
        {
            var boards = Model("boards", variant);
            Assert.LessOrEqual(boards.vertexCount, 144);
            Assert.That(boards.bounds.max.y, Is.InRange(.10f, .18f));
            Assert.Greater(boards.bounds.size.x, .9f); Assert.Greater(boards.bounds.size.z, .9f);
            CollectionAssert.AreEquivalent(new[] { Swatch(106), Swatch(64) }, boards.uv.Distinct());
            Assert.IsTrue(Hit(boards, new Vector3(0, .6f, 0), Vector3.down, .6f));
            Assert.IsFalse(Hit(boards, new Vector3(.155f, .6f, 0), Vector3.down, .6f), "Plank spacing remains a coarse physical gap, not a texture stripe.");
            Assert.IsTrue(Hit(boards, new Vector3(.155f, .04f, 1), Vector3.back, 2), "The open-gap countercontrol retains lower sleepers.");
            var palette = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(palette.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "Art3D/SpawnRing/Textures/SpawnRingPalette.png"))));
                foreach (var uv in boards.uv.Distinct())
                {
                    var c = palette.GetPixel(Mathf.FloorToInt(uv.x * palette.width), Mathf.FloorToInt(uv.y * palette.height));
                    Assert.LessOrEqual(Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)), .15f,
                        "Muted planks must not reintroduce the saturated yellow road across the excavation.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(palette); }
        }

        private static Mesh Model(string family, int variant) => DrownedLedgerVoxelKitLibrary.Load().Find(DrownedLedgerVoxelKitLibrary.ModelId(family, variant)).Mesh;

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
            var source = DrownedLedgerVoxelKitLibrary.Load(); source.Validate();
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
