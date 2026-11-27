using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    internal static class LiquidHearthFuel
    {
        const string Prefix = "FeedHearthOil|";
        const string StoredPrefix = "FeedStoredHearthOil|";
        const int MaximumOilPerAction = 3;

        internal static bool IsStoredCommand(string command) => command?.StartsWith(StoredPrefix, StringComparison.Ordinal) == true;
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true
            || IsStoredCommand(command);

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

        static bool BoxBesideHearth(Entity box, Entity hearth, Zone zone)
        {
            var install = hearth.GetPart<InstallablePart>();
            var a = zone.GetEntityCell(hearth);
            var b = zone.GetEntityCell(box);
            return install?.ParentEntity == hearth && install.State == InstallablePart.Installed
                && hearth.Parts.Count(part => part is InstallablePart) == 1
                && box != hearth && a != null && b != null && a != b
                && a.ParentZone == zone && b.ParentZone == zone
                && zone.GetCell(a.X, a.Y) == a && zone.GetCell(b.X, b.Y) == b
                && Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) <= 1;
        }

        static IEnumerable<(Entity flask, MaterialProcessStock stock)> Sources(Entity actor, Entity hearth, Zone zone, InventoryPart pack)
        {
            foreach (var flask in pack.Objects)
                yield return (flask, null);
            foreach (var box in InventorySystem.GetDepositContainers(actor, zone))
            {
                if (!BoxBesideHearth(box, hearth, zone) || MaterialProcessStock.Resolve(actor, box, zone) is not { } stock)
                    continue;
                foreach (var flask in stock.Container.Contents)
                    yield return (flask, stock);
            }
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

            foreach (var (flask, stock) in Sources(actor, hearth, zone, pack))
            {
                bool SourceCurrent(bool allowEmpty) => stock == null
                    ? LiquidVesselPart.HasCarriedOil(actor, flask, allowEmpty) && !zone.GetReadOnlyEntities().Any(entity => entity.ID == flask.ID)
                    : BoxBesideHearth(stock.Box, hearth, zone) && stock.ContainsOilFlask(flask, allowEmpty);
                if (!SourceCurrent(false)) continue;
                var vessel = flask.GetPart<LiquidVesselPart>();
                int volume = vessel.Volume, capacity = vessel.Capacity;
                int spent = Math.Min(room, volume), remaining = volume - spent;
                float filled = before + spent;
                double gain = (double)filled - before;
                if (Math.Abs(gain - spent) > .00001 || filled > maximum) continue;
                var sameFlask = RouteWorkMenu.Identity(flask);
                var box = stock?.Box;
                var boxCell = box == null ? null : zone.GetEntityCell(box);
                string flaskId = Uri.EscapeDataString(flask.ID);
                string key = box == null ? Prefix + flaskId : StoredPrefix + Uri.EscapeDataString(box.ID) + "|" + flaskId;

                bool Current(float expectedFuel, int expectedOil)
                    => Ready(actor, fire, zone, out var currentFuel) && sameHearth() && sameFlask()
                        && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(hearth) == hearthCell
                        && actor.GetPart<InventoryPart>() == pack && currentFuel == fuel && fuel.MaxFuel == maximum
                        && fuel.FuelMass == expectedFuel && fuel.BurnRate == burnRate && heat.Temperature == temperature
                        && SourceCurrent(true) && vessel.Capacity == capacity && vessel.Volume == expectedOil
                        && LiquidRegistry.Get("oil") == oil;

                yield return new RouteWorkMenu.Entry
                {
                    Actor = actor, Zone = zone, Cell = hearthCell, Claims = box == null ? new[] { flask } : new[] { box, flask },
                    Key = key,
                    Label = (box == null ? "feed coals" : "feed oil from camp box at " + boxCell.X + "," + boxCell.Y)
                        + " (" + spent + " oil, +" + spent + " fuel)",
                    Hint = "Uses " + spent + " of " + volume + " oil in " + InventoryPart.GetUnitDisplayName(flask)
                        + (box == null ? "." : ". The flask stays in the box.") + " One action. Cold coals stay cold.",
                    Before = () => Current(before, volume),
                    After = () => Current(filled, remaining),
                    Apply = tx =>
                    {
                        if (stock != null && !stock.Begin(flask, tx, key)) return false;
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
                        string source = box == null ? "the flask" : "the flask in the camp box";
                        MessageLog.Add("You feed " + spent + " oil from " + source + " to the coals (+" + spent + " fuel).");
                        Diag.Record("event", "HearthOilFed", actor, hearth, new { source = flask.ID, box = box?.ID, oil = spent, fuel = filled });
                    }
                };
            }
        }
    }
}
