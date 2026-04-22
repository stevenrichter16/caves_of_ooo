using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class SidebarRendererTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            FactionManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();

            foreach (var input in Object.FindObjectsByType<InputHandler>(FindObjectsSortMode.None))
                Object.DestroyImmediate(input.gameObject);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                Object.DestroyImmediate(tilemap.gameObject);
            foreach (var grid in Object.FindObjectsByType<Grid>(FindObjectsSortMode.None))
                Object.DestroyImmediate(grid.gameObject);
            foreach (var ui in Object.FindObjectsByType<AnnouncementUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void ZoneRenderer_RendersSidebarOnlyOnRightEdge()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);

            MessageLog.Add("A quiet turn.");
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Tilemap sidebar = zoneRenderer.SidebarTilemap;
            SidebarCameraMetrics metrics = GameplayViewportLayout.MeasureSidebarCamera(sidebarCamera, zoneRenderer.MessageReferenceZoom, zoneRenderer.SidebarWidthChars);
            bool foundAnyTile = false;
            foreach (Vector3Int pos in sidebar.cellBounds.allPositionsWithin)
            {
                if (sidebar.GetTile(pos) == null)
                    continue;

                foundAnyTile = true;
                Assert.GreaterOrEqual(pos.x, metrics.StartCharX);
            }

            Assert.IsTrue(foundAnyTile, "Sidebar should render visible glyphs.");
        }

        [Test]
        public void ZoneRenderer_SidebarFocusSection_ReflectsLookSnapshot()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            var hostile = CreateEntity("Snapjaw", "s", "&g");
            hostile.SetTag("Creature");
            hostile.SetTag("Faction", "Snapjaws");
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(hostile, 11, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);
            zoneRenderer.SetLookSnapshot(LookQueryService.BuildSnapshot(player, zone, 11, 10));

            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "Snapjaw"));
        }

        [Test]
        public void ZoneRenderer_SidebarFlashTintsBackground_OnAnnouncement()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);

            MessageLog.AddAnnouncement("Signal flare");
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Tilemap bg = zoneRenderer.SidebarBgTilemap;
            Assert.IsNotNull(bg);
            Vector3Int sample = new Vector3Int(bg.cellBounds.xMax - 2, bg.cellBounds.yMin + 1, 0);
            Color color = bg.GetColor(sample);
            Assert.Greater(color.r, 0.04f);
        }

        [Test]
        public void ZoneRenderer_SidebarTilemaps_UseUnlitUiMaterial()
        {
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var sidebarRenderer = zoneRenderer.SidebarTilemap.GetComponent<TilemapRenderer>();
            var sidebarBgRenderer = zoneRenderer.SidebarBgTilemap.GetComponent<TilemapRenderer>();

            Assert.IsNotNull(sidebarRenderer.sharedMaterial);
            Assert.IsNotNull(sidebarBgRenderer.sharedMaterial);

            string shaderName = sidebarRenderer.sharedMaterial.shader != null
                ? sidebarRenderer.sharedMaterial.shader.name
                : string.Empty;
            string bgShaderName = sidebarBgRenderer.sharedMaterial.shader != null
                ? sidebarBgRenderer.sharedMaterial.shader.name
                : string.Empty;

            Assert.That(
                shaderName == "Universal Render Pipeline/2D/Sprite-Unlit-Default" ||
                shaderName == "Sprites/Default");
            Assert.That(
                bgShaderName == "Universal Render Pipeline/2D/Sprite-Unlit-Default" ||
                bgShaderName == "Sprites/Default");
        }

        [Test]
        public void ZoneRenderer_SidebarLog_AnchorsMessagesToBottomRows()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);

            MessageLog.Add("First");
            MessageLog.Add("Second");
            MessageLog.Add("Third");
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Tilemap sidebar = zoneRenderer.SidebarTilemap;
            SidebarCameraMetrics metrics = GameplayViewportLayout.MeasureSidebarCamera(sidebarCamera, zoneRenderer.MessageReferenceZoom, zoneRenderer.SidebarWidthChars);
            int contentX = metrics.StartCharX + 2;
            Tile expectedPrefix = CP437TilesetGenerator.GetTextTile(':');

            Assert.AreSame(expectedPrefix, sidebar.GetTile(new Vector3Int(contentX, metrics.BottomTextY, 0)));
            Assert.AreSame(expectedPrefix, sidebar.GetTile(new Vector3Int(contentX + 1, metrics.BottomTextY, 0)));
            Assert.AreSame(expectedPrefix, sidebar.GetTile(new Vector3Int(contentX, metrics.BottomTextY + 1, 0)));
            Assert.AreSame(expectedPrefix, sidebar.GetTile(new Vector3Int(contentX + 1, metrics.BottomTextY + 1, 0)));
            Assert.AreSame(expectedPrefix, sidebar.GetTile(new Vector3Int(contentX, metrics.BottomTextY + 2, 0)));
            Assert.AreSame(expectedPrefix, sidebar.GetTile(new Vector3Int(contentX + 1, metrics.BottomTextY + 2, 0)));
            Assert.IsNull(sidebar.GetTile(new Vector3Int(contentX, metrics.BottomTextY + 3, 0)));
        }

        [Test]
        public void ZoneRenderer_SidebarLogScroll_ShowsHints_AndStaysScrolledWhenNewMessagesArrive()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);

            for (int i = 0; i < SidebarStateBuilder.SidebarLogMessageLimit; i++)
                MessageLog.Add("msg-" + i.ToString("00"));

            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "LOG ^"));
            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "msg-29"));

            Assert.IsTrue(zoneRenderer.ScrollSidebarLogOlder(3));
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            int offsetBefore = zoneRenderer.SidebarLogScrollOffsetRows;
            Assert.Greater(offsetBefore, 0);
            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "LOG ^v"));

            MessageLog.Add("msg-30");
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Assert.Greater(zoneRenderer.SidebarLogScrollOffsetRows, offsetBefore);
            Assert.IsTrue(
                TilemapContainsText(zoneRenderer.SidebarTilemap, "LOG ^v") ||
                TilemapContainsText(zoneRenderer.SidebarTilemap, "LOG v"));
            Assert.IsFalse(TilemapContainsText(zoneRenderer.SidebarTilemap, "msg-30"));

            zoneRenderer.ScrollSidebarLogOlder(200);
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "LOG v"));
            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "msg-01"));

            Assert.IsTrue(zoneRenderer.ScrollSidebarLogNewer(200));
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Assert.AreEqual(0, zoneRenderer.SidebarLogScrollOffsetRows);
            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "LOG ^"));
            Assert.IsTrue(TilemapContainsText(zoneRenderer.SidebarTilemap, "msg-30"));
        }

        [Test]
        public void ScreenToZoneCell_ReturnsFalseInsideSidebarStrip()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);

            Vector3 screen = sidebarCamera.WorldToScreenPoint(new Vector3(sidebarCamera.transform.position.x, sidebarCamera.transform.position.y, 0f));

            bool hit = zoneRenderer.ScreenToZoneCell(screen, camera, out _, out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void TryGetVisibleZoneBounds_ExcludesSidebarColumns()
        {
            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f));
            Camera sidebarCamera = CreateSidebarCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetZone(zone);
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);

            Assert.IsTrue(zoneRenderer.TryGetVisibleZoneBounds(camera, out _, out int maxX, out _, out _));

            Vector3 worldRight = camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, -camera.transform.position.z));
            Tilemap world = zoneRenderer.GetComponent<Tilemap>();
            float maxCenter = world.GetCellCenterWorld(new Vector3Int(maxX, 0, 0)).x;
            float nextCenter = world.GetCellCenterWorld(new Vector3Int(maxX + 1, 0, 0)).x;

            Assert.LessOrEqual(maxCenter, worldRight.x);
            Assert.Greater(nextCenter, worldRight.x);
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

        private static Entity CreatePlayer()
        {
            var player = CreateEntity("you", "@", "&Y");
            player.BlueprintName = "Player";
            player.SetTag("Player");
            player.SetTag("Creature");
            player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new InventoryPart());
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Value = 20, Min = 0, Max = 20 };
            player.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 2, Value = 2, Min = 0, Max = 5 };
            player.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 1, Value = 1, Min = 1, Max = 99 };
            player.Statistics["Experience"] = new Stat { Name = "Experience", BaseValue = 0, Value = 0, Min = 0, Max = 9999 };
            player.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Value = 10, Min = 1, Max = 100 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Value = 100, Min = 1, Max = 200 };
            return player;
        }

        private static Entity CreateEntity(string name, string glyph, string color)
        {
            var entity = new Entity { BlueprintName = name };
            entity.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = glyph,
                ColorString = color,
                RenderLayer = 10
            });
            return entity;
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
            if (typeof(ZoneRenderer).GetField("_sidebarRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(zoneRenderer) == null)
                InvokeNonPublic(zoneRenderer, "Awake");
        }

        private static void ConfigureSplitLayout(Camera camera, Camera sidebarCamera, Zone zone, Entity player)
        {
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.SidebarCamera = sidebarCamera;
            follow.ReservedSidebarWidthChars = 34;
            follow.SidebarReferenceZoom = 20f;
            follow.SnapToPlayer();
        }

        private static void InvokeNonPublic(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(instance, null);
        }

        private static bool TilemapContainsText(Tilemap tilemap, string text)
        {
            foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (MatchesTextAt(tilemap, pos.x, pos.y, text))
                    return true;
            }

            return false;
        }

        private static bool MatchesTextAt(Tilemap tilemap, int startX, int y, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                TileBase tile = tilemap.GetTile(new Vector3Int(startX + i, y, 0));
                if (c == ' ')
                    continue;
                if (tile != CP437TilesetGenerator.GetTextTile(c))
                    return false;
            }

            return true;
        }
    }
}
