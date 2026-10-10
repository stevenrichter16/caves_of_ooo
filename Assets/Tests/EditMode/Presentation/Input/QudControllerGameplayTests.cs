using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class QudControllerGameplayTests
    {
        readonly InputTestFixture scope = new InputTestFixture();
        Gamepad pad; GameObject go; InputHandler input; Entity player; Zone zone; TurnManager turns;
        [SetUp] public void Setup()
        {
            scope.Setup(); FactionManager.Initialize(); MessageLog.Clear();
            pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            go = new GameObject("Owned Qud controller test"); go.SetActive(false);
            input = go.AddComponent<InputHandler>();
            input.WorldActionMenuUI = go.AddComponent<WorldActionMenuUI>();
            player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player"); player.SetTag("Creature");
            player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new InventoryPart());
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            zone = new Zone("ControllerTest"); zone.AddEntity(player, 10, 10);
            for (int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++)
            { zone.GetCell(x,y).IsVisible=true; zone.GetCell(x,y).Explored=true; }
            turns = new TurnManager(); turns.AddEntity(player); turns.ProcessUntilPlayerTurn();
            input.PlayerEntity=player; input.CurrentZone=zone; input.TurnManager=turns;
            State(new GamepadState());
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(go); FactionManager.Reset(); scope.TearDown(); }
        void State(GamepadState value)
        {
            InputSystem.QueueStateEvent(pad,value); InputSystem.Update();
            Set("_lastMoveTime", -999f); Set("_lastWaitTime", -999f);
            Invoke("Update");
        }
        object Invoke(string name, params object[] args)
        { var m=typeof(InputHandler).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic); Assert.NotNull(m,name); return m.Invoke(input,args); }
        void Set(string name,object value) => typeof(InputHandler).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(input,value);
        string Mode => typeof(InputHandler).GetField("_inputState",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input).ToString();

        [TestCase(-1,-1)] [TestCase(0,-1)] [TestCase(1,-1)] [TestCase(-1,0)]
        [TestCase(1,0)] [TestCase(-1,1)] [TestCase(0,1)] [TestCase(1,1)]
        public void StickOnlySelectsThenRightTriggerCommitsOneStep(int dx,int dy)
        {
            long tick=turns.TickCount;
            State(new GamepadState {leftStick=new Vector2(dx,-dy)});
            Assert.AreEqual((10,10),zone.GetEntityPosition(player)); Assert.AreEqual(tick,turns.TickCount);
            State(new GamepadState {leftStick=new Vector2(dx,-dy),rightTrigger=1});
            Assert.AreEqual((10+dx,10+dy),zone.GetEntityPosition(player)); Assert.Greater(turns.TickCount,tick);
        }
        [TestCase(true)] [TestCase(false)]
        public void AttackConfirmationAcceptsAAndBCancels(bool accept)
        {
            var target = new Entity { BlueprintName = "Neutral" };
            target.SetTag("Creature"); target.AddPart(new PhysicsPart {Solid=true});
            target.AddPart(new RenderPart {DisplayName="neutral"});
            var brain=new BrainPart {Passive=true}; target.AddPart(brain);
            target.Statistics["Hitpoints"]=new Stat {Name="Hitpoints",BaseValue=100,Max=100};
            zone.AddEntity(target,11,10);
            Invoke("OpenAttackConfirmation", target); State(new GamepadState());
            long tick=turns.TickCount;
            State(new GamepadState().WithButton(accept ? GamepadButton.South : GamepadButton.East));
            Assert.AreEqual("Normal",Mode);
            Assert.AreEqual(accept, brain.IsPersonallyHostileTo(player));
            Assert.AreEqual(accept, turns.TickCount > tick);
        }
        [Test] public void ControllerCanBindAnAbilityWithoutKeyboardNumbers()
        {
            var abilities=new ActivatedAbilitiesPart(); player.AddPart(abilities);
            var id=abilities.AddAbility("Test ability","TestCommand","test");
            input.AbilityManagerUI=go.AddComponent<AbilityManagerUI>();
            Invoke("OpenAbilityManager"); State(new GamepadState());
            State(new GamepadState().WithButton(GamepadButton.North));
            Assert.AreEqual("ControllerMenu",Mode);
            State(new GamepadState()); State(new GamepadState().WithButton(GamepadButton.DpadDown));
            State(new GamepadState()); State(new GamepadState().WithButton(GamepadButton.South));
            Assert.AreEqual(id,abilities.SlotAssignments[1]); Assert.AreEqual(Guid.Empty,abilities.SlotAssignments[0]);
            Assert.AreEqual("AbilityManagerOpen",Mode);
        }

        [Test] public void ForceAttackCommitsHostilityEvenWhenAttackIsVetoed()
        {
            player.AddPart(new CancelAttackPart());
            var target = Creature("neutral", true); var brain = target.GetPart<BrainPart>();
            zone.AddEntity(target,11,10); long tick=turns.TickCount;
            Invoke("ControllerForceAttack",1,0);
            Assert.IsTrue(brain.IsPersonallyHostileTo(player));
            Assert.AreEqual(100,target.GetStatValue("Hitpoints")); Assert.Greater(turns.TickCount,tick);
        }
        [TestCase(true)] [TestCase(false)]
        public void NearestAttackRequiresVisibleOwner(bool visible)
        {
            player.AddPart(new CancelAttackPart());
            var target=Creature("enemy",visible); target.GetPart<BrainPart>().SetPersonallyHostile(player);
            zone.AddEntity(target,11,10); long tick=turns.TickCount;
            Invoke("ControllerAttackNearest"); Assert.AreEqual(visible,turns.TickCount>tick);
        }
        [Test] public void HeldTriggerCannotSpendTurnAfterPlayerRebind()
        {
            State(new GamepadState{leftStick=Vector2.right,rightTrigger=1});
            var loaded=Creature("Player",true); loaded.SetTag("Player");
            zone.RemoveEntity(player); zone.AddEntity(loaded,20,10); player=loaded;
            turns=new TurnManager(); turns.AddEntity(player); turns.ProcessUntilPlayerTurn();
            input.PlayerEntity=player; input.TurnManager=turns; long tick=turns.TickCount;
            State(new GamepadState{leftStick=Vector2.right,rightTrigger=1});
            Assert.AreEqual((20,10),zone.GetEntityPosition(player)); Assert.AreEqual(tick,turns.TickCount);
            State(new GamepadState()); State(new GamepadState{leftStick=Vector2.right,rightTrigger=1});
            Assert.AreEqual((21,10),zone.GetEntityPosition(player)); Assert.Greater(turns.TickCount,tick);
        }
        Entity Creature(string name,bool visible)
        {
            var target=new Entity{BlueprintName=name}; target.SetTag("Creature");
            target.AddPart(new PhysicsPart{Solid=true}); target.AddPart(new RenderPart{DisplayName=name,Visible=visible});
            target.AddPart(new BrainPart{Passive=true});
            target.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=100,Max=100};
            target.Statistics["Speed"]=new Stat{Name="Speed",BaseValue=100,Min=25,Max=200};
            return target;
        }

        [TestCase(false,false,true)] [TestCase(true,false,false)] [TestCase(true,true,true)]
        public void InterestExcludesOrdinaryTerrainButKeepsHarvestableTerrain(bool terrain,bool harvest,bool expected)
        {
            var owner=new Entity{BlueprintName="interest"};
            owner.AddPart(new RenderPart{Visible=true}); owner.AddPart(new PhysicsPart{Takeable=true});
            owner.AddPart(new ExaminablePart());
            if(terrain) owner.SetTag("Terrain");
            if(harvest) owner.AddPart(new HarvestablePart());
            Assert.AreEqual(expected,(bool)Invoke("ControllerInteresting",owner));
        }

        [Test] public void NeutralTriggerWaitsWithoutMovement()
        {
            long tick=turns.TickCount; State(new GamepadState {rightTrigger=1});
            Assert.AreEqual((10,10),zone.GetEntityPosition(player)); Assert.Greater(turns.TickCount,tick);
        }
        [Test] public void HighlightChordNeverWaitsOrMoves()
        {
            long tick=turns.TickCount;
            State(new GamepadState {leftTrigger=1,rightTrigger=1,leftStick=Vector2.right});
            Assert.AreEqual((10,10),zone.GetEntityPosition(player)); Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void DpadSelectsAbilityWithoutWalking()
        {
            long tick=turns.TickCount;
            State(new GamepadState().WithButton(GamepadButton.DpadRight));
            Assert.AreEqual((10,10),zone.GetEntityPosition(player)); Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void StartOpensCharacterMenuAndCancelDoesNotWait()
        {
            long tick=turns.TickCount;
            State(new GamepadState().WithButton(GamepadButton.Start)); Assert.AreEqual("ControllerMenu",Mode);
            State(new GamepadState()); State(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreEqual("Normal",Mode); Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void RestAtFullHealthDoesNotSpendTurns()
        {
            long tick=turns.TickCount;
            State(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreEqual(tick,turns.TickCount); Assert.AreEqual("Normal",Mode);
        }
        [Test] public void WaitMenuOpensWithoutSpendingTurn()
        {
            long tick=turns.TickCount;
            State(new GamepadState{leftTrigger=1}.WithButton(GamepadButton.East));
            Assert.AreEqual("ControllerMenu",Mode); Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void AInteractsWithIndicatedCellWithoutWalking()
        {
            var chest=new Entity { BlueprintName="Chest" }; chest.AddPart(new PhysicsPart());
            chest.AddPart(new RenderPart { DisplayName="chest" }); chest.AddPart(new ContainerPart());
            zone.AddEntity(chest,11,10);
            State(new GamepadState{leftStick=Vector2.right});
            long tick=turns.TickCount;
            State(new GamepadState{leftStick=Vector2.right}.WithButton(GamepadButton.South));
            Assert.AreEqual("WorldActionMenuOpen",Mode);
            Assert.AreEqual((10,10),zone.GetEntityPosition(player)); Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void RightStickEntersLookWithoutSpendingTurn()
        {
            long tick=turns.TickCount; State(new GamepadState{rightStick=Vector2.right});
            Assert.AreEqual("LookMode",Mode); Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void MenuConfirmHeldAcrossCloseDoesNotMoveOrCast()
        {
            State(new GamepadState().WithButton(GamepadButton.Start)); State(new GamepadState());
            State(new GamepadState().WithButton(GamepadButton.East));
            long tick=turns.TickCount;
            State(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreEqual("Normal",Mode); Assert.AreEqual(tick,turns.TickCount);
        }
    }
}
