using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PoisonGasGroundReadoutTests : FiftyWorldFixture
    {
        const int X = 11;
        const int Y = 10;
        const string Poison = "poison gas";

        [Test]
        public void VisiblePoisonPoolReachesLookMenuAndExamineWithoutAStoredTileLayer()
        {
            var grass = Place("Grass", X, Y);
            var gas = PlaceGas("poison", 6, new GasPoisonPart());
            var cell = Reveal();
            int beforeTick = Clock.TickCount;
            Assert.IsNull(Zone.TileState.Get(X, Y));

            string ground = CellStatusReadout.GroundLine(Zone, cell);
            StringAssert.Contains(Poison, ground ?? "");
            StringAssert.Contains("wear a filter", ground ?? "");
            StringAssert.Contains(Poison, WorldInteractionSystem.DescribeCell(cell, Zone));
            Assert.True(CavesOfOoo.Rendering.WorldActionMenuUI.BuildStatusLinesFor(gas, cell, Zone, Actor)
                .Any(line => line.Contains(Poison)), "The directional action menu should show the actual risk");
            var look = LookQueryService.BuildSnapshot(Actor, Zone, X, Y);
            Assert.True(look.DetailLines.Any(line => line.Contains(Poison)), "Look should share the ground warning");
            var document = ExamineDocumentBuilder.BuildWorld(grass, Zone, cell, Actor);
            Assert.NotNull(document);
            Assert.True(document.Sections.Any(section => section.Text.Contains(Poison)), "Examine should share the ground warning");

            Assert.IsNull(Zone.TileState.Get(X, Y));
            Assert.AreEqual(beforeTick, Clock.TickCount);
            Assert.AreEqual(6, gas.GetPart<GasPoolPart>().Density);
            Assert.AreSame(cell, Zone.GetEntityCell(gas));
            Assert.False(Actor.HasEffect<PoisonedByGasEffect>());

            Zone.TileState.WriteCloud(X, Y, "veil-mist", 3);
            string mixed = CellStatusReadout.GroundLine(Zone, cell);
            StringAssert.Contains(Poison, mixed);
            StringAssert.Contains("veil mist cloud", mixed);
            StringAssert.Contains("blocks sight", mixed);
        }

        [Test]
        public void InteractKeepsAirAndGroundOnSeparateReadableLines()
        {
            var gas = PlaceGas("poison", 6, new GasPoisonPart());
            var cell = Reveal();
            Zone.TileState.WriteCoating(X, Y, "bog-mire", ZoneTileState.Permanent);

            var lines = CavesOfOoo.Rendering.WorldActionMenuUI.BuildStatusLinesFor(gas, cell, Zone, Actor);

            CollectionAssert.Contains(lines, "Air: poison gas (wear a filter)");
            CollectionAssert.Contains(lines, "On the ground: bog mire");
            Assert.AreEqual(1, lines.Count(line => line.Contains(Poison)));
            Assert.True(lines.All(line => line.Length <= 44), "Both conditions fit the minimum menu width.");
        }

        [Test]
        public void GroundReadoutRequiresBothFogFlagsAndVisibleGasRender()
        {
            var gas = PlaceGas("poison", 6, new GasPoisonPart());
            var cell = Reveal();
            cell.IsVisible = false;
            Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));

            cell.IsVisible = true;
            cell.Explored = false;
            Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));

            cell.Explored = true;
            gas.GetPart<RenderPart>().Visible = false;
            Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));
        }

        [Test]
        public void DepletedPoolAndPartsOnDifferentOwnersCannotDisclosePoison()
        {
            PlaceGas("empty", 0, new GasPoisonPart());
            var cell = Reveal();
            Assert.IsNull(CellStatusReadout.GroundSummary(Zone, cell));

            PlaceGas("pool-only", 6, null);
            PlaceGas("poison-only", null, new GasPoisonPart());
            Assert.IsNull(CellStatusReadout.GroundSummary(Zone, cell));
        }

        [Test]
        public void VeilMistAndARealNonPoisonGasKeepTheirExistingSightReadout()
        {
            PlaceGas("sleep", 6, new GasSleepPart());
            var cell = Reveal();
            Zone.TileState.WriteCloud(X, Y, "veil-mist", 3);

            string ground = CellStatusReadout.GroundLine(Zone, cell);

            StringAssert.Contains("veil mist cloud", ground);
            StringAssert.Contains("blocks sight", ground);
            StringAssert.DoesNotContain(Poison, ground);
        }

        [Test]
        public void VisiblePoisonInAnInactiveOrReplacedAttachedZoneIsNotDisclosed()
        {
            PlaceGas("poison", 6, new GasPoisonPart());
            var cell = Reveal();
            var manager = OverworldZoneManager.CreateDetached(Factory, 73);
            manager.SetActiveZone(Zone);
            Assert.AreSame(manager, WorldLocationContext.For(Zone));

            manager.CachedZones[Zone.ZoneID] = new Zone(Zone.ZoneID);
            Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));

            manager.SetActiveZone(Zone);
            manager.SetActiveZone(new Zone("another-current-zone"));
            Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));
        }

        Cell Reveal()
        {
            var cell = Zone.GetCell(X, Y);
            cell.IsVisible = cell.Explored = true;
            return cell;
        }

        Entity PlaceGas(string id, int? density, IObjectGasBehaviorPart behavior)
        {
            var gas = new Entity { ID = id, BlueprintName = "readout-test-cloud" };
            gas.SetTag("Gas");
            gas.AddPart(new RenderPart { DisplayName = "cloud", RenderString = "°", ColorString = "&g" });
            gas.AddPart(new PhysicsPart { Solid = false, Takeable = false });
            if (density.HasValue) gas.AddPart(new GasPoolPart { GasId = id, Density = density.Value });
            if (behavior != null) gas.AddPart(behavior);
            Assert.True(Zone.AddEntity(gas, X, Y));
            return gas;
        }
    }
}
