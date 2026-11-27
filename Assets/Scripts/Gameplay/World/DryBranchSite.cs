using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>One finite length of public fallen wood, governed by the saved local weather.</summary>
    public sealed class DryBranchStockPart : HarvestablePart, IDescribeState, IPostCommitInventoryActionNotification
    {
        public override string Name => "DryBranchStock";
        public Entity Ground;
        public string WorldKey = "";
        public int PolicyVersion = RegionalWeather.PolicyVersion;

        private enum StockState { Unavailable, Covered, Workable, Spent }

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;

        public bool OwnsInventoryReceipt(string command)
            => command == "Harvest" && ParentEntity?.GetPart<HarvestablePart>() == this;
        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);

        private StockState State(Zone zone, bool showSpent = true)
        {
            var owner = ParentEntity;
            var manager = WorldLocationContext.For(zone);
            if (zone?.ZoneID != GleanersDistrict.SurfaceID || TurnManager.Active == null
                || manager?.ActiveZone != zone || string.IsNullOrEmpty(WorldKey)
                || manager.Exploration?.WorldKey != WorldKey || owner?.BlueprintName != DryBranchSite.Blueprint
                || owner.GetPart<HarvestablePart>() != this || owner.Parts.Count(p => p is HarvestablePart) != 1
                || YieldBlueprint != DryBranchSite.Product || YieldMin != 1 || YieldMax != 1 || YieldChance != 100
                || !RainCatchmentPart.FixedOn(owner, Ground, zone)
                || zone.GetEntityCell(owner) != zone.GetCell(DryBranchSite.X, DryBranchSite.Y)
                || zone.GetReadOnlyEntitiesWithPart<DryBranchStockPart>().Count() != 1)
                return StockState.Unavailable;

            var weather = RegionalWeather.Current(zone, WorldClock.CurrentTick, PolicyVersion);
            if (!weather.Valid) return StockState.Unavailable;
            if (showSpent && Harvested) return StockState.Spent;
            return weather.Season == 0 && !weather.Raining && FairWeatherDryingPart.DryFooting(zone.GetEntityCell(owner), zone)
                ? StockState.Workable : StockState.Covered;
        }

        internal (string Glyph, string Color) Appearance(Zone zone) => State(zone) switch
        {
            StockState.Covered => ("_", "&y"),
            StockState.Workable => ("=", "&Y"),
            StockState.Spent => ("-", "&y"),
            _ => (null, null)
        };

        public string DescribeState(DescribeContext context) => State(context.Zone ?? ParentEntity?.SpatialZone) switch
        {
            StockState.Covered => "The fallen branch needs dry ground and fair weather early in the growing cycle.",
            StockState.Workable => "One dry length protrudes from the fallen branch. Use a sound wood-cutting tool to take one salvaged timber.",
            StockState.Spent => "A trimmed end marks where the usable length was cut.",
            _ => null
        };

        internal override bool RetainsFixedSource => true;

        private bool InReach(Entity actor, Zone zone) => State(zone, showSpent: false) == StockState.Workable
            && SeasonalHarvestPart.ActorCanGather(actor, zone) && RouteWorkMenu.Visible(zone.GetEntityCell(ParentEntity))
            && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;

        internal override bool CanGatherFixedSource(Entity actor, Zone zone)
            => InReach(actor, zone) && MaterialCuttingWorkReceipt.Resolve(actor, ParentEntity, zone, woodStock: true) != null;

        internal override bool BeginFixedGathering(Entity actor, Zone zone, InventoryTransaction transaction)
            => MaterialCuttingWorkReceipt.Resolve(actor, ParentEntity, zone, woodStock: true)?.Begin(transaction, "Harvest") == true;

        internal override Func<bool> CaptureFixedGathering(Entity actor, Zone zone, Cell cell)
        {
            var owner = ParentEntity;
            var ground = Ground;
            var actorCell = zone.GetEntityCell(actor);
            string sourceId = owner.ID, worldKey = WorldKey;
            var clock = TurnManager.Active;
            int tick = WorldClock.CurrentTick, policy = PolicyVersion;
            return () => Harvested && ParentEntity == owner && owner.ID == sourceId && Ground == ground
                && WorldKey == worldKey && PolicyVersion == policy && TurnManager.Active == clock
                && WorldClock.CurrentTick == tick && zone.GetEntityCell(owner) == cell
                && zone.GetEntityCell(actor) == actorCell && InReach(actor, zone);
        }
    }

    internal static class DryBranchSite
    {
        internal const string Blueprint = "GleanersDryBranch";
        internal const string Product = "SalvagedTimber";
        internal const int X = 43, Y = 8;

        internal static void TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (zone?.ZoneID != GleanersDistrict.SurfaceID || manager?.Exploration is not { Enabled: true } exploration
                || string.IsNullOrEmpty(exploration.WorldKey) || manager.CachedZones.ContainsKey(zone.ZoneID)
                || manager.Factory is not { } factory || !factory.Blueprints.ContainsKey(Blueprint)
                || MaterialProcessTemplate.Capture(factory, Product, true) == null)
                return;

            var site = zone.GetCell(X, Y);
            var approach = zone.GetCell(X, Y + 1);
            var standingRoute = new[] { (40, 12), (41, 12), (42, 12), (42, 11), (42, 10), (43, 10), (43, 9) };
            bool Available() => RepairCultivationSite.Bare(site) && !zone.GenReservedCells.Contains((X, Y))
                && !zone.TileState.Has(X, Y) && approach is { IsInterior: false }
                && standingRoute.All(at => zone.GetCell(at.Item1, at.Item2)?.BlocksMovement() == false)
                && !zone.GetReadOnlyEntitiesWithPart<DryBranchStockPart>().Any();
            if (!Available()) return;

            var before = zone.GetReadOnlyEntities().ToHashSet();
            var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, before);
            var ground = site.Objects[0];
            Entity source = null;
            bool installed = false;
            try
            {
                source = factory.CreateEntity(Blueprint);
                if (source?.GetPart<DryBranchStockPart>() is not { Harvested: false } stock
                    || source.BlueprintName != Blueprint || source.SpatialZone != null || string.IsNullOrEmpty(source.ID)
                    || source.GetPart<PhysicsPart>() is not { Solid: false, Takeable: false, InInventory: null, Equipped: null }
                    || source.GetPart<RenderPart>() is not { Visible: true } || source.Parts.Any(p => p.ParentEntity != source)
                    || source.Parts.Count(p => p is HarvestablePart) != 1
                    || stock.YieldBlueprint != Product || stock.YieldMin != 1 || stock.YieldMax != 1 || stock.YieldChance != 100
                    || before.Any(e => e.ID == source.ID) || !unchanged() || !before.SetEquals(zone.GetReadOnlyEntities())
                    || !Available() || site.Objects[0] != ground)
                    return;

                stock.Ground = ground;
                stock.WorldKey = exploration.WorldKey;
                installed = zone.AddEntity(source, X, Y) && unchanged()
                    && RainCatchmentPart.FixedOn(source, ground, zone)
                    && before.Append(source).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
            }
            catch (Exception error)
            {
                CavesOfOoo.Diagnostics.Diag.Record("worldgen", "DryBranchSourceRefused", target: source,
                    payload: new { error = error.GetType().Name });
            }
            finally
            {
                if (!installed && source?.SpatialZone == zone)
                    zone.RemoveEntity(source);
            }
        }
    }
}
