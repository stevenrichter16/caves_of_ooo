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
        const float MaximumReduction = .25f;

        public override string Name => "FairWeatherDrying";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;

        public bool OwnsInventoryReceipt(string command)
            => ParentEntity?.GetPart<FairWeatherDryingPart>() == this
                && command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        static bool DryExposed(Cell cell, Zone zone)
            => cell != null && !cell.IsInterior
                && zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(coat => coat.Turns > 0) != true
                && !cell.Occupants.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>());

        bool Conditions(Zone zone, out RegionalWeather.Observation weather)
        {
            weather = default;
            var surface = ParentEntity;
            if (TurnManager.Active == null || !WorldResourceActions.Ground(surface, zone)
                || WorldLocationContext.For(zone)?.ActiveZone != zone
                || surface.HasTag("Creature") || surface.HasPart<SpatialFootprintPart>()
                || surface.GetPart<PhysicsPart>().Takeable || SecondExplorationActions.Special(surface)
                || surface.Parts.Count(part => part is FairWeatherDryingPart) != 1
                || surface.GetPart<FairWeatherDryingPart>() != this)
                return false;
            weather = RegionalWeather.Current(zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion);
            return weather.Valid && DryExposed(zone.GetEntityCell(surface), zone);
        }

        public string DescribeState(DescribeContext context)
        {
            if (!Conditions(context.Zone ?? ParentEntity?.SpatialZone, out var weather))
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
        {
            if (!RouteWorkMenu.ActorReady(actor, zone) || !Conditions(zone, out var weather) || weather.Raining)
                yield break;
            var surface = ParentEntity;
            var work = zone.GetEntityCell(surface);
            var origin = zone.GetEntityCell(actor);
            var pack = actor.GetPart<InventoryPart>();
            var clock = TurnManager.Active;
            var sameSurface = RouteWorkMenu.Identity(surface);
            bool Available() => TurnManager.Active == clock && RouteWorkMenu.ActorReady(actor, zone)
                && zone.GetEntityCell(actor) == origin && zone.GetEntityCell(surface) == work
                && actor.GetPart<InventoryPart>() == pack && sameSurface()
                && RouteWorkMenu.Visible(work) && DryExposed(origin, zone) && !origin.BlocksMovement(actor)
                && Math.Abs(origin.X - work.X) + Math.Abs(origin.Y - work.Y) == 1
                && Conditions(zone, out var current) && !current.Raining && current.Window == weather.Window;
            if (pack == null || !Available()) yield break;

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
                float before = wet.Moisture, removed = Math.Min(before, MaximumReduction), after = before - removed;
                int duration = wet.Duration;
                bool SameWet() => effects?.ParentEntity == item && wet.Owner == item
                    && item.GetPart<StatusEffectsPart>() == effects && item.GetEffect<WetEffect>() == wet;
                bool Current(float expected) => Available() && sameItem() && WorldResourceActions.Carried(actor, item)
                    && SameWet() && wet.Moisture == expected
                    && wet.Duration == duration && !(item.GetEffect<FrozenEffect>()?.Cold > 0);

                yield return new RouteWorkMenu.Entry
                {
                    Actor = actor, Zone = zone, Cell = work, Claims = new[] { item },
                    Key = Prefix + Uri.EscapeDataString(item.ID),
                    Label = "air out " + item.GetDisplayName() + " (" + Math.Round(before * 100)
                        + "% to " + Math.Round(after * 100) + "% moisture)",
                    Hint = "One action. The item stays in your pack.",
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
                        MessageLog.Add("You air out " + item.GetDisplayName() + " on the drying shelf.");
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
