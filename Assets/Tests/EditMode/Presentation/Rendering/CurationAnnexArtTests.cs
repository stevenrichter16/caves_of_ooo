using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Geometry, current-owner selection and native repair/door transitions.
    // Fixture placement is disclosed; these are not natural-discovery checks.
    public sealed class CurationAnnexArtTests
    {
        const string Prefix = "curation-yard-";
        [TestCase("service-gate-broken")]
        [TestCase("recovery-cabinet")]
        [TestCase("maintenance-rack")]
        [TestCase("inspection-slab")]
        [TestCase("annex-placard")]
        public void FiveFunctionalFormsHaveReviewedSourceAndPersistentNativeGeometry(string suffix)
        {
            string id = Prefix + suffix;
            Assert.True(CurationYardSource.IsModelId(id), "The annex needs a deliberate native form: " + id);
            var source = JsonUtility.FromJson<CurationYardSource>(File.ReadAllText("ArtSource/CurationYard3D/kit.json"));
            source.Validate(); var shape = source.models.Single(m => m.id == id);
            Assert.GreaterOrEqual(shape.boxes.Select(b => b.color).Distinct().Count(), 3);
            var prefab = Resources.Load<GameObject>("CurationYard3D/" + id); Assert.NotNull(prefab, id);
            var mesh = prefab.GetComponent<MeshFilter>().sharedMesh; Assert.NotNull(mesh);
            Assert.AreEqual(shape.boxes.Length * 24, mesh.vertexCount);
            Assert.AreEqual(shape.boxes.Length * 36, mesh.GetIndexCount(0));
            Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material, prefab.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<Animator>(true));
            Assert.Greater(mesh.bounds.size.y, .30f);
        }

        [TestCase("CurationRecoveryCabinet", "recovery-cabinet")]
        [TestCase("CurationMaintenanceRack", "maintenance-rack")]
        [TestCase("CurationInspectionSlab", "inspection-slab")]
        [TestCase("CurationAnnexPlacard", "annex-placard")]
        public void RealAnnexPropsUseDistinctApprovedFormsWithoutChangingTheirContents(string blueprint, string suffix)
        {
            using (var f = new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
            {
                Assert.True(f.Factory.Blueprints.ContainsKey(blueprint), "CONTENT PRECONDITION: " + blueprint);
                var e = f.Add(blueprint); var contents = e.GetPart<ContainerPart>()?.Contents.ToArray();
                var position = f.Zone.GetEntityPosition(e); var owners = f.Zone.GetReadOnlyEntities().ToArray();
                Exact(f, e, suffix);
                Assert.AreEqual(position, f.Zone.GetEntityPosition(e)); CollectionAssert.AreEquivalent(owners, f.Zone.GetReadOnlyEntities());
                if (contents != null) CollectionAssert.AreEqual(contents, e.GetPart<ContainerPart>().Contents);
                e.GetPart<RenderPart>().Visible = false; f.Refresh(); Assert.False(f.Rendered(e));
                e.GetPart<RenderPart>().Visible = true; Exact(f, e, suffix);
                Assert.True(f.Zone.RemoveEntity(e)); f.Refresh(); Assert.False(f.Rendered(e));
            }
        }

        [TestCase("CurationTransferGate")]
        [TestCase("CurationGalleryGate")]
        public void NativeAnnexDoorCommandsReuseBothQuarantineFormsAndTheirActualAxis(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
            {
                var gate = f.Add(blueprint); f.Approach(gate); var door = gate.GetPart<DoorPart>();
                Assert.NotNull(door); Assert.False(gate.GetPart<PhysicsPart>().Solid); door.QuarterTurns = 1;
                Assert.True(door.IsClosed); Exact(f, gate, "quarantine-gate-closed");
                Assert.AreEqual(1, SpawnRing3DRecipes.Resolve(f.Zone, gate, f.Library.Definition).QuarterTurns);
                Assert.True(door.TrySetOpen(f.Player, f.Zone, true)); Exact(f, gate, "quarantine-gate-open");
                Assert.False(f.Zone.GetEntityCell(gate).BlocksMovement(f.Player));
                Assert.True(door.TrySetOpen(f.Player, f.Zone, false)); Exact(f, gate, "quarantine-gate-closed");
                Assert.True(f.Zone.GetEntityCell(gate).BlocksMovement(f.Player));
            }
        }

        [Test]
        public void PaidTimberRepairChangesTheBrokenServiceGateThenOrdinaryClosureRemainsSaved()
        {
            using (var f = new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
            {
                var gate = f.Add("CurationServiceGate"); f.Approach(gate); string id = gate.ID;
                var fault = gate.GetPart<RepairablePart>(); var door = gate.GetPart<DoorPart>();
                Assert.NotNull(fault); Assert.AreEqual("timber-gate-frame", fault.RecipeId);
                Assert.True(door.IsOpen); Assert.False(fault.Repaired);
                var broken = Exact(f, gate, "service-gate-broken");
                Assert.False(door.TrySetOpen(f.Player, f.Zone, false)); Assert.False(fault.TryRepair(f.Player, f.Zone));
                Assert.AreSame(broken, Exact(f, gate, "service-gate-broken"));
                var material = f.Factory.CreateEntity("SalvagedTimber"); material.GetPart<StackerPart>().StackCount = 2;
                Assert.True(f.Player.GetPart<InventoryPart>().AddObject(material));
                Assert.True(fault.TryRepair(f.Player, f.Zone)); Assert.True(fault.Repaired);
                Assert.AreNotSame(broken, Exact(f, gate, "quarantine-gate-open"));
                Assert.True(door.TrySetOpen(f.Player, f.Zone, false)); Exact(f, gate, "quarantine-gate-closed");
                var saved = f.RoundTrip(); f.BindLoaded(saved);
                var restored = f.Zone.GetReadOnlyEntities().Single(e => e.ID == id);
                Assert.AreNotSame(gate, restored); Assert.True(restored.GetPart<RepairablePart>().Repaired);
                Assert.True(restored.GetPart<DoorPart>().IsClosed); Exact(f, restored, "quarantine-gate-closed");
                Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(gate, out _));
            }
        }

        [TestCase("foreign-door")][TestCase("foreign-fault")][TestCase("custom-visual")]
        public void InvalidServiceGateAuthorityCannotBorrowItsBrokenOrHealthyForms(string fault)
        {
            using (var f = new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
            {
                var gate = f.Add("CurationServiceGate"); var door = gate.GetPart<DoorPart>(); var repair = gate.GetPart<RepairablePart>();
                if (fault == "foreign-door") door.ParentEntity = f.Player;
                if (fault == "foreign-fault") repair.ParentEntity = f.Player;
                if (fault == "custom-visual") gate.GetPart<RenderPart>().VisualID = "unrelated";
                try { Assert.Null(CurationYard3DLibrary.ResolveModel(f.Zone, gate)); }
                finally { door.ParentEntity = gate; repair.ParentEntity = gate; }
            }
        }

        static Mesh Exact(SpawnRing3DIntegrationFixture f, Entity e, string suffix)
        {
            string id = Prefix + suffix; var recipe = SpawnRing3DRecipes.Resolve(f.Zone, e, f.Library.Definition);
            Assert.AreEqual(id, recipe.ModelId, recipe.Failure); Assert.Null(recipe.Failure); Assert.AreSame(e, recipe.Owner);
            f.Refresh(); Assert.True(f.Rendered(e));
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(e, out var proof), proof.Failure);
            var source = Resources.Load<GameObject>("CurationYard3D/" + id).GetComponent<MeshFilter>().sharedMesh;
            Assert.AreSame(source, proof.ExpectedMesh); Assert.AreEqual(id, proof.ModelId); return source;
        }
    }
}
