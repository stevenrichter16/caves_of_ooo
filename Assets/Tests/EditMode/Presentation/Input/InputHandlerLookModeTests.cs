using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class InputHandlerLookModeTests
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

            foreach (var input in UnityEngine.Object.FindObjectsOfType<InputHandler>())
                UnityEngine.Object.DestroyImmediate(input.gameObject);
            foreach (var camera in UnityEngine.Object.FindObjectsOfType<Camera>())
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach (var tilemap in UnityEngine.Object.FindObjectsOfType<Tilemap>())
                UnityEngine.Object.DestroyImmediate(tilemap.gameObject);
            foreach (var grid in UnityEngine.Object.FindObjectsOfType<Grid>())
                UnityEngine.Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void EnterMoveRecenterExitLookMode_DoesNotSpendTurn_AndDoesNotMovePlayer()
        {
            var zone = new Zone("LookZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);

            var turnManager = new TurnManager();
            turnManager.AddEntity(player);
            turnManager.ProcessUntilPlayerTurn();

            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            zoneRenderer.SetZone(zone);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            var cameraFollow = cameraGo.AddComponent<CameraFollow>();
            cameraFollow.Player = player;
            cameraFollow.CurrentZone = zone;
            cameraFollow.SnapToPlayer();

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.TurnManager = turnManager;
            input.ZoneRenderer = zoneRenderer;
            input.CameraFollow = cameraFollow;

            bool entered = (bool)InvokeNonPublic(input, "EnterLookMode");
            Assert.IsTrue(entered);
            Assert.AreEqual("LookMode", GetPrivateInputState(input));
            Assert.IsTrue(turnManager.WaitingForInput);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(player));
            Assert.IsFalse(cameraFollow.HasOverrideTarget);

            InvokeNonPublic(input, "MoveLookCursor", 1, 0);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(player));
            Assert.IsFalse(cameraFollow.HasOverrideTarget);
            Assert.AreEqual("LookMode", GetPrivateInputState(input));

            InvokeNonPublic(input, "RecenterLookCursor");
            Assert.IsFalse(cameraFollow.HasOverrideTarget);

            var snapshot = (LookSnapshot)typeof(ZoneRenderer)
                .GetField("_currentLookSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(zoneRenderer);
            Assert.AreEqual(10, snapshot.X);
            Assert.AreEqual(10, snapshot.Y);

            InvokeNonPublic(input, "ExitLookMode");
            Assert.AreEqual("Normal", GetPrivateInputState(input));
            Assert.IsFalse(cameraFollow.HasOverrideTarget);
            Assert.IsTrue(turnManager.WaitingForInput);
        }

        [Test]
        public void EnterLookMode_FromNonNormalState_IsRejected()
        {
            var zone = new Zone("LookZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);

            var turnManager = new TurnManager();
            turnManager.AddEntity(player);
            turnManager.ProcessUntilPlayerTurn();

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.TurnManager = turnManager;

            SetPrivateInputState(input, "InventoryOpen");

            bool entered = (bool)InvokeNonPublic(input, "EnterLookMode");

            Assert.IsFalse(entered);
            Assert.AreEqual("InventoryOpen", GetPrivateInputState(input));
        }

        [Test]
        public void MoveLookCursor_ClampsToCurrentVisibleFrame()
        {
            var zone = new Zone("LookZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);

            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            zoneRenderer.SetZone(zone);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.transform.position = new Vector3(10.5f, Zone.Height - 10 - 0.5f, -10f);

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.ZoneRenderer = zoneRenderer;

            bool entered = (bool)InvokeNonPublic(input, "EnterLookMode");
            Assert.IsTrue(entered);

            InvokeNonPublic(input, "MoveLookCursor", 20, 0);

            var cursorState = (WorldCursorState)typeof(InputHandler)
                .GetField("_worldCursorState", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(input);

            Assert.IsTrue(zoneRenderer.TryGetVisibleZoneBounds(camera, out int minX, out int maxX, out int minY, out int maxY));
            Assert.AreEqual(maxX, cursorState.X);
            Assert.GreaterOrEqual(cursorState.Y, minY);
            Assert.LessOrEqual(cursorState.Y, maxY);
        }

        [Test]
        public void SidebarLogScrollCommand_WorksInNormalAndLookMode_WithoutSpendingTurn()
        {
            var zone = new Zone("LookZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);

            var turnManager = new TurnManager();
            turnManager.AddEntity(player);
            turnManager.ProcessUntilPlayerTurn();

            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetZone(zone);

            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f), 17f);
            Camera sidebarCamera = CreateSidebarCamera();
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);
            zoneRenderer.SetSidebarCamera(sidebarCamera);

            for (int i = 0; i < SidebarStateBuilder.SidebarLogMessageLimit; i++)
                MessageLog.Add("msg-" + i.ToString("00"));

            InvokeNonPublic(zoneRenderer, "LateUpdate");

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.TurnManager = turnManager;
            input.ZoneRenderer = zoneRenderer;

            bool scrolledOlder = (bool)InvokeNonPublic(input, "TryHandleSidebarLogScrollCommand", true);

            Assert.IsTrue(scrolledOlder);
            Assert.Greater(zoneRenderer.SidebarLogScrollOffsetRows, 0);
            Assert.IsTrue(turnManager.WaitingForInput);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(player));

            SetPrivateInputState(input, "LookMode");
            int offsetBefore = zoneRenderer.SidebarLogScrollOffsetRows;

            bool scrolledNewer = (bool)InvokeNonPublic(input, "TryHandleSidebarLogScrollCommand", false);

            Assert.IsTrue(scrolledNewer);
            Assert.Less(zoneRenderer.SidebarLogScrollOffsetRows, offsetBefore);
            Assert.IsTrue(turnManager.WaitingForInput);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(player));
        }

        [Test]
        public void SidebarLogScrollCommand_IsIgnoredOutsideNormalAndLookMode()
        {
            var zone = new Zone("LookZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);

            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetZone(zone);

            Camera camera = CreateMainCamera(new Vector3(25f, 20f, -10f), 17f);
            Camera sidebarCamera = CreateSidebarCamera();
            ConfigureSplitLayout(camera, sidebarCamera, zone, player);
            zoneRenderer.SetSidebarCamera(sidebarCamera);

            for (int i = 0; i < SidebarStateBuilder.SidebarLogMessageLimit; i++)
                MessageLog.Add("msg-" + i.ToString("00"));

            InvokeNonPublic(zoneRenderer, "LateUpdate");

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.ZoneRenderer = zoneRenderer;

            SetPrivateInputState(input, "InventoryOpen");

            bool handled = (bool)InvokeNonPublic(input, "TryHandleSidebarLogScrollCommand", true);

            Assert.IsFalse(handled);
            Assert.AreEqual(0, zoneRenderer.SidebarLogScrollOffsetRows);
        }

        private static Entity CreatePlayer()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@", ColorString = "&Y", RenderLayer = 10 });
            player.AddPart(new PhysicsPart { Solid = true });
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
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
            FieldInfo field = typeof(ZoneRenderer).GetField("_worldCursorRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field.GetValue(zoneRenderer) == null)
                InvokeNonPublic(zoneRenderer, "Awake");
        }

        private static Camera CreateMainCamera(Vector3 position, float orthographicSize)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
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

        private static object InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(instance, args);
        }

        private static void SetPrivateInputState(InputHandler inputHandler, string value)
        {
            Type enumType = typeof(InputHandler).GetNestedType("InputState", BindingFlags.NonPublic);
            object enumValue = Enum.Parse(enumType, value);
            FieldInfo field = typeof(InputHandler).GetField("_inputState", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(inputHandler, enumValue);
        }

        private static string GetPrivateInputState(InputHandler inputHandler)
        {
            FieldInfo field = typeof(InputHandler).GetField("_inputState", BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(inputHandler).ToString();
        }
    }
}
