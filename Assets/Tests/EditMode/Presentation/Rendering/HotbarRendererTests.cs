using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class HotbarRendererTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var input in Object.FindObjectsByType<InputHandler>(FindObjectsSortMode.None))
                Object.DestroyImmediate(input.gameObject);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                Object.DestroyImmediate(tilemap.gameObject);
            foreach (var grid in Object.FindObjectsByType<Grid>(FindObjectsSortMode.None))
                Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void ZoneRenderer_RendersHotbarInBottomStrip_AndRejectsWorldHitsThere()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            Camera hotbarCamera = CreateHotbarCamera();
            Camera popupOverlayCamera = CreatePopupOverlayCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("HotbarZone");
            var player = CreatePlayerWithAbilities();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetHotbarCamera(hotbarCamera);
            zoneRenderer.SetPopupOverlayCamera(popupOverlayCamera);
            zoneRenderer.SetZone(zone);
            zoneRenderer.SetHotbarState(0, null);
            ConfigureSplitLayout(camera, sidebarCamera, hotbarCamera, popupOverlayCamera, zone, player);

            InvokeNonPublic(zoneRenderer, "LateUpdate");

            AssertTextAt(zoneRenderer.HotbarTilemap, 1, GameplayHotbarLayout.GridHeight - 1, "GRIMOIRES");
            AssertTextAt(zoneRenderer.HotbarTilemap, 1, 2, "[1]");

            Vector3 screen = hotbarCamera.WorldToScreenPoint(new Vector3(2f, 2f, 0f));
            Assert.IsTrue(zoneRenderer.TryGetHotbarSlotAtScreenPosition(screen, out int slot));
            Assert.AreEqual(0, slot);
            Assert.IsFalse(zoneRenderer.ScreenToZoneCell(screen, camera, out _, out _));
        }

        [Test]
        public void GameplayLayout_ReservesBottomHotbarStrip_AndPopupUsesMapRect()
        {
            var zone = new Zone("HotbarZone");
            var player = CreatePlayerWithAbilities();
            zone.AddEntity(player, 10, 10);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 16f / 9f;

            Camera sidebarCamera = CreateSidebarCamera();
            Camera hotbarCamera = CreateHotbarCamera();
            Camera popupOverlayCamera = CreatePopupOverlayCamera();
            var follow = cameraGo.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;

            follow.SnapToPlayer();
            follow.SetCenteredPopupOverlayView();

            GameplayScreenLayout layout = GameplayViewportLayout.Measure(camera, follow.SidebarReferenceZoom, follow.ReservedSidebarWidthChars, follow.ReservedHotbarHeightRows);

            Assert.Greater(layout.HotbarRect.height, 0f);
            Assert.AreEqual(layout.MapRect, camera.rect);
            Assert.AreEqual(layout.HotbarRect, hotbarCamera.rect);
            Assert.AreEqual(layout.MapRect, popupOverlayCamera.rect);
            Assert.AreEqual(CameraRenderType.Overlay, popupOverlayCamera.GetUniversalAdditionalCameraData().renderType);
        }

        private static Entity CreatePlayerWithAbilities()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@", ColorString = "&Y", RenderLayer = 10 });
            player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new InventoryPart());

            var abilities = new ActivatedAbilitiesPart();
            player.AddPart(abilities);
            abilities.AddAbility(
                "Kindle Flame",
                "CommandKindle",
                "Spell",
                AbilityTargetingMode.AdjacentCell,
                5,
                nameof(KindleMutation));

            return player;
        }

        private static void CreateZoneTilemap(out ZoneRenderer zoneRenderer)
        {
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();

            var tilemapGo = new GameObject("ZoneTilemap");
            tilemapGo.transform.SetParent(gridGo.transform, false);
            tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();
            zoneRenderer = tilemapGo.AddComponent<ZoneRenderer>();
            if (typeof(ZoneRenderer).GetField("_hotbarRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(zoneRenderer) == null)
                InvokeNonPublic(zoneRenderer, "Awake");
        }

        private static Camera CreateMainCamera(Vector3 position)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 17f;
            camera.aspect = 16f / 9f;
            camera.transform.position = position;
            return camera;
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

        private static void ConfigureSplitLayout(Camera camera, Camera sidebarCamera, Camera hotbarCamera, Camera popupOverlayCamera, Zone zone, Entity player)
        {
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.HotbarCamera = hotbarCamera;
            follow.PopupOverlayCamera = popupOverlayCamera;
            follow.ReservedSidebarWidthChars = 34;
            follow.ReservedHotbarHeightRows = GameplayHotbarLayout.GridHeight;
            follow.SidebarReferenceZoom = 20f;
            follow.SnapToPlayer();
        }

        private static void AssertTextAt(Tilemap tilemap, int startX, int y, string text)
        {
            Assert.IsNotNull(tilemap);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ')
                    continue;

                TileBase actual = tilemap.GetTile(new Vector3Int(startX + i, y, 0));
                TileBase expected = CP437TilesetGenerator.GetTextTile(c);
                Assert.NotNull(actual);
                Assert.NotNull(expected);
                Assert.AreEqual(expected.name, actual.name);
            }
        }

        private static void InvokeNonPublic(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(instance, null);
        }
    }
}
