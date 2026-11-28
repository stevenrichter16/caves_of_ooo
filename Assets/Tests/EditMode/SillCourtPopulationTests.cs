using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Real generation/stock/path tests; no renderer or simulated Play acceptance.</summary>
    public sealed class SillCourtPopulationTests
    {
        const string Sill = "Overworld.10.10.0";
        const string SettlingDeposit = "SillSettlingDeposit";
        static readonly string[] Keepers = { "Weaponsmith", "Armorer", "Apothecary", "Arcanist", "Provisioner" };
        static readonly (int x, int y)[] Aprons = {
            (37,6),(38,6),(41,6),(42,6),(37,7),(38,7),(39,7),(40,7),(41,7),(42,7) };
        DensityLootTestScope scope;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); }
        [TearDown] public void Cleanup() { scope?.Dispose(); }
        OverworldZoneManager Manager(int seed = 64)
        { scope.Seed(seed); return OverworldZoneManager.CreateDetached(scope.Factory, seed, true); }
        static Entity[] Owners(Zone zone, string name)
            => zone.GetAllEntities().Where(e => e.BlueprintName == name).ToArray();
        static Entity One(Zone zone, string name)
        { var owners = Owners(zone, name); Assert.AreEqual(1, owners.Length, name); return owners[0]; }
        static bool At(Zone zone, Entity owner, int x, int y)
        { var cell = zone.GetEntityCell(owner); return cell != null && cell.X == x && cell.Y == y; }
        static bool Has(Zone zone, int x, int y, string name)
            => zone.GetCell(x, y).Objects.Any(e => e.BlueprintName == name);
        static void FacingPair(Zone zone)
        {
            Assert.True(At(zone, One(zone, "Weaponsmith"), 32, 5), "The real smith must tend the court's west workshop.");
            Assert.True(At(zone, One(zone, "Provisioner"), 46, 5), "The real provisioner must tend the mirrored east shop.");
            Assert.True(Has(zone, 32, 6, "MarketStall")); Assert.True(Has(zone, 44, 5, "MarketStall"));
            Assert.True(Has(zone, 35, 5, "TinkersForge"));
            foreach (var p in new[] { (36,6), (43,6) })
            { Assert.False(zone.GetCell(p.Item1,p.Item2).BlocksMovement()); Assert.False(Has(zone,p.Item1,p.Item2,"VillageDoor"), "Existing shop thresholds are open gaps, not new doors."); }
        }
        static bool[,] Reachable(Zone zone, int sx, int sy)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var queue = new Queue<(int x,int y)>();
            Assert.False(zone.GetCell(sx,sy).BlocksMovement(), "The real well approach must stay open.");
            seen[sx,sy] = true; queue.Enqueue((sx,sy));
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in new[] { (1,0),(-1,0),(0,1),(0,-1) })
                {
                    int x=p.x+d.Item1, y=p.y+d.Item2;
                    if (!zone.InBounds(x,y) || seen[x,y] || zone.GetCell(x,y).BlocksMovement()) continue;
                    seen[x,y]=true; queue.Enqueue((x,y));
                }
            }
            return seen;
        }
        static string Snapshot(Zone zone)
            => string.Join(";", zone.GetAllEntities().OrderBy(e=>e.ID).Select(e=> {
                var c=zone.GetEntityCell(e); return e.ID+":"+e.BlueprintName+":"+c?.X+","+c?.Y; }))
                + "|" + zone.TileState.ToSaveString();
        ZoneGenerationPipeline Pipeline(OverworldZoneManager manager, string id)
        {
            var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone", BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(method); return (ZoneGenerationPipeline)method.Invoke(manager,new object[]{id});
        }

        [TestCase(64)] [TestCase(65)] [TestCase(777)]
        public void FreshFullTownGroupsExistingStockedShopsAndKeepsTheirRealServicesReachable(int seed)
        {
            var manager=Manager(seed); var zone=manager.GetZone(Sill); Assert.NotNull(zone); FacingPair(zone);
            foreach (string name in Keepers)
            {
                var owner=One(zone,name); Assert.Greater(owner.GetPart<InventoryPart>().Objects.Count,0,name+" must have real stock.");
                Assert.AreEqual(name+"Stock",owner.GetProperty("ShopStockTable"));
                Assert.NotNull(owner.GetPart<TraderPart>()); Assert.NotNull(owner.GetPart<ConversationPart>());
            }
            var well=One(zone,"Well"); Assert.True(At(zone,well,40,12));
            Assert.NotNull(well.GetPart<WellPart>()); Assert.NotNull(well.GetPart<WellSitePart>());
            Assert.AreEqual(Sill,well.GetProperty("SettlementId"));
            Assert.AreEqual(SettlementSiteDefinitions.MainWellSiteId,well.GetProperty("SettlementSiteId"));
            Assert.AreEqual(2,Owners(zone,"TinkersForge").Length,"One existing shop forge plus the existing hub forge; no cloned service.");
            Assert.AreEqual(2,Owners(zone,"AlchemyStill").Length,"Existing apothecary and hub stills remain.");
            foreach (string station in new[] { "TinkersForge", "AlchemyStill" })
                Assert.True(Owners(zone,station).Any(e=> { var c=zone.GetEntityCell(e); return Math.Max(Math.Abs(c.X-40),Math.Abs(c.Y-12))<=4; }),station+" hub placement");
            Assert.True(Owners(zone,"Chest").Any(e=> {
                var c=zone.GetEntityCell(e); return Math.Max(Math.Abs(c.X-40),Math.Abs(c.Y-12))<=4
                    && e.GetPart<ContainerPart>()?.Contents.Count>0; }),"The existing stocked starting chest stays near the square.");
            foreach (var p in Aprons)
            {
                Assert.AreEqual(1,zone.GetCell(p.x,p.y).Objects.Count(e=>e.BlueprintName=="StoneFloor"),"One real apron floor at "+p);
                Assert.True(Has(zone,p.x,p.y,"Grass"),"Retain the actual substrate, not invented empty terrain.");
                Assert.False(zone.GetCell(p.x,p.y).IsInterior,"The open court is not weather shelter.");
                Assert.False(zone.GetCell(p.x,p.y).BlocksMovement(),"No later decor/resident may occupy the working approach.");
            }
            var reachable=Reachable(zone,40,11);
            foreach (var p in new[] { (36,6),(43,6),(33,5),(35,6),(46,6),(79,12) })
                Assert.True(reachable[p.Item1,p.Item2],"Actual cardinal route from the well to "+p);
            Assert.IsNotEmpty(Owners(zone,"VillageDoor"),"Ordinary homes retain their existing doors.");
            Assert.IsNotEmpty(Owners(zone,"Elder")); Assert.IsNotEmpty(Owners(zone,"WellKeeper")); Assert.IsNotEmpty(Owners(zone,"Farmer"));
        }

        [TestCase(false)] [TestCase(true)]
        public void SillAidBelongsToTheOrdinaryResidentAcrossWorldRebuild(bool restore)
        {
            var manager = Manager(65);
            var zone = manager.GetZone(Sill);
            var resident = Owners(zone, "Scribe").Single(e => e.GetProperty("SettlementId") == Sill);
            if (restore)
            {
                var aid = resident.GetPart<CivilianAidPart>();
                if (aid != null) resident.RemovePart(aid);
                SaveGraphSerializer.RebuildLoadedWorld(new GameSessionState { ZoneManager = manager });
                SaveGraphSerializer.RebuildLoadedWorld(new GameSessionState { ZoneManager = manager });
            }
            Assert.That(zone.GetReadOnlyEntitiesWithPart<CivilianAidPart>(), Is.EqualTo(new[] { resident }));
            Assert.AreEqual(1, resident.Parts.OfType<CivilianAidPart>().Count());
        }

        [TestCase(2)] [TestCase(41)] [TestCase(64)] [TestCase(65)] [TestCase(777)]
        public void SettlingPocketPlacesOneFiniteStockBesideTheReachableWellCourt(int seed)
        {
            var zone = Manager(seed).GetZone(Sill);
            var deposit = One(zone, SettlingDeposit);
            var inlet = zone.GetCell(42, 18);
            Assert.AreSame(inlet, zone.GetEntityCell(deposit));
            Assert.True(deposit.HasTag("Terrain"));
            Assert.False(deposit.GetPart<PhysicsPart>().Solid);
            Assert.False(deposit.GetPart<PhysicsPart>().Takeable);
            Assert.That(deposit.GetProperty("OwnerID"), Is.Null.Or.Empty);
            var harvest = deposit.GetPart<HarvestablePart>();
            Assert.NotNull(harvest);
            Assert.False(harvest.Harvested);
            Assert.AreEqual("SilverSand", harvest.YieldBlueprint);
            Assert.AreEqual((1, 1, 100), (harvest.YieldMin, harvest.YieldMax, harvest.YieldChance));
            var stock = inlet.Occupants.Single(e => e.HasPart<LiquidPoolPart>());
            Assert.AreEqual("PouredLiquidPool", stock.BlueprintName);
            Assert.AreEqual(12, stock.GetPart<LiquidPoolPart>().Volume);
            Assert.True(zone.TileState.HasCoating(inlet.X, inlet.Y, stock.GetPart<LiquidPoolPart>().LiquidId));
            Assert.False(inlet.BlocksMovement());
            Assert.True(Reachable(zone, 40, 11)[42, 17]);
            var outlet = zone.GetCell(41, 17);
            Assert.False(outlet.BlocksMovement());
            Assert.IsEmpty(outlet.Occupants.Where(e => e.HasPart<LiquidPoolPart>()));
        }

        [TestCase("Takeable")] [TestCase("Solid")]
        public void SettlingPocketRefusesAnInvalidAuthoredPool(string unsafeProperty)
        {
            var manager = Manager();
            scope.Factory.Blueprints["PouredLiquidPool"].Parts["Physics"][unsafeProperty] = "true";
            var zone = manager.GetZone(Sill);
            Assert.NotNull(zone);
            Assert.IsEmpty(Owners(zone, SettlingDeposit));
            Assert.IsEmpty(zone.GetCell(42, 18).Occupants.Where(e => e.HasPart<LiquidPoolPart>()));
        }

        [TestCase("other-village")] [TestCase("alias")] [TestCase("name")] [TestCase("biome")] [TestCase("profile")]
        public void CourtDoesNotLeakIntoAnotherAddressOrChangedSillMeaning(string fault)
        {
            var manager=Manager(); string id=Sill;
            if (fault=="other-village") id="Overworld.5.9.0";
            if (fault=="alias")
            { id="Overworld.7.8.0"; manager.WorldMap.Tiles[7,8]=BiomeType.Spread; manager.WorldMap.SetPOI(7,8,new PointOfInterest(POIType.Village,"Sill","Villagers")); }
            if (fault=="name") manager.WorldMap.SetPOI(10,10,new PointOfInterest(POIType.Village,"Another village","Villagers"));
            if (fault=="biome") manager.WorldMap.Tiles[10,10]=BiomeType.Sodden;
            if (fault=="profile") manager.WorldMap.SetPOI(10,10,new PointOfInterest(POIType.Village,"Sill","Villagers",profile:"TentCamp"));
            var zone=manager.GetZone(id); Assert.NotNull(zone);
            Assert.False(Owners(zone,"Weaponsmith").Any(e=>At(zone,e,32,5))
                && Owners(zone,"Provisioner").Any(e=>At(zone,e,46,5)),"These preferred places belong only to the exact ordinary Sill contract.");
            Assert.IsEmpty(Owners(zone, SettlingDeposit));
            Assert.IsFalse(Owners(zone, "Scribe").Any(e => e.HasPart<CivilianAidPart>()));
        }

        sealed class Obstruction : IZoneBuilder
        {
            public string Name=>"SillCourtObstructionProbe"; public int Priority=>3859;
            public string Kind; public Entity Owner; public Entity[] Original; public string Coatings;
            public int X => Kind=="second-reserved" ? 43 : 31;
            public int AnchorX => Kind=="footprint" ? X-1 : X;
            public static string Surface(Zone z,int x) => string.Join(";",(z.TileState.Get(x,4)?.Coatings ?? new List<ZoneTileState.Layer>()).Select(c=>c.Id+":"+c.Turns))+"|"+z.TileState.Heat(x,4);
            public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
            {
                var cell=zone.GetCell(X,4); Original=cell.Objects.ToArray();
                if (Kind=="reserved" || Kind=="second-reserved") zone.GenReservedCells.Add((X,4));
                else if (Kind=="coating") zone.TileState.WriteCoating(X,4,"water",4);
                else
                {
                    Owner=factory.CreateEntity(Kind=="liquid"?"MirePool":Kind=="stairs"?"StairsDown":(Kind=="loose-prop" || Kind=="footprint")?"FireClay":"Villager");
                    Assert.NotNull(Owner);
                    if (Kind=="footprint") Owner.AddPart(new SpatialFootprintPart { CellsRaw="0,0;1,0" });
                    Assert.True(zone.AddEntity(Owner,AnchorX,4));
                    if (Kind=="footprint")
                    {
                        Assert.False(cell.Objects.Contains(Owner),"The prop is anchored outside the preferred court.");
                        Assert.True(cell.Occupants.Contains(Owner),"Its real passable body crosses into the workshop.");
                        Assert.False(cell.BlocksMovement(),"A solidity-only check cannot catch this physical occupant.");
                    }
                }
                if (Kind=="loose-prop" || Kind=="coating") Assert.False(cell.BlocksMovement(),"Passability alone must not permit the preferred shop to overwrite an owner or hazard.");
                Coatings=Surface(zone,X); return true;
            }
        }
        [TestCase("reserved")] [TestCase("second-reserved")] [TestCase("coating")] [TestCase("liquid")] [TestCase("stairs")] [TestCase("resident")] [TestCase("loose-prop")] [TestCase("footprint")]
        public void OccupiedPreferredWorkshopFallsBackWithoutClearingOrCloning(string obstruction)
        {
            // Establish this seed's real positive preferred placement before changing one fit condition.
            FacingPair(Manager().GetZone(Sill));
            var manager=Manager(); var pipeline=Pipeline(manager,Sill); pipeline.MaxRetries=1;
            var probe=new Obstruction{Kind=obstruction}; pipeline.AddBuilder(probe);
            var zone=new Zone(Sill); manager.SetActiveZone(zone);
            Assert.True(pipeline.Generate(zone,scope.Factory,new Random(64^Sill.GetHashCode())));
            var smith=One(zone,"Weaponsmith"); Assert.False(At(zone,smith,32,5),"An invalid preferred workshop must use ordinary placement.");
            Assert.Greater(smith.GetPart<InventoryPart>().Objects.Count,0,"Fallback still opens the existing shop.");
            foreach (string name in Keepers) Assert.AreEqual(1,Owners(zone,name).Length,name+" must never be duplicated.");
            foreach (var owner in probe.Original) Assert.AreSame(zone.GetCell(probe.X,4),zone.GetEntityCell(owner),"Do not erase existing owners to force a court.");
            if (probe.Owner!=null) Assert.AreSame(zone.GetCell(probe.AnchorX,4),zone.GetEntityCell(probe.Owner));
            if (obstruction=="reserved" || obstruction=="second-reserved") Assert.True(zone.GenReservedCells.Contains((probe.X,4)));
            Assert.AreEqual(probe.Coatings,Obstruction.Surface(zone,probe.X),"No presentation composition may drain or erase a hazard.");
        }

        [Test] public void MissingPairedShopContentDoesNotLeaveAHalfBuiltCourt()
        {
            FacingPair(Manager().GetZone(Sill));
            Assert.True(scope.Factory.Blueprints.Remove("Provisioner"));
            var zone=Manager().GetZone(Sill); Assert.NotNull(zone);
            Assert.IsEmpty(Owners(zone,"Provisioner"));
            Assert.False(At(zone,One(zone,"Weaponsmith"),32,5),"A missing paired shop falls back to the existing ordinary town placement.");
            Assert.False(Aprons.All(p=>Has(zone,p.x,p.y,"StoneFloor")),"Do not leave a complete new court pretending its missing service exists.");
        }

        [Test] public void CachedTownIsReattachedWithoutRegenerationOrNewCourtOwners()
        {
            var manager=Manager(); var old=new Zone(Sill);
            var grass=scope.Factory.CreateEntity("Grass"); var wall=scope.Factory.CreateEntity("StoneWall");
            Assert.True(old.AddEntity(grass,31,4)); Assert.True(old.AddEntity(wall,31,4)); old.GenReservedCells.Add((31,4));
            manager.SetActiveZone(old); string before=Snapshot(old); int version=old.EntityVersion;
            Assert.AreSame(old,manager.GetZone(Sill)); Assert.AreSame(old,manager.GetZone(Sill));
            Assert.AreEqual(before,Snapshot(old)); Assert.AreEqual(version,old.EntityVersion);
            Assert.IsEmpty(Owners(old,"Weaponsmith")); Assert.IsEmpty(Owners(old,"Provisioner"));
            Assert.IsEmpty(Owners(old, SettlingDeposit));
            Assert.True(old.GenReservedCells.Contains((31,4))); Assert.AreSame(old.GetCell(31,4),old.GetEntityCell(wall));
        }
    }

    public sealed class SillSettlingPocketInteractionTests : FiftyWorldFixture
    {
        const string Sill = "Overworld.10.10.0";
        const string Deposit = "SillSettlingDeposit";
        const string Gravel = "SillBankGravel";
        const string GatherGravel = "GatherSillBankGravel";
        const string Harvest = "Harvest";
        const string ClayAndGrit = "FireClay:1:0;CoarseGrit:1:2";
        const int InletX = 42, InletY = 18;

        OverworldZoneManager EnterSill()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(Sill);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, InletX, InletY - 1));
            foreach (var cell in Zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
            return manager;
        }

        Cell Inlet => Zone.GetCell(InletX, InletY);
        Entity Pocket => Inlet.Occupants.Single(e => e.BlueprintName == Deposit);
        InventoryAction[] HarvestRows() => CellMenu(InletX, InletY)
            .Where(row => row.Command == Harvest || row.Command == GatherGravel).ToArray();

        void DrainPocket(Entity flask)
        {
            var pool = Inlet.Occupants.Single(e => e.HasPart<LiquidPoolPart>());
            Assert.True(Act(flask, Choice(flask, "FillLiquidVessel|" + Id(pool) + "|")));
        }

        void StageGravel(Entity deposit, InventoryTransaction transaction)
        {
            var attempt = GameEvent.New("InventoryAction");
            attempt.SetParameter("Actor", Actor);
            attempt.SetParameter("Zone", Zone);
            attempt.SetParameter("Command", GatherGravel);
            attempt.SetParameter("InventoryTransaction", transaction);
            Assert.False(deposit.FireEventAndRelease(attempt));
        }

        [TestCase(false)] [TestCase(true)]
        public void SettlingPocketDrainsThroughAnOrdinaryToolAndKeepsOneSpentDeposit(bool pumping)
        {
            var manager = EnterSill();

            var inlet = Inlet;
            var deposit = inlet.Occupants.SingleOrDefault(e => e.BlueprintName == Deposit);
            Assert.NotNull(deposit, "The fresh Sill pocket has a real deposit under the inlet.");
            var harvest = deposit.GetPart<HarvestablePart>();
            var source = inlet.Occupants.Single(e => e.BlueprintName == "PouredLiquidPool");
            var liquid = source.GetPart<LiquidPoolPart>().LiquidId;
            var flask = Flask(0);
            var tool = pumping ? Carry("HandPump") : flask;
            Assert.False(CellMenu(42, 18).Any(a => a.Command == Harvest));
            Assert.False(Act(deposit, Harvest));
            Assert.AreEqual(12, PoolUnits(42, 18));
            Assert.False(harvest.Harvested);
            Assert.Zero(Count("SilverSand"));

            string drain = pumping
                ? Choice(tool, "PumpLiquid|" + source.ID + "|", row => row.Display.Contains("to 41,17"))
                : Choice(flask, "FillLiquidVessel|" + Id(source) + "|");
            Assert.True(Act(tool, drain));
            Assert.Null(Zone.GetEntityCell(source));
            Assert.False(Zone.TileState.HasCoating(42, 18, liquid));
            Assert.AreEqual(pumping ? 12 : 0, PoolUnits(41, 17));
            Assert.AreEqual(pumping ? 0 : 12, Units(flask));
            Assert.That(Pack.Objects, Does.Contain(tool));
            var dryRow = CellMenu(42, 18).Single(a => a.Command == Harvest);

            if (pumping)
            {
                var outletPool = Zone.GetCell(41, 17).Occupants.Single(e => e.HasPart<LiquidPoolPart>());
                Assert.True(Act(flask, Choice(flask, "FillLiquidVessel|" + Id(outletPool) + "|")));
            }
            Assert.True(Act(flask, Pour(flask, false, 42, 18)));
            Assert.Zero(Units(flask));
            Assert.Zero(PoolUnits(41, 17));
            Assert.AreEqual(12, PoolUnits(42, 18));
            Assert.False(Act(deposit, dryRow.Command));
            Assert.False(harvest.Harvested);
            Assert.False(CellMenu(42, 18).Any(a => a.Command == Harvest));

            var refill = inlet.Occupants.Single(e => e.HasPart<LiquidPoolPart>());
            Assert.True(Act(flask, Choice(flask, "FillLiquidVessel|" + Id(refill) + "|")));
            var interrupted = new InventoryTransaction();
            var attempt = GameEvent.New("InventoryAction");
            attempt.SetParameter("Actor", Actor);
            attempt.SetParameter("Zone", Zone);
            attempt.SetParameter("Command", Harvest);
            attempt.SetParameter("InventoryTransaction", interrupted);
            Assert.False(deposit.FireEventAndRelease(attempt));
            Zone.TileState.WriteCoating(42, 18, liquid, 3);
            Assert.Throws<InvalidOperationException>(() => interrupted.Commit());
            interrupted.Rollback();
            Assert.False(harvest.Harvested);
            Assert.Zero(Count("SilverSand"));
            Assert.True(Zone.TileState.RemoveCoating(42, 18, liquid));
            Assert.True(ChooseRow(CellMenu(42, 18).Single(a => a.Command == Harvest), 42, 18));
            Assert.That(Pack.Objects, Does.Contain(tool));
            Assert.AreEqual(1, Count("SilverSand"));
            Assert.True(harvest.Harvested);
            Assert.AreSame(inlet, Zone.GetEntityCell(deposit));
            Assert.False(Act(deposit, Harvest));

            Assert.True(Act(flask, Pour(flask, false, 42, 18)));
            Assert.AreEqual(12, PoolUnits(42, 18));
            Assert.True(harvest.Harvested);
            Assert.False(CellMenu(42, 18).Any(a => a.Command == Harvest));
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("sill-settling", "spent", manager, Clock, Actor));
            var loadedZone = loaded.ZoneManager.GetZone(Sill);
            loaded.ZoneManager.UnloadZone(Sill);
            Assert.AreSame(loadedZone, loaded.ZoneManager.GetZone(Sill), "A settled deposit must survive ordinary zone eviction pressure.");
            var oldInlet = loadedZone.GetCell(42, 18);
            var loadedDeposit = oldInlet.Occupants.Single(e => e.ID == deposit.ID);
            Assert.True(loadedDeposit.GetPart<HarvestablePart>().Harvested);
            Assert.AreEqual(12, oldInlet.Occupants.Single(e => e.HasPart<LiquidPoolPart>()).GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(1, loaded.Player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "SilverSand")
                .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
        }

        [TestCase(Harvest, "SilverSand")]
        [TestCase(GatherGravel, Gravel)]
        public void EitherNativeChoiceSpendsTheSameDepositAcrossSaveAndReload(string command, string product)
        {
            var manager = EnterSill();
            var deposit = Pocket;
            var stock = deposit.GetPart<HarvestablePart>();
            Assert.That(HarvestRows(), Is.Empty);
            DrainPocket(Flask(0));

            var rows = HarvestRows();
            Assert.That(rows.Select(row => row.Command), Is.EquivalentTo(new[] { Harvest, GatherGravel }));
            var gravel = rows.Single(row => row.Command == GatherGravel);
            Assert.That(gravel.Name, Is.EqualTo(GatherGravel));
            Assert.That(gravel.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.False(gravel.IsDisabled);
            Assert.True(ChooseRow(rows.Single(row => row.Command == command), InletX, InletY));
            Assert.True(stock.Harvested);
            Assert.That(stock.YieldBlueprint, Is.EqualTo("SilverSand"));
            Assert.That(Zone.GetEntityCell(deposit), Is.SameAs(Inlet));
            Assert.That(HarvestRows(), Is.Empty);
            Assert.False(Act(deposit, Harvest));
            Assert.False(Act(deposit, GatherGravel));
            Assert.That(Count("SilverSand"), Is.EqualTo(product == "SilverSand" ? 1 : 0));
            Assert.That(Count(Gravel), Is.EqualTo(product == Gravel ? 1 : 0));
            Assert.That(Count("CoarseGrit"), Is.Zero);

            var restored = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("sill-settling", "choice", manager, Clock, Actor));
            var restoredZone = restored.ZoneManager.GetZone(Sill);
            var restoredDeposit = restoredZone.GetCell(InletX, InletY).Occupants.Single(e => e.ID == deposit.ID);
            Assert.True(restoredDeposit.GetPart<HarvestablePart>().Harvested);
            Assert.That(restored.Player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == product)
                .GetPart<StackerPart>()?.StackCount ?? 1, Is.EqualTo(1));
        }

        [Test]
        public void DrainedPocketCanBeReadWithoutSpendingAndRefusesAStaleGravelRow()
        {
            EnterSill();
            var deposit = Pocket;
            var flask = Flask(0);
            DrainPocket(flask);
            var gravel = HarvestRows().Single(row => row.Command == GatherGravel);
            int tick = Clock.TickCount;
            var messages = MessageLog.GetMessages();
            var contents = Pack.Objects.ToArray();
            string surface = Zone.TileState.ToSaveString();

            var read = ExamineDocumentBuilder.BuildWorld(deposit, Zone, Inlet, Actor);
            Assert.NotNull(read);
            Assert.That(read.ToPlainText(), Does.Contain("silver sand").IgnoreCase);
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
            Assert.That(MessageLog.GetMessages(), Is.EqualTo(messages));
            Assert.That(Pack.Objects, Is.EqualTo(contents));
            Assert.That(Zone.TileState.ToSaveString(), Is.EqualTo(surface));

            Inlet.IsVisible = false;
            Assert.That(ExamineDocumentBuilder.BuildWorld(deposit, Zone, Inlet, Actor), Is.Null);
            Assert.That(HarvestRows(), Is.Empty);
            Assert.False(ChooseRow(gravel, InletX, InletY));
            Inlet.IsVisible = true;
            Assert.True(Zone.MoveEntity(Actor, 40, 11));
            Assert.That(HarvestRows(), Is.Empty);
            Assert.False(ChooseRow(gravel, InletX, InletY));
            Assert.True(Zone.MoveEntity(Actor, InletX, InletY - 1));
            Assert.True(Act(flask, Pour(flask, false, InletX, InletY)));
            Assert.That(HarvestRows(), Is.Empty);
            Assert.False(Act(deposit, gravel.Command));
            Assert.That(PoolUnits(InletX, InletY), Is.EqualTo(12));
            Assert.False(deposit.GetPart<HarvestablePart>().Harvested);
            Assert.That(Count(Gravel) + Count("SilverSand"), Is.Zero);

            DrainPocket(flask);
            Assert.True(ChooseRow(HarvestRows().Single(row => row.Command == GatherGravel), InletX, InletY));
            Assert.That(Count(Gravel), Is.EqualTo(1));
        }

        [Test]
        public void GravelRollsBackIfRecoatedBeforeCommitAndNotifiesOnlyAfterSuccess()
        {
            EnterSill();
            var deposit = Pocket;
            var stock = deposit.GetPart<HarvestablePart>();
            DrainPocket(Flask(0));
            var gravel = HarvestRows().Single(row => row.Command == GatherGravel);

            var interrupted = new InventoryTransaction();
            try
            {
                StageGravel(deposit, interrupted);
                Assert.True(stock.Harvested);
                Assert.That(Count(Gravel), Is.EqualTo(1));
                Zone.TileState.WriteCoating(InletX, InletY, "water", 3);
                Assert.Throws<InvalidOperationException>(() => interrupted.Commit());
            }
            finally { interrupted.Rollback(); }

            Assert.False(stock.Harvested);
            Assert.That(Count(Gravel), Is.Zero);
            Assert.True(Zone.TileState.HasCoating(InletX, InletY, "water"));
            Assert.True(Zone.TileState.RemoveCoating(InletX, InletY, "water"));

            int notifications = 0;
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
            {
                if (e.ID != "AfterInventoryAction" || e.GetStringParameter("Command") != GatherGravel) return;
                notifications++;
                Zone.TileState.WriteCoating(InletX, InletY, "water", 3);
                throw new InvalidOperationException("Gravel receipt observer");
            } });
            Assert.True(ChooseRow(gravel, InletX, InletY));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.True(stock.Harvested);
            Assert.That(Count(Gravel), Is.EqualTo(1));
            Assert.True(Zone.TileState.HasCoating(InletX, InletY, "water"));
        }

        [Test]
        public void ASubstitutedBlueprintManifestKeepsOnlyTheHandHarvestAvailable()
        {
            EnterSill();
            var deposit = Pocket;
            DrainPocket(Flask(0));
            Assert.That(HarvestRows().Any(row => row.Command == GatherGravel), Is.True);

            Factory.Blueprints[Gravel].Parts["SiftableStock"]["Contents"] = ClayAndGrit;
            Assert.That(HarvestRows().Select(row => row.Command), Is.EqualTo(new[] { Harvest }));
            Assert.False(Act(deposit, GatherGravel));
            Assert.False(deposit.GetPart<HarvestablePart>().Harvested);
            Assert.That(Count(Gravel), Is.Zero);
            Assert.True(ChooseRow(HarvestRows().Single(), InletX, InletY));
            Assert.That(Count("SilverSand"), Is.EqualTo(1));
        }

        [Test]
        public void ASubstitutedPackedManifestRejectsTheStagedGravelHarvest()
        {
            EnterSill();
            var deposit = Pocket;
            DrainPocket(Flask(0));

            var interrupted = new InventoryTransaction();
            try
            {
                StageGravel(deposit, interrupted);
                var packet = Pack.Objects.Single(item => item.BlueprintName == Gravel);
                packet.GetPart<SiftableStockPart>().Contents = ClayAndGrit;
                Assert.Throws<InvalidOperationException>(() => interrupted.Commit());
            }
            finally { interrupted.Rollback(); }

            Assert.False(deposit.GetPart<HarvestablePart>().Harvested);
            Assert.That(Count(Gravel), Is.Zero);
            Assert.True(ChooseRow(HarvestRows().Single(row => row.Command == GatherGravel), InletX, InletY));
            Assert.That(Count(Gravel), Is.EqualTo(1));
        }

        [Test]
        public void GravelPacketScreensWithTheOrdinaryHandSieveAndKeepsBothFractions()
        {
            EnterSill();
            DrainPocket(Flask(0));
            Assert.True(ChooseRow(HarvestRows().Single(row => row.Command == GatherGravel), InletX, InletY));
            var packet = Pack.Objects.Single(e => e.BlueprintName == Gravel);
            Assert.NotNull(packet.GetPart<SiftableStockPart>());
            Assert.That(HandlingService.GetWeight(packet), Is.EqualTo(2));

            var oldForge = ForgePart.Factory;
            try
            {
                ForgePart.Factory = Factory;
                MaterialProcessRegistry.ResetForTests();
                var read = ExamineDocumentBuilder.BuildInventory(packet, Actor);
                Assert.NotNull(read);
                Assert.That(read.ToPlainText(), Does.Contain("1 silver sand + 1 coarse grit").IgnoreCase);
                Assert.False(Actions(packet).Any(row => !row.IsDisabled && row.Name == "screen_size_grades"));
                var sieve = Carry("HandSieve");
                int weight = Pack.GetCarriedWeight();
                var screen = Choice(packet, "MaterialProcess|screen_size_grades|");
                Assert.True(Act(packet, screen));
                Assert.That(Count(Gravel), Is.Zero);
                Assert.That(Count("SilverSand"), Is.EqualTo(1));
                Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
                Assert.That(Pack.Objects, Does.Contain(sieve));
                Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(weight));
            }
            finally
            {
                ForgePart.Factory = oldForge;
                MaterialProcessRegistry.ResetForTests();
            }
        }
    }
}
