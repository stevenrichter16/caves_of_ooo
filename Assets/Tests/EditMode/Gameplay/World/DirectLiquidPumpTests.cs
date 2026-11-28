using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DirectLiquidPumpTests : FiftyWorldFixture
    {
        CavesOfOoo.Data.EntityFactory oldForge;
        [SetUp] public void ReadyPump()
        {
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            Bed(10,10,false);Bed(11,10,false);Bed(9,10,false);
        }
        [TearDown] public void RestorePump(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();RouteWorkCreationProbe.Run=null;}
        Entity Tool()=>Carry("HandPump");
        Entity Stock(int amount=60,string liquid="acid",int x=11,int y=10)
        {
            var e=Factory.CreateEntity(liquid=="acid"?"AcidPool":"PouredLiquidPool");Assert.NotNull(e);
            var p=e.GetPart<LiquidPoolPart>();p.LiquidId=liquid;p.Volume=amount;p.Initialize();Assert.True(Zone.AddEntity(e,x,y));return e;
        }
        string Stroke(Entity tool)=>Choice(tool,"PumpLiquid|",a=>a.Display.Contains("to 9,10"));
        Entity Outlet()=>Zone.GetCell(9,10).Objects.Single(e=>e.HasPart<LiquidPoolPart>());

        Entity ReleasedTray()
        {
            var tray = Place("CinderholdOilRecoveryTray");
            Assert.True(tray.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("CinderhookAxeHeadComponent")));
            return tray;
        }

        [Test]
        public void ReleasedTrayPumpsFromTheWorldMenuAndLetsOrdinaryTakeRecoverTheExactHead()
        {
            var tool = Tool();
            var tray = ReleasedTray();
            var stock = tray.GetPart<ContainedLiquidSourcePart>();
            var container = tray.GetPart<ContainerPart>();
            var head = container.Contents.Single();
            Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(tray, head), Actor, Zone).Success);
            var row = CellMenu(11, 10).Single(a => a.Via == tool && a.Command.StartsWith("PumpLiquid|")
                && a.Display.Contains("to 9,10"));
            Assert.That(row.Display, Does.Contain("12 oil"));
            Assert.AreEqual(row.Command, Stroke(tool));
            Assert.True(ChooseRow(row, 11, 10));

            Assert.Zero(stock.Volume);
            Assert.True(stock.AllowsCleanAccess);
            Assert.AreSame(tray, Zone.GetCell(11, 10).Objects.Single(e => e == tray));
            Assert.AreSame(head, container.Contents.Single());
            Assert.False(Zone.TileState.HasCoating(11, 10, "oil"));
            Assert.AreEqual(12, PoolUnits(9, 10));
            Assert.True(Zone.TileState.HasCoating(9, 10, "oil"));
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(tray, head), Actor, Zone).Success);
            Assert.That(Pack.Objects, Does.Contain(head));
            Assert.That(Pack.Objects, Does.Contain(tool));
            Assert.False(Actor.HasEffect<LiquidCoveredEffect>());
        }

        [Test]
        public void FlaskWithdrawalInvalidatesTheTrayStrokeWithoutSpendingEitherStock()
        {
            var tool = Tool();
            var tray = ReleasedTray();
            string stroke = Stroke(tool);
            var flask = Flask(0, capacity: 3);
            Assert.True(Act(flask, Choice(flask, "FillLiquidVessel|")));
            Assert.False(Act(tool, stroke));
            Assert.AreEqual(9, tray.GetPart<ContainedLiquidSourcePart>().Volume);
            Assert.AreEqual(3, Units(flask));
            Assert.Zero(PoolUnits(9, 10));
        }

        [Test]
        public void InterruptedTrayStrokeReturnsOnlyCreditThatCanFit()
        {
            var tool = Tool();
            var tray = ReleasedTray();
            var stock = tray.GetPart<ContainedLiquidSourcePart>();
            string stroke = Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe { Run = () => stock.Volume += 5 });
            FailAfter();

            Assert.False(Act(tool, stroke));
            Assert.AreEqual(12, stock.Volume);
            Assert.AreEqual(5, PoolUnits(9, 10));
            Assert.That(tray.GetPart<ContainerPart>().Contents, Has.Count.EqualTo(1));
        }

        [Test]
        public void InterruptedTrayStrokeCannotRefundFromStockAlreadyAtTheOutlet()
        {
            var tool = Tool();
            var tray = ReleasedTray();
            var outlet = Stock(5, "oil", 9, 10);
            string stroke = Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe { Run = () => outlet.GetPart<LiquidPoolPart>().Volume -= 11 });
            FailAfter();

            Assert.False(Act(tool, stroke));
            Assert.AreSame(outlet, Outlet());
            Assert.AreEqual(5, PoolUnits(9, 10));
            Assert.AreEqual(1, tray.GetPart<ContainedLiquidSourcePart>().Volume);
        }

        [TestCase("locked")]
        [TestCase("owned")]
        [TestCase("mixed outlet")]
        [TestCase("existing oil coating")]
        public void UnreleasedTrayOrUnsafeOutletHasNoStroke(string condition)
        {
            var tool = Tool();
            var tray = ReleasedTray();
            if (condition == "locked") tray.GetPart<ContainerPart>().Locked = true;
            if (condition == "owned") tray.Properties["OwnerID"] = "resident";
            if (condition == "mixed outlet") Stock(1, "water", 9, 10);
            if (condition == "existing oil coating") Zone.TileState.WriteCoating(9, 10, "oil", 5);
            Assert.False(Actions(tool).Any(a => a.Command.StartsWith("PumpLiquid|") && a.Display.Contains("to 9,10")));
            Assert.AreEqual(12, tray.GetPart<ContainedLiquidSourcePart>().Volume);
        }
        [Test] public void OrdinaryFlaskRemainsAnAlternativeForTheSameFiniteStock()
        {var source=Stock();var flask=Flask(0);Assert.True(Act(flask,Choice(flask,"FillLiquidVessel|")));Assert.AreEqual(48,source.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(12,Units(flask));}
        [Test] public void OneStrokeMovesTwentyRealUnitsAndKeepsTheSamePump()
        {
            var tool=Tool();var source=Stock();Assert.True(Act(tool,Stroke(tool)));
            Assert.AreEqual(40,source.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(20,PoolUnits(9,10));Assert.AreEqual("acid",Outlet().GetPart<LiquidPoolPart>().LiquidId);
            Assert.True(Pack.Objects.Contains(tool));Assert.True(Zone.TileState.HasCoating(9,10,"acid"));
        }
        [Test] public void FinalStrokeRetiresOnlyTheDepletedSourceAndItsProjection()
        {
            var tool=Tool();var source=Stock();for(int i=0;i<3;i++)Assert.True(Act(tool,Stroke(tool)));
            Assert.False(Zone.GetReadOnlyEntities().Contains(source));Assert.False(Zone.TileState.HasCoating(11,10,"acid"));
            Assert.AreEqual(60,PoolUnits(9,10));Assert.True(Zone.TileState.HasCoating(9,10,"acid"));
        }
        [Test] public void ShortFinalStrokeMovesOnlyTheRemainingStock()
        {var tool=Tool();Stock(7);Assert.True(Act(tool,Stroke(tool)));Assert.AreEqual(7,PoolUnits(9,10));Assert.AreEqual(0,PoolUnits(11,10));}
        [Test] public void ExistingOutletKeepsItsIdentityAndOriginalStock()
        {var tool=Tool();Stock();var outlet=Stock(5,"acid",9,10);Assert.True(Act(tool,Stroke(tool)));Assert.AreSame(outlet,Outlet());Assert.AreEqual(25,PoolUnits(9,10));}
        [TestCase("hidden")][TestCase("occupied")][TestCase("different")][TestCase("blocked")]
        public void UnsafeOutletHasNoDisplayedWestStroke(string mode)
        {
            var tool=Tool();Stock();
            if(mode=="hidden")Zone.GetCell(9,10).IsVisible=false;
            if(mode=="occupied")Place("Villager",9,10);
            if(mode=="different")Stock(5,"water",9,10);
            if(mode=="blocked")Place("VillageDoor",9,10);
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")&&a.Display.Contains("to 9,10")));
        }
        [TestCase("source")][TestCase("outlet")]
        public void EvenAnEmptySecondPoolOwnerRefusesAmbiguousProjection(string endpoint)
        {var tool=Tool();Stock();if(endpoint=="outlet")Stock(5,"acid",9,10);Stock(0,"acid",endpoint=="source"?11:9,10);Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));}
        [TestCase("unknown")][TestCase("owned")][TestCase("film")][TestCase("renewing")]
        public void UnsupportedSourceDoesNotBecomeGenericFreeDrainage(string mode)
        {
            var tool=Tool();var source=Stock();
            if(mode=="unknown")source.AddPart(new RouteWorkAfterProbe());
            if(mode=="owned")source.Properties["OwnerID"]="resident";
            if(mode=="film")source.AddPart(new FiniteLiquidFilmPart());
            if(mode=="renewing")source.AddPart(new WellPart());
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));Assert.AreEqual(60,source.GetPart<LiquidPoolPart>().Volume);
        }
        [Test] public void FrozenWaterRefusesRatherThanMovingIceAsLiquid()
        {var tool=Tool();var source=Stock(20,"water");source.AddPart(new ThermalPart{Temperature=-5,FreezeTemperature=0});Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));}
        [Test] public void StaleSourceStockDoesNotExecuteAnOldQuote()
        {var tool=Tool();var source=Stock();var cmd=Stroke(tool);source.GetPart<LiquidPoolPart>().Volume--;Assert.False(Act(tool,cmd));Assert.AreEqual(59,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));}
        [Test] public void RepeatedGathersKeepTheActualCurrentChoice()
        {var tool=Tool();Stock();var cmd=Stroke(tool);Actions(tool);Assert.True(Act(tool,cmd));}
        [Test] public void PhantomPumpCannotOperateTheActualCarriedOwner()
        {var phantom=Tool();Stock();Assert.True(Pack.RemoveObject(phantom));var actual=Tool();actual.ID=phantom.ID;phantom.GetPart<PhysicsPart>().InInventory=Actor;Assert.False(Actions(phantom).Any(a=>a.Command.StartsWith("PumpLiquid|")));}
        [Test] public void InterruptedStrokeRestoresQuantitiesAndLeavesNoNewProjection()
        {var tool=Tool();Stock();var cmd=Stroke(tool);FailAfter();Assert.False(Act(tool,cmd));Assert.AreEqual(60,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));Assert.False(Zone.TileState.HasCoating(9,10,"acid"));}
        [Test] public void RollbackPreservesIndependentStocksAndActorInjury()
        {
            var tool=Tool();var source=Stock();var outlet=Stock(5,"acid",9,10);var cmd=Stroke(tool);int hp=Actor.GetStat("Hitpoints").BaseValue;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{source.GetPart<LiquidPoolPart>().Volume+=3;outlet.GetPart<LiquidPoolPart>().Volume+=7;Actor.GetStat("Hitpoints").BaseValue-=3;}});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(63,PoolUnits());Assert.AreEqual(12,PoolUnits(9,10));Assert.AreEqual(hp-3,Actor.GetStat("Hitpoints").BaseValue);
        }
        [Test] public void ExternallyConsumedUncommittedCreditCannotAlsoRefundItsSource()
        {
            var tool=Tool();Stock();var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Outlet().GetPart<LiquidPoolPart>().Volume=0});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(40,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));
        }
        [Test] public void ReplacedSourcePartCannotDestroyTheAlreadyPaidOutletOnRollback()
        {
            var tool=Tool();var source=Stock();var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{source.RemovePart(source.GetPart<LiquidPoolPart>());source.AddPart(new LiquidPoolPart{LiquidId="acid",Volume=40});}});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(40,PoolUnits());Assert.AreEqual(20,PoolUnits(9,10));
        }
        [Test] public void SaturatedSourceCannotDestroyUnrefundableOutletCredit()
        {
            var tool=Tool();var source=Stock();var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>source.GetPart<LiquidPoolPart>().Volume=int.MaxValue});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(int.MaxValue,PoolUnits());Assert.AreEqual(20,PoolUnits(9,10));
        }
        [Test] public void EmptyRollbackOutputKeepsAnIndependentAttachedPayload()
        {
            var tool=Tool();Stock();var cmd=Stroke(tool);Entity output=null;var independent=new RouteWorkAfterProbe();
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{output=Outlet();output.AddPart(independent);}});FailAfter();
            Assert.False(Act(tool,cmd));Assert.True(Zone.GetReadOnlyEntities().Contains(output));Assert.True(output.Parts.Contains(independent));Assert.AreEqual(60,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));
        }
        [Test] public void FullIntegerOutletRefusesWithoutWrappingOrSpending()
        {var tool=Tool();Stock();Stock(int.MaxValue,"acid",9,10);Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")&&a.Display.Contains("to 9,10")));Assert.AreEqual(60,PoolUnits());}
        [Test] public void UnadmittedFactoryOverridesRefuseBeforeCreatingAPuddle()
        {
            var tool=Tool();Stock();bool created=false;Factory.RegisterPartType<RouteWorkCreationProbe>("Examinable");RouteWorkCreationProbe.Run=e=>created=true;
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));Assert.False(created);Assert.AreEqual(60,PoolUnits());
        }
        [Test] public void DepletedOwnerIsNotRemovedIfReplenishedBeforeCommit()
        {
            var tool=Tool();var source=Stock(20);var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>source.GetPart<LiquidPoolPart>().Volume=1});
            Assert.False(Act(tool,cmd));Assert.True(Zone.GetReadOnlyEntities().Contains(source));Assert.AreEqual(21,PoolUnits());Assert.True(Zone.TileState.HasCoating(11,10,"acid"));
        }
        [Test] public void PumpAndActualTransferredPoolSaveWithoutRenewingStock()
        {
            var tool=Tool();Stock();Assert.True(Act(tool,Stroke(tool)));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Outlet());Assert.AreEqual(20,saved.GetPart<LiquidPoolPart>().Volume);
            var savedTool=PartRoundTripHelper.RoundTripEntityViaTokenGraph(tool);Assert.NotNull(savedTool.GetPart("DirectLiquidPump"));Assert.AreEqual(tool.ID,savedTool.ID);
        }
        [Test] public void ForgeMakesReusablePumpFromExactFiniteComponents()
        {
            Place("TinkersForge",10,9);var spike=Carry("IronSpikeComponent");Carry("FireClay").GetPart<StackerPart>().StackCount=2;
            Carry("LeatherBindingComponent").GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(spike,Choice(spike,"MaterialProcess|make_hand_pump|")));Assert.AreEqual(1,Count("HandPump"));
            Assert.AreEqual(0,Count("FireClay"));Assert.AreEqual(0,Count("LeatherBindingComponent"));Assert.AreEqual(0,Count("IronSpikeComponent"));
        }

        OverworldZoneManager EnterPeatWorks()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(SoddenDistrictPlan.WorksZoneID);
            Assert.NotNull(Zone);
            SettlementRuntime.ActiveZone = Zone;
            foreach (var cell in Zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
            Assert.True(Zone.AddEntity(Actor, 52, 2));
            return manager;
        }

        [TestCase(false)] [TestCase(true)]
        public void PeatWorksGapCanBeClearedWithAnOrdinaryFlaskOrPump(bool pumping)
        {
            var manager = EnterPeatWorks();
            var sources = Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "PouredLiquidPool").ToArray();
            Assert.That(sources, Has.Length.EqualTo(2));
            var source = sources.Single(e => Zone.GetEntityPosition(e) == (52, 3));
            var neighbor = sources.Single(e => Zone.GetEntityPosition(e) == (51, 3));
            Assert.AreEqual(12, PoolUnits(52, 3));
            Assert.AreEqual(12, PoolUnits(51, 3));
            Assert.True(Zone.TileState.HasCoating(52, 3, "bog-mire"));
            Assert.True(Zone.TileState.HasCoating(51, 3, "bog-mire"));
            Assert.False(Zone.TileState.Has(54, 10), "The regular door must stay dry.");

            var tool = pumping ? Tool() : Flask(0);
            string command;
            if (pumping)
            {
                var row = CellMenu(52, 3).Single(a => a.Via == tool && a.Command.StartsWith("PumpLiquid|" + source.ID + "|53|2|"));
                Assert.AreEqual(ActionTime.OneAction, row.Time);
                StringAssert.StartsWith("One action.", row.Hint);
                StringAssert.Contains("Outlet hazards remain.", row.Hint);
                StringAssert.Contains("Fill a compatible flask at the outlet while it is liquid.", row.Hint);
                command = row.Command;
            }
            else command = Choice(tool, "FillLiquidVessel|" + Id(source) + "|");
            Assert.True(Act(tool, command));
            Assert.False(Act(tool, command), "The committed source was removed.");
            Assert.False(Zone.GetReadOnlyEntities().Contains(source));
            Assert.False(Zone.TileState.HasCoating(52, 3, "bog-mire"));
            Assert.True(Zone.GetReadOnlyEntities().Contains(neighbor));
            Assert.AreEqual(12, PoolUnits(51, 3));
            Assert.AreEqual(pumping ? 12 : 0, PoolUnits(53, 2));
            Assert.AreEqual(pumping, Zone.TileState.HasCoating(53, 2, "bog-mire"));
            if (!pumping)
            {
                Assert.AreEqual(12, Units(tool));
                Assert.AreEqual("bog-mire", tool.GetPart<LiquidVesselPart>().LiquidId);
            }
            manager.UnloadZone(Zone.ZoneID);
            Assert.AreSame(Zone, manager.GetZone(Zone.ZoneID));
            var state = GameSessionState.Capture("sodden-mire", "test", manager, null, null);
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, Factory)).ZoneManager.GetZone(Zone.ZoneID);
            Assert.That(loaded.GetCell(52, 3).Objects.Any(e => e.HasPart<LiquidPoolPart>()), Is.False);
            Assert.False(loaded.TileState.HasCoating(52, 3, "bog-mire"));
            Assert.AreEqual(pumping, loaded.TileState.HasCoating(53, 2, "bog-mire"));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void PeatWorksFlaskRowDrawsTheQuotedAmountFromTheExactPumpOutlet(int flaskVolume)
        {
            EnterPeatWorks();
            var source = Zone.GetCell(52, 3).Objects.Single(e => e.HasPart<LiquidPoolPart>());
            var sibling = Zone.GetCell(51, 3).Objects.Single(e => e.HasPart<LiquidPoolPart>());
            var siblingPool = sibling.GetPart<LiquidPoolPart>();
            var pump = Tool();
            var flask = Flask(flaskVolume, "bog-mire");
            var waterFlask = Flask(1);
            Assert.IsEmpty(CellMenu(53, 2).Where(a => a.Name == "FillLiquid"));

            var stroke = CellMenu(52, 3).Single(a => a.Via == pump && a.Command.StartsWith("PumpLiquid|" + source.ID + "|53|2|"));
            Assert.True(ChooseRow(stroke, 52, 3));
            Assert.IsNull(Zone.GetEntityCell(source));
            Assert.False(Zone.TileState.HasCoating(52, 3, "bog-mire"));
            Assert.True(Zone.GetCell(52, 3).IsPassable());

            var outlet = Zone.GetCell(53, 2).Objects.Single(e => e.HasPart<LiquidPoolPart>());
            var outletPool = outlet.GetPart<LiquidPoolPart>();
            Assert.AreNotEqual(source.ID, outlet.ID);
            Assert.AreEqual("bog-mire", outletPool.LiquidId);
            Assert.AreEqual(12, outletPool.Volume);
            Assert.True(Zone.TileState.HasCoating(53, 2, "bog-mire"));

            var fill = CellMenu(53, 2).Single(a => a.Name == "FillLiquid");
            string command = "FillLiquidVessel|" + Id(outlet) + "|bog-mire";
            Assert.AreSame(flask, fill.Via);
            Assert.AreEqual(command, fill.Command);
            Assert.AreEqual(ActionTime.OneAction, fill.Time);
            Assert.AreEqual(ToolGroups.Fill, fill.Group);
            CollectionAssert.AreEqual(new[] { (53, 2) }, fill.TargetCells);
            Assert.True(fill.Display.StartsWith("fill with bog mire (53,2)"));
            string hint = $"Draw {12 - flaskVolume} bog mire. {flaskVolume} left in the source.";
            Assert.AreEqual(hint, fill.Hint);
            Assert.AreEqual(hint, Actions(flask).Single(a => a.Command == command).Hint);
            Assert.True(ChooseRow(fill, 53, 2));

            Assert.AreEqual(12, Units(flask));
            Assert.AreEqual("bog-mire", flask.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(1, Units(waterFlask));
            Assert.AreEqual("water", waterFlask.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(flaskVolume, outletPool.Volume);
            Assert.AreSame(flaskVolume == 0 ? null : Zone.GetCell(53, 2), Zone.GetEntityCell(outlet));
            Assert.AreEqual(flaskVolume > 0, Zone.TileState.HasCoating(53, 2, "bog-mire"));
            Assert.IsEmpty(CellMenu(53, 2).Where(a => a.Name == "FillLiquid"));
            Assert.AreSame(sibling, Zone.GetCell(51, 3).Objects.Single(e => e.HasPart<LiquidPoolPart>()));
            Assert.AreSame(siblingPool, sibling.GetPart<LiquidPoolPart>());
            Assert.AreEqual("bog-mire", siblingPool.LiquidId);
            Assert.AreEqual(12, siblingPool.Volume);
            Assert.True(Zone.TileState.HasCoating(51, 3, "bog-mire"));
        }

        [TestCase("old")] [TestCase("missing")] [TestCase("altered")]
        public void OptionalGapKeepsWorksUsableWhenItsSourcesCannotStage(string mode)
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            if (mode == "old") SoddenDistrictIntegrationTests.Version(manager, 26);
            if (mode == "missing") Factory.Blueprints.Remove("PouredLiquidPool");
            if (mode == "altered") Factory.Blueprints["PouredLiquidPool"].Parts["LiquidPool"]["Volume"] = "5";
            var works = manager.GetZone(SoddenDistrictPlan.WorksZoneID);
            Assert.NotNull(works);
            Assert.That(works.GetReadOnlyEntities().Any(e => e.BlueprintName == "PouredLiquidPool"), Is.False);
            Assert.That(works.GetReadOnlyEntities().Any(e => e.BlueprintName == "SoddenWorksLocker"), Is.True);
            Assert.False(works.TileState.Has(54, 10));
        }

        [TestCase(64, 51)] [TestCase(65, 52)]
        public void TwoRealWorldSeedsUseDifferentNorthWallGaps(int seed, int left)
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, seed, true);
            var works = manager.GetZone(SoddenDistrictPlan.WorksZoneID);
            Assert.NotNull(works);
            CollectionAssert.AreEquivalent(new[] { (left, 3), (left + 1, 3) },
                works.GetReadOnlyEntities().Where(e => e.BlueprintName == "PouredLiquidPool").Select(works.GetEntityPosition));
            Assert.True(works.GetCell(54, 10).IsPassable());
            Assert.False(works.TileState.Has(54, 10));
        }
    }
}
