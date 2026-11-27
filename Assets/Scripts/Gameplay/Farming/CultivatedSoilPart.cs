using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Saved cultivation marker on the existing terrain owner. It does
    /// not replace terrain, change collision, supply moisture, or advance time.</summary>
    public sealed class CultivatedSoilPart : Part
    {
        public override string Name => "CultivatedSoil";
        public int LastNaturalRainWindow = -1;

        /// <summary>Whether this exact current ground cell contains a usable
        /// prepared bed. Barren, flooded, carried and foreign ground is refused.</summary>
        public static bool IsCultivated(Zone zone, Cell cell)
        {
            if (!UsableCell(zone, cell)) return false;
            foreach (var terrain in cell.Objects)
                if (ReadSoil(terrain, zone, cell) != null) return true;
            return false;
        }

        internal static CultivatedSoilPart ExactBed(Zone zone, Cell cell)
        {
            if (!UsableCell(zone, cell)) return null;
            CultivatedSoilPart found = null;
            foreach (var terrain in cell.Objects)
            {
                var soil = ReadSoil(terrain, zone, cell);
                if (soil == null) continue;
                if (found != null || terrain.Parts.Count(part => part is CultivatedSoilPart) != 1) return null;
                found = soil;
            }
            return found;
        }

        private static bool UsableCell(Zone zone, Cell cell) => zone != null && cell != null && cell.ParentZone == zone
            && !BarrenGroundRules.IsBarren(cell) && !cell.HasObjectWithPart<LiquidPoolPart>();

        private static CultivatedSoilPart ReadSoil(Entity terrain, Zone zone, Cell cell)
        {
            var soil = terrain?.GetPart<CultivatedSoilPart>();
            var physics = terrain?.GetPart<PhysicsPart>();
            return soil != null && soil.ParentEntity == terrain && terrain.SpatialZone == zone
                && zone.GetEntityCell(terrain) == cell && terrain.HasTag("Terrain") && terrain.HasTag("Plantable")
                && !terrain.HasTag("Solid") && !terrain.HasTag("Creature") && !terrain.HasTag("Item")
                && physics != null && physics.ParentEntity == terrain && !physics.Solid && !physics.Takeable
                && physics.InInventory == null && physics.Equipped == null ? soil : null;
        }
    }
}
