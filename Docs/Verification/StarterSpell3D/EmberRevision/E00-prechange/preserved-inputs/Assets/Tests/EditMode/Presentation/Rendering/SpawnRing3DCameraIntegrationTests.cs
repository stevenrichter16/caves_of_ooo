using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Camera framing behavior with an actually ready ring presenter.
    /// These are explicit camera/native-position APIs, not ordinary key input or
    /// a screenshot proof of UI compositing. No extra production API is required.</summary>
    public sealed class SpawnRing3DCameraIntegrationTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static CameraFollow Follow(SpawnRing3DIntegrationFixture f)
        {
            var follow=f.Source.gameObject.AddComponent<CameraFollow>();follow.Player=f.Player;follow.CurrentZone=f.Zone;
            // Match the existing Felling camera fixtures: isolate the dependency
            // from other editor-scene objects without depending on a private name.
            var field=typeof(CameraFollow).GetFields(Private).SingleOrDefault(p=>p.FieldType==f.Presenter.GetType());
            Assert.NotNull(field,"CameraFollow needs a cached ready-ring presenter dependency.");field.SetValue(follow,f.Presenter);
            follow.SnapToPlayer();f.Frame();return follow;
        }
        static void NativeBounds(Camera camera)
        {
            float h=camera.orthographicSize,w=h*camera.aspect;var p=camera.transform.position;
            Assert.That(h,Is.LessThanOrEqualTo(14f));
            Assert.That(p.x-w,Is.GreaterThanOrEqualTo(-.001f));Assert.That(p.x+w,Is.LessThanOrEqualTo(80.001f));
            Assert.That(p.y-h,Is.GreaterThanOrEqualTo(-1.501f));Assert.That(p.y+h,Is.LessThanOrEqualTo(26.501f));
        }
        [Test] public void ReadyRingFitsTwentyFiveRowsAndAllFourBordersWithTiltHeadroom()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var follow=Follow(f);Assert.AreEqual(14f,f.Source.orthographicSize,.0001f);
                foreach(var p in new[]{new Vector2Int(0,0),new Vector2Int(79,0),new Vector2Int(0,24),new Vector2Int(79,24)})
                {
                    Assert.IsTrue(f.Zone.MoveEntity(f.Player,p.x,p.y));follow.SnapToPlayer();f.Frame();NativeBounds(f.Source);
                    Assert.AreEqual((p.x,p.y),f.Zone.GetEntityPosition(f.Player));
                    var v=f.Source.WorldToViewportPoint(new Vector3(p.x+.5f,24.5f-p.y,0));
                    Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));
                }
            }
        }
        [Test] public void ReadyFellingRingUsesNativeFramingEvenWhenLegacySourcePresenterExists()
        {
            using(var f=new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                var source=f.Root.AddComponent<FellingScenePresenter>();source.Bind(f.Zone);
                Assert.IsTrue(source.PresentationVisible,"Positive legacy source control must be active for precedence.");
                var follow=Follow(f);typeof(CameraFollow).GetField("_scenePresenter",Private).SetValue(follow,source);
                follow.SnapToPlayer();f.Frame();Assert.AreEqual(14f,f.Source.orthographicSize,.0001f);
                Assert.AreEqual(12.5f,f.Source.transform.position.y,.0001f);NativeBounds(f.Source);
            }
        }
        [Test] public void DisablingRingRestoresOrdinaryZoomAndEnablingRestoresNativeBounds()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var follow=Follow(f);var position=f.Zone.GetEntityPosition(f.Player);Assert.AreEqual(14f,f.Source.orthographicSize,.0001f);
                Village3DSettings.Enabled=false;f.Frame();follow.SnapToPlayer();Assert.IsFalse(f.Get<bool>("PresentationVisible"));
                Assert.AreEqual(follow.TargetVisibleTileRows*.5f,f.Source.orthographicSize,.0001f);
                Village3DSettings.Enabled=true;f.Frame();follow.SnapToPlayer();Assert.AreEqual(14f,f.Source.orthographicSize,.0001f);NativeBounds(f.Source);
                Assert.AreEqual(position,f.Zone.GetEntityPosition(f.Player));
            }
        }
        [Test] public void AReadyRingElsewhereCannotChangeAnUnrelatedNativeZoneCamera()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var follow=Follow(f);var zone=new Zone("ordinary-camera-control");var actor=f.Factory.CreateEntity("Player");Assert.IsTrue(zone.AddEntity(actor,40,12));
                follow.CurrentZone=zone;follow.Player=actor;follow.SnapToPlayer();
                Assert.IsTrue(f.Get<bool>("PresentationVisible"),"A ready presenter in a different zone is the negative control.");
                Assert.AreEqual(follow.TargetVisibleTileRows*.5f,f.Source.orthographicSize,.0001f);Assert.AreEqual((40,12),zone.GetEntityPosition(actor));
            }
        }
        [Test] public void LookTargetTracksRingBordersAndClearingLookReturnsToUnmovedPlayer()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var follow=Follow(f);var original=f.Zone.GetEntityPosition(f.Player);follow.SetOverrideTargetCell(79,0);follow.SnapToPlayer();NativeBounds(f.Source);
                Assert.IsTrue(follow.HasOverrideTarget);var v=f.Source.WorldToViewportPoint(new Vector3(79.5f,24.5f,0));Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));
                Assert.AreEqual(original,f.Zone.GetEntityPosition(f.Player));follow.ClearOverrideTarget();follow.SnapToPlayer();NativeBounds(f.Source);
                v=f.Source.WorldToViewportPoint(new Vector3(original.x+.5f,24.5f-original.y,0));Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));Assert.IsFalse(follow.HasOverrideTarget);
            }
        }
        [Test] public void FullscreenInventoryThenRestoreReturnsToRingGameplayViewportAndBounds()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var follow=Follow(f);Rect map=f.Source.rect;Assert.Less(map.width,1f);follow.SetUIView(80,25);
                Assert.AreEqual(new Rect(0,0,1,1),f.Source.rect);Assert.Greater(f.Source.orthographicSize,12.5f);
                follow.RestoreGameView();f.Frame();Assert.AreEqual(map,f.Source.rect);Assert.AreEqual(14f,f.Source.orthographicSize,.0001f);NativeBounds(f.Source);
                Assert.AreSame(f.Borrowed,f.Source.targetTexture);
            }
        }
    }
}
