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
    public sealed class ThrowCursorInputTests : FiftyWorldFixture
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject go;InputHandler input;Zone zone;Entity actor,item,target;TurnManager turns;
        [SetUp] public void SetupCursor()
        {
            zone=new Zone("throw-input");actor=Factory.CreateEntity("Player");item=Factory.CreateEntity("Dagger");
            actor.GetPart<InventoryPart>().AddObject(item);zone.AddEntity(actor,10,10);
            target=Factory.CreateEntity("MarlbackScrabbler");target.GetPart<BrainPart>().Passive=true;zone.AddEntity(target,13,10);
            for(int x=9;x<=15;x++)for(int y=9;y<=11;y++){zone.GetCell(x,y).IsVisible=true;zone.GetCell(x,y).Explored=true;}
            turns=new TurnManager();turns.AddEntity(actor);turns.ProcessUntilPlayerTurn();
            go=new GameObject("throw input fixture");input=go.AddComponent<InputHandler>();input.PlayerEntity=actor;input.CurrentZone=zone;input.TurnManager=turns;
        }
        [TearDown] public void CleanupCursor(){UnityEngine.Object.DestroyImmediate(go);}
        object Call(string name,params object[] args)=>typeof(InputHandler).GetMethod(name,Flags).Invoke(input,args);
        WorldCursorState Cursor=>(WorldCursorState)typeof(InputHandler).GetField("_worldCursorState",Flags).GetValue(input);
        void Begin()
        {
            var kind=typeof(InputHandler).GetNestedType("ThrowOriginKind",BindingFlags.NonPublic);
            Assert.True((bool)Call("BeginThrowTargeting",item,Enum.Parse(kind,"Inventory"),10,10,true));
        }
        [Test] public void PreviewUsesKnownTraceAndExplainsBodyVersusGroundWithoutMutating()
        {
            var floor=Factory.CreateEntity("Dagger");zone.AddEntity(floor,12,10);int tick=turns.TickCount;
            var preview=ThrowPreview.Build(zone,actor,item,ThrowIntent.AtEntity(target,13,10),20,false,false);
            Assert.AreSame(target,preview.Trace.HitEntity);Assert.False(preview.Trace.Uncertain);
            Assert.That(preview.Description,Does.Contain("1 item"));Assert.That(preview.Description,Does.Contain("Body"));
            Assert.AreEqual(tick,turns.TickCount);Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));
            var ground=ThrowPreview.Build(zone,actor,item,ThrowIntent.Ground(12,10),20,false,false);
            Assert.That(ground.Description,Does.Contain("Ground"));Assert.AreSame(floor,ground.Trace.HitEntity);
        }
        [Test] public void UnknownPreviewNamesNeitherHiddenOwnerNorHiddenObstruction()
        {
            target.GetPart<RenderPart>().Visible=false;zone.GetCell(12,10).IsVisible=false;
            var preview=ThrowPreview.Build(zone,actor,item,ThrowIntent.Ground(13,10),20,false,false);
            Assert.True(preview.Trace.Uncertain);Assert.That(preview.Description,Does.Not.Contain(target.GetDisplayName()));
            Assert.IsEmpty(preview.Splash);
        }
        [TestCase(GamepadButton.North,KeyCode.Tab)] [TestCase(GamepadButton.LeftShoulder,KeyCode.F)]
        public void TargetModeBindingsAreNativeAndDoNotSpendAnAction(GamepadButton button,KeyCode expected)
        {
            var scope=new InputTestFixture();scope.Setup();
            try
            {
                var pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                NativeGamepadInput.SetContext(GamepadInputContext.Targeting);
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();int tick=turns.TickCount;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));InputSystem.Update();
                Assert.True(InputHelper.GetKeyDown(expected));Assert.False(InputHelper.GetKeyDown(KeyCode.Return));Assert.AreEqual(tick,turns.TickCount);
            }
            finally{NativeGamepadInput.SetContext(GamepadInputContext.Menu);scope.TearDown();}
        }
        [Test] public void CancelAndReopenReturnsFreshBroadIntentWithoutTurnCost()
        {
            Begin();int tick=turns.TickCount;Call("ToggleThrowAim");Assert.True(Cursor.ThrowGroundAim);
            Call("CancelThrowTargeting");Assert.AreEqual(tick,turns.TickCount);Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));
            Begin();Assert.False(Cursor.ThrowFocused);Assert.False(Cursor.ThrowGroundAim);
        }
        [Test] public void CursorCannotLeakInvisibleHostilityOrLootColors()
        {
            target.GetPart<BrainPart>().PersonalEnemies.Add(actor);
            var cell=zone.GetCell(13,10);cell.IsVisible=false;
            Assert.AreEqual(QudColorParser.Gray,WorldCursorRenderer.GetCursorColor(cell,actor));
            cell.IsVisible=true;target.GetPart<RenderPart>().Visible=false;
            Assert.AreEqual(QudColorParser.Gray,WorldCursorRenderer.GetCursorColor(cell,actor));
            target.GetPart<RenderPart>().Visible=true;
            Assert.AreEqual(QudColorParser.BrightRed,WorldCursorRenderer.GetCursorColor(cell,actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void MeasuredSkillOpensFreeSourcePickerThenPreparesWithoutThrowing(bool manager)
        {
            var skills=actor.GetPart<SkillsPart>();skills.AddSkill(new Throwing_MeasuredThrow());
            var skill=skills.GetSkill("Throwing_MeasuredThrow");var ability=actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            int tick=turns.TickCount;
            if(manager)Call("ActivateAbilityByID",ability.ID);else Call("TryActivateAbility",actor.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(ability.ID));
            Assert.AreEqual("ThrowPopupOpen",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
            Assert.AreEqual(tick,turns.TickCount);Assert.Zero(ability.CooldownRemaining);
            Call("ExecuteThrowPopupSelection");Assert.True(Cursor.Active);Cursor.SetPosition(13,10);Call("RefreshThrowPreview",true);
            Call("ConfirmThrowTarget");Assert.False(Cursor.Active);Assert.Greater(turns.TickCount,tick);
            Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.True(actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(5,ability.CooldownRemaining);Assert.False(ActionResolutionGuard.IsUnstable);
        }
        [Test] public void VisibleImpactDoesNotPromiseUnobservedSplashCoverage()
        {
            var tonic=Factory.CreateEntity("WaterTonic");zone.GetCell(13,11).IsVisible=false;
            var preview=ThrowPreview.Build(zone,actor,tonic,ThrowIntent.Ground(13,10),20,false,false);
            Assert.False(preview.Trace.Uncertain);Assert.That(preview.Description,Does.Contain("Unseen splash cells are uncertain"));
            var hidden=Factory.CreateEntity("StoneWall");zone.AddEntity(hidden,13,11);
            Assert.AreEqual(preview.Description,ThrowPreview.Build(zone,actor,tonic,ThrowIntent.Ground(13,10),20,false,false).Description);
        }
        [Test] public void PreviewDoesNotNameAStaleOrUnrenderedOwnerInAKnownCell()
        {
            var secret=new Entity{ID="secret-owner",BlueprintName="secret machinery"};
            secret.AddPart(new PhysicsPart{ThrowHeight=2.4f,Solid=true});zone.AddEntity(secret,12,10);
            var preview=ThrowPreview.Build(zone,actor,item,ThrowIntent.AtEntity(target,13,10),20,false,false);
            Assert.AreSame(target,preview.Trace.HitEntity,"Unrendered owners cannot steer a known preview.");
            Assert.That(preview.Description,Does.Not.Contain("secret machinery"));
            secret.AddPart(new RenderPart{Visible=true});
            Assert.AreSame(secret,ThrowPreview.Build(zone,actor,item,ThrowIntent.AtEntity(target,13,10),20,false,false).Trace.HitEntity);
            zone.RemoveEntity(target);
            var stale=ThrowPreview.Build(zone,actor,item,ThrowIntent.AtEntity(target,13,10),20,false,false);
            Assert.That(stale.Description,Does.Not.Contain(target.GetDisplayName()));
        }
        [Test] public void NewlyVisibleFrontierRefreshesCachedPreviewWithoutAnEntityOrTurnChange()
        {
            zone.GetCell(12,10).IsVisible=false;Begin();Cursor.SetPosition(13,10);Call("RefreshThrowPreview",true);
            Assert.True(Cursor.ThrowPreview.Trace.Uncertain);
            int version=zone.EntityVersion,tick=turns.TickCount;
            zone.GetCell(12,10).IsVisible=true;Call("RefreshThrowPreview",false);
            Assert.False(Cursor.ThrowPreview.Trace.Uncertain);Assert.AreSame(target,Cursor.ThrowPreview.Trace.HitEntity);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(tick,turns.TickCount);
        }
        [Test] public void PendingActionChargesItsOriginalActorIfAdapterReferenceChanges()
        {
            var spy=new ThrowInputTurnProbe();actor.AddPart(spy);Call("HoldThrowAction");
            var replacement=Factory.CreateEntity("Player");zone.AddEntity(replacement,12,11);input.PlayerEntity=replacement;
            Call("EndTurnAndProcess");Assert.AreEqual(1,spy.Turns,"The actor who committed the throw owes the action.");
            Assert.False(ActionResolutionGuard.IsUnstable);
        }
        [TestCase("missing-render", false)]
        [TestCase("unexplored", false)]
        [TestCase("hidden", false)]
        [TestCase("visible", true)]
        [TestCase("moved", false)]
        public void OrdinaryWorldThrowPickerOffersOnlyCurrentlyKnownSources(string state, bool expected)
        {
            var floor=Factory.CreateEntity("Dagger");
            Assert.True(zone.AddEntity(floor,11,10));
            var source=zone.GetCell(11,10);
            if(state=="missing-render") floor.RemovePart(floor.GetPart<RenderPart>());
            if(state=="unexplored") source.Explored=false;
            if(state=="hidden") floor.GetPart<RenderPart>().Visible=false;
            if(state=="moved") Assert.True(zone.MoveEntity(floor,12,11));
            int tick=turns.TickCount,energy=turns.GetEnergy(actor);
            var offered=(System.Collections.IList)Call("GetVisibleThrowableWorldItems",11,10);
            Assert.AreEqual(expected,offered.Contains(floor),"Ordinary source selection must obey the same visible-owner contract as the skill picker and preview.");
            if(state=="moved")
                Assert.True(((System.Collections.IList)Call("GetVisibleThrowableWorldItems",12,11)).Contains(floor),"A moved visible source remains available at its actual current cell.");
            Assert.AreEqual(tick,turns.TickCount);Assert.AreEqual(energy,turns.GetEnergy(actor));
            Assert.IsNull(floor.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(state=="moved"?zone.GetCell(12,11):source,zone.GetEntityCell(floor));
        }
        sealed class ThrowInputTurnProbe:Part
        {public int Turns;public override bool HandleEvent(GameEvent e){if(e.ID=="EndTurn")Turns++;return true;}}
    }
}
