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
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;

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
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;

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
        public void SnapToPlayer_BiasesCameraLeftViewport_AwayFromRightSidebar()
        {
            var zone = new Zone("CameraZone");
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            zone.AddEntity(player, 30, 10);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.ReservedSidebarWidthChars = 34;
            follow.SidebarReferenceZoom = 20f;

            follow.SnapToPlayer();

            float playerCenterX = 30.5f;
            Assert.Greater(cameraGo.transform.position.x, playerCenterX);
        }
    }
}
