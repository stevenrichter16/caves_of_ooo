using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A fixed, one-use source. Season controls gathering, not the portable product.</summary>
    public sealed class SeasonalHarvestPart : HarvestablePart, IDescribeState, IPostCommitInventoryActionNotification
    {
        const int FlexibleStemSeason = 0;
        const string Cord = "KnotflaxCord";
        const string CutStems = "HarvestKnotflaxStems";
        public override string Name => "SeasonalHarvest";
        public Entity Ground;
        public string WorldKey = "";
        public int Season = 1;
        public int PolicyVersion = RegionalWeather.PolicyVersion;
        public string ReadyDescription = "The heads rattle with loose seed. One bundle is ready to gather.";
        public string DormantDescription = "This stand sets usable heads late in the growing cycle. The ordinary allotment crops remain available.";

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 38;
        public bool OwnsInventoryReceipt(string command)
            => ParentEntity?.GetPart<HarvestablePart>() == this
                && (command == "Harvest" || command?.StartsWith(CutStems + "|", StringComparison.Ordinal) == true);
        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);
        internal static bool ActorCanGather(Entity actor, Zone zone) => PreparationActions.CurrentActor(actor, zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true;

        private RegionalWeather.Observation Weather(Zone zone)
        {
            var manager = WorldLocationContext.For(zone);
            return manager?.ActiveZone == zone && manager?.Exploration?.WorldKey == WorldKey && !string.IsNullOrEmpty(WorldKey)
                && StationReadiness.CurrentZone(zone) ? RegionalWeather.Current(zone, WorldClock.CurrentTick, PolicyVersion) : default;
        }

        private bool FixedSource(Zone zone)
        {
            var owner = ParentEntity;
            return owner?.GetPart<HarvestablePart>() == this && owner.Parts.Count(p => p is HarvestablePart) == 1
                && RainCatchmentPart.FixedOn(owner, Ground, zone);
        }

        internal bool Ready(Zone zone, int? selectedSeason = null)
        {
            if (!FixedSource(zone)) return false;
            var weather = Weather(zone);
            return weather.Valid && weather.Season == (selectedSeason ?? Season);
        }

        internal (string Glyph, string Color) KnotflaxAppearance(Zone zone)
        {
            if (zone?.ZoneID != RepairCultivationSite.ZoneID || Harvested
                || ParentEntity?.BlueprintName != SeasonalSeedSite.Stand || Season != 1
                || !FixedSource(zone)) return default;
            var weather = Weather(zone);
            if (!weather.Valid) return default;
            if (weather.Season == FlexibleStemSeason && FlexibleStems(zone)) return ("|", "&G");
            return weather.Season == Season ? ("*", "&Y") : (";", "&y");
        }

        internal (string Glyph, string Color) EarlyPitchpodAppearance(Zone zone)
        {
            if (zone?.ZoneID != BiomeCropPlacement.PosyOutskirts || Harvested
                || ParentEntity?.BlueprintName != BiomeCropPlacement.EarlyPitchpod
                || Season != 0 || YieldBlueprint != "PitchpodResin" || YieldMin != 1 || YieldMax != 1 || YieldChance != 100
                || !Ready(zone)) return default;
            return ("*", "&Y");
        }

        internal bool TryGetNorthLichenReadiness(Zone zone, out bool ready)
        {
            ready = false;
            var owner = ParentEntity;
            if (zone?.ZoneID != SpreadExplorationWorksites.NorthWorksZoneID || Harvested
                || owner?.BlueprintName != "FrostLichenPatch"
                || owner.GetProperty(SpreadExplorationWorksites.RoleKey) != SpreadExplorationWorksites.SeasonalColdForage
                || Season != 3 || YieldBlueprint != "FrostLichen" || YieldMin != 1 || YieldMax != 1 || YieldChance != 100
                || !FixedSource(zone)) return false;
            var weather = Weather(zone);
            if (!weather.Valid) return false;
            ready = weather.Season == Season;
            return true;
        }

        internal Func<bool> CaptureSeason(Zone zone, Cell sourceCell, int? selectedSeason = null)
        {
            var owner = ParentEntity;
            var ground = Ground;
            string worldKey = WorldKey, sourceID = owner.ID;
            int week = Weather(zone).Window, policy = PolicyVersion, season = Season;
            return () =>
            {
                var current = Weather(zone);
                return current.Valid && current.Window == week && current.Season == (selectedSeason ?? season)
                    && owner.ID == sourceID && ParentEntity == owner && owner.GetPart<HarvestablePart>() == this
                    && owner.Parts.Count(p => p is HarvestablePart) == 1 && Harvested && owner.SpatialZone == null
                    && Ground == ground && zone.GetEntityCell(ground) == sourceCell && !sourceCell.IsInterior
                    && WorldKey == worldKey && PolicyVersion == policy && Season == season;
            };
        }

        bool FlexibleStems(Zone zone)
            => ParentEntity?.BlueprintName == SeasonalSeedSite.Stand && Season == 1
                && YieldBlueprint == SeasonalSeedSite.Bundle && YieldMin == 1 && YieldMax == 1 && YieldChance == 100
                && Ready(zone, FlexibleStemSeason) && CanCreateYield(Factory, Cord)
                && MaterialProcessTemplate.Capture(Factory, Cord, true) != null;

        IEnumerable<RouteWorkMenu.Entry> StemChoices(Entity actor, Zone zone)
        {
            var source = ParentEntity;
            if (Harvested || !FlexibleStems(zone) || !ActorCanGather(actor, zone)) yield break;
            var from = zone.GetEntityCell(actor);
            var cell = zone.GetEntityCell(source);
            if (!RouteWorkMenu.Visible(cell) || SpatialQuery.Distance(zone, actor, source) > 1) yield break;
            var tool = MaterialCuttingWorkReceipt.Resolve(actor, source, zone);
            if (tool == null) yield break;
            var ground = Ground;
            string id = source.ID, key = WorldKey;
            int window = Weather(zone).Window, policy = PolicyVersion;
            bool Before() => !Harvested && ParentEntity == source && source.ID == id && Ground == ground
                && WorldKey == key && PolicyVersion == policy && FlexibleStems(zone) && ActorCanGather(actor, zone)
                && zone.GetEntityCell(actor) == from && zone.GetEntityCell(source) == cell && RouteWorkMenu.Visible(cell)
                && Weather(zone).Window == window && tool.Current();
            bool After() => Harvested && source.SpatialZone == null && source.BlueprintName == SeasonalSeedSite.Stand
                && zone.GetEntityCell(actor) == from && tool.Current();
            yield return new RouteWorkMenu.Entry
            {
                Actor = actor, Zone = zone, Cell = cell, Key = CutStems, Claims = new[] { tool.Tool },
                Label = "cut flexible stems for cord (1 unit)",
                Hint = "One action. Keep the cutting tool. Cut now and this stand yields no seed later.",
                Before = Before, After = After,
                Apply = tx => tool.Begin(tx, CutStems) && DoHarvest(actor, zone, tx, seasonalYield: (Cord, FlexibleStemSeason))
            };
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                base.HandleEvent(e);
                var actor = e.GetParameter<Entity>("Actor");
                var zone = PreparationActions.ZoneFor(e, actor);
                RouteWorkMenu.Rows(ParentEntity, actor, zone, e.GetParameter<InventoryActionList>("Actions"), StemChoices(actor, zone), true);
                return true;
            }
            if (e.ID == "InventoryAction" && e.GetStringParameter("Command")?.StartsWith(CutStems + "|", StringComparison.Ordinal) == true)
            {
                if (!RouteWorkMenu.Act(ParentEntity, e)) return true;
                e.Handled = true;
                return false;
            }
            return base.HandleEvent(e);
        }

        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone;
            if (FlexibleStems(zone)) return "Flexible stems are ready. Cut them for one cord or let the stand set seed.";
            return Ready(zone) ? ReadyDescription : DormantDescription;
        }
    }

    /// <summary>Fixed public mesh on the same frame that collects rain.</summary>
    public sealed class SeedFramePart : Part, IStationCapability, IStationReadinessGuard, IDescribeUse
    {
        public override string Name => "SeedFrame";
        internal const string WorkKind = "SeedFrame";
        internal const string Kind = "seed-frame";
        internal const string Chaff = "DryKnotflaxChaff";
        internal const string ChaffFuel = "combustible:1";
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
            guide.How.Add("Bring a knotflax seed-head bundle to the frame and winnow one plantable seed.");
            guide.Needs.Add("one gathered bundle and the intact public frame");
            guide.Limits.Add("In fair weather on dry ground, the frame can save the chaff for a fire. Otherwise the chaff is discarded. Winnowing takes one action, in any season, and uses no rainwater.");
        }

        internal static bool IsKnotflaxWork(MaterialProcessDefinition recipe)
            => recipe?.Id == "winnow_knotflax_seed" && recipe.WorkKind == WorkKind
                && recipe.Mode == "Process" && recipe.SourceBlueprint == SeasonalSeedSite.Bundle
                && recipe.InputCount == 1 && recipe.AdditionalInputBlueprints.Count == 0
                && recipe.OutputBlueprints.Count == 1 && recipe.OutputBlueprints[0] == SeasonalSeedSite.Output
                && recipe.OutputCounts[0] == 1;

        /// <summary>Whether this fixed public frame can keep dry chaff at the
        /// current site and time. A work quote separately checks its batch.</summary>
        internal bool CanRetainChaffAt(Zone zone)
        {
            var station = ParentEntity;
            if (TurnManager.Active == null || station?.GetPart<SeedFramePart>() != this || zone == null
                || WorldLocationContext.For(zone)?.ActiveZone != zone) return false;
            var collector = station.GetPart<RainCollectorPart>();
            var catchment = collector?.Anchor?.GetPart<RainCatchmentPart>();
            if (collector?.Clean != true || catchment == null) return false;
            var weather = RegionalWeather.Current(zone, WorldClock.CurrentTick, catchment.PolicyVersion);
            if (!weather.Valid || weather.Raining || !StationReadiness.Evaluate(station, zone, Kind, out _)) return false;
            var cell = zone.GetEntityCell(station);
            return cell is { IsInterior: false } && IsDryWorkCell(zone, cell);
        }

        internal static bool IsDryWorkCell(Zone zone, Cell cell)
        {
            if (zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(layer => layer.Turns > 0) == true) return false;
            foreach (var item in cell.Occupants)
                if (item.GetPart<LiquidPoolPart>() is { Volume: > 0 }) return false;
            return true;
        }

        internal static MaterialProcessTemplate RetainedChaff(MaterialProcessPlan plan)
        {
            if (!IsKnotflaxWork(plan.Definition)
                || HandlingService.GetWeight(plan.Source) != 1
                || !plan.Factory.Blueprints.TryGetValue(SeasonalSeedSite.Output, out var seed)
                || seed.Parts.ContainsKey("Handling") || !seed.Parts.TryGetValue("Physics", out var seedPhysics)
                || !seedPhysics.TryGetValue("Weight", out var seedWeight)
                || seedWeight != "0"
                || !plan.IsShelteredWinnow && (plan.StationKind != Kind
                    || plan.Station.GetPart<SeedFramePart>()?.CanRetainChaffAt(plan.Zone) != true)) return null;

            var template = MaterialProcessTemplate.Capture(plan.Factory, Chaff, true);
            return template != null && plan.Actor.GetPart<InventoryPart>().Objects
                .Where(item => item.BlueprintName == Chaff)
                .All(item => PreparationActions.Carried(plan.Actor, item) && template.Matches(item)) ? template : null;
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
