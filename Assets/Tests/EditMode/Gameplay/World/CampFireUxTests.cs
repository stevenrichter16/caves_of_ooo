using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Three things asked for after playing the camp kits: lighting must be doable from the interact (C) menu of the thing being lit, with no trip through the pack; a fire must always say
    /// how much fuel it has and how many turns that is; and a lit fire must look and light lit.
    /// </summary>
    public sealed class CampFireUxTests : FiftyWorldFixture
    {
        const string Oil = "oil";

        [SetUp] public void SetupUx()
        {
            Diag.ResetAll(); MessageLog.Clear(); TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void TearDownUx() { TileReactionSystem.ResetForTests(); }

        Entity Hearth(float fuel = 20f, float temperature = 25f) { var h = Place("CampHearth", 11, 10); h.GetPart<PhysicsPart>().Takeable = false; h.GetPart<FuelPart>().FuelMass = fuel; h.GetPart<ThermalPart>().Temperature = temperature; return h; }
        Entity Striker(int strikes = 20) { var s = Carry("FlintStriker"); s.GetPart<FlintStrikerPart>().Strikes = strikes; return s; }
        Cell OilCell(int x = 11, int y = 10) { Place("Grass", x, y); Zone.TileState.WriteCoating(x, y, Oil, 5); return Zone.GetCell(x, y); }
        InventoryAction[] CellRows(Cell cell) { var rows = new System.Collections.Generic.List<InventoryAction>(); WorldInteractionSystem.AppendIgnitionActions(rows, cell, Actor); return rows.ToArray(); }

        // ── glow: a lit fire looks and lights lit ──

        [Test] public void RelightingLightsTheHearthAtOnceWithoutWaitingForATurn()
        {
            var h = Hearth(20, 25); Striker(); Assert.False(h.GetPart<LightSourcePart>().Enabled); Assert.True(Act(h, "RelightCookingFire"), string.Join("|", MessageLog.GetRecent(2)));
            Assert.True(h.GetPart<LightSourcePart>().Enabled, "the glow is on as the flame takes, not at the next turn's end");
        }

        [Test] public void FeedingAColdHearthLeavesItDark_Counter()
        {
            var h = Hearth(0, 25); Carry("SalvagedTimber"); Assert.True(Act(h, "FeedCookingFire")); Assert.False(h.GetPart<LightSourcePart>().Enabled, "fed but not lit");
        }

        [Test] public void ABurningHearthDrawsInFireColorsAndAnUnlitOneDoesNot()
        {
            string Color(Entity e) { var ev = GameEvent.New("Render"); ev.SetParameter("ColorString", "&K"); e.FireEvent(ev); return ev.GetStringParameter("ColorString"); }
            var out_ = Hearth(20, 25); Assert.AreEqual("&K", Color(out_)); var lit = Hearth(20, 450); lit.GetPart<PhysicsPart>().Takeable = false;
            Zone.RemoveEntity(out_); Assert.True(Zone.AddEntity(out_, 5, 5)); Assert.Contains(Color(lit), new[] { "&R", "&Y" }, "a burning hearth flickers red and yellow like a campfire");
        }

        [Test] public void TheHearthLightIsAsBigAsACampfiresAndFlickersLikeOne()
        {
            var hearth = Factory.CreateEntity("CampHearth").GetPart<LightSourcePart>(); var campfire = Factory.CreateEntity("Campfire").GetPart<LightSourcePart>();
            Assert.GreaterOrEqual(hearth.Radius, campfire.Radius); Assert.GreaterOrEqual(hearth.Intensity, campfire.Intensity); Assert.NotNull(Factory.CreateEntity("CampHearth").GetPart<LightSourceFlickerPart>());
        }

        // ── fuel and turns, always ──

        [Test] public void AFireSaysHowMuchFuelItHasAndHowManyTurnsThatIs()
        {
            var h = Hearth(20, 25); string line = CellStatusReadout.FuelLine(h); StringAssert.Contains("20", line); StringAssert.Contains("80 turns", line, "20 fuel at a quarter a turn"); StringAssert.Contains("light", line.ToLowerInvariant());
            var camp = Factory.CreateEntity("Campfire"); StringAssert.Contains("400 turns", CellStatusReadout.FuelLine(camp), "200 fuel at half a turn");
        }

        [Test] public void ABurningHearthAlsoSaysHowLongItsHeatLasts_ItNeedsAFlameAgainBeforeTheFuelIsGone()
        {
            var h = Hearth(30, 450); string line = CellStatusReadout.FuelLine(h); StringAssert.Contains("120 turns", line, "30 fuel is 120 turns of fuel"); StringAssert.Contains("60 more turns", line, "but a lighting cools below cooking heat in about 60");
        }

        [Test] public void AnEmptyFireSaysSo_AndAThingWithNoFuelSaysNothing()
        {
            Assert.AreEqual("Fuel: none left.", CellStatusReadout.FuelLine(Hearth(0, 25))); Assert.IsNull(CellStatusReadout.FuelLine(Place("Grass", 5, 5)));
        }

        [Test] public void TheFuelLineReachesTheExamineReaderTheLookReaderAndTheCMenu()
        {
            var h = Hearth(20, 25); var lines = new System.Collections.Generic.List<string>(); StateReader.Collect(h, false, Zone, Zone.GetEntityCell(h), Actor, lines);
            Assert.True(lines.Any(l => l.StartsWith("Fuel:")), "examine: " + string.Join(" / ", lines));
            var status = WorldActionMenuUI.BuildStatusLinesFor(h, Zone.GetEntityCell(h), Zone); Assert.True(status.Any(l => l.StartsWith("Fuel:")), "the C menu's status block: " + string.Join(" / ", status));
            var torch = Factory.CreateEntity("Torch"); Assert.True(StateReaderLines(torch).Any(l => l.StartsWith("Fuel:")), "a torch says it too");
        }

        System.Collections.Generic.List<string> StateReaderLines(Entity e) { var l = new System.Collections.Generic.List<string>(); StateReader.Collect(e, false, Zone, null, Actor, l); return l; }

        // ── light things from their own C menu, not the pack ──

        [Test] public void AnAdjacentOilSlickOffersALightRowThatNamesTheStrikeAndRunsViaTheStriker()
        {
            var s = Striker(); var rows = CellRows(OilCell()); var row = rows.Single(); StringAssert.Contains("flint", row.Display); StringAssert.Contains("1 strike", row.Display);
            Assert.AreSame(s, row.Via, "the carried striker performs it; the player does not open the pack"); Assert.AreEqual(ActionTime.OneAction, row.Time);
        }

        [Test] public void RunningThatRowLightsTheSlickAndSpendsOneStrike()
        {
            var s = Striker(); var cell = OilCell(); var row = CellRows(cell).Single(); Assert.True(InventorySystem.PerformAction(Actor, row.Via, row.Command, Zone), string.Join("|", MessageLog.GetRecent(2)));
            Assert.False(Zone.TileState.HasCoating(11, 10, Oil)); Assert.True(Zone.TileState.HasResidue(11, 10, "embers")); Assert.AreEqual(19, s.GetPart<FlintStrikerPart>().Strikes);
        }

        [Test] public void NoRowWithoutAMeansOfLightingOrWithoutAnythingToCatchOrOutOfReachOrUnderfoot_Counters()
        {
            var cell = OilCell(); Assert.IsEmpty(CellRows(cell), "no striker, no torch");
            Striker(); Assert.IsEmpty(CellRows(Zone.GetCell(12, 10)), "nothing there would catch"); Assert.IsEmpty(CellRows(OilCell(14, 10)), "three cells away");
            Place("Grass", 10, 10); Zone.TileState.WriteCoating(10, 10, Oil, 5); Assert.IsEmpty(CellRows(Zone.GetCell(10, 10)), "the cell you stand in is not lit from a menu");
        }

        [Test] public void AWetOrWornStrikerShowsADisabledRowThatSaysWhy()
        {
            var s = Striker(); Assert.True(s.ApplyEffect(new WetEffect(1f))); var row = CellRows(OilCell()).Single(); Assert.True(row.IsDisabled); StringAssert.Contains("wet", row.DisabledReason);
            s.RemoveEffect<WetEffect>(); s.GetPart<FlintStrikerPart>().Strikes = 0; StringAssert.Contains("worn", CellRows(Zone.GetCell(11, 10)).Single().DisabledReason);
        }

        [Test] public void ALitTorchInHandLightsTheSlickForNothing_AndIsPreferredToTheStriker()
        {
            var s = Striker(); var torch = Carry("Torch"); Assert.True(InventorySystem.Equip(Actor, torch)); Assert.True(torch.GetPart<LightSourcePart>().Enabled);
            var cell = OilCell(); var rows = CellRows(cell); Assert.AreEqual(1, rows.Length, "one way is enough; the free one"); var row = rows[0]; StringAssert.Contains("torch", row.Display); Assert.AreSame(torch, row.Via);
            Assert.True(InventorySystem.PerformAction(Actor, row.Via, row.Command, Zone), string.Join("|", MessageLog.GetRecent(2)));
            Assert.False(Zone.TileState.HasCoating(11, 10, Oil)); Assert.AreEqual(20, s.GetPart<FlintStrikerPart>().Strikes, "no strike spent"); Assert.True(torch.GetPart<LightSourcePart>().Enabled, "the torch is still lit");
        }

        [Test] public void AStaleOrForgedCellLightCommandIsRefusedAndCostsNothing()
        {
            var s = Striker(); var cell = OilCell(); var row = CellRows(cell).Single(); Zone.TileState.RemoveCoating(11, 10, Oil);
            Assert.False(InventorySystem.PerformAction(Actor, row.Via, row.Command, Zone), "the slick is gone"); Assert.AreEqual(20, s.GetPart<FlintStrikerPart>().Strikes);
        }

        // ── lighting a fire says what stops it ──

        [Test] public void AnOutFireWithNoFuelShowsADisabledRelightRowThatSaysFeedIt()
        {
            var h = Hearth(0, 25); Striker(); var row = Actions(h).Single(a => a.Name == "RelightCookingFire"); Assert.True(row.IsDisabled); StringAssert.Contains("fuel", row.DisabledReason.ToLowerInvariant());
        }

        [Test] public void AnOutFireWithFuelButNoFlameShowsADisabledRelightRowThatSaysWhatToBring()
        {
            var h = Hearth(20, 25); var row = Actions(h).Single(a => a.Name == "RelightCookingFire"); Assert.True(row.IsDisabled); StringAssert.Contains("flame", row.DisabledReason.ToLowerInvariant());
        }

        [Test] public void AFireThatIsAlreadyBurningWellOffersNoRelightRowAtAll_Counter()
        {
            var h = Hearth(20, 450); Assert.False(Actions(h).Any(a => a.Name == "RelightCookingFire"));
        }
    }
}
