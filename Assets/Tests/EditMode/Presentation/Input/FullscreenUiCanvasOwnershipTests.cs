using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // The actual R111 image supplies the first native RED. These cases separate
    // incorrect fullscreen-map ownership from the first-paused-frame claim leak.
    public class FullscreenUiCanvasOwnershipTests
    {
        [TestCase("Trade")]
        [TestCase("Faction")]
        public void FullscreenEntryTakesMainCanvasEvenWhenLegacyPopupWasAssigned(string screen)
        {
            using (var r = new Rig())
            {
                // This is the current GameBootstrap binding, not an invented input state.
                // Open must acquire the fullscreen canvas before it renders. Correcting
                // only a test's binding would hide the real bootstrap/open-path regression.
                r.Trade.Tilemap = r.Renderer.PopupFgTilemap;
                r.Faction.Tilemap = r.Renderer.PopupFgTilemap;
                r.PaintWorldGlyph();
                Assert.That(r.Main.GetTile(r.WorldPos), Is.SameAs(r.Glyph));
                r.Open(screen);
                Assert.That(r.Main.GetTile(r.WorldPos), Is.Null,
                    "an unused fullscreen row must not retain its old world glyph");
                Assert.That(r.Main.GetUsedTilesCount(), Is.GreaterThan(0),
                    "the visible header must have been drawn, not the whole UI hidden");
                Assert.That(r.Renderer.PopupFgTilemap.GetUsedTilesCount(), Is.Zero,
                    "a fullscreen page must not leave a second old popup canvas");
                Assert.That(r.Renderer.Paused, Is.True);
                Assert.That(r.Camera.rect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            }
        }

        [TestCase("Trade")]
        [TestCase("Faction")]
        [TestCase("QuestLog")]
        [TestCase("Inventory")]
        public void FirstPausedFrameDoesNotRestoreDisplacedWorldGlyphIntoBlankUiRow(string screen)
        {
            using (var r = new Rig())
            {
                r.PaintAndClaimWorld();
                r.Open(screen); // correctly main-bound here, isolating the second failure
                Assert.That(r.Main.GetTile(r.WorldPos), Is.Null,
                    "precondition: this row is empty immediately after UI.Render");
                Assert.That(r.Main.GetUsedTilesCount(), Is.GreaterThan(0));
                r.PauseFrame();
                Assert.That(r.Main.GetTile(r.WorldPos), Is.Null,
                    "releasing a sprite claim must not resurrect stale world paint after UI.Render");
                Assert.That(r.Overlay.GetTile(r.WorldPos), Is.Null,
                    "the previous world overlay must also be gone");
                r.AssertWorldUntouched();
            }
        }

        [TestCase("Trade")]
        [TestCase("Faction")]
        public void ClosingFullscreenRestoresWorldAndCameraWithoutChangingOwnersOrStock(string screen)
        {
            using (var r = new Rig())
            {
                r.PaintAndClaimWorld();
                Vector3 originalPosition = r.Camera.transform.position;
                Quaternion originalRotation = r.Camera.transform.rotation;
                float originalZoom = r.Camera.orthographicSize;
                Rect originalRect = r.Camera.rect;
                r.Open(screen);
                r.PauseFrame();
                r.Close(screen);
                r.PauseFrame(); // actual unpause LateUpdate -> MarkDirty/full native redraw
                Assert.That(r.Renderer.Paused, Is.False);
                Assert.That(r.Camera.rect, Is.EqualTo(originalRect));
                Assert.That(r.Camera.transform.position, Is.EqualTo(originalPosition));
                Assert.That(r.Camera.transform.rotation, Is.EqualTo(originalRotation));
                Assert.That(r.Camera.orthographicSize, Is.EqualTo(originalZoom).Within(0.001));
                Assert.That(r.Main.GetTile(r.WorldPos) != null || r.Overlay.GetTile(r.WorldPos) != null,
                    Is.True, "the original terrain must reappear after the UI closes");
                Assert.That(r.Main.GetTile(new Vector3Int(2, 44, 0)), Is.Null,
                    "fullscreen-only header rows must not remain outside the 25-row world");
                Assert.That(r.Renderer.PopupFgTilemap.GetUsedTilesCount(), Is.Zero);
                r.AssertWorldUntouched();
            }
        }

        [Test]
        public void OrdinarySpriteDisableStillRestoresWorldGlyphWithoutFullscreenUi()
        {
            using (var r = new Rig())
            {
                r.PaintAndClaimWorld();
                r.Environment.RenderingEnabled = false;
                r.Environment.PostRender(r.Zone, CavesOfOoo.Core.Zone.Width, CavesOfOoo.Core.Zone.Height);
                Assert.That(r.Main.GetTile(r.WorldPos), Is.SameAs(r.Glyph),
                    "the fix must not globally remove normal claim restoration");
                Assert.That(r.Overlay.GetTile(r.WorldPos), Is.Null);
                Assert.That(r.Renderer.Paused, Is.False);
                r.AssertWorldUntouched();
            }
        }

        [Test]
        public void CenteredPopupCameraKeepsWorldAndSeparateCanvas()
        {
            using (var r = new Rig())
            {
                r.PaintWorldGlyph();
                Vector3 position = r.Camera.transform.position;
                float zoom = r.Camera.orthographicSize;
                r.Follow.SetCenteredPopupOverlayView();
                var popupPos = new Vector3Int(3, 3, 0);
                r.Renderer.CenteredPopupFgTilemap.SetTile(popupPos, CP437TilesetGenerator.GetUiTile('C'));
                Assert.That(r.Main.GetTile(r.WorldPos), Is.SameAs(r.Glyph));
                Assert.That(r.Renderer.CenteredPopupFgTilemap.GetTile(popupPos), Is.Not.Null);
                Assert.That(r.Renderer.Paused, Is.False);
                Assert.That(r.PopupCamera.enabled, Is.True);
                Assert.That(r.Camera.transform.position, Is.EqualTo(position));
                Assert.That(r.Camera.orthographicSize, Is.EqualTo(zoom));
                r.AssertWorldUntouched();
            }
        }

        private sealed class Rig : IDisposable
        {
            private readonly GameObject grid, cameraHost, popupHost, uiHost;
            private readonly Action<int, int, string> oldCellDirty = ZoneRenderHooks.CellDirtyCallback;
            private readonly Action<string> oldFullDirty = ZoneRenderHooks.FullDirtyCallback;
            public readonly Zone Zone = new Zone("FullscreenUiCanvasOwnershipTest");
            public readonly Entity Player, Trader, Grass, Stock;
            public readonly Camera Camera, PopupCamera;
            public readonly CameraFollow Follow;
            public readonly ZoneRenderer Renderer;
            public readonly EnvironmentSpriteRenderer Environment;
            public readonly InputHandler Input;
            public readonly TradeUI Trade;
            public readonly FactionUI Faction;
            public readonly QuestLogUI QuestLog;
            public readonly InventoryUI Inventory;
            public readonly Tilemap Main, Overlay;
            public readonly Tile Glyph;
            public readonly Vector3Int WorldPos = new Vector3Int(75, 9, 0);

            public Rig()
            {
                cameraHost = new GameObject("Opaque UI test camera");
                cameraHost.tag = "MainCamera";
                Camera = cameraHost.AddComponent<Camera>();
                Camera.orthographic = true;
                Camera.aspect = 16f / 9f;
                Camera.transform.position = new Vector3(40, 12, -10);
                popupHost = new GameObject("Opaque UI popup camera");
                PopupCamera = popupHost.AddComponent<Camera>();
                PopupCamera.orthographic = true;
                PopupCamera.enabled = false;
                grid = new GameObject("Opaque UI test grid");
                grid.AddComponent<Grid>();
                var worldHost = new GameObject("Opaque UI world");
                worldHost.transform.SetParent(grid.transform, false);
                Main = worldHost.AddComponent<Tilemap>();
                worldHost.AddComponent<TilemapRenderer>();
                Renderer = worldHost.AddComponent<ZoneRenderer>();
                if (Renderer.PopupFgTilemap == null) Invoke(Renderer, "Awake");
                Renderer.SetPopupOverlayCamera(PopupCamera);
                Environment = (EnvironmentSpriteRenderer)typeof(ZoneRenderer)
                    .GetField("_envSpriteRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Renderer);
                Assert.That(Environment, Is.Not.Null, "real shipping sprite renderer required");
                Overlay = grid.transform.Find("EnvironmentSpriteTilemap").GetComponent<Tilemap>();
                Glyph = ScriptableObject.CreateInstance<Tile>();
                Glyph.name = "CP437_2E";
                Player = Actor("Player", "you");
                Player.SetTag("Player");
                Trader = Actor("Merchant", "test merchant");
                Grass = new Entity { BlueprintName = "Grass" };
                Grass.AddPart(new RenderPart { DisplayName = "grass", RenderString = ".", RenderLayer = 0 });
                Stock = new Entity { BlueprintName = "TradeStockControl" };
                Stock.AddPart(new RenderPart { DisplayName = "test goods", RenderString = "!" });
                Stock.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
                Stock.AddPart(new CommercePart { Value = 3 });
                Trader.GetPart<InventoryPart>().AddObject(Stock);
                Zone.AddEntity(Player, 74, 15);
                Zone.AddEntity(Trader, 74, 14);
                Zone.AddEntity(Grass, 75, 15);
                Renderer.PlayerEntity = Player;
                Renderer.SetZone(Zone);
                Renderer.FovRadius = 100;
                Follow = cameraHost.AddComponent<CameraFollow>();
                Follow.Player = Player;
                Follow.CurrentZone = Zone;
                Follow.PopupOverlayCamera = PopupCamera;
                Follow.SnapToPlayer();
                uiHost = new GameObject("Opaque UI controls");
                Trade = uiHost.AddComponent<TradeUI>(); Trade.Tilemap = Main;
                Faction = uiHost.AddComponent<FactionUI>(); Faction.Tilemap = Main;
                QuestLog = uiHost.AddComponent<QuestLogUI>(); QuestLog.Tilemap = Main;
                Inventory = uiHost.AddComponent<InventoryUI>(); Inventory.Tilemap = Main;
                Input = uiHost.AddComponent<InputHandler>();
                Input.PlayerEntity = Player; Input.CurrentZone = Zone;
                Input.ZoneRenderer = Renderer; Input.CameraFollow = Follow;
                Input.TradeUI = Trade; Input.FactionUI = Faction; Input.QuestLogUI = QuestLog; Input.InventoryUI = Inventory;
            }

            public void PaintWorldGlyph() => Main.SetTile(WorldPos, Glyph);
            public void PaintAndClaimWorld()
            {
                Zone.GetCell(75, 15).Explored = true;
                Zone.GetCell(75, 15).IsVisible = true;
                PaintWorldGlyph();
                Environment.PostRender(Zone, CavesOfOoo.Core.Zone.Width, CavesOfOoo.Core.Zone.Height);
                Assert.That(Overlay.GetTile(WorldPos), Is.Not.Null, "non-vacuous real sprite claim");
                Assert.That(Main.GetTile(WorldPos), Is.Null, "the claim displaced the native world glyph");
            }
            public void Open(string screen)
            {
                if (screen == "Trade") Invoke(Input, "OpenTrade", Trader);
                else Invoke(Input, "Open" + screen);
            }
            public void Close(string screen)
            {
                if (screen == "Trade") Trade.Close();
                else if (screen == "Faction") Faction.Close();
                else QuestLog.Close();
                Invoke(Input, "Close" + screen);
            }
            public void PauseFrame() => Invoke(Renderer, "LateUpdate");
            public void AssertWorldUntouched()
            {
                Assert.That(Zone.GetReadOnlyEntities().Count, Is.EqualTo(3));
                Assert.That(Zone.GetEntityCell(Grass), Is.SameAs(Zone.GetCell(75, 15)));
                Assert.That(Zone.GetEntityCell(Player), Is.SameAs(Zone.GetCell(74, 15)));
                Assert.That(Zone.GetEntityCell(Trader), Is.SameAs(Zone.GetCell(74, 14)));
                Assert.That(Trader.GetPart<InventoryPart>().Objects, Is.EquivalentTo(new[] { Stock }));
                Assert.That(Player.GetPart<InventoryPart>().Objects, Is.Empty);
                Assert.That(TradeSystem.GetDrams(Player), Is.Zero);
                Assert.That(TradeSystem.GetDrams(Trader), Is.Zero);
            }
            public void Dispose()
            {
                Object.DestroyImmediate(uiHost);
                Object.DestroyImmediate(grid);
                Object.DestroyImmediate(cameraHost);
                Object.DestroyImmediate(popupHost);
                Object.DestroyImmediate(Glyph);
                ZoneRenderHooks.CellDirtyCallback = oldCellDirty;
                ZoneRenderHooks.FullDirtyCallback = oldFullDirty;
            }
            private static Entity Actor(string blueprint, string name)
            {
                var actor = new Entity { BlueprintName = blueprint };
                actor.SetTag("Creature");
                actor.AddPart(new RenderPart { DisplayName = name, RenderString = "@", RenderLayer = 10 });
                actor.AddPart(new PhysicsPart { Solid = true });
                actor.AddPart(new InventoryPart());
                actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Value = 20, Min = 0, Max = 20 };
                actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Value = 10, Min = 1, Max = 100 };
                return actor;
            }
        }

        private static void Invoke(object instance, string method, params object[] args)
        {
            var info = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(instance, args);
        }
    }
}
