using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>Real world-menu selection and keyboard inventory Apply. Only a
    /// committed preparation or dressing use pays one ordinary player action.</summary>
    public sealed class SoddenPreparationInputTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly InputTestFixture inputScope = new InputTestFixture();
        Keyboard keyboard;
        [SetUp] public void Setup() { inputScope.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); Release(); }
        [TearDown] public void Cleanup() { inputScope.TearDown(); }
        void Release() { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); }
        static object Get(object owner, string field) => owner.GetType().GetField(field, Flags).GetValue(owner);
        static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Flags).SetValue(owner, value);
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Flags).Invoke(owner, args);
        sealed class Probe : Part
        {
            public string Command, Condition;
            public int Before, After;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command") != Command) return true;
                if (e.ID == "BeforeInventoryAction") { Before++; if (Condition == "before") return false; }
                if (e.ID == "AfterInventoryAction") { After++; if (Condition == "after") throw new InvalidOperationException("sodden-native-input-rollback"); }
                return true;
            }
        }
        static int Units(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        static void Reveal(Zone zone, params Entity[] owners)
        { foreach (var owner in owners) { var cell = zone.GetEntityCell(owner); cell.IsVisible = true; cell.Explored = true; } }
        static TurnManager Turns(Entity actor)
        {
            var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor,
                new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = TurnManager.ActionThreshold } }); return turns;
        }
        [TestCase("valid")][TestCase("before")][TestCase("after")][TestCase("stale")]
        public void RealWorldMenuPreparesOnceOrRestoresInputsFeesAndTime(string condition)
        {
            using (var ui = new HotbarSaveFixture(true, false)) using (var content = new HaulingContentScope())
            {
                content.Seed(64); var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = new Zone("Overworld.15.7.0"); manager.SetActiveZone(zone);
                var actor = content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(actor, 4, 4)); TradeSystem.SetDrams(actor, 20);
                var worker = content.Factory.CreateEntity("PeatCutter"); Assert.True(zone.AddEntity(worker, 6, 4)); TradeSystem.SetDrams(worker, 7);
                var bench = content.Factory.CreateEntity("SoddenDressingBench"); Assert.True(zone.AddEntity(bench, 5, 4));
                bench.GetPart<RepairablePart>().Repaired = true; Assert.True(bench.GetPart<SoddenPreparationPart>().Configure(zone, worker)); Reveal(zone, actor, worker, bench);
                foreach (var name in new[] { "SumpsievePad", "KnotflaxCord" }) Assert.True(actor.GetPart<InventoryPart>().AddObject(content.Factory.CreateEntity(name)));
                var probe = new Probe { Command = SoddenPreparationPart.PrepareCommand, Condition = condition }; actor.AddPart(probe);
                var turns = Turns(actor); ui.BindOld(GameSessionState.Capture("sodden-input", "world-service", manager, turns, actor)); SeedPart.Factory = content.Factory;
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", Flags); state.SetValue(ui.Input, Enum.Parse(state.FieldType, "Normal"));
                Call(ui.Input, "OpenWorldActionMenuFor", bench, zone.GetEntityCell(bench), false); Assert.True(menu.IsOpen);
                var selected = ((List<InventoryAction>)Get(menu, "_actions")).Single(a => a.Command == SoddenPreparationPart.PrepareCommand);
                if (condition == "stale") bench.GetPart<RepairablePart>().Repaired = false;
                int beforeEnergy = turns.GetEnergy(actor); Call(ui.Input, "ExecuteWorldActionSelection", selected, bench, zone.GetEntityCell(bench), false);
                bool success = condition == "valid";
                Assert.AreEqual(1, probe.Before); Assert.AreEqual(success || condition == "after" ? 1 : 0, probe.After);
                Assert.AreEqual(success ? 1 : 0, Units(actor, "SoddenFieldDressing")); Assert.AreEqual(success ? 0 : 1, Units(actor, "SumpsievePad")); Assert.AreEqual(success ? 0 : 1, Units(actor, "KnotflaxCord"));
                Assert.AreEqual(success ? 18 : 20, TradeSystem.GetDrams(actor)); Assert.AreEqual(success ? 9 : 7, TradeSystem.GetDrams(worker));
                Assert.AreEqual(success ? 27 : 17, turns.TickCount);
                Assert.AreEqual(beforeEnergy - (success ? TurnManager.ActionThreshold : 0) + (turns.TickCount - 17) * actor.GetStatValue("Speed"), turns.GetEnergy(actor));
            }
        }
        [TestCase("valid")][TestCase("before")][TestCase("after")][TestCase("stale")]
        public void RealInventoryApplyTreatsOnceAndPaysOneActionOnlyAfterCommit(string condition)
        {
            using (var ui = new HotbarSaveFixture(true, false)) using (var content = new HaulingContentScope())
            {
                content.Seed(64); var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = new Zone("Overworld.15.7.0"); manager.SetActiveZone(zone);
                var actor = content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(actor, 4, 4));
                var dressing = content.Factory.CreateEntity("SoddenFieldDressing"); Assert.True(actor.GetPart<InventoryPart>().AddObject(dressing));
                // The bare Player blueprint receives status storage during gameplay bootstrap;
                // this isolated native input fixture must supply that same required owner.
                var status = actor.GetPart<StatusEffectsPart>();
                if (status == null) { status = new StatusEffectsPart(); actor.AddPart(status); }
                Assert.True(status.ApplyEffect(new PoisonedEffect())); Assert.True(status.ApplyEffect(new BleedingEffect()));
                int hp = actor.GetStatValue("Hitpoints");
                var probe = new Probe { Command = SoddenDressingPart.ApplyCommand, Condition = condition }; actor.AddPart(probe);
                var turns = Turns(actor); ui.BindOld(GameSessionState.Capture("sodden-input", "inventory-apply", manager, turns, actor)); ui.Input.WorldMap = manager.WorldMap;
                var grid = new GameObject("Sodden inventory grid"); grid.transform.SetParent(ui.Root.transform, false); grid.AddComponent<Grid>();
                var tiles = new GameObject("Sodden inventory tiles"); tiles.transform.SetParent(grid.transform, false);
                var inventory = ui.Root.AddComponent<InventoryUI>(); inventory.Tilemap = tiles.AddComponent<Tilemap>();
                inventory.PlayerEntity = actor; inventory.CurrentZone = zone; inventory.EntityFactory = content.Factory; ui.Input.InventoryUI = inventory;
                Call(ui.Input, "OpenInventory"); Assert.True(inventory.ReopenItemActionPopupFor(dressing));
                var popup = Get(inventory, "_itemActionPopup"); var actions = (IList)Get(popup, "Actions"); int index = -1;
                for (int i = 0; i < actions.Count; i++) if ((string)Get(actions[i], "Command") == SoddenDressingPart.ApplyCommand) index = i;
                Assert.That(index, Is.GreaterThanOrEqualTo(0)); Set(popup, "CursorIndex", index);
                if (condition == "stale") Assert.True(actor.GetPart<InventoryPart>().RemoveObject(dressing));
                int energy = turns.GetEnergy(actor); Release(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update();
                Assert.True(keyboard.enterKey.wasPressedThisFrame); Call(ui.Input, "HandleInventoryInput"); bool success = condition == "valid";
                Assert.AreEqual(!success, inventory.IsOpen, "A committed dressing use closes the popup and reaches native turn payment.");
                Assert.AreEqual(success ? "Normal" : "InventoryOpen", Get(ui.Input, "_inputState").ToString());
                Assert.False(inventory.ConsumePendingEverydayTurn(), "One input dispatch consumes the action signal once.");
                Assert.AreEqual(success || condition == "stale" ? 0 : 1, Units(actor, "SoddenFieldDressing"));
                Assert.AreEqual(!success, status.HasEffect<PoisonedEffect>()); Assert.AreEqual(!success, status.HasEffect<BleedingEffect>()); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
                Assert.AreEqual(success || condition == "after" ? 1 : 0, probe.After);
                if (success)
                {
                    Assert.Greater(turns.TickCount, 17); Assert.AreEqual(energy - TurnManager.ActionThreshold + (turns.TickCount - 17) * actor.GetStatValue("Speed"), turns.GetEnergy(actor));
                    int paid = turns.TickCount; Release(); Call(ui.Input, "HandleInventoryInput"); Assert.AreEqual(paid, turns.TickCount);
                }
                else { Assert.AreEqual(17, turns.TickCount); Assert.AreEqual(energy, turns.GetEnergy(actor)); Assert.AreSame(popup, Get(inventory, "_itemActionPopup")); }
                inventory.Close();
            }
        }
    }
}
