using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>One fixed reserve. The same local state governs its appearance and access.</summary>
    public sealed class RainSeamHarvestPart : HarvestablePart, IDescribeState, IPostCommitInventoryActionNotification
    {
        public override string Name => "RainSeamHarvest";
        public Entity Ground;
        public string WorldKey = "";
        public int SiteX = -1, SiteY = -1;
        public int PolicyVersion = RegionalWeather.PolicyVersion;
        internal const int GraceTicks = 150;
        private const float Dampness = .25f;

        private enum SeamState { Unavailable, Firm, Loose, Settling, Spent }

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;

        public bool OwnsInventoryReceipt(string command) => command == "Harvest"
            && ParentEntity?.GetPart<HarvestablePart>() == this;

        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);

        private SeamState State(Zone zone, bool showSpent = true)
        {
            var owner = ParentEntity;
            var manager = WorldLocationContext.For(zone);
            if (zone?.ZoneID != RepairCultivationSite.ZoneID || TurnManager.Active == null
                || manager?.ActiveZone != zone || string.IsNullOrEmpty(WorldKey)
                || manager.Exploration?.WorldKey != WorldKey || owner?.BlueprintName != RainClaySeamSite.Source
                || owner.GetPart<HarvestablePart>() != this || owner.Parts.Count(part => part is HarvestablePart) != 1
                || YieldBlueprint != RainClaySeamSite.Packet || YieldMin != 1 || YieldMax != 1 || YieldChance != 100
                || !RainCatchmentPart.FixedOn(owner, Ground, zone) || zone.GetEntityCell(owner) != zone.GetCell(SiteX, SiteY)
                || zone.GetReadOnlyEntitiesWithPart<RainSeamHarvestPart>().Count != 1)
                return SeamState.Unavailable;

            int tick = WorldClock.CurrentTick;
            var weather = RegionalWeather.Current(zone, tick, PolicyVersion);
            if (!weather.Valid) return SeamState.Unavailable;
            if (showSpent && Harvested) return SeamState.Spent;
            if (weather.Raining) return SeamState.Loose;
            int windowStart = weather.Window * RegionalWeather.WindowTicks;
            return windowStart > 0 && tick - windowStart < GraceTicks
                && RegionalWeather.Current(zone, windowStart - 1, PolicyVersion).Raining
                ? SeamState.Settling : SeamState.Firm;
        }

        internal (string Glyph, string Color) Appearance(Zone zone) => State(zone) switch
        {
            SeamState.Firm => ("_", "&r"),
            SeamState.Loose => ("*", "&R"),
            SeamState.Settling => (":", "&y"),
            SeamState.Spent => ("-", "&r"),
            _ => (null, null)
        };

        public string DescribeState(DescribeContext context) => State(context.Zone ?? ParentEntity?.SpatialZone) switch
        {
            SeamState.Loose => "Rain has slumped the bank. A loose heap of clay and grit lies below it.",
            SeamState.Settling => "The rain has passed. The loose clay is starting to set. There is still one packet to gather.",
            SeamState.Spent => "A shallow scrape marks where the loose clay and grit were lifted.",
            SeamState.Firm => "The clay holds fast in a low seam. A shower can loosen it.",
            _ => null
        };

        internal bool CanGather(Entity actor, Zone zone) => State(zone, showSpent: false) is SeamState.Loose or SeamState.Settling
            && SeasonalHarvestPart.ActorCanGather(actor, zone) && RouteWorkMenu.Visible(zone.GetEntityCell(ParentEntity))
            && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;

        internal Func<bool> CaptureGathering(Entity actor, Zone zone, Cell cell)
        {
            var owner = ParentEntity;
            var ground = Ground;
            var actorCell = zone.GetEntityCell(actor);
            var clock = TurnManager.Active;
            int tick = WorldClock.CurrentTick, policy = PolicyVersion;
            string id = owner.ID, key = WorldKey;
            return () => Harvested && ParentEntity == owner && owner.ID == id && Ground == ground
                && WorldKey == key && PolicyVersion == policy && TurnManager.Active == clock
                && WorldClock.CurrentTick == tick && zone.GetEntityCell(owner) == cell
                && zone.GetEntityCell(actor) == actorCell && CanGather(actor, zone);
        }

        internal Func<bool> PrepareYield(Entity item, Entity actor, Cell sourceCell,
            InventoryTransaction transaction, EntityFactory factory)
        {
            var template = MaterialProcessTemplate.Capture(factory, RainClaySeamSite.Packet);
            var pack = actor.GetPart<InventoryPart>();
            var zone = sourceCell.ParentZone;
            string id = item.ID;
            bool NoAlias() => actor.ID != id && actor.GetPart<InventoryPart>() == pack
                && !pack.Objects.Any(other => other != item && other.ID == id)
                && !pack.EquippedItems.Values.Any(other => other?.ID == id)
                && !zone.GetReadOnlyEntities().Any(other => other != item && other.ID == id);
            if (template?.Matches(item) != true || !PreparationActions.Fresh(item, RainClaySeamSite.Packet)
                || !NoAlias() || !transaction.TryClaim(item, actor, "Harvest")) return null;

            var wet = new WetEffect(Dampness);
            if (!item.ApplyEffect(wet)) return null;
            var effects = item.GetPart<StatusEffectsPart>();
            var manifest = item.GetPart<SiftableStockPart>();
            var physics = item.GetPart<PhysicsPart>();
            var stack = item.GetPart<StackerPart>();
            if (manifest?.Contents != "FireClay:2:0;CoarseGrit:1:2" || physics?.Weight != 3 || stack?.StackCount != 1)
                return null;
            return () => template.MatchesPreparedWetStock(item, effects) && item.ID == id && NoAlias()
                && effects.EffectCount == 1 && wet.Owner == item
                && item.GetEffect<WetEffect>() == wet && wet.Moisture == Dampness && wet.Duration == Effect.DURATION_INDEFINITE
                && item.GetPart<SiftableStockPart>() == manifest && manifest.Contents == "FireClay:2:0;CoarseGrit:1:2"
                && item.GetPart<PhysicsPart>() == physics && physics.Weight == 3 && physics.Takeable && physics.Equipped == null
                && item.GetPart<StackerPart>() == stack && stack.StackCount == 1
                && (actor.GetPart<InventoryPart>() == pack && pack.Objects.Contains(item)
                    && physics.InInventory == actor && item.SpatialZone == null
                    || physics.InInventory == null && sourceCell.ParentZone.GetEntityCell(item) == sourceCell);
        }
    }

    internal static class RainClaySeamSite
    {
        internal const string Source = "RainClaySeam", Packet = "RainClaySpoil";

        internal static void TryInstall(Zone zone, OverworldZoneManager manager)
        {
            var factory = manager.Factory;
            if (zone.ZoneID != RepairCultivationSite.ZoneID || manager.Exploration is not { Version: >= 31 } exploration
                || string.IsNullOrEmpty(exploration.WorldKey) || factory == null || !factory.Blueprints.ContainsKey(Source)
                || MaterialProcessTemplate.Capture(factory, Packet) == null) return;

            var before = zone.GetReadOnlyEntities().ToArray();
            if (before.Any(owner => owner.BlueprintName == Source || owner.HasPart<RainSeamHarvestPart>())) return;
            var banks = before.Where(owner => owner.BlueprintName == "RepairClayBank"
                && owner.GetProperty(RepairCultivationSite.RoleKey) == "clay").Take(2).ToArray();
            if (banks.Length != 1) return;
            var bankCell = zone.GetEntityCell(banks[0]);
            if (bankCell == null) return;

            var reached = RepairCultivationSite.Reach(zone, new HashSet<(int, int)>());
            bool Available(Cell cell) => RepairCultivationSite.Bare(cell) && reached.Contains((cell.X, cell.Y))
                && !zone.GenReservedCells.Contains((cell.X, cell.Y)) && !zone.TileState.Has(cell.X, cell.Y)
                && !before.Any(owner => owner.HasTag("Creature")
                    && zone.GetEntityCell(owner) is Cell other
                    && Math.Max(Math.Abs(other.X - cell.X), Math.Abs(other.Y - cell.Y)) <= 2);
            var target = (from dy in Enumerable.Range(-2, 5)
                          from dx in Enumerable.Range(-2, 5)
                          let cell = zone.GetCell(bankCell.X + dx, bankCell.Y + dy)
                          where Available(cell)
                          orderby Math.Abs(dx) + Math.Abs(dy), cell.Y, cell.X
                          select cell).FirstOrDefault();
            if (target == null) return;

            var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, before);
            var ground = target.Objects[0];
            Entity owner = null;
            bool placed = false, complete = false;
            try
            {
                owner = factory.CreateEntity(Source);
                if (owner?.GetPart<RainSeamHarvestPart>() is not { Harvested: false } seam
                    || owner.GetPart<PhysicsPart>() is not { Takeable: false, Solid: false, InInventory: null, Equipped: null }
                    || owner.SpatialZone != null || string.IsNullOrEmpty(owner.ID) || before.Any(other => other.ID == owner.ID)
                    || !unchanged() || !before.ToHashSet().SetEquals(zone.GetReadOnlyEntities())
                    || !Available(target) || target.Objects[0] != ground) return;

                seam.Ground = ground;
                seam.WorldKey = exploration.WorldKey;
                seam.SiteX = target.X;
                seam.SiteY = target.Y;
                placed = zone.AddEntity(owner, target.X, target.Y);
                complete = placed && unchanged() && RainCatchmentPart.FixedOn(owner, ground, zone)
                    && before.Append(owner).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
            }
            catch (Exception error)
            {
                CavesOfOoo.Diagnostics.Diag.Record("worldgen", "RainClaySeamRefused", target: owner,
                    payload: new { error = error.GetType().Name });
            }
            finally
            {
                if (placed && !complete && owner.SpatialZone == zone) zone.RemoveEntity(owner);
            }
        }
    }
}
