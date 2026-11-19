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

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EndTurn") return true;
            var owner = ParentEntity; var zone = owner?.SpatialZone; var light = owner?.GetPart<LightSourcePart>();
            if (zone == null || light == null) return true;   // a carried hearth is a cold kit and keeps its state
            // A burning hearth spends its fuel a turn at a time, so a timber is a bounded stretch of fire and the fire goes out when the fuel does. (Relighting a finite
            // fire needs fuel but, in the shipped relight, spends none; without this a single feed would be a permanent fire with any flame to relight it.)
            if (Burning()) { var fuel = owner.GetPart<FuelPart>(); fuel.FuelMass = System.Math.Max(0f, fuel.FuelMass - System.Math.Max(0f, fuel.BurnRate)); }
            bool burning = Burning();
            if (light.Enabled == burning) return true;
            light.Enabled = burning;
            var cell = zone.GetEntityCell(owner);
            if (cell != null && ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell)) ZoneRenderHooks.MarkCellDirty(cell, "Hearth.Glow");
            return true;
        }
    }
}
