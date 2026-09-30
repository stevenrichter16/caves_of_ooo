using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CurationReceivingInputTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class ActionProbe : Part
        {
            public override string Name => "CurationInputActionProbe";
            public int Before, After;
            public bool FailAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command") != CurationIntakePart.CertifyCommand) return true;
                if (e.ID == "BeforeInventoryAction") Before++;
                if (e.ID == "AfterInventoryAction")
                {
                    After++;
                    if (FailAfter) throw new InvalidOperationException("Curation input rollback witness");
                }
                return true;
            }
        }
        static void State(InputHandler input, string name, string value)
        {
            var field = typeof(InputHandler).GetField(name, Private);
            field.SetValue(input, Enum.Parse(field.FieldType, value));
        }
        static void Select(InputHandler input, WorldActionMenuUI menu, Entity index, Zone zone)
        {
            State(input, "_worldActionMenuReturnState", "Normal");
            typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Private)
                .Invoke(input, new object[] { index, zone.GetEntityCell(index), false });
            Assert.True(menu.IsOpen);
            var actions = (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(menu);
            var action = actions.Single(a => a.Command == CurationIntakePart.CertifyCommand);
            Assert.AreSame(index, menu.SelectedTarget);
            typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Private)
                .Invoke(input, new object[] { action, menu.SelectedTarget, menu.SelectedCell, menu.SelectedCellIsPile });
        }

        [TestCase("valid")]
        [TestCase("wrong-arrangement")]
        [TestCase("capacity")]
        [TestCase("after-callback")]
        public void NativeWorldMenuUsesOnePhysicalTransactionAndPaysOnlyForCommit(string condition)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = manager.GetZone("Overworld.12.12.0");
                var index = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationIntakeIndex");
                var intake = index.GetPart<CurationIntakePart>();
                var player = content.Factory.CreateEntity("Player");
                var at = zone.GetEntityPosition(index);
                Assert.True(zone.AddEntity(player, at.x, at.y + 1));
                manager.SetActiveZone(zone);
                if (condition != "wrong-arrangement")
                {
                    var first = zone.GetEntityPosition(intake.FirstBay);
                    var second = zone.GetEntityPosition(intake.SecondBay);
                    Assert.True(zone.MoveEntity(intake.FirstBody, first.x, first.y));
                    Assert.True(zone.MoveEntity(intake.SecondBody, second.x, second.y));
                }
                if (condition == "capacity") player.GetPart<InventoryPart>().MaxWeight = 0;
                var probe = new ActionProbe { FailAfter = condition == "after-callback" };
                player.AddPart(probe);
                var turns = new TurnManager();
                // Isolate the UI's payment from AI behavior; the Play route schedules the full yard.
                turns.RestoreSavedState(17, true, player, new List<TurnManager.SavedTurnEntry>
                    { new TurnManager.SavedTurnEntry { Entity = player, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("curation-input", "real-menu", manager, turns, player));
                var menu = ui.Root.AddComponent<WorldActionMenuUI>();
                ui.Input.WorldActionMenuUI = menu;
                var foil = intake.Counterfoil;
                int tick = turns.TickCount, energy = turns.GetEnergy(player), drams = TradeSystem.GetDrams(player);
                Select(ui.Input, menu, index, zone);
                Assert.AreEqual(1, probe.Before, "The actual selected menu command must enter the inventory transaction.");
                bool success = condition == "valid";
                Assert.AreEqual(success || condition == "after-callback" ? 1 : 0, probe.After);
                Assert.AreEqual(success, intake.Certified);
                Assert.AreEqual(success, player.GetPart<InventoryPart>().Objects.Contains(foil));
                Assert.AreEqual(!success, index.GetPart<InventoryPart>().Objects.Contains(foil));
                Assert.AreSame(success ? player : index, foil.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(drams, TradeSystem.GetDrams(player));
                Assert.AreEqual("Normal", typeof(InputHandler).GetField("_inputState", Private).GetValue(ui.Input).ToString());
                if (success)
                {
                    Assert.Greater(turns.TickCount, tick);
                    Assert.AreEqual(energy - TurnManager.ActionThreshold
                        + (turns.TickCount - tick) * player.GetStatValue("Speed", TurnManager.DefaultSpeed), turns.GetEnergy(player),
                        "Certification spends exactly one normal action, with ordinary scheduler refill.");
                    tick = turns.TickCount; energy = turns.GetEnergy(player);
                    Select(ui.Input, menu, index, zone);
                    Assert.AreEqual(2, probe.Before); Assert.AreEqual(1, probe.After);
                    Assert.AreEqual(1, player.GetPart<InventoryPart>().Objects.Count(e => e == foil));
                    Assert.True(intake.Certified); Assert.IsEmpty(index.GetPart<InventoryPart>().Objects);
                }
                Assert.AreEqual(tick, turns.TickCount, "Refusal or repeat must spend no time.");
                Assert.AreEqual(energy, turns.GetEnergy(player), "Refusal or repeat must spend no energy.");
            }
        }
    }
}
