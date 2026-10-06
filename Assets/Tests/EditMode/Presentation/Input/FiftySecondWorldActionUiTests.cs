using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondWorldActionUiTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class RoutedPart : Part
        {
            public string Command; public bool Accept; public int Calls; public bool SawTransaction;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GetInventoryActions") e.GetParameter<InventoryActionList>("Actions")?.AddAction("physical", "physical choice", Command, '\0', 25);
                if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == Command)
                {
                    Calls++; SawTransaction = e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction") != null;
                    if (Accept) { e.Handled = true; return false; }
                }
                return true;
            }
        }
        [TestCase("CopyVolume|book", true)] [TestCase("CopyVolume|book", false)]
        [TestCase("ClaimGuestLocker", true)] [TestCase("ClaimGuestLocker", false)]
        [TestCase("StripClothScreen", true)] [TestCase("StripClothScreen", false)]
        [TestCase("CompostCrop|crop", true)] [TestCase("CompostCrop|crop", false)]
        [TestCase("HarvestCropSeeds", true)] [TestCase("HarvestCropSeeds", false)]
        [TestCase("TransplantCrop|zone|5|5", true)] [TestCase("TransplantCrop|zone|5|5", false)]
        public void WorldMenuUsesReceiptAndChargesExactlyOneSuccessfulAction(string command, bool accept)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            using (var content = new HaulingContentScope())
            {
                content.Seed(64); var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = new Zone("Overworld.11.10.0"); manager.SetActiveZone(zone);
                var actor = content.Factory.CreateEntity("Player"); zone.AddEntity(actor, 4, 4);
                var target = content.Factory.CreateEntity("PhysicalObject"); zone.AddEntity(target, 5, 4);
                var probe = new RoutedPart { Command = command, Accept = accept }; target.AddPart(probe);
                var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor,
                    new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("fifty-second-menu", "controlled-command-route", manager, turns, actor));
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", Hidden); state.SetValue(ui.Input, Enum.Parse(state.FieldType, "Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Hidden).Invoke(ui.Input, new object[] { target, zone.GetEntityCell(target), false });
                var action = ((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Hidden).GetValue(menu)).Single(a => a.Command == command);
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Hidden).Invoke(ui.Input, new object[] { action, target, zone.GetEntityCell(target), false });
                Assert.AreEqual(1, probe.Calls); Assert.True(probe.SawTransaction, "Physical actions require the rollback-capable command boundary.");
                Assert.AreEqual(accept ? 27 : 17, turns.TickCount);
            }
        }
    }
}
