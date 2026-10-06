namespace CavesOfOoo.Core
{
    /// <summary>Existing cold state determines whether actual water can be drawn.
    /// This query creates no temperature simulation or liquid units.</summary>
    public static class LiquidSourcePhase
    {
        public static bool CanDrawWater(Zone zone, Entity source)
        {
            if (source == null || zone == null || source.SpatialZone != zone) return false;
            var cell = zone.GetEntityCell(source); var physics = source.GetPart<PhysicsPart>();
            // Existing literal pools/springs/wells may have no PhysicsPart. Phase
            // admission preserves that source contract, with actual world ownership.
            if (cell?.ParentZone != zone || !cell.Objects.Contains(source)
                || source.HasPart<SpatialFootprintPart>() && !zone.IsFootprintCurrent(source)
                || physics != null && (physics.ParentEntity != source || physics.Takeable || physics.InInventory != null || physics.Equipped != null)
                || source.GetEffect<FrozenEffect>()?.Cold > 0) return false;
            var heat = source.GetPart<ThermalPart>();
            if (heat != null && (!WorldResourceActions.Finite(heat.Temperature) || !WorldResourceActions.Finite(heat.FreezeTemperature)
                || heat.Temperature <= heat.FreezeTemperature)) return false;
            foreach (var occupied in zone.GetOccupiedCells(source)) if (zone.TileState.HasCoating(occupied.X, occupied.Y, "ice")) return false;
            return true;
        }
    }
}
