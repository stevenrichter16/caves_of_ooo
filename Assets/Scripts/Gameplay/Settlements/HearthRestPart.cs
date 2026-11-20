using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Rest beside a standing camp hearth (Docs/MACHINE-INVENTED-USES.md, "a camp you can sleep at"). A rest is a clock jump that nothing simulates turn by turn, so a finite fire
    /// would otherwise keep its fuel through a night. This Part charges the honest amount: the fuel the fire burns in the ticks you sleep, ticks times its own burn rate (60 ticks
    /// at the hearth's 0.25 is 15 fuel). It is refused up front when the fire is out or could not last, with the exact numbers in the reason, and nothing changes on a refusal.
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

        /// <summary>Fuel the fire burns in the given ticks: ticks times its burn rate, rounded up. Negative means the fire's own numbers are unusable.</summary>
        static int Need(FuelPart fuel, int ticks)
            => !WorldResourceActions.Finite(fuel.BurnRate) || fuel.BurnRate <= 0 ? -1 : (int)Math.Ceiling(ticks * (double)fuel.BurnRate);

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

        /// <summary>Null when the fire could last the whole rest; otherwise the refusal code and the sentence naming the numbers. The fire must have fuel for the ticks and must not cool
        /// below cooking heat before they end, so one lighting is about one night and the next night needs a flame again.</summary>
        static (string code, string text)? Refusal(CampfirePart fire, FuelPart fuel, ThermalPart heat, int need, int ticks)
        {
            if (need < 0 || heat == null) return ("invalid_fire", "That fire cannot be rested beside.");
            if (fuel.FuelMass <= 0) return ("no_fuel", "The fire has no fuel. Feed it and light it before you settle down.");
            if (!CampfireWorkActions.Hot(fire)) return ("fire_out", "The fire is out. Light it before you settle down.");
            if (fuel.FuelMass < need)
                return ("fuel_short", "The fire would not last the rest: it would burn about " + need + " fuel and there are " + (int)Math.Floor(fuel.FuelMass) + ".");
            if (Cooled(heat, ticks) < CookingService.MinimumFiniteCookingTemperature)
                return ("fire_low", "The fire would die down before morning. Relight it from a flame first.");
            return null;
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
            var heat = fire.ParentEntity.GetPart<ThermalPart>();
            int need = Need(fuel, ticks); var refusal = Refusal(fire, fuel, heat, need, ticks);
            if (refusal == null) actions.AddAction(name, label + " (the fire burns about " + need + " fuel)", command, key, priority, false, ActionTime.Free);
            else actions.AddDisabled(name, label, refusal.Value.code, refusal.Value.text, key, priority);
        }

        // ── the rest ──

        bool TryRest(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var fire = ParentEntity?.GetPart<CampfirePart>(); var clock = TurnManager.Active;
            if (actor == null || zone == null || fire == null || clock == null || clock.TickCount < 0 || !CampfireWorkActions.Source(actor, fire, zone) || !CampfireWorkActions.Fuel(fire, out var fuel))
                return Block(actor, "invalid_rest_context", "You cannot settle down here right now.");
            int ticks = command == BandCommand ? RestSystem.NextBandAdvance(clock.TickCount) : RestSystem.RestClockTurns;
            var heat = ParentEntity.GetPart<ThermalPart>(); int need = Need(fuel, ticks);
            var refusal = Refusal(fire, fuel, heat, need, ticks);
            if (refusal != null) return Block(actor, refusal.Value.code, refusal.Value.text);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command)) return Block(actor, "in_progress", "The fire is already being tended.");
                // The shared rest decides first (hostiles, a dead actor, the clock) and queues every benefit for commit; fuel is charged only once it has been admitted.
                bool admitted = command == BandCommand ? RestSystem.TryRestUntilNextBandWithTransaction(actor, zone, Site, out _, tx) : RestSystem.TryRestWithTransaction(actor, zone, Site, out _, tx);
                if (!admitted) return false;
                float before = fuel.FuelMass, heatBefore = heat.Temperature; var owner = ParentEntity;
                float after = Math.Max(0f, before - need);
                // The night passes: the fuel is spent and the fire cools as it would have turn by turn; a fire that has burnt out is cold.
                tx.Do(() => { fuel.FuelMass = after; heat.Temperature = after <= 0f ? heat.AmbientTemperature : Cooled(heat, ticks); }, () => { fuel.FuelMass = before; heat.Temperature = heatBefore; });
                tx.AfterCommit(() => { owner.GetPart<HearthGlowPart>()?.SyncLight(); Diag.Record("furniture", "HearthRested", actor, owner, new { fuel = need, remaining = fuel.FuelMass, ticks }); });
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
