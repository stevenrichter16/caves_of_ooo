using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class SeedKeeperFlatArtTests
    {
        const string Model = "spread-person-seed-keeper";
        const string Path = "SeedKeeperFlat/seed-keeper";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static T Read<T>(object value, string name) => (T)value.GetType().GetProperty(name, Hidden).GetValue(value);
        static ExaminePortraitRenderer PortraitProbe() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), Hidden,
            null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        static bool Proof(SpawnRing3DIntegrationFixture f, Entity owner, out SpreadBiomeStyleEvidence evidence)
        {
            var args = new object[] { owner, null };
            bool result = (bool)f.Call("TryGetApprovedStyle", args); evidence = (SpreadBiomeStyleEvidence)args[1]; return result;
        }

        [Test] public void BlenderAuthoredKeeperIsOneFlatPersistentMeshWithItsOwnPalette()
        {
            var prefab = Resources.Load<GameObject>(Path);
            Assert.NotNull(prefab, "Import the Blender-authored flat keeper before GREEN; existing cuboid body is not this art.");
            Assert.AreEqual(1, prefab.GetComponentsInChildren<Renderer>(true).Length);
            Assert.IsEmpty(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
            var filter = prefab.GetComponent<MeshFilter>(); Assert.NotNull(filter);
            Assert.That(filter.sharedMesh.bounds.size.z, Is.LessThan(.00001f));
            Assert.That(filter.sharedMesh.bounds.size.y, Is.InRange(1.35f, 1.65f));
            Assert.That(filter.sharedMesh.bounds.size.x, Is.InRange(.9f, 1.2f));
            Assert.That(filter.sharedMesh.vertices.All(v => Mathf.Abs(v.z) < .00001f));
            var material = prefab.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.AreNotSame(ReferenceGladeVoxelLibrary.Load().Material, material);
            Assert.AreEqual(FilterMode.Point, material.GetTexture("_BaseMap").filterMode);
            foreach (string property in new[] { "_FogLight", "_Transient", "_Exposure" }) Assert.True(material.HasProperty(property));
        }

        [Test] public void CurrentKeeperUsesFlatArtAndNativeVisibilityPickingStockAndProof()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("SpreadSeedKeeper"); var stock = owner.GetPart<InventoryPart>().Objects.ToArray();
                var counts = stock.Select(e => e.GetPart<StackerPart>()?.StackCount ?? 1).ToArray(); var at = f.Zone.GetEntityPosition(owner);
                f.Refresh(); Assert.True(f.Find(owner, out var view, out var id)); Assert.AreEqual(Model, id);
                Assert.AreSame(Resources.Load<GameObject>(Path), f.Library.FindModel(id));
                Assert.NotNull(view.GetComponent<MeshFilter>(), "The actual owner must use flat authored geometry.");
                Assert.True(Proof(f, owner, out var proof), proof.Failure); Assert.AreSame(view.GetComponent<MeshFilter>().sharedMesh, proof.SubmittedMesh);
                Assert.True(f.Rendered(owner)); Assert.True(f.Pick(owner, out _));
                var collider = view.GetComponent<BoxCollider>(); var meshBounds = view.GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That((collider.center - meshBounds.center).sqrMagnitude, Is.LessThan(.000001f));
                Assert.AreEqual(meshBounds.size.x, collider.size.x, .00001f); Assert.AreEqual(meshBounds.size.y, collider.size.y, .00001f);
                Assert.AreEqual(.04f, collider.size.z, .00001f, "Flat selection must not inherit inverse-rotated AABB depth inflation.");
                Assert.AreEqual(at, f.Zone.GetEntityPosition(owner)); CollectionAssert.AreEqual(stock, owner.GetPart<InventoryPart>().Objects);
                CollectionAssert.AreEqual(counts, stock.Select(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                owner.GetPart<RenderPart>().Visible = false; f.Refresh(); Assert.False(f.Rendered(owner)); Assert.False(Proof(f, owner, out _));
                owner.GetPart<RenderPart>().Visible = true; f.Refresh(); Assert.True(f.Rendered(owner));
                f.Zone.RemoveEntity(owner); f.Refresh(); Assert.False(f.Find(owner, out _, out _));
            }
        }

        [TestCase("SpreadWaysideCook")][TestCase("Farmer")]
        public void OtherResidentsRetainTheirExistingRiggedBodies(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            { var owner = f.Add(blueprint); f.Refresh(); Assert.NotNull(f.View(owner).GetComponentInChildren<SkinnedMeshRenderer>()); }
        }

        [TestCase("glyph")][TestCase("visual")][TestCase("portable")]
        public void ChangedKeeperCannotBorrowTheFlatNativeIdentity(string change)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("SpreadSeedKeeper");
                if (change == "glyph") owner.GetPart<RenderPart>().RenderString = "?";
                if (change == "visual") owner.GetPart<RenderPart>().VisualID = "unrelated";
                if (change == "portable") owner.GetPart<PhysicsPart>().Takeable = true;
                Assert.AreNotEqual(Model, SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition).ModelId);
            }
        }

        [Test] public void MovementAndActionFacingCannotTurnTheFlatKeeperEdgeOn()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("SpreadSeedKeeper"); f.Refresh(); var view = f.View(owner);
                Assert.NotNull(view.GetComponent<MeshFilter>());
                var initial = view.transform.rotation; var at = f.Zone.GetEntityPosition(owner);
                EntityVisualHooks.EmitMoved(owner, f.Zone, at.x, at.y, at.x + 1, at.y);
                EntityVisualHooks.AttackCallback?.Invoke(owner, f.Player, f.Zone); f.Frame();
                Assert.Less(Quaternion.Angle(initial, view.transform.rotation), .01f);
                Assert.Less(Quaternion.Angle(f.Get<Camera>("WorldCamera").transform.rotation, view.transform.rotation), .01f);
            }
        }

        [Test] public void ExamineSharesFlatWorldSourceAndFacesItsOwnCameraWithoutScripts()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            using (var renderer = PortraitProbe())
            {
                var owner = f.Add("SpreadSeedKeeper"); f.Refresh(); var result = renderer.GetPortrait(f.Zone, owner);
                Assert.True(result.IsModel); Assert.AreEqual(Model, renderer.CurrentModelId);
                Assert.AreSame(Resources.Load<GameObject>(Path), Read<GameObject>(renderer, "CurrentSourcePrefab"));
                var root = Read<GameObject>(renderer, "PreviewRoot"); var camera = Read<Camera>(renderer, "PreviewCamera");
                Assert.NotNull(root.GetComponent<MeshFilter>()); Assert.IsEmpty(root.GetComponentsInChildren<MonoBehaviour>(true));
                Assert.Less(Quaternion.Angle(root.transform.rotation, camera.transform.rotation), .01f);
                renderer.GetPortrait(f.Zone, owner); Assert.AreEqual(1, Read<int>(renderer, "CaptureCount"));
                owner.GetPart<RenderPart>().Visible = false; Assert.Null(renderer.GetPortrait(f.Zone, owner).Texture);
            }
        }
    }
}
