namespace CavesOfOoo.Core
{
    /// <summary>Saved cultivation marker on the existing terrain owner. It does
    /// not replace terrain, change collision, supply moisture, or advance time.</summary>
    public sealed class CultivatedSoilPart : Part
    {
        public override string Name => "CultivatedSoil";

        /// <summary>Whether this exact current ground cell contains a usable
        /// prepared bed. Barren, flooded, carried and foreign ground is refused.</summary>
        public static bool IsCultivated(Zone zone, Cell cell)
        {
            if (zone == null || cell == null || cell.ParentZone != zone
                || BarrenGroundRules.IsBarren(cell) || cell.HasObjectWithPart<LiquidPoolPart>()) return false;
            foreach (var terrain in cell.Objects)
            {
                var soil = terrain?.GetPart<CultivatedSoilPart>();
                var physics = terrain?.GetPart<PhysicsPart>();
                if (soil != null && soil.ParentEntity == terrain && terrain.SpatialZone == zone
                    && zone.GetEntityCell(terrain) == cell && terrain.HasTag("Terrain") && terrain.HasTag("Plantable")
                    && !terrain.HasTag("Solid") && !terrain.HasTag("Creature") && !terrain.HasTag("Item")
                    && physics != null && physics.ParentEntity == terrain && !physics.Solid && !physics.Takeable
                    && physics.InInventory == null && physics.Equipped == null) return true;
            }
            return false;
        }
    }
}
