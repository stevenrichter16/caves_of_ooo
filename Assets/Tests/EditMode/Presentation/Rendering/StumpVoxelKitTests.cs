using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class StumpVoxelKitTests
    {
        private static readonly string[] Families = { "ground", "wall", "grain", "dome", "ledge", "spray", "tank", "vein", "bone", "tree", "bush", "singer", "sentinel", "key" };

        [Test]
        public void KitHasFourCoarseSingleCellVariantsForEveryStumpFamily()
        {
            var kit = StumpVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(56, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var e = kit.Find(StumpVoxelLibrary.ModelId(family, variant));
                    Assert.NotNull(e, family);
                    Assert.That(e.Mesh.uv.Distinct().Count(), Is.InRange(1, 2), e.Id);
                    Assert.LessOrEqual(e.Mesh.vertexCount, family == "singer" ? 288 : 240, e.Id);
                    Assert.Greater(e.Mesh.vertexCount, 0, e.Id);
                    Assert.GreaterOrEqual(e.Mesh.bounds.min.x, -.5001f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.max.x, .5001f, e.Id);
                    Assert.GreaterOrEqual(e.Mesh.bounds.min.z, -.5001f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.max.z, .5001f, e.Id);
                    Assert.AreEqual(1, e.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length, e.Id);
                    Assert.AreEqual(0, e.Prefab.GetComponentsInChildren<Collider>(true).Length, e.Id);
                    Assert.AreEqual(0, e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length, e.Id);
                    Assert.AreSame(e.Mesh, e.Prefab.GetComponent<MeshFilter>().sharedMesh);
                    Assert.AreEqual(e.Mesh.bounds.center, e.Spec.boundsCenter, e.Id);
                    Assert.AreEqual(e.Mesh.bounds.size, e.Spec.boundsSize, e.Id);
                    Assert.AreEqual(e.Mesh.triangles.Length / 3, e.Spec.triangles, e.Id);
                    Assert.AreEqual(family == "ground" ? "ground" : "entity", e.Spec.kind, e.Id);
                    shapes[variant] = string.Join(";", e.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [Test]
        public void GroundAndSprayTilesJoinWithoutRaisedRimsOrColorCheckerboards()
        {
            var kit = StumpVoxelLibrary.Load();
            foreach (string family in new[] { "ground", "spray" })
            {
                Vector2? swatch = null;
                float? surface = null;
                for (int variant = 0; variant < 4; variant++)
                {
                    var e = kit.Find(StumpVoxelLibrary.ModelId(family, variant));
                    Assert.AreEqual(24, e.Mesh.vertexCount, e.Id);
                    Assert.AreEqual(1, e.Mesh.uv.Distinct().Count(), e.Id);
                    Assert.AreEqual(1f, e.Mesh.bounds.size.x, .0001f, e.Id);
                    Assert.AreEqual(1f, e.Mesh.bounds.size.z, .0001f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.size.y, .12f, e.Id);
                    Assert.LessOrEqual(e.Mesh.bounds.max.y, .06f, e.Id);
                    if (swatch.HasValue) Assert.AreEqual(swatch.Value, e.Mesh.uv[0]);
                    if (surface.HasValue) Assert.AreEqual(surface.Value, e.Mesh.bounds.max.y, .0001f);
                    swatch = e.Mesh.uv[0]; surface = e.Mesh.bounds.max.y;
                }
            }
            Assert.AreNotEqual(kit.Find(StumpVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0],
                kit.Find(StumpVoxelLibrary.ModelId("spray", 0)).Mesh.uv[0]);
            Assert.Greater(kit.Find(StumpVoxelLibrary.ModelId("spray", 0)).Mesh.bounds.min.y,
                kit.Find(StumpVoxelLibrary.ModelId("ground", 0)).Mesh.bounds.max.y);
        }

        [TestCase("wall", 1.5f, 2.4f)]
        [TestCase("grain", .60f, 1.5f)]
        [TestCase("dome", .65f, 1.4f)]
        [TestCase("ledge", .06f, .26f)]
        [TestCase("tank", .45f, .95f)]
        [TestCase("vein", .70f, 1.4f)]
        [TestCase("bone", .08f, .45f)]
        [TestCase("tree", 1.30f, 2.0f)]
        [TestCase("bush", .30f, .85f)]
        [TestCase("singer", .30f, .80f)]
        [TestCase("sentinel", .15f, .55f)]
        [TestCase("key", .05f, .20f)]
        public void HeightHierarchyKeepsStoneMassesDistinctFromWalkableLedgesAndSmallFauna(string family, float minimum, float maximum)
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = StumpVoxelLibrary.Load().Find(StumpVoxelLibrary.ModelId(family, variant));
                Assert.That(entry.Mesh.bounds.size.y, Is.InRange(minimum, maximum), entry.Id);
            }
        }

        [Test]
        public void EveryGrainLevelSpansEastWestWithoutPerCellGrooves()
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = StumpVoxelLibrary.Load().Find(StumpVoxelLibrary.ModelId("grain", variant));
                Assert.LessOrEqual(entry.Mesh.vertexCount, 96, entry.Id);
                var levels = entry.Mesh.vertices.GroupBy(v => Mathf.Round(v.y * 10000f)).ToArray();
                Assert.GreaterOrEqual(levels.Length, 3, entry.Id);
                foreach (var level in levels)
                {
                    Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f, entry.Id);
                    Assert.AreEqual(.5f, level.Max(v => v.x), .0001f, entry.Id);
                }
                Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count(), entry.Id);
            }
        }

        [Test]
        public void ComposableDomeCellsStayBroadWhileWalkableLedgesRemainLow()
        {
            var kit = StumpVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var dome = kit.Find(StumpVoxelLibrary.ModelId("dome", variant));
                Assert.GreaterOrEqual(dome.Mesh.bounds.size.x, .9f);
                Assert.GreaterOrEqual(dome.Mesh.bounds.size.z, .9f);
                var top = dome.Mesh.vertices.Where(v => Mathf.Abs(v.y - dome.Mesh.bounds.max.y) < .0001f).ToArray();
                Assert.GreaterOrEqual(top.Max(v => v.x) - top.Min(v => v.x), .90f);
                Assert.GreaterOrEqual(top.Max(v => v.z) - top.Min(v => v.z), .90f);
                var ledge = kit.Find(StumpVoxelLibrary.ModelId("ledge", variant));
                Assert.GreaterOrEqual(ledge.Mesh.bounds.size.x, .9f);
                Assert.GreaterOrEqual(ledge.Mesh.bounds.size.z, .8f);
                Assert.LessOrEqual(ledge.Mesh.bounds.max.y, .26f);
            }
        }

        [TestCase("grain", 0)]
        [TestCase("grain", 1)]
        [TestCase("grain", 2)]
        [TestCase("grain", 3)]
        [TestCase("dome", 0)]
        [TestCase("dome", 1)]
        [TestCase("dome", 2)]
        [TestCase("dome", 3)]
        public void MassBuildingSlabsJoinAcrossBothAxesWithoutPerCellWafflesOrRails(string family, int variant)
        {
            var entry = StumpVoxelLibrary.Load().Find(StumpVoxelLibrary.ModelId(family, variant));
            Assert.LessOrEqual(entry.Mesh.vertexCount, 48, "Two coarse slabs are sufficient for a native height grade: " + entry.Id);
            Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count(), entry.Id);
            var levels = entry.Mesh.vertices.GroupBy(v => Mathf.Round(v.y * 10000f)).ToArray();
            Assert.GreaterOrEqual(levels.Length, 3, entry.Id);
            foreach (var level in levels)
            {
                Assert.AreEqual(-.5f, level.Min(v => v.x), .0001f, entry.Id + " west edge");
                Assert.AreEqual(.5f, level.Max(v => v.x), .0001f, entry.Id + " east edge");
                Assert.AreEqual(-.5f, level.Min(v => v.z), .0001f, entry.Id + " south edge");
                Assert.AreEqual(.5f, level.Max(v => v.z), .0001f, entry.Id + " north edge");
            }
        }

        [TestCase("grain")]
        [TestCase("dome")]
        public void RaisedBlockedMassesUseAQuietTopSwatchDistinctFromWalkableGround(string family)
        {
            var kit = StumpVoxelLibrary.Load();
            Vector2 ground = kit.Find(StumpVoxelLibrary.ModelId("ground", 0)).Mesh.uv[0];
            Vector2? sharedTop = null;
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(StumpVoxelLibrary.ModelId(family, variant));
                var vertices = entry.Mesh.vertices; var uv = entry.Mesh.uv;
                var top = Enumerable.Range(0, vertices.Length)
                    .Where(i => Mathf.Abs(vertices[i].y - entry.Mesh.bounds.max.y) < .0001f)
                    .Select(i => uv[i]).Distinct().ToArray();
                Assert.AreEqual(1, top.Length, "A raised top remains one quiet color: " + entry.Id);
                Assert.AreNotEqual(ground, top[0], "The blocked mass must remain distinguishable from walkable ground even without a shadow: " + entry.Id);
                Assert.AreEqual(2, uv.Distinct().Count(), entry.Id);
                if (sharedTop.HasValue) Assert.AreEqual(sharedTop.Value, top[0], "Grade changes must not introduce a shade checkerboard.");
                sharedTop = top[0];
            }
        }

        [Test]
        public void TankBromeliadsHaveAnOpenLowCentralCupBelowTheLeafRim()
        {
            var kit = StumpVoxelLibrary.Load();
            var cupSwatch = new Vector2((28 % 16 + .5f) / 16f, (28 / 16 + .5f) / 8f);
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = kit.Find(StumpVoxelLibrary.ModelId("tank", variant));
                var vertices = entry.Mesh.vertices; var uv = entry.Mesh.uv;
                var cup = Enumerable.Range(0, vertices.Length).Where(i => uv[i] == cupSwatch).Select(i => vertices[i]).ToArray();
                Assert.IsNotEmpty(cup, entry.Id);
                Assert.Less(cup.Max(v => v.y), entry.Mesh.bounds.max.y - .15f, entry.Id);
                Assert.IsTrue(cup.All(v => Mathf.Abs(v.x) <= .25f && Mathf.Abs(v.z) <= .25f), entry.Id);
                var upper = vertices.Where(v => v.y > cup.Max(c => c.y) + .1f).ToArray();
                Assert.IsTrue(upper.All(v => Mathf.Abs(v.x) >= .20f || Mathf.Abs(v.z) >= .20f), "The opening is not capped with a solid cube: " + entry.Id);
            }
        }

        [TestCase("TepuiStone", "ground")]
        [TestCase("TepuiWall", "wall")]
        [TestCase("GrainRidge", "grain")]
        [TestCase("StoneDome", "dome")]
        [TestCase("DescentLedge", "ledge")]
        [TestCase("SprayPool", "spray")]
        [TestCase("TankBrocchinia", "tank")]
        [TestCase("TepuiboneVein", "vein")]
        [TestCase("Tepuibone", "bone")]
        [TestCase("Tree", "tree")]
        [TestCase("Bush", "bush")]
        [TestCase("SummitSinger", "singer")]
        [TestCase("BrocchiniaSentinel", "sentinel")]
        [TestCase("IronKey", "key")]
        [TestCase("Key", null)]
        [TestCase("PreFellingBody", null)]
        [TestCase("CaveSinger", null)]
        [TestCase("MawToad", null)]
        [TestCase("Player", null)]
        [TestCase("Grass", null)]
        [TestCase("Floor", null)]
        [TestCase(null, null)]
        public void FamilyMappingClaimsOnlyTheExactNativeBlueprints(string blueprint, string family)
        { Assert.AreEqual(family, StumpVoxelLibrary.Family(blueprint)); }

        [Test]
        public void ModelIdRejectsUnknownFamilyAndOutOfRangeVariant()
        {
            Assert.AreEqual("stump-grain-3", StumpVoxelLibrary.ModelId("grain", 3));
            Assert.Throws<ArgumentException>(() => StumpVoxelLibrary.ModelId("grane", 0));
            Assert.Throws<ArgumentException>(() => StumpVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => StumpVoxelLibrary.ModelId("grain", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StumpVoxelLibrary.ModelId("grain", 4));
            Assert.IsNull(StumpVoxelLibrary.Load().Find(null));
            Assert.IsNull(StumpVoxelLibrary.Load().Find("spread-reeds-0"));
            Assert.IsNull(StumpVoxelLibrary.Load().Find("stump-nothing-0"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void IronKeysHaveAnOpenRingShaftAndSeparatedTeethInOneCell(int variant)
        {
            var kit = StumpVoxelLibrary.Load();
            var entry = kit.Find(StumpVoxelLibrary.ModelId("key", variant));
            Assert.NotNull(entry);
            Assert.AreEqual(entry.Id, kit.Entries[52 + variant].Id, "Key variants append after the existing 52 models.");
            Assert.AreEqual("stump-sentinel-3", kit.Entries[51].Id, "The prior last model retains its position.");
            Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count(), entry.Id);
            Assert.LessOrEqual(entry.Mesh.vertexCount, 192, entry.Id);
            Assert.Greater(entry.Mesh.bounds.size.x, .80f, entry.Id);
            Assert.Less(entry.Mesh.bounds.size.y, .20f, entry.Id);
            Assert.IsTrue(HasHorizontalSurfaceAt(entry.Mesh, -.27f, .15f), "The key has a ring rim.");
            Assert.IsFalse(HasHorizontalSurfaceAt(entry.Mesh, -.27f, 0), "The ring hole is geometry, not a painted square.");
            Assert.IsTrue(HasHorizontalSurfaceAt(entry.Mesh, .05f, 0), "A long shaft joins the head to the bit.");
            Assert.IsTrue(HasHorizontalSurfaceAt(entry.Mesh, .22f, .13f), "First tooth projects from the shaft.");
            Assert.IsTrue(HasHorizontalSurfaceAt(entry.Mesh, .40f, .13f), "Second tooth projects from the shaft.");
            Assert.IsFalse(HasHorizontalSurfaceAt(entry.Mesh, .31f, .13f), "The teeth remain separate rather than a solid paddle.");
        }

        [Test]
        public void IronKeyArtAliasBelongsToTheShippedPortableKeyIdentity()
        {
            var key = GrovelandsCompositionTests.Factory().CreateEntity("IronKey");
            Assert.NotNull(key);
            Assert.AreEqual("key", StumpVoxelLibrary.Family(key.BlueprintName));
            Assert.IsTrue(key.GetPart<PhysicsPart>().Takeable);
            Assert.AreEqual("iron", key.GetPart<KeyPart>().KeyId);
            Assert.IsNull(StumpVoxelLibrary.Family("Key"), "The art library does not infer arbitrary keys from a component name.");
        }

        private static bool HasHorizontalSurfaceAt(Mesh mesh, float x, float z)
        {
            var vertices = mesh.vertices; var indices = mesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = vertices[indices[i]]; var b = vertices[indices[i + 1]]; var c = vertices[indices[i + 2]];
                if (Mathf.Abs(a.y - b.y) > .0001f || Mathf.Abs(a.y - c.y) > .0001f) continue;
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < .000001f) continue;
                float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / denominator;
                float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / denominator;
                if (u >= -.0001f && v >= -.0001f && u + v <= 1.0001f) return true;
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
        public void ValidationRejectsBrokenReferencesAndMetadataWithoutMutatingSource(string corruption)
        {
            var source = StumpVoxelLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var e = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = e; break;
                    case "missing": copy.Entries = copy.Entries.Take(55).ToArray(); break;
                    case "null-entry": copy.Entries[0] = null; break;
                    case "null-id": e.Id = null; break;
                    case "metadata-kind": e.Spec.kind = "actor"; break;
                    case "metadata-bounds": e.Spec.boundsSize += Vector3.one; break;
                    case "metadata-triangles": e.Spec.triangles++; break;
                    case "metadata-path": e.Spec.path = "wrong/path.prefab"; break;
                    case "metadata-rig": e.Spec.rigged = true; break;
                    case "metadata-material": e.Spec.materialFamily = "wrong-palette"; break;
                    case "wrong-mesh":
                        badPrefab = UnityEngine.Object.Instantiate(e.Prefab);
                        badPrefab.GetComponent<MeshFilter>().sharedMesh = copy.Entries[4].Mesh;
                        e.Prefab = badPrefab;
                        break;
                    case "wrong-material":
                        badPrefab = UnityEngine.Object.Instantiate(e.Prefab);
                        badPrefab.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WaterMaterial;
                        e.Prefab = badPrefab;
                        break;
                }
                Assert.Throws<InvalidOperationException>(() => copy.Validate(), corruption);
                Assert.DoesNotThrow(() => source.Validate(), "Mutation fixture must not change shipped source assets.");
            }
            finally
            {
                if (badPrefab != null) UnityEngine.Object.DestroyImmediate(badPrefab);
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
    }
}
