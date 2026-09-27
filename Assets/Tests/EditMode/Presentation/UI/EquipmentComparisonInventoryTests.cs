using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Density item examination through the existing inventory and announcement seams. No proposed helper/API dependency:
    /// real item-menu selection -> InventoryUI.ExecuteItemAction -> real
    /// InputHandler announcement handoff. Direct selection is an EditMode seam,
    /// not a claim that this fixture sends native keyboard input.
    /// </summary>
    public sealed class EquipmentComparisonInventoryTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager world;
        private Zone zone;
        private Entity player;
        private GameObject host;
        private InventoryUI inventoryUI;
        private InputHandler input;
        private AnnouncementUI announcement;
        private Camera camera, popupCamera;
        private CameraFollow follow;
        private InventoryPart Inventory => player.GetPart<InventoryPart>();

        [SetUp]
        public void SetUp()
        {
            // Existing fixture snapshots/restores the native static registries,
            // message/save context and borrowed MainCamera tags. No game bootstrap.
            scope = new HotbarSaveFixture(false, false);
            factory = new EntityFactory();
            var content = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.That(content, Is.Not.Null);
            factory.LoadBlueprints(content.text);
            world = OverworldZoneManager.CreateDetached(factory, 64);
            zone = new Zone("Overworld.2.6.0");
            player = Item("Player");
            Assert.That(Inventory.Objects, Is.Empty, "no developer material grant");
            Assert.That(zone.AddEntity(player, 10, 10), Is.True);
            world.SetActiveZone(zone); // attach existing graph; no generation
            Assert.That(world.CachedZones.Count, Is.EqualTo(1));
            Assert.That(WorldLocationContext.For(zone), Is.SameAs(world));
            SettlementRuntime.ActiveZone = zone;
            MessageLog.Clear();Diag.ResetAll();Diag.SetChannel("event",true);

            host = new GameObject("Material guidance test");
            host.transform.SetParent(scope.Root.transform, false); // stays inactive
            var grid = new GameObject("Material guidance grid");
            grid.transform.SetParent(host.transform, false);
            grid.AddComponent<Grid>();
            inventoryUI = host.AddComponent<InventoryUI>();
            inventoryUI.Tilemap = Tiles(grid.transform, "Inventory main");
            inventoryUI.PlayerEntity = player;
            inventoryUI.CurrentZone = zone;
            inventoryUI.EntityFactory = factory;
            announcement = host.AddComponent<AnnouncementUI>();
            announcement.Tilemap = Tiles(grid.transform, "Announcement foreground");
            announcement.BgTilemap = Tiles(grid.transform, "Announcement background");
            var cameraObject = new GameObject("Material UI camera");
            cameraObject.transform.SetParent(host.transform, false);
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(40, 22.5f, -10);
            var popupObject = new GameObject("Material popup camera");
            popupObject.transform.SetParent(host.transform, false);
            popupCamera = popupObject.AddComponent<Camera>();
            popupCamera.orthographic = true;
            popupCamera.enabled = false;
            follow = cameraObject.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.PopupOverlayCamera = popupCamera;
            announcement.PopupCamera = popupCamera;
            input = host.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            input.ZoneManager = world;
            input.WorldMap = world.WorldMap;
            input.InventoryUI = inventoryUI;
            input.AnnouncementUI = announcement;
            input.CameraFollow = follow;
            Call(input, "OpenInventory");
            Assert.That(inventoryUI.IsOpen, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (announcement != null) announcement.Close();
            if (inventoryUI != null) inventoryUI.Close();
            if (host != null) Object.DestroyImmediate(host);
            scope?.Dispose();Diag.ResetAll();
        }

        [Test]
        public void CompareIsDiscoverableAndUsesTheExistingFreePaginatedReader()
        {
            var current = Carry("Dagger");
            Assert.That(InventorySystem.Equip(player, current), Is.True);
            var candidate = Carry("DissolutionMaul");
            var before = new Snapshot(this);
            int energy = player.GetStatValue("Energy"), tick = WorldClock.CurrentTick;
            int version = EquipmentChangeBus.GlobalVersion;
            int index = OpenCompare(candidate);
            var commands = Actions().Cast<object>().Select(a => (string)Get(a, "Command")).ToArray();
            Assert.That(commands.Count(c => c == "compare_equipment"), Is.EqualTo(1));
            Assert.That(commands, Does.Contain("equip_auto"));
            Assert.That(commands, Does.Contain("examine_item"));
            Call(inventoryUI, "ExecuteItemAction", index);
            string text = OpenQueuedAnnouncement();
            StringAssert.Contains("Candidate: " + candidate.GetDisplayName(), text);
            StringAssert.Contains("Displaced: " + current.GetDisplayName(), text);
            Assert.That(announcement.PageCount, Is.GreaterThan(1));
            announcement.GoToPage(announcement.PageCount - 1);
            Assert.That(announcement.VisibleLines.Count, Is.GreaterThan(0));
            CloseAnnouncement();
            before.AssertUnchanged(this);
            Assert.That(player.GetStatValue("Energy"), Is.EqualTo(energy));
            Assert.That(WorldClock.CurrentTick, Is.EqualTo(tick));
            Assert.That(EquipmentChangeBus.GlobalVersion, Is.EqualTo(version));
            Assert.That(current.GetPart<PhysicsPart>().Equipped, Is.SameAs(player));
            Assert.That(Get(inventoryUI, "_pendingEverydayTurn"), Is.EqualTo(false));
        }

        [TestCase(false)] [TestCase(true)]
        public void SelectionRevalidatesRemovedAndForeignItems(bool foreign)
        {
            var item = Carry("Dagger");
            int index = OpenCompare(item);
            object popup = Get(inventoryUI, "_itemActionPopup");
            if (foreign) item.GetPart<PhysicsPart>().InInventory = Item("Player");
            else Assert.That(Inventory.RemoveObject(item), Is.True);
            Call(inventoryUI, "ExecuteItemAction", index);
            Assert.That(MessageLog.HasPendingAnnouncement, Is.False);
            Assert.That(Get(inventoryUI, "_itemActionPopup"), Is.SameAs(popup));
            Assert.That((string)Get(inventoryUI, "_actionStatus"), Is.Not.Empty);
        }

        [Test]
        public void ChangedEquipmentAndCandidateFieldsAreReadWhenActionIsChosen()
        {
            var candidate = Carry("LongSword"); int index = OpenCompare(candidate);
            var current = Carry("DissolutionMaul");
            Assert.That(InventorySystem.Equip(player, current), Is.True);
            candidate.GetPart<MeleeWeaponPart>().BaseDamage = "3d2+1";
            Call(inventoryUI, "ExecuteItemAction", index);
            string text = OpenQueuedAnnouncement();
            StringAssert.Contains("Displaced: " + current.GetDisplayName(), text);
            StringAssert.Contains("Damage: 3d2+1 per penetration", text);
            CloseAnnouncement();
            Assert.That(InventorySystem.IsEquipped(player, current), Is.True);
        }

        [Test]
        public void IncompatibleSlotStillOffersAComparisonWithItsUnavailableReason()
        {
            var item = Carry("LeatherCap"); item.GetPart<EquippablePart>().UsesSlots = "NoSuchSlot";
            Call(inventoryUI, "ExecuteItemAction", OpenCompare(item));
            StringAssert.Contains("Unavailable: No available NoSuchSlot slot", OpenQueuedAnnouncement());
            CloseAnnouncement();
        }

        [Test]
        public void UnequippableItemKeepsExamineWithoutAComparisonAction()
        {
            var item = Carry("HealingTonic");
            Assert.That(inventoryUI.ReopenItemActionPopupFor(item), Is.True);
            var commands = Actions().Cast<object>().Select(a => (string)Get(a, "Command")).ToArray();
            Assert.That(commands, Does.Not.Contain("compare_equipment"));
            Assert.That(commands, Does.Contain("examine_tonic"));
        }

        [Test]
        public void ReadingDoesNotAuthorizeLaterEquipAfterTheItemIsRemoved()
        {
            var item = Carry("Dagger");
            Call(inventoryUI, "ExecuteItemAction", OpenCompare(item));
            OpenQueuedAnnouncement();
            Assert.That(Inventory.RemoveObject(item), Is.True);
            Assert.That(zone.AddEntity(item, 11, 10), Is.True);
            CloseAnnouncement();
            Assert.That(Call(inventoryUI, "TryEquipViaCommand", item, null), Is.EqualTo(false));
            Assert.That(item.GetPart<PhysicsPart>().Equipped, Is.Null);
            Assert.That(zone.GetEntityCell(item), Is.SameAs(zone.GetCell(11, 10)));
        }

        [Test]
        public void EquippedCandidateCanBeReadWithoutMovingIt()
        {
            var item = Carry("LeatherArmor"); Assert.That(InventorySystem.Equip(player, item), Is.True);
            Call(inventoryUI, "ExecuteItemAction", OpenCompare(item));
            StringAssert.Contains("Currently equipped:", OpenQueuedAnnouncement());
            CloseAnnouncement();
            Assert.That(InventorySystem.IsEquipped(player, item), Is.True);
        }

        private Entity Item(string blueprint)
        {
            var entity = factory.CreateEntity(blueprint);
            Assert.That(entity, Is.Not.Null, blueprint);
            return entity;
        }
        private Entity Carry(string blueprint, int count = 1)
        {
            var item = Item(blueprint);
            var stack = item.GetPart<StackerPart>();
            Assert.That(stack, Is.Not.Null, blueprint + " shipped stack contract");
            stack.StackCount = count;
            Assert.That(Inventory.AddObject(item), Is.True);
            Assert.That(Inventory.Contains(item), Is.True);
            return item;
        }
        private static int Count(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        private IList Actions() => (IList)Get(Get(inventoryUI, "_itemActionPopup"), "Actions");
        private int OpenCompare(Entity item)
        {
            Assert.That(inventoryUI.ReopenItemActionPopupFor(item), Is.True);
            var actions = Actions();
            var found = new List<int>();
            for (int i = 0; i < actions.Count; i++)
                if ((string)Get(actions[i], "Command") == "compare_equipment") found.Add(i);
            Assert.That(found.Count, Is.EqualTo(1), "One discoverable equipment comparison action.");
            return found[0];
        }
        private string OpenQueuedAnnouncement()
        {
            Assert.That(MessageLog.HasPendingAnnouncement, Is.True,
                "inventory Examine must be visible now, not hidden in the gameplay log");
            string expected = MessageLog.GetPendingAnnouncementsSnapshot().Single();
            Assert.That(Call(input, "TryOpenAnnouncement"), Is.EqualTo(true));
            Assert.That(announcement.IsOpen, Is.True);
            Assert.That(Get(input, "_inputState").ToString(), Is.EqualTo("AnnouncementOpen"));
            Assert.That(Get(announcement, "_message"), Is.EqualTo(expected));
            Assert.That(announcement.Tilemap.GetUsedTilesCount(), Is.GreaterThan(0));
            Assert.That(announcement.BgTilemap.GetUsedTilesCount(), Is.GreaterThan(0));
            var lines = ((IList)Get(announcement, "_wrappedLines")).Cast<string>().ToArray();
            Assert.That(lines.Length, Is.GreaterThan(0));
            Assert.That(lines.All(line => line != null), Is.True);
            Assert.That((int)Get(announcement, "_popupH"), Is.LessThanOrEqualTo(CenteredPopupLayout.GridHeight), "description must fit on screen");
            Assert.That(MessageLog.HasPendingAnnouncement, Is.False, "one material inspection queues one modal");
            return expected;
        }
        private void CloseAnnouncement()
        {
            announcement.Close();
            Call(input, "CloseAnnouncement");
            Assert.That(announcement.IsOpen, Is.False);
            Assert.That(Get(input, "_inputState").ToString(), Is.EqualTo("InventoryOpen"));
        }
        private static void Has(string text, string pattern, string reason)
            => Assert.That(Regex.IsMatch(text ?? "", pattern, RegexOptions.IgnoreCase), Is.True, reason + "\n" + text);
        private static Tilemap Tiles(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var tiles = child.AddComponent<Tilemap>();
            child.AddComponent<TilemapRenderer>();
            return tiles;
        }
        private static object Get(object owner, string name)
        {
            var field = owner.GetType().GetField(name, Fields);
            Assert.That(field, Is.Not.Null, owner.GetType().Name + "." + name);
            return field.GetValue(owner);
        }
        private static object Call(object owner, string name, params object[] args)
        {
            var method = owner.GetType().GetMethod(name, Fields);
            Assert.That(method, Is.Not.Null, owner.GetType().Name + "." + name);
            return method.Invoke(owner, args);
        }

        private sealed class Snapshot
        {
            private readonly Entity[] items, owners;
            private readonly KeyValuePair<string, Zone>[] cached;
            private readonly KeyValuePair<string, string>[] properties;
            private readonly KeyValuePair<string, int>[] intProperties;
            private readonly int[] quantities;
            private readonly int health, drams;
            public Snapshot(EquipmentComparisonInventoryTests t)
            {
                items = t.Inventory.Objects.ToArray();
                quantities = items.Select(Count).ToArray();
                owners = t.zone.GetReadOnlyEntities().ToArray();
                cached = t.world.CachedZones.ToArray();
                properties = t.player.Properties.ToArray();
                intProperties = t.player.IntProperties.ToArray();
                health = t.player.GetStatValue("Hitpoints");
                drams = TradeSystem.GetDrams(t.player);
            }
            public void AssertUnchanged(EquipmentComparisonInventoryTests t)
            {
                CollectionAssert.AreEqual(items, t.Inventory.Objects);
                CollectionAssert.AreEqual(quantities, t.Inventory.Objects.Select(Count).ToArray());
                CollectionAssert.AreEquivalent(owners, t.zone.GetReadOnlyEntities());
                CollectionAssert.AreEquivalent(cached, t.world.CachedZones);
                CollectionAssert.AreEquivalent(properties, t.player.Properties);
                CollectionAssert.AreEquivalent(intProperties, t.player.IntProperties);
                Assert.That(t.player.GetStatValue("Hitpoints"), Is.EqualTo(health));
                Assert.That(TradeSystem.GetDrams(t.player), Is.EqualTo(drams));
                Assert.That(t.zone.GetEntityCell(t.player), Is.SameAs(t.zone.GetCell(10, 10)));
                foreach (var item in items) Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(t.player));
            }
        }
    }
}
