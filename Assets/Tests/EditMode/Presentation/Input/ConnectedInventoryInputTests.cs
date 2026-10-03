using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual inventory popup selection via an isolated keyboard, then
    /// native InputHandler turn payment in the same frame. No live game grant.</summary>
    public sealed class ConnectedInventoryInputTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly InputTestFixture keyboardScope = new InputTestFixture();
        Keyboard keyboard;
        [SetUp] public void Setup() { keyboardScope.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); Release(); }
        [TearDown] public void Cleanup() { keyboardScope.TearDown(); }
        void Release() { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); }
        static object Get(object owner, string field) => owner.GetType().GetField(field, Flags).GetValue(owner);
        static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Flags).SetValue(owner, value);
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Flags).Invoke(owner, args);
        sealed class ActionProbe : Part
        {
            public string Command, Condition;
            public int Before, After;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command") != Command) return true;
                if (e.ID == "BeforeInventoryAction") { Before++; if (Condition == "before") return false; }
                if (e.ID == "AfterInventoryAction") { After++; if (Condition == "after") throw new InvalidOperationException("Connected inventory rollback witness"); }
                return true;
            }
        }
        [TestCase("meal", "valid")] [TestCase("meal", "before")] [TestCase("meal", "after")] [TestCase("meal", "stale")]
        [TestCase("reink", "valid")] [TestCase("reink", "before")] [TestCase("reink", "after")] [TestCase("reink", "stale")]
        [TestCase("ordinary-food", "valid")]
        public void RealInventorySelectionChargesExactlyOneActionOnlyForCommittedConnectedUses(string feature, string condition)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            using (var f = new ConnectedKitchenFixture())
            {
                var manager = OverworldZoneManager.CreateDetached(f.Factory, 64, true); manager.SetActiveZone(f.Zone);
                var player = f.Player;
                player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Max = 1000, Owner = player };
                f.Clock.RestoreSavedState(17, true, player, new List<TurnManager.SavedTurnEntry>
                    { new TurnManager.SavedTurnEntry { Entity = player, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("connected-input", "inventory", manager, f.Clock, player));
                ui.Input.WorldMap = manager.WorldMap;
                var grid = new GameObject("Connected inventory grid"); grid.transform.SetParent(ui.Root.transform, false); grid.AddComponent<Grid>();
                var tiles = new GameObject("Connected inventory tiles"); tiles.transform.SetParent(grid.transform, false);
                var inventory = ui.Root.AddComponent<InventoryUI>(); inventory.Tilemap = tiles.AddComponent<Tilemap>();
                inventory.PlayerEntity = player; inventory.CurrentZone = f.Zone; inventory.EntityFactory = f.Factory;
                ui.Input.InventoryUI = inventory;
                var item = f.Give(feature == "reink" ? "ShatteredRimeGrimoire" : feature == "meal" ? "FieldMeal" : "ToastedEmberwheat", 1);
                var charge = item.GetPart<GrimoireChargePart>(); Entity ink = null;
                if (feature == "reink") { Assert.NotNull(charge); charge.Charges = 2; ink = f.Give("InkVial", 1); }
                player.GetStat("Hitpoints").BaseValue = 10;
                string command = feature == "reink" ? GrimoireChargePart.ReinkCommand : "Eat";
                var probe = new ActionProbe { Command = command, Condition = condition }; player.AddPart(probe);
                Call(ui.Input, "OpenInventory"); Assert.True(inventory.ReopenItemActionPopupFor(item));
                var popup = Get(inventory, "_itemActionPopup"); var actions = (IList)Get(popup, "Actions");
                int index = -1;
                for (int i = 0; i < actions.Count; i++) if ((string)Get(actions[i], "Command") == command) index = i;
                Assert.That(index, Is.GreaterThanOrEqualTo(0), "The real carried item must expose its actual action.");
                Set(popup, "CursorIndex", index);
                if (condition == "stale") Assert.True(f.Pack.RemoveObject(feature == "reink" ? ink : item));
                int tick = f.Clock.TickCount, energy = f.Clock.GetEnergy(player);
                Release(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update();
                Assert.True(keyboard.enterKey.wasPressedThisFrame);
                Call(ui.Input, "HandleInventoryInput");
                bool success = condition == "valid", paid = success && feature != "ordinary-food";
                Assert.AreEqual(!paid, inventory.IsOpen);
                Assert.AreEqual(paid ? "Normal" : "InventoryOpen", Get(ui.Input, "_inputState").ToString());
                Assert.False(inventory.ConsumePendingEverydayTurn(), "The same dispatch frame consumes the signal once.");
                if (feature == "reink")
                {
                    Assert.AreEqual(success ? 7 : 2, charge.Charges);
                    Assert.AreEqual(success || condition == "stale" ? 0 : 1, f.Units("InkVial"));
                    Assert.True(f.Pack.Objects.Contains(item));
                }
                else
                {
                    Assert.AreEqual(!success && condition != "stale", f.Pack.Objects.Contains(item));
                    if (success) Assert.Greater(player.GetStatValue("Hitpoints"), 10);
                    else Assert.AreEqual(10, player.GetStatValue("Hitpoints"));
                }
                if (condition != "stale") Assert.AreEqual(1, probe.Before);
                Assert.AreEqual(success || condition == "after" ? 1 : 0, probe.After);
                if (paid)
                {
                    Assert.Greater(f.Clock.TickCount, tick);
                    Assert.AreEqual(energy - TurnManager.ActionThreshold + (f.Clock.TickCount - tick) * player.GetStatValue("Speed"), f.Clock.GetEnergy(player));
                    int paidTick = f.Clock.TickCount, paidEnergy = f.Clock.GetEnergy(player);
                    Release(); Call(ui.Input, "HandleInventoryInput");
                    Assert.AreEqual(paidTick, f.Clock.TickCount); Assert.AreEqual(paidEnergy, f.Clock.GetEnergy(player), "Rechecking a closed menu cannot charge another action.");
                }
                else { Assert.AreEqual(tick, f.Clock.TickCount); Assert.AreEqual(energy, f.Clock.GetEnergy(player)); }
                if (!success) Assert.AreSame(popup, Get(inventory, "_itemActionPopup"), "Refusal preserves the menu for retry.");
                inventory.Close();
            }
        }
    }
}
