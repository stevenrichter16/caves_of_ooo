using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A stand-in tool: it offers rows the way a real tool does (tagging the pack row with the cell it acts on, or answering the world-cell event), and counts how often it is run.</summary>
    public sealed class WorldToolProbePart : Part
    {
        public override string Name => "WorldToolProbe";

        public sealed class Row
        {
            public string Display = "probe it", Command = "ProbeIt"; public int X, Y; public bool Tagged = true; public ActionTime Time = ActionTime.Unspecified; public char Key = 'k'; public int Priority = 10; public string Disabled = "";
            public Entity Target; public Zone TargetZone; public bool FireOnActor, Shared; public string Group = "";
        }

        public readonly List<Row> PackRows = new List<Row>(), EventRows = new List<Row>();
        public int Runs; public string LastCommand;

        InventoryAction Add(InventoryActionList list, Row r)
        {
            var action = string.IsNullOrEmpty(r.Disabled) ? list.AddAction("Probe", r.Display, r.Command, r.Key, r.Priority, r.FireOnActor, r.Time) : list.AddDisabled("Probe", r.Display, "probe-disabled", r.Disabled, r.Key, r.Priority);
            if (r.Target != null) action.AtEntity(r.TargetZone, r.Target); else if (r.Tagged) action.AtCell(r.X, r.Y);
            action.Shared = r.Shared; action.Group = r.Group; return action;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { var list = e.GetParameter<InventoryActionList>("Actions"); if (list != null) foreach (var r in PackRows) Add(list, r); }
            if (e.ID == WorldInteractionSystem.WorldCellActionsEvent)
            {
                var list = e.GetParameter<InventoryActionList>("Actions"); var cell = e.GetParameter<Cell>("Cell");
                if (list != null && cell != null) foreach (var r in EventRows) if (r.X == cell.X && r.Y == cell.Y) { var a = list.AddAction("Probe", r.Display, r.Command, r.Key, r.Priority, r.FireOnActor, r.Time); }
            }
            if (e.ID == "InventoryAction" && (e.GetStringParameter("Command") ?? "").StartsWith("ProbeIt", System.StringComparison.Ordinal)) { Runs++; LastCommand = e.GetStringParameter("Command"); e.Handled = true; return false; }
            return true;
        }
    }

    /// <summary>
    /// The interact (C) menu of a cell offers what the tools you carry can do to that cell, so a tool never needs the pack opened (Docs/WORLD-TOOL-ROWS.md). A tool tags the row it already shows in the pack with the cells it acts on
    /// (<see cref="InventoryAction.AtCell"/>), or answers the world-cell event for a cell; <see cref="WorldInteractionSystem.GatherCellActions"/> collects those rows from the carried items, runs them through the carrying item, and
    /// keeps them below the target's own rows so the first row of a menu is never one of them.
    /// </summary>
    public sealed class WorldToolRowsTests : FiftyWorldFixture
    {
        [SetUp] public void SetupRows() { Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 1000; }

        Entity Tool(out WorldToolProbePart probe, string blueprint = "IronSpikeComponent")
        { var item = Carry(blueprint); probe = new WorldToolProbePart(); item.AddPart(probe); return item; }

        List<InventoryAction> Menu(int x, int y) { var cell = Zone.GetCell(x, y); return WorldInteractionSystem.GatherCellActions(WorldInteractionSystem.ResolveTarget(cell, Actor), cell, Actor); }
        InventoryAction[] ProbeRows(int x, int y) => Menu(x, y).Where(a => a.Name == "Probe").ToArray();
        bool Choose(InventoryAction row, Entity target) => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(row.Via ?? target, row.Command), Actor, Zone).Success;
        static string Base(Entity item) => item.GetPart<RenderPart>()?.DisplayName ?? item.BlueprintName;

        // ── what is offered, and where ──

        [Test] public void ATaggedRowIsOfferedOnTheTaggedCellAndOnNoOtherCell()
        {
            var item = Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10 });
            Place("Grass", 11, 10); Place("Grass", 9, 10); Place("Grass", 11, 11);
            Assert.AreEqual(1, ProbeRows(11, 10).Length, "the cell it acts on"); Assert.IsEmpty(ProbeRows(9, 10), "the same row, another column"); Assert.IsEmpty(ProbeRows(11, 11), "the same column, another row"); Assert.IsEmpty(ProbeRows(10, 11), "a cell with nothing tagged");
            Assert.AreEqual(1, Actions(item).Count(a => a.Name == "Probe"), "the pack row is unchanged and still there");
        }

        [Test] public void ARowTheItemNeverTaggedIsNotOffered_Counter()
        {
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Tagged = false }); Place("Grass", 11, 10);
            Assert.IsEmpty(ProbeRows(11, 10), "a row about the item or the actor, not a place, stays in the pack");
        }

        [Test] public void TheRowIsRunByTheCarryingItem_AndCostsOneActionUnlessItDeclaresOtherwise()
        {
            var item = Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10 }); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Command = "ProbeIt|free", Display = "probe freely", Time = ActionTime.Free });
            probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Command = "ProbeIt|one", Display = "probe once", Time = ActionTime.OneAction }); var grass = Place("Grass", 11, 10);
            var rows = ProbeRows(11, 10); Assert.AreEqual(3, rows.Length);
            foreach (var row in rows) Assert.AreSame(item, row.Via, row.Display);
            Assert.AreEqual(ActionTime.OneAction, rows.Single(r => r.Command == "ProbeIt").Time, "a legacy row costs what the pack charges for it: one action");
            Assert.AreEqual(ActionTime.Free, rows.Single(r => r.Command == "ProbeIt|free").Time, "a declared time is kept"); Assert.AreEqual(ActionTime.OneAction, rows.Single(r => r.Command == "ProbeIt|one").Time);
            Assert.True(Choose(rows.Single(r => r.Command == "ProbeIt|free"), grass)); Assert.AreEqual(1, probe.Runs, "the item ran it, not the grass"); Assert.AreEqual("ProbeIt|free", probe.LastCommand);
        }

        [Test] public void HotkeysAreNotCarriedIntoTheCellMenu_SoTheyCannotClashWithItsOwn()
        {
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Key = 'x' }); Place("Grass", 11, 10);
            Assert.AreEqual('\0', ProbeRows(11, 10).Single().Key);
        }

        [Test] public void ADisabledRowKeepsItsReason_AndIsNeverGivenSomethingToRun()
        {
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Disabled = "It is spent." }); Place("Grass", 11, 10);
            var row = ProbeRows(11, 10).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("It is spent.", row.DisabledReason); StringAssert.StartsWith(InventoryAction.DisabledCommandPrefix, row.Command);
        }

        [Test] public void TheItemIsNamedWhenTheLabelDoesNotName_AndNotWhenItAlreadyDoes()
        {
            var item = Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "set down east", Command = "ProbeIt|a" });
            probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "lay the " + Base(item) + " east", Command = "ProbeIt|b" }); Place("Grass", 11, 10);
            var rows = ProbeRows(11, 10); StringAssert.Contains(Base(item), rows.Single(r => r.Command == "ProbeIt|a").Display, "a label that does not say which item is told which");
            Assert.AreEqual("lay the " + Base(item) + " east", rows.Single(r => r.Command == "ProbeIt|b").Display, "a label that already names it is left alone");
        }

        [Test] public void TwoDifferentItemsOfferingTheSameLabelAreToldApartByName()
        {
            var a = Tool(out var pa, "Waterskin"); var b = Tool(out var pb, "IronSpikeComponent"); Place("Grass", 11, 10);
            pa.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "pour it here", Command = "ProbeIt|a" }); pb.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "pour it here", Command = "ProbeIt|b" });
            var rows = ProbeRows(11, 10); Assert.AreEqual(2, rows.Length); Assert.AreEqual(2, rows.Select(r => r.Display).Distinct().Count(), "no two rows read the same: " + string.Join(" | ", rows.Select(r => r.Display)));
            Assert.AreSame(a, rows.Single(r => r.Command == "ProbeIt|a").Via); Assert.AreSame(b, rows.Single(r => r.Command == "ProbeIt|b").Via);
        }

        [Test] public void IdenticalItemsOfferingTheSameRowCollapseToOne_ButItemsThatReadDifferentlyStay()
        {
            var a = Tool(out var pa, "Waterskin"); var b = Tool(out var pb, "Waterskin"); var c = Tool(out var pc, "Waterskin"); Assert.AreEqual(3, new[] { a, b, c }.Distinct().Count(), "fixture: three separate skins"); Place("Grass", 11, 10);
            foreach (var p in new[] { pa, pb }) p.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "pour it here (3)", Command = "ProbeIt|same" });
            pc.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "pour it here (5)", Command = "ProbeIt|other" });
            var rows = ProbeRows(11, 10); Assert.AreEqual(2, rows.Length, string.Join(" | ", rows.Select(r => r.Display))); Assert.AreEqual(1, rows.Count(r => r.Command == "ProbeIt|same"), "the same choice is offered once"); Assert.AreEqual(1, rows.Count(r => r.Command == "ProbeIt|other"));
        }

        [Test] public void ASharedRowIsOneChoiceWhicheverItemOffersIt_AndNamesNoItem()
        {
            var a = Tool(out var pa, "Waterskin"); var b = Tool(out var pb, "Waterskin"); Assert.AreNotSame(a, b, "fixture: two separate skins"); Place("Grass", 11, 10);
            a.GetPart<WaterskinPart>().Charges = 3; b.GetPart<WaterskinPart>().Charges = 1;   // they hold differently, which would tell ordinary rows apart: a shared row is about the place
            pa.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "make camp here", Command = "ProbeIt|a", Shared = true }); pb.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "make camp here", Command = "ProbeIt|b", Shared = true });
            var rows = ProbeRows(11, 10); Assert.AreEqual(1, rows.Length, string.Join(" | ", rows.Select(r => r.Display))); Assert.AreEqual("make camp here", rows[0].Display, "a row about the place names no item and no state");
            Assert.IsNotNull(rows[0].Via); Assert.True(rows[0].Via == a || rows[0].Via == b, "run by one of the items that offered it");
        }

        [Test] public void ItemsOfTheSameNameThatHoldDifferentlyAreToldApartByWhatTheyHold_AndThoseThatHoldTheSameCollapse()
        {
            var full = Tool(out var pf, "Waterskin"); var low = Tool(out var pl, "Waterskin"); var alsoFull = Tool(out var pa, "Waterskin"); Place("Grass", 11, 10);
            full.GetPart<WaterskinPart>().Charges = 3; alsoFull.GetPart<WaterskinPart>().Charges = 3; low.GetPart<WaterskinPart>().Charges = 1;
            foreach (var p in new[] { pf, pl, pa }) p.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "douse it (1 water)", Command = "ProbeIt|douse" });
            var rows = ProbeRows(11, 10); Assert.AreEqual(2, rows.Length, string.Join(" | ", rows.Select(r => r.Display)));
            Assert.AreEqual(1, rows.Count(r => r.Display.Contains("(3/3)") && (r.Via == full || r.Via == alsoFull)), "the two full skins are one choice"); Assert.AreEqual(1, rows.Count(r => r.Display.Contains("(1/3)") && r.Via == low), "the low one says so");
        }

        [Test] public void StacksOfDifferentSizeAreToldApart_AndTheRowOfTheOtherStackIsNotLost()
        {
            var big = Carry("IronSpikeComponent"); big.GetPart<StackerPart>().StackCount = 3; Place("Grass", 11, 10);
            var separation = new SeparateOneCommand(big); Assert.True(InventorySystem.ExecuteCommand(separation, Actor, Zone).Success, "fixture: one spike split off"); var single = separation.SeparatedItem; Assert.AreNotSame(big, single);
            var pb = new WorldToolProbePart(); big.AddPart(pb); var ps = new WorldToolProbePart(); single.AddPart(ps);
            foreach (var p in new[] { pb, ps }) p.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "cook this stack", Command = "ProbeIt|cook" });
            var rows = ProbeRows(11, 10); Assert.AreEqual(2, rows.Length, string.Join(" | ", rows.Select(r => r.Display))); Assert.AreEqual(1, rows.Count(r => r.Via == big && r.Display.Contains("(x2)")), "the stack of two says so"); Assert.AreEqual(1, rows.Count(r => r.Via == single), "and the single is still offered");
        }

        [Test] public void OfTwoRowsThatReadAlikeTheOneYouCanUseIsKept_NotTheOneWithTheLowerId()
        {
            var a = Tool(out var pa, "Waterskin"); var b = Tool(out var pb, "Waterskin"); Assert.AreNotSame(a, b); a.ID = "aaa-item"; b.ID = "bbb-item"; Place("Grass", 11, 10);
            pa.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "fill it", Command = "ProbeIt|fill", Disabled = "It is spent." }); pb.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "fill it", Command = "ProbeIt|fill" });
            var rows = ProbeRows(11, 10); Assert.AreEqual(1, rows.Length); Assert.False(rows[0].IsDisabled, "the usable row is kept"); Assert.AreSame(b, rows[0].Via);
        }

        [Test] public void ItemsThatHoldTheSameAreNotGivenAStateSuffix_Counter()
        {
            var a = Tool(out var pa, "Waterskin"); var b = Tool(out var pb, "IronSpikeComponent"); Place("Grass", 11, 10); a.GetPart<WaterskinPart>().Charges = 2;
            pa.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "pour it here", Command = "ProbeIt|a" }); pb.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "pour it here", Command = "ProbeIt|b" });
            foreach (var r in ProbeRows(11, 10)) StringAssert.DoesNotContain("(2/3)", r.Display, "different names already read differently; no state is added");
        }

        // ── grouping: rows that are the same kind of work collapse into one row that opens them ──

        WorldToolProbePart.Row Grouped(string display, string command, string group, int priority = 10) => new WorldToolProbePart.Row { X = 11, Y = 10, Display = display, Command = command, Group = group, Priority = priority };
        InventoryAction[] GroupRows(int x, int y) => Menu(x, y).Where(a => a.Name == "ToolGroup").ToArray();
        const string Heading = "do a thing here";

        [Test] public void TwoRowsUnderAHeadingStayWhereTheyAre_Counter()
        {
            Tool(out var p); Place("Grass", 11, 10); p.PackRows.Add(Grouped("a", "ProbeIt|a", Heading)); p.PackRows.Add(Grouped("b", "ProbeIt|b", Heading));
            Assert.AreEqual(2, ProbeRows(11, 10).Length, "two is not a wall"); Assert.IsEmpty(GroupRows(11, 10));
        }

        [Test] public void ThreeRowsUnderAHeadingBecomeOneRowThatSaysHowManyAndOpensThem()
        {
            var item = Tool(out var p); Place("Grass", 11, 10); foreach (var n in new[] { "a", "b", "c" }) p.PackRows.Add(Grouped("do " + n, "ProbeIt|" + n, Heading));
            var group = GroupRows(11, 10); Assert.AreEqual(1, group.Length); Assert.AreEqual(Heading + " (3) >>", group[0].Display); Assert.AreEqual(WorldInteractionSystem.ToolGroupPrefix + Heading, group[0].Command); Assert.IsNull(group[0].Via, "opening a list is menu navigation, run by no tool");
            Assert.IsEmpty(ProbeRows(11, 10), "the three rows are not also shown inline"); StringAssert.Contains("Choose from: " + Base(item), group[0].Hint);
            var list = WorldInteractionSystem.BuildToolGroupActions(Zone.GetCell(11, 10), Actor, Heading); Assert.AreEqual(4, list.Count); CollectionAssert.AreEquivalent(new[] { "ProbeIt|a", "ProbeIt|b", "ProbeIt|c" }, list.Take(3).Select(a => a.Command).ToArray());
            Assert.AreEqual(WorldInteractionSystem.ToolGroupBackPrefix + Heading, list[3].Command, "the list ends with a way back"); Assert.True(list.Take(3).All(a => a.Via == item), "each listed row is still run by its tool");
        }

        [Test] public void RowsWithNoHeadingAreNeverGrouped_HoweverManyThereAre_Counter()
        {
            Tool(out var p); Place("Grass", 11, 10); foreach (var n in new[] { "a", "b", "c", "d", "e" }) p.PackRows.Add(Grouped("do " + n, "ProbeIt|" + n, ""));
            Assert.AreEqual(5, ProbeRows(11, 10).Length); Assert.IsEmpty(GroupRows(11, 10));
        }

        [Test] public void TwoHeadingsGroupSeparately_AndOnlyTheCrowdedOneCollapses()
        {
            Tool(out var p); Place("Grass", 11, 10);
            foreach (var n in new[] { "a", "b", "c" }) p.PackRows.Add(Grouped("lay " + n, "ProbeIt|lay" + n, "lay it here")); foreach (var n in new[] { "x", "y" }) p.PackRows.Add(Grouped("pour " + n, "ProbeIt|pour" + n, "pour it here"));
            var group = GroupRows(11, 10); Assert.AreEqual(1, group.Length); StringAssert.StartsWith("lay it here", group[0].Display); Assert.AreEqual(2, ProbeRows(11, 10).Count(r => r.Command.StartsWith("ProbeIt|pour")), "the two-row heading stays inline");
            var list = WorldInteractionSystem.BuildToolGroupActions(Zone.GetCell(11, 10), Actor, "lay it here"); Assert.AreEqual(4, list.Count, "only that heading's three rows and the way back"); Assert.True(list.Take(3).All(a => a.Command.StartsWith("ProbeIt|lay")));
        }

        [Test] public void IdenticalItemsCollapseBeforeTheyAreCounted_SoFourOfOneThingDoNotMakeAGroup()
        {
            Place("Grass", 11, 10); for (int i = 0; i < 4; i++) { var skin = Tool(out var p, "Waterskin"); p.PackRows.Add(Grouped("pour it here", "ProbeIt|pour", Heading)); }
            Assert.AreEqual(1, ProbeRows(11, 10).Length, "four identical skins are one choice"); Assert.IsEmpty(GroupRows(11, 10), "and one choice is not a crowd");
        }

        [Test] public void TheGroupRowSitsAmongTheToolRowsAtThePriorityOfItsBestRow_AndTheFirstRowIsStillNeverATool()
        {
            Tool(out var p); var grass = Place("Grass", 11, 10); p.PackRows.Add(Grouped("lone", "ProbeIt|lone", "", 20));
            p.PackRows.Add(Grouped("a", "ProbeIt|a", Heading, 5)); p.PackRows.Add(Grouped("b", "ProbeIt|b", Heading, 30)); p.PackRows.Add(Grouped("c", "ProbeIt|c", Heading, 7));
            var own = WorldInteractionSystem.GatherActions(grass, Actor); var menu = Menu(11, 10); var tools = menu.Skip(own.Count).ToList();
            Assert.AreEqual(2, tools.Count); Assert.AreEqual("ToolGroup", tools[0].Name, "the group carries its best row's priority (30), so it leads the 20"); Assert.AreEqual(30, tools[0].Priority); Assert.AreEqual("ProbeIt|lone", tools[1].Command);
            Assert.AreEqual(own[0].Command, menu[0].Command, "the cell's own first row still leads");
        }

        [Test] public void ADisabledRowCountsTowardAGroupAndIsListedInIt()
        {
            Tool(out var p); Place("Grass", 11, 10); p.PackRows.Add(Grouped("a", "ProbeIt|a", Heading)); p.PackRows.Add(Grouped("b", "ProbeIt|b", Heading)); p.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "c", Command = "ProbeIt|c", Group = Heading, Disabled = "Not now." });
            Assert.AreEqual(1, GroupRows(11, 10).Length); Assert.True(WorldInteractionSystem.BuildToolGroupActions(Zone.GetCell(11, 10), Actor, Heading).Any(a => a.IsDisabled), "the refusal is in the list, where the player will look");
        }

        [Test] public void GroupCommandsRoundTripTheirHeading_AndNothingElseIsAGroupCommand_Counter()
        {
            Assert.True(WorldInteractionSystem.IsToolGroupCommand(WorldInteractionSystem.ToolGroupPrefix + Heading)); Assert.True(WorldInteractionSystem.IsToolGroupCommand(WorldInteractionSystem.ToolGroupBackPrefix + Heading));
            Assert.AreEqual(Heading, WorldInteractionSystem.ToolGroupHeading(WorldInteractionSystem.ToolGroupPrefix + Heading)); Assert.AreEqual(Heading, WorldInteractionSystem.ToolGroupHeading(WorldInteractionSystem.ToolGroupBackPrefix + Heading));
            foreach (var other in new[] { "Examine", "PickCell", "ProbeIt|a", "ToolGroupish", "", null }) { Assert.False(WorldInteractionSystem.IsToolGroupCommand(other), other); Assert.IsNull(WorldInteractionSystem.ToolGroupHeading(other)); }
            Assert.AreEqual("Do a thing here", WorldInteractionSystem.ToolGroupTitle(Heading));
        }

        [Test] public void AnItemNotCarriedOffersNothing_Counter()
        {
            var item = Factory.CreateEntity("IronSpikeComponent"); var probe = new WorldToolProbePart(); item.AddPart(probe); Assert.True(Zone.AddEntity(item, 9, 9));
            probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10 }); Place("Grass", 11, 10);
            Assert.IsEmpty(ProbeRows(11, 10), "a tool on the ground is not a tool you carry");
        }

        [Test] public void ARowAnItemAnswersForTheWorldCellEventIsOfferedOnThatCellOnly()
        {
            var item = Tool(out var probe); probe.EventRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Command = "ProbeIt|event" }); Place("Grass", 11, 10); Place("Grass", 10, 11);
            var row = ProbeRows(11, 10).Single(); Assert.AreSame(item, row.Via, "the dispatcher fills in the carrying item"); Assert.AreEqual(ActionTime.OneAction, row.Time); Assert.IsEmpty(ProbeRows(10, 11));
        }

        [Test] public void ARowTaggedWithABigThingIsOfferedOnEveryCellItOccupies_AndOnNoOther()
        {
            var body = new Entity { ID = "big-thing", BlueprintName = "big" }; body.AddPart(new PhysicsPart()); body.AddPart(new RenderPart { DisplayName = "big thing", RenderLayer = 5 });
            body.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" }); body.AddPart(new DestructiblePart { HP = 10, MaxHP = 10 }); Assert.True(Zone.AddEntity(body, 11, 10));
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { Target = body, TargetZone = Zone });
            Assert.AreEqual(1, ProbeRows(11, 10).Length, "its anchor"); Assert.AreEqual(1, ProbeRows(12, 10).Length, "the other cell it occupies"); Assert.IsEmpty(ProbeRows(13, 10), "a cell it does not occupy");
        }

        [Test] public void ARowMeantToFireOnTheActorIsNotOfferedBecauseTheCarryingItemCouldNotRunIt_Counter()
        {
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, FireOnActor = true }); Place("Grass", 11, 10); Assert.IsEmpty(ProbeRows(11, 10));
        }

        // ── when nothing is offered ──

        [Test] public void NothingIsOfferedToAnActorWhoCannotActNow_Counter()
        {
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10 }); Place("Grass", 11, 10); Assert.AreEqual(1, ProbeRows(11, 10).Length, "fixture: offered while able");
            Actor.GetStat("Hitpoints").BaseValue = 0; Assert.IsEmpty(ProbeRows(11, 10), "a dead actor is offered no tool rows");
        }

        // ── order, the pile summary, and no side effects ──

        [Test] public void ToolRowsComeAfterTheTargetsOwnRows_SoTheFirstRowIsNeverATool_AndAreSortedByPriority()
        {
            var item = Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "low", Command = "ProbeIt|low", Priority = 5 }); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "high", Command = "ProbeIt|high", Priority = 30 });
            var grass = Place("Grass", 11, 10); var own = WorldInteractionSystem.GatherActions(grass, Actor); Assert.IsNotEmpty(own, "fixture: the cell's own menu has a row");
            var menu = Menu(11, 10); for (int i = 0; i < own.Count; i++) Assert.AreEqual(own[i].Command, menu[i].Command, "the target's own rows lead, in order");
            var tools = menu.Skip(own.Count).ToList(); Assert.AreEqual(new[] { "ProbeIt|high", "ProbeIt|low" }, tools.Select(a => a.Command).ToArray(), "then the tool rows, most prominent first");
        }

        [Test] public void ThePileSummaryOfACellCarriesItsToolRowsToo()
        {
            Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10 }); Place("Grass", 11, 10); Place("IronSpikeComponent", 11, 10); Place("IronSpikeComponent", 11, 10);
            var cell = Zone.GetCell(11, 10); Assert.True(WorldInteractionSystem.IsPileCell(cell, Actor), "fixture: a pile");
            Assert.AreEqual(1, WorldInteractionSystem.BuildPileSummaryActions(cell, Actor).Count(a => a.Name == "Probe"));
        }

        [Test] public void BuildingTheMenuChangesNothing_NoPackChangeNoDiagNoMessage()
        {
            var item = Tool(out var probe); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10 }); Place("Grass", 11, 10);
            var before = Pack.Objects.ToList(); int diag = Diag.Snapshot(500).Count; int messages = MessageLog.GetRecent(50).Count();
            Menu(11, 10); Menu(11, 10);
            CollectionAssert.AreEqual(before, Pack.Objects.ToList()); Assert.AreEqual(diag, Diag.Snapshot(500).Count, "opening a menu records nothing"); Assert.AreEqual(messages, MessageLog.GetRecent(50).Count()); Assert.AreEqual(0, probe.Runs);
        }
    }
}
