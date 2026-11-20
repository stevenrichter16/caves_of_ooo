namespace CavesOfOoo.Core
{
    /// <summary>
    /// A finite cooking fire gives light only while it is actually burning: hot enough to cook and with fuel left. Stateless: it reads the owner's Campfire, Thermal and Fuel and
    /// switches the owner's LightSource on or off once per turn, and spends the fuel a burning hearth burns, so a hearth you feed lights the night for a bounded time and a spent one goes dark.
    /// </summary>
    public sealed class HearthGlowPart : Part
    {
        public override string Name => "HearthGlow";

        internal bool Burning()
        {
            var owner = ParentEntity; var fire = owner?.GetPart<CampfirePart>(); var heat = owner?.GetPart<ThermalPart>(); var fuel = owner?.GetPart<FuelPart>();
            return fire != null && fire.FiniteCooking && heat != null && fuel != null && WorldResourceActions.Finite(heat.Temperature) && WorldResourceActions.Finite(fuel.FuelMass)
                && heat.Temperature >= CookingService.MinimumFiniteCookingTemperature && fuel.FuelMass > 0;
        }

        /// <summary>How many turns a fire at its current temperature stays at or above cooking heat, by the thermal part's own per-turn decay (with its snap to ambient). A fire that is
        /// already below cooking heat has none. The lifetime of one lighting, whatever fuel is left.</summary>
        internal static int HeatTurns(ThermalPart heat)
        {
            if (heat == null || !WorldResourceActions.Finite(heat.Temperature)) return 0;
            float t = heat.Temperature; int turns = 0;
            if (t < CookingService.MinimumFiniteCookingTemperature) return 0;
            for (int i = 1; i <= 2000; i++)
            {
                if (t != heat.AmbientTemperature)
                {
                    t -= (t - heat.AmbientTemperature) * heat.AmbientDecayRate;
                    if (System.Math.Abs(t - heat.AmbientTemperature) < 0.5f) t = heat.AmbientTemperature;
                }
                if (t < CookingService.MinimumFiniteCookingTemperature) break;
                turns = i;
            }
            return turns;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == InstallEvents.Uninstalled) { PutOut(); return true; }
            if (e.ID != "EndTurn") return true;
            var owner = ParentEntity; var zone = owner?.SpatialZone; var light = owner?.GetPart<LightSourcePart>();
            if (zone == null || light == null) return true;   // a carried hearth is a cold kit and keeps its state
            // A burning hearth spends its fuel a turn at a time, so a timber is a bounded stretch of fire and the fire goes out when the fuel does. (Relighting a finite
            // fire needs fuel but, in the shipped relight, spends none; without this a single feed would be a permanent fire with any flame to relight it.)
            if (Burning()) { var fuel = owner.GetPart<FuelPart>(); fuel.FuelMass = System.Math.Max(0f, fuel.FuelMass - System.Math.Max(0f, fuel.BurnRate)); }
            SyncLight();
            return true;
        }

        /// <summary>Switches the light to match whether the fire is burning, telling the light map when it changes. Also used by a rest that spends the night's fuel.</summary>
        internal void SyncLight()
        {
            var light = ParentEntity?.GetPart<LightSourcePart>(); if (light == null) return;
            bool burning = Burning();
            if (light.Enabled == burning) return;
            light.Enabled = burning;
            // The light map caches on entity and equipment versions, and the incremental render skips it, so a toggle must say so (as the torch does) or the light would not
            // show, or go out, until something else moved.
            EquipmentChangeBus.NotifyChanged(null); ZoneRenderHooks.MarkFullDirty("Hearth.Glow");
        }

        /// <summary>
        /// Taking a burning hearth up puts its fire out: a carried pan keeps its fuel but not its flame, so the next set-down needs a flame again. Without this a single
        /// flame would make a fire you could carry lit for ever, since a carried item takes no turns and would never cool.
        /// </summary>
        void PutOut()
        {
            var owner = ParentEntity; var heat = owner?.GetPart<ThermalPart>(); var light = owner?.GetPart<LightSourcePart>();
            if (heat != null && WorldResourceActions.Finite(heat.AmbientTemperature)) heat.Temperature = heat.AmbientTemperature;
            if (light != null && light.Enabled) { light.Enabled = false; EquipmentChangeBus.NotifyChanged(null); ZoneRenderHooks.MarkFullDirty("Hearth.Glow"); }
        }
    }
}
