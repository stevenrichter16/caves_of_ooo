using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>The native audit selects its stimulus from imported geometry,
    /// independently of the game picker whose result it later verifies.</summary>
    public sealed class MultiCellPilotNativePickingStimulusTests
    {
        static MethodInfo Selector()
        {
            var method = typeof(MultiCellPilotNativeAudit).GetMethod("TryProjectNativeMeshContact", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Native audit must project actual imported mesh contact before asserting a physical edge pick.");
            return method;
        }
        static Entity Ridge(SpawnRing3DIntegrationFixture f) => MultiCellPilotRuntime.FindOwner(f.Zone, "PilotRidgeNE-6-2");
        static bool Select(MethodInfo method, SpawnRing3DIntegrationFixture f, GameObject view, Entity owner, Cell cell, out Vector2 cursor, out Vector3 contact)
        {
            object[] args = { f.Source, f.Get<Camera>("WorldCamera"), view, owner, cell, Vector2.zero, Vector3.zero };
            bool found = (bool)method.Invoke(null, args); cursor = (Vector2)args[5]; contact = (Vector3)args[6]; return found;
        }
        static GameObject Positive(MethodInfo method, SpawnRing3DIntegrationFixture f, out Entity ridge, out Vector2 cursor, out Vector3 contact)
        {
            ridge = Ridge(f); Assert.NotNull(ridge); Assert.IsTrue(f.Find(ridge, out var view, out _));
            Assert.IsNotEmpty(view.GetComponentsInChildren<MeshCollider>(), "Use the real imported irregular ridge selection mesh.");
            Assert.IsTrue(f.Zone.GetCell(8,2).IsVisible); Assert.IsTrue(f.Zone.GetCell(8,2).Occupants.Contains(ridge));
            Assert.AreNotEqual(f.Zone.GetEntityPosition(ridge), (8,2)); Physics.SyncTransforms();
            Assert.IsTrue(Select(method, f, view, ridge, f.Zone.GetCell(8,2), out cursor, out contact));
            return view;
        }

        [Test]
        public void ImportedEdgeStimulusIsMeasuredBeforeRealPickerAndUsesRaisedCameraProjection()
        {
            var method = Selector();
            using (var f = new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                var view = Positive(method, f, out var ridge, out var cursor, out var contact);
                var viewport = f.Source.WorldToViewportPoint(new Vector3(cursor.x, cursor.y, 0));
                var ray = f.Get<Camera>("WorldCamera").ViewportPointToRay(viewport);
                var hits = Physics.RaycastAll(ray, 120, 1 << NativeZone3DRenderSurface.WorldLayer, QueryTriggerInteraction.Collide);
                Assert.IsNotEmpty(hits); var measured = hits.OrderBy(hit => hit.distance).First();
                Assert.IsTrue(measured.collider is MeshCollider);
                Assert.IsTrue(measured.collider.transform == view.transform || measured.collider.transform.IsChildOf(view.transform));
                Assert.Less(Vector3.Distance(contact, measured.point), .003f);
                Assert.IsTrue(Village3DProjection.TryWorldToCell(measured.point, out int cx, out int cy)); Assert.AreEqual((8,2), (cx,cy));
                Assert.Greater(contact.y, .05f); Assert.Greater(Vector2.Distance(cursor, new Vector2(contact.x, contact.z)), .01f,
                    "A raised surface must project away from its physical ground coordinates at the actual camera tilt.");
                object[] pick = { cursor, null, 0, 0 }; Assert.IsTrue((bool)f.Call("TryPickWorld", pick));
                Assert.AreSame(ridge, pick[1]); Assert.AreEqual((cx,cy), ((int)pick[2], (int)pick[3]));
            }
        }

        [TestCase("hole")]
        [TestCase("hidden")]
        [TestCase("disabled_mesh")]
        public void SameImportedMeshCannotManufactureAnInvalidContact(string mutation)
        {
            var method = Selector();
            using (var f = new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                var view = Positive(method, f, out var ridge, out _, out _); var desired = f.Zone.GetCell(8,2);
                if (mutation == "hole")
                { desired = f.Zone.GetCell(6,2); Assert.IsFalse(desired.Occupants.Contains(ridge)); }
                else if (mutation == "hidden")
                { desired.IsVisible = false; f.Refresh(); Assert.IsTrue(f.Rendered(ridge), "Other visible body cells keep the same model active."); }
                else
                { foreach (var collider in view.GetComponentsInChildren<MeshCollider>()) collider.enabled = false; Physics.SyncTransforms(); }
                Assert.IsFalse(Select(method, f, view, ridge, desired, out _, out _), mutation);
            }
        }
    }
}
