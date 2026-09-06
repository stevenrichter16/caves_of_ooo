using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public class GameAuditCameraCleanupAdversarialTests : CameraCleanupFixture
    {
        [TestCase("null")] [TestCase("follow_removed")] [TestCase("inactive")] [TestCase("camera_disabled")]
        public void MissingOrInactivePresentationParticipantsStillAllowCancellation(string state)
        {
            var handle = SeedWork();
            if (state == "null") Write(Renderer, "_mainCamera", null); // Ordinary-null control, not the destroyed-wrapper proof.
            if (state == "follow_removed") Object.DestroyImmediate(Follow);
            if (state == "inactive") CameraObject.SetActive(false);
            if (state == "camera_disabled") Camera.enabled = false;
            Renderer.CancelWorldFx(); Cleared(handle);
            if (state == "inactive" || state == "camera_disabled") ShakeCleared();
        }
        [TestCase("zone")] [TestCase("hide")] [TestCase("off")] [TestCase("sprites")]
        public void RuntimeCancellationCallersReleaseActualWorkAfterCameraDestruction(string route)
        {
            var handle = SeedWork(); DestroyCamera("component");
            if (route == "zone") Renderer.SetZone(new Zone("OtherCameraZone"));
            if (route == "hide") World.Update(0, true, false);
            if (route == "off") { SpellFxSettings.Mode = SpellFxMode.Off; World.Update(0); }
            if (route == "sprites") World.Update(0, false);
            Cleared(handle); if (route == "zone") Assert.AreEqual("OtherCameraZone", Renderer.CurrentZone.ZoneID);
        }
        [Test] public void HardTimeoutClearsDelayedLegacyAndPendingSpellWorkWithoutCamera()
        {
            AsciiFxBus.EmitBeam(Zone, new[] { new Point(4, 4) }, 1, 0, AsciiFxTheme.Fire, 100f, true, 100f);
            World.Update(0); Assert.IsTrue(World.HasBlockingFx); Assert.Greater(Ascii.ActiveBeamCount, 0);
            QueueBoth(); DestroyCamera("object"); World.Update(WorldFxPlayback.HardTimeoutSeconds + .1f);
            Cleared(); Assert.AreEqual(0, Ascii.ActiveBeamCount);
        }
        [TestCase(false)] [TestCase(true)] public void IdleAndQueuedOnlyCancellationDoNotNeedALiveCamera(bool queued)
        {
            if (queued) QueueBoth(); DestroyCamera("component"); Renderer.CancelWorldFx(); Cleared();
        }
        [TestCase("cancel_twice")] [TestCase("disable_reenable")] [TestCase("dispose_cancel")]
        public void RepeatedLifetimeOperationsRemainSafeAndEffective(string route)
        {
            var first = SeedWork(); DestroyCamera("component");
            if (route == "dispose_cancel") { World.Dispose(); Cleared(first); QueueBoth(); World.CancelAll(); World.Dispose(); Cleared(); return; }
            if (route == "disable_reenable") Lifecycle("OnDisable"); else Renderer.CancelWorldFx(); Cleared(first);
            // EditMode calls lifecycle bodies explicitly; native dispatch tests the enabled transition.
            var second = World.Play(Sequence()); Assert.AreEqual(WorldFxPlaybackState.Playing, second.State); QueueBoth();
            if (route == "disable_reenable") Lifecycle("OnDisable"); else Renderer.CancelWorldFx(); Cleared(second);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void DestructionCompletesOwnedMaterialAndHookCleanupWithoutTakingForeignResources(bool cameraDestroyed, bool foreignHooks)
        {
            var handle = SeedWork(); var shader = Shader.Find("Sprites/Default"); Assert.NotNull(shader);
            var sidebar = Read<Material>(Renderer, "_sidebarUiMaterial"); var hotbar = Read<Material>(Renderer, "_hotbarUiMaterial"); var popup = Read<Material>(Renderer, "_popupOverlayUiMaterial");
            Assert.IsNotNull(sidebar); Assert.IsNotNull(hotbar); Assert.IsNotNull(popup);
            var unrelated = new Material(shader);
            Action<int, int, string> cell = (x, y, source) => { }; Action<string> full = source => { };
            if (foreignHooks) { ZoneRenderHooks.CellDirtyCallback = cell; ZoneRenderHooks.FullDirtyCallback = full; }
            else { Assert.AreSame(Renderer, ZoneRenderHooks.CellDirtyCallback.Target); Assert.AreSame(Renderer, ZoneRenderHooks.FullDirtyCallback.Target); }
            if (cameraDestroyed) DestroyCamera("component");
            try
            {
                Lifecycle("OnDestroy"); Cleared(handle); Assert.IsTrue(sidebar == null); Assert.IsTrue(hotbar == null); Assert.IsTrue(popup == null); Assert.IsFalse(unrelated == null);
                Assert.IsNull(Read<Material>(Renderer, "_sidebarUiMaterial")); Assert.IsNull(Read<Material>(Renderer, "_hotbarUiMaterial")); Assert.IsNull(Read<Material>(Renderer, "_popupOverlayUiMaterial"));
                if (foreignHooks) { Assert.AreSame(cell, ZoneRenderHooks.CellDirtyCallback); Assert.AreSame(full, ZoneRenderHooks.FullDirtyCallback); }
                else { Assert.IsNull(ZoneRenderHooks.CellDirtyCallback); Assert.IsNull(ZoneRenderHooks.FullDirtyCallback); }
            }
            finally { if (sidebar != null) Object.DestroyImmediate(sidebar); if (hotbar != null) Object.DestroyImmediate(hotbar); if (popup != null) Object.DestroyImmediate(popup); Object.DestroyImmediate(unrelated); }
        }
        [TestCase(false)] [TestCase(true)] public void VisibleAccentedCastKeepsPlayingWithoutACameraInsteadOfBecomingCaughtFailure(bool destroyed)
        {
            if (destroyed) DestroyCamera("component"); var handle = World.Play(Sequence(2));
            Assert.AreEqual(WorldFxPlaybackState.Playing, handle.State); Assert.Greater(World.ActiveSequenceCount, 0); Assert.IsTrue(World.HasBlockingFx);
            if (!destroyed) { Assert.Greater(Read<float>(Follow, "_shakeIntensity"), 0); Assert.Greater(Read<float>(Follow, "_shakeTimeRemaining"), 0); }
            LogAssert.NoUnexpectedReceived(); Renderer.CancelWorldFx(); Cleared(handle); if (!destroyed) ShakeCleared();
        }
    }
}
