using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CavesOfOoo.Tests
{
    public sealed class VaultCursorInputTests
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject go;InputHandler input;Entity actor;Zone zone;ActivatedAbility ability;TurnManager turns;
        [SetUp] public void Setup()
        {
            AbilityClarityTestSupport.Reset();actor=AbilityClarityTestSupport.Actor();zone=new Zone();zone.AddEntity(actor,10,10);
            for(int y=8;y<=12;y++)for(int x=8;x<=12;x++)zone.GetCell(x,y).IsVisible=true;
            actor.GetPart<SkillsPart>().AddSkill(new Acrobatics_Vault());ability=actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Find(a=>a.Command=="CommandVault");
            turns=new TurnManager();turns.AddEntity(actor);turns.ProcessUntilPlayerTurn();
            go=new GameObject("vault input fixture");input=go.AddComponent<InputHandler>();input.PlayerEntity=actor;input.CurrentZone=zone;input.TurnManager=turns;
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(go);SkillRegistry.ResetForTests();MessageLog.Clear();}
        object Call(string name,params object[] args){var m=typeof(InputHandler).GetMethod(name,Flags);Assert.NotNull(m,name);return m.Invoke(input,args);}
        WorldCursorState Cursor=>(WorldCursorState)typeof(InputHandler).GetField("_worldCursorState",Flags).GetValue(input);
        [TestCase(false)] [TestCase(true)]
        public void HotbarActivationUsesCursorEvenForOldSavedDirectionMetadata(bool legacy)
        {
            if(legacy)ability.TargetingMode=AbilityTargetingMode.DirectionLine;
            Call("TryActivateAbility",0);Assert.True(Cursor.Active);Assert.AreEqual(WorldCursorMode.AbilityTarget,Cursor.Mode);Assert.AreEqual(2,Cursor.MaxRange);
            int tick=turns.TickCount;Call("ExitVaultTargeting");Assert.False(Cursor.Active);Assert.AreEqual(tick,turns.TickCount);Assert.Zero(ability.CooldownRemaining);
        }
        [Test] public void InvalidCursorConfirmationKeepsSelectionAndCostsNothing()
        {
            Call("TryActivateAbility",0);int tick=turns.TickCount;Cursor.SetPosition(13,10);Call("ConfirmVaultTarget");
            Assert.True(Cursor.Active);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));Assert.AreEqual(tick,turns.TickCount);Assert.Zero(ability.CooldownRemaining);
        }
        [TestCase(false)] [TestCase(true)]
        public void FriendlyConfirmationPrecedesAnyMovementAndDeclineIsFree(bool confirm)
        {
            var npc=AbilityClarityTestSupport.Actor();npc.ID="neutral-vault-target";npc.Tags.Remove("Player");zone.AddEntity(npc,12,10);
            Call("TryActivateAbility",0);Cursor.SetPosition(12,10);int tick=turns.TickCount;
            Call("ConfirmVaultTarget");
            Assert.AreEqual("AwaitingAttackConfirm",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
            Assert.AreEqual((10,10),zone.GetEntityPosition(actor));Assert.AreEqual(tick,turns.TickCount);
            Call("ResolveAttackConfirmation",confirm);
            Assert.False(Cursor.Active);Assert.AreEqual(confirm?(11,10):(10,10),zone.GetEntityPosition(actor));
            if(!confirm){Assert.AreEqual(tick,turns.TickCount);Assert.Zero(ability.CooldownRemaining);}
            else Assert.Greater(ability.CooldownRemaining,0);
        }
        [Test] public void UnboundManagerActivationUsesSameCursor()
        {Call("ActivateAbilityByID",ability.ID);Assert.True(Cursor.Active);Assert.AreEqual(WorldCursorMode.AbilityTarget,Cursor.Mode);}
        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void GamepadMovesVaultCursorThenAConfirmsOrBCancels(bool dpad, bool cancel)
        {
            var devices = new InputTestFixture(); devices.Setup();
            try
            {
                var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
                void State(GamepadState state) { InputSystem.QueueStateEvent(pad,state); InputSystem.Update(); }
                State(new GamepadState()); NativeGamepadInput.SetContext(GamepadInputContext.Targeting); State(new GamepadState());
                Call("TryActivateAbility",0); int tick=turns.TickCount;
                for(int i=0;i<2;i++)
                {
                    State(dpad ? new GamepadState().WithButton(GamepadButton.DpadRight) : new GamepadState {leftStick=Vector2.right});
                    Call("HandleVaultTargeting");
                    State(new GamepadState()); Call("HandleVaultTargeting");
                }
                Assert.AreEqual(12,Cursor.X,"Stick or D-pad moves the selector without pressing A.");
                Assert.AreEqual((10,10),zone.GetEntityPosition(actor)); Assert.AreEqual(tick,turns.TickCount);
                State(new GamepadState().WithButton(cancel ? GamepadButton.East : GamepadButton.South));
                Call("HandleVaultTargeting");
                Assert.False(Cursor.Active); Assert.AreEqual(cancel?(10,10):(12,10),zone.GetEntityPosition(actor));
                if(cancel) { Assert.Zero(ability.CooldownRemaining); Assert.AreEqual(tick,turns.TickCount); }
                else { Assert.Greater(ability.CooldownRemaining,0); Assert.Greater(turns.TickCount,tick); }
            }
            finally { NativeGamepadInput.SetContext(GamepadInputContext.Menu); devices.TearDown(); }
        }

        [Test]
        public void ControllerDirectionCueCannotReplaceTheVaultCursor()
        {
            var devices = new InputTestFixture(); devices.Setup();
            try
            {
                var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                NativeGamepadInput.SetContext(GamepadInputContext.Targeting);
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                Call("TryActivateAbility", 0); Cursor.SetPosition(12, 10);
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right }); InputSystem.Update();
                Assert.True(NativeGamepadInput.TrySelectedDirection(out _, out _), "actual pad direction is active");
                Call("RefreshControllerDirectionCue");
                var cue = (WorldCursorState)typeof(InputHandler).GetField("_controllerDirectionCue", Flags).GetValue(input);
                Assert.False(cue.Active, "The ordinary one-cell cue must not overwrite Vault's two-cell target.");
                Assert.True(Cursor.Active); Assert.AreEqual(12, Cursor.X);
                Call("ExitVaultTargeting");
                Call("RefreshControllerDirectionCue");
                Assert.True(cue.Active, "Counter: outside Vault the normal direction cue still works.");
            }
            finally { NativeGamepadInput.SetContext(GamepadInputContext.Menu); devices.TearDown(); }
        }

        [Test] public void UnseenTargetCannotRevealOrAttack()
        {Call("TryActivateAbility",0);Cursor.SetPosition(12,10);zone.GetCell(12,10).IsVisible=false;Call("ConfirmVaultTarget");Assert.True(Cursor.Active);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));Assert.Zero(ability.CooldownRemaining);}
    }
}
