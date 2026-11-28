using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A single flake deposited on the exposed rim of a natural Brine Lens.</summary>
    public sealed class BrineRimStockPart : Part, IDescribeState, IInventoryActionReceiptOwner
    {
        internal const string Site = "Overworld.17.13.0";
        internal const string Ground = "SaltCrust";
        const string Action = "ScrapeDryBrineCrust|";
        const int X = 40, Y = 14;

        public override string Name => "BrineRimStock";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;
        public bool Spent;

        internal static bool TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (!Supported(zone, manager) || manager.CachedZones.ContainsKey(Site)
                || BrineDistillation.QualifiedCrust(manager.Factory) == null
                || zone.GetReadOnlyEntitiesWithPart<BrineRimStockPart>().Count != 0) return false;

            var rim = zone.GetCell(X, Y);
            var approach = zone.GetCell(X, Y - 1);
            var landing = zone.GetCell(X, Y - 2);
            var poolCell = zone.GetCell(X, Y + 1);
            if (!Open(zone, rim) || !Open(zone, approach) || !Open(zone, landing)
                || !BareSand(approach) || !BareSand(landing) || rim.Objects.Count != 2) return false;

            var crust = TopCrust(zone, rim);
            var pool = poolCell.Objects.LastOrDefault(item => item.BlueprintName == "BrinePool");
            if (!FixedTerrain(zone, crust) || rim.Objects.Count(item => item.BlueprintName == "Sand") != 1
                || !FixedTerrain(zone, pool) || pool.GetPart<LiquidPoolPart>() is not { Volume: 90, LiquidId: "brine" })
                return false;

            crust.AddPart(new BrineRimStockPart());
            return true;
        }

        static bool Supported(Zone zone, OverworldZoneManager manager)
            => zone?.ZoneID == Site && manager?.WorldMap != null
                && manager.WorldMap.GetBiome(17, 13) == BiomeType.Beating && manager.WorldMap.GetPOI(17, 13) == null;

        static bool Open(Zone zone, Cell cell, Entity actor = null)
            => cell is { IsInterior: false } && !cell.BlocksMovement(actor) && !PlacementCheck.Hazard(zone, cell, actor);

        static bool BareSand(Cell cell) => cell.Objects.Count == 1 && cell.Objects[0].BlueprintName == "Sand";

        static bool FixedTerrain(Zone zone, Entity item)
            => WorldResourceActions.Ground(item, zone) && item.HasTag("Terrain") && !item.HasTag("Creature")
                && item.GetPart<RenderPart>() is { Visible: true }
                && item.GetPart<PhysicsPart>() is { Solid: false, Takeable: false };

        static Entity TopCrust(Zone zone, Cell cell)
        {
            var target = WorldInteractionSystem.ResolveTarget(cell);
            return target?.BlueprintName == Ground && zone.GetEntityCell(target) == cell ? target : null;
        }

        bool CurrentSite(Zone zone, out Cell cell)
        {
            cell = null;
            var manager = WorldLocationContext.For(zone);
            if (!Supported(zone, manager) || manager.ActiveZone != zone || TurnManager.Active == null
                || !manager.CachedZones.TryGetValue(Site, out var current) || current != zone
                || !FixedTerrain(zone, ParentEntity) || ParentEntity.GetPart<BrineRimStockPart>() != this
                || ParentEntity.Parts.Count(part => part is BrineRimStockPart) != 1
                || zone.GetReadOnlyEntitiesWithPart<BrineRimStockPart>().Count != 1) return false;
            cell = zone.GetEntityCell(ParentEntity);
            return cell is { IsInterior: false } && cell == zone.GetCell(X, Y);
        }

        enum RimState { Absent, Wet, Dry, Spent }

        RimState State(Zone zone)
        {
            if (!CurrentSite(zone, out var cell)) return RimState.Absent;
            if (Spent) return RimState.Spent;
            var weather = RegionalWeather.Current(zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion);
            if (!weather.Valid) return RimState.Absent;
            return Dry(zone, cell, weather) ? RimState.Dry : RimState.Wet;
        }

        static bool Dry(Zone zone, Cell cell, RegionalWeather.Observation weather)
            => weather.Valid && !weather.Raining && Open(zone, cell);

        public bool IsWorkable(Zone zone) => State(zone) == RimState.Dry;

        internal (string Glyph, string Color) Appearance(Zone zone) => State(zone) switch
        {
            RimState.Wet => (":", "&y"),
            RimState.Dry => ("*", "&W"),
            RimState.Spent => ("-", "&w"),
            _ => default
        };

        public string DescribeState(DescribeContext context) => State(context.Zone ?? ParentEntity?.SpatialZone) switch
        {
            RimState.Wet => "A dark streak follows the rim. Fair weather and dry footing will expose one loose brine crust.",
            RimState.Dry => "One white flake glints on the dry rim. Stand beside it with a sound scraper in your pack to take it.",
            RimState.Spent => "A scrape mark remains on the rim. The loose flake is gone.",
            _ => null
        };

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<BrineRimStockPart>() == this
            && command?.StartsWith(Action, StringComparison.Ordinal) == true;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                var zone = PreparationActions.ZoneFor(e, actor);
                RouteWorkMenu.Rows(ParentEntity, actor, zone, e.GetParameter<InventoryActionList>("Actions"), Choices(actor, zone), true);
            }
            if (e.ID == "InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")) && RouteWorkMenu.Act(ParentEntity, e))
            {
                e.Handled = true;
                return false;
            }
            return true;
        }

        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor, Zone zone)
        {
            if (!IsWorkable(zone) || !RouteWorkMenu.ActorReady(actor, zone)) yield break;
            var owner = ParentEntity;
            var cell = zone.GetEntityCell(owner);
            var origin = zone.GetEntityCell(actor);
            var pack = actor.GetPart<InventoryPart>();
            var factory = WorldLocationContext.For(zone).Factory;
            var clock = TurnManager.Active;
            int tick = clock.TickCount;
            var blueprint = BrineDistillation.QualifiedCrust(factory);
            var tool = pack?.Objects.FirstOrDefault(SoundScraper);
            if (blueprint == null || tool == null || !InReach()) yield break;

            var sameOwner = RouteWorkMenu.Identity(owner);
            var sameTool = RouteWorkMenu.Identity(tool);
            string key = Action + owner.ID;
            InventoryUnitOutputReceipt output = null;

            bool SoundScraper(Entity item) => item?.GetPart<ScraperPart>() is { } scraper && scraper.ParentEntity == item
                && item.Parts.Count(part => part is ScraperPart) == 1 && MaterialScreeningWorkReceipt.IsSoundCarriedTool(actor, owner, item, zone);
            bool InReach() => RouteWorkMenu.Visible(cell) && zone.GetEntityCell(actor) == origin && Open(zone, origin, actor)
                && Math.Abs(origin.X - cell.X) + Math.Abs(origin.Y - cell.Y) == 1;
            bool Context() => TurnManager.Active == clock && clock.TickCount == tick && CurrentSite(zone, out var at) && at == cell
                && actor.GetPart<InventoryPart>() == pack && sameOwner() && sameTool() && SoundScraper(tool) && InReach()
                && Dry(zone, cell, RegionalWeather.Current(zone, tick, RegionalWeather.PolicyVersion))
                && BrineDistillation.QualifiedCrust(factory) == blueprint;
            bool Before() => Context() && !Spent;
            bool After() => Context() && Spent && output?.Current() == true && BrineDistillation.CrustPayload(output.Recipient);

            yield return new RouteWorkMenu.Entry
            {
                Actor = actor, Zone = zone, Cell = cell, Claims = new[] { tool }, Key = key,
                Label = "Scrape dry brine crust", Hint = "One action for one flake from the rim. Leaves the pool untouched.",
                Before = Before, After = After,
                Apply = tx =>
                {
                    var crust = factory.CreateEntity(BrineDistillation.CrustBlueprint);
                    if (!BrineDistillation.CanonicalCrust(crust) || crust.SpatialZone != null
                        || pack.Objects.Concat(pack.EquippedItems.Values).Any(item => item.ID == crust.ID)
                        || zone.GetReadOnlyEntities().Any(item => item.ID == crust.ID) || !Before()) return false;

                    tx.Do(() => Spent = true, () =>
                    {
                        if (ParentEntity == owner && owner.GetPart<BrineRimStockPart>() == this
                            && output is not { RetractedUnit: false }) Spent = false;
                    });
                    return InventoryUnitOutputReceipt.TryPlace(pack, actor, crust, tx, key, out output);
                },
                Publish = () =>
                {
                    Noise.Mark(actor);
                    ZoneRenderHooks.MarkCellDirty(cell, "Dry brine rim scraped");
                    MessageLog.Add("You scrape one brine crust from the dry rim.");
                }
            };
        }
    }
}
