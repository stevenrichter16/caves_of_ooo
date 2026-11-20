using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Pouring something that does not burn onto your own lit hearth cools it (Docs/MACHINE-INVENTED-USES.md 3m). Until now the water was written to the ground and the fire was untouched, a
    /// gap in the plainest common-sense rule there is. The rule is small and uses what the world already has: a cold dose through the same <c>ApplyHeat</c> event the frost abilities use, so the
    /// crossing below flame temperature, the cooking threshold and the light all follow the fire's own thermal rules. Each unit cools by <see cref="DegreesPerUnit"/>; a fire at 450 needs about
    /// eight units to drop below cooking heat (150), so a full flask puts a hearth out and a single splash only banks it lower. The fuel it had is kept: dousing spends the water and the fire,
    /// not the timber. A liquid that burns (oil, pitch) never douses, and the rule touches only fixtures you set down yourself (a town's hearth is not yours to put out).
    /// </summary>
    public static class FireDouse
    {
        public const float DegreesPerUnit = 40f;

        /// <summary>True for a liquid that can put a fire out: one that is not itself fuel.</summary>
        public static bool Douses(string liquidId)
        {
            var def = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(liquidId) : null;
            if (def == null || def.Combustibility > 0) return false;
            // Water and the liquids that behave as water on the ground (bog mire, brine). A liquid that does not burn but is not water-like (lava, acid) is not a way to put a fire out.
            return def.Id == "water" || def.GroundReactionFamily == "water";
        }

        /// <summary>The player's own lit hearth standing in the cell, if any.</summary>
        public static Entity LitHearthAt(Zone zone, int x, int y)
        {
            if (zone == null || !zone.InBounds(x, y)) return null;
            foreach (var e in zone.GetOccupants(x, y))
            {
                if (e == null || e.HasTag("Creature") || !HeldFixtureActions.IsPlayerHeld(e)) continue;
                var fire = e.GetPart<CampfirePart>(); var heat = e.GetPart<ThermalPart>();
                var glow = e.GetPart<HearthGlowPart>();
                if (fire == null || !fire.FiniteCooking || heat == null || glow == null) continue;
                // Lit is what the hearth itself calls burning: hot enough to cook with fuel left. A spent hearth with coals still hot is already dark and is not "put out".
                if (glow.Burning()) return e;
            }
            return null;
        }

        /// <summary>What a pour of this liquid would do to a lit hearth there, for a row hint; null when it would do nothing to a fire.</summary>
        public static string Hint(Zone zone, int x, int y, string liquidId, int amount)
        {
            var hearth = LitHearthAt(zone, x, y); if (hearth == null || !Douses(liquidId) || amount <= 0) return null;
            return WouldPutOut(hearth, amount) ? "Puts out your hearth. It keeps its fuel." : "Cools your hearth; it burns lower.";
        }

        static bool WouldPutOut(Entity hearth, int amount)
            => Math.Max(hearth.GetPart<ThermalPart>().AmbientTemperature, hearth.GetPart<ThermalPart>().Temperature - amount * DegreesPerUnit) < CookingService.MinimumFiniteCookingTemperature;

        /// <summary>Called after a pour has committed. Cools the lit hearth in the cell by the poured amount and says what happened. Returns whether a fire was touched.</summary>
        public static bool Apply(Zone zone, Entity actor, int x, int y, string liquidId, int amount)
        {
            if (amount <= 0 || !Douses(liquidId)) return false;
            var hearth = LitHearthAt(zone, x, y); if (hearth == null) return false;
            var heat = hearth.GetPart<ThermalPart>(); float before = heat.Temperature;
            // Water cools a fire to the temperature of the air and no further: a dose that would take it below ambient is cut to what is left to lose (a hearth must never be frozen by a drink of water).
            float cool = Math.Min(amount * DegreesPerUnit, heat.Temperature - heat.AmbientTemperature);
            if (cool <= 0f) return false;
            var dose = GameEvent.New("ApplyHeat");
            dose.SetParameter("Joules", (object)(-cool * (heat.HeatCapacity > 0f ? heat.HeatCapacity : 1f)));
            dose.SetParameter("Radiant", (object)false); dose.SetParameter("Zone", (object)zone);
            hearth.FireEvent(dose); dose.Release();
            hearth.GetPart<HearthGlowPart>()?.SyncLight();
            bool extinguished = heat.Temperature < CookingService.MinimumFiniteCookingTemperature;
            MessageLog.Add(extinguished ? "The water hisses and your hearth goes out in a cloud of steam. It keeps its fuel." : "The water hisses on your hearth. It burns lower.");
            Diag.Record("furniture", "HearthDoused", actor, hearth, new { liquidId, amount, before, after = heat.Temperature, fuel = hearth.GetPart<FuelPart>()?.FuelMass ?? 0f, extinguished });
            return true;
        }
    }
}
