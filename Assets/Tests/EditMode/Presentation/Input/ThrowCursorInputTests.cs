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
        [TearDown] public void CleanupCursor(){ActionResolutionGuard.SettlePendingAction(actor);AsciiFxBus.Clear();UnityEngine.Object.DestroyImmediate(go);}
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
            Call("ConfirmThrowTarget");Assert.True(Cursor.Active,"The paid preparation keeps its aim ready for the explicit throw.");Assert.Greater(turns.TickCount,tick);
            Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.True(actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual("ThrowTargeting",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
            Assert.AreEqual(13,Cursor.X);Assert.AreEqual(10,Cursor.Y);
            Assert.AreEqual(5,ability.CooldownRemaining);Assert.False(ActionResolutionGuard.IsUnstable);
        }
        [TestCase("keyboard")] [TestCase("deck-a")] [TestCase("deck-rt")]
        public void MeasuredPreparationKeepsExactAimAndNeedsAReleasedThenFreshConfirm(string device)
        {
            WithThrowInputs(device,(press,release)=>
            {
                OpenMeasuredAim();var counter=new ThrowInputTurnProbe();actor.AddPart(counter);
                press();Call("HandleThrowTargetingInput");
                AssertPreparedAim(counter);
                Call("HandleThrowTargetingInput");
                Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item),"The same input update cannot prepare and launch.");
                press();Call("HandleThrowTargetingInput");
                Assert.AreEqual(1,counter.Turns,"A held confirm cannot pay a second action.");
                Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));
                release();Call("HandleThrowTargetingInput");
                AssertPreparedAim(counter);
                press();Call("HandleThrowTargetingInput");
                Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(item),"The fresh confirmation launches the selected unit.");
                Assert.NotNull(zone.GetEntityCell(item));Assert.AreEqual(1,actor.GetPart<ThrowingStatePart>().ThrowSerial);
                Assert.False(actor.GetPart<ThrowingStatePart>().Prepared);
                Assert.AreEqual("WaitingForFxResolution",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
                Call("HandleWaitingForFxResolution");
                Assert.AreEqual(2,counter.Turns,"Preparation and launch each cost exactly one action.");
                Assert.AreEqual(4,ThrowingRules.CooldownRemaining(actor,ThrowingRules.Measured));
                Assert.False(ActionResolutionGuard.IsUnstable);
            });
        }
        [TestCase("keyboard")] [TestCase("deck-a")]
        public void CancellingPreparedAimDoesNotLaunchOrSpendAnotherAction(string device)
        {
            WithThrowInputs(device,(press,release)=>
            {
                OpenMeasuredAim();var counter=new ThrowInputTurnProbe();actor.AddPart(counter);
                press();Call("HandleThrowTargetingInput");AssertPreparedAim(counter);
                release();Call("HandleThrowTargetingInput");Call("CancelThrowTargeting");
                Assert.AreEqual("Normal",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
                Assert.False(Cursor.Active);Assert.AreEqual(1,counter.Turns);
                Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.IsNull(zone.GetEntityCell(item));
                Assert.Zero(actor.GetPart<ThrowingStatePart>().ThrowSerial);
                Assert.True(actor.GetPart<ThrowingStatePart>().Prepared,"Leaving the free aiming UI preserves the existing paid preparation rules.");
                Assert.AreEqual(5,ThrowingRules.CooldownRemaining(actor,ThrowingRules.Measured));
                Assert.False(ActionResolutionGuard.IsUnstable);
            });
        }
        [TestCase("source")] [TestCase("target")] [TestCase("target-hidden")]
        public void LostPreparedSourceOrTargetCannotLaunchAtAReplacement(string invalid)
        {
            WithThrowInputs("keyboard",(press,release)=>
            {
                OpenMeasuredAim();var counter=new ThrowInputTurnProbe();actor.AddPart(counter);
                press();Call("HandleThrowTargetingInput");AssertPreparedAim(counter);
                release();Call("HandleThrowTargetingInput");
                var replacement=Factory.CreateEntity(invalid=="source"?"Dagger":"MarlbackScrabbler");
                if(invalid=="source")
                {actor.GetPart<InventoryPart>().RemoveObject(item);actor.GetPart<InventoryPart>().AddObject(replacement);}
                else if(invalid=="target")
                {zone.RemoveEntity(target);zone.AddEntity(replacement,13,10);}
                else target.GetPart<RenderPart>().Visible=false;
                int hp=replacement.GetStatValue("Hitpoints",0);
                press();Call("HandleThrowTargetingInput");
                Assert.AreEqual(1,counter.Turns,"An invalid retained selection must not launch an ordinary replacement shot.");
                Assert.Zero(actor.GetPart<ThrowingStatePart>().ThrowSerial);
                Assert.IsNull(zone.GetEntityCell(item));Assert.AreEqual(hp,replacement.GetStatValue("Hitpoints",0));
                if(invalid=="source")Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(replacement));
                else Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));
                Assert.False(ActionResolutionGuard.IsUnstable);
            });
        }
        [TestCase("keyboard")] [TestCase("deck-a")]
        public void RetainedAimFollowsTheSameVisibleTargetAfterItMoves(string device)
        {
            WithThrowInputs(device,(press,release)=>
            {
                OpenMeasuredAim();var counter=new ThrowInputTurnProbe();actor.AddPart(counter);
                press();Call("HandleThrowTargetingInput");AssertPreparedAim(counter);
                release();Call("HandleThrowTargetingInput");
                Assert.True(zone.MoveEntity(target,14,11));
                var replacement=Factory.CreateEntity("MarlbackScrabbler");zone.AddEntity(replacement,13,10);
                Call("HandleThrowTargetingInput");
                Assert.True(Cursor.Active);Assert.AreEqual(14,Cursor.X);Assert.AreEqual(11,Cursor.Y);
                Assert.AreSame(target,actor.GetPart<ThrowingStatePart>().PreparedTarget);
                Assert.AreSame(item,actor.GetPart<ThrowingStatePart>().PreparedSource);
                Assert.AreEqual(1,counter.Turns);Assert.Zero(actor.GetPart<ThrowingStatePart>().ThrowSerial);
                Assert.That((string)Call("ThrowPhaseText"),Does.Contain("READY TO THROW"));
            });
        }
        [TestCase("out-of-range")] [TestCase("actor-moved")] [TestCase("target-cell-hidden")] [TestCase("target-dead")]
        public void InterruptedPreparationExitsReadyAimWithoutLaunching(string interruption)
        {
            WithThrowInputs("keyboard",(press,release)=>
            {
                OpenMeasuredAim();var counter=new ThrowInputTurnProbe();actor.AddPart(counter);
                press();Call("HandleThrowTargetingInput");AssertPreparedAim(counter);
                release();Call("HandleThrowTargetingInput");
                if(interruption=="out-of-range")
                {
                    int x=11+HandlingService.GetThrowRange(actor,item);Assert.True(zone.InBounds(x,10));
                    Assert.True(zone.MoveEntity(target,x,10));zone.GetCell(x,10).IsVisible=true;zone.GetCell(x,10).Explored=true;
                }
                else if(interruption=="actor-moved")Assert.True(zone.MoveEntity(actor,11,10));
                else if(interruption=="target-cell-hidden")zone.GetCell(13,10).IsVisible=false;
                else target.GetStat("Hitpoints").BaseValue=0;
                press();Call("HandleThrowTargetingInput");
                Assert.False(Cursor.Active);Assert.AreEqual("Normal",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
                Assert.AreEqual(1,counter.Turns);Assert.Zero(actor.GetPart<ThrowingStatePart>().ThrowSerial);
                Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.IsNull(zone.GetEntityCell(item));
                Assert.False(ActionResolutionGuard.IsUnstable);
            });
        }
        [TestCase("keyboard")] [TestCase("deck-a")]
        public void ReadyAimCannotRetargetOrSwitchToGround(string device)
        {
            WithThrowInputs(device,(press,release)=>
            {
                OpenMeasuredAim();Call("MoveThrowCursor",1,0);Assert.AreEqual(14,Cursor.X,"Unprepared aim remains freely selectable.");
                Call("MoveThrowCursor",-1,0);Call("ToggleThrowAim");Assert.True(Cursor.ThrowGroundAim);
                Call("ToggleThrowAim");Assert.False(Cursor.ThrowGroundAim);
                var counter=new ThrowInputTurnProbe();actor.AddPart(counter);press();Call("HandleThrowTargetingInput");AssertPreparedAim(counter);
                release();Call("HandleThrowTargetingInput");
                Call("MoveThrowCursor",1,0);Call("ToggleThrowAim");
                AssertPreparedAim(counter);Assert.False(Cursor.ThrowGroundAim,"Ready confirmation is still the exact prepared owner, never ground aim.");
            });
        }
        [TestCase(false)] [TestCase(true)]
        public void MeasuredPromptSeparatesPrepareFromReadyThrowWithDeviceSpecificConfirmation(bool pad)
        {
            OpenMeasuredAim();
            string phase=(string)Call("ThrowPhaseText"),hint=(string)Call("ThrowActionHint",pad);
            Assert.That(phase,Does.Contain("1/2").And.Contain("PREPARE").And.Contain("1 turn"));
            Assert.That(hint,Does.Contain(pad?"A / RT":"Enter").And.Contain("confirm"));
            Call("ConfirmThrowTarget");
            phase=(string)Call("ThrowPhaseText");hint=(string)Call("ThrowActionHint",pad);
            Assert.That(phase,Does.Contain("2/2").And.Contain("READY TO THROW").And.Contain("no wait"));
            Assert.That(hint,Does.Contain(pad?"A / RT":"Enter").And.Contain("throw (1 turn)").And.Contain("cancel"));
            Assert.That(hint,Does.Not.Contain("aim"));
            Call("CancelThrowTargeting");Begin();Assert.IsEmpty((string)Call("ThrowPhaseText"),"Ordinary throws must not advertise a prepared bonus or second phase.");
        }
        [Test] public void PeriodWhilePreparedAimDoesNotWaitOrAutomaticallyThrow()
        {
            WithThrowInputs("keyboard",(press,release)=>
            {
                OpenMeasuredAim();var counter=new ThrowInputTurnProbe();actor.AddPart(counter);
                press();Call("HandleThrowTargetingInput");AssertPreparedAim(counter);
                release();Call("HandleThrowTargetingInput");
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Period));InputSystem.Update();Call("HandleThrowTargetingInput");
                AssertPreparedAim(counter);Assert.Zero(actor.GetPart<ThrowingStatePart>().ThrowSerial);
                release();Call("HandleThrowTargetingInput");press();Call("HandleThrowTargetingInput");
                Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(item),"The explicit fresh confirmation, not waiting, launches the retained throw.");
                Call("HandleWaitingForFxResolution");Assert.AreEqual(2,counter.Turns);
            });
        }
        void OpenMeasuredAim()
        {
            actor.GetPart<SkillsPart>().AddSkill(new Throwing_MeasuredThrow());
            var skill=actor.GetPart<SkillsPart>().GetSkill(ThrowingRules.Measured);
            var ability=actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            Call("ActivateAbilityByID",ability.ID);Call("ExecuteThrowPopupSelection");
            Cursor.SetPosition(13,10);Call("RefreshThrowPreview",true);
        }
        void AssertPreparedAim(ThrowInputTurnProbe counter)
        {
            Assert.True(Cursor.Active,"Do not discard the aim after the paid preparation.");
            Assert.AreEqual("ThrowTargeting",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());
            Assert.AreEqual(13,Cursor.X);Assert.AreEqual(10,Cursor.Y);
            var state=actor.GetPart<ThrowingStatePart>();Assert.True(state.Prepared);
            Assert.AreSame(item,state.PreparedSource);Assert.AreSame(target,state.PreparedTarget);
            Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.AreEqual(1,counter.Turns);
            Assert.False(ActionResolutionGuard.IsUnstable);
        }
        void WithThrowInputs(string device,Action<Action,Action> scenario)
        {
            var scope=new InputTestFixture();scope.Setup();
            try
            {
                if(device=="keyboard")
                {
                    var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
                    Action release=()=>{InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();};
                    Action press=()=>{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();};
                    release();scenario(press,release);
                }
                else
                {
                    var pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();
                    Action release=()=>{InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();InputHelper.GetKey(KeyCode.None);};
                    Action press=()=>{InputSystem.QueueStateEvent(pad,device=="deck-a"?new GamepadState().WithButton(GamepadButton.South):new GamepadState{rightTrigger=1});InputSystem.Update();InputHelper.GetKey(KeyCode.None);};
                    release();NativeGamepadInput.SetContext(GamepadInputContext.Targeting);release();scenario(press,release);
                }
            }
            finally{NativeGamepadInput.SetContext(GamepadInputContext.Menu);scope.TearDown();}
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
