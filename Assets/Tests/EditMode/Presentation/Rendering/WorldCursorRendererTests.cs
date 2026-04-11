using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class WorldCursorRendererTests
    {
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);

            foreach (var line in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                Object.DestroyImmediate(line.gameObject);

            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                Object.DestroyImmediate(tilemap.gameObject);

            foreach (var grid in Object.FindObjectsByType<Grid>(FindObjectsSortMode.None))
                Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void CursorColor_IsRedForHostileCreature()
        {
            var zone = new Zone("CursorZone");
            var player = CreateEntity("Player", "@", "&Y", renderLayer: 10);
            player.SetTag("Player");
            player.SetTag("Creature");
            var hostile = CreateEntity("Snapjaw", "s", "&g", renderLayer: 10);
            hostile.SetTag("Creature");
            hostile.SetTag("Faction", "Snapjaws");
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(hostile, 11, 10);

            Color color = WorldCursorRenderer.GetCursorColor(zone.GetCell(11, 10), player);

            Assert.AreEqual(QudColorParser.BrightRed, color);
        }

        [Test]
        public void CursorColor_IsYellowForTakeableItem_AndCyanForContainer()
        {
            var zone = new Zone("CursorZone");
            var player = CreateEntity("Player", "@", "&Y", renderLayer: 10);
            player.SetTag("Player");
            player.SetTag("Creature");

            var item = CreateEntity("Torch", "!", "&y", renderLayer: 5);
            item.AddPart(new PhysicsPart { Takeable = true });

            var chest = CreateEntity("Chest", "C", "&Y", renderLayer: 5);
            chest.AddPart(new ContainerPart());

            zone.AddEntity(player, 10, 10);
            zone.AddEntity(item, 12, 10);
            zone.AddEntity(chest, 13, 10);

            Assert.AreEqual(QudColorParser.BrightYellow, WorldCursorRenderer.GetCursorColor(zone.GetCell(12, 10), player));
            Assert.AreEqual(QudColorParser.BrightCyan, WorldCursorRenderer.GetCursorColor(zone.GetCell(13, 10), player));
        }

        [Test]
        public void ZoneRenderer_ClearsCursorAndSidebar_OnZoneChange()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();

            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            var zone = new Zone("ZoneA");
            var otherZone = new Zone("ZoneB");
            var player = CreateEntity("Player", "@", "&Y", renderLayer: 10);
            player.SetTag("Player");
            player.SetTag("Creature");
            zone.AddEntity(player, 10, 10);

            var state = new WorldCursorState();
            state.Activate(WorldCursorMode.Look, zone, 10, 10, 10, 10);
            var snapshot = LookQueryService.BuildSnapshot(player, zone, 10, 10);

            zoneRenderer.SetZone(zone);
            zoneRenderer.SetWorldCursorState(state, player);
            zoneRenderer.SetLookSnapshot(snapshot);
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            var cursorRenderer = (WorldCursorRenderer)typeof(ZoneRenderer)
                .GetField("_worldCursorRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(zoneRenderer);
            var sidebarRenderer = (GameplaySidebarRenderer)typeof(ZoneRenderer)
                .GetField("_sidebarRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(zoneRenderer);

            Assert.IsTrue(cursorRenderer.IsVisible);
            Assert.IsTrue(sidebarRenderer.IsVisible);

            zoneRenderer.SetZone(otherZone);

            Assert.IsFalse(cursorRenderer.IsVisible);
            Assert.IsFalse(sidebarRenderer.IsVisible);
        }

        [Test]
        public void CursorOutline_UsesTilemapCellBounds_InsteadOfIntegerCenteredWorldCoords()
        {
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            var zone = new Zone("ZoneA");
            var player = CreateEntity("Player", "@", "&Y", renderLayer: 10);
            player.SetTag("Player");
            player.SetTag("Creature");
            zone.AddEntity(player, 10, 10);

            var state = new WorldCursorState();
            state.Activate(WorldCursorMode.Look, zone, 10, 10, 10, 10);

            zoneRenderer.SetZone(zone);
            zoneRenderer.SetWorldCursorState(state, player);
            InvokeNonPublic(zoneRenderer, "LateUpdate");

            Tilemap tilemap = zoneRenderer.GetComponent<Tilemap>();
            Vector3Int tileCell = new Vector3Int(10, Zone.Height - 1 - 10, 0);
            Vector3 expectedMin = tilemap.CellToWorld(tileCell);
            Vector3 expectedMax = tilemap.CellToWorld(tileCell + new Vector3Int(1, 1, 0));

            var line = Object.FindFirstObjectByType<LineRenderer>();
            Assert.IsNotNull(line);
            Assert.AreEqual(expectedMin.x, line.GetPosition(0).x, 0.0001f);
            Assert.AreEqual(expectedMin.y, line.GetPosition(0).y, 0.0001f);
            Assert.AreEqual(expectedMax.x, line.GetPosition(2).x, 0.0001f);
            Assert.AreEqual(expectedMax.y, line.GetPosition(2).y, 0.0001f);
        }

        private static Entity CreateEntity(string name, string glyph, string color, int renderLayer)
        {
            var entity = new Entity { BlueprintName = name };
            entity.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = glyph,
                ColorString = color,
                RenderLayer = renderLayer
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
            FieldInfo field = typeof(ZoneRenderer).GetField("_worldCursorRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field.GetValue(zoneRenderer) == null)
                InvokeNonPublic(zoneRenderer, "Awake");
        }

        private static void InvokeNonPublic(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(instance, null);
        }
    }
}
