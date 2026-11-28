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
    /// how much fuel it has and how long its current flame will last; and a lit fire must look and light lit.
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

        Entity Hearth(float fuel = 20f, float temperature = 25f) { var h = Place("CampHearth", 11, 10); h.GetPart<PhysicsPart>().Takeable = false; h.AddPart(new InstallablePart { Mode = InstallablePart.Portable, State = InstallablePart.Installed }); h.GetPart<FuelPart>().FuelMass = fuel; h.GetPart<ThermalPart>().Temperature = temperature; return h; }
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

        [Test] public void AnUnlitFireShowsFractionalFuelAndNoRemainingFlame()
        {
            Assert.AreEqual("Fuel: 1.75 / 30\nFlame remaining: 0 turns", CellStatusReadout.FuelLine(Hearth(1.75f)));
        }

        [TestCase(30f, "60 turns")]
        [TestCase(1.75f, "7 turns")]
        [TestCase(0.1f, "1 turn")]
        public void ABurningHearthEstimatesTheShorterOfItsHeatAndFuel(float fuel, string duration)
        {
            var hearth = Hearth(fuel, 450f);
            int tick = Clock.TickCount;
            StringAssert.EndsWith("\nFlame remaining: about " + duration, CellStatusReadout.FuelLine(hearth));
            Assert.AreEqual(fuel, hearth.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(450f, hearth.GetPart<ThermalPart>().Temperature);
            Assert.AreEqual(tick, Clock.TickCount);
        }

        [Test] public void AFlameAtItsCoolingThresholdWillNotSurviveTheNextTurn()
        {
            var hearth = Hearth(0.1f, CookingService.MinimumFiniteCookingTemperature);
            StringAssert.EndsWith("\nFlame remaining: 0 turns", CellStatusReadout.FuelLine(hearth));
        }

        [Test] public void AnEmptyHearthHasNoFlameEvenWhenItRetainsHeat_AndGroundHasNoFuel()
        {
            Assert.AreEqual("Fuel: 0 / 30\nFlame remaining: 0 turns", CellStatusReadout.FuelLine(Hearth(0f, 450f)));
            Assert.IsNull(CellStatusReadout.FuelLine(Place("Grass", 5, 5)));
        }

        [TestCase("SpreadCookingCoals", "25", 66)]
        [TestCase("Campfire", "200", 27)]
        public void CookingCoalsAndLegacyCampfiresUseTheirOwnThermalHorizon(string blueprint, string capacity, int turns)
        {
            var fire = Place(blueprint);
            fire.GetPart<FuelPart>().FuelMass = 1f;
            Assert.AreEqual("Fuel: 1 / " + capacity + "\nFlame remaining: about " + turns + " turns", CellStatusReadout.FuelLine(fire));
        }

        [Test] public void InvalidFireMetadataIsUnavailable_StableLegacyHeatIsIndefinite()
        {
            var camp = Place("Campfire", 5, 5);
            var heat = camp.GetPart<ThermalPart>();
            heat.AmbientDecayRate = 0f;
            Assert.AreEqual("Fuel: 200 / 200\nFlame remaining: indefinite", CellStatusReadout.FuelLine(camp));
            heat.AmbientDecayRate = float.NaN;
            Assert.AreEqual("Fuel: 200 / 200\nFlame remaining: unavailable", CellStatusReadout.FuelLine(camp));

            var hearth = Hearth(1.75f, 450f);
            var fuel = hearth.GetPart<FuelPart>();
            fuel.BurnRate = float.NaN;
            Assert.AreEqual("Fuel: 1.75 / 30\nFlame remaining: unavailable", CellStatusReadout.FuelLine(hearth));
            fuel.MaxFuel = 1f;
            Assert.AreEqual("Fuel: unavailable\nFlame remaining: unavailable", CellStatusReadout.FuelLine(hearth));
        }

        [Test] public void TheSeparateFireLinesReachWorldAndInventoryExamineLookAndTheCMenu()
        {
            var hearth = Hearth(1.75f, 450f);
            var cell = Zone.GetEntityCell(hearth);
            cell.Explored = cell.IsVisible = true;
            var expected = new[] { "Fuel: 1.75 / 30", "Flame remaining: about 7 turns" };

            var status = WorldActionMenuUI.BuildStatusLinesFor(hearth, cell, Zone, Actor);
            CollectionAssert.AreEqual(expected, status);
            var look = LookQueryService.BuildSnapshot(Actor, Zone, cell.X, cell.Y);
            CollectionAssert.AreEqual(expected, look.DetailLines.Where(IsFireLine));
            var quick = hearth.GetPart<ExaminablePart>().BuildWorldExamineLine(Zone, cell, Actor);
            CollectionAssert.AreEqual(expected, quick.Split('\n').Where(IsFireLine));
            StringAssert.DoesNotContain("Cooking:", quick);
            StringAssert.DoesNotContain("Press C", quick);

            var world = ExamineDocumentBuilder.BuildWorld(hearth, Zone, cell, Actor);
            Assert.NotNull(world);
            CollectionAssert.AreEqual(expected, world.Sections.SelectMany(section => section.Text.Split('\n')).Where(line => line.Length > 0));

            var carried = Carry("CampHearth");
            carried.GetPart<FuelPart>().FuelMass = 1.75f;
            carried.GetPart<ThermalPart>().Temperature = 450f;
            var inventory = ExamineDocumentBuilder.BuildInventory(carried, Actor);
            Assert.NotNull(inventory);
            CollectionAssert.AreEqual(new[] { expected[0], "Flame remaining: 0 turns" },
                inventory.Sections.SelectMany(section => section.Text.Split('\n')).Where(line => line.Length > 0));
        }

        [TestCase("hidden-cell")]
        [TestCase("unexplored-cell")]
        [TestCase("hidden-render")]
        [TestCase("carried-alias")]
        [TestCase("equipped-alias")]
        [TestCase("replaced-cell")]
        public void WorldFireReadoutsRequireTheCurrentDisclosedPhysicalOwner(string fault)
        {
            var hearth = Hearth(1.75f, 450f);
            var original = Zone.GetEntityCell(hearth);
            original.Explored = original.IsVisible = true;
            Assert.AreEqual(2, WorldActionMenuUI.BuildStatusLinesFor(hearth, original, Zone, Actor).Count(IsFireLine));
            Assert.AreEqual(2, LookQueryService.BuildSnapshot(Actor, Zone, original.X, original.Y).DetailLines.Count(IsFireLine));

            var queried = original;
            switch (fault)
            {
                case "hidden-cell": original.IsVisible = false; break;
                case "unexplored-cell": original.Explored = false; break;
                case "hidden-render": hearth.GetPart<RenderPart>().Visible = false; break;
                case "carried-alias": hearth.GetPart<PhysicsPart>().InInventory = Actor; break;
                case "equipped-alias": hearth.GetPart<PhysicsPart>().Equipped = Actor; break;
                case "replaced-cell":
                    queried = new Cell(original.X, original.Y, Zone) { Explored = true, IsVisible = true };
                    queried.AddObject(hearth);
                    Zone.Cells[original.X, original.Y] = queried;
                    Assert.AreSame(original, Zone.GetEntityCell(hearth), "the zone still indexes the former physical anchor");
                    Assert.False(WorldActionMenuUI.BuildStatusLinesFor(hearth, original, Zone, Actor).Any(IsFireLine));
                    break;
            }

            Assert.False(WorldActionMenuUI.BuildStatusLinesFor(hearth, queried, Zone, Actor).Any(IsFireLine), fault);
            Assert.False(LookQueryService.BuildSnapshot(Actor, Zone, queried.X, queried.Y).DetailLines.Any(IsFireLine), fault);
        }

        [Test] public void CampfireOnlyOvenAndFueledNonfireKeepTheirOrdinaryReaders()
        {
            var oven = Place("Oven");
            oven.AddPart(new CampfirePart());
            Assert.IsNull(CellStatusReadout.FuelLine(oven));
            StringAssert.Contains("deep baking chamber", oven.GetPart<ExaminablePart>().BuildExamineLine());
            var torch = Factory.CreateEntity("Torch");
            var torchReadout = CellStatusReadout.FuelLine(torch);
            StringAssert.StartsWith("Fuel:", torchReadout);
            StringAssert.DoesNotContain("Flame remaining:", torchReadout);
        }

        [Test] public void AnotherOwnersThermalPartCannotAuthorizeACompactFireReadout()
        {
            var hearth = Hearth(1.75f, 450f);
            var other = Factory.CreateEntity("Oven");
            other.AddPart(hearth.GetPart<ThermalPart>());
            StringAssert.DoesNotContain("Flame remaining:", CellStatusReadout.FuelLine(hearth));
        }

        static bool IsFireLine(string line) => line.StartsWith("Fuel:") || line.StartsWith("Flame remaining:");

        [Test] public void DestructiveCombustionDoesNotReceiveAPassiveFlameEstimate()
        {
            var hearth = Hearth(1.75f, 450f);
            var effects = new StatusEffectsPart();
            hearth.AddPart(effects);
            Assert.True(effects.ForceApplyEffect(new BurningEffect(2f)));
            Assert.AreEqual("Fuel: 1.75 / 30\nFlame remaining: unavailable", CellStatusReadout.FuelLine(hearth));
            Assert.AreEqual(1.75f, hearth.GetPart<FuelPart>().FuelMass);
        }

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

        [Test] public void AnOutFireWithNoFuelExplainsWhatCanFeedItBeforeRelighting()
        {
            var hearth = Hearth(0, 25);
            Striker();
            var row = Actions(hearth).Single(action => action.Name == "RelightCookingFire");
            Assert.True(row.IsDisabled);
            Assert.AreEqual("no_fuel", row.DisabledCode);
            Assert.That(row.DisabledReason.ToLowerInvariant(),
                Does.Contain("fuel").And.Contain("salvaged timber").And.Contain("dry knotflax chaff"));
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
