using System.Collections;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        bool _skillsOnly,_skillsComplete;
        public void ConfigureSkillsAudit()=>_skillsOnly=true;
        IEnumerator RunSkillsAudit()
        {
            // Explicit UI fixture budget in an isolated save graph. No world/combat grant claim.
            _input.PlayerEntity.GetStat("SP").BaseValue=10;
            var previousPad=Gamepad.current;
            var pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();
            try
            {
                var before=ControllerBefore();
                yield return ControllerHold(pad,new GamepadState(),.05f);
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.Start));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                var ui=_input.SkillsScreenUI;
                Require(State()=="SkillsScreenOpen"&&ui.IsOpen,"controller Character > Skills");
                Check("dedicated_full_screen_field_manual",!ui.Model.Overview&&ui.GetComponentInChildren<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay);
                Require(ui.SelectedClass=="AcrobaticsSkill","first discipline is Acrobatics");
                yield return Capture("02-field-manual");
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                Require(ui.IsReviewingPurchase,"review does not spend");
                Check("review_keeps_sp",_input.PlayerEntity.GetStatValue("SP")==10);
                yield return Capture("03-review-default-cancel");
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                Check("default_cancel_keeps_sp",!ui.IsReviewingPurchase&&_input.PlayerEntity.GetStatValue("SP")==10);
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadRight));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                Check("confirmed_root_purchase_spent_once",_input.PlayerEntity.GetStatValue("SP")==9&&_input.PlayerEntity.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
                for(int i=0;i<4;i++)yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadDown));
                Require(ui.SelectedClass=="Acrobatics_Vault","four deliberate Dpad presses reach Vault");
                yield return Capture("04-vault-details");
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.North));
                Check("overview_retains_vault",ui.Model.Overview&&ui.SelectedClass=="Acrobatics_Vault");
                yield return Capture("05-vault-overview");
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadRight));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.West));
                Require(ui.IsAssigning,"X opens slot chooser for owned Vault");
                for(int i=0;i<9;i++)yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return Capture("06-slot-assignment");
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.South));
                Check("slot_ten_contains_vault",_input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9)?.SourcePowerClass=="Acrobatics_Vault");
                int idle=ui.RenderCount;
                yield return new WaitForSecondsRealtime(2);
                Check("idle_does_not_rebuild_ui",ui.RenderCount==idle);
                var visited=new System.Collections.Generic.HashSet<string>();
                for(int i=0;i<13;i++)
                {
                    visited.Add(ui.Model.Current.Class);
                    if(ui.Model.Current.Name=="Pyromancy")
                    {
                        yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.North));
                        int total=ui.Model.Current.Rows.Count;
                        for(int n=0;n<total;n++)yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadDown));
                        Check("large_discipline_final_power_reachable",ui.Model.SkillIndex==total-1);
                        yield return Capture("07-pyromancy-last-page");
                        yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.North));
                        yield return Capture("08-pyromancy-overview");
                    }
                    yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.RightShoulder));
                }
                Check("all_disciplines_reachable",visited.Count==ui.Model.Disciplines.Count);
                string beforeDetail=ui.SelectedClass;
                yield return ControllerPulse(pad,new GamepadState {rightStick=Vector2.down});
                Check("detail_paging_does_not_change_skill",ui.SelectedClass==beforeDetail);
                for(int i=0;i<13 && ui.Model.Current.Name!="Axe";i++)
                    yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.LeftShoulder));
                for(int i=0;i<3;i++)yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.DpadDown));
                Require(ui.SelectedClass=="Axe_Decapitate","overview spatial navigation reaches real dependency");
                Check("real_power_dependency_is_present",ui.Model.Prerequisites.Contains("Axe_Dismember"));
                yield return Capture("09-axe-locked-prerequisite");
                foreach(var label in ui.GetComponentsInChildren<SkillScreenText>())
                    Require(label.canvasRenderer!=null && label.rectTransform.rect.height>=label.GlyphHeight,"visible label owns renderer and fits its box");
                Check("all_visible_glyph_labels_have_renderers_and_height",true);
                yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.East));
                Require(State()=="Normal"&&!ui.IsOpen,"B restores world input");
                Check("whole_menu_visit_spends_no_turn_energy_or_position",ControllerStayed(before)&&ControllerCost(before,0));
                string selection=ui.Model.Selected.Class;
                yield return Tap(Key.X);Check("keyboard_reopen_remembers_selection",ui.SelectedClass==selection&&ui.IsOpen);
                yield return Capture("09-reopened-keyboard");yield return Tap(Key.Escape);
                Check("keyboard_close_restores_world",State()=="Normal"&&!ui.IsOpen);
                _skillsComplete=true;
            }
            finally { if(pad.added)InputSystem.RemoveDevice(pad); if(previousPad!=null && previousPad.added)previousPad.MakeCurrent(); }
        }
    }
}
