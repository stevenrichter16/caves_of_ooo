using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    internal static class LiquidHearthFuel
    {
        const string Prefix = "FeedHearthOil|";
        const int MaximumOilPerAction = 3;

        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        static bool Ready(Entity actor, CampfirePart fire, Zone zone, out FuelPart fuel)
        {
            fuel = null;
            var hearth = fire?.ParentEntity;
            return RouteWorkMenu.ActorReady(actor, zone) && CampfireWorkActions.Source(actor, fire, zone)
                && CampfireWorkActions.Fuel(fire, out fuel) && RouteWorkMenu.Visible(zone.GetEntityCell(hearth))
                && !PackableFixturePart.IsInertKit(hearth) && !RepairablePart.BlocksFunction(hearth) && !hearth.HasEffect<BrokenEffect>()
                && hearth.Parts.Count(part => part is CampfirePart) == 1 && hearth.Parts.Count(part => part is FuelPart) == 1
                && hearth.Parts.Count(part => part is ThermalPart) == 1 && hearth.Parts.Count(part => part is PhysicsPart) == 1
                && WorldResourceActions.Finite(fuel.BurnRate) && fuel.BurnRate > 0
                && WorldResourceActions.ExactGround(zone, Uri.EscapeDataString(hearth.ID)) == hearth;
        }

        internal static IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor, CampfirePart fire, Zone zone)
        {
            if (!Ready(actor, fire, zone, out var fuel) || actor.GetPart<InventoryPart>() is not InventoryPart pack) yield break;
            var hearth = fire.ParentEntity;
            var hearthCell = zone.GetEntityCell(hearth);
            var actorCell = zone.GetEntityCell(actor);
            var heat = hearth.GetPart<ThermalPart>();
            float before = fuel.FuelMass, maximum = fuel.MaxFuel, burnRate = fuel.BurnRate, temperature = heat.Temperature;
            // Fuel by itself must not restart an exhausted fire at residual cooking heat.
            if (before == 0 && temperature >= CookingService.MinimumFiniteCookingTemperature) yield break;
            int room = (int)Math.Floor(Math.Min(MaximumOilPerAction, (double)maximum - before));
            if (room < 1) yield break;
            var sameHearth = RouteWorkMenu.Identity(hearth);
            var oil = LiquidRegistry.Get("oil");

            foreach (var flask in pack.Objects)
            {
                if (!LiquidVesselPart.HasCarriedOil(actor, flask) || zone.GetReadOnlyEntities().Any(entity => entity.ID == flask.ID)) continue;
                var vessel = flask.GetPart<LiquidVesselPart>();
                int volume = vessel.Volume, capacity = vessel.Capacity;
                int spent = Math.Min(room, volume), remaining = volume - spent;
                float filled = before + spent;
                double gain = (double)filled - before;
                if (Math.Abs(gain - spent) > .00001 || filled > maximum) continue;
                var sameFlask = RouteWorkMenu.Identity(flask);

                bool Current(float expectedFuel, int expectedOil)
                    => Ready(actor, fire, zone, out var currentFuel) && sameHearth() && sameFlask()
                        && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(hearth) == hearthCell
                        && actor.GetPart<InventoryPart>() == pack && currentFuel == fuel && fuel.MaxFuel == maximum
                        && fuel.FuelMass == expectedFuel && fuel.BurnRate == burnRate && heat.Temperature == temperature
                        && LiquidVesselPart.HasCarriedOil(actor, flask, true) && vessel.Capacity == capacity && vessel.Volume == expectedOil
                        && LiquidRegistry.Get("oil") == oil && !zone.GetReadOnlyEntities().Any(entity => entity.ID == flask.ID);

                yield return new RouteWorkMenu.Entry
                {
                    Actor = actor, Zone = zone, Cell = hearthCell, Claims = new[] { flask },
                    Key = Prefix + Uri.EscapeDataString(flask.ID),
                    Label = "feed coals (" + spent + " oil, +" + spent + " fuel)",
                    Hint = "Uses " + spent + " of " + volume + " oil in " + InventoryPart.GetUnitDisplayName(flask)
                        + ". One action. Cold coals stay cold.",
                    Before = () => Current(before, volume),
                    After = () => Current(filled, remaining),
                    Apply = tx =>
                    {
                        tx.Do(() =>
                        {
                            vessel.Volume = remaining;
                            vessel.LiquidId = remaining == 0 ? "" : "oil";
                            fuel.FuelMass = filled;
                        }, () =>
                        {
                            if (vessel.ParentEntity == flask && flask.GetPart<LiquidVesselPart>() == vessel
                                && (vessel.LiquidId == "oil" || vessel.Volume == 0 && vessel.LiquidId == ""))
                            {
                                vessel.Volume = WaterQuantityUndo.Restore(vessel.Volume, -spent, vessel.Capacity);
                                vessel.LiquidId = vessel.Volume == 0 ? "" : "oil";
                            }
                            else Diag.Record("event", "HearthOilRollbackLimited", actor, flask, new { reason = "source_changed" });
                            if (fuel.ParentEntity == hearth && hearth.GetPart<FuelPart>() == fuel
                                && WorldResourceActions.Finite(fuel.FuelMass) && WorldResourceActions.Finite(fuel.MaxFuel))
                                fuel.FuelMass = (float)Math.Max(0, Math.Min(fuel.MaxFuel, fuel.FuelMass - gain));
                            else Diag.Record("event", "HearthOilRollbackLimited", actor, hearth, new { reason = "fuel_changed" });
                        });
                        return true;
                    },
                    Publish = () =>
                    {
                        ZoneRenderHooks.MarkCellDirty(hearthCell, "HearthOilFed");
                        MessageLog.Add("You feed " + spent + " oil from the flask to the coals (+" + spent + " fuel).");
                        Diag.Record("event", "HearthOilFed", actor, hearth, new { source = flask.ID, oil = spent, fuel = filled });
                    }
                };
            }
        }
    }
}
