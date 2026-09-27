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
    public sealed class DensityItemExamineInventoryTests
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

        [TestCase("HealingTonic", "4d6+4", "ApplyTonic")]
        [TestCase("Dagger", "Damage: 1d4", "equip_auto")]
        [TestCase("LeatherArmor", "DV: -1", "equip_auto")]
        [TestCase("FlamingSword", "30%", "equip_auto")]
        [TestCase("IronshodBoots", "Speed: -5", "equip_auto")]
        public void OneExamineDisplaysRealDetailsAndPreservesOtherActions(string blueprint, string detail, string otherAction)
        {
            var item = Carry(blueprint, 2);
            var before = new Snapshot(this);
            int action = OpenExamine(item, requireSingle: true);
            var commands = Actions().Cast<object>().Select(a => (string)Get(a, "Command")).ToArray();
            Assert.That(commands, Does.Contain(otherAction));
            Assert.That(commands, Does.Contain("drop"));
            Call(inventoryUI, "ExecuteItemAction", action);
            string text = OpenQueuedAnnouncement();
            StringAssert.Contains(detail, text);
            StringAssert.Contains(item.GetDisplayName(), text);
            CloseAnnouncement();
            Assert.That(inventoryUI.IsOpen, Is.True);
            before.AssertUnchanged(this);
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamined", Actor = player.ID, Target = item.ID }).Records;
            Assert.That(records.Count, Is.EqualTo(1));
            StringAssert.Contains(item.BlueprintName, records[0].PayloadJson);
            Assert.That(DiagQuery.Count(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamineRejected" }).Count, Is.Zero);
        }

        [Test]
        public void PopupRetainsAuthoredFlavorAndEnhancementAlongsideMechanics()
        {
            var item = Carry("BreacherCleaver");
            var mod = new EnhancementSerrated(); mod.ApplyTier(2); item.AddPart(mod);
            string text = ExamineAndDismiss(item);
            StringAssert.Contains(item.GetPart<ExaminablePart>().Text, text);
            StringAssert.Contains(mod.GetEffectDescription(), text);
            StringAssert.Contains("Damage: 1d10", text);
        }

        [Test]
        public void MaterialGuidanceWinsEvenIfAMaterialAlsoAcquiresAConsumablePayload()
        {
            var item = Carry("FireClay");
            item.AddPart(new TonicPart { Healing = "1d4", Drink = true });
            var before = new Snapshot(this);
            int action = OpenExamine(item, requireSingle: true);
            Assert.That((string)Get(Actions()[action], "Command"), Is.EqualTo("examine_material"));
            Call(inventoryUI, "ExecuteItemAction", action);
            string text = OpenQueuedAnnouncement();
            StringAssert.Contains("oven", text.ToLowerInvariant());
            StringAssert.DoesNotContain("Heals 1d4", text);
            CloseAnnouncement();
            before.AssertUnchanged(this);
        }

        [Test]
        public void TonicRouteKeepsItsExistingCommandAndDoesNotDrinkTheItem()
        {
            var item = Carry("HealingTonic", 2);
            var before = new Snapshot(this);
            int action = OpenExamine(item, requireSingle: true);
            Assert.That((string)Get(Actions()[action], "Command"), Is.EqualTo("examine_tonic"));
            Call(inventoryUI, "ExecuteItemAction", action);
            StringAssert.Contains("Heals 4d6+4", OpenQueuedAnnouncement());
            CloseAnnouncement();
            before.AssertUnchanged(this);
        }

        [TestCase("Dagger", true)] [TestCase("HealingTonic", true)]
        [TestCase("Dagger", false)] [TestCase("HealingTonic", false)]
        public void StaleOrUnsupportedSelectionIsRejectedVisiblyWithoutClosingItsMenu(string blueprint, bool removeItem)
        {
            var item = Carry(blueprint);
            int action = OpenExamine(item, requireSingle: true);
            object popup = Get(inventoryUI, "_itemActionPopup");
            if (removeItem)
            {
                Assert.That(Inventory.RemoveObject(item), Is.True);
                Assert.That(zone.AddEntity(item, 11, 10), Is.True);
            }
            else if (blueprint == "Dagger")
            {
                Assert.That(item.RemovePart(item.GetPart<MeleeWeaponPart>()), Is.True);
                Assert.That(item.RemovePart(item.GetPart<EquippablePart>()), Is.True);
            }
            else
                item.GetPart<TonicPart>().Healing = "";

            var before = new Snapshot(this);
            Call(inventoryUI, "ExecuteItemAction", action);
            Assert.That(MessageLog.HasPendingAnnouncement, Is.False);
            Assert.That(Get(inventoryUI, "_itemActionPopup"), Is.SameAs(popup));
            Assert.That((string)Get(inventoryUI, "_actionStatus"), Is.Not.Empty);
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamineRejected", Actor = player.ID, Target = item.ID }).Records;
            Assert.That(records.Count, Is.EqualTo(1));
            StringAssert.Contains(removeItem ? "unavailable-inventory-item" : "no-supported-details", records[0].PayloadJson);
            Assert.That(DiagQuery.Count(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamined" }).Count, Is.Zero);
            before.AssertUnchanged(this);
        }

        [Test]
        public void RemovingWeaponStillLeavesActualEquipmentSlotDetailsInspectable()
        {
            var item = Carry("Dagger");
            Assert.That(item.RemovePart(item.GetPart<MeleeWeaponPart>()), Is.True);
            Assert.That(item.GetPart<EquippablePart>(), Is.Not.Null);
            var before = new Snapshot(this);
            string text = ExamineAndDismiss(item);
            StringAssert.Contains("Equip slots:", text);
            StringAssert.DoesNotContain("Damage:", text);
            before.AssertUnchanged(this);
        }

        [Test]
        public void EquippedItemRemainsInspectableWithoutUnequippingIt()
        {
            var item = Carry("Dagger");
            Assert.That(InventorySystem.Equip(player, item), Is.True);
            Assert.That(Inventory.Contains(item), Is.True);
            Assert.That(InventorySystem.IsEquipped(player, item), Is.True);
            StringAssert.Contains("Damage: 1d4", ExamineAndDismiss(item));
            Assert.That(InventorySystem.IsEquipped(player, item), Is.True);
            Assert.That(DiagQuery.Count(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamined", Actor = player.ID, Target = item.ID }).Count, Is.EqualTo(1));
        }

        [Test]
        public void RuntimeTonicWithoutItemTagKeepsItsLegacyPopupAndState()
        {
            var item = Carry("HealingTonic", 2);
            Assert.That(item.Tags.Remove("Item"), Is.True);
            Assert.That(TonicExamineService.TryDescribe(item, out string expected), Is.True);
            var before = new Snapshot(this);
            int action = OpenExamine(item, requireSingle: true);
            Assert.That((string)Get(Actions()[action], "Command"), Is.EqualTo("examine_tonic"));
            Call(inventoryUI, "ExecuteItemAction", action);
            Assert.That(OpenQueuedAnnouncement(), Is.EqualTo(expected));
            CloseAnnouncement();
            before.AssertUnchanged(this);
            Assert.That(DiagQuery.Count(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamined", Actor = player.ID, Target = item.ID }).Count, Is.EqualTo(1));
            Assert.That(DiagQuery.Count(new DiagQuery.Filter
                { Category = "event", Kind = "ItemExamineRejected" }).Count, Is.Zero);
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
        private int OpenExamine(Entity item, bool requireSingle)
        {
            Assert.That(inventoryUI.ReopenItemActionPopupFor(item), Is.True);
            var actions = Actions();
            var found = new List<int>();
            for (int i = 0; i < actions.Count; i++)
                if (string.Equals((string)Get(actions[i], "Label"), "Examine", StringComparison.OrdinalIgnoreCase)) found.Add(i);
            if (requireSingle) Assert.That(found.Count, Is.EqualTo(1), "one discoverable Examine, not two competing entries");
            Assert.That(found.Count, Is.GreaterThan(0));
            return found[0];
        }
        private string ExamineAndDismiss(Entity item)
        {
            int action = OpenExamine(item, requireSingle: true);
            Call(inventoryUI, "ExecuteItemAction", action);
            string text = OpenQueuedAnnouncement();
            CloseAnnouncement();
            return text;
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
            Assert.That(lines.All(line => line.Length <= 52), Is.True, "real fixed-width modal wraps its text");
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
            public Snapshot(DensityItemExamineInventoryTests t)
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
            public void AssertUnchanged(DensityItemExamineInventoryTests t)
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
