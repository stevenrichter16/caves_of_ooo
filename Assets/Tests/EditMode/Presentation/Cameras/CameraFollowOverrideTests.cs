using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class CameraFollowOverrideTests
    {
        private GameObject _worldGrid;

        [TearDown]
        public void TearDown()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);
            if (_worldGrid != null)
                Object.DestroyImmediate(_worldGrid);
        }

        [Test]
        public void RectangularWorld_FollowsTranslatedCellCentre_AndUsesRowSpacingForZoom()
        {
            var follow = CreateRectangularWorld(out var camera, out var tilemap);
            follow.SnapToPlayer();

            Assert.AreEqual(53.5f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(11.75f, camera.transform.position.y, 0.001f);
            Assert.AreEqual(-10f, camera.transform.position.z, 0.001f);
            Assert.AreEqual(6f, camera.orthographicSize, 0.001f, "Eight 1.5-high rows need a six-unit half height.");

            // Same entities and camera, but removing the explicit projection restores
            // the unit-cell contract rather than retaining translated coordinates.
            follow.WorldTilemap = null;
            follow.SnapToPlayer();
            Assert.AreEqual(40.5f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(12.5f, camera.transform.position.y, 0.001f);
            Assert.AreEqual(4f, camera.orthographicSize, 0.001f);
        }

        [TestCase(0, 0, false, true)]
        [TestCase(79, 24, true, false)]
        public void RectangularWorld_ClampsToTranslatedCellEdges(int x, int y, bool right, bool top)
        {
            var follow = CreateRectangularWorld(out var camera, out var tilemap, x, y);
            follow.SnapToPlayer();

            float halfWidth = camera.orthographicSize * camera.aspect;
            Assert.AreEqual(right ? 93f - halfWidth : 13f + halfWidth,
                camera.transform.position.x, 0.001f);
            Assert.AreEqual(top ? 30.5f - 6f : -7f + 6f,
                camera.transform.position.y, 0.001f);

            follow.WorldTilemap = null;
            follow.SnapToPlayer();
            halfWidth = camera.orthographicSize * camera.aspect;
            Assert.AreEqual(right ? 79.5f - halfWidth : halfWidth - 0.5f,
                camera.transform.position.x, 0.001f, "Null keeps the existing half-cell padding contract.");
            Assert.AreEqual(top ? 24.5f - 4f : 4f - 0.5f,
                camera.transform.position.y, 0.001f);
        }

        [Test]
        public void RectangularWorld_WhenEntireZoneFits_CentresProjectedExtents()
        {
            var follow = CreateRectangularWorld(out var camera, out var tilemap, 2, 2);
            follow.TargetVisibleTileRows = 100;
            follow.SnapToPlayer();

            Assert.AreEqual(75f, camera.orthographicSize, 0.001f);
            Assert.AreEqual(53f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(11.75f, camera.transform.position.y, 0.001f);
        }

        [Test]
        public void RectangularWorld_OverrideUsesProjectedCell_AndClearingRestoresPlayer()
        {
            var follow = CreateRectangularWorld(out var camera, out var tilemap);
            follow.SnapToPlayer();
            var playerPosition = camera.transform.position;

            follow.SetOverrideTargetCell(41, 12);
            Assert.AreEqual(playerPosition, camera.transform.position, "An adjacent Look cell remains inside the margin.");

            follow.SetOverrideTargetCell(70, 2);
            var target = tilemap.GetCellCenterWorld(new Vector3Int(70, Zone.Height - 2 - 1, 0));
            float innerWidth = camera.orthographicSize * camera.aspect * (1f - follow.OverrideViewportMarginFraction);
            float innerHeight = camera.orthographicSize * (1f - follow.OverrideViewportMarginFraction);
            Assert.AreEqual(target.x - innerWidth, camera.transform.position.x, 0.001f);
            Assert.AreEqual(target.y - innerHeight, camera.transform.position.y, 0.001f);

            follow.ClearOverrideTarget();
            follow.SnapToPlayer();
            Assert.AreEqual(playerPosition, camera.transform.position);
        }

        [Test]
        public void RectangularWorld_FullscreenUIKeepsSquareCoordinates_ThenRestoresWorld()
        {
            var follow = CreateRectangularWorld(out var camera, out var tilemap);
            follow.SnapToPlayer();
            var gameplayPosition = camera.transform.position;
            float gameplayZoom = camera.orthographicSize;

            follow.SetUIView(80, 45);
            Assert.AreEqual(40f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(22.5f, camera.transform.position.y, 0.001f);
            Assert.AreEqual(Mathf.Max(22.5f, 40f / camera.aspect) + 0.65f,
                camera.orthographicSize, 0.001f);

            follow.RestoreGameView();
            Assert.AreEqual(gameplayPosition, camera.transform.position);
            Assert.AreEqual(gameplayZoom, camera.orthographicSize);
        }

        [Test]
        public void RectangularWorld_ProjectionChangesAreObserved_AndFallbackZoomRemainsWorldUnits()
        {
            var follow = CreateRectangularWorld(out var camera, out var tilemap);
            follow.SnapToPlayer();
            _worldGrid.transform.localScale = new Vector3(2f, 2f, 1f);
            follow.SnapToPlayer();

            Assert.AreEqual(94f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(30.5f, camera.transform.position.y, 0.001f);
            Assert.AreEqual(12f, camera.orthographicSize, 0.001f);

            follow.TargetVisibleTileRows = 0;
            follow.ZoomSize = 5f;
            follow.SnapToPlayer();
            Assert.AreEqual(5f, camera.orthographicSize, 0.001f, "Legacy fallback is a world-unit zoom, not a row count.");
        }

        private CameraFollow CreateRectangularWorld(out Camera camera, out Tilemap tilemap, int x = 40, int y = 12)
        {
            _worldGrid = new GameObject("Rectangular Camera Test Grid", typeof(Grid));
            _worldGrid.GetComponent<Grid>().cellSize = new Vector3(1f, 1.5f, 1f);
            _worldGrid.transform.position = new Vector3(13f, -7f, 0f);
            var tiles = new GameObject("World Tiles", typeof(Tilemap));
            tiles.transform.SetParent(_worldGrid.transform, false);
            tilemap = tiles.GetComponent<Tilemap>();

            var zone = new Zone("RectangularCameraZone");
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            zone.AddEntity(player, x, y);
            var cameraGo = new GameObject("Rectangular Main Camera");
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 1.6f;
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.TargetVisibleTileRows = 8;
            follow.ReservedSidebarWidthChars = 0;
            follow.ReservedHotbarHeightRows = 0;
            follow.WorldTilemap = tilemap;
            return follow;
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
            var hotbarCamera = CreateHotbarCamera();
            var popupOverlayCamera = CreatePopupOverlayCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;

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
            var hotbarCamera = CreateHotbarCamera();
            var popupOverlayCamera = CreatePopupOverlayCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;

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
            var hotbarCamera = CreateHotbarCamera();
            var popupOverlayCamera = CreatePopupOverlayCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;
            follow.ReservedSidebarWidthChars = 34;
            follow.SidebarReferenceZoom = 20f;

            follow.SnapToPlayer();

            float playerCenterX = 30.5f;
            GameplayScreenLayout layout = GameplayViewportLayout.Measure(camera, follow.SidebarReferenceZoom, follow.ReservedSidebarWidthChars);

            Assert.AreEqual(playerCenterX, cameraGo.transform.position.x, 0.01f);
            Assert.AreEqual(layout.MapRect.x, camera.rect.x, 0.0001f);
            Assert.AreEqual(layout.MapRect.y, camera.rect.y, 0.0001f);
            Assert.AreEqual(layout.MapRect.width, camera.rect.width, 0.0001f);
            Assert.AreEqual(layout.MapRect.height, camera.rect.height, 0.0001f);
            Assert.AreEqual(layout.HotbarRect.x, hotbarCamera.rect.x, 0.0001f);
            Assert.AreEqual(layout.HotbarRect.y, hotbarCamera.rect.y, 0.0001f);
            Assert.AreEqual(layout.HotbarRect.width, hotbarCamera.rect.width, 0.0001f);
            Assert.AreEqual(layout.HotbarRect.height, hotbarCamera.rect.height, 0.0001f);
            Assert.AreEqual(layout.SidebarRect.x, sidebarCamera.rect.x, 0.0001f);
            Assert.AreEqual(layout.SidebarRect.width, sidebarCamera.rect.width, 0.0001f);
            Assert.IsTrue(sidebarCamera.enabled);
            Assert.IsTrue(hotbarCamera.enabled);
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
            var hotbarCamera = CreateHotbarCamera();
            var popupOverlayCamera = CreatePopupOverlayCamera();

            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;

            follow.SnapToPlayer();
            Assert.IsTrue(sidebarCamera.enabled);
            Assert.IsTrue(hotbarCamera.enabled);
            Assert.Less(camera.rect.width, 1f);
            Assert.IsFalse(popupOverlayCamera.enabled);

            follow.SetUIView(80, 45);
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), camera.rect);
            Assert.IsFalse(sidebarCamera.enabled);
            Assert.IsFalse(hotbarCamera.enabled);
            Assert.IsFalse(popupOverlayCamera.enabled);

            follow.RestoreGameView();
            Assert.IsTrue(sidebarCamera.enabled);
            Assert.IsTrue(hotbarCamera.enabled);
            Assert.Less(camera.rect.width, 1f);
        }

        [Test]
        public void SetCenteredPopupOverlayView_KeepsSplitLayout_AndEnablesPopupCamera()
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
            var hotbarCamera = CreateHotbarCamera();
            var popupOverlayCamera = CreatePopupOverlayCamera();

            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;

            follow.SnapToPlayer();
            Vector3 gameplayPosition = cameraGo.transform.position;
            float gameplaySize = camera.orthographicSize;
            Rect gameplayRect = camera.rect;
            Rect hotbarRect = hotbarCamera.rect;
            Rect sidebarRect = sidebarCamera.rect;

            follow.SetCenteredPopupOverlayView();

            Assert.AreEqual(gameplayRect, camera.rect);
            Assert.IsTrue(sidebarCamera.enabled);
            Assert.IsTrue(hotbarCamera.enabled);
            Assert.IsTrue(popupOverlayCamera.enabled);
            Assert.AreEqual(gameplayRect, popupOverlayCamera.rect);
            Assert.AreEqual(CameraClearFlags.Depth, popupOverlayCamera.clearFlags);
            Assert.AreEqual(CameraRenderType.Overlay, popupOverlayCamera.GetUniversalAdditionalCameraData().renderType);
            Assert.AreEqual(gameplayPosition.x, cameraGo.transform.position.x, 0.01f);
            Assert.AreEqual(gameplayPosition.y, cameraGo.transform.position.y, 0.01f);
            Assert.AreEqual(gameplaySize, camera.orthographicSize, 0.01f);
            Assert.AreEqual(hotbarRect, hotbarCamera.rect);
            Assert.AreEqual(sidebarRect, sidebarCamera.rect);

            follow.RestoreGameView();

            Assert.IsTrue(sidebarCamera.enabled);
            Assert.IsTrue(hotbarCamera.enabled);
            Assert.Less(camera.rect.width, 1f);
            Assert.IsFalse(popupOverlayCamera.enabled);
        }

        private static Camera CreateSidebarCamera()
        {
            var cameraGo = new GameObject("Sidebar Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static Camera CreateHotbarCamera()
        {
            var cameraGo = new GameObject("Hotbar Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static Camera CreatePopupOverlayCamera()
        {
            var cameraGo = new GameObject("Popup Overlay Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.enabled = false;
            camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            return camera;
        }
    }
}
