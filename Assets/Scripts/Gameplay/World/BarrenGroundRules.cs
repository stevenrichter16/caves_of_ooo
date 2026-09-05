namespace CavesOfOoo.Core
{
    /// <summary>Persistent rooted-flora exclusion. Content classifies
    /// vegetation explicitly; Plant/Wood materials also cover carried food
    /// and furniture and therefore cannot supply this rule.</summary>
    public static class BarrenGroundRules
    {
        public static bool IsVegetation(Entity entity)
            => entity != null && (entity.HasTag("Vegetation") || entity.HasPart<CropPart>() || entity.HasPart<FlowerCharmPart>());
        public static bool IsBarren(Cell cell) => cell?.HasObjectWithTag("Barren") == true;

        /// <summary>Remove only rooted vegetation when barren ground is
        /// installed. No destruction/harvest/reputation events are implied.</summary>
        public static void ClearVegetation(Zone zone, Cell cell)
        {
            if (zone == null || cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
                if (IsVegetation(cell.Objects[i])) zone.RemoveEntity(cell.Objects[i]);
        }

        /// <summary>Loaded cells precede the zone indexes; clean the raw
        /// membership list before rebuilding those indexes, independent of
        /// saved object order. Runtime callers use ClearVegetation instead.</summary>
        internal static void RepairLoadedCell(Cell cell)
        {
            if (!IsBarren(cell)) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
                if (IsVegetation(cell.Objects[i])) cell.Objects.RemoveAt(i);
        }
    }
}
