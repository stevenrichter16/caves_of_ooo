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
    public sealed class PaidSelfConsumptionInputTests : FiftyWorldFixture
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
        [TestCase("Starapple", "Eat", false, false)] [TestCase("ToastedEmberwheat", "Eat", false, false)]
        [TestCase("HealingTonic", "ApplyTonic", false, false)] [TestCase("HealingTonic", "ApplyTonic", true, false)]
        [TestCase("FieldMeal", "Eat", false, false)]
        [TestCase("Starapple", "Eat", false, true)] [TestCase("HealingTonic", "ApplyTonic", true, true)]
        public void ActualControllerSelfUseSpendsOneUnitAndOneAction(string blueprint, string command, bool drink, bool keyboard)
        {
            var inputs = new InputTestFixture(); inputs.Setup();
            try { using(var ui = new HotbarSaveFixture(true,false)) {
                var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();var keys=InputSystem.AddDevice<Keyboard>();
                var manager = OverworldZoneManager.CreateDetached(Factory,64,true); manager.SetActiveZone(Zone);
                var item = Carry(blueprint); if(item.GetPart<TonicPart>() is TonicPart tonic) tonic.Drink=drink;
                if(item.GetPart<StackerPart>() is StackerPart stack) stack.StackCount=2;
                var witness = new Entity{BlueprintName="turn-witness"};witness.AddPart(new PhysicsPart());var observer=new TurnObserver();witness.AddPart(observer);Assert.True(Zone.AddEntity(witness,14,10));
                Clock.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold},new TurnManager.SavedTurnEntry{Entity=witness,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("paid-self-use","native-confirmation",manager,Clock,Actor));
                var inventory=ui.Root.AddComponent<InventoryUI>();inventory.PlayerEntity=Actor;inventory.CurrentZone=Zone;inventory.EntityFactory=Factory;ui.Input.InventoryUI=inventory;
                State(pad,ui.Input,new GamepadState());Call(ui.Input,"OpenInventory");Assert.True(inventory.ReopenItemActionPopupFor(item));
                var popup=Field(inventory,"_itemActionPopup");var actions=(IList)Field(popup,"Actions");int selected=-1;
                for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")==command)selected=i;
                Assert.GreaterOrEqual(selected,0);popup.GetType().GetField("CursorIndex",Hidden|BindingFlags.Public).SetValue(popup,selected);
                State(pad,ui.Input,new GamepadState());Assert.True(inventory.IsOpen);
                int tick=Clock.TickCount,energy=Clock.GetEnergy(Actor),quantity=item.GetPart<StackerPart>()?.StackCount??1;
                Confirm(pad,keys,ui.Input,keyboard);
                Assert.AreEqual(quantity-1, Pack.Objects.Contains(item)?item.GetPart<StackerPart>()?.StackCount??1:0,"an actual self-use consumed one unit");
                Assert.False(inventory.IsOpen,"successful self-use closes inventory to let the world respond");
                Assert.Greater(observer.Turns,0,"native confirmation lets another registered actor act");Assert.Greater(Clock.TickCount,tick);Assert.AreEqual(energy-TurnManager.ActionThreshold+(Clock.TickCount-tick)*Actor.GetStatValue("Speed",TurnManager.DefaultSpeed),Clock.GetEnergy(Actor));
                int afterTick=Clock.TickCount;Confirm(pad,keys,ui.Input,keyboard);Assert.AreEqual(afterTick,Clock.TickCount,"held A cannot consume or pay twice");
            }} finally{inputs.TearDown();}
        }
        static void Confirm(Gamepad pad,Keyboard keys,InputHandler input,bool keyboard)
        { if(!keyboard){State(pad,input,new GamepadState().WithButton(GamepadButton.South));return;}
            InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Enter));InputSystem.Update();typeof(InputHandler).GetField("_lastMoveTime",Hidden).SetValue(input,-999f);typeof(InputHandler).GetField("_lastWaitTime",Hidden).SetValue(input,-999f);Call(input,"Update"); }
        sealed class TurnObserver:Part {public int Turns;public override bool HandleEvent(GameEvent e){if(e.ID=="TakeTurn")Turns++;return true;}}
        sealed class Refusal:Part
        {
            readonly string reason;public int Calls;public Refusal(string reason){this.reason=reason;}
            public override bool HandleEvent(GameEvent e){if(reason=="veto"&&e.ID=="BeforeInventoryAction"){Calls++;return false;}if(reason=="handled"&&e.ID=="InventoryAction"){Calls++;e.Handled=true;return false;}return true;}
        }
        [TestCase("Starapple","cancel")] [TestCase("Starapple","lost")] [TestCase("Starapple","empty")] [TestCase("Starapple","veto")] [TestCase("Starapple","after")] [TestCase("Starapple","handled")]
        [TestCase("HealingTonic","cancel")] [TestCase("HealingTonic","lost")] [TestCase("HealingTonic","empty")] [TestCase("HealingTonic","veto")] [TestCase("HealingTonic","after")] [TestCase("HealingTonic","handled")]
        public void RefusedCancelledOrMerelyHandledNativeActionsAreFree(string blueprint,string reason)
        {
            var inputs=new InputTestFixture();inputs.Setup();
            try{using(var ui=new HotbarSaveFixture(true,false)){
                var pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);
                var item=Carry(blueprint);if(item.GetPart<StackerPart>() is StackerPart stack)stack.StackCount=2;
                Clock.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("paid-self-use","native-refusal",manager,Clock,Actor));
                var inventory=ui.Root.AddComponent<InventoryUI>();inventory.PlayerEntity=Actor;inventory.CurrentZone=Zone;inventory.EntityFactory=Factory;ui.Input.InventoryUI=inventory;
                State(pad,ui.Input,new GamepadState());Call(ui.Input,"OpenInventory");Assert.True(inventory.ReopenItemActionPopupFor(item));
                string command=blueprint=="Starapple"?"Eat":"ApplyTonic";var popup=Field(inventory,"_itemActionPopup");var actions=(IList)Field(popup,"Actions");int selected=-1;
                for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")==command)selected=i;
                Assert.GreaterOrEqual(selected,0);popup.GetType().GetField("CursorIndex",Hidden|BindingFlags.Public).SetValue(popup,selected);State(pad,ui.Input,new GamepadState());
                var refusal=new Refusal(reason);if(reason=="veto")Actor.AddPart(refusal);if(reason=="after")FailAfter();
                if(reason=="handled"){item.AddPart(refusal);item.Parts.Remove(refusal);item.Parts.Insert(0,refusal);}
                if(reason=="lost")Pack.RemoveObject(item);if(reason=="empty")item.GetPart<StackerPart>().StackCount=0;
                int tick=Clock.TickCount,energy=Clock.GetEnergy(Actor),quantity=item.GetPart<StackerPart>().StackCount;
                State(pad,ui.Input,new GamepadState().WithButton(reason=="cancel"?GamepadButton.East:GamepadButton.South));
                Assert.AreEqual(tick,Clock.TickCount);Assert.AreEqual(energy,Clock.GetEnergy(Actor));Assert.AreEqual(quantity,item.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(reason!="lost",Pack.Objects.Contains(item));if(reason=="handled"||reason=="veto")Assert.AreEqual(1,refusal.Calls,"real refusal/handled hook was reached");
            }}finally{inputs.TearDown();}
        }
    }
}
