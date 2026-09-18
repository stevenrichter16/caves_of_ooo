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
        // Cache key also tracks EquipmentChangeBus.GlobalVersion so
        // equipping/unequipping invalidates the LightMap cache immediately
        // — closes the T2.2 cache-staleness 🟡 finding (commit cd355b5).
        private int _lastEquipmentVersion = -1;

        // W0.2 — the rest of the cache key. Two bugs lived in its
        // absence:
        //   (a) ambient/tint changes rendered NOTHING until some entity
        //       happened to move, which would have made the catacomb
        //       light-economy and the day-cycle silently inert;
        //   (b) the renderer keeps ONE LightMap across zone transitions
        //       (ZoneRenderer.SetZone never replaces it), so two zones
        //       with equal EntityVersion — trivially, two freshly-loaded
        //       ones — rendered each other's light. Latent while every
        //       zone shared 0.4; a showstopper the moment it doesn't.
        private Zone _lastZone;
        private string _lastMorrowfastDoors;
        private float _lastAmbientLevel = float.NaN;
        private Color _lastAmbientTint;

        /// <summary>
        /// Ambient light level of the zone this map was last computed
        /// for — the brightness of a cell no light source reaches.
        /// 0.0 = pitch black, 1.0 = fully lit.
        ///
        /// <para>Authored per-zone on <see cref="Zone.AmbientLevel"/>;
        /// this is the renderer-side echo of the last Compute, kept so
        /// callers like the mote-spawn gate can ask "is this cell
        /// brighter than the room it's in?" without a Zone reference.</para>
        /// </summary>
        public float AmbientLevel { get; private set; } = Zone.DefaultAmbientLevel;

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
            int currentEquipmentVersion = EquipmentChangeBus.GlobalVersion;
            string currentDoors = zone.ZoneID == MorrowfastSceneRuntime.ZoneID ? MorrowfastSceneRuntime.GetState(zone)?.OpenDoorIds : null;
            // Compare the ambient inputs by VALUE, not by bumping a
            // version — the day-cycle would otherwise force a full
            // recompute every turn whether or not the light changed.
            if (ReferenceEquals(zone, _lastZone)
                && zone.EntityVersion == _lastEntityVersion
                && currentEquipmentVersion == _lastEquipmentVersion
                && currentDoors == _lastMorrowfastDoors
                && zone.AmbientLevel == _lastAmbientLevel
                && zone.AmbientTint == _lastAmbientTint)
                return;
            _lastZone = zone;
            _lastEntityVersion = zone.EntityVersion;
            _lastEquipmentVersion = currentEquipmentVersion;
            _lastMorrowfastDoors = currentDoors;
            _lastAmbientLevel = zone.AmbientLevel;
            _lastAmbientTint = zone.AmbientTint;
            AmbientLevel = zone.AmbientLevel;

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
            //
            // Two passes per entity:
            //   Pass 1 (existing): the entity itself is a light source (torch on
            //     the floor, glowing creature, lantern post).
            //   Pass 2 (T2.2): the entity holds equipped items that emit light
            //     (held FlamingSword glows red around the wielder, IceSword cyan,
            //     etc.). Light radiates from the WIELDER'S cell, not the item's
            //     blueprint cell — equipped items don't have an independent zone
            //     position. Radius/Intensity/LightColor come from the item's
            //     LightSourcePart, blueprinted on the weapon.
            //
            // Cache invalidation: the cache key combines zone.EntityVersion
            // (covers entity moves/adds/removes — Pass 1 light sources) with
            // EquipmentChangeBus.GlobalVersion (covers equip/unequip events —
            // Pass 2 equipped lights). Equipping a glowing weapon now
            // updates the next render frame even if the wielder doesn't move.
            // Closes the T2.2 v1 🟡 finding originally documented here.
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                // Pass 1: the entity itself is a light source.
                var ownLight = entity.GetPart<LightSourcePart>();
                if (ownLight != null)
                {
                    var cell = zone.GetEntityCell(entity);
                    if (cell != null)
                    {
                        Color lightColor = QudColorParser.Parse(ownLight.LightColor);
                        AddLight(zone, cell.X, cell.Y, ownLight.Radius,
                                 ownLight.Intensity, lightColor);
                    }
                }

                // Pass 2 (T2.2): equipped items project light at the wielder's cell.
                // Cheap fast path: skip if the entity has no inventory.
                var inv = entity.GetPart<InventoryPart>();
                if (inv == null || inv.EquippedItems == null || inv.EquippedItems.Count == 0)
                    continue;

                var wielderCell = zone.GetEntityCell(entity);
                if (wielderCell == null) continue;

                // Iterate EquippedItems values directly (no allocation).
                // Multi-slot items appear once per occupied slot in the dict;
                // a HashSet dedupe avoids double-counting the same item's light.
                // The set is stack-allocated-equivalent (small N — typical
                // wielder has 0-5 equipped items) so this is cheap.
                _equipDedupe.Clear();
                foreach (var kvp in inv.EquippedItems)
                {
                    var item = kvp.Value;
                    if (item == null) continue;
                    if (!_equipDedupe.Add(item)) continue;

                    var itemLight = item.GetPart<LightSourcePart>();
                    if (itemLight == null) continue;

                    Color itemColor = QudColorParser.Parse(itemLight.LightColor);
                    AddLight(zone, wielderCell.X, wielderCell.Y, itemLight.Radius,
                             itemLight.Intensity, itemColor);
                }
            }
        }

        /// <summary>
        /// Reusable dedupe set for the Pass 2 equipment walk. A multi-slot
        /// item (e.g. a two-handed weapon occupying both Hand slots) appears
        /// twice in EquippedItems; this set ensures the same LightSourcePart
        /// only contributes once. Reused across Compute calls to avoid
        /// per-frame allocation per CLAUDE.md §non-negotiable #3.
        /// </summary>
        private readonly System.Collections.Generic.HashSet<Entity> _equipDedupe
            = new System.Collections.Generic.HashSet<Entity>();

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
