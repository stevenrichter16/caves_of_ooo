using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class ContainerCurrencyInputTests : ContainerCurrencyFixture
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method,Hidden).Invoke(owner,args);
        static object Field(object owner,string name) => owner.GetType().GetField(name,Hidden|BindingFlags.Public).GetValue(owner);
        static void State(Gamepad pad,InputHandler input,GamepadState value)
        {
            InputSystem.QueueStateEvent(pad,value); InputSystem.Update();
            // Synthetic EditMode states do not advance Time.time.
            typeof(InputHandler).GetField("_lastMoveTime",Hidden).SetValue(input,-999f);
            typeof(InputHandler).GetField("_lastWaitTime",Hidden).SetValue(input,-999f); Call(input,"Update");
        }
        [TestCase("success")] [TestCase("locked")] [TestCase("overflow")] [TestCase("after")]
        public void ActualControllerContainerLootCreditsOnlyCommittedCoins(string condition)
        {
            var inputs=new InputTestFixture();inputs.Setup();
            try{using(var ui=new HotbarSaveFixture(true,false)){
                var pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);var coin=Coin();
                Clock.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("container-currency","native-confirmation",manager,Clock,Actor));
                var pickup=ui.Root.AddComponent<PickupUI>();ui.Input.PickupUI=pickup;State(pad,ui.Input,new GamepadState());Call(ui.Input,"OpenContainerLoot",Box,Contents);Assert.True(pickup.IsOpen);State(pad,ui.Input,new GamepadState());
                if(condition=="locked")Contents.Locked=true;if(condition=="overflow")TradeSystem.SetDrams(Actor,int.MaxValue);if(condition=="after")coin.AddPart(new Hook{Action=e=>throw new InvalidOperationException("native currency callback failure")});
                int tick=Clock.TickCount,energy=Clock.GetEnergy(Actor),purse=TradeSystem.GetDrams(Actor);State(pad,ui.Input,new GamepadState().WithButton(GamepadButton.South));bool success=condition=="success";
                Assert.AreEqual(success?purse+15:purse,TradeSystem.GetDrams(Actor));Assert.AreEqual(!success,Contents.Contents.Contains(coin));Assert.False(Pack.Objects.Contains(coin));Assert.AreEqual(success?0:3,coin.GetPart<StackerPart>().StackCount);Assert.AreEqual(!success,pickup.IsOpen);
                if(success){Assert.Greater(Clock.TickCount,tick);Assert.AreEqual(energy-TurnManager.ActionThreshold+(Clock.TickCount-tick)*Actor.GetStatValue("Speed",TurnManager.DefaultSpeed),Clock.GetEnergy(Actor));}else{Assert.AreEqual(tick,Clock.TickCount);Assert.AreEqual(energy,Clock.GetEnergy(Actor));}
                int after=TradeSystem.GetDrams(Actor);State(pad,ui.Input,new GamepadState().WithButton(GamepadButton.South));Assert.AreEqual(after,TradeSystem.GetDrams(Actor),"held confirm cannot mint again");
            }}finally{inputs.TearDown();}
        }
    }
}
