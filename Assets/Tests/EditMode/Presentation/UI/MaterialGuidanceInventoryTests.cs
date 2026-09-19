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
    /// R4 carried-material specifications. No proposed helper/API dependency:
    /// real item-menu selection -> InventoryUI.ExecuteItemAction -> real
    /// InputHandler announcement handoff. Direct selection is an EditMode seam,
    /// not a claim that this fixture sends native keyboard input.
    /// </summary>
    public sealed class MaterialGuidanceInventoryTests
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

        // H1: the normal factory item has a discoverable, visible use. An
        // announcement queue alone is insufficient: open the real modal, require
        // drawn text/background, then dismiss back to the selected inventory row.
        [TestCase("FireClay", "OvenBuildersGuide", "oven", "farmer")]
        [TestCase("SilverSand", "WellMaintenanceManual", "well", "keeper")]
        [TestCase("WardOil", "LanternOilRecipe", "lantern", "warden")]
        public void RealMaterialExamineShowsUsefulModalAndReturnsWithoutSideEffects(
            string blueprint, string guideBlueprint, string use, string recipient)
        {
            var item = Carry(blueprint, 2);
            string guideName = Item(guideBlueprint).GetDisplayName().ToLowerInvariant();
            var snapshot = new Snapshot(this);
            int action = OpenExamine(item, requireSingle: true);
            int panel = (int)Get(inventoryUI, "_panel");
            int cursor = (int)Get(inventoryUI, "_cursorIndex");
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float zoom = camera.orthographicSize;
            Rect viewport = camera.rect;
            Call(inventoryUI, "ExecuteItemAction", action);
            string text = OpenQueuedAnnouncement();
            Assert.That(DiagQuery.Count(new DiagQuery.Filter { Category="event",Kind="MaterialExamined",Actor=player.ID,Target=item.ID }).Count,Is.EqualTo(1));
            Assert.That(text.ToLowerInvariant(), Does.Contain(item.GetDisplayName().ToLowerInvariant()));
            Assert.That(text.ToLowerInvariant(), Does.Contain(guideName));
            Assert.That(text.ToLowerInvariant(), Does.Contain(use));
            Assert.That(text.ToLowerInvariant(), Does.Contain(recipient));
            Has(text, @"\b(one|1)\b", "state the actual one-unit payment");
            Has(text, @"\b(consum\w*|spent|spend\w*|uses?|costs?)\b", "explain the material is paid");
            Has(text, @"\b(kept|keep\w*|retain\w*)\b", "explain the guide remains");
            Assert.That(inventoryUI.IsOpen, Is.True);
            Assert.That(camera.transform.position, Is.EqualTo(position));
            Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.orthographicSize, Is.EqualTo(zoom).Within(0.001));
            Assert.That(camera.rect, Is.EqualTo(viewport));
            snapshot.AssertUnchanged(this);
            CloseAnnouncement();
            Assert.That(Get(input, "_inputState").ToString(), Is.EqualTo("InventoryOpen"));
            Assert.That(inventoryUI.IsOpen, Is.True);
            Assert.That(Get(inventoryUI, "_panel"), Is.EqualTo(panel));
            Assert.That(Get(inventoryUI, "_cursorIndex"), Is.EqualTo(cursor));
            Assert.That(camera.transform.position, Is.EqualTo(position));
            Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.orthographicSize, Is.EqualTo(zoom).Within(0.001));
            Assert.That(camera.rect, Is.EqualTo(viewport));
            Assert.That(popupCamera.enabled, Is.False);
            snapshot.AssertUnchanged(this);
        }

        // H2: the guide-status line reflects current carried identity, not an
        // earlier popup or a similarly themed book. No new helper is prescribed.
        [TestCase("FireClay", "OvenBuildersGuide", "WellMaintenanceManual")]
        [TestCase("SilverSand", "WellMaintenanceManual", "LanternOilRecipe")]
        [TestCase("WardOil", "LanternOilRecipe", "OvenBuildersGuide")]
        public void GuideStatusRefreshesAfterAcquiringAndRemovingExactRequiredBook(
            string blueprint, string required, string wrong)
        {
            var material = Carry(blueprint);
            var unrelatedGuide = Carry(wrong);
            var first = new Snapshot(this);
            string missing = ExamineAndDismiss(material);
            Has(missing, @"\b(missing|not carried|not carrying|not in (?:your )?pack)\b", "missing required book is explicit");
            first.AssertUnchanged(this);
            var guide = Carry(required);
            var second = new Snapshot(this);
            string available = ExamineAndDismiss(material);
            Assert.That(available, Is.Not.EqualTo(missing), "guide availability is not stale");
            Has(available, @"\b(carried|carrying|in your pack|you have)\b", "the exact guide is carried");
            Assert.That(Regex.IsMatch(available, @"\b(missing|not carried|not carrying|not in (?:your )?pack)\b", RegexOptions.IgnoreCase), Is.False);
            second.AssertUnchanged(this);
            Assert.That(Inventory.RemoveObject(guide), Is.True);
            string removed = ExamineAndDismiss(material);
            Has(removed, @"\b(missing|not carried|not carrying|not in (?:your )?pack)\b", "removing the guide invalidates the claim");
            Assert.That(Inventory.Contains(unrelatedGuide), Is.True);
            Assert.That(world.CachedZones.Count, Is.EqualTo(1));
        }

        // H3: consumer-truth controls are intentionally independent of wording.
        // They use the shipped factory and SettlementManager, not a fake repair
        // function; all are expected to pass before the description feature.
        [TestCase("FireClay", "OvenBuildersGuide", "VillageOven", RepairMethodId.OvenRebuild, "SilverSand")]
        [TestCase("SilverSand", "WellMaintenanceManual", "MainWell", RepairMethodId.ManualRepair, "PaleSalt")]
        [TestCase("WardOil", "LanternOilRecipe", "VillageLantern", RepairMethodId.LanternReforge, "LampOil")]
        public void AdvertisedManualRepairRequiresExactGoodsConsumesOneAndKeepsGuide(
            string blueprint, string guideBlueprint, string site, RepairMethodId method, string wrongBlueprint)
        {
            const string settlement = "Overworld.10.10.0";
            var manager = world.SettlementManager;
            Assert.That(manager.GetSite(settlement, site).Stage, Is.EqualTo(RepairStage.Fouled));
            var material = Carry(blueprint, 2);
            Assert.That(manager.ApplyRepairMethod(settlement, site, method, player), Is.False, "material alone is insufficient");
            Assert.That(Count(material), Is.EqualTo(2));
            var guide = Carry(guideBlueprint);
            Assert.That(Inventory.RemoveObject(material), Is.True);
            var wrong = Carry(wrongBlueprint, 2);
            Assert.That(manager.ApplyRepairMethod(settlement, site, method, player), Is.False, "a similar material cannot pay");
            Assert.That(Count(wrong), Is.EqualTo(2));
            Assert.That(manager.GetSite(settlement, site).Stage, Is.EqualTo(RepairStage.Fouled));
            Assert.That(Inventory.AddObject(material), Is.True);
            int drams = TradeSystem.GetDrams(player);
            Assert.That(manager.ApplyRepairMethod(settlement, site, method, player), Is.True);
            Assert.That(manager.GetSite(settlement, site).Stage, Is.EqualTo(RepairStage.StableRepair));
            Assert.That(Count(material), Is.EqualTo(1));
            Assert.That(Inventory.Contains(guide), Is.True);
            Assert.That(Count(guide), Is.EqualTo(1));
            Assert.That(Count(wrong), Is.EqualTo(2));
            Assert.That(TradeSystem.GetDrams(player), Is.EqualTo(drams));
            Assert.That(manager.ApplyRepairMethod(settlement, site, method, player), Is.False);
            Assert.That(Count(material), Is.EqualTo(1), "repeat repair must not pay again");
            Assert.That(Inventory.Contains(guide), Is.True);
            Assert.That(world.CachedZones.Count, Is.EqualTo(1), "repair state is not generated zone content");
        }

        [Test]
        public void FireClayExplainsConditionalBellUseAndTheMaterialFreeAlternative()
        {
            var material = Carry("FireClay");
            var before = new Snapshot(this);
            string text = ExamineAndDismiss(material);
            Has(text, @"\bMorrowfast\b", "name the actual place");
            Has(text, @"\b(bell|clapper)\b", "name the second consumer");
            Has(text, @"\bquiet\w*\b", "clay changes the bell's use");
            Has(text, @"\b(diagnos\w*|inspect\w*|cord)\b", "do not imply the quest prerequisite is already met");
            Has(text, @"\b(loud|bare)\b", "preserve the actual alternative");
            Has(text, @"\b(free|no material|costs? nothing|no clay)\b", "the alternative does not cost clay");
            before.AssertUnchanged(this); // specifically no quest/notes mutation
        }

        // H4: fixed scope and dispatch precedence. Reagent lamp oil is not ward
        // oil; pale salt is not silver sand; renaming a dagger cannot create help.
        [TestCase("LampOil", null)]
        [TestCase("PaleSalt", null)]
        [TestCase("Dagger", null)]
        [TestCase("Dagger", "fire clay")]
        public void UnrelatedItemsKeepTheirOrdinaryExamineAndOtherActions(string blueprint, string renamed)
        {
            var item = Carry(blueprint);
            if (renamed != null) item.GetPart<RenderPart>().DisplayName = renamed;
            Assert.That(TonicExamineService.TryDescribe(item, out _), Is.False, "this is an ordinary-examine control");
            var before = new Snapshot(this);
            int action = OpenExamine(item, requireSingle: true);
            var commands = Actions().Cast<object>().Select(a => (string)Get(a, "Command")).ToArray();
            Assert.That(commands, Does.Contain("drop"));
            if (blueprint == "Dagger") Assert.That(commands, Does.Contain("equip_auto"));
            Call(inventoryUI, "ExecuteItemAction", action);
            Assert.That(MessageLog.HasPendingAnnouncement, Is.False,
                "this bounded slice must not route unrelated items to repair guidance");
            Assert.That(MessageLog.GetLast().ToLowerInvariant(), Does.Contain(item.GetDisplayName().ToLowerInvariant()));
            before.AssertUnchanged(this);
        }

        [Test]
        public void ExistingTonicExamineStillDescribesItsRealEffectAndDoesNotDrinkIt()
        {
            var tonic = Carry("HealingTonic", 2);
            Assert.That(TonicExamineService.TryDescribe(tonic, out string expected), Is.True);
            var before = new Snapshot(this);
            Assert.That(inventoryUI.ReopenItemActionPopupFor(tonic), Is.True);
            int index = -1;
            var actions = Actions();
            for (int i = 0; i < actions.Count; i++)
                if ((string)Get(actions[i], "Command") == "examine_tonic") index = i;
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "existing richer tonic action");
            Call(inventoryUI, "ExecuteItemAction", index);
            Assert.That(OpenQueuedAnnouncement(), Is.EqualTo(expected));
            CloseAnnouncement();
            before.AssertUnchanged(this);
        }

        [TestCase("FireClay")]
        [TestCase("SilverSand")]
        [TestCase("WardOil")]
        public void StaleMaterialSelectionCannotAdvertiseAnUnownedInventoryItem(string blueprint)
        {
            var item = Carry(blueprint);
            int action = OpenExamine(item, requireSingle: true);
            Assert.That(Inventory.RemoveObject(item), Is.True);
            Assert.That(zone.AddEntity(item, 11, 10), Is.True);
            var before = new Snapshot(this);
            Call(inventoryUI, "ExecuteItemAction", action);
            Assert.That(MessageLog.HasPendingAnnouncement, Is.False,
                "revalidate inventory membership before presenting carried material instructions");
            Assert.That(zone.GetEntityCell(item), Is.SameAs(zone.GetCell(11, 10)));
            Assert.That(DiagQuery.Count(new DiagQuery.Filter { Category="event",Kind="MaterialExamineRejected",Actor=player.ID,Target=item.ID }).Count,Is.EqualTo(1));
            Assert.That(DiagQuery.Count(new DiagQuery.Filter { Category="event",Kind="MaterialExamined",Actor=player.ID,Target=item.ID }).Count,Is.Zero);
            before.AssertUnchanged(this);
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
            public Snapshot(MaterialGuidanceInventoryTests t)
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
            public void AssertUnchanged(MaterialGuidanceInventoryTests t)
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
