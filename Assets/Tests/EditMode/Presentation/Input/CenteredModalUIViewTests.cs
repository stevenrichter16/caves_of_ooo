using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class CenteredModalUIViewTests
    {
        private sealed class TestRig
        {
            public Camera MainCamera;
            public Camera SidebarCamera;
            public Camera HotbarCamera;
            public Camera PopupOverlayCamera;
            public CameraFollow CameraFollow;
            public ZoneRenderer ZoneRenderer;
            public InputHandler InputHandler;
            public AnnouncementUI AnnouncementUI;
            public PickupUI PickupUI;
            public ContainerPickerUI ContainerPickerUI;
            public DialogueUI DialogueUI;
            public TradeUI TradeUI;
            public Zone Zone;
            public Entity Player;
        }

        private readonly struct CameraSnapshot
        {
            public CameraSnapshot(Vector3 position, float orthographicSize)
            {
                Position = position;
                OrthographicSize = orthographicSize;
            }

            public Vector3 Position { get; }
            public float OrthographicSize { get; }
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            ResetConversationState();
        }

        [TearDown]
        public void TearDown()
        {
            MessageLog.Clear();
            ResetConversationState();

            foreach (var input in Object.FindObjectsByType<InputHandler>(FindObjectsSortMode.None))
                Object.DestroyImmediate(input.gameObject);
            foreach (var ui in Object.FindObjectsByType<AnnouncementUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
            foreach (var ui in Object.FindObjectsByType<PickupUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
            foreach (var ui in Object.FindObjectsByType<ContainerPickerUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
            foreach (var ui in Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
            foreach (var ui in Object.FindObjectsByType<TradeUI>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ui.gameObject);
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                Object.DestroyImmediate(tilemap.gameObject);
            foreach (var grid in Object.FindObjectsByType<Grid>(FindObjectsSortMode.None))
                Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void AnnouncementQueue_UsesPopupOverlayView_UntilQueueIsExhausted()
        {
            var rig = CreateRig();
            CameraSnapshot gameplayView = CaptureGameplayView(rig);

            MessageLog.AddAnnouncement("You study the grimoire and learn a rite.");
            MessageLog.AddAnnouncement("Quest updated.");

            bool opened = InvokePrivate<bool>(rig.InputHandler, "TryOpenAnnouncement");

            Assert.IsTrue(opened);
            Assert.IsTrue(rig.AnnouncementUI.IsOpen);
            AssertCenteredPopupOverlayView(rig, gameplayView);
            AssertPopupFitsOverlayGrid(rig.AnnouncementUI, "_worldOriginX", "_worldTopY", 56, GetPrivateInt(rig.AnnouncementUI, "_popupH"));
            AssertPopupBackgroundPresent(rig.ZoneRenderer.CenteredPopupBgTilemap, rig.AnnouncementUI, "_worldOriginX", "_worldTopY");

            rig.AnnouncementUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseAnnouncement");

            Assert.IsTrue(rig.AnnouncementUI.IsOpen);
            AssertCenteredPopupOverlayView(rig, gameplayView);

            rig.AnnouncementUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseAnnouncement");

            AssertSplitGameplayView(rig);
        }

        [Test]
        public void PickupAndContainerPicker_UsePopupOverlayView_AndRestoreAfterClose()
        {
            var rig = CreateRig();
            CameraSnapshot gameplayView = CaptureGameplayView(rig);

            var dagger = CreateItem("Dagger", '/', "&W");
            var grimoire = CreateItem("Kindle Flame Grimoire", '?', "&Y");
            InvokePrivate<object>(rig.InputHandler, "OpenPickup", new List<Entity> { dagger, grimoire });

            Assert.IsTrue(rig.PickupUI.IsOpen);
            AssertCenteredPopupOverlayView(rig, gameplayView);
            AssertPopupFitsOverlayGrid(rig.PickupUI, "_worldOriginX", "_worldTopY", 46, GetPrivateInt(rig.PickupUI, "_popupH"));
            AssertPopupBackgroundPresent(rig.ZoneRenderer.CenteredPopupBgTilemap, rig.PickupUI, "_worldOriginX", "_worldTopY");

            rig.PickupUI.Close();
            InvokePrivate<object>(rig.InputHandler, "ClosePickup");

            AssertSplitGameplayView(rig);

            var chest = CreateContainer("Chest");
            var satchel = CreateContainer("Satchel");
            InvokePrivate<object>(rig.InputHandler, "OpenContainerPicker", new List<Entity> { chest, satchel });

            AssertCenteredPopupOverlayView(rig, gameplayView);
            AssertPopupFitsOverlayGrid(rig.ContainerPickerUI, "_worldOriginX", "_worldTopY", 54, GetPrivateInt(rig.ContainerPickerUI, "_popupH"));
            AssertPopupBackgroundPresent(rig.ZoneRenderer.CenteredPopupBgTilemap, rig.ContainerPickerUI, "_worldOriginX", "_worldTopY");

            InvokePrivate<object>(rig.ContainerPickerUI, "Cancel");
            InvokePrivate<object>(rig.InputHandler, "CloseContainerPicker", false);

            AssertSplitGameplayView(rig);
        }

        [Test]
        public void DialogueAndAttackConfirmation_StayInPopupOverlayView_AndRestoreAfterDecline()
        {
            var rig = CreateRig();
            CameraSnapshot gameplayView = CaptureGameplayView(rig);
            var speaker = CreateNpc("Marrow Scribe");
            PrepareConversation(speaker, rig.Player, "The glyphs are old, but they still burn.");

            InvokePrivate<object>(rig.InputHandler, "OpenDialogue");

            Assert.IsTrue(rig.DialogueUI.IsOpen);
            AssertCenteredPopupOverlayView(rig, gameplayView);
            AssertPopupFitsOverlayGrid(
                rig.DialogueUI,
                "_worldOriginX",
                "_worldTopY",
                GetPrivateInt(rig.DialogueUI, "_popupW"),
                GetPrivateInt(rig.DialogueUI, "_popupH"));

            ConversationManager.PendingAttackTarget = speaker;
            rig.DialogueUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseDialogue");

            AssertCenteredPopupOverlayView(rig, gameplayView);
            AssertPopupFitsOverlayGrid(
                rig.InputHandler,
                "_confirmOriginX",
                "_confirmTopY",
                GetPrivateInt(rig.InputHandler, "_confirmW"),
                GetPrivateInt(rig.InputHandler, "_confirmH"));

            InvokePrivate<object>(rig.InputHandler, "ResolveAttackConfirmation", false);

            AssertSplitGameplayView(rig);
        }

        [Test]
        public void DialogueTransitionsToTradeWithoutRestoringGameplayView()
        {
            var rig = CreateRig();
            CameraSnapshot gameplayView = CaptureGameplayView(rig);
            var trader = CreateNpc("Ward Merchant");
            trader.AddPart(new InventoryPart());

            PrepareConversation(trader, rig.Player, "Care to barter?");
            InvokePrivate<object>(rig.InputHandler, "OpenDialogue");

            ConversationManager.PendingTradePartner = trader;
            rig.DialogueUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseDialogue");

            Assert.IsTrue(rig.TradeUI.IsOpen);
            AssertFullscreenUIView(rig);
            Assert.That(rig.MainCamera.transform.position, Is.Not.EqualTo(gameplayView.Position));
            Assert.IsFalse(rig.PopupOverlayCamera.enabled);

            rig.TradeUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseTrade");

            AssertSplitGameplayView(rig);
        }

        [Test]
        public void DialogueTransitionsToAnnouncementWithoutLeavingPopupOverlayView()
        {
            var rig = CreateRig();
            CameraSnapshot gameplayView = CaptureGameplayView(rig);
            var speaker = CreateNpc("Lantern Keeper");
            PrepareConversation(speaker, rig.Player, "The ward flame gutters.");
            InvokePrivate<object>(rig.InputHandler, "OpenDialogue");

            MessageLog.AddAnnouncement("You restore the ward lantern.");
            rig.DialogueUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseDialogue");

            Assert.IsTrue(rig.AnnouncementUI.IsOpen);
            AssertCenteredPopupOverlayView(rig, gameplayView);

            rig.AnnouncementUI.Close();
            InvokePrivate<object>(rig.InputHandler, "CloseAnnouncement");

            AssertSplitGameplayView(rig);
        }

        [Test]
        public void CenteredPopupTilemaps_RenderOnPopupOverlayLayer_WithUnlitMaterial()
        {
            var rig = CreateRig();

            var fgRenderer = rig.ZoneRenderer.CenteredPopupFgTilemap.GetComponent<TilemapRenderer>();
            var bgRenderer = rig.ZoneRenderer.CenteredPopupBgTilemap.GetComponent<TilemapRenderer>();

            Assert.AreEqual(10, rig.ZoneRenderer.CenteredPopupFgTilemap.gameObject.layer);
            Assert.AreEqual(10, rig.ZoneRenderer.CenteredPopupBgTilemap.gameObject.layer);
            Assert.NotNull(fgRenderer.sharedMaterial);
            Assert.NotNull(bgRenderer.sharedMaterial);

            string fgShader = fgRenderer.sharedMaterial.shader != null ? fgRenderer.sharedMaterial.shader.name : string.Empty;
            string bgShader = bgRenderer.sharedMaterial.shader != null ? bgRenderer.sharedMaterial.shader.name : string.Empty;

            Assert.IsTrue(
                fgShader == "Universal Render Pipeline/2D/Sprite-Unlit-Default" ||
                fgShader == "Sprites/Default");
            Assert.IsTrue(
                bgShader == "Universal Render Pipeline/2D/Sprite-Unlit-Default" ||
                bgShader == "Sprites/Default");
        }

        private static TestRig CreateRig()
        {
            var mainCameraGo = new GameObject("Main Camera");
            mainCameraGo.tag = "MainCamera";
            var mainCamera = mainCameraGo.AddComponent<Camera>();
            mainCamera.orthographic = true;
            mainCamera.aspect = 16f / 9f;
            mainCamera.transform.position = new Vector3(20f, 20f, -10f);

            var sidebarCameraGo = new GameObject("Sidebar Camera");
            var sidebarCamera = sidebarCameraGo.AddComponent<Camera>();
            sidebarCamera.orthographic = true;
            sidebarCamera.transform.position = new Vector3(0f, 0f, -10f);

            var hotbarCameraGo = new GameObject("Hotbar Camera");
            var hotbarCamera = hotbarCameraGo.AddComponent<Camera>();
            hotbarCamera.orthographic = true;
            hotbarCamera.transform.position = new Vector3(0f, 0f, -10f);

            var popupOverlayCameraGo = new GameObject("Popup Overlay Camera");
            var popupOverlayCamera = popupOverlayCameraGo.AddComponent<Camera>();
            popupOverlayCamera.orthographic = true;
            popupOverlayCamera.transform.position = new Vector3(0f, 0f, -10f);
            popupOverlayCamera.enabled = false;

            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();

            var tilemapGo = new GameObject("ZoneTilemap");
            tilemapGo.transform.SetParent(gridGo.transform, false);
            tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();
            var zoneRenderer = tilemapGo.AddComponent<ZoneRenderer>();
            if (typeof(ZoneRenderer).GetField("_sidebarRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(zoneRenderer) == null)
            {
                typeof(ZoneRenderer)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(zoneRenderer, null);
            }
            zoneRenderer.SetSidebarCamera(sidebarCamera);
            zoneRenderer.SetHotbarCamera(hotbarCamera);
            zoneRenderer.SetPopupOverlayCamera(popupOverlayCamera);

            var zone = new Zone("ModalPopupZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 20, 10);
            zoneRenderer.PlayerEntity = player;
            zoneRenderer.SetZone(zone);

            var cameraFollow = mainCameraGo.AddComponent<CameraFollow>();
            cameraFollow.Player = player;
            cameraFollow.CurrentZone = zone;
            cameraFollow.SidebarCamera = sidebarCamera;
            cameraFollow.HotbarCamera = hotbarCamera;
            cameraFollow.PopupOverlayCamera = popupOverlayCamera;
            cameraFollow.SnapToPlayer();

            var announcementHost = new GameObject("AnnouncementUI");
            var announcementUI = announcementHost.AddComponent<AnnouncementUI>();
            announcementUI.Tilemap = zoneRenderer.CenteredPopupFgTilemap;
            announcementUI.BgTilemap = zoneRenderer.CenteredPopupBgTilemap;
            announcementUI.PopupCamera = popupOverlayCamera;

            var pickupHost = new GameObject("PickupUI");
            var pickupUI = pickupHost.AddComponent<PickupUI>();
            pickupUI.Tilemap = zoneRenderer.CenteredPopupFgTilemap;
            pickupUI.BgTilemap = zoneRenderer.CenteredPopupBgTilemap;
            pickupUI.PopupCamera = popupOverlayCamera;
            pickupUI.PlayerEntity = player;
            pickupUI.CurrentZone = zone;

            var containerHost = new GameObject("ContainerPickerUI");
            var containerPickerUI = containerHost.AddComponent<ContainerPickerUI>();
            containerPickerUI.Tilemap = zoneRenderer.CenteredPopupFgTilemap;
            containerPickerUI.BgTilemap = zoneRenderer.CenteredPopupBgTilemap;
            containerPickerUI.PopupCamera = popupOverlayCamera;

            var dialogueHost = new GameObject("DialogueUI");
            var dialogueUI = dialogueHost.AddComponent<DialogueUI>();
            dialogueUI.Tilemap = zoneRenderer.CenteredPopupFgTilemap;
            dialogueUI.BgTilemap = zoneRenderer.CenteredPopupBgTilemap;
            dialogueUI.PopupCamera = popupOverlayCamera;
            dialogueUI.PlayerEntity = player;
            dialogueUI.CurrentZone = zone;

            var tradeHost = new GameObject("TradeUI");
            var tradeUI = tradeHost.AddComponent<TradeUI>();
            tradeUI.Tilemap = zoneRenderer.PopupFgTilemap;
            tradeUI.PlayerEntity = player;
            tradeUI.CurrentZone = zone;

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.ZoneRenderer = zoneRenderer;
            input.CameraFollow = cameraFollow;
            input.AnnouncementUI = announcementUI;
            input.PickupUI = pickupUI;
            input.ContainerPickerUI = containerPickerUI;
            input.DialogueUI = dialogueUI;
            input.TradeUI = tradeUI;

            return new TestRig
            {
                MainCamera = mainCamera,
                SidebarCamera = sidebarCamera,
                HotbarCamera = hotbarCamera,
                PopupOverlayCamera = popupOverlayCamera,
                CameraFollow = cameraFollow,
                ZoneRenderer = zoneRenderer,
                InputHandler = input,
                AnnouncementUI = announcementUI,
                PickupUI = pickupUI,
                ContainerPickerUI = containerPickerUI,
                DialogueUI = dialogueUI,
                TradeUI = tradeUI,
                Zone = zone,
                Player = player
            };
        }

        private static CameraSnapshot CaptureGameplayView(TestRig rig)
        {
            return new CameraSnapshot(rig.MainCamera.transform.position, rig.MainCamera.orthographicSize);
        }

        private static void AssertFullscreenUIView(TestRig rig)
        {
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), rig.MainCamera.rect);
            Assert.IsFalse(rig.SidebarCamera.enabled);
            Assert.IsFalse(rig.HotbarCamera.enabled);
            Assert.IsFalse(rig.PopupOverlayCamera.enabled);
            Assert.IsTrue(rig.ZoneRenderer.Paused);
        }

        private static void AssertCenteredPopupOverlayView(TestRig rig, CameraSnapshot expectedView)
        {
            Assert.Less(rig.MainCamera.rect.width, 1f);
            Assert.IsTrue(rig.SidebarCamera.enabled);
            Assert.IsTrue(rig.HotbarCamera.enabled);
            Assert.IsTrue(rig.PopupOverlayCamera.enabled);
            Assert.AreEqual(rig.MainCamera.rect, rig.PopupOverlayCamera.rect);
            Assert.IsFalse(rig.ZoneRenderer.Paused);
            Assert.AreEqual(expectedView.Position.x, rig.MainCamera.transform.position.x, 0.01f);
            Assert.AreEqual(expectedView.Position.y, rig.MainCamera.transform.position.y, 0.01f);
            Assert.AreEqual(expectedView.OrthographicSize, rig.MainCamera.orthographicSize, 0.01f);
        }

        private static void AssertSplitGameplayView(TestRig rig)
        {
            Assert.Less(rig.MainCamera.rect.width, 1f);
            Assert.IsTrue(rig.SidebarCamera.enabled);
            Assert.IsTrue(rig.HotbarCamera.enabled);
            Assert.IsFalse(rig.PopupOverlayCamera.enabled);
            Assert.IsFalse(rig.ZoneRenderer.Paused);
        }

        private static void AssertPopupFitsOverlayGrid(object instance, string originFieldName, string topFieldName, int width, int height)
        {
            int originX = GetPrivateInt(instance, originFieldName);
            int topY = GetPrivateInt(instance, topFieldName);
            int bottomY = topY - height + 1;

            Assert.GreaterOrEqual(originX, 0);
            Assert.LessOrEqual(originX + width, CenteredPopupLayout.GridWidth);
            Assert.GreaterOrEqual(bottomY, 0);
            Assert.LessOrEqual(topY, CenteredPopupLayout.GridHeight - 1);
        }

        private static void AssertPopupBackgroundPresent(Tilemap bgTilemap, object instance, string originFieldName, string topFieldName)
        {
            int originX = GetPrivateInt(instance, originFieldName);
            int topY = GetPrivateInt(instance, topFieldName);
            Assert.NotNull(bgTilemap.GetTile(new Vector3Int(originX, topY, 0)));
        }

        private static int GetPrivateInt(object instance, string fieldName)
        {
            return (int)instance.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(instance);
        }

        private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
        {
            object result = instance.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, args);
            return result == null ? default(T) : (T)result;
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

        private static Entity CreateNpc(string name)
        {
            var npc = new Entity { BlueprintName = name.Replace(" ", string.Empty) };
            npc.SetTag("Creature");
            npc.AddPart(new RenderPart { DisplayName = name, RenderString = "n", ColorString = "&c", RenderLayer = 10 });
            npc.AddPart(new PhysicsPart { Solid = true });
            return npc;
        }

        private static Entity CreateItem(string name, char glyph, string color)
        {
            var item = new Entity { BlueprintName = name.Replace(" ", string.Empty) };
            item.AddPart(new RenderPart { DisplayName = name, RenderString = glyph.ToString(), ColorString = color, RenderLayer = 10 });
            item.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            return item;
        }

        private static Entity CreateContainer(string name)
        {
            var container = new Entity { BlueprintName = name.Replace(" ", string.Empty) };
            container.AddPart(new RenderPart { DisplayName = name, RenderString = "C", ColorString = "&y", RenderLayer = 10 });
            container.AddPart(new PhysicsPart { Solid = true });
            container.AddPart(new ContainerPart());
            return container;
        }

        private static void PrepareConversation(Entity speaker, Entity listener, string text)
        {
            var conversation = new ConversationData { ID = "TestConversation" };
            conversation.Nodes.Add(new NodeData
            {
                ID = "Start",
                Text = text
            });

            ConversationManager.CurrentConversation = conversation;
            ConversationManager.CurrentNode = conversation.GetStartNode();
            ConversationManager.Speaker = speaker;
            ConversationManager.Listener = listener;
            ConversationManager.RefreshVisibleChoices();
        }

        private static void ResetConversationState()
        {
            ConversationManager.PendingAttackTarget = null;
            ConversationManager.PendingTradePartner = null;
            ConversationManager.EndConversation();
        }
    }
}
