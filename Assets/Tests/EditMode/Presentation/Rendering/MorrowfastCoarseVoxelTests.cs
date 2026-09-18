#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Native-coordinate contracts for the offline town restyle. Tests
    /// never rebuild assets and never replace the imported prefabs or owners.</summary>
    public sealed class MorrowfastCoarseVoxelTests
    {
        const string OutputRoot = "Assets/Art3D/VoxelWorld/Morrowfast/";
        static Type Builder
        {
            get
            {
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("CavesOfOoo.Editor.MorrowfastCoarseVoxelBuilder"))
                    .FirstOrDefault(t => t != null);
                Assert.NotNull(type, "The native-coordinate coarse scenery generator must exist before export.");
                return type;
            }
        }
        static object Call(string name, params object[] args)
        {
            var method = Builder.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, name);
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        static MorrowfastSceneDefinition Native()
            => MorrowfastSceneDefinition.Parse(Resources.Load<TextAsset>("SceneArt/Morrowfast/definition").text);
        static Village3DLibrary Village()
        {
            var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            Assert.NotNull(library); library.Validate(); return library;
        }
        static Mesh Make(string id, MorrowfastSceneDefinition native = null, bool water = false)
            => (Mesh)Call("CreateModelMesh", id, native ?? Native(), water);
        static bool Supports(string id) => (bool)Call("SupportsModel", id);
        static void Geometry(Mesh mesh, int budget)
        {
            Assert.NotNull(mesh); Assert.That(mesh.vertexCount, Is.InRange(24, budget));
            Assert.AreEqual(mesh.vertexCount, mesh.uv.Length);
            Assert.That(mesh.uv.Distinct().Count(), Is.InRange(1, 2));
            Assert.AreEqual(1, mesh.subMeshCount);
            Assert.IsEmpty(mesh.bindposes); Assert.IsEmpty(mesh.boneWeights);
            foreach (var vertex in mesh.vertices)
                foreach (float n in new[] { vertex.x, vertex.y, vertex.z })
                    Assert.IsTrue(!float.IsNaN(n) && !float.IsInfinity(n));
            var vertices = mesh.vertices; var indices = mesh.triangles;
            Assert.Greater(indices.Length, 0); Assert.AreEqual(0, indices.Length % 3);
            for (int i = 0; i < indices.Length; i += 3)
                Assert.Greater(Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]],
                    vertices[indices[i + 2]] - vertices[indices[i]]).sqrMagnitude, .000000000001f);
        }
        // Vertical ray through real triangles, including holes: bounds alone
        // cannot prove that a doorway or water cell remains visually open.
        static float TopAt(Mesh mesh, float x, float z)
        {
            var vertices = mesh.vertices; var indices = mesh.triangles; float highest = float.NegativeInfinity;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = vertices[indices[i]]; var b = vertices[indices[i + 1]]; var c = vertices[indices[i + 2]];
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < .000001f) continue;
                float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / denominator;
                float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / denominator;
                if (u < -.00001f || v < -.00001f || u + v > 1.00001f) continue;
                highest = Mathf.Max(highest, u * a.y + v * b.y + (1 - u - v) * c.y);
            }
            return highest;
        }

        [Test]
        public void ContractCoversAll111StaticModelsWithoutClaimingRiggedActorsEquipmentOrCreatures()
        {
            var library = Village(); int covered = 0;
            foreach (var model in library.Definition.models)
            {
                bool expected = !model.rigged && !model.id.StartsWith("equipment-", StringComparison.Ordinal)
                    && model.id != "frog" && model.id != "tortoise";
                Assert.AreEqual(expected, Supports(model.id), model.id);
                if (expected) covered++;
            }
            Assert.AreEqual(111, covered);
            foreach (string invalid in new[] { null, "", "foreign-ground", "ground-patch-4", "detail-patch-08-00", "detail-patch-00-05" })
                Assert.IsFalse(Supports(invalid), invalid);
        }

        [TestCase("keeper-gatehouse")][TestCase("dry-hem-guesthouse")]
        [TestCase("long-loop-ropeshop")][TestCase("return-desk-archive")][TestCase("second-bowl-kitchen")]
        public void ShellKeepsActualInteriorAndNativeDoorApertureWhileUsingBroadWallRuns(string roomId)
        {
            var native = Native(); var room = native.buildings.Single(b => b.id == roomId);
            var owner = native.FindOwner(roomId + "-shell"); var door = native.FindOwner(room.doorId);
            var mesh = Make(roomId + "-shell", native);
            try
            {
                Geometry(mesh, 384);
                foreach (var cell in room.interior)
                    Assert.That(TopAt(mesh, cell.x - owner.anchorX, owner.anchorY - cell.y), Is.InRange(0f, .18f), roomId + " interior");
                float dx = door.anchorX - owner.anchorX, dz = owner.anchorY - door.anchorY;
                foreach (float offset in new[] { -.35f, 0f, .35f })
                    Assert.Less(TopAt(mesh, dx + offset, dz), .2f, "Native doorway must not be filled by coarse wall geometry.");
                int west = room.interior.Min(c => c.x) - 1;
                int north = room.interior.Min(c => c.y);
                Assert.Greater(TopAt(mesh, west - owner.anchorX, owner.anchorY - north), 1f, "Control: the native side wall must remain visible.");
                Assert.That(mesh.bounds.max.y, Is.InRange(1.05f, 1.55f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [TestCase("keeper-gatehouse")][TestCase("dry-hem-guesthouse")]
        [TestCase("long-loop-ropeshop")][TestCase("return-desk-archive")][TestCase("second-bowl-kitchen")]
        public void RemovableRoofIsFewBroadTerracesCoveringItsOwnRoom(string roomId)
        {
            var native = Native(); var room = native.buildings.Single(b => b.id == roomId);
            var owner = native.FindOwner(roomId + "-roof"); var mesh = Make(roomId + "-roof", native);
            try
            {
                Geometry(mesh, 240);
                Assert.That(mesh.bounds.min.y, Is.InRange(1.3f, 1.9f));
                Assert.LessOrEqual(mesh.bounds.max.y, 2.3f);
                foreach (var cell in room.interior)
                    Assert.Greater(TopAt(mesh, cell.x - owner.anchorX, owner.anchorY - cell.y), 1.3f);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void AllFiveRemovableRoofsActuallyRestOnTheirOwnWallTops()
        {
            var native = Native();
            foreach (var room in native.buildings)
            {
                var shell = Make(room.id + "-shell", native); var roof = Make(room.id + "-roof", native);
                try { Assert.That(roof.bounds.min.y, Is.EqualTo(shell.bounds.max.y).Within(.001f), room.id + " has an unsupported roof gap."); }
                finally { Object.DestroyImmediate(shell); Object.DestroyImmediate(roof); }
            }
        }

        [Test]
        public void DoorRetainsLeftHingeAndNarrowPanelInsteadOfGrowingAroundItsBoundingBox()
        {
            var mesh = Make("oak-door");
            try
            {
                Geometry(mesh, 120);
                Assert.That(mesh.bounds.min.x, Is.InRange(-.025f, .025f));
                Assert.That(mesh.bounds.max.x, Is.InRange(.86f, .94f));
                Assert.LessOrEqual(mesh.bounds.size.z, .24f);
                Assert.That(mesh.bounds.max.y, Is.InRange(1.5f, 1.8f));
                Assert.Greater(TopAt(mesh, .45f, 0), 1.5f);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void GroundPatchesHaveOneContinuousQuietTopWithoutFlecksOrRepeatedRaisedCubes(int variant)
        {
            var mesh = Make("ground-patch-" + variant);
            try
            {
                Geometry(mesh, 48); Assert.AreEqual(1, mesh.uv.Distinct().Count());
                Assert.That(mesh.bounds.size.x, Is.EqualTo(10).Within(.0001f));
                Assert.That(mesh.bounds.size.z, Is.EqualTo(5).Within(.0001f));
                Assert.That(mesh.bounds.max.y, Is.InRange(-.015f, .015f));
                for (float x = -4.75f; x < 5; x += .5f)
                    for (float z = -2.25f; z < 2.5f; z += .5f)
                        Assert.That(TopAt(mesh, x, z), Is.EqualTo(mesh.bounds.max.y).Within(.0001f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [TestCase(22)][TestCase(26)]
        public void WesternFootbridgeHasReadableDryApproachesWithoutPavingItsNativeWater(int bankX)
        {
            var native = Native(); var bridge = native.FindOwner("western-footbridge");
            Assert.IsTrue(bridge.bridgeSupport.Any(p => p.y == bridge.anchorY));
            Assert.IsFalse(native.cells.Single(c => c.x == bankX && c.y == bridge.anchorY).water);
            int gx = bankX / 10, gy = (24 - bridge.anchorY) / 5;
            var mesh = Make("detail-patch-" + gx.ToString("00") + "-" + gy.ToString("00"), native);
            try
            {
                float z = 24.5f - bridge.anchorY - (gy * 5 + 2.5f);
                Assert.Greater(TopAt(mesh, bankX + .5f - (gx * 10 + 5), z), .01f, "The road should visibly reach the existing bridge's dry bank.");
                foreach (var cell in bridge.bridgeSupport.Where(p => p.y == bridge.anchorY))
                    Assert.Less(TopAt(mesh, cell.x + .5f - (gx * 10 + 5), z), .005f, "The bridge, not a permanent terrain slab, spans this water.");
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void TerrainDetailUsesExactNativeWaterCellsAndNeverPavesTheWaterOrStarterGarden()
        {
            var native = Native(); int wet = 0, dry = 0;
            for (int gy = 0; gy < 5; gy++) for (int gx = 0; gx < 8; gx++)
            {
                string id = "detail-patch-" + gx.ToString("00") + "-" + gy.ToString("00");
                var opaque = Make(id, native); var water = Make(id, native, true);
                try
                {
                    Geometry(opaque, 1200); Geometry(water, 1200);
                    Assert.LessOrEqual(opaque.bounds.max.y, .09f);
                    foreach (var cell in native.cells.Where(c => c.x / 10 == gx && (24 - c.y) / 5 == gy))
                    {
                        float x = cell.x + .5f - (gx * 10 + 5), z = 24.5f - cell.y - (gy * 5 + 2.5f);
                        bool shownWater = TopAt(water, x, z) > 0;
                        Assert.AreEqual(cell.water, shownWater, id + " native water " + cell.x + "," + cell.y);
                        if (cell.water)
                        { wet++; Assert.Less(TopAt(opaque, x, z), .005f, "No ground detail covers native water."); }
                        else dry++;
                        if ((cell.x == 41 || cell.x == 42) && cell.y >= 21 && cell.y <= 23)
                            Assert.Less(TopAt(opaque, x, z), .005f, "Living starter plants own this clear garden cell.");
                    }
                }
                finally { Object.DestroyImmediate(opaque); Object.DestroyImmediate(water); }
            }
            Assert.Greater(wet, 0); Assert.Greater(dry, wet);
        }

        [TestCase("central-well")][TestCase("market-stall-0")][TestCase("market-stall-1")]
        [TestCase("bread-oven")][TestCase("table-2")][TestCase("bookshelf")]
        [TestCase("oath-arch")][TestCase("footbridge")][TestCase("handcart")]
        public void FunctionalPropsHaveSmallPurposefulGeometryBudgets(string modelId)
        {
            var mesh = Make(modelId); try { Geometry(mesh, 240); } finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void WellRetainsVisibleOpenCenterRatherThanBecomingASolidStoneBox()
        {
            var opaque = Make("central-well"); var water = Make("central-well", null, true);
            try
            {
                Assert.Less(TopAt(opaque, 0, 0), .1f);
                Assert.That(TopAt(water, 0, 0), Is.InRange(.05f, .5f));
                Assert.Greater(TopAt(opaque, 1.55f, 0), .8f);
                Assert.AreEqual(1, opaque.uv.Distinct().Count(), "Native water already consumes the second object color.");
            }
            finally { Object.DestroyImmediate(opaque); Object.DestroyImmediate(water); }
        }

        [TestCase("fence-", 4)][TestCase("planter-", 4)][TestCase("bed-", 2)]
        public void RepeatedAuthoredFamiliesRetainDistinctQuietShapeVariants(string prefix, int count)
        {
            var meshes = new List<Mesh>();
            try
            {
                for (int variant = 0; variant < count; variant++) meshes.Add(Make(prefix + variant));
                for (int a = 0; a < count; a++) for (int b = a + 1; b < count; b++)
                    Assert.IsFalse(meshes[a].vertices.SequenceEqual(meshes[b].vertices), prefix + a + " must differ in shape from " + prefix + b);
                foreach (var mesh in meshes) Geometry(mesh, 240);
            }
            finally { foreach (var mesh in meshes) Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void RepeatedGenerationIsDeterministicAndRejectsForeignModelRequests()
        {
            var first = Make("keeper-gatehouse-shell"); var second = Make("keeper-gatehouse-shell");
            try
            {
                CollectionAssert.AreEqual(first.vertices, second.vertices); CollectionAssert.AreEqual(first.triangles, second.triangles);
                CollectionAssert.AreEqual(first.uv, second.uv);
                foreach (string id in new[] { "character-teal", "equipment-staff", "frog", "foreign-shell" })
                    Assert.Throws<ArgumentException>(() => Make(id), id);
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [Test]
        public void ImportedChildTransformPreservesEveryCoarseModelCoordinateAndPalette()
        {
            var village = Village(); var native = Native();
            var catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            Assert.NotNull(catalog); catalog.Validate(); int meshes = 0, waterMeshes = 0;
            foreach (var model in village.Models.Where(m => Supports(m.Id)))
            {
                foreach (var filter in model.Prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    bool water = filter.GetComponent<MeshRenderer>().sharedMaterial.shader.name == "CavesOfOoo/Village3D/Water";
                    var expected = Make(model.Id, native, water);
                    try
                    {
                        var actual = catalog.Resolve(filter.sharedMesh);
                        Assert.IsTrue(AssetDatabase.GetAssetPath(actual).StartsWith(OutputRoot, StringComparison.Ordinal), model.Id);
                        var toModel = model.Prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                        var vertices = actual.vertices; var points = expected.vertices;
                        Assert.AreEqual(points.Length, vertices.Length, model.Id);
                        for (int i = 0; i < points.Length; i++)
                            Assert.Less(Vector3.Distance(points[i], toModel.MultiplyPoint3x4(vertices[i])), .0001f, model.Id + " transformed vertex " + i);
                        CollectionAssert.AreEqual(expected.uv, actual.uv, model.Id + " unchanged material-frame swatches");
                        meshes++; if (water) waterMeshes++;
                    }
                    finally { Object.DestroyImmediate(expected); }
                }
            }
            Assert.AreEqual(117, meshes); Assert.AreEqual(6, waterMeshes);
        }

        [Test]
        public void ImportedOverlayCoversOnlyTheExact111VillageSceneryModelsAndKeepsNativeHierarchy()
        {
            var village = Village(); var catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            Assert.NotNull(catalog); catalog.Validate(); int covered = 0;
            var expectedSources = new HashSet<Mesh>();
            foreach (var model in village.Models)
            {
                var filters = model.Prefab.GetComponentsInChildren<MeshFilter>(true);
                if (Supports(model.Id))
                {
                    covered++; Assert.IsEmpty(model.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true));
                    foreach (var filter in filters)
                    {
                        expectedSources.Add(filter.sharedMesh); var replacement = catalog.Resolve(filter.sharedMesh);
                        Assert.IsTrue(AssetDatabase.GetAssetPath(replacement).StartsWith(OutputRoot, StringComparison.Ordinal), model.Id);
                        Assert.AreNotSame(filter.sharedMesh, replacement); Assert.AreEqual(filter.sharedMesh.subMeshCount, replacement.subMeshCount);
                        Assert.IsEmpty(replacement.bindposes); Assert.That(replacement.uv.Distinct().Count(), Is.InRange(1, 2));
                    }
                    Assert.IsEmpty(model.Prefab.GetComponentsInChildren<Collider>(true));
                    Assert.IsEmpty(model.Prefab.GetComponentsInChildren<Rigidbody>(true));
                    Assert.IsEmpty(model.Prefab.GetComponentsInChildren<Light>(true));
                }
                else
                    foreach (var skin in model.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        Assert.IsFalse(AssetDatabase.GetAssetPath(catalog.Resolve(skin.sharedMesh)).StartsWith(OutputRoot, StringComparison.Ordinal), model.Id);
            }
            Assert.AreEqual(111, covered);
            CollectionAssert.AreEquivalent(expectedSources, catalog.Bindings.Where(b => AssetDatabase.GetAssetPath(b.Voxel)
                .StartsWith(OutputRoot, StringComparison.Ordinal)).Select(b => b.Source));
        }
    }

    /// <summary>Legacy art gates may delegate geometry only for these exact
    /// published native sources. A path prefix alone never grants an exemption.</summary>
    internal static class MorrowfastCoarseArtContract
    {
        internal const string Root = "Assets/Art3D/VoxelWorld/Morrowfast/";
        internal static bool IsVerified(VoxelWorldMeshCatalog.Binding binding)
        {
            string target = AssetDatabase.GetAssetPath(binding.Voxel);
            if (!target.StartsWith(Root, StringComparison.Ordinal)) return false;
            string source = AssetDatabase.GetAssetPath(binding.Source);
            const string models = "Assets/Art3D/Village/Models/";
            Assert.IsTrue(source.StartsWith(models, StringComparison.Ordinal), "A coarse town exception cannot claim foreign content.");
            string id = System.IO.Path.GetFileNameWithoutExtension(source);
            var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath); Assert.NotNull(village);
            var spec = village.Definition.FindModel(id); Assert.NotNull(spec, id);
            Assert.IsFalse(spec.rigged || id.StartsWith("equipment-", StringComparison.Ordinal) || id == "frog" || id == "tortoise", id);
            Assert.AreEqual(models + id + ".fbx", source);
            var prefab = village.FindModel(id); Assert.NotNull(prefab);
            Assert.IsTrue(prefab.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh == binding.Source), id);
            Assert.AreEqual(Root + binding.SourceKey + ".asset", target);
            Assert.AreEqual("CavesOfOoo.Morrowfast.CoarseScenery/1", AssetImporter.GetAtPath(target).userData);
            Assert.AreEqual(0, binding.Source.bindposeCount); Assert.IsEmpty(binding.Voxel.bindposes);
            Assert.AreEqual(binding.Source.subMeshCount, binding.Voxel.subMeshCount);
            Assert.That(binding.Voxel.uv.Distinct().Count(), Is.InRange(1, 2));
            return true;
        }
    }
}
#endif
