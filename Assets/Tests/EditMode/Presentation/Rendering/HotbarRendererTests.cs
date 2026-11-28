using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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

            AssertTextAt(zoneRenderer.HotbarTilemap, 1, GameplayHotbarLayout.GridHeight - 1, "[1] Kindle");
            AssertTextAt(zoneRenderer.HotbarTilemap, 1, 4, "1");

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

        [TestCase(.02f, .03f, .73f, .15f)]
        [TestCase(.06f, .01f, .62f, .19f)]
        public void LedgerCardsStayInsideTheHotbarAndTheirCentersHitExactSlots(float x, float y, float width, float height)
        {
            var viewport = new Rect(x, y, width, height);
            var zoneRenderer = CreateLedgerHotbar(viewport, out var camera, out _);
            Canvas canvas = LedgerCanvas(zoneRenderer);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(canvas.worldCamera, Is.SameAs(camera));
            Rect strip = camera.pixelRect;
            Rect[] cards = LedgerCards(canvas).Select(card => ScreenBounds(card, camera)).ToArray();
            var header = canvas.GetComponentsInChildren<Image>().Single(image => image.name == "Hotbar Header");
            Rect headerBounds = ScreenBounds(header.rectTransform, camera);
            Assert.That(headerBounds.yMax, Is.EqualTo(strip.yMax).Within(1));

            float laneWidth = strip.width / cards.Length;
            for (int slot = 0; slot < cards.Length; slot++)
            {
                Rect card = cards[slot];
                Assert.That(card.width, Is.GreaterThan(0));
                Assert.That(card.height, Is.GreaterThan(0));
                Assert.That(card.xMin, Is.GreaterThanOrEqualTo(strip.xMin + slot * laneWidth - 1));
                Assert.That(card.xMax, Is.LessThanOrEqualTo(strip.xMin + (slot + 1) * laneWidth + 1));
                Assert.That(card.yMin, Is.GreaterThanOrEqualTo(strip.yMin - 1));
                Assert.That(card.yMax, Is.LessThanOrEqualTo(headerBounds.yMin + 1));
                if (slot > 0)
                    Assert.That(card.xMin, Is.GreaterThanOrEqualTo(cards[slot - 1].xMax - 1));
                Assert.True(zoneRenderer.TryGetHotbarSlotAtScreenPosition(card.center, out int hit));
                Assert.That(hit, Is.EqualTo(slot));
            }

            Assert.False(zoneRenderer.TryGetHotbarSlotAtScreenPosition(new Vector2(strip.xMax + 4, strip.center.y), out _));
            Assert.False(zoneRenderer.TryGetHotbarSlotAtScreenPosition(new Vector2(strip.center.x, strip.yMax + 4), out _));
            Assert.False(zoneRenderer.TryGetHotbarSlotAtScreenPosition(headerBounds.center, out _));
        }

        [Test]
        public void LedgerHotbarRendersReadyCooldownPendingAndEmptyFromTheActualAbilities()
        {
            var zoneRenderer = CreateLedgerHotbar(new Rect(0, 0, .77f, .15f), out _, out var abilities);
            var canvas = LedgerCanvas(zoneRenderer);
            RectTransform[] cards = LedgerCards(canvas);
            Assert.That(LedgerLabels(cards[0]), Does.Contain("1").And.Contain("Ready"));
            Assert.That(LedgerLabels(cards[1]), Does.Contain("2").And.Contain("CD 7"));
            Assert.That(LedgerLabels(cards[9]), Does.Contain("0").And.Contain("Empty"));
            Assert.True(cards[0].Find("Current Slot").gameObject.activeSelf);
            Assert.False(cards[1].Find("Current Slot").gameObject.activeSelf);
            Assert.False(cards[9].Find("Skill Icon").gameObject.activeSelf);
            var lungeIcon = cards[1].Find("Skill Icon").GetComponent<Image>();
            Assert.That(lungeIcon.sprite, Is.SameAs(HudSkillIconAtlas.GetTile(SkillHudIcon.Lunge).sprite));
            Assert.That(lungeIcon.color.a, Is.LessThan(1f));

            var lunge = abilities.GetAbilityBySlot(1);
            lunge.CooldownRemaining = 0;
            zoneRenderer.SetHotbarState(1, lunge);
            InvokeNonPublic(zoneRenderer, "RenderHotbar");
            Canvas.ForceUpdateCanvases();

            Assert.That(LedgerLabels(cards[1]), Does.Contain("Aiming").And.Not.Contain("CD 7"));
            Assert.That(LedgerLabels(cards[0]), Does.Contain("Ready").And.Not.Contain("Aiming"));
            Assert.False(cards[0].Find("Current Slot").gameObject.activeSelf);
            Assert.True(cards[1].Find("Current Slot").gameObject.activeSelf);
            Assert.That(lungeIcon.color.a, Is.EqualTo(1f));
            Assert.That(LedgerLabels(canvas).Any(label => label.Contains("Lunge") && label.Contains("AIM")), Is.True);
        }

        [Test]
        public void LedgerHotbarHidesAndRejectsHitsWhileItsViewportIsDisabledThenRestores()
        {
            var zoneRenderer = CreateLedgerHotbar(new Rect(0, 0, .77f, .15f), out var camera, out _);
            var canvas = LedgerCanvas(zoneRenderer);
            Vector2 point = ScreenBounds(LedgerCards(canvas)[0], camera).center;

            camera.enabled = false;
            InvokeNonPublic(zoneRenderer, "RenderHotbar");

            Assert.False(canvas.isActiveAndEnabled);
            Assert.False(zoneRenderer.TryGetHotbarSlotAtScreenPosition(point, out _));

            camera.enabled = true;
            InvokeNonPublic(zoneRenderer, "RenderHotbar");
            Canvas.ForceUpdateCanvases();

            Assert.True(LedgerCanvas(zoneRenderer).isActiveAndEnabled);
            Assert.True(zoneRenderer.TryGetHotbarSlotAtScreenPosition(point, out int hit));
            Assert.That(hit, Is.Zero);
        }

        private static ZoneRenderer CreateLedgerHotbar(Rect viewport, out Camera camera, out ActivatedAbilitiesPart abilities)
        {
            CreateZoneTilemap(out var zoneRenderer);
            var player = CreatePlayerWithAbilities();
            abilities = player.GetPart<ActivatedAbilitiesPart>();
            var lungeId = abilities.AddAbility("Lunge", "CommandLunge", "Skill", AbilityTargetingMode.DirectionLine,
                2, "LongBlades_Lunge");
            abilities.GetAbility(lungeId).CooldownRemaining = 7;
            var zone = new Zone("LedgerHotbarZone");
            zone.AddEntity(player, 10, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetZone(zone);
            camera = CreateHotbarCamera();
            GameplayHotbarLayout.ConfigureOverlayCamera(camera, viewport,
                GameplayViewportLayout.GetDisplayAspect(camera) * viewport.width / viewport.height);
            zoneRenderer.SetHotbarCamera(camera);
            // EditMode keeps the tile fallback by default. Submit through the same
            // renderer overload that ZoneRenderer selects during normal play.
            var ledgerConstructor = typeof(GameplayHotbarRenderer).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Tilemap), typeof(Tilemap), typeof(Transform) }, null);
            Assert.NotNull(ledgerConstructor, "The gameplay renderer must construct its Canvas view.");
            var ledgerRenderer = ledgerConstructor.Invoke(new object[] {
                zoneRenderer.HotbarTilemap, zoneRenderer.HotbarBgTilemap, zoneRenderer.transform
            });
            typeof(ZoneRenderer).GetField("_hotbarRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(zoneRenderer, ledgerRenderer);
            zoneRenderer.SetHotbarState(0, null);
            InvokeNonPublic(zoneRenderer, "RenderHotbar");
            Canvas.ForceUpdateCanvases();
            return zoneRenderer;
        }

        private static Canvas LedgerCanvas(ZoneRenderer renderer)
        {
            Canvas canvas = renderer.GetComponentInChildren<Canvas>();
            Assert.NotNull(canvas, "The live hotbar must submit a visible Canvas.");
            return canvas;
        }

        private static RectTransform[] LedgerCards(Canvas canvas)
        {
            var cards = canvas.GetComponentsInChildren<RectTransform>()
                .Where(card => card.name.StartsWith("Hotbar Slot ", System.StringComparison.Ordinal)).ToArray();
            Assert.That(cards.Length, Is.EqualTo(GameplayHotbarLayout.SlotCount));
            return Enumerable.Range(0, GameplayHotbarLayout.SlotCount)
                .Select(i => cards.Single(card => card.name == $"Hotbar Slot {HotbarStateBuilder.SlotToHotkey(i)}"))
                .ToArray();
        }

        private static string[] LedgerLabels(Component parent) => parent.GetComponentsInChildren<Text>()
            .Where(label => label.isActiveAndEnabled).Select(label => label.text).ToArray();

        private static Rect ScreenBounds(RectTransform rect, Camera camera)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
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
                "Pyromancy_Kindle");

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
                TileBase expected = HudPixelFont.GetTile(c);
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
