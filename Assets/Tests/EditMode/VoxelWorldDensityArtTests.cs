#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual imported art: coarser cells must reduce geometric density,
    /// without deleting animated parts or changing an object's occupied envelope.
    /// Fresh old-pitch bakes are disposable controls, never published assets.</summary>
    [Category("VoxelWorldRealArt")]
    public sealed class VoxelWorldDensityArtTests
    {
        const string VillageModels = "Assets/Art3D/Village/Models/";
        const string ToolkitRoot = "Assets/Art3D/VoxelWorld/Toolkit/";
        readonly Dictionary<Mesh, Mesh> baselines = new Dictionary<Mesh, Mesh>();
        Dictionary<Mesh, float> scales;
        VoxelWorldMeshCatalog catalog;

        [OneTimeSetUp]
        public void BorrowNativeCatalogAndCalculateEffectiveSourceScales()
        {
            catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            Assert.NotNull(catalog); catalog.Validate();
            var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var pilot = Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
            Assert.NotNull(village); Assert.NotNull(ring); Assert.NotNull(pilot);
            village.Validate(); ring.Validate(); pilot.Validate();
            var prefabs = village.Models.Select(m => m.Prefab).Concat(ring.Models.Select(m => m.Prefab))
                .Concat(pilot.Models.Select(m => m.Prefab))
                .Concat(ring.EquipmentLibrary.Models.Select(m => m.Prefab)).Distinct();
            scales = new Dictionary<Mesh, float>();
            foreach (var renderer in prefabs.SelectMany(p => p.GetComponentsInChildren<Renderer>(true)))
            {
                var source = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (source == null) continue;
                var matrix = renderer.transform.localToWorldMatrix;
                float scale = Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,
                    Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude, ((Vector3)matrix.GetColumn(2)).magnitude));
                Assert.That(scale, Is.GreaterThan(0));
                scales[source] = scales.TryGetValue(source, out var previous) ? Mathf.Max(previous, scale) : scale;
            }
        }

        [OneTimeTearDown]
        public void DisposeOnlyFreshControlMeshes()
        {
            foreach (var mesh in baselines.Values) if (mesh != null) Object.DestroyImmediate(mesh);
            baselines.Clear();
        }

        float OldWorldPitch(Mesh source) => source.bounds.size.magnitude * scales[source] >= 3f ? .2f : .125f;
        float OldLocalPitch(Mesh source) => OldWorldPitch(source) / scales[source];
        Mesh Baseline(Mesh source)
        {
            if (!baselines.TryGetValue(source, out var result))
            {
                result = VoxelWorldMeshBaker.Bake(source, OldLocalPitch(source));
                baselines.Add(source, result);
            }
            return result;
        }
        static bool IsToolkit(VoxelWorldMeshCatalog.Binding row)
            => AssetDatabase.GetAssetPath(row.Voxel).StartsWith(ToolkitRoot, StringComparison.Ordinal);

        // Every output voxel is closed. Opposing caps at rigid bone boundaries
        // cancel in signed volume, so this counts occupied cells despite greedy
        // face merging, UV differences and additional joint faces.
        static long OccupiedCells(Mesh mesh, float pitch)
        {
            var vertices = mesh.vertices; var indices = mesh.triangles;
            var origin = mesh.bounds.center; double volume6 = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = vertices[indices[i]] - origin;
                var b = vertices[indices[i + 1]] - origin;
                var c = vertices[indices[i + 2]] - origin;
                volume6 += (double)a.x * ((double)b.y * c.z - (double)b.z * c.y)
                    + (double)a.y * ((double)b.z * c.x - (double)b.x * c.z)
                    + (double)a.z * ((double)b.x * c.y - (double)b.y * c.x);
            }
            double cells = volume6 / (6d * pitch * pitch * pitch);
            Assert.That(cells, Is.GreaterThan(0), mesh.name + " must have outward closed voxel volume.");
            double rounded = Math.Round(cells);
            Assert.That(Math.Abs(cells - rounded), Is.LessThan(Math.Max(.005d, rounded * .00001d)),
                mesh.name + " volume must represent an integral number of cells.");
            return checked((long)rounded);
        }

        [TestCase("herb-pot")]
        [TestCase("character-teal")]
        [TestCase("ground-patch-0")]
        public void OrdinaryPropCharacterAndTerrainReduceActualGeometricComplexity(string model)
        {
            var row = catalog.Bindings.Single(b => AssetDatabase.GetAssetPath(b.Source) == VillageModels + model + ".fbx");
            Assert.IsFalse(IsToolkit(row), "The comparison must use the same native design on both sides.");
            if (MorrowfastCoarseArtContract.IsVerified(row))
            {
                // Deliberately authored broad boxes are no longer a uniform
                // surface-voxel lattice. Count their actual geometry, not a
                // fictitious number of quarter-metre occupied cubes.
                var original = Baseline(row.Source);
                Assert.Less(row.Voxel.vertexCount, original.vertexCount, model + " must genuinely simplify the former source detail.");
                Assert.Less(row.Voxel.triangles.Length, original.triangles.Length);
                Assert.AreEqual(0, row.Source.bindposeCount);
                return;
            }
            long before = OccupiedCells(Baseline(row.Source), OldLocalPitch(row.Source));
            long after = OccupiedCells(row.Voxel, row.VoxelSize);
            TestContext.WriteLine(model + ": " + before + " -> " + after + " occupied cells; world pitch "
                + OldWorldPitch(row.Source) + " -> " + row.WorldVoxelSize);
            Assert.That(row.WorldVoxelSize, Is.GreaterThan(OldWorldPitch(row.Source)), model + " must actually use larger cells.");
            Assert.That(after, Is.LessThan(before), model + " must contain fewer cells, not only claim a larger pitch.");
        }

        static HashSet<int> UsedBones(Mesh mesh)
        {
            var used = new HashSet<int>();
            foreach (var w in mesh.boneWeights)
            {
                if (w.weight0 > 0) used.Add(w.boneIndex0);
                if (w.weight1 > 0) used.Add(w.boneIndex1);
                if (w.weight2 > 0) used.Add(w.boneIndex2);
                if (w.weight3 > 0) used.Add(w.boneIndex3);
            }
            return used;
        }

        [Test]
        public void AllTwentyEightSkinsRetainEveryBoneThatHadVisibleBaselineGeometry()
        {
            var skins = catalog.Bindings.Where(b => b.Source.bindposeCount > 0).ToArray();
            Assert.AreEqual(28, skins.Length, "Pin the complete current animated library, not a selected humanoid.");
            int articulated = 0;
            foreach (var row in skins)
            {
                var before = UsedBones(Baseline(row.Source)); var after = UsedBones(row.Voxel);
                Assert.Greater(before.Count, 0, row.Source.name);
                if (before.Count > 1) articulated++;
                CollectionAssert.IsSubsetOf(before, after, row.Source.name + " lost a previously visible limb/body part.");
            }
            Assert.Greater(articulated, 0, "The coverage gate must actually exercise articulated bodies.");
        }

        [Test]
        public void EveryPublishedSkinExactlyMatchesAFreshBakeAtItsRecordedPitch()
        {
            var skins = catalog.Bindings.Where(b => b.Source.bindposeCount > 0).ToArray();
            Assert.AreEqual(28, skins.Length);
            foreach (var row in skins)
            {
                // Reusing an existing Mesh asset must replace its vertex streams
                // as well as submesh descriptors. Mixed old vertices/new indices
                // can retain valid bounds and bone sets but corrupt the body.
                var fresh = VoxelWorldMeshBaker.Bake(row.Source, row.VoxelSize);
                try
                {
                    Assert.AreEqual(fresh.vertexCount, row.Voxel.vertexCount, row.Source.name + " vertex count");
                    CollectionAssert.AreEqual(fresh.vertices, row.Voxel.vertices, row.Source.name + " vertices");
                    CollectionAssert.AreEqual(fresh.normals, row.Voxel.normals, row.Source.name + " normals");
                    // P1 deliberately recolors UV0. The complete-object palette
                    // gate checks paint; this gate retains every rig/shape channel.
                    CollectionAssert.AreEqual(fresh.boneWeights, row.Voxel.boneWeights, row.Source.name + " weights");
                    CollectionAssert.AreEqual(fresh.bindposes, row.Voxel.bindposes, row.Source.name + " bindposes");
                    Assert.AreEqual(fresh.subMeshCount, row.Voxel.subMeshCount, row.Source.name + " material slots");
                    for (int sub = 0; sub < fresh.subMeshCount; sub++)
                        CollectionAssert.AreEqual(fresh.GetTriangles(sub), row.Voxel.GetTriangles(sub),
                            row.Source.name + " indices for material slot " + sub);
                    Assert.AreEqual(fresh.bounds, row.Voxel.bounds, row.Source.name + " bounds");
                }
                finally { Object.DestroyImmediate(fresh); }
            }
        }

        [Test]
        public void CompleteNativeCoverageKeepsEveryBaselineBoundsEdgeWithinOneNewCell()
        {
            CollectionAssert.AreEquivalent(scales.Keys, catalog.Bindings.Select(b => b.Source));
            Assert.Greater(scales.Count, 400, "An incomplete catalog cannot satisfy the art gate.");
            int toolkit = 0, coarse = 0, compared = 0;
            foreach (var row in catalog.Bindings)
            {
                // Fifteen ring recipes retain their own topology/fit gate;
                // Morrowfast's native-coordinate scenery has explicit doorway,
                // water, quiet-ground and imported-transform contracts instead.
                if (IsToolkit(row)) { toolkit++; continue; }
                if (MorrowfastCoarseArtContract.IsVerified(row)) { coarse++; continue; }
                var before = Baseline(row.Source).bounds; var after = row.Voxel.bounds;
                float tolerance = row.VoxelSize + .0001f / scales[row.Source];
                for (int axis = 0; axis < 3; axis++)
                {
                    Assert.That(Mathf.Abs(after.min[axis] - before.min[axis]), Is.LessThanOrEqualTo(tolerance),
                        row.Source.name + " minimum edge moved too far on axis " + axis);
                    Assert.That(Mathf.Abs(after.max[axis] - before.max[axis]), Is.LessThanOrEqualTo(tolerance),
                        row.Source.name + " maximum edge moved too far on axis " + axis);
                }
                compared++;
            }
            Assert.AreEqual(15, toolkit, "Only the unchanged SpawnRing toolkit bindings remain active here.");
            Assert.AreEqual(117, coarse, "Only the separately verified 111 native scenery models (six water children) change shape.");
            Assert.AreEqual(274, compared);
            Assert.AreEqual(catalog.Bindings.Length - toolkit - coarse, compared);
        }

        [Test]
        public void SmallHeldEquipmentRetainsItsExactBaselineGeometryAsTheCountercheck()
        {
            var gear = catalog.Bindings.Where(b => AssetDatabase.GetAssetPath(b.Source)
                .StartsWith(VillageModels + "equipment-", StringComparison.Ordinal)).ToArray();
            Assert.AreEqual(6, gear.Length);
            foreach (var row in gear)
            {
                var before = Baseline(row.Source);
                Assert.That(row.WorldVoxelSize, Is.EqualTo(OldWorldPitch(row.Source)).Within(.000001f), row.Source.name);
                CollectionAssert.AreEqual(before.vertices, row.Voxel.vertices, row.Source.name + " vertices");
                CollectionAssert.AreEqual(before.triangles, row.Voxel.triangles, row.Source.name + " faces");
                CollectionAssert.AreEqual(before.normals, row.Voxel.normals, row.Source.name + " normals");
                // P1 changes paint; small-equipment geometry remains exact.
            }
        }

        [Test]
        public void AllFiveLongNarrowPipesKeepTheirExactBaselineContactSilhouettes()
        {
            var paths = new[] {
                "Assets/Art3D/SpawnRing/Models/ring-copper-pipe.fbx",
                "Assets/Art3D/MultiCellPilot/Models/PilotPipe_0.fbx",
                "Assets/Art3D/MultiCellPilot/Models/PilotPipe_1.fbx",
                "Assets/Art3D/MultiCellPilot/Models/PilotPipe_2.fbx",
                "Assets/Art3D/MultiCellPilot/Models/PilotPipe_3.fbx"
            };
            foreach (string path in paths)
            {
                var row = catalog.Bindings.Single(b => AssetDatabase.GetAssetPath(b.Source) == path);
                Assert.IsFalse(IsToolkit(row));
                Assert.AreEqual(0, row.Source.bindposeCount);
                var before = Baseline(row.Source);
                // Native contact picking deliberately leaves corners beside
                // these long bodies empty. Retaining logical owner cells alone
                // does not protect those corners from an inflated visual mesh.
                Assert.That(row.WorldVoxelSize, Is.EqualTo(OldWorldPitch(row.Source)).Within(.000001f), path);
                CollectionAssert.AreEqual(before.vertices, row.Voxel.vertices, path + " contact silhouette");
                CollectionAssert.AreEqual(before.triangles, row.Voxel.triangles, path + " faces");
                CollectionAssert.AreEqual(before.normals, row.Voxel.normals, path + " normals");
                // P1 changes paint; the original pipe contact envelope survives.
                Assert.AreEqual(before.bounds, row.Voxel.bounds, path + " selection envelope");
            }
        }
    }
}
#endif
