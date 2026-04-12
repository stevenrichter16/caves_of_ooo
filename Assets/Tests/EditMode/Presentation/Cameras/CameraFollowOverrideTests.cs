using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class CameraFollowOverrideTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);
        }

        [Test]
        public void OverrideTarget_WithinViewportMargin_DoesNotRecentreCamera()
        {
            var zone = new Zone("CameraZone");
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            zone.AddEntity(player, 30, 10);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            var sidebarCamera = CreateSidebarCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;

            follow.SnapToPlayer();
            float playerX = cameraGo.transform.position.x;

            follow.SetOverrideTargetCell(40, 10);
            follow.SnapToPlayer();
            float overrideX = cameraGo.transform.position.x;

            Assert.AreEqual(playerX, overrideX, 0.01f);
        }

        [Test]
        public void OverrideTarget_BeyondViewportMargin_PansCamera_AndClearsBackToPlayer()
        {
            var zone = new Zone("CameraZone");
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            zone.AddEntity(player, 10, 10);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            var sidebarCamera = CreateSidebarCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;

            follow.SnapToPlayer();
            float playerX = cameraGo.transform.position.x;

            follow.SetOverrideTargetCell(70, 10);
            follow.SnapToPlayer();
            float overrideX = cameraGo.transform.position.x;

            follow.ClearOverrideTarget();
            follow.SnapToPlayer();
            float restoredX = cameraGo.transform.position.x;

            Assert.Greater(overrideX, playerX);
            Assert.AreEqual(playerX, restoredX, 0.01f);
        }

        [Test]
        public void SnapToPlayer_UsesSplitRects_WithoutBiasingCameraPosition()
        {
            var zone = new Zone("CameraZone");
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            zone.AddEntity(player, 30, 10);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 16f / 9f;
            var sidebarCamera = CreateSidebarCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.ReservedSidebarWidthChars = 34;
            follow.SidebarReferenceZoom = 20f;

            follow.SnapToPlayer();

            float playerCenterX = 30.5f;
            GameplayScreenLayout layout = GameplayViewportLayout.Measure(camera, follow.SidebarReferenceZoom, follow.ReservedSidebarWidthChars);

            Assert.AreEqual(playerCenterX, cameraGo.transform.position.x, 0.01f);
            Assert.AreEqual(layout.GameplayRect.x, camera.rect.x, 0.0001f);
            Assert.AreEqual(layout.GameplayRect.width, camera.rect.width, 0.0001f);
            Assert.AreEqual(layout.SidebarRect.x, sidebarCamera.rect.x, 0.0001f);
            Assert.AreEqual(layout.SidebarRect.width, sidebarCamera.rect.width, 0.0001f);
            Assert.IsTrue(sidebarCamera.enabled);
        }

        [Test]
        public void SetUIView_DisablesSidebarCamera_AndRestoreGameView_ReenablesSplitLayout()
        {
            var zone = new Zone("CameraZone");
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            zone.AddEntity(player, 20, 10);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 16f / 9f;
            var sidebarCamera = CreateSidebarCamera();

            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;

            follow.SnapToPlayer();
            Assert.IsTrue(sidebarCamera.enabled);
            Assert.Less(camera.rect.width, 1f);

            follow.SetUIView(80, 45);
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), camera.rect);
            Assert.IsFalse(sidebarCamera.enabled);

            follow.RestoreGameView();
            Assert.IsTrue(sidebarCamera.enabled);
            Assert.Less(camera.rect.width, 1f);
        }

        private static Camera CreateSidebarCamera()
        {
            var cameraGo = new GameObject("Sidebar Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }
    }
}
