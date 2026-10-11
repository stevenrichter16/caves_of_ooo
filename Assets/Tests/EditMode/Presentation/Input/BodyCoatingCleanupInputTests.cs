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
    public sealed class BodyCoatingCleanupInputTests : BodyCoatingCleanupFixture
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
        [TestCase("partial")] [TestCase("full")] [TestCase("stale")] [TestCase("after")]
        public void ActualControllerConfirmationPaysOnlyCommittedBodyCleanup(string condition)
        {
            var inputs = new InputTestFixture(); inputs.Setup();
            try { using(var ui = new HotbarSaveFixture(true,false)) {
                var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
                var manager = OverworldZoneManager.CreateDetached(Factory,64,true); manager.SetActiveZone(Zone);
                var target = Friend(); var coat = Coat(target,"pitch",condition == "full" ? 10 : 35); var pith = Carry("PrismreedPith");
                Clock.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("body-cleanup","native-confirmation",manager,Clock,Actor));
                var inventory=ui.Root.AddComponent<InventoryUI>();inventory.PlayerEntity=Actor;inventory.CurrentZone=Zone;inventory.EntityFactory=Factory;ui.Input.InventoryUI=inventory;
                State(pad,ui.Input,new GamepadState());Call(ui.Input,"OpenInventory");Assert.True(inventory.ReopenItemActionPopupFor(pith));
                string command=Pick(pith,target); var popup=Field(inventory,"_itemActionPopup");var actions=(IList)Field(popup,"Actions");int selected=-1;
                for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")==command)selected=i;
                Assert.GreaterOrEqual(selected,0);popup.GetType().GetField("CursorIndex",Hidden|BindingFlags.Public).SetValue(popup,selected);
                State(pad,ui.Input,new GamepadState());Assert.True(inventory.IsOpen);
                if(condition=="stale")coat.Amount++;if(condition=="after")FailAfter();
                int tick=Clock.TickCount,energy=Clock.GetEnergy(Actor),amount=coat.Amount;
                State(pad,ui.Input,new GamepadState().WithButton(GamepadButton.South));bool success=condition=="partial"||condition=="full";
                Assert.AreEqual(!success,inventory.IsOpen);Assert.AreEqual(!success,Pack.Objects.Contains(pith));
                Assert.AreEqual(success?Math.Max(0,amount-20):amount,coat.Amount);
                Assert.AreEqual(condition!="full",target.HasEffect<LiquidCoveredEffect>());
                if(success){Assert.Greater(Clock.TickCount,tick);Assert.AreEqual(energy-TurnManager.ActionThreshold+(Clock.TickCount-tick)*Actor.GetStatValue("Speed",TurnManager.DefaultSpeed),Clock.GetEnergy(Actor));}
                else{Assert.AreEqual(tick,Clock.TickCount);Assert.AreEqual(energy,Clock.GetEnergy(Actor));}
                int afterTick=Clock.TickCount;State(pad,ui.Input,new GamepadState().WithButton(GamepadButton.South));Assert.AreEqual(afterTick,Clock.TickCount,"held A cannot repeat the completed or refused action");
            }} finally{inputs.TearDown();}
        }
    }
}
