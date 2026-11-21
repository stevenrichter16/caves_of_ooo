using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Declared action time and disabled rows (Docs/MACHINE-ACTION-ROWS.md, spec S3). A row can declare its turn cost and a reason it
    /// is unavailable; the legacy command chains keep deciding for undeclared rows. Every positive case has the undeclared control beside it.
    /// </summary>
    public sealed class ActionTimeProbePart : Part
    {
        /// <summary>The command the probe offers and handles. Neither legacy chain lists "ProbeDeclared".</summary>
        public string Command = "ProbeDeclared";
        public string Time = "Unspecified";
        public bool Disabled;
        public string DisabledCode = "faulted";
        public string DisabledReason = "The probe is fouled and needs repair first.";
        public bool AlsoDisableDeclared;
        public bool Handles = true;
        public char Key = 'p';
        public static int Handled;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var list = e.GetParameter<InventoryActionList>("Actions");
                if (Disabled) list.AddDisabled("Probe", "probe the thing", DisabledCode, DisabledReason, Key, 30);
                else
                {
                    var row = list.AddAction("Probe", "probe the thing", Command, Key, 30, false, (ActionTime)Enum.Parse(typeof(ActionTime), Time));
                    if (AlsoDisableDeclared) { row.DisabledCode = DisabledCode; row.DisabledReason = DisabledReason; }
                }
                return true;
            }
            if (e.ID == "InventoryAction" && Handles && !string.IsNullOrEmpty(Command) && e.GetStringParameter("Command") == Command) { Handled++; return false; }
            return true;
        }
    }

    public sealed class ActionTimeDeclarationTests : FiftyWorldFixture
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;

        string lastMessage;
        [SetUp] public void ResetProbe() { ActionTimeProbePart.Handled = 0; Diag.ResetAll(); MessageLog.Clear(); lastMessage = null; }

        Entity Probe(string time = "Unspecified", string command = "ProbeDeclared", Action<ActionTimeProbePart> configure = null, int x = 11, int y = 10)
        {
            var bench = Place("TinkersForge", x, y); var probe = new ActionTimeProbePart { Time = time, Command = command };
            configure?.Invoke(probe); bench.AddPart(probe); return bench;
        }

        /// <summary>Opens the real world menu on the target, lets the caller act, and reports the ticks the turn clock advanced.</summary>
        int WorldMenu(Entity target, Func<WorldActionMenuUI, InputHandler, TurnManager, InventoryAction> pick, Action<WorldActionMenuUI, InputHandler> after = null, bool execute = true)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            {
                var manager = OverworldZoneManager.CreateDetached(Factory, 64, true); manager.SetActiveZone(Zone);
                var turns = new TurnManager(); turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("action-time", "world-menu", manager, turns, Actor));
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", Hidden); state.SetValue(ui.Input, Enum.Parse(state.FieldType, "Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Hidden).Invoke(ui.Input, new object[] { target, Zone.GetEntityCell(target), false });
                var action = pick(menu, ui.Input, turns);
                int before = turns.TickCount;
                if (execute) typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Hidden).Invoke(ui.Input, new object[] { action, target, Zone.GetEntityCell(target), false });
                int spent = turns.TickCount - before;
                after?.Invoke(menu, ui.Input);
                lastMessage = MessageLog.GetLast();   // the fixture restores shared log state when it is disposed
                return spent;
            }
        }

        static List<InventoryAction> Rows(WorldActionMenuUI menu) => (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Hidden).GetValue(menu);
        static InventoryAction Row(WorldActionMenuUI menu) => Rows(menu).Single(r => r.Name == "Probe");

        // ── model ──

        [Test] public void AddActionDefaultsToUnspecifiedAndReturnsTheRow()
        {
            var list = new InventoryActionList(); var row = list.AddAction("X", "x", "XCommand", 'x', 3);
            Assert.AreSame(row, list.Actions.Single()); Assert.AreEqual(ActionTime.Unspecified, row.Time); Assert.False(row.IsDisabled);
            Assert.AreEqual(ActionTime.Unspecified, new InventoryAction().Time, "a bare row is also undeclared");
            Assert.AreEqual(ActionTime.OneAction, list.AddAction("Y", "y", "Y", time: ActionTime.OneAction).Time);
        }

        [TestCase("", false)] [TestCase("   ", false)] [TestCase(null, false)] [TestCase("The forge is fouled.", true)]
        public void OnlyANonBlankReasonDisablesARow(string reason, bool disabled)
            => Assert.AreEqual(disabled, new InventoryAction { DisabledReason = reason }.IsDisabled);

        [Test] public void AddDisabledNeverLeavesANullCommandOrABlankReason()
        {
            var list = new InventoryActionList(); var row = list.AddDisabled("X", "x", "", "   ");
            Assert.True(row.IsDisabled); Assert.AreEqual("unavailable", row.DisabledCode); StringAssert.StartsWith(InventoryAction.DisabledCommandPrefix, row.Command);
            Assert.False(string.IsNullOrWhiteSpace(row.DisabledReason));
            Assert.AreEqual(ActionTime.Unspecified, row.Time);
        }

        [Test] public void DuplicateDisabledNamesAreBothListedAndSortStaysStable()
        {
            var list = new InventoryActionList(); list.AddDisabled("D", "d", "a", "No."); list.AddDisabled("D", "d", "b", "No again."); list.AddAction("E", "e", "ECommand", 'e', 5);
            list.Sort(); Assert.AreEqual(3, list.Actions.Count); Assert.AreEqual("E", list.Actions[0].Name, "priority still orders disabled rows below enabled ones");
            list.Actions[1].DisabledReason = ""; Assert.DoesNotThrow(() => list.Sort(), "a row mutated after the sort is harmless");
        }

        [Test] public void ADisabledAndAnEnabledRowCanShareAHotkeyWithoutTheDisabledOneStealingIt()
        {
            var list = new InventoryActionList(); list.AddDisabled("D", "d", "a", "No.", 'p', 30); list.AddAction("E", "e", "ECommand", 'p', 29);
            var map = typeof(WorldActionMenuUI).Assembly.GetTypes().Single(t => t.Name == "MenuShortcutMap");
            var keys = (char[])map.GetMethod("ForActions", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { list.Actions }); Assert.AreEqual('p', keys[0]); Assert.AreNotEqual('p', keys[1]); Assert.AreNotEqual('\0', keys[1], "the enabled row still gets a key");
        }

        // ── world menu: declared time ──

        Entity WorkHearth(string command)
        {
            var hearth = Place("CampHearth", 11, 10);
            hearth.GetPart<PhysicsPart>().Takeable = false;
            hearth.GetPart<FuelPart>().FuelMass = 5;
            hearth.GetPart<ThermalPart>().Temperature = command == "DryGear" ? 450 : 25;
            Carry("SalvagedTimber"); Carry("SalvagedTimber"); Carry("SalvagedTimber");
            Carry("FlintStriker");
            if (command == "DryGear") Carry("Dagger").ApplyEffect(new WetEffect { Moisture = 100 });
            return hearth;
        }

        [TestCase("FeedCookingFire")] [TestCase("FeedCookingFireFull")]
        [TestCase("RelightCookingFire")] [TestCase("StokeCookingFire")] [TestCase("DryGear")]
        public void RealHearthWorkRowDeclaresAndSpendsExactlyOneAction(string command)
        {
            var hearth = WorkHearth(command);
            int beforeTimber = Pack.Objects.Where(e => e.BlueprintName == "SalvagedTimber")
                .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            var striker = Pack.Objects.Single(e => e.BlueprintName == "FlintStriker").GetPart<FlintStrikerPart>();
            int strikes = striker.Strikes;
            int spent = WorldMenu(hearth, (menu, input, turns) =>
            {
                var row = Rows(menu).Single(r => r.Name == command);
                Assert.AreEqual(ActionTime.OneAction, row.Time, "real work rows own their turn cost");
                return row;
            });
            Assert.AreEqual(10, spent, "a real menu commit advances exactly one ordinary action");
            int timber = Pack.Objects.Where(e => e.BlueprintName == "SalvagedTimber")
                .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            Assert.AreEqual(beforeTimber - (command == "FeedCookingFire" ? 1
                : command == "FeedCookingFireFull" || command == "StokeCookingFire" ? 3 : 0), timber);
            Assert.AreEqual(strikes - (command == "RelightCookingFire" || command == "StokeCookingFire" ? 1 : 0), striker.Strikes);
            if (command == "StokeCookingFire" || command == "RelightCookingFire")
                Assert.True(hearth.GetPart<LightSourcePart>().Enabled, "the work actually lit the hearth");
        }

        [TestCase("FeedCookingFireFull", false)] [TestCase("StokeCookingFire", false)]
        [TestCase("FeedCookingFireFull", true)] [TestCase("StokeCookingFire", true)]
        public void RealHearthWorkRefusalOrOuterFailureSpendsNothing(string command, bool veto)
        {
            var hearth = WorkHearth(command);
            var striker = Pack.Objects.Single(e => e.BlueprintName == "FlintStriker").GetPart<FlintStrikerPart>();
            int strikes = striker.Strikes;
            int spent = WorldMenu(hearth, (menu, input, turns) =>
            {
                var row = Rows(menu).Single(r => r.Name == command);
                if (veto) FailAfter();
                else foreach (var item in Pack.Objects.Where(e => e.BlueprintName == "SalvagedTimber"
                    || command == "StokeCookingFire" && e.BlueprintName == "FlintStriker").ToArray()) Pack.RemoveObject(item);
                return row;
            });
            Assert.AreEqual(0, spent);
            Assert.AreEqual(5, hearth.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(25, hearth.GetPart<ThermalPart>().Temperature);
            Assert.AreEqual(strikes, striker.Strikes);
            Assert.AreEqual(veto ? 3 : 0, Pack.Objects.Where(e => e.BlueprintName == "SalvagedTimber")
                .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
        }

        [TestCase("FeedCookingFireFull")] [TestCase("StokeCookingFire")]
        public void UndeclaredNewHearthWorkStillCommitsButLosesItsTurn_Control(string command)
        {
            var hearth = WorkHearth(command);
            int spent = WorldMenu(hearth, (menu, input, turns) =>
            {
                var row = Rows(menu).Single(r => r.Name == command);
                row.Time = ActionTime.Unspecified;
                return row;
            });
            Assert.AreEqual(0, spent, "reproduces the live timing bug through the real fallback");
            Assert.Greater(hearth.GetPart<FuelPart>().FuelMass, 5, "the resource mutation still happened");
        }

        [Test] public void DeclaredOneActionRowRunsInTransactionAndChargesOneTurn()
        {
            var bench = Probe("OneAction");
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(10, spent, "one action is one 10-tick turn");
        }

        [Test] public void UndeclaredRowWithTheSameCommandRunsButChargesNothing_Control()
        {
            var bench = Probe("Unspecified");
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(0, spent, "neither legacy chain lists the command, which is the gap S3 closes");
        }

        [Test] public void DeclaredFreeRowChargesNothingEvenWhenALegacyChainWouldCharge()
        {
            var bench = Probe("Free", command: "Harvest");
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(0, spent);
        }

        [Test] public void LegacyChainStillChargesAnUndeclaredHarvestRow_Control()
        {
            var bench = Probe("Unspecified", command: "Harvest");
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(10, spent, "the legacy world chain charges a committed Harvest exactly as before");
        }

        [Test] public void DeclaredRowThatDoesNotCommitChargesNothing()
        {
            var bench = Probe("OneAction", configure: p => p.Handles = false);
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(0, ActionTimeProbePart.Handled); Assert.AreEqual(0, spent, "a refusal is free");
        }

        [Test] public void DeclaredRowStillObeysTheReachGate()
        {
            var bench = Probe("OneAction", x: 16, y: 10);
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(0, ActionTimeProbePart.Handled); Assert.AreEqual(0, spent); StringAssert.Contains("out of reach", lastMessage);
        }

        [Test] public void DeclaredRowWithAnEmptyCommandFallsThroughToTheLegacyPathInsteadOfDispatching()
        {
            var bench = Probe("OneAction", command: "");
            int spent = 0;
            Assert.DoesNotThrow(() => spent = WorldMenu(bench, (m, i, t) => Row(m)));
            Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled);
        }

        // ── world menu: disabled rows ──

        [Test] public void DisabledRowIsListedAndSelectionLogsReasonWithoutExecuting()
        {
            var bench = Probe(configure: p => p.Disabled = true);
            int spent = WorldMenu(bench, (m, i, t) => Row(m), execute: false, after: (menu, input) =>
            {
                var row = Row(menu); Assert.True(row.IsDisabled); Assert.AreEqual("faulted", row.DisabledCode);
                typeof(WorldActionMenuUI).GetMethod("SelectAction", Hidden).Invoke(menu, new object[] { Rows(menu).IndexOf(row) });
                Assert.False(menu.SelectionMade, "selecting a disabled row selects nothing"); Assert.True(menu.IsOpen, "and keeps the menu open");
            });
            Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled); StringAssert.Contains("fouled", lastMessage);
        }

        [Test] public void AnEnabledNeighbourStillSelectsNormally_Control()
        {
            var bench = Probe("OneAction");
            WorldMenu(bench, (m, i, t) => Row(m), execute: false, after: (menu, input) =>
            {
                typeof(WorldActionMenuUI).GetMethod("SelectAction", Hidden).Invoke(menu, new object[] { Rows(menu).IndexOf(Row(menu)) });
                Assert.True(menu.SelectionMade); Assert.False(menu.IsOpen);
            });
        }

        [Test] public void DisabledHotkeyBehavesLikeEnter()
        {
            var scope = new InputTestFixture(); scope.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                var bench = Probe(configure: p => p.Disabled = true);
                WorldMenu(bench, (m, i, t) => Row(m), execute: false, after: (menu, input) =>
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); InputHelper.GetKey(KeyCode.None);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P)); InputSystem.Update(); InputHelper.GetKey(KeyCode.None);
                    menu.HandleInput();
                    Assert.False(menu.SelectionMade); Assert.True(menu.IsOpen); Assert.AreEqual(KeyCode.None, menu.SelectedActivationKey, "a disabled hotkey must not arm the activation key");
                });
                StringAssert.Contains("fouled", lastMessage);
            }
            finally { scope.TearDown(); }
        }

        [Test] public void DisabledRowReachedByExecuteStillOnlyReports()
        {
            var bench = Probe(configure: p => p.Disabled = true);
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled); StringAssert.Contains("fouled", lastMessage);
        }

        [Test] public void DisabledWinsOverDeclaredTime()
        {
            var bench = Probe("OneAction", configure: p => p.AlsoDisableDeclared = true);
            int spent = WorldMenu(bench, (m, i, t) => Row(m));
            Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled);
        }

        [Test] public void DisabledRowWithFireOnActorIsStillOnlyAReport()
        {
            var bench = Probe(configure: p => p.Disabled = true);
            int spent = WorldMenu(bench, (m, i, t) => { var row = Row(m); row.FireOnActor = true; return row; });
            Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled);
        }

        [Test] public void ActionDisabledDiagIsRecordedWithCodeCommandAndName()
        {
            var bench = Probe(configure: p => p.Disabled = true);
            WorldMenu(bench, (m, i, t) => Row(m));
            var record = Diag.Snapshot(50).Single(r => r.Category == "event" && r.Kind == "ActionDisabled");
            StringAssert.Contains("faulted", record.PayloadJson); StringAssert.Contains("Disabled:faulted", record.PayloadJson); StringAssert.Contains("Probe", record.PayloadJson);
        }

        [Test] public void NoDiagWhenNothingIsDisabled()
        {
            var bench = Probe("OneAction"); WorldMenu(bench, (m, i, t) => Row(m));
            Assert.IsEmpty(Diag.Snapshot(50).Where(r => r.Kind == "ActionDisabled"));
        }

        // ── pack menu ──

        (InventoryUI menu, InputHandler input, HotbarSaveFixture ui, TurnManager turns, IList actions, object popup, Type type) PackMenu(Entity item)
        {
            var ui = new HotbarSaveFixture(true, false);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true); manager.SetActiveZone(Zone);
            var turns = new TurnManager(); turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
            ui.BindOld(GameSessionState.Capture("action-time", "pack", manager, turns, Actor));
            var menu = ui.Root.AddComponent<InventoryUI>(); menu.PlayerEntity = Actor; menu.CurrentZone = Zone; menu.EntityFactory = Factory; ui.Input.InventoryUI = menu;
            var inputState = typeof(InputHandler).GetField("_inputState", Hidden); inputState.SetValue(ui.Input, Enum.Parse(inputState.FieldType, "InventoryOpen")); menu.Open();
            var display = InventoryScreenData.Build(Actor).Categories.SelectMany(c => c.Items).Single(i => i.Item == item);
            typeof(InventoryUI).GetMethod("OpenItemActionPopup", Hidden).Invoke(menu, new object[] { display });
            var popup = typeof(InventoryUI).GetField("_itemActionPopup", Hidden).GetValue(menu); var type = popup.GetType();
            return (menu, ui.Input, ui, turns, (IList)type.GetField("Actions").GetValue(popup), popup, type);
        }

        static object Field(object o, string name) => o.GetType().GetField(name).GetValue(o);

        int PressEnterOnProbeRow(Entity item, out string label, out bool popupStillOpen, out IList actionsSeen,
            string prefix = "probe the thing", Action<object> configure = null)
        {
            var scope = new InputTestFixture(); scope.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                var p = PackMenu(item);
                try
                {
                    int index = Enumerable.Range(0, p.actions.Count).Single(i => ((string)Field(p.actions[i], "Label")).StartsWith(prefix));
                    label = (string)Field(p.actions[index], "Label"); actionsSeen = p.actions;
                    if (configure != null) { var action = p.actions[index]; configure(action); p.actions[index] = action; }
                    p.type.GetField("CursorIndex").SetValue(p.popup, index);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); InputHelper.GetKey(KeyCode.None);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update(); InputHelper.GetKey(KeyCode.None);
                    int before = p.turns.TickCount;
                    typeof(InputHandler).GetMethod("HandleInventoryInput", Hidden).Invoke(p.input, null);
                    popupStillOpen = typeof(InventoryUI).GetField("_itemActionPopup", Hidden).GetValue(p.menu) != null;
                    lastMessage = MessageLog.GetLast();
                    return p.turns.TickCount - before;
                }
                finally { p.ui.Dispose(); }
            }
            finally { scope.TearDown(); }
        }

        [TestCase("success", 1)] [TestCase("success", 3)]
        [TestCase("stale", 1)] [TestCase("veto", 1)] [TestCase("undeclared", 1)]
        public void RealProofingPressChargesOnlyForCommittedBottles(string mode, int presses)
        {
            var reagent = Carry("FrostLichen"); Carry("FrostLichen"); Carry("FrostLichen");
            var flask = Flask(0); var vessel = flask.GetPart<LiquidVesselPart>();
            int spent = 0;
            for (int i = 0; i < presses; i++)
                spent += PressEnterOnProbeRow(reagent, out _, out _, out _, "press into liquid flask", action =>
                {
                    if (mode == "stale") { vessel.LiquidId = "bog-mire"; vessel.Volume = 1; }
                    if (mode == "veto") FailAfter();
                    if (mode == "undeclared") action.GetType().GetField("TimeDeclared").SetValue(action, false);
                });
            Assert.Contains(flask, Pack.Objects, "the result must be on the currently carried flask");
            vessel = flask.GetPart<LiquidVesselPart>();
            Assert.AreSame(flask, vessel.ParentEntity, "read the current owned Part, not a payment snapshot's detached reference");
            bool committed = mode == "success" || mode == "undeclared";
            Assert.AreEqual(mode == "success" ? presses * 10 : 0, spent);
            Assert.AreEqual(committed ? presses * 2 : mode == "stale" ? 1 : 0, vessel.Volume);
            Assert.AreEqual(committed ? "cold-proofing" : mode == "stale" ? "bog-mire" : "", vessel.LiquidId);
            Assert.AreEqual(3 - (committed ? presses : 0), Pack.Objects.Where(e => e.BlueprintName == "FrostLichen")
                .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
        }

        [Test] public void PackMenuCopiesDeclaredFieldsToItemAction()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "OneAction" });
            var p = PackMenu(item);
            try
            {
                var row = p.actions.Cast<object>().Single(a => ((string)Field(a, "Command")) == "ProbeDeclared");
                Assert.True((bool)Field(row, "TimeDeclared")); Assert.AreEqual(ActionTime.OneAction, (ActionTime)Field(row, "Time")); Assert.False((bool)row.GetType().GetProperty("IsDisabled").GetValue(row));
            }
            finally { p.ui.Dispose(); }
        }

        [Test] public void PackMenuUndeclaredRowIsNotMistakenForFreeBecauseAStructDefaultsToZero()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "Unspecified" });
            var p = PackMenu(item);
            try
            {
                var row = p.actions.Cast<object>().Single(a => ((string)Field(a, "Command")) == "ProbeDeclared");
                Assert.False((bool)Field(row, "TimeDeclared"), "the legacy chain must keep deciding");
            }
            finally { p.ui.Dispose(); }
        }

        [Test] public void DeclaredOneActionPackRowChargesOneTurn()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "OneAction" });
            int spent = PressEnterOnProbeRow(item, out _, out bool open, out _);
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.False(open, "a committed row closes the popup"); Assert.AreEqual(10, spent, "the inventory hands the turn to the input handler, which charges one action");
        }

        [Test] public void DeclaredFreePackRowChargesNothing()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "Free", Command = "Harvest" });
            int spent = PressEnterOnProbeRow(item, out _, out _, out _);
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(0, spent, "declared Free beats a legacy chain that charges Harvest");
        }

        [Test] public void UndeclaredHarvestPackRowStillChargesLikeBefore_Control()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "Unspecified", Command = "Harvest" });
            int spent = PressEnterOnProbeRow(item, out _, out _, out _);
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(10, spent);
        }

        [Test] public void UndeclaredUnknownPackRowChargesNothing_Control()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "Unspecified" });
            int spent = PressEnterOnProbeRow(item, out _, out bool open, out _);
            Assert.AreEqual(1, ActionTimeProbePart.Handled); Assert.AreEqual(0, spent); Assert.False(open);
        }

        [Test] public void DisabledPackRowIsDimmedByLabelAndDoesNothing()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Disabled = true });
            int spent = PressEnterOnProbeRow(item, out string label, out bool open, out _);
            StringAssert.EndsWith("(unavailable)", label); Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled); Assert.True(open, "the popup keeps the same row for another try");
            StringAssert.Contains("fouled", lastMessage);
            Assert.True(Diag.Snapshot(50).Any(r => r.Kind == "ActionDisabled"));
        }

        [Test] public void DisabledWinsOverDeclaredTimeInThePackMenuToo()
        {
            var item = Carry("Dagger"); item.AddPart(new ActionTimeProbePart { Time = "OneAction", AlsoDisableDeclared = true });
            int spent = PressEnterOnProbeRow(item, out _, out bool open, out _);
            Assert.AreEqual(0, spent); Assert.AreEqual(0, ActionTimeProbePart.Handled); Assert.True(open);
        }

        // ── the legacy pack chain is untouched ──

        [TestCase("FillWaterskin", true)] [TestCase("DrinkWaterskin", true)] [TestCase("Cook", true)] [TestCase("LightTorch", true)]
        [TestCase("ExtinguishTorch", true)] [TestCase("Harvest", true)] [TestCase("Examine", false)] [TestCase("Take", false)] [TestCase("ProbeDeclared", false)]
        public void LegacyPackChargeTableIsUnchanged(string command, bool charges)
        {
            var method = typeof(InventoryUI).GetMethod("LegacyPackActionSpendsTurn", HiddenStatic); Assert.NotNull(method);
            var itemActionType = typeof(InventoryUI).GetNestedType("ItemAction", BindingFlags.NonPublic);
            var action = Activator.CreateInstance(itemActionType); itemActionType.GetField("Command").SetValue(action, command);
            var item = Carry("Dagger"); var proof = SelfConsumptionTurnProof.Capture(Actor, item, command);
            Assert.AreEqual(charges, (bool)method.Invoke(null, new object[] { action, item, proof, true }));
        }
    }
}
