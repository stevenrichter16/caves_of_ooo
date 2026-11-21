using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The tool rows of the interact (C) menu through the real input handler (Docs/WORLD-TOOL-ROWS.md): the menu opened on a cell offers the carried tool's row, choosing it runs the tool and pays exactly one action only when it
    /// commits, and the details reader (F1) rebuilds the menu with the same rows, so reading about a row never drops the tool rows from the menu it returns to.
    /// </summary>
    public sealed class WorldToolRowsInputTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        sealed class Scene : IDisposable
        {
            readonly EntityFactory oldHarvest = HarvestablePart.Factory;   // a static the fixture does not snapshot: put it back so later tests do not inherit this scene's factory
            public HotbarSaveFixture Ui; public HaulingContentScope Content; public OverworldZoneManager Manager; public Zone Zone; public Entity Player, Strip, Grass; public Cell Cell; public TurnManager Turns; public WorldActionMenuUI Menu;
            public Scene()
            {
                Ui = new HotbarSaveFixture(true, false); Content = new HaulingContentScope(); Content.Seed(64);
                Manager = OverworldZoneManager.CreateDetached(Content.Factory, 64, true); Zone = new Zone("Overworld.11.10.0"); Manager.SetActiveZone(Zone);
                Player = Content.Factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(Player, 4, 4));
                Grass = Content.Factory.CreateEntity("Grass"); Assert.True(Zone.AddEntity(Grass, 5, 4)); Cell = Zone.GetCell(5, 4); Cell.IsVisible = Cell.Explored = true;
                Strip = Content.Factory.CreateEntity("HushStrip"); Assert.True(Player.GetPart<InventoryPart>().AddObject(Strip)); HarvestablePart.Factory = Content.Factory;
                Turns = new TurnManager();
                Turns.RestoreSavedState(17, true, Player, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold } });
                Ui.BindOld(GameSessionState.Capture("tool-rows-input", "actual-menu", Manager, Turns, Player)); SettlementRuntime.ActiveZone = Zone;
                Menu = Ui.Root.AddComponent<WorldActionMenuUI>(); Ui.Input.WorldActionMenuUI = Menu;
                var returnState = typeof(InputHandler).GetField("_worldActionMenuReturnState", Private); returnState.SetValue(Ui.Input, Enum.Parse(returnState.FieldType, "Normal"));
            }
            public void Open() => typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Private).Invoke(Ui.Input, new object[] { Grass, Cell, false });
            public List<InventoryAction> Actions => (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(Menu);
            public void Choose(InventoryAction action) => typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Private).Invoke(Ui.Input, new object[] { action, Menu.SelectedTarget, Menu.SelectedCell, Menu.SelectedCellIsPile });
            public void Dispose() { Content?.Dispose(); Ui?.Dispose(); HarvestablePart.Factory = oldHarvest; }
        }

        [Test] public void ChoosingAToolRowOnTheRealMenuRunsTheToolAndPaysExactlyOneAction()
        {
            using (var s = new Scene())
            {
                s.Open(); Assert.True(s.Menu.IsOpen); var row = s.Actions.SingleOrDefault(a => a.Name == "LayDevice"); Assert.NotNull(row, string.Join(" / ", s.Actions.Select(a => a.Display)));
                Assert.AreSame(s.Strip, row.Via); Assert.AreEqual(ActionTime.OneAction, row.Time); int tick = s.Turns.TickCount, energy = s.Turns.GetEnergy(s.Player);
                Assert.AreNotEqual("LayDevice", s.Actions[0].Name, "the first row of the menu is never a tool, so C, direction, Enter still examines");
                s.Choose(row);
                Assert.NotNull(s.Zone.GetCell(5, 4).Objects.FirstOrDefault(e => e.BlueprintName == "HushStripLaid"), "the strip lies on the cell the menu was opened for");
                Assert.Greater(s.Turns.TickCount, tick); Assert.AreEqual(energy - TurnManager.ActionThreshold + (s.Turns.TickCount - tick) * s.Player.GetStatValue("Speed", TurnManager.DefaultSpeed), s.Turns.GetEnergy(s.Player), "one action was paid");
            }
        }

        [Test] public void ARefusedToolRowPaysNothingAndKeepsTheItem_Counter()
        {
            using (var s = new Scene())
            {
                s.Open(); var row = s.Actions.Single(a => a.Name == "LayDevice"); var blocker = s.Content.Factory.CreateEntity("MarlbackScrabbler"); Assert.True(s.Zone.AddEntity(blocker, 5, 4));   // the cell fills after the menu was drawn
                int tick = s.Turns.TickCount, energy = s.Turns.GetEnergy(s.Player);
                s.Choose(row);
                Assert.IsNull(s.Zone.GetCell(5, 4).Objects.FirstOrDefault(e => e.BlueprintName == "HushStripLaid"), "refused: nothing was laid"); Assert.True(s.Player.GetPart<InventoryPart>().Objects.Contains(s.Strip), "the strip is kept");
                Assert.AreEqual(tick, s.Turns.TickCount); Assert.AreEqual(energy, s.Turns.GetEnergy(s.Player), "a refused action costs nothing");
            }
        }

        [Test] public void TheDetailsReaderRebuildsTheMenuWithTheToolRows_SoReadingNeverDropsThem()
        {
            using (var s = new Scene())
            {
                s.Open(); var chosen = s.Actions.Single(a => a.Name == "LayDevice"); s.Menu.RestoreHighlight(chosen); Assert.AreSame(s.Strip, s.Menu.HighlightedAction.Via, "fixture: the tool row is the highlighted one");
                // The very code that opens the details reader builds the context from the open menu's highlighted row.
                var context = typeof(InputHandler).GetMethod("BuildReaderContext", Private).Invoke(s.Ui.Input, new object[] { s.Menu, s.Menu.HighlightedAction }); var contextType = context.GetType();
                Assert.AreSame(s.Strip, ((InventoryAction)contextType.GetField("Identity").GetValue(context)).Via, "the remembered row identity includes the tool that runs it");
                var rows = (List<InventoryAction>)typeof(InputHandler).GetMethod("GatherActionReaderRows", Private).Invoke(s.Ui.Input, new object[] { context });
                Assert.AreEqual(1, rows.Count(a => a.Name == "LayDevice" && a.Command == chosen.Command && ReferenceEquals(a.Via, s.Strip)), "the reader's rows are the menu's rows: " + string.Join(" / ", rows.Select(a => a.Display)));
                typeof(InputHandler).GetMethod("RestoreWorldActionReader", Private).Invoke(s.Ui.Input, new object[] { context });
                Assert.True(s.Menu.IsOpen, "the menu is back"); Assert.True(s.Actions.Any(a => a.Name == "LayDevice"), "with its tool rows"); Assert.AreEqual(chosen.Command, s.Menu.HighlightedAction.Command, "and the row you were reading is still the highlighted one");
                Assert.AreSame(s.Strip, s.Menu.HighlightedAction.Via);
            }
        }

        [Test] public void TheHighlightReturnsToTheRowOfTheToolYouWereReadingAbout_WhenTwoToolsShareACommand()
        {
            using (var s = new Scene())
            {
                var first = s.Content.Factory.CreateEntity("LiquidFlask"); var second = s.Content.Factory.CreateEntity("LiquidFlask");
                var rows = new List<InventoryAction> { new InventoryAction("FillLiquid", "fill with water [liquid flask] (empty)", "FillLiquidVessel|x|water") { Via = first }, new InventoryAction("FillLiquid", "fill with water [liquid flask] (5/12 water)", "FillLiquidVessel|x|water") { Via = second } };
                s.Menu.Open(s.Player, s.Grass, s.Cell, rows, s.Zone);
                s.Menu.RestoreHighlight(new InventoryAction("FillLiquid", "x", "FillLiquidVessel|x|water") { Via = second }); Assert.AreSame(second, s.Menu.HighlightedAction.Via, "the second flask's row, not the first that shares its command");
                s.Menu.RestoreHighlight(new InventoryAction("FillLiquid", "x", "FillLiquidVessel|x|water") { Via = first }); Assert.AreSame(first, s.Menu.HighlightedAction.Via);
            }
        }

        [Test] public void TheReaderFindsTheRowOfTheToolYouWereReadingAbout_AndRefusesARowWhoseToolIsGone_Counter()
        {
            var first = new Entity { ID = "tool-a" }; var second = new Entity { ID = "tool-b" }; var stranger = new Entity { ID = "tool-c" };
            var rows = new List<InventoryAction> { new InventoryAction("FillLiquid", "a", "FillLiquidVessel|x|water") { Via = first }, new InventoryAction("FillLiquid", "b", "FillLiquidVessel|x|water") { Via = second } };
            var find = typeof(InputHandler).GetMethod("FindReaderRow", BindingFlags.Static | BindingFlags.NonPublic); Assert.NotNull(find);
            var args = new object[] { rows, new InventoryAction("FillLiquid", "x", "FillLiquidVessel|x|water") { Via = second }, 0 };
            Assert.AreSame(rows[1], find.Invoke(null, args), "the second flask's row, not the first that shares its command"); Assert.AreEqual(1, (int)args[2]);
            args = new object[] { rows, new InventoryAction("FillLiquid", "x", "FillLiquidVessel|x|water") { Via = stranger }, 0 }; Assert.IsNull(find.Invoke(null, args)); Assert.AreEqual(0, (int)args[2], "a tool that is no longer offering it matches nothing");
            args = new object[] { rows, new InventoryAction("FillLiquid", "x", "FillLiquidVessel|x|water"), 0 }; find.Invoke(null, args); Assert.AreEqual(0, (int)args[2], "a row with no tool is not the row of a tool");
        }

        [Test] public void TheMenuOpenedForAnotherCellOffersNoToolRowForThisOne_Counter()
        {
            using (var s = new Scene())
            {
                var far = s.Content.Factory.CreateEntity("Grass"); Assert.True(s.Zone.AddEntity(far, 9, 4)); var farCell = s.Zone.GetCell(9, 4); farCell.IsVisible = farCell.Explored = true;
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Private).Invoke(s.Ui.Input, new object[] { far, farCell, false });
                Assert.IsFalse(s.Actions.Any(a => a.Name == "LayDevice"), "five cells away: out of reach, so no row");
            }
        }
    }
}
