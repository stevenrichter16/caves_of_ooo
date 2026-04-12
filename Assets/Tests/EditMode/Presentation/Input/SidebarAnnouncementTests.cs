using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class SidebarAnnouncementTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
        }

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
            foreach (var ui in Object.FindObjectsByType<AnnouncementUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
        }

        [Test]
        public void AddAnnouncement_StillOpensModal_AndAlsoFeedsSidebarLog()
        {
            CreateMainCamera();
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);

            var zone = new Zone("AnnouncementZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetZone(zone);

            var announcementHost = new GameObject("AnnouncementUI");
            var announcementUi = announcementHost.AddComponent<AnnouncementUI>();
            announcementUi.Tilemap = zoneRenderer.CenteredPopupFgTilemap;
            announcementUi.BgTilemap = zoneRenderer.CenteredPopupBgTilemap;

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.ZoneRenderer = zoneRenderer;
            input.AnnouncementUI = announcementUi;

            MessageLog.AddAnnouncement("Signal flare");

            SidebarSnapshot snapshot = SidebarStateBuilder.Build(player, zone, null);
            Assert.AreEqual("Signal flare", snapshot.LogEntriesNewestFirst[0].Text);

            bool opened = (bool)typeof(InputHandler)
                .GetMethod("TryOpenAnnouncement", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(input, null);

            Assert.IsTrue(opened);
            Assert.IsTrue(announcementUi.IsOpen);
            Assert.IsFalse(zoneRenderer.Paused);
        }

        private static void CreateMainCamera()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(20f, 20f, -10f);
        }

        private static Entity CreatePlayer()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@", ColorString = "&Y", RenderLayer = 10 });
            player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new InventoryPart());
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Value = 20, Min = 0, Max = 20 };
            player.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Value = 10, Min = 1, Max = 100 };
            player.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 1, Value = 1, Min = 1, Max = 99 };
            player.Statistics["Experience"] = new Stat { Name = "Experience", BaseValue = 0, Value = 0, Min = 0, Max = 9999 };
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
            if (typeof(ZoneRenderer).GetField("_sidebarRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(zoneRenderer) == null)
            {
                typeof(ZoneRenderer)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(zoneRenderer, null);
            }
        }
    }
}
