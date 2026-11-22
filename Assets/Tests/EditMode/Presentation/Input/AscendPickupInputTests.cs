using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class AscendPickupInputTests : FiftyWorldFixture
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        void Press(Keyboard keyboard, InputHandler input, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            typeof(InputHandler).GetField("_lastMoveTime", Hidden).SetValue(input, -999f);
            typeof(InputHandler).GetField("_lastWaitTime", Hidden).SetValue(input, -999f);
            typeof(InputHandler).GetMethod("Update", Hidden).Invoke(input, null);
        }

        void Bind(HotbarSaveFixture ui, bool underground)
        {
            Zone.RemoveEntity(Actor);
            Zone = new Zone(underground ? "Overworld.4.9.1" : "Overworld.4.9.0");
            Assert.True(Zone.AddEntity(Actor, 10, 10));
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.CachedZones[WorldMap.WorldMapZoneID] = new Zone(WorldMap.WorldMapZoneID);
            manager.SetActiveZone(Zone);
            Clock.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry>
            {
                new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold }
            });
            ui.BindOld(GameSessionState.Capture("ascend-input", "isolated-keyboard", manager, Clock, Actor));
            ui.Input.PickupUI = ui.Root.AddComponent<PickupUI>();
        }

        [TestCase(Key.LeftShift, false, false)] [TestCase(Key.RightShift, false, false)]
        [TestCase(Key.LeftShift, true, false)] [TestCase(Key.RightShift, true, false)]
        [TestCase(Key.LeftShift, false, true)] [TestCase(Key.RightShift, false, true)]
        public void AscendChordLeavesUnderfootStockAndPaysOnlyForSuccessfulTravel(Key shift, bool blocked, bool multiple)
        {
            var inputs = new InputTestFixture(); inputs.Setup();
            try
            {
                using (var ui = new HotbarSaveFixture(true, false))
                {
                    Bind(ui, blocked);
                    var stock = Place("PitchpodResin", 10, 10);
                    stock.GetPart<StackerPart>().StackCount = 6;
                    var other = multiple ? Place("FireMoss", 10, 10) : null;
                    var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                    Press(keyboard, ui.Input);
                    Press(keyboard, ui.Input, shift, Key.Comma);
                    Assert.NotNull(Zone.GetEntityCell(stock), "Ascending must not pick up the stack.");
                    Assert.AreSame(stock, Zone.GetCell(10, 10).Objects.Find(e => e == stock));
                    Assert.AreEqual(6, stock.GetPart<StackerPart>().StackCount);
                    Assert.False(Pack.Objects.Contains(stock));
                    if (other != null) Assert.NotNull(Zone.GetEntityCell(other));
                    Assert.False(ui.Input.PickupUI.IsOpen, "The ascent chord must not leave a pickup modal open.");
                    Assert.AreEqual("Normal", typeof(InputHandler).GetField("_inputState", Hidden).GetValue(ui.Input).ToString());
                    Assert.AreEqual(blocked ? Zone.ZoneID : WorldMap.WorldMapZoneID, ui.Input.CurrentZone.ZoneID);
                    Assert.AreEqual(blocked ? 17 : 27, Clock.TickCount);
                }
            }
            finally { inputs.TearDown(); }
        }

        [TestCase(Key.Comma, Key.None)] [TestCase(Key.G, Key.None)]
        [TestCase(Key.G, Key.LeftShift)] [TestCase(Key.G, Key.RightShift)]
        public void OrdinaryPickupKeysStillTakeOneStackAndPayOneAction(Key key, Key modifier)
        {
            var inputs = new InputTestFixture(); inputs.Setup();
            try
            {
                using (var ui = new HotbarSaveFixture(true, false))
                {
                    Bind(ui, false);
                    var stock = Place("PitchpodResin", 10, 10);
                    stock.GetPart<StackerPart>().StackCount = 6;
                    var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                    Press(keyboard, ui.Input);
                    Press(keyboard, ui.Input, modifier == Key.None ? new[] { key } : new[] { modifier, key });
                    Assert.True(Pack.Objects.Contains(stock));
                    Assert.IsNull(Zone.GetEntityCell(stock));
                    Assert.AreEqual(6, stock.GetPart<StackerPart>().StackCount);
                    Assert.AreSame(Zone, ui.Input.CurrentZone);
                    Assert.AreEqual(27, Clock.TickCount);
                }
            }
            finally { inputs.TearDown(); }
        }
    }
}
