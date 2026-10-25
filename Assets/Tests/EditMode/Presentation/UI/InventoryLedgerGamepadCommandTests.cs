using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    // Native input regressions exercise the shipping InventoryUI dispatcher,
    // not direct ExecuteItemAction/command delegates. No Play/Deck claim.
    public sealed class InventoryLedgerGamepadCommandTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly InputTestFixture inputScope = new InputTestFixture();
        Gamepad pad;
        Keyboard keyboard;

        [SetUp] public void Setup()
        {
            inputScope.Setup(); pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            State(new GamepadState()); NativeGamepadInput.SetContext(GamepadInputContext.Menu); State(new GamepadState());
        }
        [TearDown] public void Cleanup()
        { NativeGamepadInput.SetContext(GamepadInputContext.Menu); inputScope.TearDown(); }
        void State(GamepadState state)
        { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); InputHelper.GetKey(KeyCode.None); }
        void Keys(params Key[] keys)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); InputHelper.GetKey(KeyCode.None); }
        void Neutral()
        { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); InputHelper.GetKey(KeyCode.None); }

        [Test]
        public void WestOpensCraftingCommandsAndDpadAChooseTheActuallyOfferedBrewMode()
        {
            using (var r = new Rig())
            {
                r.Open(false); Set(r.UI, "_panel", 4); SetEnum(r.UI, "_craftingMode", "Forge"); Call(r.UI, "Render");
                int tick = TurnManager.Active?.TickCount ?? 0;
                State(new GamepadState().WithButton(GamepadButton.West));
                Assert.True(InputHelper.GetKeyDown(KeyCode.I), "Existing native Menu mapping is the source of X's synthetic I.");
                Assert.False(InputHelper.GetKeyboardKeyDown(KeyCode.I));
                r.UI.HandleInput();
                Assert.True(r.UI.IsOpen, "X must open the Commands view instead of closing the crafting screen.");
                Assert.True((bool)Get(r.UI, "_ledgerCommandsOpen"));
                var commands = (IList)Get(r.UI, "_ledgerCommands"); Assert.Greater(commands.Count, 0);
                int brew = -1;
                for (int i = 0; i < commands.Count; i++)
                    if (string.Equals((string)Get(commands[i], "Label"), "Brew", StringComparison.OrdinalIgnoreCase)) brew = i;
                Assert.GreaterOrEqual(brew, 0, "The real context menu must offer Brew; do not invoke its callback directly.");
                Assert.AreEqual("Forge", Get(r.UI, "_craftingMode").ToString());
                Assert.AreEqual(0, Get(r.UI, "_ledgerCommandIndex"));
                // A held X cannot reopen/toggle/commit the newly opened modal.
                State(new GamepadState().WithButton(GamepadButton.West)); r.UI.HandleInput();
                Assert.True((bool)Get(r.UI, "_ledgerCommandsOpen")); Assert.AreEqual(0, Get(r.UI, "_ledgerCommandIndex"));
                for (int i = 0; i < brew; i++)
                { Neutral(); State(new GamepadState().WithButton(GamepadButton.DpadDown)); r.UI.HandleInput(); }
                Assert.AreEqual(brew, Get(r.UI, "_ledgerCommandIndex"));
                Neutral(); State(new GamepadState().WithButton(GamepadButton.South)); r.UI.HandleInput();
                Assert.AreEqual("Brew", Get(r.UI, "_craftingMode").ToString());
                Assert.False((bool)Get(r.UI, "_ledgerCommandsOpen")); Assert.True(r.UI.IsOpen);
                Assert.Zero(r.Probe.Executed.Count); Assert.True(r.Pack.Contains(r.Item));
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.AreEqual(tick, TurnManager.Active?.TickCount ?? 0);
            }
        }

        [Test]
        public void NativeBClosesOnlyCommandsAndLeavesTheSameInventorySelection()
        {
            using (var r = new Rig())
            {
                r.Open(false); Set(r.UI, "_panel", 4); SetEnum(r.UI, "_craftingMode", "Forge"); Call(r.UI, "Render");
                State(new GamepadState().WithButton(GamepadButton.West)); r.UI.HandleInput();
                Assert.True(r.UI.IsOpen); Assert.True((bool)Get(r.UI, "_ledgerCommandsOpen"));
                Neutral(); State(new GamepadState().WithButton(GamepadButton.East)); r.UI.HandleInput();
                Assert.False((bool)Get(r.UI, "_ledgerCommandsOpen")); Assert.True(r.UI.IsOpen);
                Assert.AreEqual(4, Get(r.UI, "_panel")); Assert.AreEqual("Forge", Get(r.UI, "_craftingMode").ToString());
                Assert.Zero(r.Probe.Executed.Count); Assert.True(r.Pack.Contains(r.Item)); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void WestCannotBecomeTheNinthLetterActionInsideAnExistingItemPopup()
        {
            using (var r = new Rig())
            {
                r.Open(true); var popup = Get(r.UI, "_itemActionPopup");
                Assert.AreEqual("LedgerProbe8", r.CommandAt(8)); Assert.AreEqual(0, Get(popup, "CursorIndex"));
                int tick = TurnManager.Active?.TickCount ?? 0;
                State(new GamepadState().WithButton(GamepadButton.West));
                Assert.True(InputHelper.GetKeyDown(KeyCode.I)); Assert.False(InputHelper.GetKeyboardKeyDown(KeyCode.I));
                r.UI.HandleInput();
                Assert.Zero(r.Probe.Executed.Count, "The synthesized I must not dispatch ordinal action 9.");
                Assert.AreSame(popup, Get(r.UI, "_itemActionPopup"), "X is inert in an existing nested popup.");
                Assert.AreEqual(0, Get(popup, "CursorIndex")); Assert.True(r.UI.IsOpen);
                State(new GamepadState().WithButton(GamepadButton.West)); r.UI.HandleInput();
                Assert.Zero(r.Probe.Executed.Count); Assert.True(r.Pack.Contains(r.Item));
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.AreEqual(tick, TurnManager.Active?.TickCount ?? 0);
            }
        }

        [Test]
        public void PhysicalKeyboardIStillExecutesTheNinthActionExactlyOnce()
        {
            using (var r = new Rig())
            {
                r.Open(true); Assert.AreEqual("LedgerProbe8", r.CommandAt(8));
                Assert.AreEqual(0, Get(Get(r.UI, "_itemActionPopup"), "CursorIndex"));
                Keys(Key.I); Assert.True(InputHelper.GetKeyboardKeyDown(KeyCode.I)); r.UI.HandleInput();
                CollectionAssert.AreEqual(new[] { 8 }, r.Probe.Executed);
                Assert.IsNull(Get(r.UI, "_itemActionPopup")); Assert.True(r.UI.IsOpen);
                Keys(Key.I); r.UI.HandleInput(); CollectionAssert.AreEqual(new[] { 8 }, r.Probe.Executed);
                Assert.True(r.UI.IsOpen); Assert.True(r.Pack.Contains(r.Item)); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void NativeAStillConfirmsTheSelectedNinthActionExactlyOnce()
        {
            using (var r = new Rig())
            {
                r.Open(true); Assert.AreEqual("LedgerProbe8", r.CommandAt(8));
                Set(Get(r.UI, "_itemActionPopup"), "CursorIndex", 8);
                State(new GamepadState().WithButton(GamepadButton.South)); r.UI.HandleInput();
                CollectionAssert.AreEqual(new[] { 8 }, r.Probe.Executed); Assert.IsNull(Get(r.UI, "_itemActionPopup"));
                State(new GamepadState().WithButton(GamepadButton.South)); r.UI.HandleInput();
                CollectionAssert.AreEqual(new[] { 8 }, r.Probe.Executed);
                Assert.True(r.UI.IsOpen); Assert.True(r.Pack.Contains(r.Item)); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void PhysicalKeyboardIStillClosesTheBaseInventoryWithAGamepadConnected()
        {
            using (var r = new Rig())
            {
                r.Open(false); Assert.IsNull(Get(r.UI, "_itemActionPopup"));
                Keys(Key.I); Assert.True(InputHelper.GetKeyboardKeyDown(KeyCode.I)); r.UI.HandleInput();
                Assert.False(r.UI.IsOpen); Assert.Zero(r.Probe.Executed.Count); Assert.True(r.Pack.Contains(r.Item));
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void CommandsExposeCharacterStatusForEveryPanelAndTransferTheRequestOnce(int panel)
        {
            using (var r = new Rig())
            {
                r.Open(false); Set(r.UI, "_panel", panel); Call(r.UI, "Render");
                var owner = r.UI.PlayerEntity; var zone = r.UI.CurrentZone;
                var cell = zone.GetEntityCell(owner);
                int tick = TurnManager.Active?.TickCount ?? 0;
                int carried = r.Pack.Objects.Count;
                State(new GamepadState().WithButton(GamepadButton.West)); r.UI.HandleInput();
                Assert.True(r.UI.IsOpen); Assert.True((bool)Get(r.UI, "_ledgerCommandsOpen"));
                var commands = (IList)Get(r.UI, "_ledgerCommands");
                int status = -1;
                for (int i = 0; i < commands.Count; i++)
                    if (string.Equals((string)Get(commands[i], "Label"), "Character status", StringComparison.OrdinalIgnoreCase)) status = i;
                Assert.GreaterOrEqual(status, 0, "Every panel must offer the real Character status command before testing its new request seam.");
                var consume = r.UI.GetType().GetMethod("ConsumeStatusRequest", BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(consume, "InputHandler must consume the free status-reader request explicitly.");
                Assert.False((bool)Get(r.UI, "_ledgerStatusRequested"), "Opening Commands does not request status.");
                Assert.False((bool)consume.Invoke(r.UI, null));
                Neutral(); State(new GamepadState().WithButton(GamepadButton.East)); r.UI.HandleInput();
                Assert.False((bool)Get(r.UI, "_ledgerCommandsOpen")); Assert.True(r.UI.IsOpen);
                Assert.False((bool)Get(r.UI, "_ledgerStatusRequested")); Assert.False((bool)consume.Invoke(r.UI, null));
                Assert.AreEqual(panel, Get(r.UI, "_panel"));

                Neutral(); State(new GamepadState().WithButton(GamepadButton.West)); r.UI.HandleInput();
                Assert.True((bool)Get(r.UI, "_ledgerCommandsOpen"));
                Assert.AreEqual("Character status", (string)Get(((IList)Get(r.UI, "_ledgerCommands"))[status], "Label"));
                for (int i = 0; i < status; i++)
                { Neutral(); State(new GamepadState().WithButton(GamepadButton.DpadDown)); r.UI.HandleInput(); }
                Assert.AreEqual(status, Get(r.UI, "_ledgerCommandIndex"));
                Neutral(); State(new GamepadState().WithButton(GamepadButton.South)); r.UI.HandleInput();
                Assert.True((bool)Get(r.UI, "_ledgerStatusRequested"));
                Assert.False((bool)Get(r.UI, "_ledgerCommandsOpen")); Assert.True(r.UI.IsOpen);
                Assert.True((bool)consume.Invoke(r.UI, null));
                Assert.False((bool)Get(r.UI, "_ledgerStatusRequested")); Assert.False((bool)consume.Invoke(r.UI, null));
                State(new GamepadState().WithButton(GamepadButton.South)); r.UI.HandleInput();
                Assert.False((bool)consume.Invoke(r.UI, null), "A held confirm must not issue a second status request.");
                Assert.AreSame(owner, r.UI.PlayerEntity); Assert.AreSame(zone, r.UI.CurrentZone);
                Assert.AreSame(cell, zone.GetEntityCell(owner)); Assert.AreEqual(panel, Get(r.UI, "_panel"));
                Assert.AreEqual(carried, r.Pack.Objects.Count); Assert.True(r.Pack.Contains(r.Item));
                Assert.Zero(r.Probe.Executed.Count); Assert.False(r.UI.ConsumePendingEverydayTurn());
                Assert.IsNull(r.UI.ConsumePendingThrowRequest()); Assert.AreEqual(tick, TurnManager.Active?.TickCount ?? 0);
            }
        }

        sealed class NineActions : Part
        {
            public override string Name => "LedgerNineActionProbe";
            public readonly List<int> Executed = new List<int>();
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GetInventoryActions")
                {
                    var actions = e.GetParameter<InventoryActionList>("Actions");
                    if (actions != null) for (int i = 0; i < 9; i++)
                        actions.AddAction("LedgerProbe" + i, "test action " + (i + 1), "LedgerProbe" + i, '\0', 100 - i);
                    return true;
                }
                if (e.ID != "InventoryAction") return true;
                string command = e.GetStringParameter("Command");
                if (command == null || !command.StartsWith("LedgerProbe", StringComparison.Ordinal)
                    || !int.TryParse(command.Substring("LedgerProbe".Length), out int index)) return true;
                Executed.Add(index); e.Handled = true; return false;
            }
        }

        sealed class Rig : IDisposable
        {
            readonly HotbarSaveFixture saved;
            public readonly InventoryUI UI;
            public readonly Entity Player, Item;
            public readonly NineActions Probe;
            public InventoryPart Pack => Player.GetPart<InventoryPart>();
            public Rig()
            {
                saved = new HotbarSaveFixture(false, false);
                try
                {
                    var factory = new EntityFactory(); var content = Resources.Load<TextAsset>("Content/Blueprints/Objects"); Assert.NotNull(content); factory.LoadBlueprints(content.text);
                    Player = factory.CreateEntity("Player"); Assert.NotNull(Player);
                    var zone = new Zone("InventoryLedgerCommands"); Assert.True(zone.AddEntity(Player, 10, 10));
                    Item = new Entity { ID = "ledger-command-probe", BlueprintName = "LedgerCommandProbe" };
                    Item.AddPart(new RenderPart { DisplayName = "command probe", RenderString = "!" });
                    Item.AddPart(new PhysicsPart { Takeable = true, Weight = 1 }); Item.SetTag("Item");
                    Probe = new NineActions(); Item.AddPart(Probe); Assert.True(Pack.AddObject(Item));
                    var grid = new GameObject("Ledger controls grid", typeof(Grid)); grid.transform.SetParent(saved.Root.transform, false);
                    var tiles = new GameObject("Ledger controls tiles", typeof(Tilemap)); tiles.transform.SetParent(grid.transform, false);
                    UI = saved.Root.AddComponent<InventoryUI>(); UI.Tilemap = tiles.GetComponent<Tilemap>();
                    UI.PlayerEntity = Player; UI.CurrentZone = zone; UI.EntityFactory = factory;
                }
                catch { Dispose(); throw; }
            }
            public void Open(bool popup)
            {
                UI.Open(); Assert.True(UI.IsOpen);
                if (popup)
                {
                    Assert.True(UI.ReopenItemActionPopupFor(Item));
                    for (int i = 0; i < 9; i++) Assert.AreEqual("LedgerProbe" + i, CommandAt(i), "Real action discovery preserves nine ordered probe actions before utility rows.");
                }
            }
            public string CommandAt(int index)
            { var popup = Get(UI, "_itemActionPopup"); Assert.NotNull(popup); var actions = (IList)Get(popup, "Actions"); Assert.Greater(actions.Count, index); return (string)Get(actions[index], "Command"); }
            public void Dispose() { UI?.Close(); saved?.Dispose(); }
        }
        static object Get(object owner, string name)
        { Assert.NotNull(owner); var field = owner.GetType().GetField(name, Members); Assert.NotNull(field, "Expected private UI seam: " + name); return field.GetValue(owner); }
        static void Set(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, Members); Assert.NotNull(field); field.SetValue(owner, value); }
        static void SetEnum(object owner, string name, string value)
        { var field = owner.GetType().GetField(name, Members); Assert.NotNull(field); field.SetValue(owner, Enum.Parse(field.FieldType, value)); }
        static void Call(object owner, string name) => owner.GetType().GetMethod(name, Members).Invoke(owner, null);
    }
}
