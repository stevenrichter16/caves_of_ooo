using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MundaneCropWateringInputSaveTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Prefix = "WaterCrop|";

        sealed class Fixture : IDisposable
        {
            public readonly HotbarSaveFixture UI = new HotbarSaveFixture(true, false);
            public readonly HaulingContentScope Content = new HaulingContentScope();
            public readonly OverworldZoneManager Manager;
            public readonly Zone Zone;
            public readonly Entity Player, Crop, Vessel;
            public readonly TurnManager Turns;
            public readonly WorldActionMenuUI Menu;
            public readonly string Command;
            public Fixture(string kind)
            {
                Content.Seed(64);
                Manager = OverworldZoneManager.CreateDetached(Content.Factory, 64, true);
                Zone = new Zone("Overworld.11.10.0"); Manager.SetActiveZone(Zone);
                Player = Content.Factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(Player, 4, 4));
                var terrain = Content.Factory.CreateEntity("Grass"); terrain.SetTag("Plantable");
                terrain.AddPart(new CultivatedSoilPart()); Assert.True(Zone.AddEntity(terrain, 5, 4));
                Crop = Content.Factory.CreateEntity("KnotflaxCrop"); Assert.True(Zone.AddEntity(Crop, 5, 4));
                Vessel = Content.Factory.CreateEntity(kind == "flask" ? "LiquidFlask" : "DrawgourdShell");
                if (kind == "flask") { Vessel.GetPart<LiquidVesselPart>().LiquidId = "water"; Vessel.GetPart<LiquidVesselPart>().Volume = 3; }
                else Vessel.GetPart<WaterskinPart>().Charges = 3;
                Assert.True(Player.GetPart<InventoryPart>().AddObject(Vessel));
                Command = Prefix + Uri.EscapeDataString(Vessel.ID);
                Turns = new TurnManager(); Turns.RestoreSavedState(17, true, Player,
                    new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold } });
                UI.BindOld(GameSessionState.Capture("mundane-watering", "actual-menu-save", Manager, Turns, Player));
                SettlementRuntime.ActiveZone = Zone; CropSystem.Factory = Content.Factory;
                Assert.True(CropTime.Reconcile(Crop.GetPart<CropPart>(), Zone, 17));
                Menu = UI.Root.AddComponent<WorldActionMenuUI>(); UI.Input.WorldActionMenuUI = Menu;
            }
            public int Units => Vessel.GetPart<WaterskinPart>()?.Charges ?? Vessel.GetPart<LiquidVesselPart>().Volume;
            public InventoryAction Open()
            {
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", Private);
                state.SetValue(UI.Input, Enum.Parse(state.FieldType, "Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Private)
                    .Invoke(UI.Input, new object[] { Crop, Zone.GetEntityCell(Crop), false });
                Assert.True(Menu.IsOpen); Assert.AreSame(Crop, Menu.SelectedTarget);
                var action = ((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(Menu))
                    .SingleOrDefault(a => a.Command == Command);
                Assert.NotNull(action, "The native crop menu must expose exact carried-water selection.");
                return action;
            }
            public void Select(InventoryAction action) => typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Private)
                .Invoke(UI.Input, new object[] { action, Menu.SelectedTarget, Menu.SelectedCell, Menu.SelectedCellIsPile });
            public void Empty()
            {
                if (Vessel.GetPart<WaterskinPart>() is WaterskinPart skin) skin.Charges = 0;
                else { Vessel.GetPart<LiquidVesselPart>().Volume = 0; Vessel.GetPart<LiquidVesselPart>().LiquidId = ""; }
            }
            public void Dispose() { Content.Dispose(); UI.Dispose(); }
        }

        sealed class Probe : Part
        {
            public int Before, After; public bool RefuseBefore, FailAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command")?.StartsWith(Prefix, StringComparison.Ordinal) != true) return true;
                if (e.ID == "BeforeInventoryAction") { Before++; if (RefuseBefore) return false; }
                if (e.ID == "AfterInventoryAction") { After++; if (FailAfter) throw new InvalidOperationException("Watering outer rollback witness."); }
                return true;
            }
        }

        [TestCase("skin", "valid")] [TestCase("flask", "valid")]
        [TestCase("skin", "empty")] [TestCase("flask", "empty")]
        [TestCase("skin", "before")] [TestCase("flask", "before")]
        [TestCase("skin", "after")] [TestCase("flask", "after")]
        public void ActualWorldSelectionPaysOneActionOnlyAfterCommittedIrrigation(string kind, string condition)
        {
            using (var f = new Fixture(kind))
            {
                var probe = new Probe { RefuseBefore = condition == "before", FailAfter = condition == "after" }; f.Player.AddPart(probe);
                int tick = f.Turns.TickCount, energy = f.Turns.GetEnergy(f.Player);
                var selected = f.Open();
                Assert.AreEqual(tick, f.Turns.TickCount); Assert.AreEqual(3, f.Units);
                if (condition == "empty") f.Empty();
                f.Select(selected);
                bool success = condition == "valid";
                Assert.AreEqual(1, probe.Before, "Selection must use the shared command transaction.");
                Assert.AreEqual(success || condition == "after" ? 1 : 0, probe.After);
                Assert.AreEqual(success ? 2 : condition == "empty" ? 0 : 3, f.Units);
                var crop = f.Crop.GetPart<CropPart>();
                Assert.AreEqual(success, crop.MoistureTicks > 0);
                Assert.False(f.Zone.GetCell(5, 4).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
                Assert.AreEqual("Normal", typeof(InputHandler).GetField("_inputState", Private).GetValue(f.UI.Input).ToString());
                if (success)
                {
                    Assert.Greater(f.Turns.TickCount, tick);
                    Assert.AreEqual(energy - TurnManager.ActionThreshold + (f.Turns.TickCount - tick)
                        * f.Player.GetStatValue("Speed", TurnManager.DefaultSpeed), f.Turns.GetEnergy(f.Player));
                }
                else { Assert.AreEqual(tick, f.Turns.TickCount); Assert.AreEqual(energy, f.Turns.GetEnergy(f.Player)); }
            }
        }

        [Test] public void CancellingTheRealMenuDoesNotWaterOrChargeTime()
        {
            using (var f = new Fixture("skin"))
            {
                int energy = f.Turns.GetEnergy(f.Player); f.Open();
                typeof(WorldActionMenuUI).GetMethod("Cancel", Private).Invoke(f.Menu, null);
                Assert.False(f.Menu.IsOpen); Assert.AreEqual(17, f.Turns.TickCount);
                Assert.AreEqual(energy, f.Turns.GetEnergy(f.Player)); Assert.AreEqual(3, f.Units);
                Assert.AreEqual(0, f.Crop.GetPart<CropPart>().MoistureTicks);
            }
        }

        [TestCase("skin", false)] [TestCase("skin", true)]
        [TestCase("flask", false)] [TestCase("flask", true)]
        public void FullSaveGraphPreservesOnlyPaidWaterAndContinuesItsWetFraction(string kind, bool watered)
        {
            using (var f = new Fixture(kind))
            {
                if (watered) Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(f.Crop, f.Command), f.Player, f.Zone).Success);
                f.Turns.AdvanceClock(9); Assert.True(CropTime.Reconcile(f.Crop.GetPart<CropPart>(), f.Zone, f.Turns.TickCount));
                var loaded = HotbarSaveFixture.RoundTrip(f.UI.Capture());
                var zone = loaded.ZoneManager.ActiveZone;
                var cropOwner = zone.GetReadOnlyEntities().Single(e => e.ID == f.Crop.ID);
                var crop = cropOwner.GetPart<CropPart>();
                var vessel = loaded.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == f.Vessel.ID);
                Assert.AreNotSame(f.Crop, cropOwner); Assert.AreNotSame(f.Vessel, vessel);
                Assert.AreEqual(watered ? 2 : 3, vessel.GetPart<WaterskinPart>()?.Charges ?? vessel.GetPart<LiquidVesselPart>().Volume);
                Assert.AreSame(loaded.Player, vessel.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(26, crop.LastGrowthWorldTick); Assert.AreEqual(watered ? 9 : 0, crop.GrowthWetTickRemainder);
                Assert.AreEqual(watered ? 40 : 0, crop.MoistureTicks); Assert.AreEqual(0, crop.TicksInStage);
                Assert.True(CultivatedSoilPart.IsCultivated(zone, zone.GetEntityCell(cropOwner)));
                Assert.False(zone.GetEntityCell(cropOwner).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
                Assert.True(CropTime.Reconcile(crop, zone, 27));
                Assert.AreEqual(watered ? 1 : 0, crop.TicksInStage); Assert.AreEqual(watered ? 39 : 0, crop.MoistureTicks);
                Assert.AreEqual(0, crop.GrowthWetTickRemainder);
            }
        }
    }
}
