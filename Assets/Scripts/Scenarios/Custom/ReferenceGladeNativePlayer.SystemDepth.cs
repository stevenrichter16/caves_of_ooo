using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _systemDepthOnly, _systemDepthComplete;
        private Action _systemDepthCleanup;
        private const string SystemDepthCanVerify = "Isolated ordinary seed64 game with one explicitly arranged factory Villager/recruit and an arranged long quest journal. Actual keyboard look/context commands and synthetic native Gamepad LT+A/A/B route through InputHandler. Ordinary paid east/west steps show stay and resumed follow. Actual Q/PgDn and controller LB/RB/Y/B display and navigate the complete journal. Exact actor positions, party ownership, tick/energy cost and screenshots are recorded. Launcher owns isolated saves/scenes/input settings; fixture registry and previous current gamepad are restored.";
        private const string SystemDepthCannotVerify = "Arranged participants and quest data: not proof of ordinary recruitment acquisition, organic quest accumulation, all-zone follower navigation, physical Steam Deck feel or performance. No explicit save/load round trip is tested; normal new-game checkpoint writes remain confined to the isolated save root; native EditMode checks cover order persistence and stale ownership. Screenshot readability requires independent visual review.";
        public void ConfigureSystemDepthAudit() => _systemDepthOnly = true;

        private IEnumerator RunSystemDepthAudit()
        {
            var zone = _input.CurrentZone; var actor = _input.PlayerEntity;
            var companion = _context.Factory.CreateEntity("Villager");
            Require(companion != null && zone.GetCell(39, 12).IsPassable(), "arranged companion uses an open starting cell");
            companion.GetPart<RenderPart>().DisplayName = "audit companion";
            Require(zone.AddEntity(companion, 39, 12), "place explicitly arranged factory companion");
            var brain = companion.GetPart<BrainPart>();
            Require(brain != null, "factory companion brain"); brain.CurrentZone = zone; brain.Rng = new System.Random(64);
            Require(companion.ApplyEffect(new RecruitedEffect(actor), actor, zone), "explicit scenario recruitment arrangement");
            _input.TurnManager.AddEntity(companion);
            ZoneRenderHooks.MarkFullDirty("Scenario.SystemDepthCompanion");
            var previousPad = Gamepad.current; Gamepad pad = null;
            var previousStory = StoryletPart.Current;
            var previousRegistry = StoryletRegistry.GetAll();
            _systemDepthCleanup = () =>
            {
                if (pad != null && pad.added) { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.RemoveDevice(pad); }
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                StoryletPart.Current = previousStory;
                StoryletRegistry.Reset(); foreach (var data in previousRegistry) StoryletRegistry.Register(data);
                companion.GetEffect<RecruitedEffect>()?.Dismiss(actor);
                _input.TurnManager.RemoveEntity(companion); zone.RemoveEntity(companion);
                ZoneRenderHooks.MarkFullDirty("Scenario.SystemDepthCleanup");
                Check("system_depth_fixture_story_registry_restored", StoryletPart.Current == previousStory
                    && StoryletRegistry.GetAll().Count == previousRegistry.Count && StoryletRegistry.Get("SystemDepthJournal") == null);
                Check("system_depth_synthetic_gamepad_removed", pad == null || !pad.added);
                Check("system_depth_previous_gamepad_restored", previousPad == null || !previousPad.added || Gamepad.current == previousPad);
            };
            try
            {
                var before = ControllerBefore();
                yield return Tap(Key.L); yield return Tap(Key.A); yield return Tap(Key.Enter);
                Require(State() == "WorldActionMenuOpen" && _input.WorldActionMenuUI.SelectedTarget == companion, "keyboard context targets actual arranged companion");
                yield return Capture("02-keyboard-companion-actions");
                yield return SystemDepthKeyboardChoice(CompanionOrders.StayCommand);
                if (State() == "LookMode") yield return Tap(Key.Escape);
                Check("keyboard_stay_command_is_free_and_keeps_recruitment", CompanionOrders.IsStaying(companion)
                    && brain.PartyLeader == actor && ControllerCost(before, 0));
                yield return Tap(Key.D); yield return Tap(Key.D);
                Check("staying_companion_remains_at_ordered_cell_during_real_walk", Cell().X == 42 && Cell().Y == 12
                    && zone.GetEntityPosition(companion) == (39, 12) && CompanionOrders.IsStaying(companion));
                yield return Capture("03-companion-stays-during-walk");
                yield return Tap(Key.A); yield return Tap(Key.A);
                Require(Cell().X == 40 && Cell().Y == 12, "ordinary return to companion reach");
                pad = InputSystem.AddDevice<Gamepad>();
                yield return ControllerHold(pad, new GamepadState(), .03f);
                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState { leftStick = Vector2.left, leftTrigger = 1 }.WithButton(GamepadButton.South));
                Require(State() == "WorldActionMenuOpen" && _input.WorldActionMenuUI.SelectedTarget == companion, "native LT+A opens actual companion");
                yield return SystemDepthPadChoice(pad, CompanionOrders.FollowCommand, false);
                yield return Capture("04-controller-follow-choice");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
                Check("native_follow_command_is_free", !CompanionOrders.IsStaying(companion) && brain.PartyLeader == actor && ControllerCost(before, 0));
                yield return Tap(Key.D); yield return Tap(Key.D);
                Check("resumed_companion_moves_on_ordinary_scheduler", Cell().X == 42 && Cell().Y == 12
                    && zone.GetEntityPosition(companion).x > 39 && brain.HasGoal<FollowLeaderGoal>());
                yield return Capture("05-companion-resumes-following");

                var story = new StoryletPart(); StoryletPart.Current = story;
                var objectives = new List<QuestObjectiveData>();
                for (int i = 0; i < 32; i++) objectives.Add(new QuestObjectiveData { ID = "route" + i,
                    Text = "Recorded route " + (i + 1) + ": follow the old path beside the hedges, then bring the permitted sample back to the keeper at the west well. RETURN" + i + "." });
                StoryletRegistry.Register(new StoryletData { ID = "SystemDepthJournal", Quest = new QuestData { Stages = new List<QuestStageData> {
                    new QuestStageData { ID = "Bring the sample back", Objectives = objectives } } } });
                story.StartQuest(new QuestState { QuestId = "SystemDepthJournal" });
                story.UndertakeAct("depth-closed", "A meal returned to its cook"); story.CloseAct("depth-closed");
                story.UndertakeAct("depth-refused", "A promise declined aloud"); story.RefuseAct("depth-refused");
                story.UndertakeAct("depth-open", "A request still waiting for an answer");
                before = ControllerBefore(); yield return Tap(Key.Q);
                var journal = _input.QuestLogUI;
                Require(journal != null && journal.IsOpen && journal.QuestPage == 0 && journal.QuestPageCount > 1, "native Q opens arranged long journal");
                yield return Capture("06-quest-journal-first-page");
                yield return Tap(Key.PageDown); Check("keyboard_page_down_reaches_later_objectives", journal.QuestPage == 1);
                yield return Capture("07-quest-journal-next-page");
                for (int i = 0; i < 12 && journal.QuestPage < journal.QuestPageCount - 1; i++)
                    yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.RightShoulder));
                Check("native_right_shoulder_reaches_final_history_page", journal.QuestPage == journal.QuestPageCount - 1 && journal.QuestPage > 0);
                yield return Capture("08-quest-journal-history-page");
                int last = journal.QuestPage;
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.North));
                Check("native_y_opens_field_notes", journal.NotesVisible);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.North));
                Check("quest_page_survives_field_note_tab", !journal.NotesVisible && journal.QuestPage == last);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
                Check("native_left_shoulder_returns_one_quest_page", journal.QuestPage == last - 1);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Check("native_b_closes_journal_without_world_time_or_quest_mutation", !journal.IsOpen && State() == "Normal"
                    && ControllerCost(before, 0) && story.GetActiveQuests().Count == 1
                    && story.GetClosure("depth-refused").State == ClosureState.Refused);
                _systemDepthComplete = true;
            }
            finally { _systemDepthCleanup?.Invoke(); _systemDepthCleanup = null; }
        }
        private IEnumerator SystemDepthKeyboardChoice(string command)
        {
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            int index = actions.FindIndex(a => a.Command == command); Require(index >= 0, "actual keyboard menu offers " + command);
            yield return Tap((Key)Enum.Parse(typeof(Key), MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }
        private IEnumerator SystemDepthPadChoice(Gamepad pad, string command, bool confirm)
        {
            for (int i = 0; i < 16 && _input.WorldActionMenuUI.HighlightedAction?.Command != command; i++)
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadDown));
            Require(_input.WorldActionMenuUI.HighlightedAction?.Command == command, "controller reaches actual " + command);
            if (confirm) yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
        }
    }
}
