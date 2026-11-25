using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>A fixed, one-use source. Season controls gathering, not the portable product.</summary>
    public sealed class SeasonalHarvestPart : HarvestablePart, IDescribeState, IPostCommitInventoryActionNotification
    {
        public override string Name => "SeasonalHarvest";
        public Entity Ground;
        public string WorldKey = "";
        public int Season = 1;
        public int PolicyVersion = RegionalWeather.PolicyVersion;

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;
        public bool OwnsInventoryReceipt(string command)
            => command == "Harvest" && ParentEntity?.GetPart<HarvestablePart>() == this;
        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);
        internal static bool ActorCanGather(Entity actor, Zone zone) => PreparationActions.CurrentActor(actor, zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true;

        private RegionalWeather.Observation Weather(Zone zone)
        {
            var manager = WorldLocationContext.For(zone);
            return manager?.ActiveZone == zone && manager?.Exploration?.WorldKey == WorldKey && !string.IsNullOrEmpty(WorldKey)
                && StationReadiness.CurrentZone(zone) ? RegionalWeather.Current(zone, WorldClock.CurrentTick, PolicyVersion) : default;
        }

        internal bool Ready(Zone zone)
        {
            var owner = ParentEntity;
            if (owner?.GetPart<HarvestablePart>() != this || owner.Parts.Count(p => p is HarvestablePart) != 1
                || !RainCatchmentPart.FixedOn(owner, Ground, zone)) return false;
            var weather = Weather(zone);
            return weather.Valid && weather.Season == Season;
        }

        internal Func<bool> CaptureSeason(Zone zone, Cell sourceCell)
        {
            var owner = ParentEntity;
            var ground = Ground;
            string worldKey = WorldKey, sourceID = owner.ID;
            int week = Weather(zone).Window, policy = PolicyVersion, season = Season;
            return () =>
            {
                var current = Weather(zone);
                return current.Valid && current.Window == week && current.Season == season
                    && owner.ID == sourceID && ParentEntity == owner && owner.GetPart<HarvestablePart>() == this
                    && owner.Parts.Count(p => p is HarvestablePart) == 1 && Harvested && owner.SpatialZone == null
                    && Ground == ground && zone.GetEntityCell(ground) == sourceCell && !sourceCell.IsInterior
                    && WorldKey == worldKey && PolicyVersion == policy && Season == season;
            };
        }

        public string DescribeState(DescribeContext context)
            => Ready(context.Zone ?? ParentEntity?.SpatialZone)
                ? "The heads rattle with loose seed. One bundle is ready to gather."
                : "This stand sets usable heads late in the growing cycle. The ordinary allotment crops remain available.";
    }

    /// <summary>Fixed public mesh on the same frame that collects rain.</summary>
    public sealed class SeedFramePart : Part, IStationCapability, IStationReadinessGuard, IDescribeUse
    {
        public override string Name => "SeedFrame";
        internal const string WorkKind = "SeedFrame";
        internal const string Kind = "seed-frame";
        private static readonly IReadOnlyList<string> AllKinds = Array.AsReadOnly(new[] { Kind });
        public IReadOnlyList<string> Kinds => AllKinds;
        public int Reach => 1;
        public ReadinessMode Readiness => ReadinessMode.Strict;
        public int Order => 46;

        public bool TryBlock(Entity station, Zone zone, out string code)
        {
            code = "fixed-public-frame";
            var collector = station.GetPart<RainCollectorPart>();
            var claim = collector?.Anchor?.GetPart<RainCatchmentPart>();
            if (ParentEntity != station || collector?.ParentEntity != station || claim == null
                || !claim.Bound(zone, WorldClock.CurrentTick) || !RainCatchmentPart.FixedOn(station, collector.Ground, zone)
                || SpatialQuery.Distance(zone, station, collector.Anchor) > RainCatchmentPart.Reach) return true;
            code = null;
            return false;
        }

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Bring a knotflax seed-head bundle to the frame. Use its dry mesh to winnow one plantable seed.");
            guide.Needs.Add("one gathered bundle and the intact public frame");
            guide.Limits.Add("The work takes one action and discards dry chaff. It can be done after the gathering season and uses no rainwater.");
        }
    }

    internal static class SeasonalSeedSite
    {
        internal const string Stand = "LateKnotflaxStand";
        internal const string Bundle = "KnotflaxSeedHeads";
        internal const string Output = "KnotflaxSeed";
        private static readonly HashSet<Zone> Working = new();

        internal static void TryInstall(Zone zone, OverworldZoneManager manager)
        {
            var factory = manager.Factory;
            if (zone.ZoneID != RepairCultivationSite.ZoneID || manager.Exploration is not { Version: >= 21 }
                || factory == null || !factory.Blueprints.ContainsKey(Stand) || !HarvestablePart.CanCreateYield(factory, Bundle)
                || MaterialProcessTemplate.Capture(factory, Bundle) == null || MaterialProcessTemplate.Capture(factory, Output, true) == null)
                return;
            var before = zone.GetReadOnlyEntities().ToArray();
            var frames = before.Where(e => e.HasPart<RainCollectorPart>()).ToArray();
            if (frames.Length != 2 || before.Any(e => e.HasPart<SeedFramePart>() || e.HasPart<SeasonalHarvestPart>())) return;
            var frame = frames[0].GetPart<RainCollectorPart>();
            var anchor = zone.GetEntityCell(frame.Anchor);
            if (anchor == null || frames.Any(e => e.GetPart<RainCollectorPart>().Anchor != frame.Anchor)) return;
            if (before.Any(e => e.HasTag("Creature") && SpatialQuery.Distance(zone, e, frame.Anchor) <= 4)) return;
            var reachable = RepairCultivationSite.Reach(zone, new HashSet<(int, int)>());
            bool Clear(Cell cell) => cell != null && !cell.IsInterior && !zone.GenReservedCells.Contains((cell.X, cell.Y))
                && reachable.Contains((cell.X, cell.Y)) && cell.Objects.Count == 1
                && cell.Objects[0].BlueprintName == "Grass" && !cell.BlocksMovement()
                && !zone.TileState.Has(cell.X, cell.Y);
            var ground = Enumerable.Range(-2, 5).SelectMany(dx => Enumerable.Range(-2, 5)
                .Select(dy => zone.GetCell(anchor.X + dx, anchor.Y + dy)))
                .FirstOrDefault(Clear);
            if (ground == null || !Working.Add(zone)) return;

            var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, before);
            var terrain = ground.Objects[0];
            string worldKey = manager.Exploration.WorldKey;
            Entity stand = null;
            var work = new List<SeedFramePart>();
            bool complete = false;
            try
            {
                stand = factory.CreateEntity(Stand);
                if (stand?.GetPart<SeasonalHarvestPart>() is not { Harvested:false, YieldBlueprint:Bundle, YieldMin:1, YieldMax:1, YieldChance:100 } source
                    || stand.GetPart<PhysicsPart>() is not { Takeable:false, Solid:false } physics || physics.InInventory != null
                    || physics.Equipped != null || stand.SpatialZone != null || string.IsNullOrEmpty(stand.ID)
                    || before.Any(e => e.ID == stand.ID) || !unchanged() || !before.ToHashSet().SetEquals(zone.GetReadOnlyEntities())
                    || !Clear(ground) || ground.Objects[0] != terrain || worldKey != manager.Exploration.WorldKey) return;
                source.Ground = terrain;
                source.WorldKey = worldKey;
                if (!zone.AddEntity(stand, ground.X, ground.Y) || !unchanged()
                    || !RainCatchmentPart.FixedOn(stand, terrain, zone)) return;

                foreach (var owner in frames)
                {
                    var part = new SeedFramePart();
                    work.Add(part);
                    owner.AddPart(part);
                }
                complete = before.Append(stand).ToHashSet().SetEquals(zone.GetReadOnlyEntities())
                    && frames.Zip(work, (owner, part) => owner.GetPart<SeedFramePart>() == part && part.ParentEntity == owner).All(x => x);
            }
            catch (Exception error)
            {
                CavesOfOoo.Diagnostics.Diag.Record("worldgen", "SeasonalSeedSiteRefused", target: frame.Anchor,
                    payload: new { error = error.GetType().Name });
            }
            finally
            {
                Working.Remove(zone);
                if (!complete)
                {
                    foreach (var part in work) part.ParentEntity?.RemovePart(part);
                    if (stand?.SpatialZone == zone) zone.RemoveEntity(stand);
                }
            }
        }
    }
}
