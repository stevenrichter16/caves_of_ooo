namespace CavesOfOoo.Core
{
    /// <summary>Cold worldgen predicates. Indicator fauna must be placed
    /// beside the actual authored microhabitat, not uniformly across rock.</summary>
    public static class StumpFaunaHabitat
    {
        public static bool Allows(string blueprint, Cell cell)
        {
            switch (blueprint)
            {
                // Cascade indicators occupy actual spray, not nearby dry rock or standing water.
                case "CascadeFather": return Contains(cell, "SprayPool");
                case "BrocchiniaSentinel": return Near(cell, "TankBrocchinia", 2);
                case "SummitSinger":
                case "HelmwoodFrog": return Near(cell, "Tree", 2);
                // P5: the riverwarden lies in or beside standing water.
                case "Riverwarden":
                case "OldRiverwarden": return NearLiquid(cell, 1);
                default: return true;
            }
        }
        public static bool Contains(Cell cell, string blueprint)
        {
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].BlueprintName == blueprint) return true;
            return false;
        }
        private static bool NearLiquid(Cell cell, int radius)
        {
            if (cell?.ParentZone == null) return false;
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var c = cell.ParentZone.GetCell(cell.X + dx, cell.Y + dy);
                    if (c == null) continue;
                    for (int i = 0; i < c.Objects.Count; i++)
                        if (c.Objects[i].GetPart<LiquidPoolPart>()?.Volume > 0) return true;
                }
            return false;
        }

        private static bool Near(Cell cell, string blueprint, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                    if (Contains(cell.ParentZone.GetCell(cell.X + dx, cell.Y + dy), blueprint)) return true;
            return false;
        }
    }
}
