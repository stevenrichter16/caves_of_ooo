using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Geometric countercheck for the native occupancy-hole gate.
    /// Vertical rays test actual presenter-created voxel MeshColliders, avoiding
    /// the tilted camera's legitimate contacts in neighboring physical cells.
    /// Pilot roots have fixed orientation; NE/SE are shipped authored designs,
    /// not a claim that runtime footprint rotation is supported.</summary>
    [Category("VoxelWorldRealArt")]
    public sealed class VoxelWorldDensityHoleTests
    {
        [TestCase("PilotRidgeNE-6-2", "PilotRidgeNE_0", 0, 1, 2)]
        [TestCase("PilotRidgeNE-3-6", "PilotRidgeNE_1", 0, 1, 2)]
        [TestCase("PilotRidgeNE-0-14", "PilotRidgeNE_2", 0, 1, 2)]
        [TestCase("PilotRidgeNE-0-20", "PilotRidgeNE_3", 0, 1, 2)]
        [TestCase("PilotRidgeSE-47-0", "PilotRidgeSE_0", 3, 2, 1)]
        [TestCase("PilotRidgeSE-49-4", "PilotRidgeSE_1", 3, 2, 1)]
        [TestCase("PilotRidgeSE-51-8", "PilotRidgeSE_2", 3, 2, 1)]
        [TestCase("PilotRidgeSE-54-14", "PilotRidgeSE_3", 3, 2, 1)]
        public void CoarseRidgeColliderKeepsEmptyCornerAndAdjacentGapOpen(
            string ownerId, string modelId, int cornerX, int gapX, int occupiedX)
        {
            using (var fixture = new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                Assert.IsTrue(fixture.Get<bool>("VoxelPresentationActive"));
                Assert.Greater(fixture.Get<int>("VoxelAppliedMeshCount"), 0);
                Assert.AreEqual(0, fixture.Get<int>("VoxelMissingMeshCount"));
                var owner = fixture.Zone.GetReadOnlyEntities().Single(e =>
                    e.GetPart<MultiCellPilotPropPart>()?.OwnerId == ownerId);
                Assert.AreEqual(modelId, owner.GetPart<MultiCellPilotPropPart>().ModelId);
                Assert.AreEqual(10, fixture.Zone.GetOccupiedCells(owner).Count);
                Assert.IsTrue(fixture.Find(owner, out var root, out var actualModel));
                Assert.AreEqual(modelId, actualModel);
                Assert.IsTrue(fixture.Rendered(owner));
                Assert.AreEqual(Quaternion.identity, root.transform.rotation,
                    "A fixed pilot footprint must retain its authored orientation.");

                var catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
                var pilot = Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
                Assert.NotNull(catalog); catalog.Validate(); Assert.NotNull(pilot); pilot.Validate();
                var prefab = pilot.FindModel(modelId); Assert.NotNull(prefab);
                var sourceMeshes = prefab.GetComponentsInChildren<MeshFilter>(true)
                    .Select(f => f.sharedMesh).ToArray();
                Assert.IsNotEmpty(sourceMeshes);
                var expectedMeshes = sourceMeshes.Select(source =>
                {
                    Assert.NotNull(source);
                    var binding = catalog.Bindings.Single(b => b.Source == source);
                    Assert.AreNotSame(source, binding.Voxel, "The ray must exercise installed voxel art.");
                    Assert.That(binding.WorldVoxelSize, Is.EqualTo(.25f).Within(.000001f),
                        "This gate must exercise the new coarser stationary-object pitch.");
                    return binding.Voxel;
                }).ToArray();

                var colliders = root.GetComponentsInChildren<MeshCollider>(true);
                Assert.IsNotEmpty(colliders, "A missing collider cannot masquerade as an open hole.");
                Assert.AreEqual(colliders.Length, root.GetComponentsInChildren<Collider>(true).Length,
                    "This irregular ridge must use mesh selection, not a whole-bounds box.");
                CollectionAssert.AreEquivalent(expectedMeshes, colliders.Select(c => c.sharedMesh));
                foreach (var collider in colliders)
                {
                    Assert.IsTrue(collider.enabled && collider.gameObject.activeInHierarchy);
                    Assert.IsFalse(collider.convex, "A convex hull would fill the irregular footprint gaps.");
                    var filter = collider.GetComponent<MeshFilter>(); Assert.NotNull(filter);
                    Assert.AreSame(filter.sharedMesh, collider.sharedMesh,
                        "Selection must use the actual displayed voxel surface.");
                }
                Physics.SyncTransforms();
                Bounds bounds = colliders[0].bounds;
                foreach (var collider in colliders.Skip(1)) bounds.Encapsulate(collider.bounds);
                var anchor = fixture.Zone.GetEntityPosition(owner);
                var occupied = fixture.Zone.GetCell(anchor.x + occupiedX, anchor.y);
                var gap = fixture.Zone.GetCell(anchor.x + gapX, anchor.y);
                var corner = fixture.Zone.GetCell(anchor.x + cornerX, anchor.y);
                Assert.NotNull(occupied); Assert.NotNull(gap); Assert.NotNull(corner);
                Assert.IsTrue(occupied.Occupants.Contains(owner));
                Assert.IsFalse(gap.Occupants.Contains(owner));
                Assert.IsFalse(corner.Occupants.Contains(owner));
                Assert.AreEqual(1, Mathf.Abs(gap.X - occupied.X), "Positive and negative cells must be adjacent.");

                // Countercheck first: neither an inactive mesh nor a ray aimed
                // outside the entire body can make the negative probes pass.
                Assert.IsTrue(HitsVertical(colliders, bounds, occupied, out var contact),
                    ownerId + " occupied-cell center must hit the real coarse mesh.");
                Assert.IsTrue(Village3DProjection.TryWorldToCell(contact, out int hitX, out int hitY));
                Assert.AreEqual((occupied.X, occupied.Y), (hitX, hitY));
                Assert.IsFalse(HitsVertical(colliders, bounds, gap, out _),
                    ownerId + " coarse geometry sealed the empty cell beside its occupied edge.");
                Assert.IsFalse(HitsVertical(colliders, bounds, corner, out _),
                    ownerId + " coarse geometry sealed the known empty bounding-box corner.");
                TestContext.WriteLine(modelId + ": occupied (" + occupied.X + "," + occupied.Y
                    + ") hit; gap (" + gap.X + "," + gap.Y + ") and corner ("
                    + corner.X + "," + corner.Y + ") miss actual voxel mesh colliders.");
            }
        }

        static bool HitsVertical(MeshCollider[] colliders, Bounds bounds, Cell cell, out Vector3 contact)
        {
            var centre = Village3DProjection.CellCentre(cell.X, cell.Y);
            Assert.That(centre.x, Is.GreaterThan(bounds.min.x).And.LessThan(bounds.max.x));
            Assert.That(centre.z, Is.GreaterThan(bounds.min.z).And.LessThan(bounds.max.z));
            var ray = new Ray(new Vector3(centre.x, bounds.max.y + 2f, centre.z), Vector3.down);
            float maximum = bounds.size.y + 4f, nearest = float.PositiveInfinity;
            contact = default;
            foreach (var collider in colliders)
                if (collider.Raycast(ray, out var hit, maximum) && hit.distance < nearest)
                { nearest = hit.distance; contact = hit.point; }
            return !float.IsPositiveInfinity(nearest);
        }
    }
}
