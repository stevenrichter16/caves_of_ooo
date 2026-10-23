using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>F1's actual inventory reader must carry the selected owner without losing
    /// its decision prose or pretending a recipe/container preview is one examined item.</summary>
    public sealed class InventoryDetailsPresentationTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        HotbarSaveFixture scope;
        readonly List<GameObject> hosts = new List<GameObject>();
        EntityFactory factory;
        Entity player;
        Zone zone;
        InputHandler input;
        TurnManager turns;
        InventoryUI UI => input.InventoryUI;
        InventoryPart Pack => player.GetPart<InventoryPart>();

        sealed class DescriptionCallback : IItemEnhancement
        {
            public override string Name => "InventoryDetailsPresentationCallback";
            public Action Callback;
            public int Calls;
            public override string GetEffectDescription()
            {
                if (Calls++ == 0) Callback?.Invoke();
                return "A selected-item enhancement note.";
            }
        }

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = new EntityFactory();
            factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
            player = factory.CreateEntity("Player");
            zone = new Zone("Overworld.2.6.0"); zone.AddEntity(player, 10, 10);
            zone.GetCell(10, 10).Explored = zone.GetCell(10, 10).IsVisible = true;
            turns = new TurnManager(); turns.AddEntity(player); turns.ProcessUntilPlayerTurn();
            input = Make<InputHandler>(); input.PlayerEntity = player; input.CurrentZone = zone; input.TurnManager = turns;
            input.InventoryUI = Make<InventoryUI>(); UI.PlayerEntity = player; UI.CurrentZone = zone; UI.EntityFactory = factory;
            input.AnnouncementUI = Make<AnnouncementUI>();
            MessageLog.Clear();
        }

        [TearDown] public void Cleanup()
        {
            for (int i = hosts.Count - 1; i >= 0; i--) if (hosts[i] != null) Object.DestroyImmediate(hosts[i]);
            hosts.Clear(); scope?.Dispose();
        }

        [TestCase("HealingTonic", false)] [TestCase("HealingTonic", true)]
        [TestCase("Dagger", false)] [TestCase("Dagger", true)]
        [TestCase("FireClay", false)] [TestCase("FireClay", true)]
        [TestCase("DriedMeat", false)] [TestCase("DriedMeat", true)]
        public void SelectedItemReaderPreservesDecisionProseAndSuppliesExactSubject(string blueprint, bool popup)
        {
            var item = Select(blueprint, popup);
            string before = UI.SelectedDecisionDetails(); Assert.IsNotEmpty(before);
            int energy = turns.GetEnergy(player), tick = turns.TickCount, quantity = item.GetPart<StackerPart>().StackCount;
            Call(input, "OpenInventoryDetailsReader");
            AssertDocument(item, before);
            Assert.True(Pack.Contains(item)); Assert.AreEqual(quantity, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(energy, turns.GetEnergy(player)); Assert.AreEqual(tick, turns.TickCount);
            input.AnnouncementUI.Close(); Call(input, "CloseAnnouncement");
            Assert.AreEqual("InventoryOpen", State); Assert.True(UI.IsOpen);
            Assert.AreEqual(before, UI.SelectedDecisionDetails(), "Closing the reader preserves the same inventory selection.");
        }

        [Test] public void KeyboardF1DispatchesTheRealInventoryRouteWithItemIdentity()
        {
            var item = Select("HealingTonic", false); var devices = new InputTestFixture(); devices.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1)); InputSystem.Update();
                Call(input, "UpdateInput");
                AssertDocument(item, null);
            }
            finally { devices.TearDown(); }
        }

        [Test] public void EquippedSelectionKeepsItsActualOwnershipAndDecisionComparison()
        {
            var item = Select("Dagger", false);
            Assert.True(Pack.EquipToBodyPart(item, player.GetPart<Body>().GetParts().Find(part => part.Type == "Hand")));
            UI.Open();
            var slots = (List<InventoryScreenData.EquipmentSlot>)Field(UI, "_equipSlots");
            int index = slots.FindIndex(slot => slot.EquippedItem == item); Assert.GreaterOrEqual(index, 0);
            Set(UI, "_equipCursorIndex", index);
            string before = UI.SelectedDecisionDetails();
            Call(input, "OpenInventoryDetailsReader"); AssertDocument(item, before);
            Assert.AreSame(player, item.GetPart<PhysicsPart>().Equipped);
        }

        [TestCase("removed")] [TestCase("hidden")] [TestCase("empty-stack")]
        [TestCase("foreign-render")] [TestCase("foreign-examinable")]
        public void InvalidSelectedSubjectDoesNotFallBackToPlainStaleDetails(string invalidation)
        {
            var item = Select("HealingTonic", false);
            if (invalidation == "removed") Pack.RemoveObject(item);
            else if (invalidation == "hidden") item.GetPart<RenderPart>().Visible = false;
            else if (invalidation == "empty-stack") item.GetPart<StackerPart>().StackCount = 0;
            else if (invalidation == "foreign-render") item.GetPart<RenderPart>().ParentEntity = new Entity();
            else item.GetPart<ExaminablePart>().ParentEntity = new Entity();
            Call(input, "OpenInventoryDetailsReader");
            Assert.AreEqual("InventoryOpen", State); Assert.False(input.AnnouncementUI.IsOpen);
            Assert.False(MessageLog.HasPendingAnnouncement);
        }

        [TestCase("remove")] [TestCase("viewer")] [TestCase("zone")]
        public void DescriptionCallbackCannotPublishAfterItsReadContextChanges(string change)
        {
            var item = Select("HealingTonic", false);
            var callback = new DescriptionCallback { Callback = () =>
            {
                if (change == "remove") Pack.RemoveObject(item);
                else if (change == "viewer") input.PlayerEntity = factory.CreateEntity("Player");
                else input.CurrentZone = new Zone("replacement-details-zone");
            }};
            item.AddPart(callback);
            Call(input, "OpenInventoryDetailsReader");
            Assert.Greater(callback.Calls, 0);
            Assert.AreEqual("InventoryOpen", State); Assert.False(input.AnnouncementUI.IsOpen);
        }

        [Test] public void CraftingPreviewRemainsNeutralAndKeepsItsDecisionText()
        {
            Select("HealingTonic", false); Set(UI, "_panel", 4);
            string before = UI.SelectedDecisionDetails(); Assert.IsNotEmpty(before);
            Call(input, "OpenInventoryDetailsReader");
            Assert.AreEqual("AnnouncementOpen", State); Assert.IsNull(input.AnnouncementUI.Document);
            Assert.AreEqual(before, Field(input.AnnouncementUI, "_message"));
        }

        [Test] public void ContainerTransferPreviewDoesNotMisidentifyEitherOwnerAsTheSubject()
        {
            var chest = factory.CreateEntity("Chest"); zone.AddEntity(chest, 10, 10);
            zone.GetCell(11, 10).Explored = zone.GetCell(11, 10).IsVisible = true;
            Select("HealingTonic", true);
            var popup = Field(UI, "_itemActionPopup"); var actions = (IList)Field(popup, "Actions");
            int index = -1;
            for (int i = 0; i < actions.Count; i++) if ((string)Field(actions[i], "Command") == "put_container") index = i;
            Assert.GreaterOrEqual(index, 0, "The actual chest at the player's feet supplies a real composite transfer preview.");
            Set(popup, "CursorIndex", index);
            string before = UI.SelectedDecisionDetails(); Assert.IsNotEmpty(before);
            Call(input, "OpenInventoryDetailsReader");
            Assert.AreEqual("AnnouncementOpen", State); Assert.IsNull(input.AnnouncementUI.Document);
            Assert.AreEqual(before, Field(input.AnnouncementUI, "_message"));
        }

        [Test] public void SearchDoesNotOpenSelectedItemDetails()
        {
            Select("HealingTonic", false); Call(UI, "BeginInventorySearch");
            Call(input, "OpenInventoryDetailsReader");
            Assert.True(UI.IsSearching); Assert.AreEqual("InventoryOpen", State); Assert.False(input.AnnouncementUI.IsOpen);
        }

        [Test] public void ReaderLeavesPendingWorldNoticesQueuedAndReturnsAfterTheyAreRead()
        {
            var item = Select("HealingTonic", false);
            MessageLog.AddAnnouncement("Existing world notice.");
            var queue = MessageLog.GetPendingAnnouncementsSnapshot();
            Call(input, "OpenInventoryDetailsReader"); AssertDocument(item, null);
            CollectionAssert.AreEqual(queue, MessageLog.GetPendingAnnouncementsSnapshot());
            input.AnnouncementUI.Close(); Call(input, "CloseAnnouncement");
            Assert.AreEqual("AnnouncementOpen", State); Assert.IsNull(input.AnnouncementUI.Document);
            Assert.AreEqual("Existing world notice.", Field(input.AnnouncementUI, "_message"));
            input.AnnouncementUI.Close(); Call(input, "CloseAnnouncement");
            Assert.AreEqual("InventoryOpen", State); Assert.True(UI.IsOpen);
        }

        Entity Select(string blueprint, bool popup)
        {
            var item = factory.CreateEntity(blueprint); Assert.NotNull(item); Assert.True(Pack.AddObject(item));
            Call(input, "OpenInventory"); Assert.True(UI.ReopenItemActionPopupFor(item));
            if (!popup) Set(UI, "_itemActionPopup", null);
            return item;
        }
        void AssertDocument(Entity item, string decision)
        {
            Assert.AreEqual("AnnouncementOpen", State);
            var document = input.AnnouncementUI.Document;
            Assert.NotNull(document, "Inventory F1 must use the current selected-item document rather than NOTICE / Field notes.");
            Assert.AreEqual("EXAMINE", document.Kind); Assert.AreEqual(item.GetDisplayName(), document.Title);
            Assert.AreEqual(item.BlueprintName, document.BlueprintName);
            Assert.AreSame(item, typeof(AnnouncementDocument).GetProperty("Subject", Members).GetValue(document));
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            if (decision != null)
            {
                Assert.AreEqual(decision, Field(input.AnnouncementUI, "_message"));
                Assert.True(document.Sections.Any(section => section.Text == decision),
                    "The original comparison/handling/food/material prose must remain a displayed section, not merely a hidden fallback.");
            }
        }
        string State => Field(input, "_inputState").ToString();
        T Make<T>() where T : Component { var host = new GameObject(typeof(T).Name); host.SetActive(false); hosts.Add(host); return host.AddComponent<T>(); }
        static object Field(object owner, string name) => owner.GetType().GetField(name, Members).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Members).SetValue(owner, value);
        static object Call(object owner, string name, params object[] args)
        {
            var method = owner.GetType().GetMethod(name, Members); Assert.NotNull(method, name);
            return method.Invoke(owner, args);
        }
    }
}
