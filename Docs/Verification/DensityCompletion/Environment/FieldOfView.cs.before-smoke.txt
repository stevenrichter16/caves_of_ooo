namespace CavesOfOoo.Core
{
    /// <summary>
    /// Recursive shadowcasting field-of-view algorithm.
    /// Computes which cells are visible from a given origin within a zone.
    /// Updates Cell.IsVisible and Cell.Explored flags.
    /// </summary>
    public static class FieldOfView
    {
        // Multiplier arrays for the 8 octants of shadowcasting
        private static readonly int[] xx = { 1, 0, 0, -1, -1, 0, 0, 1 };
        private static readonly int[] xy = { 0, 1, -1, 0, 0, -1, 1, 0 };
        private static readonly int[] yx = { 0, 1, 1, 0, 0, -1, -1, 0 };
        private static readonly int[] yy = { 1, 0, 0, 1, -1, 0, 0, -1 };

        /// <summary>
        /// Compute FOV from (originX, originY) with the given radius.
        /// Clears all IsVisible flags first, then marks visible cells.
        /// </summary>
        public static void Compute(Zone zone, int originX, int originY, int radius)
        {
            // Clear visibility
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell != null)
                        cell.IsVisible = false;
                }
            }

            // Origin is always visible
            var originCell = zone.GetCell(originX, originY);
            if (originCell != null)
            {
                originCell.IsVisible = true;
                originCell.Explored = true;
            }

            // Cast light in all 8 octants
            for (int oct = 0; oct < 8; oct++)
            {
                CastLight(zone, originX, originY, radius, 1,
                    1.0f, 0.0f,
                    xx[oct], xy[oct], yx[oct], yy[oct]);
            }
        }

        private static void CastLight(Zone zone, int cx, int cy, int radius,
            int row, float startSlope, float endSlope,
            int dxx, int dxy, int dyx, int dyy)
        {
            if (startSlope < endSlope)
                return;

            float newStart = startSlope;
            int radiusSq = radius * radius;

            for (int j = row; j <= radius; j++)
            {
                int dx = -j - 1;
                int dy = -j;
                bool blocked = false;

                while (dx <= 0)
                {
                    dx++;
                    int mapX = cx + dx * dxx + dy * dxy;
                    int mapY = cy + dx * dyx + dy * dyy;

                    if (mapX < 0 || mapX >= Zone.Width || mapY < 0 || mapY >= Zone.Height)
                        continue;

                    float lSlope = (dx - 0.5f) / (dy + 0.5f);
                    float rSlope = (dx + 0.5f) / (dy - 0.5f);

                    if (rSlope > startSlope)
                        continue;
                    if (lSlope < endSlope)
                        break;

                    // Within radius?
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        var cell = zone.GetCell(mapX, mapY);
                        if (cell != null)
                        {
                            cell.IsVisible = true;
                            cell.Explored = true;
                        }
                    }

                    var checkCell = zone.GetCell(mapX, mapY);
                    bool isOpaque = checkCell != null && checkCell.IsWall();

                    if (blocked)
                    {
                        if (isOpaque)
                        {
                            newStart = rSlope;
                        }
                        else
                        {
                            blocked = false;
                            startSlope = newStart;
                        }
                    }
                    else if (isOpaque && j < radius)
                    {
                        blocked = true;
                        CastLight(zone, cx, cy, radius, j + 1,
                            startSlope, lSlope,
                            dxx, dxy, dyx, dyy);
                        newStart = rSlope;
                    }
                }

                if (blocked)
                    break;
            }
        }
    }
}
