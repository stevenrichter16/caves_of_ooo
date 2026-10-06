using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FiftyClarityReaderInputTests
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        HotbarSaveFixture scope;
        InputHandler input;
        EntityFactory factory;
        Entity actor;
        Zone zone;
        TurnManager turns;
        readonly List<GameObject> objects = new List<GameObject>();
        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = new EntityFactory(); factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
            actor = factory.CreateEntity("Player"); zone = new Zone("Overworld.2.6.0"); zone.AddEntity(actor, 10, 10);
            zone.GetCell(10, 10).Explored = zone.GetCell(10, 10).IsVisible = true;
            turns = new TurnManager(); turns.AddEntity(actor); turns.ProcessUntilPlayerTurn();
            input = Make<InputHandler>(); input.PlayerEntity = actor; input.CurrentZone = zone; input.TurnManager = turns;
            input.AnnouncementUI = Make<AnnouncementUI>(); input.InventoryUI = Make<InventoryUI>();
            input.InventoryUI.PlayerEntity = actor; input.InventoryUI.CurrentZone = zone;
            input.PickupUI = Make<PickupUI>(); input.PickupUI.PlayerEntity = actor; input.PickupUI.CurrentZone = zone;
            input.TradeUI = Make<TradeUI>(); input.TradeUI.PlayerEntity = actor; input.TradeUI.CurrentZone = zone;
            MessageLog.Clear();
        }
        [TearDown] public void Teardown() { foreach (var obj in objects) UnityEngine.Object.DestroyImmediate(obj); objects.Clear(); scope?.Dispose(); }
        T Make<T>() where T : Component { var obj = new GameObject(typeof(T).Name); obj.SetActive(false); objects.Add(obj); return obj.AddComponent<T>(); }
        object Call(string method, params object[] args)
        { var info = typeof(InputHandler).GetMethod(method, Private); Assert.NotNull(info, "Missing actual UI reader route " + method); return info.Invoke(input, args); }
        string State => typeof(InputHandler).GetField("_inputState", Private).GetValue(input).ToString();
        void StateAs(string state) { var field = typeof(InputHandler).GetField("_inputState", Private); field.SetValue(input, Enum.Parse(field.FieldType, state)); }
        void Close() { input.AnnouncementUI.Close(); Call("CloseAnnouncement"); }
        string Read()
        { var lines = new List<string>(); for (int p = 0; p < input.AnnouncementUI.PageCount; p++) { input.AnnouncementUI.GoToPage(p); lines.AddRange(input.AnnouncementUI.VisibleLines); } return string.Join(" ", lines); }

        [TestCase("controls", false)] [TestCase("controls", true)]
        [TestCase("status", false)] [TestCase("status", true)]
        [TestCase("loot", false)] [TestCase("loot", true)]
        [TestCase("trade", false)] [TestCase("trade", true)]
        public void ReaderUsesActualSelectedContextPreservesWorldNoticeAndReturnsWithoutAction(string kind, bool pending)
        {
            string originalState = "Normal", expected = "Controls", method = "OpenControlsReader";
            Entity item = null;
            if (kind == "status")
            {
                actor.ApplyEffect(new HeartFlameEffect(2, 9)); Call("OpenInventory");
                originalState = "InventoryOpen"; expected = "Heart Flame"; method = "OpenPlayerStatusReader";
            }
            else if (kind == "loot")
            {
                item = factory.CreateEntity("Dagger"); item.GetPart<RenderPart>().DisplayName = "CLARITY_LOOT_SENTINEL";
                zone.AddEntity(item, 10, 10); input.PickupUI.Open(new List<Entity> { item }); StateAs("PickupOpen");
                originalState = "PickupOpen"; expected = "CLARITY_LOOT_SENTINEL"; method = "OpenPickupDetailsReader";
            }
            else if (kind == "trade")
            {
                var trader = factory.CreateEntity("Player"); item = factory.CreateEntity("Dagger");
                item.GetPart<RenderPart>().DisplayName = "CLARITY_STOCK_SENTINEL";
                trader.GetPart<InventoryPart>().AddObject(item); input.TradeUI.Open(trader); StateAs("TradeOpen");
                originalState = "TradeOpen"; expected = "CLARITY_STOCK_SENTINEL"; method = "OpenTradeDetailsReader";
            }
            MessageLog.Add("Existing combat observation.");
            if (pending) MessageLog.AddAnnouncement("Existing world notice.");
            var log = MessageLog.GetAllEntries(); var queue = MessageLog.GetPendingAnnouncementsSnapshot();
            int tick = turns.TickCount, energy = turns.GetEnergy(actor), quantity = item?.GetPart<StackerPart>()?.StackCount ?? 0;
            Call(method);
            Assert.AreEqual("AnnouncementOpen", State); StringAssert.Contains(expected, Read());
            CollectionAssert.AreEqual(log, MessageLog.GetAllEntries()); CollectionAssert.AreEqual(queue, MessageLog.GetPendingAnnouncementsSnapshot());
            Close();
            if (pending) { Assert.AreEqual("AnnouncementOpen", State); StringAssert.Contains("Existing world notice.", Read()); Close(); }
            Assert.AreEqual(originalState, State);
            Assert.AreEqual(tick, turns.TickCount); Assert.AreEqual(energy, turns.GetEnergy(actor));
            if (item != null) { Assert.False(actor.GetPart<InventoryPart>().Contains(item)); Assert.AreEqual(quantity, item.GetPart<StackerPart>().StackCount); }
            if (kind == "loot") Assert.True(input.PickupUI.IsOpen);
            if (kind == "trade") Assert.True(input.TradeUI.IsOpen);
        }

        [TestCase("actor")] [TestCase("zone")]
        public void ReplacingTheLiveContextCancelsReaderReturnInsteadOfReopeningStaleInventory(string change)
        {
            input.ZoneRenderer = Make<ZoneRenderer>();
            Call("OpenInventory"); Assert.True(input.ZoneRenderer.Paused);
            Call("OpenPlayerStatusReader"); Assert.AreEqual("AnnouncementOpen", State);
            if (change == "actor") input.PlayerEntity = factory.CreateEntity("Player");
            else input.CurrentZone = new Zone("other-reader-zone");
            Close(); Assert.AreEqual("Normal", State); Assert.False(input.InventoryUI.IsOpen);
            Assert.False(input.ZoneRenderer.Paused, "Returning to Normal must let the current world render again.");
        }
        [Test] public void MissingOrForeignSelectedLootCannotOpenAReader()
        {
            var item = factory.CreateEntity("Dagger"); zone.AddEntity(item, 10, 10);
            input.PickupUI.Open(new List<Entity> { item }); StateAs("PickupOpen");
            zone.RemoveEntity(item); actor.GetPart<InventoryPart>().AddObject(item);
            Call("OpenPickupDetailsReader"); Assert.AreEqual("PickupOpen", State); Assert.False(input.AnnouncementUI.IsOpen);
            Assert.False(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void SearchOwnsDestructiveAndReaderKeysUntilEscape()
        {
            var keyboardScope = new UnityEngine.InputSystem.InputTestFixture(); keyboardScope.Setup();
            try
            {
                var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>(); keyboard.MakeCurrent();
                var item = factory.CreateEntity("Dagger"); actor.GetPart<InventoryPart>().AddObject(item); Call("OpenInventory");
                typeof(InventoryUI).GetMethod("BeginInventorySearch", Private).Invoke(input.InventoryUI, null);
                foreach (var key in new[] { UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.F1, UnityEngine.InputSystem.Key.F2 })
                {
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update();
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key)); UnityEngine.InputSystem.InputSystem.Update();
                    Call("HandleInventoryInput");
                }
                Assert.AreEqual("d", input.InventoryUI.SearchQuery); Assert.True(input.InventoryUI.IsSearching);
                Assert.AreEqual("InventoryOpen", State); Assert.True(actor.GetPart<InventoryPart>().Contains(item)); Assert.False(input.AnnouncementUI.IsOpen);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape)); UnityEngine.InputSystem.InputSystem.Update();
                Call("HandleInventoryInput"); Assert.AreEqual("", input.InventoryUI.SearchQuery); Assert.False(input.InventoryUI.IsSearching); Assert.True(input.InventoryUI.IsOpen);
            }
            finally { keyboardScope.TearDown(); }
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualWaterTransferMenuPaysOnceOnlyWhenTheSelectedDestinationStillExists(bool stale)
        {
            var keyboardScope = new UnityEngine.InputSystem.InputTestFixture(); keyboardScope.Setup();
            try
            {
                var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>(); keyboard.MakeCurrent();
                var source = factory.CreateEntity("LiquidFlask"); var water = source.GetPart<LiquidVesselPart>(); water.Volume = 3; water.LiquidId = "water";
                var destination = factory.CreateEntity("Waterskin"); destination.GetPart<WaterskinPart>().Charges = 0;
                Assert.True(actor.GetPart<InventoryPart>().AddObject(source)); Assert.True(actor.GetPart<InventoryPart>().AddObject(destination));
                Call("OpenInventory"); Assert.True(input.InventoryUI.ReopenItemActionPopupFor(source));
                var popup = typeof(InventoryUI).GetField("_itemActionPopup", Private).GetValue(input.InventoryUI);
                var actions = (System.Collections.IList)popup.GetType().GetField("Actions").GetValue(popup); int index = -1;
                for (int j = 0; j < actions.Count; j++)
                    if (((string)actions[j].GetType().GetField("Command").GetValue(actions[j])).StartsWith("TransferWater|", StringComparison.Ordinal)) index = j;
                Assert.GreaterOrEqual(index, 0, "Actual carried vessels must expose transfer choice.");
                popup.GetType().GetField("CursorIndex").SetValue(popup, index);
                if (stale) actor.GetPart<InventoryPart>().RemoveObject(destination);
                int tick = turns.TickCount, energy = turns.GetEnergy(actor);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter)); UnityEngine.InputSystem.InputSystem.Update();
                Call("HandleInventoryInput");
                Assert.AreEqual(stale ? 3 : 0, water.Volume); Assert.AreEqual(stale ? 0 : 3, destination.GetPart<WaterskinPart>().Charges);
                Assert.AreEqual(stale ? "InventoryOpen" : "Normal", State);
                Assert.AreEqual(stale ? tick : tick + 10, turns.TickCount); Assert.False(input.InventoryUI.ConsumePendingEverydayTurn());
                if (stale) Assert.AreEqual(energy, turns.GetEnergy(actor));
                int paidTick = turns.TickCount;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update();
                Call("HandleInventoryInput"); Assert.AreEqual(paidTick, turns.TickCount);
            }
            finally { keyboardScope.TearDown(); }
        }

        [Test] public void PauseControlsReaderSurvivesTheSharedPopupCanvasClearAndReturnsToPauseSelection()
        {
            var keyboardScope = new UnityEngine.InputSystem.InputTestFixture(); keyboardScope.Setup();
            try
            {
                var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>(); keyboard.MakeCurrent();
                input.PauseMenuUI = Make<PauseMenuUI>();
                var fg = Make<UnityEngine.Tilemaps.Tilemap>(); var bg = Make<UnityEngine.Tilemaps.Tilemap>();
                input.AnnouncementUI.Tilemap = input.PauseMenuUI.Tilemap = fg;
                input.AnnouncementUI.BgTilemap = input.PauseMenuUI.BgTilemap = bg;
                var controller = input.PauseMenuUI.Controller; controller.Open(); controller.HoverSelect(PauseMenuController.ControlsIndex);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update();
                Call("Update"); Assert.True(input.PauseMenuUI.IsOpen);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter)); UnityEngine.InputSystem.InputSystem.Update();
                Call("Update"); Assert.AreEqual("AnnouncementOpen", State);
                int Count(UnityEngine.Tilemaps.Tilemap map) { int n = 0; foreach (var pos in map.cellBounds.allPositionsWithin) if (map.HasTile(pos)) n++; return n; }
                int originalFg = Count(fg), originalBg = Count(bg);
                Assert.Greater(originalFg, 0); Assert.Greater(originalBg, 0);
                input.AnnouncementUI.GoToPage(0); // A complete redraw is the expected pixel footprint.
                Assert.AreEqual(Count(fg), originalFg, "Pause must not erase any newly drawn reader glyphs.");
                Assert.AreEqual(Count(bg), originalBg, "Pause must not erase the reader backdrop.");
                Close(); Assert.AreEqual("Normal", State); Assert.True(input.PauseMenuUI.IsOpen);
                Assert.AreEqual(PauseMenuController.ControlsIndex, controller.SelectedIndex);
            }
            finally { keyboardScope.TearDown(); }
        }

        [Test] public void TradeConfirmationKeepsWholeStackPricePaintedForShortAndLongNames()
        {
            input.TradeUI.Tilemap = Make<UnityEngine.Tilemaps.Tilemap>();
            var trader = factory.CreateEntity("Player"); var item = factory.CreateEntity("Dagger");
            item.GetPart<StackerPart>().StackCount = 3; item.GetPart<CommercePart>().Value = 17;
            Assert.True(trader.GetPart<InventoryPart>().AddObject(item));
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(actor), trader);
            foreach (var name in new[] { "dagger", new string('W', 100) })
            {
                item.GetPart<RenderPart>().DisplayName = name; input.TradeUI.Open(trader);
                typeof(TradeUI).GetMethod("ShowConfirmation", Private).Invoke(input.TradeUI, null);
                string painted = "";
                for (int x = 22; x < 58; x++)
                {
                    var tile = input.TradeUI.Tilemap.GetTile(new Vector3Int(x, 24, 0)); char glyph = ' ';
                    for (int c = 33; c < 127; c++) if (tile == CP437TilesetGenerator.GetUiTile((char)c)) { glyph = (char)c; break; }
                    painted += glyph;
                }
                StringAssert.Contains(" for " + price + "$?", painted, "Truncate only the item name, never the transaction price.");
                Assert.True(trader.GetPart<InventoryPart>().Contains(item)); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            }
        }

    }
}
