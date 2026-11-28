using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    public sealed class SillSettlingDepositPart : HarvestablePart, IDescribeState, IPostCommitInventoryActionNotification
    {
        public override string Name => "SillSettlingDeposit";
        public Entity Ground;
        public string WorldKey = "";
        public int SiteX = -1, SiteY = -1;
        const string GatherGravel = "GatherSillBankGravel";
        const string Gravel = "SillBankGravel";
        const string GravelContents = "SilverSand:1:0;CoarseGrit:1:2";

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;
        internal override bool RetainsFixedSource => true;

        enum DepositState { Unavailable, Covered, Exposed, Spent }

        public bool OwnsInventoryReceipt(string command)
            => (command == "Harvest" || command == GatherGravel) && ParentEntity?.GetPart<HarvestablePart>() == this;

        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);

        internal override string AlternateFixedYield(string choice)
        {
            if (choice != GatherGravel || Factory?.Blueprints == null || !Factory.Blueprints.TryGetValue(Gravel, out var packet)
                || packet?.Parts == null || !packet.Parts.TryGetValue("SiftableStock", out var mixture)
                || mixture == null || !mixture.TryGetValue(nameof(SiftableStockPart.Contents), out var contents)
                || contents != GravelContents || SiftableStockPart.Weight(Factory, Gravel) != 2) return null;
            return Gravel;
        }

        bool OffersGravel() => AlternateFixedYield(GatherGravel) != null
            && MaterialProcessTemplate.CaptureFixedHarvest(Factory, Gravel) != null;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (!Harvested && CanGatherFixedSource(actor, zone) && OffersGravel())
                    e.GetParameter<InventoryActionList>("Actions")?.AddActionWithHint(GatherGravel,
                        "gather the whole bank gravel (2 lb)", GatherGravel,
                        "The same deposit as the sand. A hand sieve separates one silver sand and one coarse grit.",
                        '\0', 19, false, ActionTime.OneAction);
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != GatherGravel)
                return base.HandleEvent(e);
            var harvester = e.GetParameter<Entity>("Actor");
            if (!DoHarvest(harvester, e.GetParameter<Zone>("Zone") ?? harvester?.SpatialZone,
                e.GetParameter<InventoryTransaction>("InventoryTransaction"), fixedChoice: GatherGravel)) return true;
            e.Handled = true;
            return false;
        }

        DepositState State(Zone zone, bool showSpent = true)
        {
            var owner = ParentEntity;
            var manager = WorldLocationContext.For(zone);
            if (manager?.ActiveZone != zone || !SillSettlingPocket.Supported(manager, zone)
                || manager.Exploration.WorldKey != WorldKey || owner?.BlueprintName != SillSettlingPocket.Deposit
                || owner.GetPart<HarvestablePart>() != this || owner.Parts.Count(p => p is HarvestablePart) != 1
                || YieldBlueprint != "SilverSand" || YieldMin != 1 || YieldMax != 1 || YieldChance != 100
                || !RainCatchmentPart.FixedOn(owner, Ground, zone) || zone.GetEntityCell(owner) != zone.GetCell(SiteX, SiteY)
                || zone.GetReadOnlyEntitiesWithPart<SillSettlingDepositPart>().Count != 1) return DepositState.Unavailable;

            if (showSpent && Harvested) return DepositState.Spent;
            var cell = zone.GetCell(SiteX, SiteY);
            return cell.Occupants.Any(item => item.HasPart<LiquidPoolPart>())
                || zone.TileState.Get(SiteX, SiteY)?.Coatings.Any(coating => coating.Turns > 0) == true
                ? DepositState.Covered : DepositState.Exposed;
        }

        public string DescribeState(DescribeContext context) => State(context.Zone ?? ParentEntity?.SpatialZone) switch
        {
            DepositState.Covered => "A pale streak lies beneath the surface. Clear the shallow pocket to collect the sand.",
            DepositState.Exposed => OffersGravel()
                ? "The water has cleared. Fine silver sand lies between the coarse stones. Pick the sand or take the whole loose gravel to sieve."
                : "The water has cleared. One measure of silver sand lies exposed.",
            DepositState.Spent => "A short scrape marks where the silver sand was gathered.",
            _ => null
        };

        internal (string Glyph, string Color) Appearance(Zone zone) => State(zone) switch
        {
            DepositState.Covered => ("~", "&c"),
            DepositState.Exposed => (":", "&W"),
            DepositState.Spent => ("_", "&w"),
            _ => (null, null)
        };

        internal override bool CanGatherFixedSource(Entity actor, Zone zone)
            => State(zone, showSpent: false) == DepositState.Exposed && SeasonalHarvestPart.ActorCanGather(actor, zone)
                && RouteWorkMenu.Visible(zone.GetEntityCell(ParentEntity)) && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;

        internal override Func<bool> CaptureFixedGathering(Entity actor, Zone zone, Cell cell)
        {
            var owner = ParentEntity;
            var ground = Ground;
            var actorCell = zone.GetEntityCell(actor);
            var original = RouteWorkMenu.Identity(owner);
            return () => Harvested && ParentEntity == owner && original() && Ground == ground
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(owner) == cell
                && CanGatherFixedSource(actor, zone);
        }
    }

    internal static class SillSettlingPocket
    {
        internal const string Deposit = "SillSettlingDeposit";
        const string Sill = "Overworld.10.10.0";
        const int InitialWater = 12;

        internal static bool Supported(OverworldZoneManager manager, Zone zone)
            => zone?.ZoneID == Sill && manager?.Exploration is { Enabled: true, Version: >= 31 }
                && !string.IsNullOrEmpty(manager.Exploration.WorldKey) && SillCourtComposition.MatchesSite(manager, Sill);

        internal static bool Retain(OverworldZoneManager manager, string id)
            => id == Sill && manager.CachedZones.TryGetValue(id, out var zone)
                && zone.GetReadOnlyEntitiesWithPart<SillSettlingDepositPart>().Count > 0;

        internal static void Install(OverworldZoneManager manager, Zone zone)
        {
            var factory = manager.Factory;
            if (!Supported(manager, zone) || factory == null || manager.CachedZones.ContainsKey(Sill)
                || zone.GetReadOnlyEntitiesWithPart<SillSettlingDepositPart>().Count != 0
                || !factory.Blueprints.ContainsKey(Deposit) || !HarvestablePart.CanCreateYield(factory, "SilverSand")
                || MaterialProcessTemplate.Capture(factory, "SilverSand", true) == null) return;
            var poolTemplate = MaterialProcessTemplate.Capture(factory, LiquidVesselService.PoolBlueprint, true);
            if (poolTemplate == null) return;

            var reached = ReachableDryBank(zone);
            var stand = (from sy in Enumerable.Range(16, 3)
                         from sx in Enumerable.Range(37, 11)
                         where reached.Contains((sx, sy)) && Bare(zone, sx, sy) && Bare(zone, sx, sy + 1) && Bare(zone, sx - 1, sy)
                         orderby Math.Abs(sx - 42) + Math.Abs(sy - 17), sy, sx
                         select zone.GetCell(sx, sy)).FirstOrDefault();
            if (stand == null) return;
            int x = stand.X, y = stand.Y + 1;
            var ground = zone.GetCell(x, y).Objects[0];
            var before = zone.GetReadOnlyEntities().ToArray();
            var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, before);
            var deposit = factory.CreateEntity(Deposit);
            var pool = factory.CreateEntity(LiquidVesselService.PoolBlueprint);
            if (deposit?.BlueprintName != Deposit || deposit.GetPart<SillSettlingDepositPart>() is not
                    { Harvested: false, YieldBlueprint: "SilverSand", YieldMin: 1, YieldMax: 1, YieldChance: 100 } source
                || source.ParentEntity != deposit || deposit.Parts.Count(part => part is HarvestablePart) != 1
                || deposit.GetPart<PhysicsPart>() is not { Takeable: false, Solid: false, InInventory: null, Equipped: null }
                || !deposit.HasTag("Terrain") || SecondExplorationActions.Special(deposit)
                || pool?.GetPart<LiquidPoolPart>() is not { Volume: 0, LiquidId: "" } water
                || pool.GetPart<PhysicsPart>() is not { Takeable: false, Solid: false, InInventory: null, Equipped: null }
                || !pool.HasTag("Terrain") || !poolTemplate.Matches(pool)
                || deposit.SpatialZone != null || pool.SpatialZone != null || deposit.ID == pool.ID
                || string.IsNullOrEmpty(deposit.ID) || string.IsNullOrEmpty(pool.ID)
                || before.Any(e => e.ID == deposit.ID || e.ID == pool.ID) || !unchanged() || zone.EntityCount != before.Length
                || !Bare(zone, x, y) || !Bare(zone, stand.X, stand.Y) || !Bare(zone, stand.X - 1, stand.Y)
                || zone.GetCell(x, y).Objects[0] != ground) return;

            source.Ground = ground;
            source.WorldKey = manager.Exploration.WorldKey;
            source.SiteX = x;
            source.SiteY = y;
            water.LiquidId = "water";
            water.Volume = InitialWater;
            water.Initialize();
            if (!zone.AddEntity(deposit, x, y)) return;
            if (!zone.AddEntity(pool, x, y)) zone.RemoveEntity(deposit);
        }

        static bool DryPath(Cell cell)
            => cell != null && !cell.IsInterior && !cell.BlocksMovement()
                && !cell.Occupants.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GreatdewSnarePart>());

        static bool Bare(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return DryPath(cell) && !zone.GenReservedCells.Contains((x, y)) && !zone.TileState.Has(x, y)
                && cell.Objects.Count == 1 && cell.Objects[0].BlueprintName == "Grass" && cell.Occupants.Count() == 1;
        }

        static HashSet<(int, int)> ReachableDryBank(Zone zone)
        {
            var reached = new HashSet<(int, int)>();
            if (!DryPath(zone.GetCell(40, 11))) return reached;
            var pending = new Queue<(int X, int Y)>();
            var directions = new[] { (X: 1, Y: 0), (X: -1, Y: 0), (X: 0, Y: 1), (X: 0, Y: -1) };
            reached.Add((40, 11));
            pending.Enqueue((40, 11));
            while (pending.Count > 0)
            {
                var cell = pending.Dequeue();
                foreach (var direction in directions)
                {
                    var candidate = (cell.X + direction.X, cell.Y + direction.Y);
                    if (DryPath(zone.GetCell(candidate.Item1, candidate.Item2)) && reached.Add(candidate)) pending.Enqueue(candidate);
                }
            }
            return reached;
        }
    }
}
