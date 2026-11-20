using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Rest beside a standing camp hearth (Docs/MACHINE-INVENTED-USES.md, "a camp you can sleep at"). A rest is a clock jump that nothing simulates turn by turn, so a finite fire
    /// would otherwise keep its fuel through a night. This Part charges the honest amount: the fuel the fire burns in the ticks you sleep, ticks times its own burn rate (60 ticks
    /// at the hearth's 0.25 is 15 fuel). It is refused up front when the fire is out or could not keep even a normal night, with what would fix it in the reason, and nothing changes on a refusal. A longer rest (until the next time of day) is not refused: the fire burns what it can, goes out in the night, and the row says so.
    /// The heal, bleeding cure, clock advance and the hostile-within-eight rule are the shared <see cref="RestSystem"/>; only the paid fuel is new.
    /// <para>It is its own Part with its own commands, so the ordinary campfire rest (<c>AllowRest</c>, free) and every other finite fire keep exactly the rules they have.</para>
    /// </summary>
    public sealed class HearthRestPart : Part
    {
        public override string Name => "HearthRest";

        public const string RestCommand = "RestAtHearth", BandCommand = "RestUntilNextBandAtHearth", Site = "hearth";

        public static bool IsCommand(string command) => command == RestCommand || command == BandCommand;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                AddRows(actor, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (!IsCommand(command)) return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (TryRest(actor, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction")))
                { e.Handled = true; return false; }
            }
            return true;
        }

        // ── the accounting ──

        /// <summary>The fire's temperature after the given ticks, by the same per-turn decay the thermal part applies (with its snap to ambient).</summary>
        static float Cooled(ThermalPart heat, int ticks)
        {
            float t = heat.Temperature;
            for (int i = 0; i < ticks; i++)
            {
                if (t == heat.AmbientTemperature) break;
                t -= (t - heat.AmbientTemperature) * heat.AmbientDecayRate;
                if (Math.Abs(t - heat.AmbientTemperature) < 0.5f) t = heat.AmbientTemperature;
            }
            return t;
        }

        /// <summary>What the fire actually does through a rest: it burns until the ticks end, the fuel runs out, or it cools below cooking heat (the thermal part's own decay), whichever
        /// comes first. A night longer than the fire's life is not refused: the fire burns what it can and is out in the morning.</summary>
        readonly struct Night
        {
            public readonly int BurnTicks, Fuel; public readonly bool BurnsOut;
            public Night(int burnTicks, int fuel, bool burnsOut) { BurnTicks = burnTicks; Fuel = fuel; BurnsOut = burnsOut; }
        }

        static bool UsableRate(FuelPart fuel) => WorldResourceActions.Finite(fuel.BurnRate) && fuel.BurnRate > 0;

        static int FuelTicks(FuelPart fuel) => (int)Math.Min(int.MaxValue / 2, Math.Floor(fuel.FuelMass / (double)fuel.BurnRate + 1e-6));

        static int FuelFor(FuelPart fuel, int ticks) => (int)Math.Ceiling(ticks * (double)fuel.BurnRate - 1e-6);

        static Night Plan(FuelPart fuel, ThermalPart heat, int ticks)
        {
            int fuelTicks = FuelTicks(fuel), life = 0; float t = heat.Temperature;
            for (int i = 1; i <= ticks && life < fuelTicks; i++)
            {
                if (t != heat.AmbientTemperature)
                {
                    t -= (t - heat.AmbientTemperature) * heat.AmbientDecayRate;
                    if (Math.Abs(t - heat.AmbientTemperature) < 0.5f) t = heat.AmbientTemperature;
                }
                if (t < CookingService.MinimumFiniteCookingTemperature) break;
                life = i;
            }
            int burn = Math.Min(ticks, Math.Min(life, fuelTicks));
            return new Night(burn, FuelFor(fuel, burn), burn < ticks);
        }

        /// <summary>Null when the fire can keep a night (at least a normal rest, or all of a shorter one); otherwise the refusal code and the sentence naming what would fix it.</summary>
        static (string code, string text)? Refusal(CampfirePart fire, FuelPart fuel, ThermalPart heat, int ticks, out Night night)
        {
            night = default;
            if (heat == null || !UsableRate(fuel)) return ("invalid_fire", "That fire cannot be rested beside.");
            if (fuel.FuelMass <= 0) return ("no_fuel", "The fire has no fuel. Feed it and light it before you settle down.");
            if (!CampfireWorkActions.Hot(fire)) return ("fire_out", "The fire is out. Light it before you settle down.");
            night = Plan(fuel, heat, ticks); int minimum = Math.Min(ticks, RestSystem.RestClockTurns);
            if (night.BurnTicks >= minimum) return null;
            if (FuelTicks(fuel) < minimum)
                return ("fuel_short", "The fire would not last the night: it needs about " + FuelFor(fuel, minimum) + " fuel and there are " + (int)Math.Floor(fuel.FuelMass) + ".");
            return ("fire_low", "The fire is too low to keep through the night. Relight it from a flame first.");
        }

        // ── rows ──

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            var fire = ParentEntity?.GetPart<CampfirePart>();
            if (actions == null || fire == null || !CampfireWorkActions.Source(actor, fire, zone) || !CampfireWorkActions.Fuel(fire, out var fuel)) return;
            AddRow(actions, fire, fuel, "HearthRest", "rest", RestCommand, 'r', 20, RestSystem.RestClockTurns);
            var clock = TurnManager.Active;
            if (clock != null && clock.TickCount >= 0)
                AddRow(actions, fire, fuel, "HearthRestBand", "rest until the next time of day", BandCommand, 's', 19, RestSystem.NextBandAdvance(clock.TickCount));
        }

        static void AddRow(InventoryActionList actions, CampfirePart fire, FuelPart fuel, string name, string label, string command, char key, int priority, int ticks)
        {
            var refusal = Refusal(fire, fuel, fire.ParentEntity.GetPart<ThermalPart>(), ticks, out var night);
            if (refusal == null) actions.AddAction(name, label + " (the fire burns about " + night.Fuel + " fuel" + (night.BurnsOut ? ", then goes out" : "") + ")", command, key, priority, false, ActionTime.Free);
            else actions.AddDisabled(name, label, refusal.Value.code, refusal.Value.text, key, priority);
        }

        // ── the rest ──

        bool TryRest(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var fire = ParentEntity?.GetPart<CampfirePart>(); var clock = TurnManager.Active;
            if (actor == null || zone == null || fire == null || clock == null || clock.TickCount < 0 || !CampfireWorkActions.Source(actor, fire, zone) || !CampfireWorkActions.Fuel(fire, out var fuel))
                return Block(actor, "invalid_rest_context", "You cannot settle down here right now.");
            int ticks = command == BandCommand ? RestSystem.NextBandAdvance(clock.TickCount) : RestSystem.RestClockTurns;
            var heat = ParentEntity.GetPart<ThermalPart>();
            var refusal = Refusal(fire, fuel, heat, ticks, out var night);
            if (refusal != null) return Block(actor, refusal.Value.code, refusal.Value.text);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command)) return Block(actor, "in_progress", "The fire is already being tended.");
                // The shared rest decides first (hostiles, a dead actor, the clock) and queues every benefit for commit; fuel is charged only once it has been admitted.
                bool admitted = command == BandCommand ? RestSystem.TryRestUntilNextBandWithTransaction(actor, zone, Site, out _, tx) : RestSystem.TryRestWithTransaction(actor, zone, Site, out _, tx);
                if (!admitted) return false;
                float before = fuel.FuelMass, heatBefore = heat.Temperature; var owner = ParentEntity;
                float after = Math.Max(0f, before - night.Fuel);
                // The night passes: the fuel is spent and the fire cools as it would have turn by turn. A fire that burned out in the night (its fuel or its heat ran out before the
                // night did) is cold and dark in the morning, with whatever fuel it did not burn still in it.
                tx.Do(() => { fuel.FuelMass = after; heat.Temperature = night.BurnsOut || after <= 0f ? heat.AmbientTemperature : Cooled(heat, ticks); }, () => { fuel.FuelMass = before; heat.Temperature = heatBefore; });
                tx.AfterCommit(() => { owner.GetPart<HearthGlowPart>()?.SyncLight(); Diag.Record("furniture", "HearthRested", actor, owner, new { fuel = night.Fuel, remaining = fuel.FuelMass, ticks, burnsOut = night.BurnsOut }); });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "RestBlocked", actor: actor, target: ParentEntity, payload: new { site = Site, reason });
            return false;
        }
    }
}
