using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public class FullscreenSkillsInputTests
    {
        readonly InputTestFixture scope=new InputTestFixture();
        Gamepad pad; GameObject go; SkillsScreenUI ui; Entity actor;
        void State(GamepadState s) { InputSystem.QueueStateEvent(pad,s);InputSystem.Update(); InputHelper.GetKey(KeyCode.None); }
        void Press(GamepadButton b) { State(new GamepadState());ui.HandleInput();State(new GamepadState().WithButton(b));ui.HandleInput(); }
        [SetUp] public void Setup()
        {
            scope.Setup(); pad=InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            State(new GamepadState());NativeGamepadInput.SetContext(GamepadInputContext.Skills);State(new GamepadState());
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            actor=new Entity { ID="full-skills" }; actor.AddPart(new ActivatedAbilitiesPart()); actor.AddPart(new SkillsPart());
            actor.Statistics["SP"]=new Stat { Owner=actor,Name="SP",BaseValue=10 };
            actor.Statistics["Agility"]=new Stat { Owner=actor,Name="Agility",BaseValue=18 };
            go=new GameObject("Skills-test");ui=go.AddComponent<SkillsScreenUI>();ui.Open(actor);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(go);scope.TearDown();SkillRegistry.ResetForTests(); }
        [Test] public void OpensFullScreenCanvasAndBCloses()
        {
            Assert.True(ui.IsOpen); Assert.True(go.GetComponentInChildren<Canvas>().isRootCanvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay,go.GetComponentInChildren<Canvas>().renderMode);
            Press(GamepadButton.East);Assert.False(ui.IsOpen);
        }
        [Test] public void ShouldersChangeDiscipline_YTogglesAndDoesNotSpend()
        {
            int before=ui.Model.DisciplineIndex;Press(GamepadButton.RightShoulder);
            Assert.AreEqual(before+1,ui.Model.DisciplineIndex);
            Press(GamepadButton.North); Assert.True(ui.Model.Overview);
            Press(GamepadButton.North); Assert.False(ui.Model.Overview);
            Assert.AreEqual(10,actor.GetStatValue("SP"));
        }
        [Test] public void PurchaseDefaultsToCancel_ThenExplicitLearnWorks()
        {
            ui.Model.SelectClass("AcrobaticsSkill");
            Press(GamepadButton.South);Assert.True(ui.IsReviewingPurchase);Assert.AreEqual(10,actor.GetStatValue("SP"));
            Press(GamepadButton.South);Assert.False(ui.IsReviewingPurchase);Assert.AreEqual(10,actor.GetStatValue("SP"));
            Press(GamepadButton.South);Press(GamepadButton.DpadRight);Press(GamepadButton.South);
            Assert.True(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));Assert.AreEqual(9,actor.GetStatValue("SP"));
        }
        [Test] public void HeldConfirmDoesNotPurchaseOrActivateWhenClosing()
        {
            Press(GamepadButton.South);State(new GamepadState().WithButton(GamepadButton.South));ui.HandleInput();
            Assert.AreEqual(10,actor.GetStatValue("SP"));
            NativeGamepadInput.SetContext(GamepadInputContext.World);State(new GamepadState().WithButton(GamepadButton.South));
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.Interact));
        }
        [Test] public void BoardRightEdgeDoesNotWrapIntoNextRow()
        {
            ui.Model.SelectClass("Acrobatics_Tumble"); Press(GamepadButton.North);
            Press(GamepadButton.DpadRight);Assert.AreEqual("Acrobatics_Tumble",ui.SelectedClass);
            Press(GamepadButton.DpadDown);Assert.AreEqual("Acrobatics_Vault",ui.SelectedClass);
            Press(GamepadButton.DpadLeft);Assert.AreEqual("Acrobatics_EvasiveRoll",ui.SelectedClass);
        }
        [Test] public void RightStickPagesOnlySelectedDetails()
        {
            ui.Model.SelectClass("Acrobatics_Vault");ui.RefreshSnapshot();
            Assert.Greater(ui.DetailPageCount,1);
            State(new GamepadState {rightStick=Vector2.down});ui.HandleInput();
            Assert.AreEqual(1,ui.DetailPage);Assert.AreEqual("Acrobatics_Vault",ui.SelectedClass);
            State(new GamepadState());ui.HandleInput();State(new GamepadState {rightStick=Vector2.up});ui.HandleInput();
            Assert.Zero(ui.DetailPage);
        }
        [Test] public void AssignmentCanBeCanceledWithoutChangingBindings()
        {
            actor.GetPart<SkillsPart>().AddSkill("Acrobatics_Vault");ui.RefreshSnapshot();ui.Model.SelectClass("Acrobatics_Vault");
            var abilities=actor.GetPart<ActivatedAbilitiesPart>();var before=(System.Guid[])abilities.SlotAssignments.Clone();
            Press(GamepadButton.West);Assert.True(ui.IsAssigning);Press(GamepadButton.DpadDown);Press(GamepadButton.East);
            Assert.False(ui.IsAssigning);Assert.True(ui.IsOpen);CollectionAssert.AreEqual(before,abilities.SlotAssignments);
            ui.Model.SelectClass("AcrobaticsDodgePower");Press(GamepadButton.West);Assert.False(ui.IsAssigning);
        }
        [Test] public void SkillNavigationDoesNotExposeWorldDirectionOrCommands()
        {
            State(new GamepadState {leftStick=Vector2.right,rightTrigger=1});ui.HandleInput();
            Assert.False(NativeGamepadInput.TrySelectedDirection(out _,out _));
            Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.Step));
            Assert.False(InputHelper.GetKeyDown(KeyCode.Return));
        }
        [Test] public void EveryVisibleLabelHasRendererAndEnoughHeight()
        {
            foreach(var label in go.GetComponentsInChildren<SkillScreenText>())
            { Assert.NotNull(label.canvasRenderer);Assert.GreaterOrEqual(label.rectTransform.rect.height,label.GlyphHeight,label.Content); }
        }
        [Test] public void IdleDoesNotRebuildUi()
        {
            int n=ui.RenderCount; for(int i=0;i<100;i++) { State(new GamepadState());ui.HandleInput(); }
            Assert.AreEqual(n,ui.RenderCount);
        }
    }
}
