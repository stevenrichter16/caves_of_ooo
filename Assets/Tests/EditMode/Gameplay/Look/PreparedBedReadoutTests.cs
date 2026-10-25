using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Readout of actual cultivated ground below ordinary decoration.
    /// These are native query/transaction tests, not an ordinary-input replay.</summary>
    public sealed class PreparedBedReadoutTests
    {
        const string Cue = "prepared planting bed";

        [Test]
        public void ActualAuthorizedPitchpodHarvestAndPickupExposeTheExistingBedAcrossReaders()
        {
            using (var f = new Fixture())
            {
                f.BindClaim();
                Assert.True(f.Claim.TryAct(f.Player, f.Zone, "pay"));
                Assert.AreEqual(ReserveAccessState.Granted, f.Claim.GetState(f.Player));
                Assert.AreEqual(32, TradeSystem.GetDrams(f.Player));
                var crop = f.Crop(2);
                string soilId = f.Soil.ID, rowId = f.Row.ID, rowName = f.Row.GetDisplayName();
                string rowProse = f.Row.GetPart<ExaminablePart>().Text;
                Assert.AreSame(crop, WorldInteractionSystem.ResolveTarget(f.Cell));
                Assert.Null(f.Zone.TileState.Get(4, 5));

                Assert.True(InventorySystem.ExecuteCommand(
                    new PerformInventoryActionCommand(crop, "HarvestCultivatedCrop"), f.Player, f.Zone).Success);
                Assert.Null(f.Zone.GetEntityCell(crop));
                var outputs = f.Cell.Objects.Where(e => e.BlueprintName == "PitchpodResin" || e.BlueprintName == "PitchpodSeed").ToArray();
                Assert.AreEqual(2, outputs.Count(e => e.BlueprintName == "PitchpodResin"));
                Assert.AreEqual(1, outputs.Count(e => e.BlueprintName == "PitchpodSeed"));
                foreach (var output in outputs)
                    Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(output), f.Player, f.Zone).Success);
                Assert.AreEqual(2, f.Carried("PitchpodResin"));
                Assert.AreEqual(1, f.Carried("PitchpodSeed"));
                Assert.AreEqual(ReserveAccessState.Granted, f.Claim.GetState(f.Player));
                Assert.AreEqual(32, TradeSystem.GetDrams(f.Player));
                Assert.AreSame(f.Row, WorldInteractionSystem.ResolveTarget(f.Cell), "Keep the real decorative target; do not rename or replace it.");
                Assert.AreEqual(soilId, f.Soil.ID); Assert.AreEqual(rowId, f.Row.ID);
                Assert.AreEqual(rowName, f.Row.GetDisplayName());
                Assert.AreEqual(rowProse, f.Row.GetPart<ExaminablePart>().Text);
                CollectionAssert.AreEqual(new[] { f.Soil, f.Row }, f.Cell.Objects);
                Assert.Null(f.Zone.TileState.Get(4, 5), "Cultivation is an owner part, not a synthetic coating.");

                AssertCue(f);
                StringAssert.Contains(Cue, WorldInteractionSystem.DescribeCell(f.Cell, f.Zone));
                var snapshot = LookQueryService.BuildSnapshot(f.Player, f.Zone, 4, 5);
                Assert.AreSame(f.Row, snapshot.PrimaryEntity);
                Assert.That(snapshot.DetailLines, Does.Contain(CellStatusReadout.GroundLine(f.Zone, f.Cell)));
                var document = ExamineDocumentBuilder.BuildWorld(f.Row, f.Zone, f.Cell, f.Player);
                Assert.NotNull(document);
                Assert.AreEqual(rowName, document.Title);
                StringAssert.Contains(Cue, document.ToPlainText());
                Assert.True(WorldActionMenuUI.BuildStatusLinesFor(f.Row, f.Cell, f.Zone).Any(s => s.Contains(Cue)));
            }
        }

        [Test]
        public void OrdinaryCutRowWithoutCultivationKeepsItsNameAndHasNoPreparedCue()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Soil.RemovePart(f.Soil.GetPart<CultivatedSoilPart>()));
                Assert.AreSame(f.Row, WorldInteractionSystem.ResolveTarget(f.Cell));
                Assert.AreEqual("cut emberwheat row", f.Row.GetDisplayName());
                Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell));
                Assert.Null(CellStatusReadout.GroundSummary(f.Zone, f.Cell));
                StringAssert.DoesNotContain(Cue, ExamineDocumentBuilder.BuildWorld(f.Row, f.Zone, f.Cell, f.Player).ToPlainText());
            }
        }

        [TestCase("hidden-cell")]
        [TestCase("unexplored-cell")]
        [TestCase("replaced-cell")]
        [TestCase("foreign-cell")]
        [TestCase("replaced-zone")]
        [TestCase("inactive-zone")]
        [TestCase("removed-soil")]
        [TestCase("foreign-marker")]
        [TestCase("foreign-physics")]
        [TestCase("foreign-render")]
        [TestCase("hidden-render")]
        [TestCase("carried-soil")]
        public void APreviouslyReadableBedLosesTheCueWhenItsAuthorityIsLost(string fault)
        {
            using (var f = new Fixture())
            {
                AssertCue(f);
                Cell queried = f.Cell;
                switch (fault)
                {
                    case "hidden-cell": queried.IsVisible = false; break;
                    case "unexplored-cell": queried.Explored = false; break;
                    case "replaced-cell": f.Zone.Cells[4, 5] = new Cell(4, 5, f.Zone) { Explored = true, IsVisible = true }; break;
                    case "foreign-cell": queried = new Cell(4, 5, new Zone(f.Zone.ZoneID)) { Explored = true, IsVisible = true }; break;
                    case "replaced-zone": f.Manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID); break;
                    case "inactive-zone": f.Manager.SetActiveZone(new Zone("Overworld.12.11.0")); break;
                    case "removed-soil": Assert.True(f.Zone.RemoveEntity(f.Soil)); break;
                    case "foreign-marker": f.Soil.GetPart<CultivatedSoilPart>().ParentEntity = f.Row; break;
                    case "foreign-physics": f.Soil.GetPart<PhysicsPart>().ParentEntity = f.Row; break;
                    case "foreign-render": f.Soil.GetPart<RenderPart>().ParentEntity = f.Row; break;
                    case "hidden-render": f.Soil.GetPart<RenderPart>().Visible = false; break;
                    case "carried-soil": f.Soil.GetPart<PhysicsPart>().InInventory = f.Player; break;
                }
                Assert.Null(CellStatusReadout.GroundLine(f.Zone, queried));
                Assert.Null(CellStatusReadout.GroundSummary(f.Zone, queried));
            }
        }

        [Test]
        public void VisibilityAndCultivationMustBelongToTheSameLowerOwner()
        {
            using (var f = new Fixture())
            {
                AssertCue(f);
                f.Soil.GetPart<RenderPart>().Visible = false;
                var other = f.Factory.CreateEntity("Grass");
                other.AddPart(new CultivatedSoilPart());
                Assert.True(f.Zone.AddEntity(other, 4, 5));
                other.GetPart<CultivatedSoilPart>().ParentEntity = f.Row;
                Assert.True(CultivatedSoilPart.IsCultivated(f.Zone, f.Cell), "The invisible first owner still supplies gameplay cultivation.");
                Assert.True(other.GetPart<RenderPart>().Visible, "The second owner supplies visibility, but not an owned marker.");
                Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell), "Never combine authority from two different owners.");
                other.GetPart<CultivatedSoilPart>().ParentEntity = other;
                AssertCue(f);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void OnlyADisclosedCurrentCropSuppressesTheNeutralBedCue(bool hiddenCrop)
        {
            using (var f = new Fixture())
            {
                AssertCue(f);
                var crop = f.Crop(1); var part = crop.GetPart<CropPart>();
                part.MoistureTicks = 20;
                crop.GetPart<RenderPart>().Visible = !hiddenCrop;
                if (hiddenCrop) AssertCue(f);
                else
                {
                    Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell));
                    var document = ExamineDocumentBuilder.BuildWorld(crop, f.Zone, f.Cell, f.Player);
                    Assert.NotNull(document);
                    StringAssert.Contains("Growth: sprout", document.ToPlainText());
                    StringAssert.DoesNotContain(Cue, document.ToPlainText());
                }
                Assert.AreEqual(1, part.GrowthStage); Assert.AreEqual(20, part.MoistureTicks);
                Assert.True(f.Zone.RemoveEntity(crop));
                AssertCue(f);
            }
        }

        [TestCase("foreign-crop-part")]
        [TestCase("foreign-render")]
        [TestCase("stale-anchor")]
        public void ForeignOrMovedCropReferencesCannotSuppressTheCurrentBed(string fault)
        {
            using (var f = new Fixture())
            {
                AssertCue(f);
                var crop = f.Crop(1);
                Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell), "The same current visible crop really suppresses the extra cue.");
                if (fault == "foreign-crop-part") crop.GetPart<CropPart>().ParentEntity = f.Row;
                else if (fault == "foreign-render") crop.GetPart<RenderPart>().ParentEntity = f.Row;
                else
                {
                    Assert.True(f.Zone.MoveEntity(crop, 5, 4));
                    Assert.AreSame(f.Zone.GetCell(5, 4), f.Zone.GetEntityCell(crop));
                    f.Cell.Objects.Add(crop); // Deliberately stale old membership is not current ownership.
                }
                AssertCue(f);
            }
        }

        [TestCase("barren")] [TestCase("pool")]
        public void PhysicallyUnusablePreparedGroundDoesNotAdvertiseTheBed(string blocker)
        {
            using (var f = new Fixture())
            {
                AssertCue(f);
                if (blocker == "barren") f.Row.SetTag("Barren");
                else f.Row.AddPart(new LiquidPoolPart());
                Assert.False(CultivatedSoilPart.IsCultivated(f.Zone, f.Cell));
                Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell));
            }
        }

        [Test]
        public void PreparedCueAppendsWithoutReplacingOrReorderingExistingHazardText()
        {
            using (var f = new Fixture())
            {
                var marker = f.Soil.GetPart<CultivatedSoilPart>(); Assert.True(f.Soil.RemovePart(marker));
                f.Zone.TileState.WriteCoating(4, 5, "water", 5);
                f.Zone.TileState.WriteCoating(4, 5, "ice", 4);
                f.Zone.TileState.WriteResidue(4, 5, "embers", 3);
                f.Zone.TileState.AddCharge(4, 5, 2);
                f.Zone.TileState.WriteCloud(4, 5, "smoke", 2);
                string line = CellStatusReadout.GroundLine(f.Zone, f.Cell), summary = CellStatusReadout.GroundSummary(f.Zone, f.Cell);
                StringAssert.Contains("water", line); StringAssert.Contains("ice", line);
                StringAssert.Contains("embers", line); StringAssert.Contains("charged", line); StringAssert.Contains("smoke cloud", line);
                f.Soil.AddPart(marker);
                Assert.AreEqual(line + ", " + Cue, CellStatusReadout.GroundLine(f.Zone, f.Cell));
                Assert.AreEqual(summary + ", " + Cue, CellStatusReadout.GroundSummary(f.Zone, f.Cell));
                f.Soil.RemovePart(marker);
                Assert.AreEqual(line, CellStatusReadout.GroundLine(f.Zone, f.Cell));
                Assert.AreEqual(summary, CellStatusReadout.GroundSummary(f.Zone, f.Cell));
            }
        }

        [TestCase("inactive-zone")]
        [TestCase("replaced-zone")]
        [TestCase("replaced-cell")]
        public void ExistingHazardsRequireCurrentAuthorityAndReturnUnchangedWhenRestored(string fault)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Soil.RemovePart(f.Soil.GetPart<CultivatedSoilPart>()));
                f.Zone.TileState.WriteCoating(4, 5, "water", 5);
                f.Zone.TileState.AddCharge(4, 5, 2);
                var originalState = f.Zone.TileState.Get(4, 5);
                Assert.AreSame(f.Zone, f.Manager.ActiveZone);
                Assert.AreSame(f.Zone, f.Manager.CachedZones[f.Zone.ZoneID]);
                Assert.AreSame(f.Cell, f.Zone.GetCell(4, 5));
                const string line = "On the ground: water (5 turns), charged";
                const string summary = "water, charged";
                Assert.AreEqual(line, CellStatusReadout.GroundLine(f.Zone, f.Cell));
                Assert.AreEqual(summary, CellStatusReadout.GroundSummary(f.Zone, f.Cell));

                if (fault == "inactive-zone") f.Manager.SetActiveZone(new Zone("Overworld.12.11.0"));
                else if (fault == "replaced-zone") f.Manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID);
                else f.Zone.Cells[4, 5] = new Cell(4, 5, f.Zone) { Explored = true, IsVisible = true };
                Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell));
                Assert.Null(CellStatusReadout.GroundSummary(f.Zone, f.Cell));
                Assert.AreSame(originalState, f.Zone.TileState.Get(4, 5));

                if (fault == "replaced-cell") f.Zone.Cells[4, 5] = f.Cell;
                else f.Manager.SetActiveZone(f.Zone);
                Assert.AreSame(f.Zone, f.Manager.ActiveZone);
                Assert.AreSame(f.Zone, f.Manager.CachedZones[f.Zone.ZoneID]);
                Assert.AreSame(f.Cell, f.Zone.GetCell(4, 5));
                Assert.AreSame(originalState, f.Zone.TileState.Get(4, 5));
                Assert.AreEqual(line, CellStatusReadout.GroundLine(f.Zone, f.Cell));
                Assert.AreEqual(summary, CellStatusReadout.GroundSummary(f.Zone, f.Cell));
            }
        }

        [Test]
        public void RepeatedQueriesDoNotAdvanceGrowthGrantRightsRepairBindingsOrDirtyTheWorld()
        {
            using (var f = new Fixture())
            {
                f.BindClaim();
                var crop = f.Crop(1); var part = crop.GetPart<CropPart>();
                part.MoistureTicks = 20; part.TicksInStage = 3; part.LastGrowthWorldTick = 0;
                int tick = WorldClock.CurrentTick, drams = TradeSystem.GetDrams(f.Player), units = f.Player.GetPart<InventoryPart>().Objects.Count;
                int dirty = 0, tileWrites = 0;
                ZoneRenderHooks.CellDirtyCallback = (x, y, source) => dirty++;
                ZoneRenderHooks.FullDirtyCallback = source => dirty++;
                f.Zone.TileState.OnCellChanged = (x, y) => tileWrites++;
                for (int i = 0; i < 3; i++)
                {
                    Assert.Null(CellStatusReadout.GroundLine(f.Zone, f.Cell));
                    Assert.NotNull(ExamineDocumentBuilder.BuildWorld(crop, f.Zone, f.Cell, f.Player));
                }
                Assert.AreEqual(1, part.GrowthStage); Assert.AreEqual(20, part.MoistureTicks);
                Assert.AreEqual(3, part.TicksInStage); Assert.AreEqual(0, part.LastGrowthWorldTick);
                Assert.True(f.Zone.RemoveEntity(crop));
                f.Claim.FirstSoilID = "stale saved binding";
                dirty = tileWrites = 0;
                for (int i = 0; i < 3; i++)
                {
                    AssertCue(f);
                    string text = ExamineDocumentBuilder.BuildWorld(f.Row, f.Zone, f.Cell, f.Player).ToPlainText();
                    StringAssert.Contains(Cue, text);
                    StringAssert.DoesNotContain("Nella's tied reserve", text);
                }
                Assert.AreEqual("stale saved binding", f.Claim.FirstSoilID);
                Assert.AreEqual(0, f.Claim.Permissions.Count);
                Assert.AreEqual(ReserveAccessState.Unknown, f.Claim.GetState(f.Player));
                Assert.AreEqual(tick, WorldClock.CurrentTick); Assert.AreEqual(drams, TradeSystem.GetDrams(f.Player));
                Assert.AreEqual(units, f.Player.GetPart<InventoryPart>().Objects.Count);
                Assert.AreEqual(0, dirty); Assert.AreEqual(0, tileWrites);
                Assert.AreEqual(0, f.Zone.TileState.WrittenCount);
            }
        }

        [Test]
        public void NullQueryInputsDoNotInventPreparedGround()
        {
            using (var f = new Fixture())
            {
                Assert.Null(CellStatusReadout.GroundLine(null, f.Cell));
                Assert.Null(CellStatusReadout.GroundSummary(f.Zone, null));
                Assert.Null(CellStatusReadout.GroundLine(null, null));
            }
        }

        static void AssertCue(Fixture f)
        {
            Assert.AreEqual(Cue, CellStatusReadout.GroundSummary(f.Zone, f.Cell));
            Assert.AreEqual("On the ground: " + Cue, CellStatusReadout.GroundLine(f.Zone, f.Cell));
        }

        sealed class Fixture : IDisposable
        {
            readonly DensityLootTestScope scope = new DensityLootTestScope();
            internal EntityFactory Factory => scope.Factory;
            internal readonly OverworldZoneManager Manager;
            internal readonly Zone Zone;
            internal readonly Cell Cell;
            internal readonly Entity Player, Soil, Row;
            internal LocalGatheringClaimPart Claim;

            internal Fixture()
            {
                try
                {
                    ZoneRenderHooks.Reset();
                    CropSystem.Factory = SeedPart.Factory = Factory;
                    Zone = new Zone("Overworld.11.8.0");
                    Manager = OverworldZoneManager.CreateDetached(Factory, 1729, true);
                    Manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
                    Player = Factory.CreateEntity("Player");
                    Player.GetPart<InventoryPart>().MaxWeight = 500;
                    Assert.True(Zone.AddEntity(Player, 4, 6));
                    Soil = Factory.CreateEntity("Grass"); Soil.AddPart(new CultivatedSoilPart());
                    Row = Factory.CreateEntity("CropRow");
                    Assert.True(Zone.AddEntity(Soil, 4, 5)); Assert.True(Zone.AddEntity(Row, 4, 5));
                    Cell = Zone.GetCell(4, 5); Cell.Explored = Cell.IsVisible = true;
                    Assert.True(CultivatedSoilPart.IsCultivated(Zone, Cell));
                    Assert.False(Row.HasPart<CropPart>());
                }
                catch { scope.Dispose(); throw; }
            }

            internal Entity Crop(int stage)
            {
                var entity = Factory.CreateEntity("PitchpodCrop"); var crop = entity.GetPart<CropPart>();
                crop.GrowthStage = stage; crop.GrowthTimingVersion = CropTime.CurrentVersion;
                crop.LastGrowthWorldTick = WorldClock.CurrentTick;
                Assert.True(Zone.AddEntity(entity, 4, 5));
                return entity;
            }

            internal void BindClaim()
            {
                var keeper = Factory.CreateEntity("SpreadSeedKeeper");
                var second = Factory.CreateEntity("Grass"); second.AddPart(new CultivatedSoilPart());
                var tray = Factory.CreateEntity("ConnectedReserveTray");
                Assert.True(Zone.AddEntity(keeper, 5, 5)); Assert.True(Zone.AddEntity(second, 4, 4)); Assert.True(Zone.AddEntity(tray, 6, 5));
                Claim = new LocalGatheringClaimPart(); keeper.AddPart(Claim);
                Assert.True(Claim.Configure(Zone, Soil, second, tray, 1729));
                PlayerReputation.Set(FactionManager.GetFaction(keeper), 0);
                TradeSystem.SetDrams(Player, 40); TradeSystem.SetDrams(keeper, 10);
            }

            internal int Carried(string blueprint) => Player.GetPart<InventoryPart>().Objects
                .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

            public void Dispose() => scope.Dispose();
        }
    }
}
