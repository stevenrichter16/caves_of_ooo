using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A fixed public outlet that tends one actual prepared bed.</summary>
    public sealed class SoddenRainGuidePart : Part, IDescribeState, IDescribeUse, IDescribeUseRows, IInventoryActionReceiptOwner
    {
        internal const string Blueprint = "SoddenRainGuide";
        internal const int FirstVersion = 26;
        private const string Prefix = "GuideRain|";
        private static readonly HashSet<OverworldZoneManager> Installing = new();
        public override string Name => Blueprint;
        public Entity Crop, Soil;
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;

        public bool OwnsInventoryReceipt(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        public string DescribeState(DescribeContext context)
        {
            if (!ReadSite(context.Zone ?? ParentEntity.SpatialZone, out var claim, out var weather))
                return "This channel needs its original exposed sumpsieve bed and public catchment.";
            if (!weather.Raining) return "Fair weather. The sumpsieve can still take ordinary watering.";
            if (claim.Remaining(weather.Window) == 0) return "The sumpsieve bed has used this shower's water portion.";
            var crop = Crop.GetPart<CropPart>();
            if (crop.GrowthStage == 2) return "The sumpsieve is ripe and needs no more water.";
            if (crop.MoistureTicks >= CropWateringService.MoisturePerUnit) return "The sumpsieve bed already has enough water.";
            return "A mild shower supplies one portion for the sumpsieve bed.";
        }

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Stand beside the channel, press C and choose guide shower water to sumpsieve. Keep the water you carry.");
            guide.Limits.Add("One portion is available per shower interval. In fair weather, water the crop as usual.");
        }

        public void AddUseRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Ready(actor, zone, out var claim, out var weather, out _)) return;
            actions.AddActionWithHint(Name, "guide shower water to sumpsieve", Quote(claim, weather),
                "One local rain portion and one action. Carried water stays with you.", '\0', 20, time: ActionTime.OneAction);
        }

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions") AddUseRows(actor, zone, e.GetParameter<InventoryActionList>("Actions"));
            if (e.ID != "InventoryAction" || !OwnsInventoryReceipt(e.GetStringParameter("Command"))) return true;
            e.Handled = TryGuide(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"));
            return !e.Handled;
        }

        private bool ReadSite(Zone zone, out RainCatchmentPart claim, out RegionalWeather.Observation weather)
        {
            claim = ParentEntity?.GetPart<RainCatchmentPart>();
            weather = default;
            var manager = WorldLocationContext.For(zone);
            if (manager?.ActiveZone != zone || manager?.Exploration is not { Enabled: true, Version: >= FirstVersion }
                || ParentEntity?.GetPart<SoddenRainGuidePart>() != this || claim?.ParentEntity != ParentEntity || claim?.Capacity != 1
                || !claim.Bound(zone, WorldClock.CurrentTick) || !RainCatchmentPart.FixedOn(Crop, Soil, zone)
                || Crop.GetProperty(SoddenDistrictBuilder.RoleKey) != "dry-sumpsieve"
                || !CropTime.IsCurrentOwner(Crop.GetPart<CropPart>(), zone) || SpatialQuery.Distance(zone, ParentEntity, Crop) != 1
                || !CultivatedSoilPart.IsCultivated(zone, zone.GetEntityCell(Soil))) return false;
            weather = RegionalWeather.Current(zone, WorldClock.CurrentTick, claim.PolicyVersion);
            return weather.Valid;
        }

        private bool Ready(Entity actor, Zone zone, out RainCatchmentPart claim, out RegionalWeather.Observation weather, out CropPart crop)
        {
            crop = null;
            return ReadSite(zone, out claim, out weather) && weather.Raining && claim.Remaining(weather.Window) > 0
                && actor != null && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1
                && CropWateringService.CropInvalid(actor, Crop, zone, out crop) == null && crop.GrowthStage < 2
                && crop.MoistureTicks < CropWateringService.MoisturePerUnit;
        }

        private string Quote(RainCatchmentPart claim, RegionalWeather.Observation weather) =>
            $"{Prefix}{ParentEntity.ID}|{Crop.ID}|{Soil.ID}|{weather.Window}|{claim.LastWindow}|{claim.Spent}";

        private bool TryGuide(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (tx == null || !Ready(actor, zone, out var claim, out var weather, out var crop) || command != Quote(claim, weather)) return false;
            var owner = ParentEntity;
            var target = Crop;
            var ground = Soil;
            var cropCell = zone.GetEntityCell(target);
            var guideCell = zone.GetEntityCell(owner);
            var actorCell = zone.GetEntityCell(actor);
            if (!CropTime.Reconcile(crop, zone, WorldClock.CurrentTick)
                || !Ready(actor, zone, out var current, out var now, out var currentCrop)
                || current != claim || currentCrop != crop || command != Quote(current, now) || ParentEntity != owner || Crop != target || Soil != ground
                || zone.GetEntityCell(owner) != guideCell || zone.GetEntityCell(target) != cropCell || zone.GetEntityCell(actor) != actorCell
                || !tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command) || !tx.TryClaim(target, actor, command)) return false;

            int last = claim.LastWindow, spent = claim.Spent, moisture = crop.MoistureTicks;
            int stage = crop.GrowthStage, growth = crop.TicksInStage, stamp = crop.LastGrowthWorldTick;
            CropWateringService.StageMoisture(tx, actor, target, zone, crop, "CropRainWater", () =>
            {
                if (owner.GetPart<RainCatchmentPart>() != claim || claim.LastWindow != weather.Window || claim.Spent != 1) return;
                claim.LastWindow = last;
                claim.Spent = spent;
            });
            claim.LastWindow = weather.Window;
            claim.Spent = 1;
            tx.BeforeCommit(() => ParentEntity == owner && Crop == target && Soil == ground
                && zone.GetEntityCell(owner) == guideCell && zone.GetEntityCell(target) == cropCell && zone.GetEntityCell(actor) == actorCell
                && ReadSite(zone, out var checkedClaim, out var checkedWeather) && checkedClaim == claim
                && checkedWeather.Raining && checkedWeather.Window == weather.Window && claim.LastWindow == weather.Window && claim.Spent == 1
                && CropWateringService.CropInvalid(actor, target, zone, out var checkedCrop) == null
                && checkedCrop == crop && crop.GrowthStage == stage && crop.TicksInStage == growth && crop.LastGrowthWorldTick == stamp
                && crop.MoistureTicks == CropWateringService.MoisturePerUnit);
            tx.AfterCommit(() =>
            {
                MessageLog.Add("You guide one portion of shower water into the sumpsieve bed.");
                Diag.Record("crop", "RainGuided", actor, target, new { guide = owner.ID, weather.Window,
                    unitsSpent = 1, moistureBefore = moisture, moistureAfter = CropWateringService.MoisturePerUnit });
            });
            return true;
        }

        internal static void TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (zone.ZoneID != SoddenDistrictPlan.StopZoneID || manager.Exploration.Version < FirstVersion
                || !manager.Factory.Blueprints.ContainsKey(Blueprint) || manager.CachedZones.ContainsKey(zone.ZoneID)) return;
            var at = zone.GetCell(51, 9);
            var bed = zone.GetCell(50, 9);
            if (at.IsInterior || bed.IsInterior || at.Objects.Count != 1 || at.BlocksMovement()
                || zone.GenReservedCells.Contains((at.X, at.Y)) || zone.TileState.Has(at.X, at.Y)
                || !CultivatedSoilPart.IsCultivated(zone, bed)) return;
            var ground = at.Objects[0];
            var soil = bed.Objects.SingleOrDefault(e => e.HasPart<CultivatedSoilPart>());
            var crop = bed.Objects.SingleOrDefault(e => e.GetProperty(SoddenDistrictBuilder.RoleKey) == "dry-sumpsieve");
            if (ground.BlueprintName != "Grass" || !RainCatchmentPart.FixedOn(crop, soil, zone) || !Installing.Add(manager)) return;
            Entity fixture = null;
            bool installed = false;
            try
            {
                var plan = manager.Exploration;
                var before = zone.GetReadOnlyEntities().ToArray();
                var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, before);
                fixture = manager.Factory.CreateEntity(Blueprint);
                if (fixture?.GetPart<PhysicsPart>() is not { Takeable: false, Solid: false, InInventory: null, Equipped: null }
                    || fixture.BlueprintName != Blueprint || fixture.SpatialZone != null || string.IsNullOrEmpty(fixture.ID)
                    || fixture.GetPart<RenderPart>()?.Visible != true || fixture.GetPart<ExaminablePart>() == null
                    || fixture.HasPart<SoddenRainGuidePart>() || fixture.HasPart<RainCatchmentPart>() || before.Any(e => e.ID == fixture.ID)
                    || manager.Exploration != plan || manager.CachedZones.ContainsKey(zone.ZoneID) || !unchanged()
                    || !before.ToHashSet().SetEquals(zone.GetReadOnlyEntities()) || at.Objects.Count != 1 || at.Objects[0] != ground) return;
                fixture.AddPart(new RainCatchmentPart
                {
                    Ground = ground, WorldKey = plan.WorldKey, PlotKey = "sodden-shelter-sumpsieve",
                    Public = true, PolicyVersion = RegionalWeather.PolicyVersion, ActivatedAt = WorldClock.CurrentTick, ConfiguredAllowance = 1
                });
                fixture.AddPart(new SoddenRainGuidePart { Crop = crop, Soil = soil });
                installed = zone.AddEntity(fixture, at.X, at.Y) && unchanged() && RainCatchmentPart.FixedOn(fixture, ground, zone);
            }
            catch (Exception error)
            {
                Diag.Record("worldgen", "SoddenRainGuideRefused", target: ground, payload: new { error = error.GetType().Name });
            }
            finally
            {
                Installing.Remove(manager);
                if (!installed && fixture?.SpatialZone == zone) zone.RemoveEntity(fixture);
            }
        }
    }
}
