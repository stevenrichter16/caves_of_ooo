using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>A fixed surface that airs out one carried item during a fair, dry window.</summary>
    public sealed class FairWeatherDryingPart : Part, IDescribeState, IDescribeUse,
        IDescribeUseRows, IInventoryActionReceiptOwner
    {
        const string Prefix = "DryInFairAir|";
        internal const string ShelteredPrefix = "DryUnderGuestCloth|";
        const float MaximumReduction = .25f;
        const float ShelteredReduction = .15f;

        public override string Name => "FairWeatherDrying";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;

        public bool OwnsInventoryReceipt(string command)
            => ParentEntity?.GetPart<FairWeatherDryingPart>() == this
                && command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        internal static bool DryFooting(Cell cell, Zone zone)
            => cell != null
                && zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(coat => coat.Turns > 0) != true
                && !cell.Occupants.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>());

        bool SurfaceReady(Zone zone)
        {
            var surface = ParentEntity;
            if (!WorldResourceActions.Ground(surface, zone)
                || surface.HasTag("Creature") || surface.HasPart<SpatialFootprintPart>()
                || surface.GetPart<PhysicsPart>().Takeable || SecondExplorationActions.Special(surface)
                || surface.Parts.Count(part => part is FairWeatherDryingPart) != 1
                || surface.GetPart<FairWeatherDryingPart>() != this)
                return false;
            var cell = zone.GetEntityCell(surface);
            return DryFooting(cell, zone) && !cell.IsInterior;
        }

        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone;
            var weather = RegionalWeather.Current(zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion);
            if (TurnManager.Active == null || WorldLocationContext.For(zone)?.ActiveZone != zone
                || !weather.Valid || !SurfaceReady(zone))
                return "This surface needs open, dry footing to air out gear.";
            return weather.Raining ? "Mild showers. There is no fair-air drying here now."
                : "Fair weather. A dry approach to this shelf lets you air out carried gear.";
        }

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Stand one step beside the shelf and choose air out on the item you want to dry.");
            guide.Needs.Add("Fair weather, dry exposed footing and one wet, unfrozen item in your pack.");
            guide.Limits.Add("One action removes up to 25 points on the moisture scale. Nearby hearths and ordinary ground drying still work.");
            guide.WhyNot = "stand beside the shelf in fair weather with an eligible wet item in your pack";
        }

        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor, Zone zone)
            => ChoicesAt(ParentEntity, actor, zone, () => SurfaceReady(zone));

        internal static IEnumerable<RouteWorkMenu.Entry> ChoicesAt(Entity surface, Entity actor, Zone zone,
            Func<bool> surfaceReady, bool underCover = false)
        {
            var clock = TurnManager.Active;
            if (clock == null || !RouteWorkMenu.ActorReady(actor, zone)
                || WorldLocationContext.For(zone)?.ActiveZone != zone || !surfaceReady()) yield break;
            var weather = RegionalWeather.Current(zone, clock.TickCount, RegionalWeather.PolicyVersion);
            if (!weather.Valid || weather.Raining != underCover)
                yield break;
            var work = zone.GetEntityCell(surface);
            var origin = zone.GetEntityCell(actor);
            var pack = actor.GetPart<InventoryPart>();
            var sameSurface = RouteWorkMenu.Identity(surface);
            bool SameWeather()
            {
                var current = RegionalWeather.Current(zone, clock.TickCount, RegionalWeather.PolicyVersion);
                return current.Valid && current.Raining == underCover && current.Window == weather.Window;
            }
            bool Available() => TurnManager.Active == clock && WorldLocationContext.For(zone)?.ActiveZone == zone
                && RouteWorkMenu.ActorReady(actor, zone) && actor.GetPart<InventoryPart>() == pack
                && zone.GetEntityCell(actor) == origin && zone.GetEntityCell(surface) == work && sameSurface()
                && RouteWorkMenu.Visible(work) && DryFooting(origin, zone) && (underCover || !origin.IsInterior)
                && !origin.BlocksMovement(actor) && Math.Abs(origin.X - work.X) + Math.Abs(origin.Y - work.Y) == 1
                && surfaceReady() && SameWeather();
            if (pack == null || !Available()) yield break;

            float maximum = underCover ? ShelteredReduction : MaximumReduction;
            string prefix = underCover ? ShelteredPrefix : Prefix;
            string place = underCover ? "under the guest-cloth" : "on the drying shelf";
            foreach (var item in pack.Objects)
            {
                if (!WorldResourceActions.Carried(actor, item)
                    || item.GetEffect<WetEffect>() is not WetEffect wet
                    || item.GetEffect<FrozenEffect>()?.Cold > 0
                    || !WorldResourceActions.Finite(wet.Moisture) || wet.Moisture <= 0
                    || wet.Moisture > 1 || wet.Duration == 0)
                    continue;
                var effects = item.GetPart<StatusEffectsPart>();
                var sameItem = RouteWorkMenu.Identity(item);
                float before = wet.Moisture, removed = Math.Min(before, maximum), after = before - removed;
                int duration = wet.Duration;
                bool SameWet() => effects?.ParentEntity == item && wet.Owner == item
                    && item.GetPart<StatusEffectsPart>() == effects && item.GetEffect<WetEffect>() == wet;
                bool Current(float expected) => Available() && sameItem() && WorldResourceActions.Carried(actor, item)
                    && SameWet() && wet.Moisture == expected
                    && wet.Duration == duration && !(item.GetEffect<FrozenEffect>()?.Cold > 0);

                yield return new RouteWorkMenu.Entry
                {
                    Actor = actor, Zone = zone, Cell = work, Claims = new[] { item },
                    Key = prefix + Uri.EscapeDataString(item.ID),
                    Label = "air out " + item.GetDisplayName() + " (" + Math.Round(before * 100)
                        + "% to " + Math.Round(after * 100) + "% moisture)",
                    Hint = (underCover ? "One action under cover." : "One action.") + " The item stays in your pack.",
                    Before = () => Current(before),
                    After = () => Current(after),
                    Apply = tx =>
                    {
                        tx.Do(() => wet.Moisture = after, () =>
                        {
                            if (SameWet() && WorldResourceActions.Finite(wet.Moisture))
                                wet.Moisture = Math.Min(1f, wet.Moisture + removed);
                        });
                        return true;
                    },
                    Publish = () =>
                    {
                        if (after == 0) effects.RemoveEffect(wet);
                        MessageLog.Add("You air out " + item.GetDisplayName() + " " + place + ".");
                    }
                };
            }
        }

        public void AddUseRows(Entity actor, Zone zone, InventoryActionList rows)
            => RouteWorkMenu.Rows(ParentEntity, actor, zone, rows, Choices(actor, zone), false);

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions")
                RouteWorkMenu.Rows(ParentEntity, actor, zone, e.GetParameter<InventoryActionList>("Actions"),
                    Choices(actor, zone), true);
            if (e.ID != "InventoryAction" || !OwnsInventoryReceipt(e.GetStringParameter("Command"))
                || !RouteWorkMenu.Act(ParentEntity, e))
                return true;
            e.Handled = true;
            return false;
        }
    }
}
