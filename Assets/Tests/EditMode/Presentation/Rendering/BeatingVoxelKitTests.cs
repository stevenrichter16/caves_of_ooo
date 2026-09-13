using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class BeatingVoxelKitTests
    {
        private static readonly string[] Families = { "sand", "pan", "road", "crust", "dune", "ruin", "vein", "brine", "bones", "sign", "rubble", "briar" };

        [Test]
        public void KitHasFourCoarseSingleCellVariantsForEveryBeatingFamily()
        {
            var kit = BeatingVoxelLibrary.Load();
            Assert.NotNull(kit); kit.Validate();
            Assert.AreEqual(48, kit.Entries.Length);
            foreach (string family in Families)
            {
                var shapes = new string[4];
                for (int variant = 0; variant < 4; variant++)
                {
                    var e = kit.Find(BeatingVoxelLibrary.ModelId(family, variant));
                    Assert.NotNull(e, family);
                    Assert.That(e.Mesh.uv.Distinct().Count(), Is.InRange(1, 2), e.Id);
                    Assert.LessOrEqual(e.Mesh.vertexCount, 240, e.Id);
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
                    Assert.AreEqual(family == "sand" || family == "pan" || family == "road" ? "ground" : "entity", e.Spec.kind, e.Id);
                    shapes[variant] = string.Join(";", e.Mesh.vertices.Select(v => v.ToString("F4")));
                }
                Assert.AreEqual(4, shapes.Distinct().Count(), family);
            }
        }

        [Test]
        public void GroundAndBrineTilesJoinWithoutRaisedRimsOrColorCheckerboards()
        {
            var kit = BeatingVoxelLibrary.Load();
            foreach (string family in new[] { "sand", "pan", "road", "brine" })
            {
                Vector2? swatch = null;
                float? surface = null;
                for (int variant = 0; variant < 4; variant++)
                {
                    var e = kit.Find(BeatingVoxelLibrary.ModelId(family, variant));
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
            Assert.AreNotEqual(kit.Find(BeatingVoxelLibrary.ModelId("sand", 0)).Mesh.uv[0],
                kit.Find(BeatingVoxelLibrary.ModelId("brine", 0)).Mesh.uv[0]);
            Assert.Greater(kit.Find(BeatingVoxelLibrary.ModelId("brine", 0)).Mesh.bounds.min.y,
                kit.Find(BeatingVoxelLibrary.ModelId("sand", 0)).Mesh.bounds.max.y);
        }

        [TestCase("dune", .4f, 1.7f)]
        [TestCase("ruin", 1.5f, 2.3f)]
        [TestCase("vein", 1.1f, 1.7f)]
        [TestCase("sign", 1.6f, 2.0f)]
        [TestCase("crust", .04f, .29f)]
        [TestCase("bones", .12f, .50f)]
        [TestCase("rubble", .15f, .65f)]
        [TestCase("briar", .60f, 1.2f)]
        public void HeightHierarchyKeepsLandmarksReadableAndLooseDebrisLow(string family, float minimum, float maximum)
        {
            var kit = BeatingVoxelLibrary.Load();
            for (int variant = 0; variant < 4; variant++)
            {
                var e = kit.Find(BeatingVoxelLibrary.ModelId(family, variant));
                Assert.That(e.Mesh.bounds.size.y, Is.InRange(minimum, maximum), e.Id);
                if (family == "dune" || family == "ruin")
                    Assert.GreaterOrEqual(e.Mesh.bounds.size.x, .90f, e.Id);
            }
        }

        [TestCase(0, .4f, .65f)]
        [TestCase(1, .65f, 1.0f)]
        [TestCase(2, 1.1f, 1.4f)]
        [TestCase(3, 1.4f, 1.7f)]
        public void DuneVariantsProvideFlankShoulderCrestAndPeakWithinTheCoarseGeometryBudget(int variant, float minimum, float maximum)
        {
            var entry = BeatingVoxelLibrary.Load().Find(BeatingVoxelLibrary.ModelId("dune", variant));
            Assert.That(entry.Mesh.bounds.size.y, Is.InRange(minimum, maximum), entry.Id);
            Assert.LessOrEqual(entry.Mesh.vertexCount, 96, "Each height grade stays within the original coarse geometry budget.");
            Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count(), entry.Id);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryDuneHorizontalLevelSpansExactlyOneCellWithoutRepeatedLongitudinalGrooves(int variant)
        {
            var entry = BeatingVoxelLibrary.Load().Find(BeatingVoxelLibrary.ModelId("dune", variant));
            var levels = entry.Mesh.vertices.GroupBy(vertex => Mathf.Round(vertex.y * 10000f)).ToArray();
            Assert.GreaterOrEqual(levels.Length, 3, "The dune retains a base and upper silhouette, not a single undifferentiated box.");
            foreach (var level in levels)
            {
                float minimum = level.Min(vertex => vertex.x);
                float maximum = level.Max(vertex => vertex.x);
                Assert.AreEqual(-.5f, minimum, .0001f, entry.Id + " lower X at height " + level.Key / 10000f);
                Assert.AreEqual(.5f, maximum, .0001f, entry.Id + " upper X at height " + level.Key / 10000f);
                Assert.AreEqual(1f, maximum - minimum, .0001f, entry.Id + " full-width horizontal level");
            }
            Assert.AreEqual(2, entry.Mesh.uv.Distinct().Count(), entry.Id);
        }

        [Test]
        public void DunePaletteUsesMutedWarmStoneAndTheExistingSandRatherThanBrightGold()
        {
            var mutedStone = new Vector2((7 + .5f) / 16f, .5f / 8f);
            var sand = new Vector2(.5f / 16f, (2 + .5f) / 8f); // Palette 32, shared with the actual sand floor.
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = BeatingVoxelLibrary.Load().Find(BeatingVoxelLibrary.ModelId("dune", variant));
                CollectionAssert.AreEquivalent(new[] { mutedStone, sand }, entry.Mesh.uv.Distinct().ToArray(), entry.Id);
            }
        }

        [Test]
        public void RuinFoundationSpansTheNativeCellInBothDirectionsNearGround()
        {
            for (int variant = 0; variant < 4; variant++)
            {
                var entry = BeatingVoxelLibrary.Load().Find(BeatingVoxelLibrary.ModelId("ruin", variant));
                // Inspect the low foundation, not a cap or overhang that merely inflates total mesh bounds.
                var foundation = entry.Mesh.vertices.Where(v => v.y >= -.0001f && v.y < .3f).ToArray();
                Assert.IsNotEmpty(foundation, entry.Id);
                Assert.GreaterOrEqual(foundation.Max(v => v.x) - foundation.Min(v => v.x), .90f, entry.Id);
                Assert.GreaterOrEqual(foundation.Max(v => v.z) - foundation.Min(v => v.z), .90f, entry.Id);
            }
        }

        [Test]
        public void PanSandAndRoadAreDistinctQuietSurfacesAndVeinsContrastWithThePan()
        {
            var kit = BeatingVoxelLibrary.Load();
            Vector2 sand = kit.Find(BeatingVoxelLibrary.ModelId("sand", 0)).Mesh.uv[0];
            Vector2 pan = kit.Find(BeatingVoxelLibrary.ModelId("pan", 0)).Mesh.uv[0];
            Vector2 road = kit.Find(BeatingVoxelLibrary.ModelId("road", 0)).Mesh.uv[0];
            Assert.AreNotEqual(sand, pan); Assert.AreNotEqual(sand, road); Assert.AreNotEqual(pan, road);
            for (int variant = 0; variant < 4; variant++)
            {
                var e = kit.Find(BeatingVoxelLibrary.ModelId("vein", variant));
                Assert.AreEqual(2, e.Mesh.uv.Distinct().Count(), e.Id);
                Assert.IsFalse(e.Mesh.uv.Contains(pan), "Veins need pale crystals and darker host rock, distinct from the flat pan: " + e.Id);
            }
        }

        [TestCase("Sand", "sand")]
        [TestCase("RoadStone", "road")]
        [TestCase("SandstoneFloor", "road")]
        [TestCase("SaltCrust", "crust")]
        [TestCase("DuneCrest", "dune")]
        [TestCase("SandstoneWall", "ruin")]
        [TestCase("PaleSaltVein", "vein")]
        [TestCase("BrinePool", "brine")]
        [TestCase("Bones", "bones")]
        [TestCase("Signpost", "sign")]
        [TestCase("Rubble", "rubble")]
        [TestCase("Saltbriar", "briar")]
        [TestCase("SaltPan", null)]
        [TestCase("PreFellingBody", null)]
        [TestCase("Corpse", null)]
        [TestCase("Player", null)]
        [TestCase("DryBrush", null)]
        [TestCase("Rock", null)]
        [TestCase("Grass", null)]
        [TestCase(null, null)]
        public void FamilyMappingIsExactAndPanFloorRemainsAContextualRecipe(string blueprint, string family)
        { Assert.AreEqual(family, BeatingVoxelLibrary.Family(blueprint)); }

        [Test]
        public void ModelIdRejectsUnknownFamilyAndOutOfRangeVariant()
        {
            Assert.AreEqual("beating-dune-3", BeatingVoxelLibrary.ModelId("dune", 3));
            Assert.Throws<ArgumentException>(() => BeatingVoxelLibrary.ModelId("dun", 0));
            Assert.Throws<ArgumentException>(() => BeatingVoxelLibrary.ModelId(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BeatingVoxelLibrary.ModelId("dune", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BeatingVoxelLibrary.ModelId("dune", 4));
            Assert.IsNull(BeatingVoxelLibrary.Load().Find(null));
            Assert.IsNull(BeatingVoxelLibrary.Load().Find("spread-reeds-0"));
            Assert.IsNull(BeatingVoxelLibrary.Load().Find("beating-nothing-0"));
        }

        [Test]
        public void NativeCoarseKitMeshesAreBorrowedExactlyWithoutSecondVoxelizationOrWorldMutation()
        {
            bool old = Village3DSettings.Enabled;
            Village3DSettings.Enabled = true;
            try
            {
                var zone = new Zone(BeatingCompositionTests.Id);
                Assert.IsTrue(BeatingCompositionPlan.IsWildernessZone(zone.ZoneID));
                int version = zone.EntityVersion;
                var bridge = VoxelWorldPresentation.ForZone(zone);
                Assert.NotNull(bridge);
                foreach (var entry in BeatingVoxelLibrary.Load().Entries)
                {
                    Assert.AreSame(entry.Mesh, bridge.Resolve(entry.Mesh), entry.Id);
                    Assert.AreSame(entry.Mesh, bridge.Resolve(entry.Mesh), entry.Id + " repeated borrow");
                }
                Assert.AreEqual(0, bridge.MissingMeshCount, "Returning an unmapped mesh unchanged is not a successful borrowed-kit mapping.");
                Assert.AreEqual(0, bridge.AppliedMeshCount, "Coarse source assets must not be replaced by a second voxel conversion.");
                Assert.AreEqual(version, zone.EntityVersion);
                Assert.AreEqual(0, zone.EntityCount);
                Village3DSettings.Enabled = false;
                Assert.IsNull(VoxelWorldPresentation.ForZone(zone), "The same eligible zone obeys the disabled-presentation control.");
                Village3DSettings.Enabled = true;
                Assert.IsNull(VoxelWorldPresentation.ForZone(new Zone("Overworld.16.16.1")), "Surface biome art must not claim underground content.");
            }
            finally { Village3DSettings.Enabled = old; }
        }

        [TestCase("SandstoneWall", "ruin")]
        [TestCase("PaleSaltVein", "vein")]
        [TestCase("BrinePool", "brine")]
        [TestCase("Signpost", "sign")]
        public void RealPresenterRebuildRemovesOnlyTheDeletedNativeOwnersGeometry(string blueprint, string family)
        {
            Assert.IsTrue(BeatingCompositionPlan.IsWildernessZone(BeatingCompositionTests.Id));
            using (var f = new SpawnRing3DIntegrationFixture(BeatingCompositionTests.Id))
            {
                f.Set("FullReveal", true);
                var removed = f.Add(blueprint, 20, 10);
                var control = f.Add(blueprint, 60, 20);
                f.Refresh();
                Assert.IsTrue(f.Authored(removed)); Assert.IsTrue(f.Authored(control));
                Assert.IsTrue(f.Rendered(removed)); Assert.IsTrue(f.Rendered(control));
                Assert.IsTrue(f.Get<bool>("VoxelPresentationActive"));
                Assert.IsTrue(f.Find(removed, out var patch, out string model));
                Assert.IsTrue(model.StartsWith("beating-" + family + "-", StringComparison.Ordinal));
                var source = BeatingVoxelLibrary.Load().Find(model);
                Assert.NotNull(source);
                int originalSourceVertices = source.Mesh.vertexCount;
                int patchVertices = PatchVertexCount(patch);
                int revision = f.Revision(20, 10), distant = f.Revision(60, 20);
                Assert.IsTrue(f.Find(control, out var controlPatch, out string controlModel));
                var distantMeshes = controlPatch.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray();

                Assert.IsTrue(f.Zone.RemoveEntity(removed));
                Assert.IsFalse(f.Find(removed, out _, out _), "A removed native reference cannot expose stale geometry before refresh.");
                // An explicitly empty dirty input must not suppress membership reconciliation.
                f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Authored(removed)); Assert.IsFalse(f.Rendered(removed));
                Assert.IsFalse(f.Find(removed, out _, out _));
                Assert.IsTrue(f.Authored(control)); Assert.IsTrue(f.Rendered(control));
                Assert.IsTrue(f.Find(control, out var survivingPatch, out string survivingModel));
                Assert.AreSame(controlPatch, survivingPatch);
                Assert.AreEqual(controlModel, survivingModel);
                CollectionAssert.AreEqual(distantMeshes, survivingPatch.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray());
                Assert.Greater(f.Revision(20, 10), revision);
                Assert.AreEqual(distant, f.Revision(60, 20));
                Assert.AreEqual(originalSourceVertices, patchVertices - PatchVertexCount(patch),
                    "The committed local patch must lose exactly the deleted owner's mesh, not merely hide its entity lookup.");
                Assert.NotNull(source.Mesh, "Patch disposal must preserve borrowed kit assets.");
                Assert.AreEqual(originalSourceVertices, source.Mesh.vertexCount);
                Assert.IsTrue(f.Get<bool>("IsReady"), f.Get<string>("Failure"));
                Assert.AreEqual(0, f.Get<int>("VoxelMissingMeshCount"));
            }
        }

        [Test]
        public void CurrentDuneHeightReconcilesAcrossARealPatchBoundaryWithAnEmptyDirtySet()
        {
            using (var f = new SpawnRing3DIntegrationFixture(BeatingCompositionTests.Id))
            {
                f.Set("FullReveal", true);
                // Current patch dimensions are 10 by 5: x19/20 crosses a boundary; x15/16 does not.
                var cells = new[] { (19, 12), (18, 12), (20, 12), (19, 11), (19, 13) };
                foreach (var cell in cells)
                    foreach (var existing in f.Zone.GetCell(cell.Item1, cell.Item2).Objects.ToArray())
                        if (existing.BlueprintName == "DuneCrest") Assert.IsTrue(f.Zone.RemoveEntity(existing));
                var center = f.Add("DuneCrest", 19, 12);
                f.Add("DuneCrest", 18, 12);
                var right = f.Add("DuneCrest", 20, 12);
                f.Add("DuneCrest", 19, 11);
                f.Add("DuneCrest", 19, 13);
                var control = f.Add("DuneCrest", 60, 22);
                f.Refresh();
                Assert.IsTrue(f.Find(center, out var centerPatch, out string firstModel));
                Assert.AreEqual(BeatingVoxelLibrary.ModelId("dune", 3), firstModel);
                Assert.IsTrue(f.Find(right, out var rightPatch, out _));
                Assert.AreNotSame(centerPatch, rightPatch, "The neighbor must actually occupy a different patch.");
                Assert.IsTrue(f.Find(control, out var controlPatch, out string controlModel));
                var controlMeshes = controlPatch.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray();
                int revision = f.Revision(19, 12), distantRevision = f.Revision(60, 22);

                Assert.IsTrue(f.Zone.RemoveEntity(right));
                f.Call("Refresh", null, new HashSet<int>());
                f.Frame();
                Assert.IsTrue(f.Find(center, out var rebuiltPatch, out string rebuiltModel));
                Assert.AreSame(centerPatch, rebuiltPatch);
                Assert.AreEqual(BeatingVoxelLibrary.ModelId("dune", 2), rebuiltModel);
                Assert.Greater(f.Revision(19, 12), revision, "Native neighboring removal changes a surviving owner's mesh even with no explicit dirty cells.");
                Assert.IsFalse(f.Find(right, out _, out _));
                Assert.IsTrue(f.Find(control, out var survivingControl, out string survivingModel));
                Assert.AreSame(controlPatch, survivingControl); Assert.AreEqual(controlModel, survivingModel);
                Assert.AreEqual(distantRevision, f.Revision(60, 22));
                CollectionAssert.AreEqual(controlMeshes, survivingControl.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray());
                Assert.IsTrue(f.Get<bool>("IsReady"), f.Get<string>("Failure"));
            }
        }

        private static int PatchVertexCount(GameObject patch)
        {
            Assert.NotNull(patch);
            return patch.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh != null)
                .Sum(filter => filter.sharedMesh.vertexCount);
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
            var source = BeatingVoxelLibrary.Load(); source.Validate();
            var copy = UnityEngine.Object.Instantiate(source);
            GameObject badPrefab = null;
            try
            {
                var e = copy.Entries[0];
                switch (corruption)
                {
                    case "duplicate": copy.Entries[1] = e; break;
                    case "missing": copy.Entries = copy.Entries.Take(47).ToArray(); break;
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
