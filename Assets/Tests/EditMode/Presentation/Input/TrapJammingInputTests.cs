using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual world-menu dispatch in a controlled cached zone. Checks
    /// exact material and scheduler cost; the ordinary expedition is separate.</summary>
    public sealed class TrapJammingInputTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class Probe : Part
        {
            public int Before, After; public string Condition;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command") != "JamTrap") return true;
                if (e.ID == "BeforeInventoryAction") { Before++; if (Condition == "veto") return false; }
                if (e.ID == "AfterInventoryAction") { After++; if (Condition == "rollback") throw new InvalidOperationException("jam menu rollback"); }
                return true;
            }
        }
        [TestCase("success")] [TestCase("missing")] [TestCase("veto")]
        [TestCase("rollback")] [TestCase("stale")] [TestCase("hidden")]
        public void ActualSelectedMenuPaysOneActionOnlyForCommittedJam(string condition)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = new Zone("Overworld.11.10.0"); manager.SetActiveZone(zone);
                var player = content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(player, 4, 4));
                var trap = content.Factory.CreateEntity("SpikeTrap"); Assert.True(zone.AddEntity(trap, 5, 4));
                var cell = zone.GetEntityCell(trap); cell.IsVisible = cell.Explored = true;
                var timber = content.Factory.CreateEntity("SalvagedTimber"); timber.GetPart<StackerPart>().StackCount = 2;
                if (condition != "missing") Assert.True(player.GetPart<InventoryPart>().AddObject(timber));
                var probe = new Probe { Condition = condition }; player.AddPart(probe);
                var turns = new TurnManager();
                turns.RestoreSavedState(17, true, player, new List<TurnManager.SavedTurnEntry>
                    { new TurnManager.SavedTurnEntry { Entity = player, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("trap-input", "actual-menu", manager, turns, player));
                SettlementRuntime.ActiveZone = zone;
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                var returnState = typeof(InputHandler).GetField("_worldActionMenuReturnState", Private);
                returnState.SetValue(ui.Input, Enum.Parse(returnState.FieldType, "Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Private).Invoke(ui.Input, new object[] { trap, cell, false });
                Assert.True(menu.IsOpen); Assert.AreSame(trap, menu.SelectedTarget);
                var actions = (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(menu);
                var action = actions.Single(a => a.Command == "JamTrap");
                int tick = turns.TickCount, energy = turns.GetEnergy(player);
                if (condition == "stale") zone.RemoveEntity(trap);
                if (condition == "hidden") cell.IsVisible = false;
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Private).Invoke(ui.Input,
                    new object[] { action, menu.SelectedTarget, menu.SelectedCell, menu.SelectedCellIsPile });
                bool success = condition == "success";
                // A removed target fails the menu's shared reach gate before any
                // inventory command. Current reachable targets enter the transaction.
                Assert.AreEqual(condition == "stale" ? 0 : 1, probe.Before,
                    "Only current reachable resource actions enter the shared transaction.");
                Assert.AreEqual(success || condition == "rollback" ? 1 : 0, probe.After);
                Assert.AreEqual(success, trap.GetPart<TrapJammingPart>().Jammed);
                Assert.AreEqual(success ? 1 : 2, timber.GetPart<StackerPart>().StackCount);
                if (condition != "missing") Assert.AreSame(player, timber.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual("Normal", typeof(InputHandler).GetField("_inputState", Private).GetValue(ui.Input).ToString());
                if (success)
                {
                    Assert.Greater(turns.TickCount, tick);
                    Assert.AreEqual(energy - TurnManager.ActionThreshold + (turns.TickCount - tick)
                        * player.GetStatValue("Speed", TurnManager.DefaultSpeed), turns.GetEnergy(player));
                }
                else { Assert.AreEqual(tick, turns.TickCount); Assert.AreEqual(energy, turns.GetEnergy(player)); }
            }
        }
    }
}
