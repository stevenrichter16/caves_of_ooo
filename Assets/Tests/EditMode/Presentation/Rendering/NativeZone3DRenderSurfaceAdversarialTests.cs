using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Resource ownership, material override precedence and extracted
    /// presenter fallback. These EditMode tests observe object/API state, not
    /// final GPU pixels, engine frame timing or native gameplay input.</summary>
    public sealed class NativeZone3DRenderSurfaceAdversarialTests
    {
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly Camera Source;
            public readonly RenderTexture Borrowed;
            public readonly Village3DLibrary Library;
            readonly List<NativeZone3DRenderSurface> surfaces = new List<NativeZone3DRenderSurface>();
            readonly List<Object> extras = new List<Object>();
            public Fixture()
            {
                try
                {
                    Library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                    Assert.NotNull(Library); Library.Validate();
                    Root = new GameObject("owned surface adversarial fixture");
                    Source = Camera("borrowed source"); Source.orthographic = true;
                    Source.orthographicSize = 12.75f; Source.transform.position = new Vector3(40, 12.75f, -10);
                    Borrowed = Own(new RenderTexture(384, 256, 16)); Assert.IsTrue(Borrowed.Create());
                    Source.targetTexture = Borrowed; Source.aspect = 1.5f;
                }
                catch { Dispose(); throw; }
            }
            public T Own<T>(T value) where T : Object { extras.Add(value); return value; }
            public Camera Camera(string name)
            {
                var go = new GameObject(name); go.transform.SetParent(Root.transform, false);
                var camera = go.AddComponent<Camera>(); camera.enabled = false; return camera;
            }
            public NativeZone3DRenderSurface Create(Material[] sources = null, float exposure = 2.2f)
            {
                var surface = new NativeZone3DRenderSurface(Root.transform, Library.Renderer,
                    Library.RendererIndex, Library.CompositeMaterial,
                    sources ?? new[] { Library.WorldMaterial, Library.WaterMaterial }, exposure);
                surfaces.Add(surface); return surface;
            }
            public GameObject Model(NativeZone3DRenderSurface surface)
            {
                var model = new GameObject("owned two-renderer model");
                model.transform.SetParent(surface.ContentRoot, false);
                for (int i = 0; i < 2; i++)
                {
                    var child = new GameObject("material child " + i); child.transform.SetParent(model.transform, false);
                    child.layer = 0;
                    var renderer = child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = new[] { Library.WorldMaterial, Library.WaterMaterial };
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                }
                return model;
            }
            public void Dispose()
            {
                try { for (int i = surfaces.Count - 1; i >= 0; i--) surfaces[i].Dispose(); }
                finally
                {
                    if (Root != null) Object.DestroyImmediate(Root);
                    for (int i = extras.Count - 1; i >= 0; i--) if (extras[i] != null)
                    { if (extras[i] is RenderTexture rt) rt.Release(); Object.DestroyImmediate(extras[i]); }
                }
            }
        }
        static Renderer[] Renderers(GameObject go) => go.GetComponentsInChildren<Renderer>(true);
        static MaterialPropertyBlock Block(Renderer renderer, int slot = -1)
        {
            var block = new MaterialPropertyBlock();
            if (slot < 0) renderer.GetPropertyBlock(block); else renderer.GetPropertyBlock(block, slot);
            return block;
        }
        static HashSet<int> ResourcesNow()
        {
            var result = new HashSet<int>();
            foreach (var value in Resources.FindObjectsOfTypeAll<Material>()) result.Add(value.GetInstanceID());
            foreach (var value in Resources.FindObjectsOfTypeAll<Texture>()) result.Add(value.GetInstanceID());
            foreach (var value in Resources.FindObjectsOfTypeAll<Mesh>()) result.Add(value.GetInstanceID());
            foreach (var value in Resources.FindObjectsOfTypeAll<GameObject>()) result.Add(value.GetInstanceID());
            return result;
        }
        static void Hidden(NativeZone3DRenderSurface surface)
        {
            Assert.IsFalse(surface.IsVisible);
            if (surface.WorldCamera != null) Assert.IsFalse(surface.WorldCamera.enabled);
            if (surface.ContentRoot != null) Assert.IsFalse(surface.ContentRoot.gameObject.activeInHierarchy);
            if (surface.CompositeRenderer != null) Assert.IsFalse(surface.CompositeRenderer.gameObject.activeInHierarchy);
        }
        static void TargetSize(NativeZone3DRenderSurface surface, Camera source, float fraction)
        {
            var target = surface.WorldCamera.targetTexture; Assert.NotNull(target); Assert.IsTrue(target.IsCreated());
            Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(source.pixelRect.width * fraction)), target.width);
            Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(source.pixelRect.height * fraction)), target.height);
        }

        [TestCase(false)] [TestCase(true)]
        public void LateInvalidMaterialRejectsTheWholeModelWithoutChangingEarlierRenderers(bool nullSlot)
        {
            using (var f = new Fixture())
            {
                var surface = f.Create(); var model = f.Model(surface); var renderers = Renderers(model);
                var unknown = f.Own(new Material(f.Library.WorldMaterial));
                renderers[1].sharedMaterials = new[] { f.Library.WaterMaterial, nullSlot ? null : unknown };
                var before = renderers.Select(r => r.sharedMaterials).ToArray();
                var marker = new MaterialPropertyBlock(); marker.SetFloat("_SurfaceProbe", 19);
                renderers[0].SetPropertyBlock(marker);
                Assert.Throws<InvalidOperationException>(() => surface.PrepareModel(model, true));
                for (int i = 0; i < renderers.Length; i++)
                {
                    CollectionAssert.AreEqual(before[i], renderers[i].sharedMaterials);
                    Assert.AreEqual(0, renderers[i].gameObject.layer);
                    Assert.AreEqual(ShadowCastingMode.Off, renderers[i].shadowCastingMode);
                    Assert.IsFalse(renderers[i].receiveShadows);
                }
                Assert.AreEqual(19, Block(renderers[0]).GetFloat("_SurfaceProbe"));
                Assert.AreEqual(0, Block(renderers[0]).GetFloat("_Transient"));
                renderers[1].sharedMaterials = new[] { f.Library.WaterMaterial, f.Library.WorldMaterial };
                surface.PrepareModel(model, true);
                Assert.IsTrue(renderers.All(r => r.gameObject.layer == NativeZone3DRenderSurface.WorldLayer));
                Assert.IsTrue(renderers.All(r => Block(r).GetFloat("_Transient") == 1));
            }
        }
        [TestCase(false, true)] [TestCase(true, true)] [TestCase(true, false)]
        public void ExistingPerMaterialOverridesReceiveTransientWithoutLosingTheirOwnData(bool transient, bool hadFlag)
        {
            using (var f = new Fixture())
            {
                var surface = f.Create(); var model = f.Model(surface); var renderer = Renderers(model)[0];
                var rendererBlock = new MaterialPropertyBlock(); rendererBlock.SetFloat("_RendererProbe", 37);
                renderer.SetPropertyBlock(rendererBlock);
                for (int slot = 0; slot < 2; slot++)
                {
                    var block = new MaterialPropertyBlock(); block.SetFloat("_SlotProbe", 10 + slot);
                    block.SetColor("_BaseColor", slot == 0 ? Color.red : Color.blue);
                    // A stale override must be replaced in both directions. Indexed
                    // blocks take precedence over the renderer-level block in Unity.
                    if (hadFlag) block.SetFloat("_Transient", transient ? 0 : 1);
                    renderer.SetPropertyBlock(block, slot);
                }
                float sourceValue = f.Library.WorldMaterial.GetFloat("_Transient");
                surface.PrepareModel(model, transient);
                for (int slot = 0; slot < 2; slot++)
                {
                    var actual = Block(renderer, slot);
                    Assert.AreEqual(transient ? 1 : 0, actual.GetFloat("_Transient"), "Indexed MPB can override actor fog policy.");
                    Assert.AreEqual(10 + slot, actual.GetFloat("_SlotProbe"));
                    Assert.AreEqual(slot == 0 ? Color.red : Color.blue, actual.GetColor("_BaseColor"));
                }
                Assert.AreEqual(37, Block(renderer).GetFloat("_RendererProbe"));
                Assert.AreEqual(sourceValue, f.Library.WorldMaterial.GetFloat("_Transient"));
            }
        }
        [Test] public void RendererPropertyDataDoesNotLeakIntoItsSiblingOrCreateIndexedOverrides()
        {
            using (var f = new Fixture())
            {
                var surface = f.Create(); var model = f.Model(surface); var r = Renderers(model);
                var first = new MaterialPropertyBlock(); first.SetFloat("_PrivateProbe", 41); r[0].SetPropertyBlock(first);
                surface.PrepareModel(model, true);
                Assert.AreEqual(41, Block(r[0]).GetFloat("_PrivateProbe"));
                Assert.AreEqual(0, Block(r[1]).GetFloat("_PrivateProbe"));
                Assert.AreEqual(1, Block(r[1]).GetFloat("_Transient"));
                Assert.IsTrue(Block(r[0], 0).isEmpty); Assert.IsTrue(Block(r[1], 1).isEmpty);
            }
        }
        [Test] public void RepeatedSourceFamilyEntriesShareExactlyOneOwnedClone()
        {
            using (var f = new Fixture())
            {
                var s = f.Create(new[] { f.Library.WorldMaterial, f.Library.WorldMaterial, f.Library.WaterMaterial });
                var own = s.MaterialFor(f.Library.WorldMaterial); var water = s.MaterialFor(f.Library.WaterMaterial);
                Assert.AreSame(own, s.MaterialFor(own)); Assert.AreNotSame(own, water);
                var model = f.Model(s); s.PrepareModel(model, false); s.PrepareModel(model, false);
                Assert.IsTrue(Renderers(model).All(r => r.sharedMaterials[0] == own && r.sharedMaterials[1] == water));
                s.Dispose(); Assert.IsTrue(own == null && water == null);
                Assert.IsTrue(f.Library.WorldMaterial != null && f.Library.WaterMaterial != null);
            }
        }
        [Test] public void EqualNamedEqualValuedFamiliesStillKeepDistinctIdentity()
        {
            using (var f = new Fixture())
            {
                var first = f.Own(new Material(f.Library.WorldMaterial)); var second = f.Own(new Material(first));
                first.name = second.name = "same authored family name";
                var s = f.Create(new[] { first, second }); var a = s.MaterialFor(first); var b = s.MaterialFor(second);
                Assert.AreNotSame(a, b); a.SetColor("_BaseColor", Color.red);
                Assert.AreEqual(second.GetColor("_BaseColor"), b.GetColor("_BaseColor"));
                Assert.AreEqual(first.GetColor("_BaseColor"), second.GetColor("_BaseColor"));
            }
        }
        [Test] public void ForeignOwnedCloneCannotBePreparedOrDisposedByAnotherSurface()
        {
            using (var f = new Fixture())
            {
                var a = f.Create(); var b = f.Create(); var foreign = a.MaterialFor(f.Library.WorldMaterial);
                var model = f.Model(b); var renderer = Renderers(model)[0]; renderer.sharedMaterial = foreign;
                Assert.Throws<InvalidOperationException>(() => b.PrepareModel(model, false));
                Assert.AreSame(foreign, renderer.sharedMaterial);
                renderer.sharedMaterials = new[] { f.Library.WorldMaterial, f.Library.WaterMaterial };
                b.PrepareModel(model, false); b.Dispose(); Assert.IsTrue(foreign != null);
                a.Sync(f.Source, true, false); Assert.IsTrue(a.IsVisible);
            }
        }
        [Test] public void DestroyedParentCanBeSyncedBeforeDisposeWithoutLeakingNonHierarchyResources()
        {
            using (var f = new Fixture())
            {
                var s = f.Create(); s.Sync(f.Source, true, false);
                var target = s.WorldCamera.targetTexture; var fog = s.FogTexture;
                var material = s.MaterialFor(f.Library.WorldMaterial);
                f.Own(f.Source.gameObject); f.Source.transform.SetParent(null, true);
                Object.DestroyImmediate(f.Root);
                Assert.DoesNotThrow(() => s.Sync(f.Source, true, false)); Hidden(s);
                s.Dispose(); Assert.IsTrue(target == null && fog == null && material == null);
                Assert.IsTrue(f.Source != null && f.Borrowed.IsCreated());
            }
        }
        [TestCase("camera")] [TestCase("content")] [TestCase("composite")]
        public void LostRequiredHierarchyMemberFailsClosedAndDisposeStillReleasesTheTarget(string member)
        {
            using (var f = new Fixture())
            {
                var s = f.Create(); s.Sync(f.Source, true, false);
                var target = s.WorldCamera.targetTexture; var fog = s.FogTexture;
                var material = s.MaterialFor(f.Library.WorldMaterial);
                var go = member == "camera" ? s.WorldCamera.gameObject : member == "content" ? s.ContentRoot.gameObject : s.CompositeRenderer.gameObject;
                Object.DestroyImmediate(go);
                Assert.DoesNotThrow(() => s.Sync(f.Source, true, false)); Hidden(s);
                s.Dispose(); Assert.IsTrue(target == null && fog == null && material == null);
                Assert.IsTrue(f.Source != null && f.Borrowed.IsCreated());
            }
        }
        [Test] public void LostRenderTextureBackingIsRecreatedAtUnchangedDimensions()
        {
            using (var f = new Fixture())
            {
                var s = f.Create(); s.Sync(f.Source, true, false); var before = s.WorldCamera.targetTexture;
                Assert.IsTrue(before.IsCreated()); int width = before.width, height = before.height;
                // A graphics-context loss leaves the Unity object alive but drops
                // its native backing. No borrowed source resource is released.
                before.Release(); Assert.IsFalse(before.IsCreated());
                s.Sync(f.Source, true, false); var after = s.WorldCamera.targetTexture;
                Assert.IsTrue(after.IsCreated()); Assert.AreEqual(width, after.width); Assert.AreEqual(height, after.height);
                Assert.AreSame(after, s.CompositeRenderer.sharedMaterial.GetTexture("_MainTex"));
                Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [TestCase("perspective")] [TestCase("tiny-viewport")] [TestCase("destroyed-source")]
        public void InvalidBorrowedSourceHidesRetainedResourcesAndValidReplacementRecovers(string fault)
        {
            using (var f = new Fixture())
            {
                var s = f.Create(); s.Sync(f.Source, true, false); var target = s.WorldCamera.targetTexture;
                if (fault == "perspective") f.Source.orthographic = false;
                else if (fault == "tiny-viewport") f.Source.pixelRect = new Rect(0, 0, .5f, .5f);
                else Object.DestroyImmediate(f.Source.gameObject);
                s.Sync(f.Source, true, false); Hidden(s); Assert.AreSame(target, s.WorldCamera.targetTexture);
                var next = f.Camera("replacement borrowed source"); next.orthographic = true; next.orthographicSize = 12.75f;
                next.targetTexture = f.Borrowed; next.aspect = 1.5f; next.transform.position = new Vector3(20, 10, -10);
                s.Sync(next, true, false); Assert.IsTrue(s.IsVisible); Assert.AreSame(target, s.WorldCamera.targetTexture);
                Assert.Less(Vector3.Distance(new Vector3(20, NativeZone3DRenderSurface.CameraAltitude, 10-35/Mathf.Tan(56*Mathf.Deg2Rad)), s.WorldCamera.transform.position),.001f);
                Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [Test] public void HiddenResizeAndDetailChangesWaitForShowWithoutDestroyingOldTargetEarly()
        {
            using (var f = new Fixture())
            {
                var s = f.Create(); s.Sync(f.Source, true, false); var target = s.WorldCamera.targetTexture;
                f.Source.rect = new Rect(0, 0, .5f, .5f);
                s.Sync(f.Source, false, true); Hidden(s); Assert.AreSame(target, s.WorldCamera.targetTexture);
                Assert.IsTrue(target.IsCreated());
                s.Sync(f.Source, true, true); TargetSize(s, f.Source, .75f); Assert.IsTrue(target == null || !target.IsCreated());
                Assert.AreEqual(LightShadows.None, s.Sun.shadows);
                s.Sync(f.Source, true, false); TargetSize(s, f.Source, 1); Assert.AreEqual(LightShadows.Soft, s.Sun.shadows);
            }
        }
        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidExposureRejectsBeforeAnyOwnedAllocation(float exposure)
        {
            using (var f = new Fixture())
            {
                var positive = f.Create(); positive.Dispose(); var before = ResourcesNow();
                Assert.Throws<InvalidOperationException>(() => f.Create(exposure: exposure));
                CollectionAssert.AreEquivalent(before, ResourcesNow()); Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [Test] public void LateNonFogMaterialRejectsBeforeCloningEarlierValidFamily()
        {
            using (var f = new Fixture())
            {
                Assert.IsFalse(f.Library.CompositeMaterial.HasProperty("_FogLight"));
                var positive = f.Create(); positive.Dispose(); var before = ResourcesNow();
                Assert.Throws<InvalidOperationException>(() => f.Create(new[] { f.Library.WorldMaterial, f.Library.CompositeMaterial }));
                CollectionAssert.AreEquivalent(before, ResourcesNow()); Assert.IsTrue(f.Library.WorldMaterial != null);
            }
        }
        [Test] public void NullParentRejectsWithoutAllocatingOrChangingBorrowedInputs()
        {
            using (var f = new Fixture())
            {
                var positive = f.Create(); positive.Dispose(); var before = ResourcesNow();
                Assert.Throws<InvalidOperationException>(() => new NativeZone3DRenderSurface(null, f.Library.Renderer,
                    f.Library.RendererIndex, f.Library.CompositeMaterial, new[] { f.Library.WorldMaterial }, 2.2f));
                CollectionAssert.AreEquivalent(before, ResourcesNow()); Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [Test] public void HiddenFogRefreshUsesLatestNativeStateOnShowWithoutDiscoveringCells()
        {
            using (var f = new Fixture())
            {
                var s = f.Create(); var fog = s.FogTexture; var zone = new Zone("surface-hidden-refresh");
                var cell = zone.GetCell(6, 7); cell.Explored = cell.IsVisible = true;
                s.UpdateFog(zone, null, false); Assert.AreEqual(255, fog.GetPixels32()[(24 - 7) * 80 + 6].a);
                s.Sync(f.Source, false, false); cell.IsVisible = false; s.UpdateFog(zone, null, false);
                s.Sync(f.Source, true, false); Assert.AreSame(fog, s.FogTexture);
                Assert.That(fog.GetPixels32()[(24 - 7) * 80 + 6].a, Is.InRange(127, 128));
                Assert.IsTrue(cell.Explored); Assert.IsFalse(cell.IsVisible); Assert.IsFalse(zone.GetCell(7, 7).Explored);
                Assert.AreSame(fog, s.MaterialFor(f.Library.WorldMaterial).GetTexture("_FogLight"));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void VillageDropsFallbackClaimsWhenItsSurfaceCannotDisplayAndRecovers(bool tinyViewport)
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var view = f.View("central-cistern"); var cell = f.Zone.GetEntityCell(view.owner);
                Assert.IsTrue(f.Presenter.PresentationVisible); Assert.IsTrue(f.Presenter.ClaimsCell(cell.X, cell.Y));
                Assert.IsTrue(f.Presenter.IsAuthoredEntity(view.owner)); Assert.Greater(f.Presenter.RenderedOwnerCount, 0);
                Rect rect = f.Source.rect;
                if (tinyViewport) f.Source.pixelRect = new Rect(0, 0, .5f, .5f); else f.Source.orthographic = false;
                f.Refresh(); Assert.IsFalse(f.Presenter.WorldCamera.enabled, "Surface refusal must actually occur before testing presenter fallback.");
                Assert.IsFalse(f.Presenter.PresentationVisible);
                Assert.IsFalse(f.Presenter.ClaimsCell(cell.X, cell.Y)); Assert.IsFalse(f.Presenter.IsAuthoredEntity(view.owner));
                Assert.AreEqual(0, f.Presenter.RenderedOwnerCount); Assert.IsFalse(f.Presenter.IsRenderedEntity(view.owner));
                f.Source.orthographic = true; f.Source.rect = rect; f.Refresh();
                Assert.IsTrue(f.Presenter.PresentationVisible); Assert.IsTrue(f.Presenter.IsRenderedEntity(view.owner));
                Assert.IsTrue(f.Presenter.ClaimsCell(cell.X, cell.Y)); Assert.AreSame(view.owner, f.View("central-cistern").owner);
            }
        }
        [Test] public void VillageInvalidSourceInterruptsQueuedActionBeforeRecovery()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                const string id = "north-guard-west";
                var actor = f.View(id).owner;
                var field = typeof(Village3DPresenter).GetField("byId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var view = ((System.Collections.IDictionary)field.GetValue(f.Presenter))[id];
                var until = view.GetType().GetField("ActionUntil");
                EntityVisualHooks.EmitAttack(actor, f.Player, f.Zone);
                Assert.Greater((float)until.GetValue(view), Time.unscaledTime, "Actual registered hook must first queue an action.");
                // Same-zone Bind synchronizes the surface without advancing
                // LateUpdate's action timer: expiry cannot make this pass vacuously.
                f.Source.orthographic = false; f.Presenter.Bind(f.Zone, f.Source);
                Assert.IsFalse(f.Presenter.WorldCamera.enabled, "Surface must actually hide on this invalid source.");
                Assert.AreEqual(0, (float)until.GetValue(view), "Failed presentation must discard stale action, like explicit hiding.");
                f.Source.orthographic = true; f.Presenter.Bind(f.Zone, f.Source);
                Assert.IsTrue(f.Presenter.IsRenderedEntity(actor)); Assert.AreEqual(0, (float)until.GetValue(view));
                EntityVisualHooks.EmitAttack(actor, f.Player, f.Zone);
                Assert.Greater((float)until.GetValue(view), Time.unscaledTime, "Recovery must retain the live hook, not disable actions permanently.");
            }
        }
        [Test] public void VillageSameZoneSourceReplacementKeepsOwnerBindingsAndBorrowedCameraResources()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var view = f.View("central-cistern"); var ownCamera = f.Presenter.WorldCamera; var oldTarget = ownCamera.targetTexture;
                var go = new GameObject("owned replacement village source"); go.transform.SetParent(f.Root.transform, false);
                var next = go.AddComponent<Camera>(); next.enabled = false; next.orthographic = true; next.orthographicSize = 10;
                next.targetTexture = f.BorrowedTarget; next.rect = new Rect(0, 0, .5f, .5f); next.aspect = 1.5f;
                next.transform.position = new Vector3(30, 10, -20);
                f.Presenter.Bind(f.Zone, next); f.Refresh();
                Assert.AreSame(ownCamera, f.Presenter.WorldCamera); Assert.IsTrue(oldTarget == null || !oldTarget.IsCreated());
                Assert.AreSame(view.owner, f.View("central-cistern").owner); Assert.AreSame(view.root, f.View("central-cistern").root);
                Assert.Less(Vector3.Distance(new Vector3(30, NativeZone3DRenderSurface.CameraAltitude, 10-35/Mathf.Tan(56*Mathf.Deg2Rad)), ownCamera.transform.position),.001f);
                Assert.AreSame(f.BorrowedTarget, next.targetTexture); Assert.AreSame(f.BorrowedTarget, f.Source.targetTexture);
                Assert.IsTrue(f.Presenter.IsRenderedEntity(view.owner));
            }
        }
        [Test] public void VillageHiddenDetailSwitchPreservesNativeOwnerIdentityAndFogWhileReusingItsWorldCamera()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var view = f.View("central-cistern"); var camera = f.Presenter.WorldCamera;
                var material = view.root.GetComponentInChildren<Renderer>(true).sharedMaterial;
                var fog = material.GetTexture("_FogLight"); int shown = f.Presenter.RenderedOwnerCount;
                var members = f.Zone.GetReadOnlyEntities().ToArray(); var at = f.Zone.GetEntityPosition(view.owner);
                f.Presenter.SetPresentationVisible(false); Village3DSettings.LowDetail = true; f.Refresh();
                Assert.IsFalse(f.Presenter.PresentationVisible); Assert.AreEqual(0, f.Presenter.RenderedOwnerCount);
                f.Presenter.SetPresentationVisible(true); f.Refresh();
                Assert.AreSame(camera, f.Presenter.WorldCamera); Assert.AreSame(fog, material.GetTexture("_FogLight"));
                Assert.AreSame(view.root, f.View("central-cistern").root); Assert.AreEqual(shown, f.Presenter.RenderedOwnerCount);
                Assert.AreEqual(at, f.Zone.GetEntityPosition(view.owner)); CollectionAssert.AreEquivalent(members, f.Zone.GetReadOnlyEntities());
                Assert.IsTrue(f.Presenter.IsRenderedEntity(view.owner));
            }
        }
    }
}
