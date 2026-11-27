using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class RouteWorkCreationProbe : ExaminablePart
    {public static Action<Entity> Run;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Run?.Invoke(ParentEntity);return base.HandleEvent(e);}}
    public sealed class RouteWorkAfterProbe : Part
    {public Action Run;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Run?.Invoke();return true;}}
    public sealed class RouteWorkToolsTests : FiftyWorldFixture
    {
        CavesOfOoo.Data.EntityFactory previousForge;
        System.Collections.Generic.Dictionary<string,GasDefinition> previousGases;bool previousGasInitialized;
        static readonly System.Reflection.FieldInfo GasRows=typeof(GasRegistry).GetField("_byId",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        static readonly System.Reflection.FieldInfo GasReady=typeof(GasRegistry).GetField("_initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        [SetUp] public void ReadyRouteWork()
        {
            previousForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            previousGases=new System.Collections.Generic.Dictionary<string,GasDefinition>((System.Collections.Generic.Dictionary<string,GasDefinition>)GasRows.GetValue(null));previousGasInitialized=GasRegistry.IsInitialized;
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));
        }
        [TearDown] public void RestoreRouteWork()
        {
            ForgePart.Factory=previousForge;MaterialProcessRegistry.ResetForTests();
            var rows=(System.Collections.Generic.Dictionary<string,GasDefinition>)GasRows.GetValue(null);rows.Clear();foreach(var pair in previousGases)rows[pair.Key]=pair.Value;GasReady.SetValue(null,previousGasInitialized);
        }
        Entity Cloud(int density=30,int x=11,int y=10)
        {var e=GasFactory.SpawnGas(Zone,x,y,"poison-vapor",density,2,Actor);Assert.NotNull(e);return e;}
        Entity Door(bool closed=true)
        {var e=Place("SalvagedTimber");e.GetPart<PhysicsPart>().Takeable=false;e.AddPart(new DoorPart{IsOpen=!closed});return e;}
        [Test] public void BellowsMovesSameThinCloudWithoutSpawningOrLosingDensity()
        {
            var tool=Carry("HandBellows");var gas=Cloud();var parts=gas.Parts.ToArray();var pool=gas.GetPart<GasPoolPart>();
            Assert.True(Act(tool,Choice(tool,"PushGas|")));Assert.AreSame(Zone.GetCell(12,10),Zone.GetEntityCell(gas));
            Assert.AreEqual(30,pool.Density);Assert.AreSame(Actor,pool.Creator);Assert.AreEqual(2,pool.Level);CollectionAssert.AreEqual(parts,gas.Parts);
            Assert.AreEqual(1,Zone.GetReadOnlyEntities().Count(e=>e.HasPart<GasPoolPart>()));
        }
        [TestCase("dense", "cloud-too-thick")]
        [TestCase("stable", "cloud-stable")]
        [TestCase("hidden", "cloud-no-path")]
        [TestCase("door", "cloud-no-path")]
        public void BellowsExplainsVisibleGasRefusalsInThePackAndWorldMenus(string mode, string code)
        {
            var tool = Carry("HandBellows");
            var gas = Cloud();
            var pool = gas.GetPart<GasPoolPart>();
            var source = Zone.GetEntityCell(gas);
            var beyond = Zone.GetCell(12, 10);
            Entity blocker = null;
            if (mode == "dense") pool.Density = DirectedAirPart.MaximumDensity + 1;
            if (mode == "stable") pool.Stable = true;
            if (mode == "hidden") beyond.IsVisible = false;
            if (mode == "door")
            {
                blocker = Place("SalvagedTimber", 12, 10);
                blocker.GetPart<PhysicsPart>().Takeable = false;
                blocker.AddPart(new DoorPart { IsOpen = false });
            }

            var packRow = Actions(tool).SingleOrDefault(row => row.DisabledCode == code);
            var worldRow = CellMenu(11, 10).SingleOrDefault(row => row.DisabledCode == code);
            Assert.NotNull(packRow);
            Assert.NotNull(worldRow);
            Assert.True(packRow.IsDisabled);
            Assert.True(worldRow.IsDisabled);
            Assert.AreEqual(packRow.DisabledReason, worldRow.DisabledReason);
            Assert.AreEqual(worldRow.DisabledReason, worldRow.Hint);
            Assert.False(Actions(tool).Any(row => !row.IsDisabled && row.Command?.StartsWith("PushGas|", StringComparison.Ordinal) == true));

            int tick = Clock.TickCount;
            int density = pool.Density;
            Assert.False(Act(tool, packRow.Command));
            Assert.False(ChooseRow(worldRow, 11, 10));
            Assert.False(Act(tool, "PushGas|" + gas.ID + "|unquoted"));
            Assert.AreSame(source, Zone.GetEntityCell(gas));
            Assert.AreEqual(density, pool.Density);
            Assert.AreEqual(tick, Clock.TickCount);
            if (blocker != null)
            {
                Assert.True(Zone.RemoveEntity(blocker));
                beyond.IsVisible = false;
                var hiddenRow = Actions(tool).Single(row => row.DisabledCode == code);
                Assert.AreEqual(packRow.DisabledReason, hiddenRow.DisabledReason, "The refusal does not distinguish hidden terrain from an obstruction.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BellowsDoesNotExposeAnUnseenSourceInEitherMenu(bool staleReference)
        {
            var tool = Carry("HandBellows");
            var gas = Cloud(DirectedAirPart.MaximumDensity + 1, staleReference ? 8 : 11, 10);
            Zone.GetEntityCell(gas).IsVisible = false;
            if (staleReference) Zone.GetCell(11, 10).Objects.Add(gas);
            bool GasRow(InventoryAction row) => row.DisabledCode?.StartsWith("cloud-", StringComparison.Ordinal) == true
                || row.Command?.StartsWith("PushGas|", StringComparison.Ordinal) == true;
            Assert.False(Actions(tool).Any(GasRow));
            Assert.False(CellMenu(11, 10).Any(GasRow));
        }

        [Test]
        public void ThinningTheSameCloudToTheBellowsLimitAllowsTheNormalWorldAction()
        {
            Carry("HandBellows");
            var gas = Cloud(DirectedAirPart.MaximumDensity + 1);
            var pool = gas.GetPart<GasPoolPart>();
            Assert.True(CellMenu(11, 10).Any(row => row.IsDisabled && row.DisabledCode == "cloud-too-thick"));

            pool.Density = DirectedAirPart.MaximumDensity;
            var actions = CellMenu(11, 10);
            Assert.False(actions.Any(row => row.DisabledCode == "cloud-too-thick"));
            var push = actions.Single(row => !row.IsDisabled && row.Command?.StartsWith("PushGas|", StringComparison.Ordinal) == true);
            Assert.True(ChooseRow(push, 11, 10));
            Assert.AreSame(Zone.GetCell(12, 10), Zone.GetEntityCell(gas));
            Assert.AreSame(pool, gas.GetPart<GasPoolPart>());
            Assert.AreEqual(DirectedAirPart.MaximumDensity, pool.Density);
        }
        [Test] public void InterruptedBellowsDoesNotMoveOrThinGas()
        {var tool=Carry("HandBellows");var gas=Cloud();var choice=Choice(tool,"PushGas|");FailAfter();Assert.False(Act(tool,choice));Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(gas));Assert.AreEqual(30,gas.GetPart<GasPoolPart>().Density);}
        [Test] public void RepeatedBellowsGatherRetainsDisplayedChoice()
        {var tool=Carry("HandBellows");Cloud();var choice=Choice(tool,"PushGas|");Actions(tool);Assert.True(Act(tool,choice));}
        [Test] public void PhantomBellowsWithDuplicateIdCannotOperateRealGas()
        {
            var phantom=Carry("HandBellows");Cloud();Assert.True(Pack.RemoveObject(phantom));
            var actual=Carry("HandBellows");actual.ID=phantom.ID;phantom.GetPart<PhysicsPart>().InInventory=Actor;
            Assert.False(Actions(phantom).Any(a=>a.Command.StartsWith("PushGas|")));
        }
        [Test] public void SandMarkSpendsOneUnitAndPersistsItsPlayerMeaning()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");sand.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("water"))));Assert.AreEqual(1,sand.GetPart<StackerPart>().StackCount);
            var mark=Zone.GetCell(10,10).Objects.Single(e=>e.BlueprintName=="TrailMark");Assert.False(mark.GetPart<PhysicsPart>().Solid);Assert.False(mark.HasPart<LightSourcePart>());
            StringAssert.Contains("water",mark.GetPart<TrailMarkPart>().DescribeState(default));
            var fresh=PartRoundTripHelper.RoundTripEntityViaTokenGraph(mark);Assert.AreEqual(mark.ID,fresh.ID);StringAssert.Contains("water",fresh.GetPart<TrailMarkPart>().DescribeState(default));
        }
        OverworldZoneManager EnterBareCaveFloor()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 2, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone("Overworld.6.7.2");
            Assert.True(BiomeCropPlan.IsOrdinaryCave(manager, Zone.ZoneID));
            var floor = Zone.Cells.Cast<Cell>().First(cell => cell.IsInterior && cell.IsEmpty()
                && cell.IsPassable() && !Zone.TileState.Has(cell.X, cell.Y));
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, floor.X, floor.Y));
            floor.IsVisible = floor.Explored = true;
            return manager;
        }

        [Test]
        public void MarkOnCarvedCaveFloorSpendsOneUnitAndSurvivesWholeSave()
        {
            var manager = EnterBareCaveFloor();
            var floor = Zone.GetEntityCell(Actor);
            var sand = Carry("SilverSand");
            sand.GetPart<StackerPart>().StackCount = 2;

            var command = Choice(sand, "MakeTrailMark|exit|");
            Assert.True(Act(sand, command));
            var marker = floor.Occupants.Single(entity => entity.BlueprintName == "TrailMark");
            Assert.That(sand.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.False(Actions(sand).Any(action => action.Command.StartsWith("MakeTrailMark|")));

            var state = GameSessionState.Capture("cave-trail", "test", manager, Clock, Actor);
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, Factory));
            var loadedFloor = loaded.ZoneManager.ActiveZone.GetEntityCell(loaded.Player);
            var loadedMarker = loadedFloor.Occupants.Single(entity => entity.BlueprintName == "TrailMark");
            Assert.That(loadedMarker.ID, Is.EqualTo(marker.ID));
            Assert.That(loadedMarker.GetPart<TrailMarkPart>().Note, Is.EqualTo("exit"));
            Assert.False(loadedFloor.Occupants.Any(entity => entity.HasTag("Terrain")));
        }

        [TestCase("unattached")]
        [TestCase("inactive")]
        [TestCase("fixture")]
        [TestCase("wet")]
        [TestCase("hidden")]
        public void EmptyGroundNeedsAnActiveDryAndVisibleCaveFloor(string context)
        {
            var manager = context == "unattached" ? null : EnterBareCaveFloor();
            var floor = Zone.GetEntityCell(Actor);
            if (context == "unattached") floor.IsInterior = true;
            if (context == "inactive") manager.SetActiveZone(new Zone("Overworld.6.7.1"));
            if (context == "fixture") Place("SalvagedTimber", floor.X, floor.Y);
            if (context == "wet") Zone.TileState.WriteCoating(floor.X, floor.Y, "water", 5);
            if (context == "hidden") floor.IsVisible = false;

            var sand = Carry("SilverSand");
            Assert.False(Actions(sand).Any(action => action.Command.StartsWith("MakeTrailMark|")));
            Assert.That(Pack.Objects, Does.Contain(sand));
        }

        [Test]
        public void LateWetCaveFloorLeavesSandAndIndependentCoating()
        {
            EnterBareCaveFloor();
            var floor = Zone.GetEntityCell(Actor);
            var sand = Carry("SilverSand");
            var command = Choice(sand, "MakeTrailMark|exit|");
            Actor.AddPart(new RouteWorkAfterProbe
            {
                Run = () => Zone.TileState.WriteCoating(floor.X, floor.Y, "water", 5)
            });

            Assert.False(Act(sand, command));
            Assert.That(Pack.Objects, Does.Contain(sand));
            Assert.True(Zone.TileState.HasCoating(floor.X, floor.Y, "water"));
            Assert.False(floor.Occupants.Any(entity => entity.BlueprintName == "TrailMark"));
        }

        [Test] public void WetFloorAndDuplicateMarkRefuseSandPayment()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Zone.TileState.WriteCoating(10,10,"water",5);
            Assert.False(Actions(sand).Any(a=>a.Command.StartsWith("MakeTrailMark|")));
        }
        [Test] public void InterruptedMarkLeavesSandAndNoGroundMarker()
        {Bed(10,10,false);var sand=Carry("SilverSand");var choice=Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("exit"));FailAfter();Assert.False(Act(sand,choice));Assert.True(Pack.Objects.Contains(sand));Assert.False(Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="TrailMark"));}
        [TestCase("IronSpikeComponent")]
        [TestCase("FieldHaftComponent")]
        public void SamePhysicalBraceHoldsSharedDoorAndCanBeRecovered(string blueprint)
        {
            var door = Door();
            var brace = Carry(blueprint);
            string id = brace.ID;
            Assert.True(Act(brace, Choice(brace, "BraceDoor|")));
            Assert.False(door.GetPart<DoorPart>().CanOperate(Actor, Zone));
            Assert.False(door.GetPart<DoorPart>().TrySetOpen(Actor, Zone, true));
            string name = blueprint == "IronSpikeComponent" ? "iron spike" : "rough field haft";
            StringAssert.Contains(name, Actions(door).Single(row => row.DisabledCode == "braced").DisabledReason);
            Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(brace));
            Assert.False(brace.GetPart<PhysicsPart>().Takeable);
            Assert.True(Act(brace, Choice(brace, "RemoveDoorBrace|")));
            Assert.AreEqual(id, brace.ID);
            Assert.True(Pack.Objects.Contains(brace));
            Assert.True(door.GetPart<DoorPart>().TrySetOpen(Actor, Zone, true));
        }
        [TestCase("open")] [TestCase("locked")] [TestCase("owned")] [TestCase("stack")]
        public void DoorBracingRefusesUnsupportedDoorOrAmbiguousSpike(string mode)
        {
            var door=Door();var spike=Carry("IronSpikeComponent");
            if(mode=="open")door.GetPart<DoorPart>().IsOpen=true;
            if(mode=="locked")door.AddPart(new LockPart{IsLocked=true});
            if(mode=="owned")door.GetPart<DoorPart>().OwnerId="other";
            if(mode=="stack")spike.GetPart<StackerPart>().StackCount=2;
            Assert.False(Actions(spike).Any(a=>a.Command.StartsWith("BraceDoor|")));
        }
        [TestCase("IronSpikeComponent")]
        [TestCase("FieldHaftComponent")]
        public void OrdinaryDoorOpeningAiAlsoRespectsAndRecoversFromBrace(string blueprint)
        {
            var door=Door();var spike=Carry(blueprint);var npc=Place("Villager",12,10);npc.SetTag("CanOpenDoors");
            Assert.True(door.GetPart<DoorPart>().CanOperate(npc,Zone));Assert.True(Act(spike,Choice(spike,"BraceDoor|")));
            Assert.False(door.GetPart<DoorPart>().CanOperate(npc,Zone));Assert.False(door.GetPart<DoorPart>().TrySetOpen(npc,Zone,true));
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.True(door.GetPart<DoorPart>().TrySetOpen(npc,Zone,true));
        }
        [Test] public void BrushingMarkDoesNotReturnSandAndAllowsLaterMarking()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("north"))));
            var mark=Zone.GetCell(10,10).Objects.Single(e=>e.BlueprintName=="TrailMark");Assert.True(Act(mark,Choice(mark,"ClearTrailMark|")));
            Assert.AreEqual(0,Count("SilverSand"));Assert.IsNull(Zone.GetEntityCell(mark));
        }
        [Test] public void ExistingMarkRefusesSecondUnit()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");sand.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("exit"))));
            Assert.False(Actions(sand).Any(a=>a.Command.StartsWith("MakeTrailMark|")));Assert.AreEqual(1,sand.GetPart<StackerPart>().StackCount);
        }
        [Test] public void LateMarkerCustodyChangeCannotLeaveHeldAndGroundAlias()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Entity mark=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{mark=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="TrailMark");Assert.True(Pack.AddObject(mark));}});
            Assert.False(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("water"))));
            Assert.IsNull(Zone.GetEntityCell(mark));Assert.True(Pack.Objects.Contains(mark));Assert.AreEqual(0,Count("SilverSand"),"a retained paid output does not also refund material");
        }
        [TestCase("IronSpikeComponent")]
        [TestCase("FieldHaftComponent")]
        public void LateDoorRemovalRefusesBraceWithoutRestoringDoorOrLosingStock(string blueprint)
        {
            var door=Door();var spike=Carry(blueprint);var choice=Choice(spike,"BraceDoor|");
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Zone.RemoveEntity(door)});Assert.False(Act(spike,choice));
            Assert.IsNull(Zone.GetEntityCell(door));Assert.True(Pack.Objects.Contains(spike));Assert.True(spike.GetPart<PhysicsPart>().Takeable);
        }
        [Test] public void RecoveryKeepsOriginalIdentityBesideAnotherSpikeStack()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));
            var spare=Carry("IronSpikeComponent");spare.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.True(Pack.Objects.Contains(spike));Assert.AreEqual(1,spike.GetPart<StackerPart>().StackCount);Assert.AreEqual(2,spare.GetPart<StackerPart>().StackCount);
        }
        [TestCase("IronSpikeComponent")]
        [TestCase("FieldHaftComponent")]
        public void BracedDoorReferenceRoundTripsWithTheOriginalStockIdentity(string blueprint)
        {
            var door=Door();var spike=Carry(blueprint);Assert.True(Act(spike,Choice(spike,"BraceDoor|")));
            var fresh=PartRoundTripHelper.RoundTripEntityViaTokenGraph(spike);Assert.AreEqual(spike.ID,fresh.ID);
            Assert.AreEqual(blueprint,fresh.BlueprintName);
            Assert.AreEqual(door.ID,fresh.GetPart<LodgedSpikePart>().Door.ID);Assert.True(fresh.GetPart<LodgedSpikePart>().Door.GetPart<DoorPart>().IsClosed);Assert.False(fresh.GetPart<PhysicsPart>().Takeable);
        }

        [TestCase("SalvagedTimber", false)]
        [TestCase("FieldHaftComponent", true)]
        public void RawTimberAndStackedHaftsDoNotOfferABrace(string blueprint, bool stacked)
        {
            Door();
            var stock = Carry(blueprint);
            if (stacked) stock.GetPart<StackerPart>().StackCount = 2;
            Assert.IsFalse(Actions(stock).Any(action => action.Command.StartsWith("BraceDoor|")));
        }
        [Test] public void FullPackRefusesSpikeRecoveryWithoutLosingTheBrace()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));Pack.MaxWeight=0;
            Assert.False(Actions(spike).Any(a=>a.Command.StartsWith("RemoveDoorBrace|")));Assert.False(door.GetPart<DoorPart>().CanOperate(Actor,Zone));Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(spike));
        }
        [Test] public void UnadmittedFactoryPartsRefuseMarkerBeforeCreationAndPayment()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Entity created=null;
            Factory.RegisterPartType<RouteWorkCreationProbe>("Examinable");
            RouteWorkCreationProbe.Run=e=>{if(e.BlueprintName=="TrailMark"){created=e;Assert.True(Pack.AddObject(e));}};
            try{Assert.False(Actions(sand).Any(a=>a.Command.StartsWith("MakeTrailMark|")));Assert.IsNull(created);Assert.True(Pack.Objects.Contains(sand));}
            finally{RouteWorkCreationProbe.Run=null;}
        }
        [Test] public void DoorClosingBeyondGasBeforeCommitKeepsCloudAtSource()
        {
            var tool=Carry("HandBellows");var gas=Cloud();string command=Choice(tool,"PushGas|");
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{var d=Place("VillageDoor",12,10);d.GetPart<DoorPart>().IsOpen=false;}});
            Assert.False(Act(tool,command));Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(gas));Assert.AreEqual(30,gas.GetPart<GasPoolPart>().Density);
        }
        [Test] public void BellowsForgeManifestSpendsExactThreeComponentsAndSurvivesHintRefresh()
        {
            Place("TinkersForge");var spike=Carry("IronSpikeComponent");Carry("OakHaftComponent");Carry("LeatherBindingComponent");
            string command=Choice(spike,"MaterialProcess|make_hand_bellows|");UseGuides.SectionText(spike,Zone,null,Actor);Actions(spike);
            Assert.True(Act(spike,command));Assert.AreEqual(0,Count("IronSpikeComponent"));Assert.AreEqual(0,Count("OakHaftComponent"));Assert.AreEqual(0,Count("LeatherBindingComponent"));Assert.AreEqual(1,Count("HandBellows"));
        }

        [TestCase("make_hand_bellows", "OakHaftComponent", "FieldHaftComponent")]
        [TestCase("make_field_haft_bellows", "FieldHaftComponent", "OakHaftComponent")]
        public void BellowsOffersBothBillsAndSpendsOnlyTheSelectedHaft(string recipe, string spent, string retained)
        {
            Place("TinkersForge");
            var spike = Carry("IronSpikeComponent");
            var binding = Carry("LeatherBindingComponent");
            var usedHaft = Carry(spent);
            var unusedHaft = Carry(retained);
            Assert.That(Choice(spike, "MaterialProcess|make_hand_bellows|"), Is.Not.EqualTo(
                Choice(spike, "MaterialProcess|make_field_haft_bellows|")));

            string selected = Choice(spike, "MaterialProcess|" + recipe + "|");
            UseGuides.SectionText(spike, Zone, null, Actor);
            Actions(spike);
            Assert.True(Act(spike, selected));
            Assert.Zero(Count("IronSpikeComponent"));
            Assert.Zero(Count("LeatherBindingComponent"));
            Assert.Zero(Count(spent));
            Assert.That(Count(retained), Is.EqualTo(1));
            Assert.That(unusedHaft.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.False(Pack.Contains(spike) || Pack.Contains(binding) || Pack.Contains(usedHaft));
            Assert.That(Count("HandBellows"), Is.EqualTo(1));
        }

        [Test]
        public void FieldPreparedTimberCanSupplyBellowsWithoutAnyOakHaft()
        {
            var timber = Carry("SalvagedTimber");
            Assert.True(Act(timber, "PrepareRecipe|shape_haft"));
            var fieldHaft = Pack.Objects.Single(item => item.BlueprintName == "FieldHaftComponent");
            Assert.Zero(Count("SalvagedTimber"));
            Assert.Zero(Count("OakHaftComponent"));

            Place("TinkersForge");
            var spike = Carry("IronSpikeComponent");
            Carry("LeatherBindingComponent");
            Assert.True(Act(spike, Choice(spike, "MaterialProcess|make_field_haft_bellows|")));
            Assert.False(Pack.Contains(fieldHaft));
            Assert.Zero(Count("FieldHaftComponent"));
            Assert.That(Count("HandBellows"), Is.EqualTo(1));
        }

        [TestCase("SalvagedTimber")]
        [TestCase("WillowHaftComponent")]
        public void NeitherBellowsBillAcceptsUnpreparedTimberOrOtherWeaponHafts(string unsuitable)
        {
            Place("TinkersForge");
            var spike = Carry("IronSpikeComponent");
            var binding = Carry("LeatherBindingComponent");
            var wrong = Carry(unsuitable);
            Assert.False(Actions(spike).Any(action => !action.IsDisabled &&
                (action.Command.StartsWith("MaterialProcess|make_hand_bellows|", StringComparison.Ordinal)
                || action.Command.StartsWith("MaterialProcess|make_field_haft_bellows|", StringComparison.Ordinal))));
            Assert.True(Pack.Contains(spike) && Pack.Contains(binding) && Pack.Contains(wrong));
            Assert.Zero(Count("HandBellows"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FieldHaftBellowsRefusesStaleStockOrRollsBackAStagedBill(bool lateFailure)
        {
            Place("TinkersForge");
            var spike = Carry("IronSpikeComponent");
            var binding = Carry("LeatherBindingComponent");
            var fieldHaft = Carry("FieldHaftComponent");
            var oakHaft = Carry("OakHaftComponent");
            string command = Choice(spike, "MaterialProcess|make_field_haft_bellows|");
            if (lateFailure) FailAfter();
            else Assert.True(InventorySystem.Drop(Actor, fieldHaft, Zone));

            Assert.False(Act(spike, command));
            Assert.True(Pack.Contains(spike) && Pack.Contains(binding) && Pack.Contains(oakHaft));
            Assert.That(Count("IronSpikeComponent"), Is.EqualTo(1));
            Assert.That(Count("LeatherBindingComponent"), Is.EqualTo(1));
            Assert.That(Count("OakHaftComponent"), Is.EqualTo(1));
            Assert.That(Count("FieldHaftComponent"), Is.EqualTo(1));
            Assert.That(Pack.Contains(fieldHaft), Is.EqualTo(lateFailure));
            if (!lateFailure) Assert.That(Zone.GetEntityCell(fieldHaft), Is.SameAs(Zone.GetEntityCell(Actor)));
            Assert.Zero(Count("HandBellows"));
        }

        [Test] public void DestroyedDoorLeavesExactSpikeRecoverable()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));Assert.True(Zone.RemoveEntity(door));
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.True(Pack.Objects.Contains(spike));Assert.AreEqual(1,Count("IronSpikeComponent"));
        }
        [Test] public void InterruptedBraceLeavesOrdinaryDoorAndOriginalSpike()
        {var door=Door();var spike=Carry("IronSpikeComponent");var choice=Choice(spike,"BraceDoor|");FailAfter();Assert.False(Act(spike,choice));Assert.True(Pack.Objects.Contains(spike));Assert.True(spike.GetPart<PhysicsPart>().Takeable);Assert.True(door.GetPart<DoorPart>().CanOperate(Actor,Zone));}
    }
}
