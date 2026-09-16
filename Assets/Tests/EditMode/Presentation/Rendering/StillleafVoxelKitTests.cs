using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class StillleafVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "floor", "tepuibone", "marble", "iron", "door", "open-door", "shelf", "bear", "slime", "spring", "boots", "wall" };

        [Test]
        public void EveryFamilyHasFourCoarseSingleCellAssetsWithExactReferences()
        {
            var kit = StillleafVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(52, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var entry = kit.Find(StillleafVoxelLibrary.ModelId(family, variant));
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
                    Assert.AreEqual((family == "ground" || family == "floor") ? "ground" : "entity", entry.Spec.kind, entry.Id);
                    shapes[variant] = string.Join(";", entry.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [TestCase("SandstoneFloor", "ground")]
        [TestCase("StoneFloor", "ground")]
        [TestCase("SealedLibraryFloor", "floor")]
        [TestCase("LibraryTepuiboneWall", "tepuibone")]
        [TestCase("LibraryMemoryMarbleWall", "marble")]
        [TestCase("LibraryChoirIronWall", "iron")]
        [TestCase("SealedLibraryDoor", "door")]
        [TestCase("SealedArchiveShelf", "shelf")]
        [TestCase("CaveBear", "bear")]
        [TestCase("CaveSlime", "slime")]
        [TestCase("ConvalescencePool", "spring")]
        [TestCase("IronshodBoots", "boots")]
        [TestCase("Bear", null)]
        [TestCase("Slime", null)]
        [TestCase("Water", null)]
        [TestCase("LeatherBoots", null)]
        [TestCase("OpenSealedLibraryDoor", null)]
        [TestCase("TepuiWall", "wall")]
        [TestCase("TepuiStone", null)]
        [TestCase("SandstoneWall", null)]
        [TestCase("LockedChest", null)]
        [TestCase("Player", null)]
        [TestCase(null, null)]
        public void FamilyClaimsOnlyExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, StillleafVoxelLibrary.Family(blueprint)); }

        [Test]
        public void InvalidFamilyAndVariantFailClearlyWithoutBorrowingOtherArt()
        {
            Assert.AreEqual("stillleaf-ground-3", StillleafVoxelLibrary.ModelId("ground", 3));
            Assert.Throws<ArgumentException>(() => StillleafVoxelLibrary.ModelId("not-a-family", 0));
            Assert.Throws<ArgumentException>(() => StillleafVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => StillleafVoxelLibrary.ModelId("ground", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StillleafVoxelLibrary.ModelId("ground", 4));
            var kit = StillleafVoxelLibrary.Load();
            Assert.IsNull(kit.Find(null));
            Assert.IsNull(kit.Find("stump-ground-0"));
            Assert.IsNull(kit.Find("stillleaf-no-such-model-0"));
        }

        [TestCase("ground")]
        [TestCase("floor")]
        public void FloorsHaveOneQuietContinuousTopAcrossAllVariants(string family)
        {
            var kit = StillleafVoxelLibrary.Load();
            Vector2? swatch = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var mesh = kit.Find(StillleafVoxelLibrary.ModelId(family, variant)).Mesh;
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
        public void ThreeArchiveMaterialsHaveBroadFoundationsAndDifferentQuietPalettes()
        {
            var kit = StillleafVoxelLibrary.Load();
            var palettes = new System.Collections.Generic.HashSet<string>();
            foreach (string family in new[] { "tepuibone", "marble", "iron" })
            {
                string previous = null;
                for (int variant = 0; variant < 4; variant++)
                {
                    var mesh = kit.Find(StillleafVoxelLibrary.ModelId(family, variant)).Mesh;
                    Assert.That(mesh.bounds.max.y, Is.InRange(1.7f, 2.1f));
                    Assert.LessOrEqual(mesh.vertexCount, 192);
                    var bottom = mesh.vertices.Where(v => v.y <= .2f).ToArray();
                    Assert.AreEqual(-.5f, bottom.Min(v => v.x), .0001f);
                    Assert.AreEqual(.5f, bottom.Max(v => v.x), .0001f);
                    Assert.AreEqual(-.5f, bottom.Min(v => v.z), .0001f);
                    Assert.AreEqual(.5f, bottom.Max(v => v.z), .0001f);
                    var colors = mesh.uv.Distinct().OrderBy(v => v.x).ThenBy(v => v.y).ToArray();
                    Assert.AreEqual(2, colors.Length);
                    string palette = string.Join(";", colors);
                    if (previous != null) Assert.AreEqual(previous, palette);
                    previous = palette;
                }
                Assert.IsTrue(palettes.Add(previous), "Each archive material needs a distinct pair: " + family);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void UnlockedDoorBecomesALowThresholdWithNoTallVisualBarrier(int variant)
        {
            var kit = StillleafVoxelLibrary.Load();
            var closed = kit.Find(StillleafVoxelLibrary.ModelId("door", variant)).Mesh;
            var open = kit.Find(StillleafVoxelLibrary.ModelId("open-door", variant)).Mesh;
            Assert.Less(open.bounds.max.y, .3f);
            Assert.AreEqual(closed.bounds.size.x, open.bounds.size.x, .0001f);
            Assert.Greater(closed.bounds.max.y, 1.6f);
            CollectionAssert.AreEquivalent(closed.uv.Distinct(), open.uv.Distinct());
            foreach (float height in new[] { .25f, .75f, 1.25f })
            {
                Assert.IsTrue(IntersectsForwardRay(closed, new Vector3(0, height, -1)), "The closed native barrier reads as a closed panel.");
                Assert.IsFalse(IntersectsForwardRay(open, new Vector3(0, height, -1)), "Unlocking must visibly clear the same owner's central passage.");
                Assert.IsTrue(IntersectsForwardRay(closed, new Vector3(.43f, height, -1)), "The closed side frame is a positive control for the ray test.");
            }
            Assert.IsTrue(IntersectsForwardRay(open, new Vector3(0, .05f, -1)), "A low retained threshold is the open model ray control.");
            Assert.AreEqual("door", StillleafVoxelLibrary.Family("SealedLibraryDoor"));
            Assert.IsNull(StillleafVoxelLibrary.Family("OpenSealedLibraryDoor"), "There is no replacement native open-door blueprint.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ArchiveShelvesHaveExposedSeparatedBundlesInsideAShallowStoneFrame(int variant)
        {
            var mesh = StillleafVoxelLibrary.Load().Find(StillleafVoxelLibrary.ModelId("shelf", variant)).Mesh;
            Assert.That(mesh.bounds.max.y, Is.InRange(.9f, 1.6f));
            Assert.Greater(mesh.bounds.size.x, .75f);
            Assert.Less(mesh.bounds.size.z, .65f);
            Assert.AreEqual(2, mesh.uv.Distinct().Count());
            var dark = new Vector2((15 % 16 + .5f) / 16f, (15 / 16 + .5f) / 8f);
            var bundles = Enumerable.Range(0, mesh.vertexCount).Where(i => mesh.uv[i] == dark)
                .Select(i => mesh.vertices[i]).ToArray();
            Assert.IsNotEmpty(bundles);
            Assert.IsTrue(bundles.Any(v => v.x < -.15f));
            Assert.IsTrue(bundles.Any(v => v.x > .15f));
            Assert.Less(bundles.Max(v => v.y), mesh.bounds.max.y);
            Assert.IsTrue(bundles.All(v => Mathf.Abs(v.x) < .4f));
        }

        // Pure mesh ray/triangle intersection: no collider can hide an incorrect open silhouette.
        private static bool IntersectsForwardRay(Mesh mesh, Vector3 origin)
        {
            var vertices = mesh.vertices; var triangles = mesh.triangles;
            for (int n = 0; n < triangles.Length; n += 3)
            {
                Vector3 a = vertices[triangles[n]], edge1 = vertices[triangles[n + 1]] - a,
                    edge2 = vertices[triangles[n + 2]] - a;
                Vector3 p = Vector3.Cross(Vector3.forward, edge2);
                float determinant = Vector3.Dot(edge1, p);
                if (Mathf.Abs(determinant) < .000001f) continue;
                float inverse = 1 / determinant;
                Vector3 t = origin - a;
                float u = Vector3.Dot(t, p) * inverse;
                if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(t, edge1);
                float v = Vector3.Dot(Vector3.forward, q) * inverse;
                if (v < 0 || u + v > 1) continue;
                if (Vector3.Dot(edge2, q) * inverse >= 0) return true;
            }
            return false;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void NativeCaveBearIsABroadQuadrupedWithASnoutAndPairedShortEars(int variant)
        {
            var kit = StillleafVoxelLibrary.Load();
            var mesh = kit.Find(StillleafVoxelLibrary.ModelId("bear", variant)).Mesh;
            Assert.AreEqual("stillleaf-shelf-3", kit.Entries[31].Id, "The initial archive identities remain in place.");
            Assert.AreEqual("stillleaf-bear-" + variant, kit.Entries[32 + variant].Id);
            Assert.That(mesh.bounds.max.y, Is.InRange(.75f, 1.15f));
            Assert.Greater(mesh.bounds.size.x, .65f);
            Assert.Greater(mesh.bounds.size.z, .80f);
            var low = mesh.vertices.Where(v => v.y < .10f).ToArray();
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    Assert.IsTrue(low.Any(v => x * v.x > .15f && z * v.z > .15f), "Four low paw positions must remain readable.");
            var ears = mesh.vertices.Where(v => v.y > .84f).ToArray();
            Assert.IsTrue(ears.Any(v => v.x < -.07f));
            Assert.IsTrue(ears.Any(v => v.x > .07f));
            Assert.IsTrue(ears.All(v => Mathf.Abs(v.x) > .07f), "A single pointed central horn is not a bear's ears.");
            var snout = mesh.vertices.Where(v => v.z > .42f).ToArray();
            Assert.IsNotEmpty(snout);
            Assert.Less(snout.Max(v => v.x) - snout.Min(v => v.x), .4f);
            Assert.Less(snout.Max(v => v.y), .8f);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SlimeAndPortableBootsHaveDifferentLowSilhouettes(int variant)
        {
            var kit = StillleafVoxelLibrary.Load();
            var slime = kit.Find(StillleafVoxelLibrary.ModelId("slime", variant)).Mesh;
            var boots = kit.Find(StillleafVoxelLibrary.ModelId("boots", variant)).Mesh;
            Assert.AreEqual("stillleaf-slime-" + variant, kit.Entries[36 + variant].Id);
            Assert.AreEqual("stillleaf-boots-" + variant, kit.Entries[44 + variant].Id);
            Assert.That(slime.bounds.max.y, Is.InRange(.3f, .65f));
            Assert.Greater(slime.bounds.size.x, .70f);
            Assert.LessOrEqual(slime.vertexCount, 120);
            Assert.That(boots.bounds.max.y, Is.InRange(.35f, .65f));
            Assert.Less(boots.bounds.size.x, .65f);
            Assert.LessOrEqual(boots.vertexCount, 168);
            Assert.IsTrue(boots.vertices.Any(v => v.x < -.10f));
            Assert.IsTrue(boots.vertices.Any(v => v.x > .10f));
            Assert.IsTrue(boots.vertices.All(v => Mathf.Abs(v.x) >= .049f), "Paired boots need an actual gap, not a single crate-shaped block.");
            Assert.AreNotEqual(slime.uv[0], boots.uv[0]);
        }

        [Test]
        public void NativeHealingSpringIsACyanContinuousOwnerBoundSurface()
        {
            var kit = StillleafVoxelLibrary.Load();
            Vector2? swatch = null;
            float? top = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(StillleafVoxelLibrary.ModelId("spring", variant));
                var mesh = entry.Mesh;
                Assert.AreEqual("stillleaf-spring-" + variant, kit.Entries[40 + variant].Id);
                Assert.AreEqual("entity", entry.Spec.kind, "Native liquid ownership must not become a permanent ground coating.");
                Assert.AreEqual(24, mesh.vertexCount);
                Assert.AreEqual(1, mesh.uv.Distinct().Count());
                Assert.AreEqual(1, mesh.bounds.size.x, .0001f);
                Assert.AreEqual(1, mesh.bounds.size.z, .0001f);
                Assert.That(mesh.bounds.max.y, Is.InRange(.02f, .07f));
                if (swatch.HasValue) Assert.AreEqual(swatch.Value, mesh.uv[0]);
                if (top.HasValue) Assert.AreEqual(top.Value, mesh.bounds.max.y, .0001f);
                swatch = mesh.uv[0]; top = mesh.bounds.max.y;
            }
            Assert.AreNotEqual(swatch.Value, kit.Find(StillleafVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0]);
            Assert.AreNotEqual(swatch.Value, GinmereVoxelLibrary.Load().Find(GinmereVoxelLibrary.ModelId("water", 0)).Mesh.uv[0]);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void WindcutPinkWallsJoinAsTwoBroadQuietStrataInsteadOfTinyCaps(int variant)
        {
            var kit = StillleafVoxelLibrary.Load();
            var entry = kit.Find(StillleafVoxelLibrary.ModelId("wall", variant));
            Assert.AreEqual("stillleaf-wall-" + variant, kit.Entries[48 + variant].Id);
            Assert.AreEqual("stillleaf-boots-3", kit.Entries[47].Id, "Earlier models keep their identities.");
            var mesh = entry.Mesh;
            Assert.AreEqual("entity", entry.Spec.kind);
            Assert.LessOrEqual(mesh.vertexCount, 48, "A repeated wall cell should not carry its own little decorative cap.");
            Assert.AreEqual(2, mesh.uv.Distinct().Count());
            Assert.AreEqual(2.0f, mesh.bounds.max.y, .0001f);
            foreach (var level in mesh.vertices.GroupBy(v => Mathf.Round(v.y * 10000)))
            {
                Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f);
                Assert.AreEqual(.5f, level.Max(v => v.x), .0001f);
                Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f);
                Assert.AreEqual(.5f, level.Max(v => v.z), .0001f);
            }
            var tops = Enumerable.Range(0, mesh.vertexCount).Where(i => mesh.vertices[i].y > 1.999f)
                .Select(i => mesh.uv[i]).Distinct().ToArray();
            Assert.AreEqual(1, tops.Length);
            var ground = StumpVoxelLibrary.Load().Find(StumpVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0];
            Assert.AreNotEqual(ground, tops[0], "Raised blocking masses must remain visible against the native tepui floor.");
            var first = kit.Find(StillleafVoxelLibrary.ModelId("wall", 0)).Mesh;
            var firstTop = Enumerable.Range(0, first.vertexCount).Where(i => first.vertices[i].y > 1.999f).Select(i => first.uv[i]).Distinct().Single();
            Assert.AreEqual(firstTop, tops[0]);
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
            var source = StillleafVoxelLibrary.Load(); source.Validate();
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
