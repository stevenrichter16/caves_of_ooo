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
        public void Renderer_ThrownObjectProjectile_RendersWithoutError()
        {
            Tilemap tilemap = CreateFxTilemap();
            var renderer = new AsciiFxRenderer(tilemap);
            var zone = new Zone("FxZone");

            renderer.SetZone(zone);
            AsciiFxBus.EmitProjectile(
                zone,
                new[] { new Point(4, 4), new Point(5, 4), new Point(6, 4) },
                AsciiFxTheme.ThrownObject,
                trail: true,
                blocksTurnAdvance: true);

            Assert.DoesNotThrow(() => renderer.Update(0f));
            Assert.IsTrue(HasTileAtWorld(tilemap, 4, 4));
        }

        [Test]
        public void Renderer_EmptyProjectileTheme_FallsBackWithoutCrash()
        {
            Tilemap tilemap = CreateFxTilemap();
            var renderer = new AsciiFxRenderer(tilemap);
            var zone = new Zone("FxZone");

            renderer.SetZone(zone);
            AsciiFxBus.EmitProjectile(
                zone,
                new[] { new Point(8, 8), new Point(9, 8) },
                AsciiFxTheme.Earth,
                trail: true,
                blocksTurnAdvance: true);

            Assert.DoesNotThrow(() => renderer.Update(0f));
            Assert.IsTrue(HasTileAtWorld(tilemap, 8, 8));
        }

        [Test]
        public void Renderer_TracksAdvancedBlockingFx_AndClearsAfterExpiry()
        {
            Tilemap tilemap = CreateFxTilemap();
            var renderer = new AsciiFxRenderer(tilemap);
            var zone = new Zone("FxZone");
            var anchor = CreatePlayer();
            zone.AddEntity(anchor, 10, 10);

            renderer.SetZone(zone);
            AsciiFxBus.EmitChargeOrbit(zone, anchor, 1, 0.10f, AsciiFxTheme.Arcane, blocksTurnAdvance: true);
            AsciiFxBus.EmitBeam(zone, new[] { new Point(11, 10), new Point(12, 10) }, 1, 0, AsciiFxTheme.Arcane, 0.12f, blocksTurnAdvance: true);
            AsciiFxBus.EmitRingWave(zone, 10, 10, 2, 0.08f, AsciiFxTheme.Ice, blocksTurnAdvance: true);
            AsciiFxBus.EmitChainArc(zone, new[] { new Point(10, 10), new Point(12, 10), new Point(12, 12) }, AsciiFxTheme.Lightning, 0.05f, blocksTurnAdvance: true);

            renderer.Update(0f);

            Assert.AreEqual(1, renderer.ActiveChargeOrbitCount);
            Assert.AreEqual(1, renderer.ActiveBeamCount);
            Assert.AreEqual(1, renderer.ActiveRingWaveCount);
            Assert.AreEqual(1, renderer.ActiveChainArcCount);
            Assert.IsTrue(renderer.HasBlockingFx);

            renderer.Update(1f);

            Assert.AreEqual(0, renderer.ActiveChargeOrbitCount);
            Assert.AreEqual(0, renderer.ActiveBeamCount);
            Assert.AreEqual(0, renderer.ActiveRingWaveCount);
            Assert.AreEqual(0, renderer.ActiveChainArcCount);
            Assert.IsFalse(renderer.HasBlockingFx);
        }

        [Test]
        public void Renderer_ChargeOrbit_FollowsAnchorAcrossMovement()
        {
            Tilemap tilemap = CreateFxTilemap();
            var renderer = new AsciiFxRenderer(tilemap);
            var zone = new Zone("FxZone");
            var anchor = CreatePlayer();
            zone.AddEntity(anchor, 10, 10);

            renderer.SetZone(zone);
            AsciiFxBus.EmitChargeOrbit(zone, anchor, 1, 0.5f, AsciiFxTheme.Arcane, blocksTurnAdvance: true);

            renderer.Update(0f);
            Assert.Greater(CountTilesInWorldBox(tilemap, 9, 11, 9, 11), 0);

            zone.MoveEntity(anchor, 15, 10);
            renderer.Update(0.05f);

            Assert.AreEqual(0, CountTilesInWorldBox(tilemap, 9, 11, 9, 11));
            Assert.Greater(CountTilesInWorldBox(tilemap, 14, 16, 9, 11), 0);
        }

        [Test]
        public void Renderer_RingWave_ExpandsByRadius_AndChainArc_AdvancesByHop()
        {
            Tilemap tilemap = CreateFxTilemap();
            var renderer = new AsciiFxRenderer(tilemap);
            var zone = new Zone("FxZone");

            renderer.SetZone(zone);
            AsciiFxBus.EmitRingWave(zone, 10, 10, 2, 0.08f, AsciiFxTheme.Ice, blocksTurnAdvance: true);
            AsciiFxBus.EmitChainArc(zone, new[] { new Point(10, 10), new Point(12, 10), new Point(12, 12) }, AsciiFxTheme.Lightning, 0.05f, blocksTurnAdvance: true);

            renderer.Update(0f);
            Assert.IsTrue(HasTileAtWorld(tilemap, 10, 9));
            Assert.IsFalse(HasTileAtWorld(tilemap, 10, 8));
            Assert.IsTrue(HasTileAtWorld(tilemap, 11, 10));
            Assert.IsFalse(HasTileAtWorld(tilemap, 12, 11));

            renderer.Update(0.08f);
            Assert.IsFalse(HasTileAtWorld(tilemap, 10, 9));
            Assert.IsTrue(HasTileAtWorld(tilemap, 10, 8));
            Assert.IsTrue(HasTileAtWorld(tilemap, 12, 11));
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

        [Test]
        public void InputHandler_SelfCenteredAbility_ResolvesImmediatelyWithoutAwaitingDirection()
        {
            CreateZoneTilemap(out ZoneRenderer zoneRenderer);
            var zone = new Zone("FxZone");
            var player = CreatePlayer();
            var abilityPart = new TestAbilityPart();
            player.AddPart(new ActivatedAbilitiesPart());
            player.AddPart(abilityPart);
            player.GetPart<ActivatedAbilitiesPart>().AddAbility(
                "Test Nova",
                TestAbilityPart.Command,
                "Test",
                AbilityTargetingMode.SelfCentered,
                2);
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

            InvokeNonPublic(inputHandler, "TryActivateAbility", 0);

            Assert.IsTrue(abilityPart.SeenCommand);
            Assert.AreEqual("Normal", GetPrivateInputState(inputHandler));
            Assert.IsTrue(turnManager.WaitingForInput);
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

        private static void InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(instance, args);
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

        private static bool HasTileAtWorld(Tilemap tilemap, int x, int y)
        {
            return tilemap.HasTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
        }

        private static int CountTilesInWorldBox(Tilemap tilemap, int minX, int maxX, int minY, int maxY)
        {
            int count = 0;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (HasTileAtWorld(tilemap, x, y))
                        count++;
                }
            }

            return count;
        }

        private class TestAbilityPart : Part
        {
            public const string Command = "CommandTestSelfCenteredAbility";

            public bool SeenCommand;

            public override string Name => "TestAbilityPart";

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != Command)
                    return true;

                SeenCommand = true;
                e.Handled = true;
                return false;
            }
        }
    }
}
