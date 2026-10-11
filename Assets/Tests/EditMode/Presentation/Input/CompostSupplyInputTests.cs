using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class CompostSupplyInputTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class Probe : Part
        {
            public int Before, After; public bool Refuse, ThrowAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command")?.StartsWith("CompostCrop|", StringComparison.Ordinal) != true) return true;
                if (e.ID == "BeforeInventoryAction") { Before++; if (Refuse) return false; }
                if (e.ID == "AfterInventoryAction") { After++; if (ThrowAfter) throw new InvalidOperationException("native compost rollback"); }
                return true;
            }
        }
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Hidden).Invoke(owner, args);
        static object Field(object owner, string name) => owner.GetType().GetField(name, Hidden | BindingFlags.Public).GetValue(owner);
        static void State(Gamepad pad, InputHandler input, GamepadState value)
        {
            InputSystem.QueueStateEvent(pad, value); InputSystem.Update();
            // EditMode input updates do not advance Unity's frame clock. Model
            // a new input opportunity, as QudControllerGameplayTests does,
            // while retaining real device events and the full Update route.
            typeof(InputHandler).GetField("_lastMoveTime", Hidden).SetValue(input, -999f);
            typeof(InputHandler).GetField("_lastWaitTime", Hidden).SetValue(input, -999f);
            Call(input, "Update");
        }
        [TestCase("valid")] [TestCase("stale")] [TestCase("before")] [TestCase("after")]
        public void RealInventoryPlantTurnLeavesDryCropReadyForPaidWorldCompost(string condition)
        {
            var inputs = new InputTestFixture(); inputs.Setup();
            try
            {
                using (var ui = new HotbarSaveFixture(true, false))
                using (var content = new HaulingContentScope())
                {
                    content.Seed(64); var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
                    var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                    var zone = new Zone("Overworld.11.10.0"); manager.SetActiveZone(zone);
                    var player = content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(player, 4, 4));
                    var terrain = content.Factory.CreateEntity("Grass"); terrain.SetTag("Plantable"); terrain.AddPart(new CultivatedSoilPart()); Assert.True(zone.AddEntity(terrain, 5, 4));
                    var seller = MorrowfastContent.CreateResident("southern-food-vendor", content.Factory); TradeSystem.SetDrams(player, 10000);
                    foreach (var item in seller.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == "InertSludge" || i.BlueprintName == "KnotflaxSeed").ToArray())
                        Assert.True(TradeSystem.BuyFromTrader(player, seller, item));
                    var pack = player.GetPart<InventoryPart>(); Assert.AreEqual(2, pack.Objects.Where(i => i.BlueprintName == "InertSludge").Sum(i => i.GetPart<StackerPart>()?.StackCount ?? 1));
                    var seed = pack.Objects.First(i => i.BlueprintName == "KnotflaxSeed");
                    var turns = new TurnManager(); turns.RestoreSavedState(17, true, player, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = player, Energy = TurnManager.ActionThreshold } });
                    ui.BindOld(GameSessionState.Capture("compost-input", "plant-then-compost", manager, turns, player));
                    SeedPart.Factory = CropSystem.Factory = content.Factory; SettlementRuntime.ActiveZone = zone;
                    foreach (var cell in zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
                    var inventory = ui.Root.AddComponent<InventoryUI>(); inventory.PlayerEntity = player; inventory.CurrentZone = zone; inventory.EntityFactory = content.Factory; ui.Input.InventoryUI = inventory;
                    State(pad, ui.Input, new GamepadState()); Call(ui.Input, "OpenInventory"); Assert.True(inventory.ReopenItemActionPopupFor(seed));
                    var popup = Field(inventory, "_itemActionPopup"); var actions = (IList)Field(popup, "Actions"); int selected = -1;
                    for (int i = 0; i < actions.Count; i++) if (((string)Field(actions[i], "Command")).EndsWith("|5|4", StringComparison.Ordinal)) selected = i;
                    Assert.GreaterOrEqual(selected, 0); popup.GetType().GetField("CursorIndex", Hidden | BindingFlags.Public).SetValue(popup, selected);
                    State(pad, ui.Input, new GamepadState()); int plantingTick = turns.TickCount, plantingEnergy = turns.GetEnergy(player);
                    Assert.True(inventory.IsOpen); Assert.AreEqual("InventoryOpen", Field(ui.Input, "_inputState").ToString());
                    Assert.AreSame(popup, Field(inventory, "_itemActionPopup"), "neutral release must retain the selected planting action");
                    Assert.False(zone.GetCell(5, 4).Objects.Any(e => e.HasPart<CropPart>()), "no crop before controller confirmation");
                    State(pad, ui.Input, new GamepadState().WithButton(GamepadButton.South));
                    Assert.False(inventory.IsOpen, "controller A must confirm the paid planting action and close inventory");
                    Assert.AreEqual("Normal", Field(ui.Input, "_inputState").ToString());
                    Assert.Greater(turns.TickCount, plantingTick, "actual inventory confirmation must pay a planting turn");
                    Assert.AreEqual(plantingEnergy - TurnManager.ActionThreshold + (turns.TickCount - plantingTick) * player.GetStatValue("Speed", TurnManager.DefaultSpeed), turns.GetEnergy(player));
                    var crop = zone.GetCell(5, 4).Objects.Single(e => e.HasPart<CropPart>()); var part = crop.GetPart<CropPart>();
                    Assert.AreEqual(0, part.MoistureTicks); Assert.AreEqual(0, part.TicksInStage); Assert.AreEqual(0, part.GrowthWetTickRemainder);
                    State(pad, ui.Input, new GamepadState());
                    var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                    var returnState = typeof(InputHandler).GetField("_worldActionMenuReturnState", Hidden); returnState.SetValue(ui.Input, Enum.Parse(returnState.FieldType, "Normal"));
                    Call(ui.Input, "OpenWorldActionMenuFor", crop, zone.GetEntityCell(crop), false); Assert.True(menu.IsOpen);
                    var action = ((List<InventoryAction>)Field(menu, "_actions")).First(a => a.Command.StartsWith("CompostCrop|", StringComparison.Ordinal));
                    var probe = new Probe { Refuse = condition == "before", ThrowAfter = condition == "after" }; player.AddPart(probe);
                    if (condition == "stale") part.GrowthStage = 1;
                    int tick = turns.TickCount, energy = turns.GetEnergy(player), duration = part.TicksPerStage;
                    Call(ui.Input, "ExecuteWorldActionSelection", action, crop, zone.GetEntityCell(crop), false);
                    bool success = condition == "valid";
                    Assert.AreEqual(1, probe.Before); Assert.AreEqual(success || condition == "after" ? 1 : 0, probe.After);
                    Assert.AreEqual(success ? 1 : 2, pack.Objects.Where(i => i.BlueprintName == "InertSludge").Sum(i => i.GetPart<StackerPart>()?.StackCount ?? 1));
                    Assert.AreEqual(success, part.Composted); Assert.AreEqual(success ? (duration * 3 + 3) / 4 : duration, part.TicksPerStage);
                    Assert.AreEqual(0, part.MoistureTicks);
                    if (success) { Assert.Greater(turns.TickCount, tick); Assert.AreEqual(energy - TurnManager.ActionThreshold + (turns.TickCount - tick) * player.GetStatValue("Speed", TurnManager.DefaultSpeed), turns.GetEnergy(player)); }
                    else { Assert.AreEqual(tick, turns.TickCount); Assert.AreEqual(energy, turns.GetEnergy(player)); }
                }
            }
            finally { inputs.TearDown(); }
        }
    }
}
