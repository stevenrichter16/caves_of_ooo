using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Computes per-cell light levels from all light sources in a zone.
    /// Each cell gets a color tint and brightness level that the renderer
    /// uses to modulate foreground and background colors.
    /// </summary>
    public class LightMap
    {
        private float[,] _brightness;
        private Color[,] _tint;
        private int _lastEntityVersion = -1;

        /// <summary>
        /// Ambient light level for cells not reached by any light source.
        /// 0.0 = pitch black, 1.0 = fully lit.
        /// </summary>
        public float AmbientLevel = 0.4f;

        public LightMap()
        {
            _brightness = new float[Zone.Width, Zone.Height];
            _tint = new Color[Zone.Width, Zone.Height];
        }

        /// <summary>
        /// Recompute light levels from all light sources in the zone.
        /// Call once per render pass, after FOV has been computed.
        /// Skips recomputation if no entities have changed since last call.
        /// </summary>
        public void Compute(Zone zone)
        {
            if (zone.EntityVersion == _lastEntityVersion)
                return;
            _lastEntityVersion = zone.EntityVersion;

            // Reset to ambient with biome tint
            Color baseTint = zone.AmbientTint;
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    _brightness[x, y] = AmbientLevel;
                    _tint[x, y] = baseTint;
                }
            }

            // Accumulate light from each source — iterate _entityCells.Keys directly
            // via the zone's entity collection to avoid allocating a temporary list.
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                var light = entity.GetPart<LightSourcePart>();
                if (light == null) continue;

                var cell = zone.GetEntityCell(entity);
                if (cell == null) continue;

                Color lightColor = QudColorParser.Parse(light.LightColor);
                AddLight(zone, cell.X, cell.Y, light.Radius, light.Intensity, lightColor);
            }
        }

        private void AddLight(Zone zone, int srcX, int srcY, int radius, float intensity, Color color)
        {
            int r2 = radius * radius;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = srcX + dx;
                    int y = srcY + dy;
                    if (x < 0 || x >= Zone.Width || y < 0 || y >= Zone.Height)
                        continue;

                    int dist2 = dx * dx + dy * dy;
                    if (dist2 > r2) continue;

                    // Check if light can reach this cell (simple line-of-sight)
                    if (!HasLineOfSight(zone, srcX, srcY, x, y))
                        continue;

                    // Linear falloff
                    float dist = Mathf.Sqrt(dist2);
                    float falloff = 1f - (dist / (radius + 1));
                    float contribution = intensity * falloff;

                    // Add brightness (clamped to 1.0)
                    float current = _brightness[x, y];
                    _brightness[x, y] = Mathf.Min(1f, current + contribution);

                    // Blend tint toward light color
                    Color existingTint = _tint[x, y];
                    float blend = contribution * 0.5f; // subtle tinting
                    _tint[x, y] = Color.Lerp(existingTint, color, blend);
                }
            }
        }

        private bool HasLineOfSight(Zone zone, int x0, int y0, int x1, int y1)
        {
            // Bresenham line — check intermediate cells for walls
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int cx = x0, cy = y0;

            while (cx != x1 || cy != y1)
            {
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; cx += sx; }
                if (e2 < dx) { err += dx; cy += sy; }

                // Don't check the final cell
                if (cx == x1 && cy == y1) break;

                var cell = zone.GetCell(cx, cy);
                if (cell != null && cell.IsWall())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get the light brightness at a cell (0.0 to 1.0).
        /// </summary>
        public float GetBrightness(int x, int y)
        {
            if (x < 0 || x >= Zone.Width || y < 0 || y >= Zone.Height)
                return AmbientLevel;
            return _brightness[x, y];
        }

        /// <summary>
        /// Get the light tint color at a cell.
        /// </summary>
        public Color GetTint(int x, int y)
        {
            if (x < 0 || x >= Zone.Width || y < 0 || y >= Zone.Height)
                return Color.white;
            return _tint[x, y];
        }

        /// <summary>
        /// Apply light to a foreground color — multiplies by brightness and blends tint.
        /// </summary>
        public Color ApplyToColor(Color baseColor, int x, int y)
        {
            float b = GetBrightness(x, y);
            Color t = GetTint(x, y);
            return new Color(
                baseColor.r * b * t.r,
                baseColor.g * b * t.g,
                baseColor.b * b * t.b,
                baseColor.a
            );
        }
    }
}
