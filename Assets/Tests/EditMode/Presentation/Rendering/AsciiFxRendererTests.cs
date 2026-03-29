using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class AsciiFxRendererTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var tilemap in UnityEngine.Object.FindObjectsOfType<Tilemap>())
                UnityEngine.Object.DestroyImmediate(tilemap.gameObject);

            foreach (var grid in UnityEngine.Object.FindObjectsOfType<Grid>())
                UnityEngine.Object.DestroyImmediate(grid.gameObject);

            foreach (var go in UnityEngine.Object.FindObjectsOfType<InputHandler>())
                UnityEngine.Object.DestroyImmediate(go.gameObject);
        }

        [Test]
        public void Renderer_ConsumesProjectileRequest_AndClearsOnZoneChange()
        {
            Tilemap tilemap = CreateFxTilemap();
            var renderer = new AsciiFxRenderer(tilemap);
            var zone = new Zone("FxZone");
            var otherZone = new Zone("OtherZone");

            renderer.SetZone(zone);
            AsciiFxBus.EmitProjectile(
                zone,
                new[] { new Point(4, 4), new Point(5, 4), new Point(6, 4) },
                AsciiFxTheme.Fire,
                trail: true,
                blocksTurnAdvance: true);

            renderer.Update(0f);

            Assert.AreEqual(1, renderer.ActiveProjectileCount);
            Assert.IsTrue(renderer.HasBlockingFx);

            renderer.SetZone(otherZone);

            Assert.AreEqual(0, renderer.ActiveProjectileCount);
            Assert.AreEqual(0, renderer.ActiveBurstCount);
            Assert.IsFalse(renderer.HasBlockingFx);
        }

        [Test]
        public void InputHandler_WaitsForBlockingFx_UntilRendererFinishes()
        {
            Tilemap zoneTilemap = CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            var zone = new Zone("FxZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);

            zoneRenderer.SetZone(zone);

            var turnManager = new TurnManager();
            turnManager.AddEntity(player);
            turnManager.ProcessUntilPlayerTurn();

            var inputGo = new GameObject("InputHandler");
            var inputHandler = inputGo.AddComponent<InputHandler>();
            inputHandler.PlayerEntity = player;
            inputHandler.CurrentZone = zone;
            inputHandler.TurnManager = turnManager;
            inputHandler.ZoneRenderer = zoneRenderer;

            AsciiFxBus.EmitProjectile(
                zone,
                new[] { new Point(11, 10), new Point(12, 10) },
                AsciiFxTheme.Fire,
                trail: true,
                blocksTurnAdvance: true);

            InvokeNonPublic(zoneRenderer, "LateUpdate");
            SetPrivateInputState(inputHandler, "WaitingForFxResolution");
            InvokeNonPublic(inputHandler, "HandleWaitingForFxResolution");

            Assert.AreEqual("WaitingForFxResolution", GetPrivateInputState(inputHandler));

            FieldInfo fxField = typeof(ZoneRenderer).GetField("_asciiFxRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            var fxRenderer = (AsciiFxRenderer)fxField.GetValue(zoneRenderer);
            fxRenderer.Update(1f);

            InvokeNonPublic(inputHandler, "HandleWaitingForFxResolution");

            Assert.AreEqual("Normal", GetPrivateInputState(inputHandler));
            Assert.IsTrue(turnManager.WaitingForInput);
            Assert.IsNotNull(zoneTilemap);
        }

        private static Tilemap CreateFxTilemap()
        {
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();

            var tilemapGo = new GameObject("FxTilemap");
            tilemapGo.transform.SetParent(gridGo.transform, false);
            return tilemapGo.AddComponent<Tilemap>();
        }

        private static Tilemap CreateZoneTilemap(out ZoneRenderer zoneRenderer)
        {
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();

            var tilemapGo = new GameObject("ZoneTilemap");
            tilemapGo.transform.SetParent(gridGo.transform, false);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();
            zoneRenderer = tilemapGo.AddComponent<ZoneRenderer>();
            FieldInfo fxField = typeof(ZoneRenderer).GetField("_asciiFxRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fxField.GetValue(zoneRenderer) == null)
                InvokeNonPublic(zoneRenderer, "Awake");
            return tilemap;
        }

        private static Entity CreatePlayer()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.Tags["Creature"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@", ColorString = "&W" });
            player.AddPart(new PhysicsPart { Solid = true });
            return player;
        }

        private static void InvokeNonPublic(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(instance, null);
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
            object value = field.GetValue(inputHandler);
            return value.ToString();
        }
    }
}
